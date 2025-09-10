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
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Configuration;

/// <summary>
/// 表示键值对的选项设置类，可以通过一个以分号为分隔符的字符串来表示该设置类。
/// </summary>
/// <example>
/// key1=value1;key2=;key3='contains whitespaces or other escape characters(e.g. :=;"\t\r\n\').'
/// </example>
public partial class Settings : ISettings, IEquatable<Settings>
{
	#region 成员字段
	private string _value;
	private bool _changed;
	private readonly Dictionary<string, string> _entries;
	#endregion

	#region 构造函数
	/// <summary>构建一个设置。</summary>
	/// <param name="value">指定的设置内容。</param>
	public Settings(string value = null) : this(null, value) { }

	/// <summary>构建一个设置。</summary>
	/// <param name="name">指定的设置名称。</param>
	/// <param name="value">指定的设置内容。</param>
	public Settings(string name, string value = null)
	{
		_entries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		this.Name = name ?? string.Empty;
		this.Value = value ?? string.Empty;
	}

	internal Settings(string name, string value, params IEnumerable<KeyValuePair<string, string>> entries)
	{
		this.Name = name ?? string.Empty;
		_entries = new(entries, StringComparer.OrdinalIgnoreCase);
		_value = value ?? string.Empty;
	}
	#endregion

	#region 公共属性
	public string Name { get; }
	public string Value
	{
		get
		{
			if(_changed)
				_value = string.Join(';', _entries.Select(entry => $"{entry.Key}={Unescape(entry.Value)}"));

			return _value;
		}
		set
		{
			value ??= string.Empty;

			if(!string.Equals(_value, value))
			{
				_value = value;
				this.OnValueChanged(_value);
			}
		}
	}

	int IReadOnlyCollection<KeyValuePair<string, string>>.Count => _entries.Count;
	public bool IsEmpty => _entries.Count == 0;
	public bool HasValue => _entries.Count > 0;

	public virtual string this[string key]
	{
		get => key != null && _entries.TryGetValue(key, out var value) ? value : null;
		set
		{
			if(string.IsNullOrEmpty(key))
				return;

			if(string.IsNullOrEmpty(value))
				_entries.Remove(key);
			else
				_entries[key] = value.Trim();

			_changed = true;
		}
	}
	#endregion

	#region 保护属性
	protected IDictionary<string, string> Entries => _entries;
	#endregion

	#region 虚拟方法
	protected virtual void OnValueChanged(string value)
	{
		_entries.Clear();

		foreach(var entry in Parse(value, message => throw new ArgumentException(message)))
			_entries[entry.Key] = entry.Value;
	}
	#endregion

	#region 重写方法
	public bool Equals(Settings other)
	{
		var result = other is not null &&
			_entries.Count == other._entries.Count &&
			string.Equals(this.Name, other.Name, StringComparison.OrdinalIgnoreCase);

		if(!result)
			return false;

		foreach(var entry in _entries)
		{
			if(!other._entries.TryGetValue(entry.Key, out var value))
				return false;

			if(!string.Equals(entry.Value, value))
				return false;
		}

		return true;
	}

	public bool Equals(ISettings settings) => settings is Settings other && this.Equals(other);
	public override bool Equals(object obj) => obj is Settings other && this.Equals(other);
	public override int GetHashCode() => HashCode.Combine(this.Name.ToLowerInvariant(), string.Join(';', _entries.Select(entry => $"{entry.Key}={entry.Value}")));
	public override string ToString() => string.IsNullOrEmpty(this.Name) ? this.Value : $"[{this.Name}]{this.Value}";
	#endregion

	#region 私有方法
	private static string Unescape(string value)
	{
		if(string.IsNullOrEmpty(value))
			return null;

		var unescaped = value.Contains('=');

		if(value.Contains('\\'))
		{
			unescaped = true;
			value = value.Replace(@"\", @"\\");
		}

		if(value.Contains('\n'))
		{
			unescaped = true;
			value = value.Replace("\n", @"\n");
		}

		if(value.Contains('\r'))
		{
			unescaped = true;
			value = value.Replace("\r", @"\n");
		}

		if(value.Contains('\''))
		{
			unescaped = true;
			value = value.Replace("\'", @"\'");
		}

		return unescaped ? $"'{value}'" : value;
	}
	#endregion

	#region 枚举遍历
	IEnumerator IEnumerable.GetEnumerator() => _entries.GetEnumerator();
	public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _entries.GetEnumerator();
	#endregion
}
