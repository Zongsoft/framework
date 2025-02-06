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
	#region 构造函数
	internal ConnectionSettingsDriver()
	{
		this.Name = string.Empty;
		this.Descriptors = new(this);

		var properties = typeof(TSettings).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
		for(int i = 0; i < properties.Length; i++)
			this.Descriptors.Add(properties[i]);
	}

	protected ConnectionSettingsDriver(string name, string description = null)
	{
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		this.Name = name;
		this.Description = description;
		this.Descriptors = new(this);

		var properties = typeof(TSettings).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
		for(int i = 0; i < properties.Length; i++)
			this.Descriptors.Add(properties[i]);

	}
	#endregion

	#region 公共属性
	public string Name { get; }
	public string Description { get; }
	public ConnectionSettingDescriptorCollection Descriptors { get; }
	#endregion

	#region 公共方法
	public virtual TSettings GetSettings(string connectionString) => (TSettings)Activator.CreateInstance(typeof(TSettings), this, connectionString);
	public virtual TSettings GetSettings(string name, string connectionString) => (TSettings)Activator.CreateInstance(typeof(TSettings), this, name, connectionString);
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
	#endregion
}