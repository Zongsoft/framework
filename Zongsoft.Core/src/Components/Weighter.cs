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
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Components
{
	/// <summary>
	/// 提供按权重调度功能的类。
	/// </summary>
	/// <typeparam name="T">加权元素的类型。</typeparam>
	public class Weighter<T> : IEnumerable<Weighter<T>.Entry> where T : class
	{
		#region 私有变量
		private int _index;
		private int _total;
		private int[] _weights;
		private Entry[] _entries;
		private readonly object _syncRoot;
		#endregion

		#region 构造函数
		public Weighter(IEnumerable<Entry> entries)
		{
			if(entries == null)
				throw new ArgumentNullException(nameof(entries));

			_syncRoot = new object();
			_entries = entries.Where(entry => entry.Value != null).ToArray();
			_weights = new int[_entries.Length];

			var maximum = 0;

			for(int i = 0; i < _entries.Length; i++)
			{
				_weights[i] = _entries[i].Weight;
				_total += _entries[i].Weight;

				if(_weights[i] > maximum)
				{
					_index = i;
					maximum = _weights[i];
				}
			}
		}
		#endregion

		#region 公共属性
		public int Count => _entries.Length;
		#endregion

		#region 公共方法
		public void Add(T value, int weight = 100) => this.Add(new Entry(value, weight));
		public void Add(Entry entry)
		{
			if(entry.Value == null)
				return;

			lock(_syncRoot)
			{
				var entries = new Entry[_entries.Length + 1];
				Array.Copy(_entries, entries, _entries.Length);
				entries[^1] = entry;
				_entries = entries;

				var weights = new int[entries.Length];
				Array.Copy(_weights, weights, _weights.Length);
				weights[^1] = entry.Weight;
				_weights = weights;

				_total += entry.Weight;

				if(entry.Weight > _weights[_index])
					_index = weights.Length - 1;
			}
		}

		public void Clear()
		{
			lock(_syncRoot)
			{
				_index = 0;
				_total = 0;
				_weights = Array.Empty<int>();
				_entries = Array.Empty<Entry>();
			}
		}

		public int Remove(T value)
		{
			if(value == null || _entries == null || _weights == null)
				return 0;

			lock(_syncRoot)
			{
				var index = 0;
				var count = 0;
				var entries = _entries;
				var weights = _weights;

				while(index < entries.Length)
				{
					if(object.Equals(entries[index].Value, value))
					{
						if(index < entries.Length - 1)
						{
							Array.Copy(entries, index + 1, entries, index, entries.Length - 1);
							Array.Copy(weights, index + 1, weights, index, weights.Length - 1);
						}

						Array.Resize(ref _entries, entries.Length - 1);
						Array.Resize(ref _weights, weights.Length - 1);

						count++;
					}
					else
					{
						index++;
					}
				}

				return count;
			}
		}

		public T Get()
		{
			if(_entries == null || _entries.Length == 0)
				return default;

			lock(_syncRoot)
			{
				var maximum = 0;
				var entries = _entries;
				var weights = _weights;

				weights[_index] -= _total;

				for(int i = 0; i < _weights.Length; i++)
				{
					_weights[i] += entries[i].Weight;

					if(_weights[i] > maximum)
					{
						_index = i;
						maximum = _weights[i];
					}
				}

				return entries[_index].Value;
			}
		}
		#endregion

		#region 枚举遍历
		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		public IEnumerator<Entry> GetEnumerator()
		{
			var entries = _entries;

			for(int i = 0; i < entries.Length; i++)
			{
				yield return entries[i];
			}
		}
		#endregion

		#region 嵌套结构
		public readonly struct Entry
		{
			public Entry(T value, int weight = 100)
			{
				this.Value = value;
				this.Weight = Math.Max(weight, 0);
			}

			public readonly T Value;
			public readonly int Weight;

			public override string ToString() => $"{this.Value}={this.Weight}";
		}
		#endregion
	}
}
