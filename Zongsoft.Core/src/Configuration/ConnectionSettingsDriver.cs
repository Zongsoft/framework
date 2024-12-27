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
 * Copyright (C) 2010-2024 Zongsoft Studio <http://www.zongsoft.com>
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

namespace Zongsoft.Configuration;

public class ConnectionSettingsDriver<TDescriptors> : IConnectionSettingsDriver<TDescriptors> where TDescriptors : ConnectionSettingDescriptorCollection, new()
{
	#region 单例字段
	internal static readonly IConnectionSettingsDriver Unnamed = new UnnamedDriver();
	#endregion

	#region 静态构造
	static ConnectionSettingsDriver() => Descriptors = new();
	#endregion

	#region 静态属性
	public static TDescriptors Descriptors { get; }
	#endregion

	#region 构造函数
	public ConnectionSettingsDriver(string name, string description = null) : this(name, null, null, description) { }
	public ConnectionSettingsDriver(string name, IConnectionSettingsMapper mapper, string description = null) : this(name, mapper, null, description) { }
	public ConnectionSettingsDriver(string name, IConnectionSettingsMapper mapper, IConnectionSettingsModeler modeler, string description = null)
	{
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		this.Name = name;
		this.Mapper = mapper;
		this.Modeler = modeler;
		this.Description = description;
	}
	#endregion

	#region 公共属性
	public string Name { get; }
	public string Description { get; set; }
	public IConnectionSettingsMapper Mapper { get; init; }
	public IConnectionSettingsModeler Modeler { get; init; }
	#endregion

	#region 公共方法
	public ConnectionSettings Create(string connectionString) => new(this.Name, connectionString) { Driver = this };
	public ConnectionSettings Create(string name, string connectionString) => new(name, connectionString) { Driver = this };
	#endregion

	#region 重写方法
	public bool Equals(IConnectionSettingsDriver other) => other != null && string.Equals(this.Name, other.Name, StringComparison.OrdinalIgnoreCase);
	public override bool Equals(object obj) => obj is IConnectionSettingsDriver other && this.Equals(other);
	public override int GetHashCode() => string.IsNullOrEmpty(this.Name) ? 0 : this.Name.ToUpperInvariant().GetHashCode();
	public override string ToString() => this.Name;
	#endregion

	#region 嵌套子类
	private sealed class UnnamedDriver() : ConnectionSettingsDriver<ConnectionSettingDescriptorCollection>("?") { }
	#endregion
}

public class ConnectionSettingsDriver : ConnectionSettingsDriver<ConnectionSettingDescriptorCollection>
{
	#region 构造函数
	public ConnectionSettingsDriver(string name, string description = null) : base(name, null, null, description) { }
	public ConnectionSettingsDriver(string name, IConnectionSettingsMapper mapper, string description = null) : base(name, mapper, null, description) { }
	public ConnectionSettingsDriver(string name, IConnectionSettingsMapper mapper, IConnectionSettingsModeler modeler, string description = null) : base(name, mapper, modeler, description) { }
	#endregion
}