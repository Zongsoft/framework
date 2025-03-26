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
using System.Collections.Generic;

using Zongsoft.Data;
using Zongsoft.Components;

namespace Zongsoft.Security.Privileges;

/// <summary>
/// 提供角色服务的接口。
/// </summary>
public interface IRoleService
{
	#region 通用方法
	/// <summary>启用指定的角色。</summary>
	/// <param name="identifier">指定的角色标识。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>如果启用成功则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
	ValueTask<bool> EnableAsync(Identifier identifier, CancellationToken cancellation = default);

	/// <summary>禁用指定的角色。</summary>
	/// <param name="identifier">指定的角色标识。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>如果禁用成功则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
	ValueTask<bool> DisableAsync(Identifier identifier, CancellationToken cancellation = default);

	/// <summary>确定指定的角色是否存在。</summary>
	/// <param name="identifier">指定要查找的角色标识。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>如果指定的角色是存在的则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
	ValueTask<bool> ExistsAsync(Identifier identifier, CancellationToken cancellation = default);

	/// <summary>更改角色名称。</summary>
	/// <param name="identifier">要更名的角色标识。</param>
	/// <param name="name">要更名的新名称。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>如果更名成功则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
	ValueTask<bool> RenameAsync(Identifier identifier, string name, CancellationToken cancellation = default);

	/// <summary>删除一个角色。</summary>
	/// <param name="identifier">要删除的角色标识。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>如果删除成功则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
	ValueTask<bool> DeleteAsync(Identifier identifier, CancellationToken cancellation = default);

	/// <summary>删除多个角色。</summary>
	/// <param name="identifiers">要删除的角色标识集。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回删除成功的角色数量。</returns>
	ValueTask<int> DeleteAsync(IEnumerable<Identifier> identifiers, CancellationToken cancellation = default);
	#endregion

	#region 接口参数
	/// <summary>获取指定的角色对象。</summary>
	/// <param name="identifier">要查找的角色标识。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回由<paramref name="identifier"/>参数指定的角色对象，如果没有找到则返回空(<c>null</c>)。</returns>
	ValueTask<IRole> GetAsync(Identifier identifier, CancellationToken cancellation = default);

	/// <summary>获取指定的角色对象。</summary>
	/// <param name="identifier">要查找的角色标识。</param>
	/// <param name="schema">获取的数据模式。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回由<paramref name="identifier"/>参数指定的角色对象，如果没有找到则返回空(<c>null</c>)。</returns>
	ValueTask<IRole> GetAsync(Identifier identifier, string schema, CancellationToken cancellation = default);

	/// <summary>查找指定关键字的角色。</summary>
	/// <param name="keyword">指定的查找关键字。</param>
	/// <param name="schema">查找的数据模式。</param>
	/// <param name="paging">查找的分页设置。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回找到的角色结果集。</returns>
	IAsyncEnumerable<IRole> FindAsync(string keyword, string schema, Paging paging, CancellationToken cancellation = default);

	/// <summary>查找指定条件的角色。</summary>
	/// <param name="criteria">指定的查找条件。</param>
	/// <param name="schema">查找的数据模式。</param>
	/// <param name="paging">查找的分页设置。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回找到的角色结果集。</returns>
	IAsyncEnumerable<IRole> FindAsync(ICondition criteria, string schema, Paging paging, CancellationToken cancellation = default);

	/// <summary>创建一个角色。</summary>
	/// <param name="role">要创建的角色对象。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>如果创建成功则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
	ValueTask<bool> CreateAsync(IRole role, CancellationToken cancellation = default);

	/// <summary>创建多个角色。</summary>
	/// <param name="roles">要创建的角色对象集。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回创建成功的角色数量。</returns>
	ValueTask<int> CreateAsync(IEnumerable<IRole> roles, CancellationToken cancellation = default);

	/// <summary>更新角色信息。</summary>
	/// <param name="role">要更新的角色对象。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>如果更新成功则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
	ValueTask<bool> UpdateAsync(IRole role, CancellationToken cancellation = default);
	#endregion
}
