/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
 *
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Collections.Generic;

namespace Zongsoft.Common
{
	public abstract class SequenceBase : ISequence, IDisposable
	{
		#region 内部结构
		[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
		private struct Entry
		{
			public long Value;      //当前序号值
			public long Threshold;  //本地递增的上限值
			public long Timestamp;  //最后更新时间，频率为秒
			public int Count;       //成长因子，最小值为10
			public int Flags;       //同步锁标记
		}
		#endregion

		#region 常量定义
		private const int MINIMUM_FACTOR = 10;
		private const int MAXIMUM_FACTOR = 10000;

		private const int LOCKED_FLAG = 1;
		private const int UNLOCK_FLAG = 0;
		#endregion

		#region 成员字段
		private Entry[] _entries;
		private readonly Dictionary<string, int> _map;
		private readonly ReaderWriterLockSlim _locker;
		#endregion

		#region 构造函数
		protected SequenceBase() : this(64)
		{
		}

		protected SequenceBase(int capacity)
		{
			if(capacity < 8)
				throw new ArgumentOutOfRangeException(nameof(capacity));

			_map = new Dictionary<string, int>(capacity);
			_entries = new Entry[capacity];
			_locker = new ReaderWriterLockSlim();
		}
		#endregion

		#region 公共方法
		public bool TryGetValue(string key, out long value)
		{
			if(string.IsNullOrEmpty(key))
				throw new ArgumentNullException(nameof(key));

			if(_map.TryGetValue(key, out var index))
			{
				value = _entries[index].Value;
				return true;
			}

			value = 0;
			return false;
		}

		public long Decrement(string key, int interval = 1, int seed = 0)
		{
			return this.Increment(key, -interval, seed);
		}

		public Task<long> DecrementAsync(string key, int interval = 1, int seed = 0, CancellationToken cancellation = default)
		{
			return this.IncrementAsync(key, -interval, seed, cancellation);
		}

		public long Increment(string key, int interval = 1, int seed = 0)
		{
			if(string.IsNullOrEmpty(key))
				throw new ArgumentNullException(nameof(key));

			if(_map.TryGetValue(key, out var index))
				_locker.EnterReadLock();
			else
				_locker.EnterUpgradeableReadLock();

			try
			{
				if(_locker.IsUpgradeableReadLockHeld && !_map.TryGetValue(key, out index))
				{
					try
					{
						_locker.EnterWriteLock();

						index = _map.Count;
						_map.Add(key, index);

						if(index == _entries.Length)
							Expand();
					}
					finally
					{
						_locker.ExitWriteLock();
					}
				}

				unsafe
				{
					fixed(Entry* entry = &_entries[index])
					{
						var value = Interlocked.Increment(ref entry->Value);

						if(value >= entry->Threshold)
						{
							var hold = Interlocked.CompareExchange(ref entry->Flags, LOCKED_FLAG, UNLOCK_FLAG);

							if(hold == UNLOCK_FLAG)
							{
								try
								{
									entry->Threshold = this.Reserve(key, ref *entry, value, seed);
									entry->Timestamp = GetTimestamp();
								}
								finally
								{
									Interlocked.Exchange(ref entry->Flags, UNLOCK_FLAG);
								}
							}
						}

						return value;
					}
				}
			}
			finally
			{
				if(_locker.IsReadLockHeld)
					_locker.ExitReadLock();
				else
					_locker.ExitUpgradeableReadLock();
			}
		}

		public Task<long> IncrementAsync(string key, int interval = 1, int seed = 0, CancellationToken cancellation = default)
		{
			cancellation.ThrowIfCancellationRequested();
			return Task.FromResult(this.Increment(key, interval, seed));
		}

		public void Reset(string key, int value = 0)
		{
			if(string.IsNullOrEmpty(key))
				throw new ArgumentNullException(nameof(key));

			_locker.EnterWriteLock();

			try
			{
				if(_map.TryGetValue(key, out var index))
				{
					unsafe
					{
						fixed(Entry* entry = &_entries[index])
						{
							var hold = Interlocked.CompareExchange(ref entry->Flags, LOCKED_FLAG, UNLOCK_FLAG);

							if(hold == 0)
							{
								try
								{
									entry->Value = value;
									entry->Count = MINIMUM_FACTOR;
									entry->Threshold = value + MINIMUM_FACTOR;
									entry->Timestamp = GetTimestamp();

									this.OnReset(key, value);
								}
								finally
								{
									Interlocked.Exchange(ref entry->Flags, UNLOCK_FLAG);
								}
							}
						}
					}
				}
			}
			finally
			{
				_locker.ExitWriteLock();
			}
		}

		public Task ResetAsync(string key, int value = 0, CancellationToken cancellation = default)
		{
			cancellation.ThrowIfCancellationRequested();
			this.Reset(key, value);
			return Task.CompletedTask;
		}
		#endregion

		#region 抽象方法
		/// <summary>
		/// 实现重置的方法。
		/// </summary>
		/// <param name="key">指定要重置的序号键名。</param>
		/// <param name="value">指定要重置的序号值。</param>
		protected abstract void OnReset(string key, int value);

		/// <summary>
		/// 表示增加的序号器的实现方法。
		/// </summary>
		/// <param name="key">指定的要批量递增(减)的序号键名。</param>
		/// <param name="count">指定的要递增(减)的序号值。</param>
		/// <param name="seed">当指定的序号键名不存在时设定的种子数。</param>
		/// <returns>返回递增(减)后的序号值。</returns>
		protected abstract long OnReserve(string key, int count, int seed);
		#endregion

		#region 私有方法
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private void Expand()
		{
			int capacity = _entries.Length == 0 ? 16 : _entries.Length * 2;

			if((uint)capacity > 0X7FEFFFFF)
				capacity = 0X7FEFFFFF;

			if(capacity < 16)
				capacity = 16;

			var entries = new Entry[capacity];

			if(_entries.Length > 0)
				Array.Copy(_entries, entries, _entries.Length);

			_entries = entries;
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private long Reserve(string key, ref Entry entry, long value, int seed)
		{
			static int Bound(int min, int max, double number)
			{
				return (int)Math.Min(max, Math.Max(min, number));
			}

			var duration = Math.Max(0, GetTimestamp() - entry.Timestamp);
			var count = entry.Count = Bound(
				MINIMUM_FACTOR,
				MAXIMUM_FACTOR,
				entry.Count * Math.Max(0.1, 2 - (duration / 300))
			);

			if(value < entry.Value && count < MAXIMUM_FACTOR)
				count += (int)(entry.Value - value);

			return this.OnReserve(key, count, seed);
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private long GetTimestamp()
		{
			return System.Environment.TickCount64 / 1000;
			//return System.Diagnostics.Stopwatch.GetTimestamp() / System.Diagnostics.Stopwatch.Frequency;
		}
		#endregion

		#region 处置方法
		protected virtual void Dispose(bool disposing)
		{
			if(disposing)
			{
				_map.Clear();
				Array.Resize(ref _entries, 0);
			}

			_locker.Dispose();
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		#endregion
	}
}
