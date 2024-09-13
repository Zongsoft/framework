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
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;

namespace Zongsoft.Configuration
{
	public class ConnectionSettings : Setting, IConnectionSettings, IEquatable<ConnectionSettings>, IEquatable<IConnectionSettings>
	{
		#region 静态构造
		static ConnectionSettings() => Drivers = new();
		#endregion

		#region 静态属性
		public static ConnectionSettingsDriverCollection Drivers { get; }
		#endregion

		#region 成员字段
		private readonly ConnectionSettingOptions _options;
		private IConnectionSettingsDriver _driver;
		#endregion

		#region 构造函数
		public ConnectionSettings() => _options = new ConnectionSettingOptions(this);
		public ConnectionSettings(string name, string value) : base(name, value)
		{
			_options = new ConnectionSettingOptions(this);

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
					if(this.HasProperties && this.Properties.TryGetValue(nameof(Driver), out var name) && name != null)
						_driver = Drivers.TryGetValue(name, out var driver) ? driver : ConnectionSettingsDriver.Unnamed;
					else
						_driver = ConnectionSettingsDriver.Unnamed;
				}

				return _driver;
			}
			set => _driver = value ?? ConnectionSettingsDriver.Unnamed;
		}

		public IConnectionSettingsOptions Options => _options;
		#endregion

		#region 公共方法
		public bool IsDriver(string name) => ConnectionSettingUtility.IsDriver(this.Driver, name);
		public bool IsDriver(IConnectionSettingsDriver driver) => ConnectionSettingUtility.IsDriver(this.Driver, driver);
		#endregion

		#region 参数解析
		protected override void OnValueChanged(string value)
		{
			if(string.IsNullOrEmpty(value))
			{
				_options.Clear();
				return;
			}

			foreach(var option in Zongsoft.Common.StringExtension.Slice(value, ';'))
			{
				var index = option.IndexOf('=');

				if(index < 0)
					_options[string.Empty] = option;
				else if(index == option.Length - 1)
					_options[option[0..^1]] = null;
				else if(index > 0 && index < option.Length - 1)
					_options[option.Substring(0, index)] = option[(index + 1)..];
			}
		}
		#endregion

		#region 重写方法
		public bool Equals(IConnectionSettings settings) => settings != null &&
			string.Equals(this.Name, settings.Name, StringComparison.OrdinalIgnoreCase) && this.IsDriver(settings.Driver);

		public bool Equals(ConnectionSettings settings) => settings != null &&
			string.Equals(this.Name, settings.Name, StringComparison.OrdinalIgnoreCase) && this.IsDriver(settings.Driver);

		public override bool Equals(object obj) => obj is IConnectionSettings settings && this.Equals(settings);
		public override int GetHashCode() => HashCode.Combine(this.Name.ToLowerInvariant(), this.Driver?.Name?.ToLowerInvariant());
		public override string ToString() => this.Driver == null ?
			$"[{this.Name}]{this.Value}" :
			$"[{this.Name}@{this.Driver}]{this.Value}";
		#endregion

		#region 枚举遍历
		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _options.GetEnumerator();
		#endregion

		#region 嵌套子类
		private sealed class DriverConverter : TypeConverter
		{
			public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string);
			public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) => new DriverProxy(value as string);

			private sealed class DriverProxy(string name) : IConnectionSettingsDriver
			{
				private readonly string _name = name;

				public string Name => _name;
				public IConnectionSettingsDriver Driver => string.IsNullOrEmpty(_name) ? ConnectionSettingsDriver.Unnamed : Drivers.TryGetValue(_name, out var driver) ? driver : ConnectionSettingsDriver.Unnamed;

				public string Description
				{
					get => this.Driver.Description;
					set => this.Driver.Description = value;
				}

				public IConnectionSettingsMapper Mapper => this.Driver.Mapper;
				public ConnectionSettingDescriptorCollection Descriptors => this.Driver.Descriptors;
				public bool Equals(IConnectionSettingsDriver other) => this.Driver.Equals(other);
			}
		}

		private sealed class ConnectionSettingOptions : IConnectionSettingsOptions
		{
			#region 成员字段
			private readonly IConnectionSettings _connectionSettings;
			private readonly Dictionary<string, string> _dictionary;
			#endregion

			#region 构造函数
			public ConnectionSettingOptions(IConnectionSettings connectionSetting)
			{
				_connectionSettings = connectionSetting;
				_dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			}
			#endregion

			#region 通用属性
			public int Count => _dictionary.Count;
			public string this[string name]
			{
				get => this.GetValue(name);
				set => this.SetValue(name, value);
			}
			#endregion

			#region 特定属性
			public string Group
			{
				get => this.GetValue(nameof(Group));
				set => this.SetValue(nameof(Group), value);
			}

			public string Client
			{
				get => this.GetValue(nameof(Client));
				set => this.SetValue(nameof(Client), value);
			}

			public string Server
			{
				get => this.GetValue(nameof(Server));
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
				get => this.GetValue(nameof(Charset));
				set => this.SetValue(nameof(Charset), value);
			}

			public string Encoding
			{
				get => this.GetValue(nameof(Encoding));
				set => this.SetValue(nameof(Encoding), value);
			}

			public string Provider
			{
				get => this.GetValue(nameof(Provider));
				set => this.SetValue(nameof(Provider), value);
			}

			public string Database
			{
				get => this.GetValue(nameof(Database));
				set => this.SetValue(nameof(Database), value);
			}

			public string UserName
			{
				get => this.GetValue(nameof(UserName));
				set => this.SetValue(nameof(UserName), value);
			}

			public string Password
			{
				get => this.GetValue(nameof(Password));
				set => this.SetValue(nameof(Password), value);
			}

			public string Instance
			{
				get => this.GetValue(nameof(Instance));
				set => this.SetValue(nameof(Instance), value);
			}

			public string Application
			{
				get => this.GetValue(nameof(Application)) ?? Services.ApplicationContext.Current?.Name;
				set => this.SetValue(nameof(Application), value);
			}
			#endregion

			#region 公共方法
			public void Clear() => _dictionary.Clear();
			public bool Contains(string name) => _dictionary.ContainsKey(name);
			public bool Remove(string name) => _dictionary.Remove(name);
			public bool Remove(string name, out string value) => _dictionary.Remove(name, out value);

			public bool SetValue(string name, string value)
			{
				if(_connectionSettings.Driver != null && _connectionSettings.Driver.Mapper != null)
				{
					if(!_connectionSettings.Driver.Mapper.Validate(name, value))
						return false;
				}

				_dictionary[name] = value;
				return true;
			}

			public string GetValue(string name)
			{
				if(_connectionSettings.Driver != null && _connectionSettings.Driver.Mapper != null && _connectionSettings.Driver.Mapper.Mapping.ContainsKey(name))
					return _connectionSettings.Driver.Mapper.Map<string>(name, _dictionary);

				return _dictionary.TryGetValue(name, out var value) ? value : null;
			}

			public T GetValue<T>(string name, T defaultValue = default)
			{
				if(_connectionSettings.Driver != null && _connectionSettings.Driver.Mapper != null && _connectionSettings.Driver.Mapper.Mapping.ContainsKey(name))
					return _connectionSettings.Driver.Mapper.Map<T>(name, _dictionary);

				if(_dictionary.TryGetValue(name, out var value))
					return Zongsoft.Common.Convert.ConvertValue<T>(value, defaultValue);

				return defaultValue;
			}

			public bool TryGetValue<T>(string name, out T value)
			{
				if(_connectionSettings.Driver != null && _connectionSettings.Driver.Mapper != null && _connectionSettings.Driver.Mapper.Mapping.ContainsKey(name))
					return _connectionSettings.Driver.Mapper.Map<T>(name, _dictionary, out value);

				if(_dictionary.TryGetValue(name, out var text))
					return Zongsoft.Common.Convert.TryConvertValue<T>(text, out value);

				value = default;
				return false;
			}
			#endregion

			#region 重写方法
			public override string ToString()
			{
				return _dictionary == null || _dictionary.Count == 0 ? string.Empty : string.Join(';', _dictionary.Select(entry => $"{entry.Key}={entry.Value}"));
			}
			#endregion

			#region 枚举遍历
			public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _dictionary.GetEnumerator();
			IEnumerator IEnumerable.GetEnumerator() => _dictionary.GetEnumerator();
			#endregion
		}
		#endregion
	}
}
