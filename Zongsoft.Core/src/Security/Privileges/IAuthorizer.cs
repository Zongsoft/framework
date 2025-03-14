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
using System.Threading;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Collections.Generic;

using Zongsoft.Components;
using Zongsoft.Collections;

namespace Zongsoft.Security.Privileges;

/// <summary>
/// 提供关于鉴权相关功能的接口。
/// </summary>
public interface IAuthorizer
{
	/// <summary>获取鉴权器名称。</summary>
	string Scheme { get; }

	/// <summary>获取权限定义器。</summary>
	PrivilegeCategory Privileger { get; }

	/// <summary>判断指定用户是否具有指定的授权。</summary>
	/// <param name="user">指定的用户身份。</param>
	/// <param name="privilege">指定的权限标识。</param>
	/// <param name="parameters">指定的附加参数集。</param>
	/// <param name="cancellation">异步操作的取消标记。</param>
	/// <returns>如果具有授权则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
	/// <remarks>该验证会对指定的用户所属角色逐级向上展开做授权判断，因此只需对本方法一次调用即可得知指定用户的最终授权运算结果。</remarks>
	ValueTask<bool> AuthorizeAsync(ClaimsIdentity user, string privilege, Parameters parameters, CancellationToken cancellation = default);

	/// <summary>判断指定用户或角色是否具有指定的授权。</summary>
	/// <param name="identifier">指定的用户或角色标识。</param>
	/// <param name="privilege">指定的权限标识。</param>
	/// <param name="parameters">指定的附加参数集。</param>
	/// <param name="cancellation">异步操作的取消标记。</param>
	/// <returns>如果具有授权则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
	/// <remarks>该验证会对指定的用户或角色所属角色逐级向上展开做授权判断，因此只需对本方法一次调用即可得知指定用户或角色的最终授权运算结果。</remarks>
	ValueTask<bool> AuthorizeAsync(Identifier identifier, string privilege, Parameters parameters, CancellationToken cancellation = default);

	/// <summary>获取指定用户的最终授权状态集。</summary>
	/// <param name="user">指定要获取的最终授权状态集的用户身份。</param>
	/// <param name="parameters">指定的附加参数集。</param>
	/// <param name="cancellation">异步操作的取消标记。</param>
	/// <returns>返回指定用户的最终授权状态集。</returns>
	/// <remarks>
	/// 	<para>注意：该集合仅包含了最终的已授权状态信息。</para>
	/// 	<para>该方法会对指定的用户所属角色逐级向上展开做授权判断，因此只需对本方法一次调用即可得知指定用户的最终授权运算结果。</para>
	/// </remarks>
	IAsyncEnumerable<Privilege> AuthorizesAsync(ClaimsIdentity user, Parameters parameters, CancellationToken cancellation = default);

	/// <summary>获取指定用户或角色的最终授权状态集。</summary>
	/// <param name="identifier">指定要获取的最终授权状态集的用户或角色标识。</param>
	/// <param name="parameters">指定的附加参数集。</param>
	/// <param name="cancellation">异步操作的取消标记。</param>
	/// <returns>返回指定用户或角色的最终授权状态集。</returns>
	/// <remarks>
	/// 	<para>注意：该集合仅包含了最终的已授权状态信息。</para>
	/// 	<para>该方法会对指定的用户或角色所属角色逐级向上展开做授权判断，因此只需对本方法一次调用即可得知指定用户或角色的最终授权运算结果。</para>
	/// </remarks>
	IAsyncEnumerable<Privilege> AuthorizesAsync(Identifier identifier, Parameters parameters, CancellationToken cancellation = default);
}
