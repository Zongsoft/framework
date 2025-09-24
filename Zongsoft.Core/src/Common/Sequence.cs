/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@qq.com>
 *
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Zongsoft.Common;

public static class Sequence
{
	public static ISequence Variate(ISequence sequence)
	{
		if(sequence == null)
			throw new ArgumentNullException(nameof(sequence));

		return new VariableSequence(sequence);
	}

	private sealed class VariableSequence(ISequence sequence) : ISequence
	{
		private readonly ISequence _sequence = sequence;
		private readonly ConcurrentDictionary<string, Variator> _variators = new();

		public long Decrease(string key, int interval = 1, int seed = 0, TimeSpan? expiry = null) => this.Increase(key, -interval, seed, expiry);
		public double Decrease(string key, double interval, double seed = 0, TimeSpan? expiry = null) => this.Increase(key, -interval, seed, expiry);
		public ValueTask<long> DecreaseAsync(string key, int interval = 1, int seed = 0, TimeSpan? expiry = null, CancellationToken cancellation = default) => this.IncreaseAsync(key, -interval, seed, expiry, cancellation);
		public ValueTask<double> DecreaseAsync(string key, double interval, double seed = 0, TimeSpan? expiry = null, CancellationToken cancellation = default) => this.IncreaseAsync(key, -interval, seed, expiry, cancellation);

		public long Increase(string key, int interval = 1, int seed = 0, TimeSpan? expiry = null)
		{
			return _variators.GetOrAdd(key, key => new Variator(key, _sequence)).Increase(interval, seed);
		}

		public double Increase(string key, double interval, double seed = 0, TimeSpan? expiry = null) => throw new NotImplementedException();
		public ValueTask<long> IncreaseAsync(string key, int interval = 1, int seed = 0, TimeSpan? expiry = null, CancellationToken cancellation = default)
		{
			return _variators.GetOrAdd(key, key => new Variator(key, _sequence)).IncreaseAsync(interval, seed, cancellation);
		}

		public ValueTask<double> IncreaseAsync(string key, double interval, double seed = 0, TimeSpan? expiry = null, CancellationToken cancellation = default) => throw new NotImplementedException();

		public void Reset(string key, int value = 0, TimeSpan? expiry = null)
		{
			if(_variators.TryGetValue(key, out var variator))
				variator.Reset(value);
			else
				_sequence.Reset(key, value, expiry);
		}

		public void Reset(string key, double value, TimeSpan? expiry = null) => throw new NotImplementedException();
		public ValueTask ResetAsync(string key, int value = 0, TimeSpan? expiry = null, CancellationToken cancellation = default)
		{
			if(_variators.TryGetValue(key, out var variator))
				return variator.ResetAsync(value, cancellation);
			else
				return _sequence.ResetAsync(key, value, expiry, cancellation);
		}

		public ValueTask ResetAsync(string key, double value, TimeSpan? expiry = null, CancellationToken cancellation = default) => throw new NotImplementedException();
	}

	private sealed class Variator(string name, ISequence sequence)
	{
		private long _hits;
		private long _value;
		private long _threshold;
		private long _timestamp;
		private readonly string _name = name;
		private readonly ISequence _sequence = sequence;
		private readonly SemaphoreSlim _mutex = new(1, 1);

		public long Increase(int interval, int seed)
		{
			if(_value >= _threshold)
				return this.Acquire(interval, seed);

			var value = Interlocked.Add(ref _value, interval);

			if(value > _threshold)
				return this.Acquire(interval, seed);
			else
				return value;
		}

		public ValueTask<long> IncreaseAsync(int interval, int seed, CancellationToken cancellation)
		{
			if(_value >= _threshold)
				return this.AcquireAsync(interval, seed, cancellation);

			var value = Interlocked.Add(ref _value, interval);

			if(value >= _threshold)
				return this.AcquireAsync(interval, seed, cancellation);
			else
				return ValueTask.FromResult(value);
		}

		public void Reset(int value)
		{
			try
			{
				_mutex.Wait();
				_sequence.Reset(_name, value);

				_value = 0;
				_threshold = 0;
				_timestamp = 0;
			}
			finally
			{
				_mutex.Release();
			}
		}

		public async ValueTask ResetAsync(int value, CancellationToken cancellation)
		{
			try
			{
				await _mutex.WaitAsync(cancellation);
				await _sequence.ResetAsync(_name, value, null, cancellation);

				_value = 0;
				_threshold = 0;
				_timestamp = 0;
			}
			finally
			{
				_mutex.Release();
			}
		}

		private long Acquire(int interval, int seed)
		{
			const float RATIO = 2.5f;
			const int MAXIMUM = 2000;

			try
			{
				_mutex.Wait();

				var duration = _timestamp > 0 ? Environment.TickCount64 - _timestamp : 0;
				var length = duration <= 0 || duration > MAXIMUM ? interval :
					(int)Math.Max(1, MAXIMUM / duration * RATIO) * interval;

				Interlocked.Increment(ref _hits);
				_threshold = _sequence.Increase(_name, length, seed);
				_value = _threshold == seed ? seed : _threshold - length;
				_timestamp = Environment.TickCount64;
			}
			finally
			{
				_mutex.Release();
			}

			return this.Increase(interval, seed);
		}

		private async ValueTask<long> AcquireAsync(int interval, int seed, CancellationToken cancellation)
		{
			const float RATIO = 2.5f;
			const int MAXIMUM = 2000;

			try
			{
				await _mutex.WaitAsync(cancellation);

				var duration = _timestamp > 0 ? Environment.TickCount64 - _timestamp : 0;
				var length = duration <= 0 || duration > MAXIMUM ? interval :
					(int)Math.Max(1, MAXIMUM / duration * RATIO) * interval;

				Interlocked.Increment(ref _hits);
				_threshold = await _sequence.IncreaseAsync(_name, length, seed, null, cancellation);
				_value = _threshold == seed ? seed : _threshold - length;
				_timestamp = Environment.TickCount64;
			}
			finally
			{
				_mutex.Release();
			}

			return await this.IncreaseAsync(interval, seed, cancellation);
		}
	}
}
