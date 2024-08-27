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
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Zongsoft.Configuration.Profiles
{
	internal abstract class ProfileItemViewBase<T> : IProfileItemCollection<T> where T : ProfileItem
	{
		#region 成员字段
		private readonly ProfileItemCollection _items;
		private readonly Dictionary<string, T> _innerDictionary;
		#endregion

		#region 构造函数
		protected ProfileItemViewBase(ProfileItemCollection items)
		{
			_items = items ?? throw new ArgumentNullException(nameof(items));
			_innerDictionary = new Dictionary<string, T>(items.Count, StringComparer.OrdinalIgnoreCase);

			foreach(var item in items)
			{
				if(this.OnItemMatch(item))
					_innerDictionary.Add(this.GetKeyForItem((T)item), (T)item);
			}

			items.CollectionChanged += Items_CollectionChanged;
		}
		#endregion

		#region 公共属性
		public int Count => _innerDictionary.Count;
		public IEnumerable<string> Keys => _innerDictionary.Keys;

		public T this[string name]
		{
			get
			{
				if(_innerDictionary.TryGetValue(name, out var result))
					return result;

				throw new KeyNotFoundException();
			}
		}
		#endregion

		#region 公共方法
		public void Add(T item)
		{
			if(item == null)
				throw new ArgumentNullException(nameof(item));

			_items.Add(item);
		}

		public bool TryGetValue(string name, out T value) => _innerDictionary.TryGetValue(name, out value);

		public void Clear()
		{
			_innerDictionary.Clear();
			_items.Clear();
		}

		public bool Remove(string name)
		{
			if(_innerDictionary.TryGetValue(name, out var item))
				return _items.Remove(item);

			return false;
		}

		public bool Remove(T item)
		{
			return _items.Remove(item);
		}

		public bool Contains(string name)
		{
			return _innerDictionary.ContainsKey(name);
		}

		public bool Contains(T item)
		{
			if(item == null)
				return false;

			if(_innerDictionary.TryGetValue(this.GetKeyForItem(item), out var entry))
				return object.ReferenceEquals(item, entry);

			return false;
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			if(array == null)
				return;

			if(arrayIndex >= array.Length)
				throw new ArgumentOutOfRangeException("arrayIndex");

			int index = 0;

			foreach(var item in _innerDictionary.Values)
			{
				if(arrayIndex + index >= array.Length)
					return;

				array[arrayIndex + index++] = item;
			}
		}
		#endregion

		#region 抽象方法
		protected abstract string GetKeyForItem(T item);

		/// <summary>当需要过滤集合元素时调用此方法被调用。</summary>
		/// <param name="item">指定要匹配的集合元素。</param>
		/// <returns>如果匹配成功则返回真(true)，否则返回假(false)。</returns>
		/// <remarks>
		///		<para>对实现者的要求：当该方法返回真(true)，则必须确保参数<paramref name="item"/>是可被直接转换为<typeparamref name="T"/>类型的。</para>
		/// </remarks>
		protected abstract bool OnItemMatch(ProfileItem item);
		#endregion

		#region 集合事件
		private void Items_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			switch(e.Action)
			{
				case NotifyCollectionChangedAction.Add:
					foreach(var item in e.NewItems)
					{
						if(this.OnItemMatch((ProfileItem)item))
						{
							_innerDictionary.Add(this.GetKeyForItem((T)item), (T)item);
						}
					}
					break;
				case NotifyCollectionChangedAction.Remove:
					foreach(var item in e.OldItems)
					{
						if(this.OnItemMatch((ProfileItem)item))
						{
							_innerDictionary.Remove(this.GetKeyForItem((T)item));
						}
					}
					break;
				case NotifyCollectionChangedAction.Replace:
					foreach(var item in e.OldItems)
					{
						if(this.OnItemMatch((ProfileItem)item))
						{
							_innerDictionary.Remove(this.GetKeyForItem((T)item));
						}
					}

					foreach(var item in e.NewItems)
					{
						if(this.OnItemMatch((ProfileItem)item))
						{
							_innerDictionary.Add(this.GetKeyForItem((T)item), (T)item);
						}
					}
					break;
				case NotifyCollectionChangedAction.Reset:
					_innerDictionary.Clear();
					break;
			}
		}
		#endregion

		#region 遍历枚举
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => this.GetEnumerator();
		public IEnumerator<T> GetEnumerator()
		{
			foreach(var item in _innerDictionary.Values)
				yield return item;
		}
		#endregion
	}
}
