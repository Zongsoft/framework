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
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
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

namespace Zongsoft.Common;

/// <summary>提供按变量名称、命名空间及回退设置查询原始变量值的契约。</summary>
/// <remarks>
/// 	<para>名称与命名空间通常按 <see cref="StringComparer.OrdinalIgnoreCase"/> 比较；环境变量视图遵循操作系统的名称比较规则。</para>
/// 	<para>命名空间为 <see langword="null"/> 或空字符串时均表示全局命名空间。不带回退参数的重载等价于 <c>fallback=false</c>。</para>
/// 	<para>允许回退时，从指定命名空间逐级向上查询，例如 <c>A.B.C</c>、<c>A.B</c>、<c>A</c>、全局；全局仍未找到时，实现可提供自身已声明的默认值。回退不扩大变量视图的数据范围。</para>
/// 	<para>命名空间只用于限定名称，没有保留的查询模式名称。多个来源的组合查询由 <see cref="VariablesExtension"/> 按命名空间层级及来源顺序执行，最后才允许来源自身的默认值。</para>
/// 	<para>查询成功由布尔返回值表示；存在且值为 <see langword="null"/>、空字符串、<see langword="false"/> 或零的变量均属于成功并停止回退。</para>
/// 	<para>返回未经转换的原始值，不负责模板解析、成员访问或格式化。变量不存在返回 <see langword="false"/>；数据访问等异常向调用方传播。</para>
/// 	<para>实现负责来源的访问和并发策略；本接口不要求多次查询期间的值保持不变，也不承诺跨来源的原子快照。</para>
/// </remarks>
public interface IVariables
{
	/// <summary>尝试获取全局命名空间中的变量，不允许回退。</summary>
	/// <param name="name">变量名称，不包含命名空间限定部分。</param>
	/// <param name="value">成功时返回原始值，可以为 <see langword="null"/>；失败时不得使用此值。</param>
	/// <returns>找到变量返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
	/// <exception cref="ArgumentNullException"><paramref name="name"/> 为 <see langword="null"/>。</exception>
	bool TryGetValue(string name, out object value);

	/// <summary>尝试获取全局命名空间中的变量，并明确是否允许来源自身的默认值。</summary>
	/// <param name="name">变量名称，不包含命名空间限定部分。</param>
	/// <param name="fallback">是否在普通变量不存在时查询来源已声明的默认值；不以值是否为空判断回退。</param>
	/// <param name="value">成功时返回原始值，可以为 <see langword="null"/>；失败时不得使用此值。</param>
	/// <returns>找到变量或允许的默认值返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
	/// <exception cref="ArgumentNullException"><paramref name="name"/> 为 <see langword="null"/>。</exception>
	bool TryGetValue(string name, bool fallback, out object value);

	/// <summary>尝试获取指定命名空间中的变量，不允许回退。</summary>
	/// <param name="namespace">命名空间，<see langword="null"/> 或空字符串表示全局命名空间。</param>
	/// <param name="name">变量名称，不裁剪、转换或再次拆分命名空间。</param>
	/// <param name="value">成功时返回原始值，可以为 <see langword="null"/>；失败时不得使用此值。</param>
	/// <returns>在指定命名空间中找到变量返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
	/// <exception cref="ArgumentNullException"><paramref name="name"/> 为 <see langword="null"/>。</exception>
	bool TryGetValue(string @namespace, string name, out object value);

	/// <summary>尝试获取指定命名空间中的变量，并明确是否允许回退。</summary>
	/// <param name="namespace">命名空间，<see langword="null"/> 或空字符串表示全局命名空间；层级以点号分隔。</param>
	/// <param name="name">变量名称，不裁剪、转换或再次拆分命名空间。</param>
	/// <param name="fallback">为 <see langword="true"/> 时依次查询指定命名空间、各级父命名空间、全局命名空间及来源自身已声明的默认值；为 <see langword="false"/> 时仅查询指定命名空间。</param>
	/// <param name="value">成功时返回原始值，可以为 <see langword="null"/>；失败时不得使用此值。</param>
	/// <returns>找到变量或允许的默认值返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
	/// <remarks>
	/// 	<para>回退只在查询失败时进行，找到 <see langword="null"/>、空字符串、<see langword="false"/> 或零时立即结束；实现不得把类型的零值当作未声明的默认值。</para>
	/// 	<para>只提供全局变量的来源在允许回退时可直接查询全局值；没有自身默认值的来源在全局未找到后返回 <see langword="false"/>。</para>
	/// </remarks>
	/// <exception cref="ArgumentNullException"><paramref name="name"/> 为 <see langword="null"/>。</exception>
	bool TryGetValue(string @namespace, string name, bool fallback, out object value);
}
