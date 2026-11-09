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

	#region 公共属性
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
	#endregion

	#region 公共方法
	public void Add(string key, string value)
	{
		if(!_database.HashSet(_name, key, value, When.NotExists))
			throw new ArgumentException(string.Format(Properties.Resources.DictionaryKeyAlreadyExists_Message, key, _name));
	}
	#endregion

	#region 显式实现
	void ICollection<KeyValuePair<string, string>>.Add(KeyValuePair<string, string> field) => this.Add(field.Key, field.Value);
	#endregion
	#region 公共方法
	public void Clear() => _database.KeyDelete(_name);
	public bool Remove(string key) => _database.HashDelete(_name, key);
	#endregion
	#region 显式实现
	bool ICollection<KeyValuePair<string, string>>.Remove(KeyValuePair<string, string> field)
	{
		var result = _database.ScriptEvaluate(REMOVE_SCRIPT, [(RedisKey)_name], [field.Key, field.Value]);
		return (long)result != 0;
	}
	#endregion

	#region 公共方法
	public bool Contains(string key) => _database.HashExists(_name, key);
	#endregion
	#region 显式实现
	bool IDictionary<string, string>.ContainsKey(string key) => this.Contains(key);
	bool ICollection<KeyValuePair<string, string>>.Contains(KeyValuePair<string, string> field)
	{
		var value = _database.HashGet(_name, field.Key);
		return value.HasValue && value == field.Value;
	}
	#endregion

	#region 公共方法
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
			throw new ArgumentException(Properties.Resources.DestinationArrayInsufficient_Message, nameof(array));

		for(int i = 0; i < entries.Length; i++)
			array[arrayIndex + i] = new KeyValuePair<string, string>(entries[i].Name, entries[i].Value);
	}
	#endregion

	#region 显式实现
	IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
	#endregion
	#region 公共方法
	public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
	{
		foreach(var entry in _database.HashScan(_name))
			yield return new KeyValuePair<string, string>(entry.Name, entry.Value);
	}
	#endregion
}
