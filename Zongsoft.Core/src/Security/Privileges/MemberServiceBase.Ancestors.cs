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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Data;
using Zongsoft.Components;
using Zongsoft.Collections;

namespace Zongsoft.Security.Privileges;

partial class MemberServiceBase<TRole, TMember>
{
	#region 公共方法
	public async IAsyncEnumerable<TRole> GetAncestorsAsync(Member member, [System.Runtime.CompilerServices.EnumeratorCancellation]CancellationToken cancellation = default)
	{
		var members = new Stack<Identifier>();
		var result = new HashSet<TRole>(RoleComparer.Instance);

		//获取指定成员的父角色集
		var parents = this.GetParentsAsync(member, cancellation);

		//依次将父角色加入到结果集和待查成员集
		await foreach(var parnet in parents)
		{
			if(parnet != null && result.Add(parnet))
				members.Push(parnet.Identifier);
		}

		while(members.TryPop(out var child))
		{
			//获取待查成员的父角色集
			parents = this.GetParentsAsync(Member.Role(child), cancellation);

			//依次将父角色加入到结果集和待查成员集
			await foreach(var parnet in parents)
			{
				if(parnet != null && result.Add(parnet))
					members.Push(parnet.Identifier);
			}
		}

		foreach(var role in result)
			yield return role;
	}

	public async IAsyncEnumerable<ICollection<Identifier>> GetAncestorsAsync(Member member, int depth, [System.Runtime.CompilerServices.EnumeratorCancellation]CancellationToken cancellation = default)
	{
		var result = new Stack<HashSet<Identifier>>();

		//获取指定成员的父角色集
		var parents = GetParentsAsync(member, cancellation);

		//将父角色集压入结果栈
		result.Push(parents.Synchronize(cancellation).ToHashSet());

		//如果结果集为空则返回
		if(result.Count == 0)
			yield break;

		//将结果集合并到去重集（注：最大层级数为1时，不需要去重处理）
		var distinction = depth == 1 ? null : new HashSet<Identifier>(result.Peek());

		//定义当前层级数
		var current = 0;

		//获取最上层的角色集
		while(result.TryPeek(out var roles))
		{
			//如果指定了最大层级数则递增当前层级数；
			//如果当前层级数已经到达了最大层级数则中断
			if(depth > 0 && ++current == depth)
				break;

			//创建当前层的角色集
			var hashset = new HashSet<Identifier>();

			foreach(var role in roles)
			{
				//获取待查成员的父角色集
				parents = GetParentsAsync(Member.Role(role), cancellation);

				//依次将待查成员父角色加入到当前层集和去重集
				await foreach(var parent in parents)
				{
					if(distinction.Add(parent))
						hashset.Add(parent);
				}
			}

			//将当前层压入结果栈
			if(hashset.Count > 0)
				result.Push(hashset);
			else
				break;
		}

		foreach(var roles in result)
			yield return roles;

		IAsyncEnumerable<Identifier> GetParentsAsync(Member member, CancellationToken cancellation) =>
			this.Accessor.SelectAsync<object>(
				this.Name,
				this.GetCriteria(member),
				nameof(IMember<TRole>.RoleId),
				cancellation
			).Map(id => new Identifier(typeof(IRole), id));
	}
	#endregion

	#region 显式实现
	IAsyncEnumerable<IRole> IMemberService.GetAncestorsAsync(Member member, CancellationToken cancellation) => this.GetAncestorsAsync(member, cancellation).Map(role => (IRole)role);
	#endregion

	#region 嵌套子类
	private class RoleComparer : IEqualityComparer<TRole>
	{
		public static readonly RoleComparer Instance = new();

		public bool Equals(TRole x, TRole y)
		{
			if(x == null)
				return y == null;
			else
				return y != null && x.Identifier == y.Identifier;
		}

		public int GetHashCode(TRole role) => role.Identifier.GetHashCode();
	}
	#endregion
}
