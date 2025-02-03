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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Configuration;

public abstract class ConnectionSettingsBase<TDriver> : Setting, IConnectionSettings, IEquatable<ConnectionSettingsBase<TDriver>>, IEquatable<IConnectionSettings> where TDriver : IConnectionSettingsDriver
{
	#region 成员字段
	private readonly TDriver _driver;
	private readonly Dictionary<string, string> _entries;
	#endregion

	#region 构造函数
	protected ConnectionSettingsBase(TDriver driver, string settings)
	{
		_driver = driver ?? throw new ArgumentNullException(nameof(driver));
		_entries = new(StringComparer.OrdinalIgnoreCase);
		base.Value = settings;
	}

	protected ConnectionSettingsBase(TDriver driver, string name, string settings) : base(name, settings)
	{
		_driver = driver ?? throw new ArgumentNullException(nameof(driver));
		_entries = new(StringComparer.OrdinalIgnoreCase);

		if(!string.IsNullOrEmpty(settings))
			this.OnValueChanged(settings);
	}
	#endregion

	#region 公共属性
	public TDriver Driver => _driver;
	IConnectionSettingsDriver IConnectionSettings.Driver => _driver;
	#endregion

	#region 保护属性
	protected IDictionary<string, string> Entries => _entries;
	#endregion

	#region 公共方法
	public bool IsDriver(string name) => ConnectionSettingsUtility.IsDriver(this, name);
	public bool IsDriver(IConnectionSettingsDriver driver) => ConnectionSettingsUtility.IsDriver(this, driver);
	#endregion

	#region 保护方法
	protected bool SetValue<T>(T value, [System.Runtime.CompilerServices.CallerMemberName]string name = null) => this.SetValue<T>(name, value);
	protected bool SetValue<T>(string name, T value)
	{
		if(_driver.Descriptors.TryGetValue(name, out var descriptor))
			return this.SetValue(descriptor, value);

		return false;
	}

	protected virtual bool SetValue<T>(ConnectionSettingDescriptor descriptor, T value)
	{
		if(Common.Convert.TryConvertValue<string>(value, () => descriptor.Converter, out var result))
		{
			if(string.IsNullOrEmpty(result))
				_entries.Remove(descriptor.Name);
			else
				_entries[descriptor.Name] = result;

			//更新 Value 属性值
			this.Value = string.Join(';', _entries.Where(entry => !string.IsNullOrEmpty(entry.Value)).Select(entry => $"{entry.Key}={entry.Value}"));

			return true;
		}

		return false;
	}

	protected T GetValue<T>([System.Runtime.CompilerServices.CallerMemberName]string name = null)
	{
		if(_driver.Descriptors.TryGetValue(name, out var descriptor))
			return this.GetValue<T>(descriptor);

		throw new InvalidOperationException($"The setting named '{name}' is illegal in connection Settings of the '{_driver.Name}' driver type.");
	}

	protected virtual T GetValue<T>(ConnectionSettingDescriptor descriptor)
	{
		if(_entries.TryGetValue(descriptor.Name, out var text) && Common.Convert.TryConvertValue<T>(text, () => descriptor.Converter, out var value))
			return value;

		return Common.Convert.ConvertValue<T>(descriptor.DefaultValue, () => descriptor.Converter);
	}

	protected virtual object GetValue(ConnectionSettingDescriptor descriptor)
	{
		if(_entries.TryGetValue(descriptor.Name, out var text) && Common.Convert.TryConvertValue(text, descriptor.Type, () => descriptor.Converter, out var value))
			return value;

		return Common.Convert.ConvertValue(descriptor.DefaultValue, descriptor.Type, () => descriptor.Converter);
	}

	internal bool TryGetValue<T>(string name, out T value)
	{
		if(_driver.Descriptors.TryGetValue(name, out var descriptor))
		{
			value = this.GetValue<T>(descriptor);
			return true;
		}

		value = default;
		return false;
	}

	internal bool TryGetValue(string name, out object value)
	{
		if(_driver.Descriptors.TryGetValue(name, out var descriptor))
		{
			value = this.GetValue(descriptor);
			return true;
		}

		value = null;
		return false;
	}
	#endregion

	#region 参数解析
	protected override void OnValueChanged(string value)
	{
		if(string.IsNullOrEmpty(value))
		{
			_entries.Clear();
			return;
		}

		var parts = value.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

		for(int i = 0; i < parts.Length; i++)
		{
			var part = parts[i];
			var index = part.IndexOf('=');

			if(index < 0)
			{
				var key = part.Trim();

				if(_driver.Descriptors.TryGetValue(key, out var descriptor))
					_entries[descriptor.Name] = null;
				else
					_entries[key] = null;
			}
			else if(index == part.Length - 1)
			{
				var key = part[0..^1].Trim();

				if(_driver.Descriptors.TryGetValue(key, out var descriptor))
					_entries[descriptor.Name] = null;
				else
					_entries[key] = null;
			}
			else if(index > 0 && index < part.Length - 1)
			{
				var key = part[..index].Trim();

				if(_driver.Descriptors.TryGetValue(key, out var descriptor))
					_entries[descriptor.Name] = string.IsNullOrWhiteSpace(part[(index + 1)..]) ? null : part[(index + 1)..].Trim();
				else
					_entries[key] = string.IsNullOrWhiteSpace(part[(index + 1)..]) ? null : part[(index + 1)..].Trim();
			}
		}
	}
	#endregion

	#region 重写方法
	public bool Equals(IConnectionSettings settings) => settings != null && string.Equals(this.Name, settings.Name, StringComparison.OrdinalIgnoreCase) && this.IsDriver(settings.Driver);
	public bool Equals(ConnectionSettingsBase<TDriver> settings) => settings != null && string.Equals(this.Name, settings.Name, StringComparison.OrdinalIgnoreCase) && this.IsDriver(settings.Driver);
	public override bool Equals(object obj) => obj is IConnectionSettings settings && this.Equals(settings);
	public override int GetHashCode() => HashCode.Combine(this.Name.ToLowerInvariant(), _driver.Name.ToLowerInvariant());
	public override string ToString() => _driver == null ?
		$"[{this.Name}]{this.Value}" :
		$"[{this.Name}@{this.Driver}]{this.Value}";
	#endregion

	#region 枚举遍历
	IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
	public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => ((IEnumerable<KeyValuePair<string, string>>)_entries).GetEnumerator();
	#endregion
}

public abstract class ConnectionSettingsBase<TDriver, TOptions> : ConnectionSettingsBase<TDriver> where TDriver : IConnectionSettingsDriver
{
	#region 静态成员
	private static readonly PropertyInfo[] _properties = typeof(TOptions)
		.GetProperties(BindingFlags.Public | BindingFlags.Instance)
		.Where(property => property.CanWrite && property.SetMethod.IsPublic)
		.ToArray();
	#endregion

	#region 构造函数
	protected ConnectionSettingsBase(TDriver driver, string settings) : base(driver, settings) { }
	protected ConnectionSettingsBase(TDriver driver, string name, string settings) : base(driver, name, settings) { }
	#endregion

	#region 公共方法
	public virtual TOptions GetOptions()
	{
		var options = this.CreateOptions();

		for(int i = 0; i < _properties.Length; i++)
		{
			if(this.TryGetValue(_properties[i].Name, out var value) && value is not null)
				Reflection.Reflector.TrySetValue(_properties[i], ref options, value);
		}

		return options;
	}
	#endregion

	#region 保护方法
	protected virtual TOptions CreateOptions() => Activator.CreateInstance<TOptions>();
	#endregion
}