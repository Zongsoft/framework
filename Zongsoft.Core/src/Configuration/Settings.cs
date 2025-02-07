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
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Configuration;

public class Settings : IEnumerable<KeyValuePair<string, string>>
{
	#region 成员字段
	private string _name;
	private string _value;
	private Dictionary<string, string> _settings;
	#endregion

	#region 构造函数
	/// <summary>构建一个设置。</summary>
	/// <param name="name">指定的设置名称。</param>
	/// <param name="value">指定的设置内容。</param>
	/// <remarks>注意：通过本构造函数的 <paramref name="value"/> 参数设置 <see cref="Value"/> 属性并不会触发 <see cref="OnValueChanged(string)"/> 回调方法。</remarks>
	public Settings(string name, string value = null)
	{
		_name = name == null ? string.Empty : name.Trim();
		_value = value?.Trim();
		_settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	}
	#endregion

	#region 公共属性
	public string Name => _name;
	public string Value
	{
		get => _value;
		set
		{
			if(!string.Equals(_value, value))
			{
				_value = value;
				this.OnValueChanged(_value);
			}
		}
	}

	public bool IsEmpty => _settings.Count == 0;
	public string this[string name]
	{
		get => _settings.TryGetValue(name, out var value) ? value : null;
		set
		{
			if(string.IsNullOrEmpty(value))
				_settings.Remove(name);
			else
				_settings[name] = value.Trim();

			_value = string.Join(';', _settings);
		}
	}
	#endregion

	#region 静态方法
	public static IEnumerable<KeyValuePair<string, string>> Parse(string text)
	{
		if(string.IsNullOrEmpty(text))
			yield break;

		var parts = text.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

		for(int i = 0; i < parts.Length; i++)
		{
			var part = parts[i];
			var index = part.IndexOf('=');

			if(index < 0)
				yield return new KeyValuePair<string, string>(part, null);
			else if(index == part.Length - 1)
				yield return new KeyValuePair<string, string>(part[0..^1].Trim(), null);
			else if(index > 0 && index < part.Length - 1)
				yield return new KeyValuePair<string, string>(
					part[..index].Trim(),
					string.IsNullOrWhiteSpace(part[(index + 1)..]) ? null : part[(index + 1)..].Trim()
				);
		}
	}
	#endregion

	#region 虚拟方法
	protected virtual void OnValueChanged(string value)
	{
		_settings.Clear();

		foreach(var entry in Parse(value))
			_settings[entry.Key] = entry.Value;
	}
	#endregion

	#region 重写方法
	public bool Equals(Settings settings) => settings is not null && string.Equals(_name, settings._name, StringComparison.OrdinalIgnoreCase);
	public override bool Equals(object obj) => obj is Settings settings && this.Equals(settings);
	public override int GetHashCode() => HashCode.Combine(_name.ToLowerInvariant());
	public override string ToString() => $"{this.Name}={this.Value}";
	#endregion

	#region 枚举遍历
	IEnumerator IEnumerable.GetEnumerator() => _settings.GetEnumerator();
	public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _settings.GetEnumerator();
	#endregion
}
