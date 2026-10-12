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

/// <summary>表示以分号分隔的键值对设置。</summary>
public partial class Settings : ISettings, IReadOnlyDictionary<string, string>, IEquatable<Settings>
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
	public Settings(string name, string value = null) : this(name, value, true) { }

	/// <summary>构建设置，允许派生类延迟至其驱动初始化后解析。</summary>
	protected Settings(string name, string value, bool parse)
	{
		_entries = new(StringComparer.OrdinalIgnoreCase);
		this.Name = name ?? string.Empty;
		_value = value ?? string.Empty;

		if(parse)
			this.OnValueChanged(_value);
	}

	internal Settings(string name, string value, IEnumerable<KeyValuePair<string, string>> entries) : this(name, value, false)
	{
		foreach(var entry in entries)
			_entries[entry.Key] = entry.Value;
	}
	#endregion

	#region 公共属性
	public string Name { get; internal set; }
	public string Value
	{
		get
		{
			if(_changed)
			{
				_value = string.Join(';', this.GetEntries().Select(entry => $"{entry.Key}={Escape(entry.Value)}"));
				_changed = false;
			}

			return _value;
		}
		set
		{
			value ??= string.Empty;

			if(_changed || !string.Equals(_value, value, StringComparison.Ordinal))
			{
				this.OnValueChanged(value);
				_value = value;
				_changed = false;
			}
		}
	}

	public int Count => _entries.Count;
	public bool IsEmpty => _entries.Count == 0;
	public bool HasValue => _entries.Count > 0;

	public virtual string this[string key]
	{
		get => this.TryGetValue(key, out var value) ? value : null;
		set
		{
			if(string.IsNullOrEmpty(key))
				return;

			key = this.GetKey(key, validate: true);

			if(string.IsNullOrEmpty(value))
				_entries.Remove(key);
			else
				_entries[key] = value.Trim();

			this.OnChanged();
		}
	}
	#endregion

	#region 显式实现
	ICollection<string> IDictionary<string, string>.Keys => _entries.Keys;
	ICollection<string> IDictionary<string, string>.Values => _entries.Values;
	IEnumerable<string> IReadOnlyDictionary<string, string>.Keys => _entries.Keys;
	IEnumerable<string> IReadOnlyDictionary<string, string>.Values => _entries.Values;
	bool ICollection<KeyValuePair<string, string>>.IsReadOnly => false;

	void ICollection<KeyValuePair<string, string>>.Clear()
	{
		_entries.Clear();
		this.OnChanged();
	}

	void IDictionary<string, string>.Add(string key, string value)
	{
		ArgumentNullException.ThrowIfNull(key);

		_entries.Add(this.GetKey(key, validate: true), value);
		this.OnChanged();
	}

	void ICollection<KeyValuePair<string, string>>.Add(KeyValuePair<string, string> item) => ((IDictionary<string, string>)this).Add(item.Key, item.Value);
	bool IDictionary<string, string>.Remove(string key)
	{
		if(key == null || !_entries.Remove(this.GetKey(key)))
			return false;

		this.OnChanged();
		return true;
	}

	bool ICollection<KeyValuePair<string, string>>.Remove(KeyValuePair<string, string> item) =>
		((ICollection<KeyValuePair<string, string>>)this).Contains(item) && ((IDictionary<string, string>)this).Remove(item.Key);

	bool ICollection<KeyValuePair<string, string>>.Contains(KeyValuePair<string, string> item) =>
		this.TryGetValue(item.Key, out var value) && string.Equals(value, item.Value, StringComparison.Ordinal);

	void ICollection<KeyValuePair<string, string>>.CopyTo(KeyValuePair<string, string>[] array, int arrayIndex) =>
		((ICollection<KeyValuePair<string, string>>)_entries).CopyTo(array, arrayIndex);
	#endregion

	#region 保护属性
	protected IDictionary<string, string> Entries => _entries;
	#endregion

	#region 公共方法
	public bool Contains(string key) => this.ContainsKey(key);
	public bool ContainsKey(string key) => key != null && _entries.ContainsKey(this.GetKey(key));
	public bool TryGetValue(string key, out string value)
	{
		value = null;
		return key != null && _entries.TryGetValue(this.GetKey(key), out value);
	}
	#endregion

	#region 虚拟方法
	/// <summary>获取规范化的主参数键名，并按需校验是否允许写入。</summary>
	/// <param name="key">待处理的主参数键名。</param>
	/// <param name="validate">是否校验写入有效性；查询和删除时为 <see langword="false"/>，索引器赋值和添加时为 <see langword="true"/>。</param>
	/// <returns>返回规范化后的键名；基类实现原样返回 <paramref name="key"/>。</returns>
	/// <remarks>派生类可在此解析别名和复合路径；只在启用写入校验时拒绝未知键，保持查询及删除的未命中语义。</remarks>
	protected virtual string GetKey(string key, bool validate = false) => key;

	/// <summary>标记设置条目已发生变更，使缓存的设置文本失效。</summary>
	/// <remarks>下次读取 <see cref="Value"/> 时通过 <see cref="GetEntries"/> 重建文本；派生类直接修改条目或扩展属性后应调用此方法。</remarks>
	protected void OnChanged() => _changed = true;

	/// <summary>获取用于重建 <see cref="Value"/> 的全部设置条目。</summary>
	/// <returns>返回待序列化的键值对序列；基类实现返回主字典中的条目。</returns>
	/// <remarks>派生类可在返回序列中包含扩展属性，确保 <see cref="Value"/> 保存完整设置。此方法不改变字典接口的枚举内容。</remarks>
	protected virtual IEnumerable<KeyValuePair<string, string>> GetEntries() => _entries;

	/// <summary>在应用新设置文本前清空现有条目。</summary>
	/// <remarks>
	///		<para>由 <see cref="OnValueChanged"/> 在文本解析成功后、写入新条目前调用；基类实现清空主字典。</para>
	///		<para>派生类应调用基类实现并清理需要随文本整体替换的扩展状态。字典接口的清空操作不调用此方法。</para>
	/// </remarks>
	protected virtual void OnReset() => _entries.Clear();

	/// <summary>处理从设置文本解析得到的条目。</summary>
	/// <param name="key">已解析的参数键名。</param>
	/// <param name="value">已解析的参数值。</param>
	/// <remarks>由 <see cref="OnValueChanged"/> 按文本顺序调用，派生类可将条目分派到主字典或扩展属性。索引器、添加、删除和清空操作不调用此方法。</remarks>
	protected virtual void OnEntryChanged(string key, string value) => _entries[key] = value;

	/// <summary>解析设置文本并替换现有条目。</summary>
	/// <param name="value">新的设置文本；<see langword="null"/> 或空字符串表示不包含任何条目。</param>
	/// <exception cref="ArgumentException">设置文本格式无效。</exception>
	/// <remarks>
	///		<para>用于构造时初始化和 <see cref="Value"/> 赋值时重新加载。</para>
	///		<para>基类先完整解析文本，再调用 <see cref="OnReset"/>，最后按文本顺序逐项调用 <see cref="OnEntryChanged"/>；文本解析失败时不会清空现有条目。</para>
	/// </remarks>
	protected virtual void OnValueChanged(string value)
	{
		var entries = Parse(value, message => throw new ArgumentException(message));
		this.OnReset();

		foreach(var entry in entries)
			this.OnEntryChanged(entry.Key, entry.Value);
	}
	#endregion

	#region 重写方法
	public virtual bool Equals(Settings other) => other is not null &&
		(this is IConnectionSettings) == (other is IConnectionSettings) &&
		string.Equals(this.Name, other.Name, StringComparison.OrdinalIgnoreCase) &&
		_entries.Count == other._entries.Count &&
		_entries.All(entry => other._entries.TryGetValue(entry.Key, out var value) && string.Equals(entry.Value, value, StringComparison.Ordinal));

	public bool Equals(ISettings settings) => settings is Settings other && this.Equals(other);
	public override bool Equals(object obj) => obj is Settings other && this.Equals(other);
	public override string ToString() => string.IsNullOrEmpty(this.Name) ? this.Value : $"[{this.Name}]{this.Value}";
	public override int GetHashCode() => HashCode.Combine(StringComparer.OrdinalIgnoreCase.GetHashCode(this.Name), GetEntriesHashCode(_entries));

	protected static int GetEntriesHashCode(IEnumerable<KeyValuePair<string, string>> entries)
	{
		var hash = 0;

		foreach(var entry in entries)
			hash ^= HashCode.Combine(StringComparer.OrdinalIgnoreCase.GetHashCode(entry.Key), entry.Value == null ? 0 : StringComparer.Ordinal.GetHashCode(entry.Value));

		return hash;
	}
	#endregion

	#region 私有方法
	private static readonly System.Buffers.SearchValues<char> _escaped_text_ = System.Buffers.SearchValues.Create("=;'\"\\\t\r\n");
	private static string Escape(string value)
	{
		if(string.IsNullOrEmpty(value))
			return value;

		if(value.AsSpan().IndexOfAny(_escaped_text_) < 0 && !char.IsWhiteSpace(value[0]) && !char.IsWhiteSpace(value[^1]))
			return value;

		return "'" + value.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\t", "\\t").Replace("\r", "\\r").Replace("\n", "\\n") + "'";
	}
	#endregion

	#region 枚举遍历
	IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
	public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _entries.GetEnumerator();
	#endregion
}
