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
 * Copyright (C) 2010-2024 Zongsoft Studio <http://www.zongsoft.com>
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

public class RedisDictionary : IDictionary<string, string>
{
	private const string REMOVE_SCRIPT = "if redis.call('hget',KEYS[1],ARGV[1])==ARGV[2] then return redis.call('hdel',KEYS[1],ARGV[1]) else return 0 end";

	private readonly IDatabase _database;
	private readonly string _name;

	internal RedisDictionary(IDatabase database, string name)
	{
		_database = database ?? throw new ArgumentNullException(nameof(database));
		_name = name ?? throw new ArgumentNullException(nameof(name));
	}

	public int Count => (int)_database.HashLength(_name);
	public bool IsReadOnly => false;

	public string this[string key]
	{
		get => _database.HashGet(_name, key);
		set => _database.HashSet(_name, key, value, When.Always);
	}

	public ICollection<string> Keys
	{
		get
		{
			var keys = _database.HashKeys(_name);
			if(keys == null || keys.Length == 0)
				return Array.Empty<string>();

			var result = new string[keys.Length];

			for(int i = 0; i < keys.Length; i++)
				result[i] = keys[i];

			return result;
		}
	}

	public ICollection<string> Values
	{
		get
		{
			var values = _database.HashValues(_name);
			if(values == null || values.Length == 0)
				return Array.Empty<string>();

			var result = new string[values.Length];

			for(int i = 0; i < values.Length; i++)
				result[i] = values[i];

			return result;
		}
	}

	public void Add(string key, string value)
	{
		if(!_database.HashSet(_name, key, value, When.NotExists))
			throw new ArgumentException($"The specified '{key}' key already exists in the '{_name}' dictionary.");
	}

	void ICollection<KeyValuePair<string, string>>.Add(KeyValuePair<string, string> field) => this.Add(field.Key, field.Value);
	public void Clear() => _database.KeyDelete(_name);
	public bool Remove(string key) => _database.HashDelete(_name, key);
	bool ICollection<KeyValuePair<string, string>>.Remove(KeyValuePair<string, string> field)
	{
		var result = _database.ScriptEvaluate(REMOVE_SCRIPT, [(RedisKey)_name], [field.Key, field.Value]);
		return (long)result != 0;
	}

	public bool Contains(string key) => _database.HashExists(_name, key);
	bool IDictionary<string, string>.ContainsKey(string key) => this.Contains(key);
	bool ICollection<KeyValuePair<string, string>>.Contains(KeyValuePair<string, string> field)
	{
		var value = _database.HashGet(_name, field.Key);
		return value.HasValue && value == field.Value;
	}

	public bool TryGetValue(string key, out string value)
	{
		var result = _database.HashGet(_name, key);
		value = result;
		return result.HasValue;
	}

	public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
	{
		ArgumentNullException.ThrowIfNull(array);

		if(arrayIndex < 0 || arrayIndex > array.Length)
			throw new ArgumentOutOfRangeException(nameof(arrayIndex));

		var entries = _database.HashGetAll(_name);

		if(entries.Length > array.Length - arrayIndex)
			throw new ArgumentException("The destination array does not have enough available space.", nameof(array));

		for(int i = 0; i < entries.Length; i++)
			array[arrayIndex + i] = new KeyValuePair<string, string>(entries[i].Name, entries[i].Value);
	}

	IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
	public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
	{
		foreach(var entry in _database.HashScan(_name))
			yield return new KeyValuePair<string, string>(entry.Name, entry.Value);
	}
}
