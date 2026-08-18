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
 * This file is part of Zongsoft.Externals.Redis library.
 *
 * The Zongsoft.Externals.Redis is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Redis is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Redis library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections;
using System.Collections.Generic;

using StackExchange.Redis;

namespace Zongsoft.Externals.Redis;

public class RedisHashset : ISet<string>, ICollection<string>
{
	private readonly IDatabase _database;
	private readonly string _name;
	private readonly string _prefix;

	internal RedisHashset(IDatabase database, string name, string prefix = null)
	{
		_database = database ?? throw new ArgumentNullException(nameof(database));
		_name = name ?? throw new ArgumentNullException(nameof(name));
		_prefix = prefix ?? string.Empty;
	}

	public int Count => (int)_database.SetLength(_name);
	public bool IsReadOnly => false;

	public TimeSpan? GetExpiry() => _database.KeyTimeToLive(_name);

	public bool Add(string item) => item != null && _database.SetAdd(_name, item);
	public long AddRange(IEnumerable<string> items)
	{
		var values = GetValues(items);
		return values.Length == 0 ? 0 : _database.SetAdd(_name, values);
	}
	void ICollection<string>.Add(string item) => this.Add(item);

	public bool Move(string destination, string item) => !string.IsNullOrEmpty(destination) && item != null && _database.SetMove(_name, _prefix + destination, item);
	public bool Remove(string item) => _database.SetRemove(_name, item);
	public long RemoveRange(IEnumerable<string> items)
	{
		var values = GetValues(items);
		return values.Length == 0 ? 0 : _database.SetRemove(_name, values);
	}

	public void Clear() => _database.KeyDelete(_name);
	public bool Contains(string item) => item != null && _database.SetContains(_name, item);

	public void ExceptWith(IEnumerable<string> items)
	{
		this.RemoveRange(items);
	}

	public void SymmetricExceptWith(IEnumerable<string> items)
	{
		var current = this.GetSnapshot();
		var other = GetItems(items);
		var additions = new List<RedisValue>();
		var removals = new List<RedisValue>();

		foreach(var item in other)
		{
			if(current.Contains(item))
				removals.Add(item);
			else
				additions.Add(item);
		}

		if(additions.Count == 0 && removals.Count == 0)
			return;

		var transaction = _database.CreateTransaction();

		if(removals.Count > 0)
			_ = transaction.SetRemoveAsync(_name, removals.ToArray());

		if(additions.Count > 0)
			_ = transaction.SetAddAsync(_name, additions.ToArray());

		transaction.Execute();
	}

	public void IntersectWith(IEnumerable<string> items)
	{
		var current = this.GetSnapshot();
		current.ExceptWith(GetItems(items));

		if(current.Count > 0)
			_database.SetRemove(_name, GetValues(current));
	}

	public void UnionWith(IEnumerable<string> items) => this.AddRange(items);
	public bool IsProperSubsetOf(IEnumerable<string> other) => this.GetSnapshot().IsProperSubsetOf(GetItems(other));
	public bool IsProperSupersetOf(IEnumerable<string> other) => this.GetSnapshot().IsProperSupersetOf(GetItems(other));
	public bool IsSubsetOf(IEnumerable<string> other) => this.GetSnapshot().IsSubsetOf(GetItems(other));
	public bool IsSupersetOf(IEnumerable<string> other) => this.GetSnapshot().IsSupersetOf(GetItems(other));
	public bool Overlaps(IEnumerable<string> other) => this.GetSnapshot().Overlaps(GetItems(other));
	public bool SetEquals(IEnumerable<string> other) => this.GetSnapshot().SetEquals(GetItems(other));

	void ICollection<string>.CopyTo(string[] array, int arrayIndex)
	{
		ArgumentNullException.ThrowIfNull(array);

		if(arrayIndex < 0 || arrayIndex > array.Length)
			throw new ArgumentOutOfRangeException(nameof(arrayIndex));

		var items = _database.SetMembers(_name);

		if(items.Length > array.Length - arrayIndex)
			throw new ArgumentException("The destination array does not have enough available space.", nameof(array));

		for(int i = 0; i < items.Length; i++)
			array[arrayIndex + i] = (string)items[i];
	}

	public IEnumerator<string> GetEnumerator()
	{
		foreach(var item in _database.SetScan(_name))
			yield return item;
	}

	IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

	private HashSet<string> GetSnapshot()
	{
		var result = new HashSet<string>(StringComparer.Ordinal);

		foreach(var item in _database.SetMembers(_name))
			result.Add(item);

		return result;
	}

	private static HashSet<string> GetItems(IEnumerable<string> items)
	{
		ArgumentNullException.ThrowIfNull(items);

		var result = new HashSet<string>(StringComparer.Ordinal);

		foreach(var item in items)
		{
			if(item != null)
				result.Add(item);
		}

		return result;
	}

	private static RedisValue[] GetValues(IEnumerable<string> items)
	{
		ArgumentNullException.ThrowIfNull(items);

		var values = items is ICollection<string> collection ? new List<RedisValue>(collection.Count) : new List<RedisValue>();

		foreach(var item in items)
		{
			if(item != null)
				values.Add(item);
		}

		return values.ToArray();
	}
}
