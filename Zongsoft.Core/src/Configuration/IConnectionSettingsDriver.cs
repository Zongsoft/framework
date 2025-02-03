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

/// <summary>
/// 表示连接设置驱动器的基础接口。
/// </summary>
public interface IConnectionSettingsDriver : IEquatable<IConnectionSettingsDriver>
{
	#region 静态属性
	static IConnectionSettingsDriver Instance { get; }
	#endregion

	#region 实例属性
	/// <summary>获取驱动名称。</summary>
	string Name { get; }
	/// <summary>获取驱动的描述信息。</summary>
	string Description { get; }
	/// <summary>获取连接设置项描述器集合。</summary>
	ConnectionSettingDescriptorCollection Descriptors { get; }
	#endregion

	#region 默认实现
	bool IsDriver(string name)
	{
		if(string.IsNullOrEmpty(name))
			return string.IsNullOrEmpty(this.Name);

		return string.Equals(this.Name, name, StringComparison.OrdinalIgnoreCase) || this.Name.StartsWith($".{name}", StringComparison.OrdinalIgnoreCase);
	}
	#endregion
}

/// <summary>
/// 表示连接设置驱动器的接口。
/// </summary>
public interface IConnectionSettingsDriver<out TSettings> : IConnectionSettingsDriver where TSettings : IConnectionSettings
{
	#region 方法定义
	/// <summary>构建指定连接字符串对应的连接设置对象。</summary>
	/// <param name="connectionString">指定的连接字符串。</param>
	/// <returns>返回构建的连接设置。</returns>
	TSettings GetSettings(string connectionString);

	/// <summary>构建指定连接字符串对应的连接设置对象。</summary>
	/// <param name="name">指定的连接设置名称。</param>
	/// <param name="connectionString">指定的连接字符串。</param>
	/// <returns>返回构建的连接设置。</returns>
	TSettings GetSettings(string name, string connectionString);
	#endregion
}
