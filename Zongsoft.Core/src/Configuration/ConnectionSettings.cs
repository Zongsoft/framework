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
					if(this.HasProperties && this.Properties.TryGetValue(nameof(Driver), out var name) && name != null)
						_driver = Drivers.TryGetValue(name, out var driver) ? driver : ConnectionSettingsDriver.Unnamed;
					else
						_driver = ConnectionSettingsDriver.Unnamed;
				}

				return _driver;
			}
			set => _driver = value ?? ConnectionSettingsDriver.Unnamed;
		}

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
		public bool Contains(string name) => _values.ContainsKey(name);
		public bool IsDriver(string name) => ConnectionSettingsUtility.IsDriver(this, name);
		public bool IsDriver(IConnectionSettingsDriver driver) => ConnectionSettingsUtility.IsDriver(this, driver);

		public TModel Model<TModel>()
		{
			var modeler = this.Driver.Modeler;
			if(modeler == null)
				return default;

			var model = this.Driver.Modeler.Model(this);
			if(model == null)
				return default;

			return typeof(TModel).IsAssignableFrom(model.GetType()) ? (TModel)model :
				throw new InvalidOperationException($"Unable to convert a connection configuration object of type '{model.GetType().FullName}' to type '{typeof(TModel).FullName}'.");
		}

		public bool SetValue(string name, string value)
		{
			if(this.Driver != null && this.Driver.Mapper != null)
			{
				if(!this.Driver.Mapper.Validate(name, value))
					return false;
			}

			_values[name] = value;
			return true;
		}

		public string GetValue(string name)
		{
			if(this.Driver != null && this.Driver.Mapper != null && this.Driver.Mapper.Mapping.ContainsKey(name))
				return this.Driver.Mapper.Map<string>(name, _values);

			return _values.TryGetValue(name, out var value) ? value : null;
		}

		public object GetValue(string name, Type type)
		{
			if(this.Driver != null && this.Driver.Mapper != null && this.Driver.Mapper.Mapping.ContainsKey(name))
				return this.Driver.Mapper.Map<object>(name, _values);

			return _values.TryGetValue(name, out var value) ? Common.Convert.ConvertValue(value, type) : null;
		}

		public T GetValue<T>(string name, T defaultValue = default)
		{
			if(this.Driver != null && this.Driver.Mapper != null && this.Driver.Mapper.Mapping.ContainsKey(name))
				return this.Driver.Mapper.Map<T>(name, _values);

			if(_values.TryGetValue(name, out var value))
				return Zongsoft.Common.Convert.ConvertValue<T>(value, defaultValue);

			return defaultValue;
		}

		public bool TryGetValue<T>(string name, out T value)
		{
			if(this.Driver != null && this.Driver.Mapper != null && this.Driver.Mapper.Mapping.ContainsKey(name))
				return this.Driver.Mapper.Map<T>(name, _values, out value);

			if(_values.TryGetValue(name, out var text))
				return Zongsoft.Common.Convert.TryConvertValue<T>(text, out value);

			value = default;
			return false;
		}
		#endregion

		#region 参数解析
		protected override void OnValueChanged(string value)
		{
			if(string.IsNullOrEmpty(value))
			{
				_values.Clear();
				return;
			}

			foreach(var option in Zongsoft.Common.StringExtension.Slice(value, ';'))
			{
				var index = option.IndexOf('=');

				if(index < 0)
					_values[string.Empty] = option;
				else if(index == option.Length - 1)
					_values[option[0..^1]] = null;
				else if(index > 0 && index < option.Length - 1)
					_values[option.Substring(0, index)] = option[(index + 1)..];
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

				public string Name => _name;
				public IConnectionSettingsDriver Driver => string.IsNullOrEmpty(_name) ?
					ConnectionSettingsDriver.Unnamed :
					Drivers.TryGetValue(_name, out var driver) ? driver : ConnectionSettingsDriver.Unnamed;

				public string Description
				{
					get => this.Driver.Description;
					set => this.Driver.Description = value;
				}

				public IConnectionSettingsMapper Mapper => this.Driver.Mapper;
				public IConnectionSettingsModeler Modeler => this.Driver.Modeler;
				public ConnectionSettingDescriptorCollection Descriptors => this.Driver.Descriptors;
				public bool Equals(IConnectionSettingsDriver other) => this.Driver.Equals(other);
			}
		}
		#endregion
	}
}
