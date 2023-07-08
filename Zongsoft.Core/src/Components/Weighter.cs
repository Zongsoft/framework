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
 * Copyright (C) 2010-2022 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Linq;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Components
{
	/// <summary>
	/// 提供按权重调度功能的类。
	/// </summary>
	/// <typeparam name="T">加权元素的类型。</typeparam>
	/// <remarks>本实现基于 Nginx 的加权轮调算法，详细说明请参考：<c>https://zongsoft.com/blog/zh-cn/zongsoft/smooth-weighted-round-robin-balancing</c>。</remarks>
	public class Weighter<T> : IEnumerable<T>, IDisposable
	{
		#region 常量定义
		private const int DEFAULT_WEIGHT = 100;
		#endregion

		#region 私有变量
		private int _total;
		private int[] _weights;
		private Entry[] _entries;
		private Func<T, int> _weightResolver;
		private ReaderWriterLockSlim _locker;
		#endregion

		#region 构造函数
		public Weighter(IEnumerable<T> entries, Func<T, int> weightThunk = null)
		{
			if(entries == null)
				throw new ArgumentNullException(nameof(entries));

			_locker = new ReaderWriterLockSlim();
			_weightResolver = weightThunk;
			_entries = entries.Where(entry => entry != null).Select(entry => new Entry(entry, this.GetWeight(entry))).ToArray();
			_weights = new int[_entries.Length];
			_total = _entries.Sum(entry => entry.Weight);
		}
		#endregion

		#region 公共属性
		public int Count => _entries?.Length ?? 0;
		#endregion

		#region 公共方法
		public void Add(T entry, int weight) => this.Add(entry, entry => weight > 0 ? weight : DEFAULT_WEIGHT);
		public void Add(T entry, Func<T, int> weighter = null)
		{
			if(entry == null)
				return;

			try
			{
				_locker.EnterWriteLock();

				var weight = weighter != null ? weighter(entry) : this.GetWeight(entry);

				var entries = new Entry[_entries.Length + 1];
				Array.Copy(_entries, entries, _entries.Length);
				entries[^1] = new Entry(entry, weight);
				_entries = entries;

				var weights = new int[entries.Length];
				Array.Copy(_weights, weights, _weights.Length);
				weights[^1] = weight;
				_weights = weights;

				_total += weight;
			}
			finally
			{
				_locker.ExitWriteLock();
			}
		}

		public void Clear()
		{
			try
			{
				_locker.EnterWriteLock();

				_total = 0;
				_weights = Array.Empty<int>();
				_entries = Array.Empty<Entry>();
			}
			finally
			{
				_locker.ExitWriteLock();
			}
		}

		public bool Remove(T entry)
		{
			if(entry == null || _entries == null || _weights == null)
				return false;

			try
			{
				_locker.EnterWriteLock();

				var entries = _entries;
				var weights = _weights;

				for(int i = 0; i < entries.Length; i++)
				{
					if(object.Equals(entries[i].Value, entry))
					{
						_total -= entries[i].Weight;

						if(i < entries.Length - 1)
						{
							Array.Copy(entries, i + 1, entries, i, entries.Length - i - 1);
							Array.Copy(weights, i + 1, weights, i, weights.Length - i - 1);
						}

						Array.Resize(ref _entries, entries.Length - 1);
						Array.Resize(ref _weights, weights.Length - 1);

						return true;
					}
				}

				return false;
			}
			finally
			{
				_locker.ExitWriteLock();
			}
		}

		public T Get()
		{
			if(_entries == null || _entries.Length == 0)
				return default;

			try
			{
				_locker.EnterReadLock();

				var index = 0;
				var maximum = 0;
				var entries = _entries.AsSpan();
				var weights = _weights.AsSpan();

				for(int i = 0; i < weights.Length; i++)
				{
					weights[i] += entries[i].Weight;

					if(weights[i] > maximum)
					{
						index = i;
						maximum = weights[i];
					}
				}

				weights[index] -= _total;
				return _entries[index].Value;
			}
			finally
			{
				_locker.ExitReadLock();
			}
		}
		#endregion

		#region 虚拟方法
		protected virtual int GetWeight(T entry) => _weightResolver?.Invoke(entry) ?? DEFAULT_WEIGHT;
		#endregion

		#region 枚举遍历
		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		public IEnumerator<T> GetEnumerator()
		{
			var entries = _entries ?? throw new ObjectDisposedException(this.GetType().Name);

			for(int i = 0; i < entries.Length; i++)
				yield return entries[i].Value;
		}
		#endregion

		#region 处置方法
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			var total = Interlocked.Exchange(ref _total, -1);
			if(total < 0)
				return;

			if(disposing)
				_locker.Dispose();

			_entries = null;
			_weights = null;
			_weightResolver = null;
		}
		#endregion

		#region 嵌套结构
		private readonly struct Entry
		{
			public Entry(T value, int weight)
			{
				this.Value = value;
				this.Weight = Math.Max(weight, 1);
			}

			public readonly T Value;
			public readonly int Weight;

			public override string ToString() => $"{this.Value}={this.Weight}";
		}
		#endregion
	}
}
