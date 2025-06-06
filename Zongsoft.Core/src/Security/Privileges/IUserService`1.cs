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
/// 提供用户服务的接口。
/// </summary>
public interface IUserService<TUser> : IUserService where TUser : IUser
{
	/// <summary>获取指定的用户对象。</summary>
	/// <param name="identifier">要查找的用户标识。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回由<paramref name="identifier"/>参数指定的用户对象，如果没有找到则返回空(<c>null</c>)。</returns>
	new ValueTask<TUser> GetAsync(Identifier identifier, CancellationToken cancellation = default);

	/// <summary>获取指定的用户对象。</summary>
	/// <param name="identifier">要查找的用户标识。</param>
	/// <param name="schema">获取的数据模式。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回由<paramref name="identifier"/>参数指定的用户对象，如果没有找到则返回空(<c>null</c>)。</returns>
	new ValueTask<TUser> GetAsync(Identifier identifier, string schema, CancellationToken cancellation = default);

	/// <summary>查找指定关键字的用户。</summary>
	/// <param name="keyword">指定的查找关键字。</param>
	/// <param name="schema">查找的数据模式。</param>
	/// <param name="paging">查找的分页设置。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回找到的用户结果集。</returns>
	new IAsyncEnumerable<TUser> FindAsync(string keyword, string schema, Paging paging, CancellationToken cancellation = default);

	/// <summary>查找指定条件的用户。</summary>
	/// <param name="criteria">指定的查找条件。</param>
	/// <param name="schema">查找的数据模式。</param>
	/// <param name="paging">查找的分页设置。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回找到的用户结果集。</returns>
	new IAsyncEnumerable<TUser> FindAsync(ICondition criteria, string schema, Paging paging, CancellationToken cancellation = default);

	/// <summary>创建一个用户。</summary>
	/// <param name="user">要创建的用户对象。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>如果创建成功则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
	ValueTask<bool> CreateAsync(TUser user, CancellationToken cancellation = default);

	/// <summary>创建一个用户，并为其设置密码。</summary>
	/// <param name="user">要创建的用户对象。</param>
	/// <param name="password">新建用户的初始密码。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>如果创建成功则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
	ValueTask<bool> CreateAsync(TUser user, string password, CancellationToken cancellation = default);

	/// <summary>创建多个用户。</summary>
	/// <param name="users">要创建的用户对象集。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回创建成功的用户数量。</returns>
	ValueTask<int> CreateAsync(IEnumerable<TUser> users, CancellationToken cancellation = default);

	/// <summary>更新用户信息。</summary>
	/// <param name="user">要更新的用户对象。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>如果更新成功则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
	ValueTask<bool> UpdateAsync(TUser user, CancellationToken cancellation = default);
}
