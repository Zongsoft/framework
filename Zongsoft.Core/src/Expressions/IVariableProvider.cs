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

namespace Zongsoft.Expressions;

/// <summary>提供按变量名称及可选命名空间查询原始变量值的契约。</summary>
/// <remarks>
/// 	<para>实现类负责从其数据来源中查找变量，并使用 <see cref="StringComparer.OrdinalIgnoreCase"/> 比较变量名称与命名空间。</para>
/// 	<para>
/// 		默认命名空间由 <see langword="null"/> 表示；未指定命名空间的重载与显式传入默认命名空间的重载应具有相同的查询语义。
/// 		查询指定命名空间中的变量时，不应自动回退到默认命名空间。
/// 	</para>
/// 	<para>查询结果由方法的布尔返回值表示，而不是由变量值是否为空判断。存在且值为 <see langword="null"/> 的变量仍属于查询成功。</para>
/// 	<para>
/// 		提供器返回未经格式化的原始对象值，不负责解析变量引用语法、访问对象成员或展开文本模板。
/// 		实现类负责其数据来源的访问、缓存及并发同步策略，本接口不要求变量值在多次查询之间保持不变。
/// 	</para>
/// </remarks>
public interface IVariableProvider
{
	/// <summary>尝试获取默认命名空间中指定名称的变量值。</summary>
	/// <param name="name">要查找的变量名称，不包含命名空间限定部分；名称比较应忽略大小写。</param>
	/// <param name="value">查询成功时返回变量的原始值，该值可以为 <see langword="null"/>；查询失败时，调用方不应使用此参数的值。</param>
	/// <returns>如果找到指定变量则返回 <see langword="true"/>，即使其值为 <see langword="null"/>；未找到则返回 <see langword="false"/>。</returns>
	/// <remarks>
	/// 	<para>此重载仅查询默认命名空间，等价于调用 <c>TryGetValue(null, name, out value)</c>，不遍历其它命名空间。</para>
	/// 	<para>变量不存在是正常的查询失败，不应因此抛出异常；访问数据来源等过程中发生的其它异常可以向调用方传播。</para>
	/// </remarks>
	/// <seealso cref="TryGetValue(string, string, out object)"/>
	bool TryGetValue(string name, out object value);

	/// <summary>尝试获取指定命名空间中指定名称的变量值。</summary>
	/// <param name="namespace">要查询的命名空间，传入 <see langword="null"/> 表示默认命名空间；命名空间比较应忽略大小写。</param>
	/// <param name="name">要查找的变量名称，不包含命名空间限定部分；名称比较应忽略大小写。</param>
	/// <param name="value">查询成功时返回变量的原始值，该值可以为 <see langword="null"/>；查询失败时，调用方不应使用此参数的值。</param>
	/// <returns>如果在指定命名空间中找到变量则返回 <see langword="true"/>，即使其值为 <see langword="null"/>；未找到则返回 <see langword="false"/>。</returns>
	/// <remarks>
	/// 	<para>命名空间和变量名称由两个独立参数传入，提供器不应将 <paramref name="name"/> 再次拆分为命名空间与名称。</para>
	/// 	<para>
	/// 		当 <paramref name="namespace"/> 为 <see langword="null"/> 时，查询语义与 <see cref="TryGetValue(string, out object)"/> 相同。
	/// 		指定命名空间中不存在该变量时应返回 <see langword="false"/>，不自动查询默认命名空间。
	/// 	</para>
	/// 	<para>变量不存在是正常的查询失败，不应因此抛出异常；访问数据来源等过程中发生的其它异常可以向调用方传播。</para>
	/// </remarks>
	/// <seealso cref="TryGetValue(string, out object)"/>
	bool TryGetValue(string @namespace, string name, out object value);
}
