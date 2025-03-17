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

namespace Zongsoft.Security.Privileges;

partial class MemberServiceBase<TRole, TMember>
{
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
}
