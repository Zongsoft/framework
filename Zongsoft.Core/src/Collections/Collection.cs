﻿/*
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
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace Zongsoft.Collections
{
	[Obsolete]
	public class Collection<T> : IList<T>, IList, IReadOnlyCollection<T>, IReadOnlyList<T>, INotifyCollectionChanged
	{
		#region 事件定义
		public event NotifyCollectionChangedEventHandler CollectionChanged;
		#endregion

		#region 成员字段
		private object _syncRoot;
		private List<T> _items;
		#endregion

		#region 构造函数
		public Collection()
		{
			_items = new List<T>();
		}

		public Collection(IEnumerable<T> items)
		{
			if(items == null)
				_items = new List<T>();
			else
				_items = new List<T>(items.Where(p => p != null));
		}
		#endregion

		#region 公共属性
		public int Count
		{
			get
			{
				return _items.Count;
			}
		}

		public T this[int index]
		{
			get
			{
				return _items[index];
			}
			set
			{
				if(this.Items.IsReadOnly)
					throw new NotSupportedException();

				if(index < 0 || index >= _items.Count)
					throw new ArgumentOutOfRangeException("index");

				this.SetItem(index, value);
			}
		}
		#endregion

		#region 保护属性
		protected IList<T> Items
		{
			get
			{
				return _items;
			}
		}
		#endregion

		#region 公共方法
		public void Add(T item)
		{
			if(this.Items.IsReadOnly)
				throw new NotSupportedException();

			this.InsertItems(_items.Count, new T[] { item });
		}

		public void AddRange(params T[] items)
		{
			if(items == null)
				throw new ArgumentNullException("items");

			this.AddRange((IEnumerable<T>)items);
		}

		public virtual void AddRange(IEnumerable<T> items)
		{
			if(items == null)
				throw new ArgumentNullException("items");

			if(this.Items.IsReadOnly)
				throw new NotSupportedException();

			this.InsertItems(_items.Count, items);
		}

		public void Clear()
		{
			if(this.Items.IsReadOnly)
				throw new NotSupportedException();

			this.ClearItems();
		}

		public void CopyTo(T[] array, int index)
		{
			_items.CopyTo(array, index);
		}

		public bool Contains(T item)
		{
			return _items.Contains(item);
		}

		public bool IsReadOnly
		{
			get
			{
				return this.Items.IsReadOnly;
			}
		}

		public int IndexOf(T item)
		{
			return _items.IndexOf(item);
		}

		public void Insert(int index, T item)
		{
			if(this.Items.IsReadOnly)
				throw new NotSupportedException();

			if(index < 0 || index > _items.Count)
				throw new ArgumentOutOfRangeException();

			this.InsertItems(index, new T[] { item });
		}

		public bool Remove(T item)
		{
			if(this.Items.IsReadOnly)
				throw new NotSupportedException();

			int index = _items.IndexOf(item);
			if(index < 0)
				return false;

			this.RemoveItem(index);
			return true;
		}

		public void RemoveAt(int index)
		{
			if(this.Items.IsReadOnly)
				throw new NotSupportedException();

			if(index < 0 || index >= _items.Count)
				throw new ArgumentOutOfRangeException();

			this.RemoveItem(index);
		}
		#endregion

		#region 虚拟方法
		protected virtual void ClearItems()
		{
			_items.Clear();

			//激发“CollectionChanged”事件
			this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}

		protected virtual void InsertItems(int index, IEnumerable<T> items)
		{
			if(items == null)
				return;

			_items.AddRange(items);

			var list = items as IList;

			if(list == null)
				list = new List<T>(items);

			//激发“CollectionChanged”事件
			if(list.Count > 0)
				this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, list, index));
		}

		protected virtual void RemoveItem(int index)
		{
			var item = _items[index];

			_items.RemoveAt(index);

			//激发“CollectionChanged”事件
			this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
		}

		protected virtual void SetItem(int index, T item)
		{
			var oldItem = _items[index];

			_items[index] = item;

			//激发“CollectionChanged”事件
			this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, item, oldItem, index));
		}

		protected virtual bool TryConvertItem(object value, out T item)
		{
			return Zongsoft.Common.Convert.TryConvertValue<T>(value, out item);
		}
		#endregion

		#region 激发事件
		protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs args)
		{
			var collectionChanged = this.CollectionChanged;

			if(collectionChanged != null)
				collectionChanged(this, args);
		}
		#endregion

		#region 显式实现
		bool ICollection.IsSynchronized
		{
			get
			{
				return false;
			}
		}

		object ICollection.SyncRoot
		{
			get
			{
				if(_syncRoot == null)
				{
					ICollection c = _items as ICollection;

					if(c != null)
						_syncRoot = c.SyncRoot;
					else
						System.Threading.Interlocked.CompareExchange<Object>(ref _syncRoot, new Object(), null);
				}

				return _syncRoot;
			}
		}

		void ICollection.CopyTo(Array array, int index)
		{
			if(array == null)
				throw new ArgumentNullException("array");

			if(array.Rank != 1)
				throw new ArgumentException();

			if(array.GetLowerBound(0) != 0)
				throw new ArgumentException();

			if(index < 0 || array.Length - index < this.Count)
				throw new ArgumentOutOfRangeException("index");

			T[] tArray = array as T[];
			if(tArray != null)
			{
				_items.CopyTo(tArray, index);
			}
			else
			{
				Type targetType = array.GetType().GetElementType();
				Type sourceType = typeof(T);

				if(!(targetType.IsAssignableFrom(sourceType) || sourceType.IsAssignableFrom(targetType)))
					throw new ArgumentException();

				object[] objects = array as object[];
				if(objects == null)
					throw new ArgumentException();

				int count = _items.Count;

				for(int i = 0; i < count; i++)
				{
					objects[index++] = _items[i];
				}
			}
		}

		object IList.this[int index]
		{
			get
			{
				return _items[index];
			}
			set
			{
				if(value == null)
					throw new ArgumentNullException();

				T result;

				if(this.TryConvertItem(value, out result))
					this.SetItem(index, result);
				else
					throw new InvalidCastException();
			}
		}

		bool IList.IsFixedSize
		{
			get
			{
				IList list = _items as IList;

				if(list != null)
					return list.IsFixedSize;

				return this.Items.IsReadOnly;
			}
		}

		int IList.Add(object value)
		{
			if(this.Items.IsReadOnly)
				throw new NotSupportedException();

			if(value == null)
				throw new ArgumentNullException();

			T result;

			if(this.TryConvertItem(value, out result))
				this.Add(result);
			else
				return -1;

			return this.Count - 1;
		}

		bool IList.Contains(object value)
		{
			T result;

			if(this.TryConvertItem(value, out result))
				return Contains(result);

			return false;
		}

		int IList.IndexOf(object value)
		{
			T result;

			if(this.TryConvertItem(value, out result))
				return IndexOf(result);

			return -1;
		}

		void IList.Insert(int index, object value)
		{
			if(this.Items.IsReadOnly)
				throw new NotSupportedException();

			if(value == null)
				throw new ArgumentNullException();

			T result;

			if(this.TryConvertItem(value, out result))
				this.Insert(index, result);

			throw new InvalidCastException();
		}

		void IList.Remove(object value)
		{
			if(this.Items.IsReadOnly)
				throw new NotSupportedException();

			T result;

			if(this.TryConvertItem(value, out result))
				this.Remove(result);
		}
		#endregion

		#region 枚举遍历
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable)_items).GetEnumerator();
		}

		public IEnumerator<T> GetEnumerator()
		{
			return _items.GetEnumerator();
		}
		#endregion
	}
}
