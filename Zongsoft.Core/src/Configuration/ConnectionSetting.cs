/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
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

namespace Zongsoft.Configuration
{
	public class ConnectionSetting : Setting, IConnectionSetting, IEquatable<ConnectionSetting>, IEquatable<IConnectionSetting>
	{
		#region 静态字段
		private static readonly Dictionary<string, IConnectionSettingValuesMapper> _mappers;
		#endregion

		#region 静态构造
		static ConnectionSetting()
		{
			_mappers = new Dictionary<string, IConnectionSettingValuesMapper>(StringComparer.OrdinalIgnoreCase);
			Mappers = new ConnectionSettingValuesMapperCollection();
		}
		#endregion

		#region 静态属性
		public static ICollection<IConnectionSettingValuesMapper> Mappers { get; }
		#endregion

		#region 成员字段
		private readonly ConnectionSettingValues _values;
		#endregion

		#region 构造函数
		public ConnectionSetting()
		{
			_values = new ConnectionSettingValues(this);
		}

		public ConnectionSetting(string name, string value) : base(name, value)
		{
			_values = new ConnectionSettingValues(this);

			if(!string.IsNullOrEmpty(value))
				this.OnValueChanged(value);
		}
		#endregion

		#region 公共属性
		public string Driver
		{
			get => this.HasProperties && this.Properties.TryGetValue(nameof(Driver), out var value) ? value : null;
			set => this.Properties[nameof(Driver)] = value;
		}

		public IConnectionSettingValues Values { get => _values; }
		#endregion

		#region 公共方法
		public bool IsDriver(string driver)
		{
			if(string.IsNullOrWhiteSpace(driver))
				return string.IsNullOrWhiteSpace(this.Driver);

			return string.IsNullOrWhiteSpace(this.Driver) || string.Equals(this.Driver, driver, StringComparison.OrdinalIgnoreCase);
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
		public bool Equals(IConnectionSetting setting) => setting != null &&
			string.Equals(this.Name, setting.Name, StringComparison.OrdinalIgnoreCase) &&
			string.Equals(this.Driver, setting.Driver, StringComparison.OrdinalIgnoreCase);

		public bool Equals(ConnectionSetting setting) => setting != null &&
			string.Equals(this.Name, setting.Name, StringComparison.OrdinalIgnoreCase) &&
			string.Equals(this.Driver, setting.Driver, StringComparison.OrdinalIgnoreCase);

		public override bool Equals(object obj) => obj is IConnectionSetting setting && this.Equals(setting);
		public override int GetHashCode() => HashCode.Combine(this.Name.ToLowerInvariant(), this.Driver.ToLowerInvariant());
		public override string ToString()
		{
			return string.IsNullOrEmpty(this.Driver) ?
				$"[{this.Name}]{this.Value}" :
				$"[{this.Name}@{this.Driver}]{this.Value}";
		}
		#endregion

		#region 嵌套子类
		private class ConnectionSettingValues : IConnectionSettingValues
		{
			#region 成员字段
			private readonly IConnectionSetting _connectionSetting;
			private readonly Dictionary<string, string> _dictionary;
			#endregion

			#region 构造函数
			public ConnectionSettingValues(IConnectionSetting connectionSetting)
			{
				_connectionSetting = connectionSetting;
				_dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			}
			#endregion

			#region 通用属性
			public int Count => _dictionary.Count;

			public string this[string key]
			{
				get => this.GetValue(key);
				set => this.SetValue(key, value);
			}

			public IEnumerable<KeyValuePair<string, string>> Mapping
			{
				get
				{
					if(_mappers.TryGetValue(_connectionSetting.Driver, out var mapper))
					{
						foreach(var entry in _dictionary)
							yield return new KeyValuePair<string, string>(mapper.Map(entry.Key), entry.Value);
					}
					else
					{
						foreach(var entry in _dictionary)
							yield return entry;
					}
				}
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
				get => this.GetValue(nameof(Application));
				set => this.SetValue(nameof(Application), value);
			}
			#endregion

			#region 公共方法
			public void Clear() => _dictionary.Clear();
			public bool Contains(string key) => _dictionary.ContainsKey(GetKey(key));
			public bool Remove(string key) => _dictionary.Remove(GetKey(key));
			public bool Remove(string key, out string value) => _dictionary.Remove(GetKey(key), out value);

			public string GetValue(string name)
			{
				if(_mappers.TryGetValue(_connectionSetting.Driver, out var mapper))
					return mapper.GetValue(name, _dictionary);

				return _dictionary.TryGetValue(name, out var value) ? value : null;
			}

			public T GetValue<T>(string name, T defaultValue = default)
			{
				string value;

				if(_connectionSetting.Driver != null && _mappers.TryGetValue(_connectionSetting.Driver, out var mapper))
				{
					value = mapper.GetValue(name, _dictionary);
					return Zongsoft.Common.Convert.ConvertValue<T>(value, defaultValue);
				}

				if(_dictionary.TryGetValue(name, out value))
					return Zongsoft.Common.Convert.ConvertValue<T>(value, defaultValue);

				return defaultValue;
			}

			public bool SetValue(string name, string value)
			{
				if(_connectionSetting.Driver != null && _mappers.TryGetValue(_connectionSetting.Driver, out var mapper))
				{
					if(!mapper.Validate(name, value))
						return false;
				}

				_dictionary[name] = value;
				return true;
			}
			#endregion

			#region 重写方法
			public override string ToString()
			{
				var dictionary = _dictionary;
				return dictionary == null || dictionary.Count == 0 ? string.Empty : string.Join(';', dictionary.Select(entry => $"{entry.Key}={entry.Value}"));
			}
			#endregion

			#region 私有方法
			[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
			private string GetKey(string name) => _mappers.TryGetValue(_connectionSetting.Driver, out var mapper) ? mapper.Map(name) : name;
			#endregion

			#region 枚举遍历
			public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _dictionary.GetEnumerator();
			IEnumerator IEnumerable.GetEnumerator() => _dictionary.GetEnumerator();
			#endregion
		}

		private class ConnectionSettingValuesMapperCollection : ICollection<IConnectionSettingValuesMapper>
		{
			#region 公共属性
			public int Count => _mappers.Count;
			public bool IsReadOnly => false;
			#endregion

			#region 公共方法
			public void Clear() => _mappers.Clear();
			public bool Contains(IConnectionSettingValuesMapper item) => item != null && item.Driver != null && _mappers.ContainsKey(item.Driver);
			public bool Remove(IConnectionSettingValuesMapper item) => item != null && item.Driver != null && _mappers.Remove(item.Driver);
			public void Add(IConnectionSettingValuesMapper item)
			{
				if(item != null && item.Driver != null)
					_mappers.TryAdd(item.Driver, item);
			}

			public void CopyTo(IConnectionSettingValuesMapper[] array, int arrayIndex)
			{
				if(array == null || arrayIndex >= array.Length - 1)
					return;

				var iterator = _mappers.GetEnumerator();

				for(int i = 0; i < array.Length - arrayIndex; i++)
				{
					while(iterator.MoveNext())
						array[arrayIndex + i] = iterator.Current.Value;
				}
			}
			#endregion

			#region 枚举遍历
			public IEnumerator<IConnectionSettingValuesMapper> GetEnumerator()
			{
				foreach(var mapper in _mappers)
					yield return mapper.Value;
			}

			IEnumerator IEnumerable.GetEnumerator() => _mappers.Values.GetEnumerator();
			#endregion
		}
		#endregion
	}
}
