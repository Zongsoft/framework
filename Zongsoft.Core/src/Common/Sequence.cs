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
	/// <param name="options">指定的自适应可变速率序号选项。</param>
	/// <returns>返回自适应可变速率的序列号器。</returns>
	/// <exception cref="ArgumentNullException">指定的<paramref name="sequence"/>参数为空(<c>null</c>)。</exception>
	/// <remarks>
	/// 	<para>序列号器的实现一般依赖于 Redis、Etcd、数据库等进行持久化，这在高频调用场景下会出现性能瓶颈问题，譬如在批量写数据时需要频繁获取序列号。</para>
	/// 	<para>该方法会返回一个包装指定序列号提供程序的自适应可变速率的序号器，它会根据调用频率来加大递增(减)的步长(<c>interval</c>)，然后在本地进行递增(减)，待本地计数器溢满后再根据频率来调用远程的序列号器。</para>
	/// </remarks>
	public static IVariator Variate(this ISequenceBase sequence, VariatorOptions options = null)
	{
		if(sequence == null)
			throw new ArgumentNullException(nameof(sequence));

		return sequence is SequenceVariator variator ? variator : new SequenceVariator(sequence, options);
	}

	#region 公共嵌套
	public sealed class VariatorOptions
	{
		#region 常量定义
		const int GROWTH_LOWER = 100;
		const int GROWTH_UPPER = 1000;
		#endregion

		#region 单例字段
		public static readonly VariatorOptions Default = new(0, GROWTH_LOWER, GROWTH_UPPER);
		#endregion

		#region 构造函数
		public VariatorOptions(int growthLower, int growthUpper) : this(0, growthLower, growthUpper) { }
		public VariatorOptions(int initiate, int growthLower, int growthUpper)
		{
			ArgumentOutOfRangeException.ThrowIfNegative(initiate);
			ArgumentOutOfRangeException.ThrowIfNegative(growthLower);
			ArgumentOutOfRangeException.ThrowIfNegative(growthUpper);

			this.GrowthLower = Math.Min(growthLower, growthUpper);
			this.GrowthUpper = Math.Max(growthLower, growthUpper);
			this.Initiate = Math.Min(initiate, this.GrowthUpper);
		}
		#endregion

		#region 公共属性
		/// <summary>获取批次递增(减)的起增量。</summary>
		public int Initiate { get; }
		/// <summary>获取高频递增(减)的最小步长。</summary>
		public int GrowthLower { get; }
		/// <summary>获取高频递增(减)的最大步长。</summary>
		public int GrowthUpper { get; }
		#endregion

		#region 重写方法
		public override string ToString() => $"{this.Initiate}({this.GrowthLower}~{this.GrowthUpper})";
		#endregion
	}

	public interface IVariator : ISequenceBase
	{
		VariatorOptions Options { get; }
		IVariatorStatistics GetStatistics(string key);
	}

	public interface IVariatorStatistics
	{
		/// <summary>获取被递增请求的次数。</summary>
		long Count { get; }
		/// <summary>获取本地的计数器数值。</summary>
		long Value { get; }
		/// <summary>获取最近递增请求的步长。</summary>
		int Interval { get; }
		/// <summary>获取最长的递增请求步长。</summary>
		int Longest { get; }
		/// <summary>获取本地计数器的阈值。</summary>
		long Threshold { get; }
		/// <summary>获取最近递增请求的时间。</summary>
		DateTime Timestamp { get; }
	}
	#endregion

	#region 私有嵌套
	private sealed class SequenceVariator(ISequenceBase sequence, VariatorOptions options = null) : IVariator, ISequenceBase
	{
		private readonly ISequenceBase _sequence = sequence;
		private readonly VariatorOptions _options = options ?? VariatorOptions.Default;
		private readonly ConcurrentDictionary<string, Variator> _variators = new();

		public VariatorOptions Options => options;
		public IVariatorStatistics GetStatistics(string key) => _variators.TryGetValue(key, out var variator) ? variator : null;

		public long Decrease(string key, int interval = 1, int seed = 0) => this.Increase(key, -interval, seed);
		public ValueTask<long> DecreaseAsync(string key, int interval = 1, int seed = 0, CancellationToken cancellation = default) => this.IncreaseAsync(key, -interval, seed, cancellation);

		public long Increase(string key, int interval = 1, int seed = 0)
		{
			return _variators.GetOrAdd(key, key => new Variator(key, _sequence, _options)).Increase(interval, seed);
		}

		public ValueTask<long> IncreaseAsync(string key, int interval = 1, int seed = 0, CancellationToken cancellation = default)
		{
			return _variators.GetOrAdd(key, key => new Variator(key, _sequence, _options)).IncreaseAsync(interval, seed, cancellation);
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

	private sealed class Variator(string name, ISequenceBase sequence, VariatorOptions options) : IVariatorStatistics
	{
		#region 常量定义
		const int ACQUIRE_LIMIT = 1000; //间隔最大时长（单位：毫秒）
		#endregion

		#region 成员变量
		private long _count;
		private long _value;
		private int _longest;
		private int _interval;
		private long _threshold;
		private long _timestamp;
		private readonly string _name = name;
		private readonly ISequenceBase _sequence = sequence;
		private readonly VariatorOptions _options = options;
		private readonly SemaphoreSlim _mutex = new(1, 1);
		#endregion

		#region 公共属性
		public long Count => _count;
		public long Value => _value;
		public int Longest => _longest;
		public int Interval => _interval;
		public long Threshold => _threshold;
		public DateTime Timestamp => _timestamp > 0 ? DateTime.Now.AddMilliseconds(_timestamp - Environment.TickCount64) : DateTime.MinValue;
		#endregion

		#region 公共方法
		public void Reset(int value)
		{
			try
			{
				_mutex.Wait();
				_sequence.Reset(_name, value);

				_count = 0;
				_value = 0;
				_longest = 0;
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

				_count = 0;
				_value = 0;
				_longest = 0;
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
			if(interval == 0)
				return _value;

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
			if(interval == 0)
				return ValueTask.FromResult(_value);

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
					Interlocked.Increment(ref _count);
					var length = this.GetAcquireInterval(interval);

					//确保请求递增的步长在限定范围内
					length = ClampInterval(length, _options.Initiate, _options.GrowthUpper);

					_threshold = _sequence.Increase(_name, length, seed);
					_value = _threshold == seed ? seed : _threshold - length;
					_timestamp = Environment.TickCount64;
					_interval = length;
					_longest = _interval > 0 ?
						Math.Max(_interval, _longest):
						Math.Min(_interval, _longest);
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
					Interlocked.Increment(ref _count);
					var length = this.GetAcquireInterval(interval);

					//确保请求递增的步长在限定范围内
					length = ClampInterval(length, _options.Initiate, _options.GrowthUpper);

					_threshold = await _sequence.IncreaseAsync(_name, length, seed, cancellation);
					_value = _threshold == seed ? seed : _threshold - length;
					_timestamp = Environment.TickCount64;
					_interval = length;
					_longest = _interval > 0 ?
						Math.Max(_interval, _longest):
						Math.Min(_interval, _longest);
				}
			}
			finally
			{
				_mutex.Release();
			}

			return await this.IncreaseAsync(interval, seed, cancellation);
		}

		private int GetAcquireInterval(int interval)
		{
			var duration = _timestamp > 0 ? Environment.TickCount64 - _timestamp : ACQUIRE_LIMIT;

			//请求的间隔超过最大时长或者 Tick 计时器被重置则返回指定的步长
			if(duration >= ACQUIRE_LIMIT || duration < 0)
				return interval;

			//请求的间隔等于零（即请求频率低于1毫秒）
			if(duration == 0)
				return _interval > 0 ?
					Math.Max(_options.GrowthLower, _interval * 2) :
					Math.Min(-_options.GrowthLower, _interval * 2);

			/*
			 * 剩下的分支：请求的间隔介于1毫秒至最大时长之间
			 */

			var growthRate = Math.Max(2.0f, (float)ACQUIRE_LIMIT / duration);
			var declineRate = 1.0f - (duration / (float)ACQUIRE_LIMIT);

			return interval > 0 ?
				(int)Math.Max(interval * growthRate, _interval * declineRate):
				(int)Math.Min(interval * growthRate, _interval * declineRate);
		}

		private static int ClampInterval(int interval, int minimum, int maximum) => interval > 0 ?
			Math.Clamp(interval, minimum, maximum) :
			Math.Clamp(interval, -maximum, -minimum);
		#endregion

		#region 重写方法
		public override string ToString() => $"{_value}/{_threshold}({_count}|{_interval})@{this.Timestamp}";
		#endregion
	}
	#endregion
}
