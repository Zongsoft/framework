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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
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

namespace Zongsoft.Collections;

public class SynchronizedList<T> : IList<T>, ICollection<T>, ICollection
{
	#region 成员字段
	private ReaderWriterLockSlim _lock = new();
	private readonly IList<T> _list;
	#endregion

	#region 构造函数
	public SynchronizedList() : this(null) { }
	public SynchronizedList(int capacity) : this(new List<T>(capacity)) { }
	internal SynchronizedList(IList<T> list) => _list = list ?? [];
	#endregion

	#region 公共属性
	public int Count => _list.Count;
	bool ICollection<T>.IsReadOnly => _list.IsReadOnly;
	bool ICollection.IsSynchronized => true;
	object ICollection.SyncRoot => _lock;

	public T this[int index]
	{
		get
		{
			_lock.EnterReadLock();
			try { return _list[index]; }
			finally { _lock.ExitReadLock(); }
		}
		set
		{
			_lock.EnterWriteLock();
			try { _list[index] = value; }
			finally { _lock.ExitWriteLock(); }
		}
	}
	#endregion

	#region 公共方法
	public void Add(T item)
	{
		_lock.EnterWriteLock();
		try { _list.Add(item); }
		finally { _lock.ExitWriteLock(); }
	}

	public void Insert(int index, T item)
	{
		_lock.EnterWriteLock();
		try { _list.Insert(index, item); }
		finally { _lock.ExitWriteLock(); }
	}

	public void Clear()
	{
		_lock.EnterWriteLock();
		try { _list.Clear(); }
		finally { _lock.ExitWriteLock(); }
	}

	public bool Remove(T item)
	{
		_lock.EnterWriteLock();
		try { return _list.Remove(item); }
		finally { _lock.ExitWriteLock(); }
	}

	public void RemoveAt(int index)
	{
		_lock.EnterWriteLock();
		try { _list.RemoveAt(index); }
		finally { _lock.ExitWriteLock(); }
	}

	public bool Contains(T item)
	{
		_lock.EnterReadLock();
		try { return _list.Contains(item); }
		finally { _lock.ExitReadLock(); }
	}

	public int IndexOf(T item)
	{
		_lock.EnterReadLock();
		try { return _list.IndexOf(item); }
		finally { _lock.ExitReadLock(); }
	}

	public void CopyTo(T[] array, int arrayIndex)
	{
		_lock.EnterReadLock();
		try { _list.CopyTo(array, arrayIndex); }
		finally { _lock.ExitReadLock(); }
	}

	void ICollection.CopyTo(Array array, int arrayIndex)
	{
		_lock.EnterReadLock();
		try
		{
			if(_list is ICollection collection)
				collection.CopyTo(array, arrayIndex);
			else if(array is T[] target)
				_list.CopyTo(target, arrayIndex);
			else
			{
				var index = arrayIndex;

				foreach(var item in _list)
					array.SetValue(item, index++);
			}
		}
		finally { _lock.ExitReadLock(); }
	}
	#endregion

	#region 枚举遍历
	IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
	public IEnumerator<T> GetEnumerator()
	{
		T[] array;

		_lock.EnterReadLock();
		try { array = _list.ToArray(); }
		finally { _lock.ExitReadLock(); }

		return Enumerable.GetEnumerator(array);
	}
	#endregion

	#region 析构函数
	~SynchronizedList()
	{
		_lock?.Dispose();
		_lock = null;
	}
	#endregion
}