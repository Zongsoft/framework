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

public abstract class ConnectionSettingsBase : Setting, IConnectionSettings, IEquatable<ConnectionSettingsBase>, IEquatable<IConnectionSettings>
{
	#region 成员字段
	private readonly Dictionary<string, string> _entries;
	#endregion

	#region 构造函数
	protected ConnectionSettingsBase(string settings) : base(string.Empty, settings)
	{
		_entries = new(StringComparer.OrdinalIgnoreCase);
	}

	protected ConnectionSettingsBase(string name, string settings) : base(name, settings)
	{
		_entries = new(StringComparer.OrdinalIgnoreCase);
	}
	#endregion

	#region 公共属性
	public string this[string name]
	{
		get
		{
			if(this.GetDriver().Descriptors.TryGetValue(name, out var descriptor))
				return this.GetValue<string>(descriptor);

			return _entries.TryGetValue(name, out var value) ? value : null;
		}
		set
		{
			if(this.GetDriver().Descriptors.TryGetValue(name, out var descriptor))
			{
				this.SetValue(descriptor, value);
				return;
			}

			if(string.IsNullOrEmpty(value))
				_entries.Remove(name);
			else
				_entries[name] = value;

			//更新 Value 属性值
			this.Value = string.Join(';', _entries.Where(entry => !string.IsNullOrEmpty(entry.Value)).Select(entry => $"{entry.Key}={entry.Value}"));
		}
	}
	#endregion

	#region 保护属性
	protected IDictionary<string, string> Entries => _entries;
	#endregion

	#region 显式实现
	IConnectionSettingsDriver IConnectionSettings.Driver => this.GetDriver();
	#endregion

	#region 抽象方法
	protected abstract IConnectionSettingsDriver GetDriver();
	#endregion

	#region 公共方法
	public bool IsDriver(string name) => ConnectionSettingsUtility.IsDriver(this, name);
	public bool IsDriver(IConnectionSettingsDriver driver) => ConnectionSettingsUtility.IsDriver(this, driver);
	#endregion

	#region 保护方法
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
	#endregion

	#region 参数解析
	protected override void OnValueChanged(string value)
	{
		if(string.IsNullOrEmpty(value))
		{
			_entries.Clear();
			return;
		}

		var driver = this.GetDriver();
		var parts = value.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

		for(int i = 0; i < parts.Length; i++)
		{
			var part = parts[i];
			var index = part.IndexOf('=');

			if(index < 0)
			{
				var key = part.Trim();

				if(driver.Descriptors.TryGetValue(key, out var descriptor))
					_entries[descriptor.Name] = null;
				else
					_entries[key] = null;
			}
			else if(index == part.Length - 1)
			{
				var key = part[0..^1].Trim();

				if(driver.Descriptors.TryGetValue(key, out var descriptor))
					_entries[descriptor.Name] = null;
				else
					_entries[key] = null;
			}
			else if(index > 0 && index < part.Length - 1)
			{
				var key = part[..index].Trim();

				if(driver.Descriptors.TryGetValue(key, out var descriptor))
					_entries[descriptor.Name] = string.IsNullOrWhiteSpace(part[(index + 1)..]) ? null : part[(index + 1)..].Trim();
				else
					_entries[key] = string.IsNullOrWhiteSpace(part[(index + 1)..]) ? null : part[(index + 1)..].Trim();
			}
		}
	}
	#endregion

	#region 重写方法
	public bool Equals(IConnectionSettings settings) => settings != null && string.Equals(this.Name, settings.Name, StringComparison.OrdinalIgnoreCase) && this.IsDriver(settings.Driver);
	public bool Equals(ConnectionSettingsBase settings) => settings != null && string.Equals(this.Name, settings.Name, StringComparison.OrdinalIgnoreCase) && this.IsDriver(settings.GetDriver());
	public override bool Equals(object obj) => obj is IConnectionSettings settings && this.Equals(settings);
	public override int GetHashCode() => HashCode.Combine(this.Name.ToLowerInvariant(), this.GetDriver().Name.ToLowerInvariant());
	public override string ToString() => $"[{this.Name}@{this.GetDriver()}]{this.Value}";
	#endregion

	#region 枚举遍历
	IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
	public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _entries.GetEnumerator();
	#endregion
}

public abstract class ConnectionSettingsBase<TDriver> : ConnectionSettingsBase, IEquatable<ConnectionSettingsBase<TDriver>> where TDriver : IConnectionSettingsDriver
{
	#region 成员字段
	private readonly TDriver _driver;
	#endregion

	#region 构造函数
	protected ConnectionSettingsBase(TDriver driver, string settings) : base(settings)
	{
		_driver = driver ?? throw new ArgumentNullException(nameof(driver));

		//注意：必须显式调用基类的 OnValueChanged 方法以解析设置项集。
		if(!string.IsNullOrEmpty(settings))
			this.OnValueChanged(settings);
	}

	protected ConnectionSettingsBase(TDriver driver, string name, string settings) : base(name, settings)
	{
		_driver = driver ?? throw new ArgumentNullException(nameof(driver));

		//注意：必须显式调用基类的 OnValueChanged 方法以解析设置项集。
		if(!string.IsNullOrEmpty(settings))
			this.OnValueChanged(settings);
	}
	#endregion

	#region 公共属性
	public TDriver Driver => _driver;
	protected override IConnectionSettingsDriver GetDriver() => _driver;
	#endregion

	#region 保护方法
	protected bool SetValue<T>(T value, [System.Runtime.CompilerServices.CallerMemberName]string name = null) => this.SetValue<T>(name, value);
	protected bool SetValue<T>(string name, T value)
	{
		if(_driver.Descriptors.TryGetValue(name, out var descriptor))
			return this.SetValue(descriptor, value);

		return false;
	}

	protected T GetValue<T>([System.Runtime.CompilerServices.CallerMemberName]string name = null)
	{
		if(_driver.Descriptors.TryGetValue(name, out var descriptor))
			return this.GetValue<T>(descriptor);

		throw new InvalidOperationException($"The setting named '{name}' is illegal in connection Settings of the '{_driver.Name}' driver type.");
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

	#region 重写方法
	public bool Equals(ConnectionSettingsBase<TDriver> settings) => settings != null && string.Equals(this.Name, settings.Name, StringComparison.OrdinalIgnoreCase) && this.IsDriver(settings.Driver);
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
	public TOptions GetOptions()
	{
		var options = this.CreateOptions();
		this.Populate(options);
		return options;
	}
	#endregion

	#region 虚拟方法
	protected virtual TOptions CreateOptions() => Activator.CreateInstance<TOptions>();
	protected virtual void Populate(TOptions options)
	{
		if(options is null)
			throw new ArgumentNullException(nameof(options));

		for(int i = 0; i < _properties.Length; i++)
		{
			//获取当前属性对应的设置项描述器，如果获取成功并且该属性不为忽略项则进行属性设置
			if(this.Driver.Descriptors.TryGetValue(_properties[i].Name, out var descriptor) && !descriptor.Ignored)
			{
				//获取当前属性对应的设置项值
				var value = this.GetValue(descriptor);

				//如果设置项指定了组装器，则必须将解析后的值再通过组装器转换为目标属性的类型
				if(descriptor.Populator != null)
					value = Common.Convert.ConvertValue(value, _properties[i].PropertyType, () => descriptor.Populator);

				Reflection.Reflector.TrySetValue(_properties[i], ref options, value);
			}
		}
	}
	#endregion
}