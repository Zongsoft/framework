﻿/*
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
using System.Collections.Generic;

using Zongsoft.Data;
using Zongsoft.Components;

namespace Zongsoft.Security.Privileges;

/// <summary>
/// 提供角色成员服务的接口。
/// </summary>
public interface IMemberService<TRole, TMember> : IMemberService where TRole : IRole where TMember : IMember<TRole>
{
	/// <summary>获取指定成员的所有祖先角色集。</summary>
	/// <param name="member">指定的成员标识。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回的所有祖先角色集。</returns>
	new IAsyncEnumerable<TRole> GetAncestorsAsync(Member member, CancellationToken cancellation = default);

	/// <summary>获取指定成员的父级角色集。</summary>
	/// <param name="member">指定的成员标识。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回的上级角色集。</returns>
	new IAsyncEnumerable<TRole> GetParentsAsync(Member member, CancellationToken cancellation = default);

	/// <summary>获取指定角色的直属成员集。</summary>
	/// <param name="role">指定的角色标识。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回隶属于指定角色的直属子级成员集。</returns>
	new IAsyncEnumerable<TMember> GetAsync(Identifier role, CancellationToken cancellation = default);

	/// <summary>获取指定角色的直属成员集。</summary>
	/// <param name="role">指定的角色标识。</param>
	/// <param name="schema">指定的数据模式表达式文本。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回隶属于指定角色的直属子级成员集。</returns>
	new IAsyncEnumerable<TMember> GetAsync(Identifier role, string schema, CancellationToken cancellation = default);

	/// <summary>新增或更新指定角色下的成员。</summary>
	/// <param name="members">要更新的角色成员集。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>如果更新成功则返回更新的数量，否则返回零。</returns>
	ValueTask<int> SetAsync(IEnumerable<TMember> members, CancellationToken cancellation = default);

	/// <summary>删除多个角色成员。</summary>
	/// <param name="members">要删除的角色成员集。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>如果删除成功则返回删除的数量，否则返回零。</returns>
	ValueTask<int> RemoveAsync(IEnumerable<TMember> members, CancellationToken cancellation = default);
}
