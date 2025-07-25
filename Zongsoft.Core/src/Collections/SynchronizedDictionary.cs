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

public class SynchronizedDictionary<TKey, TValue> : IDictionary<TKey, TValue>
{
	#region 成员字段
	private ReaderWriterLockSlim _lock = new();
	private readonly IDictionary<TKey, TValue> _dictionary;
	#endregion

	#region 构造函数
	public SynchronizedDictionary() : this(new Dictionary<TKey, TValue>()) { }
	public SynchronizedDictionary(int capacity) : this(new Dictionary<TKey, TValue>(capacity)) { }
	public SynchronizedDictionary(IEqualityComparer<TKey> comparer) : this(new Dictionary<TKey, TValue>(comparer)) { }
	public SynchronizedDictionary(int capacity, IEqualityComparer<TKey> comparer) : this(new Dictionary<TKey, TValue>(capacity, comparer)) { }
	internal SynchronizedDictionary(IDictionary<TKey, TValue> dictionary) => _dictionary = dictionary ?? new Dictionary<TKey, TValue>();
	#endregion

	#region 公共属性
	public int Count => _dictionary.Count;
	bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => _dictionary.IsReadOnly;

	public ICollection<TKey> Keys
	{
		get
		{
			_lock.EnterReadLock();
			try { return [.. _dictionary.Keys]; }
			finally { _lock.ExitReadLock(); }
		}
	}

	public ICollection<TValue> Values
	{
		get
		{
			_lock.EnterReadLock();
			try { return [.. _dictionary.Values]; }
			finally { _lock.ExitReadLock(); }
		}
	}

	public TValue this[TKey key]
	{
		get
		{
			_lock.EnterReadLock();
			try { return _dictionary[key]; }
			finally { _lock.ExitReadLock(); }
		}
		set
		{
			_lock.EnterWriteLock();
			try { _dictionary[key] = value; }
			finally { _lock.ExitWriteLock(); }
		}
	}
	#endregion

	#region 公共方法
	public void Add(TKey key, TValue value)
	{
		_lock.EnterWriteLock();
		try { _dictionary.Add(key, value); }
		finally { _lock.ExitWriteLock(); }
	}

	public void Clear()
	{
		_lock.EnterWriteLock();
		try { _dictionary.Clear(); }
		finally { _lock.ExitWriteLock(); }
	}

	public bool Remove(TKey key)
	{
		_lock.EnterWriteLock();
		try { return _dictionary.Remove(key); }
		finally { _lock.ExitWriteLock(); }
	}

	public bool ContainsKey(TKey key)
	{
		_lock.EnterReadLock();
		try { return _dictionary.ContainsKey(key); }
		finally { _lock.ExitReadLock(); }
	}

	public bool TryGetValue(TKey key, out TValue value)
	{
		_lock.EnterReadLock();
		try { return _dictionary.TryGetValue(key, out value); }
		finally { _lock.ExitReadLock(); }
	}
	#endregion

	#region 显式实现
	void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) => this.Add(item.Key, item.Value);
	bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item) => this.Remove(item.Key);
	bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item) => this.ContainsKey(item.Key);
	void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
	{
		_lock.EnterReadLock();
		try { _dictionary.CopyTo(array, arrayIndex); }
		finally { _lock.ExitReadLock(); }
	}
	#endregion

	#region 枚举遍历
	IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
	public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
	{
		KeyValuePair<TKey, TValue>[] array;

		_lock.EnterReadLock();
		try { array = _dictionary.ToArray(); }
		finally { _lock.ExitReadLock(); }

		return Enumerable.GetEnumerator(array);
	}
	#endregion

	#region 析构函数
	~SynchronizedDictionary()
	{
		_lock?.Dispose();
		_lock = null;
	}
	#endregion
}
