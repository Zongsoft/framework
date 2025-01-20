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
using System.Globalization;
using System.ComponentModel;

namespace Zongsoft.Configuration;

[Zongsoft.Services.Service(Members = nameof(Drivers))]
public class ConnectionSettings : Setting, IConnectionSettings, IEquatable<ConnectionSettings>, IEquatable<IConnectionSettings>
{
	#region 静态构造
	static ConnectionSettings() => Drivers = new();
	#endregion

	#region 静态属性
	public static ConnectionSettingsDriverCollection Drivers { get; }
	#endregion

	#region 成员字段
	private IConnectionSettingsDriver _driver;
	private readonly Dictionary<string, string> _values;
	#endregion

	#region 构造函数
	public ConnectionSettings() => _values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	public ConnectionSettings(string name, string value) : base(name, value)
	{
		_values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		if(!string.IsNullOrEmpty(value))
			this.OnValueChanged(value);
	}
	#endregion

	#region 公共属性
	[TypeConverter(typeof(DriverConverter))]
	public IConnectionSettingsDriver Driver
	{
		get
		{
			if(_driver == null)
			{
				if(this.HasProperties && this.Properties.TryGetValue(nameof(this.Driver), out var name) && name != null)
					_driver = Drivers.TryGetValue(name, out var driver) ? driver : ConnectionSettingsDriver.Unnamed;
				else
					_driver = ConnectionSettingsDriver.Unnamed;
			}

			return _driver;
		}
		init => _driver = value ?? ConnectionSettingsDriver.Unnamed;
	}
	public IDictionary<string, string> Values => _values;
	public object this[string name]
	{
		get => this.GetValue(name);
		set => this.SetValue(name, value);
	}
	#endregion

	#region 特定属性
	public string Group
	{
		get => this.GetValue<string>(nameof(Group));
		set => this.SetValue(nameof(Group), value);
	}

	public string Client
	{
		get => this.GetValue<string>(nameof(Client));
		set => this.SetValue(nameof(Client), value);
	}

	public string Server
	{
		get => this.GetValue<string>(nameof(Server));
		set => this.SetValue(nameof(Server), value);
	}

	public ushort Port
	{
		get => this.GetValue(nameof(Port), (ushort)0);
		set => this.SetValue(nameof(Port), value.ToString());
	}

	public TimeSpan Timeout
	{
		get => this.GetValue(nameof(Timeout), TimeSpan.Zero);
		set => this.SetValue(nameof(Timeout), value.ToString());
	}

	public string Charset
	{
		get => this.GetValue<string>(nameof(Charset));
		set => this.SetValue(nameof(Charset), value);
	}

	public string Encoding
	{
		get => this.GetValue<string>(nameof(Encoding));
		set => this.SetValue(nameof(Encoding), value);
	}

	public string Provider
	{
		get => this.GetValue<string>(nameof(Provider));
		set => this.SetValue(nameof(Provider), value);
	}

	public string Database
	{
		get => this.GetValue<string>(nameof(Database));
		set => this.SetValue(nameof(Database), value);
	}

	public string UserName
	{
		get => this.GetValue<string>(nameof(UserName));
		set => this.SetValue(nameof(UserName), value);
	}

	public string Password
	{
		get => this.GetValue<string>(nameof(Password));
		set => this.SetValue(nameof(Password), value);
	}

	public string Instance
	{
		get => this.GetValue<string>(nameof(Instance));
		set => this.SetValue(nameof(Instance), value);
	}

	public string Application
	{
		get => this.GetValue<string>(nameof(Application)) ?? Services.ApplicationContext.Current?.Name;
		set => this.SetValue(nameof(Application), value);
	}
	#endregion

	#region 公共方法
	public bool Contains(string name) => _values.ContainsKey(name);
	public bool IsDriver(string name) => ConnectionSettingsUtility.IsDriver(this, name);
	public bool IsDriver(IConnectionSettingsDriver driver) => ConnectionSettingsUtility.IsDriver(this, driver);

	public TOptions GetOptions<TOptions>() => this.Driver.GetOptions<TOptions>(this);
	public bool SetValue<T>(string name, T value) => this.Driver.SetValue(name, value, _values);
	public object GetValue(string name) => this.Driver.TryGetValue(name, _values, out var value) ? value : default;
	public T GetValue<T>(string name, T defaultValue = default) => this.Driver.GetValue(name, _values, defaultValue);
	public bool TryGetValue(string name, out object value) => this.Driver.TryGetValue(name, _values, out value);
	#endregion

	#region 参数解析
	protected override void OnValueChanged(string value)
	{
		if(string.IsNullOrEmpty(value))
		{
			_values.Clear();
			return;
		}

		var parts = value.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

		for(int i = 0; i < parts.Length; i++)
		{
			var part = parts[i];
			var index = part.IndexOf('=');

			if(index < 0)
				_values[part.Trim()] = null;
			else if(index == part.Length - 1)
				_values[part[0..^1].Trim()] = null;
			else if(index > 0 && index < part.Length - 1)
				_values[part[..index].Trim()] = string.IsNullOrWhiteSpace(part[(index + 1)..]) ? null : part[(index + 1)..].Trim();
		}
	}
	#endregion

	#region 重写方法
	public bool Equals(IConnectionSettings settings) => settings != null && string.Equals(this.Name, settings.Name, StringComparison.OrdinalIgnoreCase) && this.IsDriver(settings.Driver);
	public bool Equals(ConnectionSettings settings) => settings != null && string.Equals(this.Name, settings.Name, StringComparison.OrdinalIgnoreCase) && this.IsDriver(settings.Driver);
	public override bool Equals(object obj) => obj is IConnectionSettings settings && this.Equals(settings);
	public override int GetHashCode() => HashCode.Combine(this.Name.ToLowerInvariant(), this.Driver?.Name?.ToLowerInvariant());
	public override string ToString() => this.Driver == null ?
		$"[{this.Name}]{this.Value}" :
		$"[{this.Name}@{this.Driver}]{this.Value}";
	#endregion

	#region 枚举遍历
	IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
	public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _values.GetEnumerator();
	#endregion

	#region 嵌套子类
	private sealed class DriverConverter : TypeConverter
	{
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string);
		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) => new DriverProxy(value as string);

		private sealed class DriverProxy(string name) : IConnectionSettingsDriver
		{
			private readonly string _name = name;

			public string Name => _name ?? "?";
			public IConnectionSettingsDriver Driver => Drivers.TryGetValue(_name, out var driver) ? driver : ConnectionSettingsDriver.Unnamed;

			public string Description
			{
				get => this.Driver.Description;
				set => this.Driver.Description = value;
			}

			public TOptions GetOptions<TOptions>(IConnectionSettings settings) => this.Driver.GetOptions<TOptions>(settings);
			public bool TryGetValue(string name, IDictionary<string, string> values, out object value) => this.Driver.TryGetValue(name, values, out value);
			public T GetValue<T>(string name, IDictionary<string, string> values, T defaultValue) => this.Driver.GetValue(name, values, defaultValue);
			public bool SetValue<T>(string name, T value, IDictionary<string, string> values) => this.Driver.SetValue(name, value, values);

			public static ConnectionSettingDescriptorCollection Descriptors => null;
			public bool Equals(IConnectionSettingsDriver other) => this.Driver.Equals(other);
		}
	}
	#endregion
}
