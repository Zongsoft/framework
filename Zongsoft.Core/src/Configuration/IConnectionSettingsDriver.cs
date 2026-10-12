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

/// <summary>表示提供连接参数描述与驱动识别能力的连接设置驱动器。</summary>
public interface IConnectionSettingsDriver : IEquatable<IConnectionSettingsDriver>
{
	#region 静态属性
	/// <summary>获取接口的静态驱动实例。</summary>
	/// <value>接口静态属性保存的实例，默认为 <see langword="null"/>。</value>
	/// <remarks>驱动发现读取具体驱动类型公开的同名静态字段或属性；此接口属性不会转发到具体驱动类型。</remarks>
	static IConnectionSettingsDriver Instance { get; }
	#endregion

	#region 实例属性
	/// <summary>获取驱动名称。</summary>
	/// <value>标识驱动的名称；默认的无名驱动使用空字符串。</value>
	string Name { get; }

	/// <summary>获取驱动的描述信息。</summary>
	/// <value>供展示使用的描述文本；未提供时可为 <see langword="null"/>。</value>
	string Description { get; }

	/// <summary>获取连接参数的描述器集合。</summary>
	/// <value>包含参数名称、别名、类型、默认值和转换规则的描述器集合。</value>
	/// <remarks>带选项类型约束的连接设置依据此集合区分主参数与扩展参数；标记为 <see cref="ConnectionSettingDescriptor.Ignored"/> 的参数仍属于已定义主参数。</remarks>
	ConnectionSettingDescriptorCollection Descriptors { get; }
	#endregion

	#region 默认实现
	/// <summary>判断当前驱动是否匹配指定名称。</summary>
	/// <param name="name">要匹配的驱动名称；<see langword="null"/> 或空字符串表示无名驱动。</param>
	/// <returns>当前驱动满足名称匹配规则时返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
	/// <remarks>默认实现忽略大小写，匹配完整名称或以“.”加指定名称开头的驱动名称；空名称仅匹配无名驱动。具体驱动可以提供自身的匹配规则。</remarks>
	bool IsDriver(string name)
	{
		if(string.IsNullOrEmpty(name))
			return string.IsNullOrEmpty(this.Name);

		return string.Equals(this.Name, name, StringComparison.OrdinalIgnoreCase) || this.Name.StartsWith($".{name}", StringComparison.OrdinalIgnoreCase);
	}
	#endregion
}

/// <summary>表示能够创建指定类型连接设置的驱动器。</summary>
/// <typeparam name="TSettings">由当前驱动创建的连接设置类型。</typeparam>
public interface IConnectionSettingsDriver<out TSettings> : IConnectionSettingsDriver where TSettings : IConnectionSettings
{
	#region 方法定义
	/// <summary>根据完整设置文本创建无名连接设置。</summary>
	/// <param name="connectionString">包含主参数及扩展参数的设置文本；<see langword="null"/> 或空字符串表示没有显式参数。</param>
	/// <returns>返回由当前驱动解释参数的 <typeparamref name="TSettings"/> 实例。</returns>
	/// <remarks>默认值由类型化读取或选项生成提供，不会自动成为显式参数。</remarks>
	TSettings GetSettings(string connectionString);

	/// <summary>根据名称和完整设置文本创建连接设置。</summary>
	/// <param name="name">连接设置名称；<see langword="null"/> 或空字符串表示无名连接设置。</param>
	/// <param name="connectionString">包含主参数及扩展参数的设置文本；<see langword="null"/> 或空字符串表示没有显式参数。</param>
	/// <returns>返回具有指定名称、由当前驱动解释参数的 <typeparamref name="TSettings"/> 实例。</returns>
	/// <remarks>可将已有设置的名称和完整文本传入此方法，显式重建为当前驱动对应的连接设置类型。</remarks>
	TSettings GetSettings(string name, string connectionString);
	#endregion
}
