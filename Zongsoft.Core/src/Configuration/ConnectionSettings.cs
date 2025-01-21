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
	private readonly EntryCollection _values;
	#endregion

	#region 构造函数
	public ConnectionSettings() => _values = new();
	public ConnectionSettings(string settings, IConnectionSettingsDriver driver = null)
	{
		_values = new();
		this.Driver = driver;
		this.Value = settings;
	}

	public ConnectionSettings(string name, string value) : base(name, value)
	{
		_values = new();
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
					_driver = Drivers.TryGetValue(name, out var driver) ? driver : ConnectionSettingsDriver.Default;
				else
					_driver = ConnectionSettingsDriver.Default;
			}

			return _driver;
		}
		init => _driver = value ?? ConnectionSettingsDriver.Default;
	}
	public IDictionary<object, string> Values => _values;
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
		set => this.SetValue(nameof(Port), value);
	}

	public TimeSpan Timeout
	{
		get => this.GetValue(nameof(Timeout), TimeSpan.Zero);
		set => this.SetValue(nameof(Timeout), value);
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

	public object GetOptions() => this.Driver.GetOptions(this);
	public TOptions GetOptions<TOptions>() => this.Driver.GetOptions(this) is TOptions options ? options : default;
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
	public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => ((IEnumerable<KeyValuePair<string, string>>)_values).GetEnumerator();
	#endregion

	#region 嵌套子类
	private sealed class DriverConverter : TypeConverter
	{
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string);
		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) => new DriverProxy(value as string);

		private sealed class DriverProxy(string name) : IConnectionSettingsDriver
		{
			private readonly string _name = name;

			public string Name => _name ?? string.Empty;
			public string Description => this.Driver.Description;
			public IConnectionSettingsDriver Driver => Drivers.TryGetValue(_name, out var driver) ? driver : ConnectionSettingsDriver.Default;

			public object GetOptions(IConnectionSettings settings) => this.Driver.GetOptions(settings);
			public bool TryGetValue(string name, IDictionary<object, string> values, out object value) => this.Driver.TryGetValue(name, values, out value);
			public T GetValue<T>(string name, IDictionary<object, string> values, T defaultValue) => this.Driver.GetValue(name, values, defaultValue);
			public bool SetValue<T>(string name, T value, IDictionary<object, string> values) => this.Driver.SetValue(name, value, values);

			public static ConnectionSettingDescriptorCollection Descriptors => null;
			public bool Equals(IConnectionSettingsDriver other) => this.Driver.Equals(other);
		}
	}

	private sealed class EntryCollection : IDictionary<object, string>, IEnumerable<KeyValuePair<string, string>>
	{
		private readonly Dictionary<string, string> _dictionary = new(StringComparer.OrdinalIgnoreCase);

		public string this[object key]
		{
			get
			{
				if(key == null)
					throw new ArgumentNullException(nameof(key));

				return key switch
				{
					string name => this[name],
					ConnectionSettingDescriptor descriptor => this[descriptor],
					_ => throw new ArgumentException($"Unsupported key type: {key.GetType().FullName}.")
				};
			}
			set
			{
				if(key == null)
					throw new ArgumentNullException(nameof(key));

				switch(key)
				{
					case string name:
						this[name] = value;
						break;
					case ConnectionSettingDescriptor descriptor:
						this[descriptor] = value;
						break;
					default:
						throw new ArgumentException($"Unsupported key type: {key.GetType().FullName}.");
				}
			}
		}

		public string this[string key]
		{
			get => _dictionary[key];
			set
			{
				if(string.IsNullOrEmpty(key))
					throw new ArgumentNullException(nameof(key));

				if(string.IsNullOrEmpty(value))
					_dictionary.Remove(key);
				else
					_dictionary[key] = value;
			}
		}

		public string this[ConnectionSettingDescriptor key]
		{
			get
			{
				if(key == null)
					throw new ArgumentNullException(nameof(key));

				if(_dictionary.TryGetValue(key.Name, out var value))
					return value;

				if(key.Alias != null)
					_dictionary[key.Alias] = value;

				throw new KeyNotFoundException();
			}
			set
			{
				if(key == null)
					throw new ArgumentNullException(nameof(key));

				if(string.IsNullOrEmpty(value))
					_dictionary.Remove(key.Name);
				else
					_dictionary[key.Name] = value;

				if(!string.IsNullOrEmpty(key.Alias))
					_dictionary.Remove(key.Alias);
			}
		}

		public ICollection<object> Keys => [.._dictionary.Keys];
		public ICollection<string> Values => _dictionary.Values;

		public int Count => _dictionary.Count;
		bool ICollection<KeyValuePair<object, string>>.IsReadOnly => false;

		public void Add(object key, string value)
		{
			if(key == null)
				throw new ArgumentNullException(nameof(key));

			switch(key)
			{
				case string name:
					_dictionary.Add(name, value);
					break;
				case ConnectionSettingDescriptor descriptor:
					_dictionary.Add(descriptor.Name, value);
					break;
				default:
					throw new ArgumentException($"Unsupported key type: {key.GetType().FullName}.");
			}
		}

		public void Add(string key, string value)
		{
			if(string.IsNullOrEmpty(key))
				throw new ArgumentNullException(nameof(key));

			if(!string.IsNullOrEmpty(value))
				_dictionary.Add(key, value);
		}

		public void Add(ConnectionSettingDescriptor key, string value)
		{
			if(key == null)
				throw new ArgumentNullException(nameof(key));

			if(!string.IsNullOrEmpty(value))
				_dictionary.Add(key.Name, value);
		}

		void ICollection<KeyValuePair<object, string>>.Add(KeyValuePair<object, string> item) => this.Add(item.Key, item.Value);
		public void Clear() => _dictionary.Clear();
		bool ICollection<KeyValuePair<object, string>>.Contains(KeyValuePair<object, string> item) => this.ContainsKey(item.Key);
		public bool ContainsKey(object key) => key switch
		{
			string name => this.Contains(name),
			ConnectionSettingDescriptor descriptor => this.Contains(descriptor),
			_ => false,
		};
		public bool Contains(string key) => key != null && _dictionary.ContainsKey(key);
		public bool Contains(ConnectionSettingDescriptor key) => key != null && (_dictionary.ContainsKey(key.Name) || (key.Alias != null && _dictionary.ContainsKey(key.Alias)));

		void ICollection<KeyValuePair<object, string>>.CopyTo(KeyValuePair<object, string>[] array, int arrayIndex) => ((ICollection<KeyValuePair<object, string>>)_dictionary).CopyTo(array, arrayIndex);
		public bool Remove(object key) => key switch
		{
			string name => this.Remove(name),
			ConnectionSettingDescriptor descriptor => this.Remove(descriptor),
			_ => false,
		};
		public bool Remove(string key) => key != null && _dictionary.Remove(key);
		public bool Remove(ConnectionSettingDescriptor key)
		{
			if(key == null)
				return false;

			var result = _dictionary.Remove(key.Name);
			result |= key.Alias != null && _dictionary.Remove(key.Alias);
			return result;
		}

		bool ICollection<KeyValuePair<object, string>>.Remove(KeyValuePair<object, string> item) => this.Remove(item.Key);
		public bool TryGetValue(object key, out string value)
		{
			switch(key)
			{
				case string name:
					return this.TryGetValue(name, out value);
				case ConnectionSettingDescriptor descriptor:
					return this.TryGetValue(descriptor, out value);
			}

			value = null;
			return false;
		}

		public bool TryGetValue(string key, out string value) => _dictionary.TryGetValue(key, out value);
		public bool TryGetValue(ConnectionSettingDescriptor key, out string value)
		{
			if(_dictionary.TryGetValue(key.Name, out value))
				return true;

			if(key.Alias != null && _dictionary.TryGetValue(key.Alias, out value))
				return true;

			value = null;
			return false;
		}

		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		IEnumerator<KeyValuePair<string, string>> IEnumerable<KeyValuePair<string, string>>.GetEnumerator() => _dictionary.GetEnumerator();
		public IEnumerator<KeyValuePair<object, string>> GetEnumerator()
		{
			foreach(var entry in _dictionary)
				yield return new KeyValuePair<object, string>(entry.Key, entry.Value);
		}
	}
	#endregion
}
