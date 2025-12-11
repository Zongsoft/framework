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

namespace Zongsoft.Common;

public static class Sequence
{
	/// <summary>为指定的序列号提供程序包装一个自适应可变速率的序列号器。</summary>
	/// <param name="sequence">指定的待包装的序列号器。</param>
	/// <returns>返回自适应可变速率的序列号器。</returns>
	/// <exception cref="ArgumentNullException">指定的<paramref name="sequence"/>参数为空(<c>null</c>)。</exception>
	/// <remarks>
	/// 	<para>序列号器的实现一般依赖于 Redis、Etcd、数据库等进行持久化，每次调用都会导致网络通讯，这在高频调用场景下会出现性能瓶颈问题，譬如在批量写数据时需要频繁获取序列号。</para>
	/// 	<para>该方法会返回一个包装指定序列号提供程序的自适应可变速率的序号器，它会根据调用频率来加大递增(减)的步长(<c>interval</c>)，然后在本地进行递增(减)，待本地计数器溢满后再根据频率来调用远程的序列号器。</para>
	/// </remarks>
	public static ISequenceBase Variate(ISequenceBase sequence)
	{
		if(sequence == null)
			throw new ArgumentNullException(nameof(sequence));

		return sequence is VariableSequence variable ? variable : new VariableSequence(sequence);
	}

	#region 嵌套子类
	private sealed class VariableSequence(ISequenceBase sequence) : ISequenceBase
	{
		private readonly ISequenceBase _sequence = sequence;
		private readonly ConcurrentDictionary<string, Variator> _variators = new();

		public long Decrease(string key, int interval = 1, int seed = 0) => this.Increase(key, -interval, seed);
		public ValueTask<long> DecreaseAsync(string key, int interval = 1, int seed = 0, CancellationToken cancellation = default) => this.IncreaseAsync(key, -interval, seed, cancellation);

		public long Increase(string key, int interval = 1, int seed = 0)
		{
			return _variators.GetOrAdd(key, key => new Variator(key, _sequence)).Increase(interval, seed);
		}

		public ValueTask<long> IncreaseAsync(string key, int interval = 1, int seed = 0, CancellationToken cancellation = default)
		{
			return _variators.GetOrAdd(key, key => new Variator(key, _sequence)).IncreaseAsync(interval, seed, cancellation);
		}

		public void Reset(string key, int value = 0)
		{
			if(_variators.TryGetValue(key, out var variator))
				variator.Reset(value);
			else
				_sequence.Reset(key, value);
		}

		public ValueTask ResetAsync(string key, int value = 0, CancellationToken cancellation = default)
		{
			if(_variators.TryGetValue(key, out var variator))
				return variator.ResetAsync(value, cancellation);
			else
				return _sequence.ResetAsync(key, value, cancellation);
		}
	}

	private sealed class Variator(string name, ISequenceBase sequence)
	{
		#region 常量定义
		const float ACQUIRE_RATIO = 2.0f;
		const float ACQUIRE_INTERVAL = 2000;
		#endregion

		#region 成员变量
		private long _hits;
		private long _value;
		private int _interval;
		private long _threshold;
		private long _timestamp;
		private readonly string _name = name;
		private readonly ISequenceBase _sequence = sequence;
		private readonly SemaphoreSlim _mutex = new(1, 1);
		#endregion

		#region 公共方法
		public void Reset(int value)
		{
			try
			{
				_mutex.Wait();
				_sequence.Reset(_name, value);

				_hits = 0;
				_value = 0;
				_interval = 0;
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
				await _sequence.ResetAsync(_name, value, cancellation);

				_hits = 0;
				_value = 0;
				_interval = 0;
				_threshold = 0;
				_timestamp = 0;
			}
			finally
			{
				_mutex.Release();
			}
		}

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

			if(value > _threshold)
				return this.AcquireAsync(interval, seed, cancellation);
			else
				return ValueTask.FromResult(value);
		}
		#endregion

		#region 私有方法
		private long Acquire(int interval, int seed)
		{
			try
			{
				_mutex.Wait();

				if(_value >= _threshold)
				{
					var duration = _timestamp > 0 ? Environment.TickCount64 - _timestamp : ACQUIRE_INTERVAL;
					var length = duration switch
					{
						<= 0 => Math.Max(1, _interval) * 2,
						>= ACQUIRE_INTERVAL => interval,
						_ => (int)Math.Max(1, ACQUIRE_INTERVAL / duration * ACQUIRE_RATIO) * interval,
					};

					Interlocked.Increment(ref _hits);
					_threshold = _sequence.Increase(_name, length, seed);
					_value = _threshold == seed ? seed : _threshold - length;
					_timestamp = Environment.TickCount64;
					_interval = length;
				}
			}
			finally
			{
				_mutex.Release();
			}

			return this.Increase(interval, seed);
		}

		private async ValueTask<long> AcquireAsync(int interval, int seed, CancellationToken cancellation)
		{
			try
			{
				await _mutex.WaitAsync(cancellation);

				if(_value >= _threshold)
				{
					var duration = _timestamp > 0 ? Environment.TickCount64 - _timestamp : ACQUIRE_INTERVAL;
					var length = duration switch
					{
						<= 0 => Math.Max(1, _interval) * 2,
						>= ACQUIRE_INTERVAL => interval,
						_ => (int)Math.Max(1, ACQUIRE_INTERVAL / duration * ACQUIRE_RATIO) * interval,
					};

					Interlocked.Increment(ref _hits);
					_threshold = await _sequence.IncreaseAsync(_name, length, seed, cancellation);
					_value = _threshold == seed ? seed : _threshold - length;
					_timestamp = Environment.TickCount64;
					_interval = length;
				}
			}
			finally
			{
				_mutex.Release();
			}

			return await this.IncreaseAsync(interval, seed, cancellation);
		}
		#endregion

		#region 重写方法
		public override string ToString() => $"{_value}/{_threshold}({_hits}|{_interval})@{TimeSpan.FromMilliseconds(_timestamp)}";
		#endregion
	}
	#endregion
}
