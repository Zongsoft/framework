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
using System.Reflection;
using System.Collections.Generic;

namespace Zongsoft.Configuration;

public partial class ConnectionSettingsDriver<TSettings> : IConnectionSettingsDriver<TSettings>, IEquatable<IConnectionSettingsDriver>, IEquatable<IConnectionSettingsDriver<TSettings>> where TSettings : IConnectionSettings
{
	#region 静态构造
	static ConnectionSettingsDriver()
	{
		Descriptors = new();

		var properties = typeof(TSettings).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
		for(int i = 0; i < properties.Length; i++)
			Descriptors.Add(properties[i]);
	}
	#endregion

	#region 静态属性
	public static ConnectionSettingDescriptorCollection Descriptors { get; }
	#endregion

	#region 构造函数
	internal ConnectionSettingsDriver() => this.Name = string.Empty;
	protected ConnectionSettingsDriver(string name, string description = null)
	{
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		this.Name = name;
		this.Description = description;
	}
	#endregion

	#region 公共属性
	public string Name { get; }
	public string Description { get; }
	#endregion

	#region 保护属性
	private MapperBase _mapper;
	protected MapperBase Mapper
	{
		get => _mapper ??= new DefaultMapper(this);
		init => _mapper = value;
	}
	#endregion

	#region 公共方法
	public virtual TSettings GetSettings(string connectionString) => (TSettings)Activator.CreateInstance(typeof(TSettings), this, connectionString);
	public virtual bool TryGetValue(string name, IDictionary<object, string> values, out object value) => this.Mapper.Map(name, values, out value);
	public virtual T GetValue<T>(string name, IDictionary<object, string> values, T defaultValue = default) => this.Mapper.Map(name, values, out var value) && Common.Convert.TryConvertValue<T>(value, out var result) ? result : defaultValue;
	public virtual bool SetValue<T>(string name, T value, IDictionary<object, string> values)
	{
		var text = this.Mapper.Map(name, value, values);

		if(string.IsNullOrEmpty(text))
			return values.Remove(name);

		values[name] = text;
		return true;
	}
	#endregion

	#region 重写方法
	public bool Equals(IConnectionSettingsDriver other) => other is not null && string.Equals(this.Name, other.Name, StringComparison.OrdinalIgnoreCase);
	public bool Equals(IConnectionSettingsDriver<TSettings> other) => other is not null && string.Equals(this.Name, other.Name, StringComparison.OrdinalIgnoreCase);
	public override bool Equals(object obj) => obj is IConnectionSettingsDriver<TSettings> other && this.Equals(other);
	public override int GetHashCode() => string.IsNullOrEmpty(this.Name) ? 0 : this.Name.ToUpperInvariant().GetHashCode();
	public override string ToString() => this.Name;
	#endregion
}

public class ConnectionSettingsDriver : ConnectionSettingsDriver<ConnectionSettings>, IConnectionSettingsDriver
{
	#region 单例字段
	public static readonly ConnectionSettingsDriver Default = new();
	#endregion

	#region 私有构造
	private ConnectionSettingsDriver() { }
	#endregion

	#region 重写方法
	bool IConnectionSettingsDriver.IsDriver(string name) => string.IsNullOrEmpty(name);
	public override ConnectionSettings GetSettings(string connectionString) => new(connectionString);
	public override bool TryGetValue(string name, IDictionary<object, string> values, out object value)
	{
		if(values.TryGetValue(name, out var text))
		{
			value = text;
			return true;
		}

		value = null;
		return false;
	}
	public override T GetValue<T>(string name, IDictionary<object, string> values, T defaultValue)
	{
		return values.TryGetValue(name, out var text) ? Common.Convert.ConvertValue(text, defaultValue) : defaultValue;
	}
	public override bool SetValue<T>(string name, T value, IDictionary<object, string> values)
	{
		if(value is null)
			return values.Remove(name);

		if(Common.Convert.TryConvertValue<string>(value, out var text))
		{
			values[name] = text;
			return true;
		}

		return false;
	}
	#endregion
}