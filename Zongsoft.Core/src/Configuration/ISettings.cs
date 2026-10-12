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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
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

/// <summary>表示具有名称和完整文本表示的字符串键值设置。</summary>
/// <remarks>
///		<para>字典键忽略大小写，字典值区分大小写。字典成员仅访问显式主参数，不包含连接设置的扩展属性或描述器默认值。</para>
///		<para>索引器读取缺失键时返回 <see langword="null"/>，写入 <see langword="null"/> 或空字符串时删除对应主参数。字典的添加、修改、删除及清空操作均反映到 <see cref="Value"/>。</para>
///		<para>普通设置（<see cref="Settings"/>）按名称和键值内容判断相等；连接设置（<see cref="IConnectionSettings"/>）还比较驱动名称及扩展属性。</para>
/// </remarks>
public interface ISettings : IDictionary<string, string>, IEquatable<ISettings>
{
	/// <summary>获取设置的名称。</summary>
	/// <value>设置名称；未指定名称时为空字符串。</value>
	/// <remarks>名称比较忽略大小写，名称不属于主字典中的键值项。</remarks>
	string Name { get; }

	/// <summary>获取或设置包含所有显式参数的完整设置文本。</summary>
	/// <value>以分号分隔的键值文本；连接设置还包含扩展属性，但不补入描述器默认值。</value>
	/// <remarks>
	/// <para>赋值会重新解析并替换全部主参数及扩展属性；赋 <see langword="null"/> 或空字符串会清空全部参数。</para>
	/// <para>通过字典修改参数后，读取此属性会生成反映当前状态的文本，并对特殊字符进行转义。
	/// 输出顺序、键名大小写及末尾分号不属于格式保证。此文本不保证可直接作为第三方提供程序的原生连接字符串。</para>
	/// </remarks>
	string Value { get; set; }

	/// <summary>获取主字典是否为空。</summary>
	/// <value>没有显式主参数时为 <see langword="true"/>，否则为 <see langword="false"/>。</value>
	/// <remarks>此属性不统计连接设置的扩展属性，也不考虑描述器默认值；扩展属性非空时，此属性仍可能为 <see langword="true"/>。</remarks>
	bool IsEmpty { get; }

	/// <summary>获取主字典是否包含显式参数。</summary>
	/// <value>存在显式主参数时为 <see langword="true"/>，否则为 <see langword="false"/>；其值与 <see cref="IsEmpty"/> 相反。</value>
	/// <remarks>此属性不依据 <see cref="Value"/> 的文本长度判断，也不统计连接设置的扩展属性或描述器默认值。</remarks>
	bool HasValue { get; }
}
