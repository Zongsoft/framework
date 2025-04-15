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

namespace Zongsoft.Data;

public sealed class DataDeleteReturning : IDictionary<string, object>
{
	#region 成员字段
	private readonly Dictionary<string, object> _dictionary;
	#endregion

	#region 构造函数
	public DataDeleteReturning(params string[] names) => _dictionary = names == null ?
		new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) :
		new Dictionary<string, object>(names.Select(name => new KeyValuePair<string, object>(name, null)), StringComparer.OrdinalIgnoreCase);
	public DataDeleteReturning(IEnumerable<string> names) => _dictionary = names == null ?
		new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) :
		new Dictionary<string, object>(names.Select(name => new KeyValuePair<string, object>(name, null)), StringComparer.OrdinalIgnoreCase);
	#endregion

	#region 公共属性
	public int Count => _dictionary.Count;
	public object this[string name]
	{
		get => _dictionary[name];
		set => _dictionary[name] = value;
	}
	public ICollection<string> Keys => _dictionary.Keys;
	public ICollection<object> Values => _dictionary.Values;
	public bool IsEmpty => _dictionary == null || _dictionary.Count == 0;
	public bool HasValue => _dictionary != null && _dictionary.Count > 0;
	#endregion

	#region 公共方法
	public void Add(string name) => _dictionary.Add(name, null);
	public void Add(string name, object value) => _dictionary.Add(name, value);
	public bool Contains(string name) => _dictionary.ContainsKey(name);
	public bool ContainsKey(string name) => _dictionary.ContainsKey(name);
	public bool Remove(string name) => _dictionary.Remove(name);
	public bool TryGetValue(string name, out object value) => _dictionary.TryGetValue(name, out value);
	public void Clear() => _dictionary.Clear();
	#endregion

	#region 重写方法
	public override string ToString() => _dictionary == null || _dictionary.Count == 0 ?
		string.Empty : string.Join(',', _dictionary.Select(entry => $"{entry.Key}={entry.Value}"));
	#endregion

	#region 显式实现
	bool ICollection<KeyValuePair<string, object>>.IsReadOnly => false;
	void ICollection<KeyValuePair<string, object>>.Add(KeyValuePair<string, object> item) => ((ICollection<KeyValuePair<string, object>>)_dictionary).Add(item);
	bool ICollection<KeyValuePair<string, object>>.Contains(KeyValuePair<string, object> item) => ((ICollection<KeyValuePair<string, object>>)_dictionary).Contains(item);
	void ICollection<KeyValuePair<string, object>>.CopyTo(KeyValuePair<string, object>[] array, int arrayIndex) => ((ICollection<KeyValuePair<string, object>>)_dictionary).CopyTo(array, arrayIndex);
	bool ICollection<KeyValuePair<string, object>>.Remove(KeyValuePair<string, object> item) => ((ICollection<KeyValuePair<string, object>>)_dictionary).Remove(item);
	IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
	public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => _dictionary.GetEnumerator();
	#endregion
}
