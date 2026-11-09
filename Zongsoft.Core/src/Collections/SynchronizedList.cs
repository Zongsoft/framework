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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Collections;

public class SynchronizedList<T> : IList<T>, ICollection<T>, ICollection
{
	#region 成员字段
	private readonly IList<T> _list;
	private readonly ReaderWriterLockSlim _lock;
	#endregion

	#region 构造函数
	public SynchronizedList()
	{
		_lock = new(LockRecursionPolicy.SupportsRecursion);
		_list = new List<T>();
	}
	public SynchronizedList(int capacity)
	{
		_lock = new(LockRecursionPolicy.SupportsRecursion);
		_list = new List<T>(capacity);
	}
	public SynchronizedList(params IEnumerable<T> items)
	{
		_lock = new(LockRecursionPolicy.SupportsRecursion);
		_list = new List<T>(items);
	}
	internal SynchronizedList(IList<T> list)
	{
		ArgumentNullException.ThrowIfNull(list);
		_lock = new(LockRecursionPolicy.SupportsRecursion);
		_list = list;
	}
	#endregion

	#region 公共属性
	public int Count
	{
		get
		{
			_lock.EnterReadLock();

			try { return _list.Count; }
			finally { _lock.ExitReadLock(); }
		}
	}

	bool ICollection<T>.IsReadOnly => false;
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
		_lock.EnterReadLock();

		try
		{
			foreach(var item in _list)
				yield return item;
		}
		finally
		{
			_lock.ExitReadLock();
		}
	}
	#endregion
}