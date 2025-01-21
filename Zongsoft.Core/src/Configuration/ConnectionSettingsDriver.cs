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
using System.Collections.Generic;

namespace Zongsoft.Configuration;

public partial class ConnectionSettingsDriver<TDescriptors> : IConnectionSettingsDriver<TDescriptors> where TDescriptors : ConnectionSettingDescriptorCollection, new()
{
	#region 静态构造
	static ConnectionSettingsDriver() => Descriptors = new();
	#endregion

	#region 静态属性
	public static TDescriptors Descriptors { get; }
	#endregion

	#region 构造函数
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
	protected MapperBase Mapper { get; init; }
	#endregion

	#region 公共方法
	public ConnectionSettings Create(string connectionString) => new(connectionString, this);
	object IConnectionSettingsDriver.GetOptions(IConnectionSettings settings) => settings;
	public bool TryGetValue(string name, IDictionary<object, string> values, out object value) => this.Mapper.Map(name, values, out value);
	public T GetValue<T>(string name, IDictionary<object, string> values, T defaultValue) => this.Mapper.Map(name, values, out var value) && Common.Convert.TryConvertValue<T>(value, out var result) ? result : defaultValue;
	public bool SetValue<T>(string name, T value, IDictionary<object, string> values)
	{
		var text = this.Mapper.Map(name, value, values);

		if(string.IsNullOrEmpty(text))
			return values.Remove(name);

		values[name] = text;
		return true;
	}
	#endregion

	#region 重写方法
	public bool Equals(IConnectionSettingsDriver other) => other != null && string.Equals(this.Name, other.Name, StringComparison.OrdinalIgnoreCase);
	public override bool Equals(object obj) => obj is IConnectionSettingsDriver other && this.Equals(other);
	public override int GetHashCode() => string.IsNullOrEmpty(this.Name) ? 0 : this.Name.ToUpperInvariant().GetHashCode();
	public override string ToString() => this.Name;
	#endregion
}

public partial class ConnectionSettingsDriver<TOptions, TDescriptors> : ConnectionSettingsDriver<TDescriptors>, IConnectionSettingsDriver<TOptions, TDescriptors> where TDescriptors : ConnectionSettingDescriptorCollection, new()
{
	#region 构造函数
	protected ConnectionSettingsDriver(string name, string description = null) : base(name, description) { }
	#endregion

	#region 保护属性
	protected PopulatorBase Populator { get; init; }
	#endregion

	#region 公共方法
	object IConnectionSettingsDriver.GetOptions(IConnectionSettings settings) => this.GetOptions(settings);
	public TOptions GetOptions(IConnectionSettings settings) => this.Populator.Populate(settings);
	public TOptions GetOptions(string connectionString) => this.Populator.Populate(this.Create(connectionString));
	#endregion
}

public class ConnectionSettingsDriver : IConnectionSettingsDriver, IEquatable<IConnectionSettingsDriver>
{
	#region 单例字段
	public static readonly IConnectionSettingsDriver Default = new ConnectionSettingsDriver();
	#endregion

	#region 私有构造
	private ConnectionSettingsDriver() { }
	#endregion

	#region 公共属性
	public string Name => string.Empty;
	public string Description => null;
	#endregion

	#region 公共方法
	public object GetOptions(IConnectionSettings settings) => settings;
	public T GetValue<T>(string name, IDictionary<object, string> values, T defaultValue) => values.TryGetValue(name, out var value) ? Common.Convert.ConvertValue(value, defaultValue) : defaultValue;
	public bool TryGetValue(string name, IDictionary<object, string> values, out object value)
	{
		if(values.TryGetValue(name, out var text))
		{
			value = text;
			return true;
		}

		value = null;
		return false;
	}
	public bool SetValue<T>(string name, T value, IDictionary<object, string> values)
	{
		if(value is null)
			return values.Remove(name);

		values[name] = value.ToString();
		return true;
	}
	#endregion

	#region 显式实现
	bool IEquatable<IConnectionSettingsDriver>.Equals(IConnectionSettingsDriver other) => object.ReferenceEquals(this, other);
	#endregion
}