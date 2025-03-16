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
using Zongsoft.Common;
using Zongsoft.Components;

namespace Zongsoft.Security.Privileges;

public abstract class MemberServiceBase<TRole, TMember> : IMemberService<TRole, TMember>
	where TRole : IRole
	where TMember : IMember<TRole>
{
	#region 公共属性
	public virtual string Name => this.Accessor.Naming.Get<TMember>();
	#endregion

	#region 保护属性
	protected abstract IDataAccess Accessor { get; }
	#endregion

	#region 公共方法
	public IAsyncEnumerable<TRole> GetAncestorsAsync(Member member, CancellationToken cancellation = default) => throw new NotImplementedException();
	public IAsyncEnumerable<TRole> GetRolesAsync(Member member, CancellationToken cancellation = default)
	{
		return this.Accessor.SelectAsync<TMember>(
			this.GetCriteria(member),
			$"*, {nameof(IMember<TRole>.Role)}" + "{*}",
			cancellation
		).Map(member => member.Role);
	}

	public ValueTask<int> SetRolesAsync(Member member, IEnumerable<Identifier> roles, CancellationToken cancellation = default)
	{
		if(roles == null)
			return this.Accessor.DeleteAsync(this.Name, this.GetCriteria(member), cancellation: cancellation);

		var members = roles.Select(role => this.Create(role, member));
		return this.Accessor.UpsertManyAsync(this.Name, members, cancellation);
	}

	public async ValueTask<bool> SetRoleAsync(Member member, Identifier role, CancellationToken cancellation = default)
	{
		return role.HasValue && await this.Accessor.UpsertAsync(this.Create(role, member), cancellation) > 0;
	}

	public IAsyncEnumerable<TMember> GetAsync(Identifier role, CancellationToken cancellation = default) => this.GetAsync(role, null, cancellation);
	public IAsyncEnumerable<TMember> GetAsync(Identifier role, string schema, CancellationToken cancellation = default)
	{
		if(role.IsEmpty)
			return Zongsoft.Collections.Enumerable.Empty<TMember>();

		var criteria = this.GetCriteria(role);
		if(criteria == null)
			return Zongsoft.Collections.Enumerable.Empty<TMember>();

		return this.Accessor.SelectAsync<TMember>(criteria, schema, cancellation);
	}

	public async ValueTask<bool> SetAsync(Identifier role, Member member, CancellationToken cancellation = default) => role.HasValue && await this.SetAsync(this.Create(role, member), cancellation);
	public async ValueTask<bool> SetAsync(TMember member, CancellationToken cancellation = default) => member != null && await this.Accessor.UpsertAsync(member, cancellation) > 0;

	public ValueTask<int> SetAsync(Identifier role, IEnumerable<Member> members, CancellationToken cancellation = default) => this.SetAsync(role, members, false, cancellation);
	public async ValueTask<int> SetAsync(Identifier role, IEnumerable<Member> members, bool shouldResetting, CancellationToken cancellation = default)
	{
		if(role.IsEmpty || members == null)
			return 0;

		var criteria = this.GetCriteria(role);
		if(criteria == null)
			return 0;

		if(shouldResetting)
			await this.Accessor.DeleteAsync(this.Name, criteria, cancellation: cancellation);

		return await this.Accessor.UpsertAsync(members.Select(member => this.Create(role, member)), cancellation);
	}

	public ValueTask<int> SetAsync(IEnumerable<TMember> members, CancellationToken cancellation = default) => this.Accessor.UpsertManyAsync(members, cancellation);

	public async ValueTask<bool> RemoveAsync(Identifier role, Member member, CancellationToken cancellation = default)
	{
		if(role.IsEmpty)
			return false;

		var criteria = this.GetCriteria(role, member);
		return criteria != null && await this.Accessor.DeleteAsync(this.Name, criteria, cancellation: cancellation) > 0;
	}

	public async ValueTask<int> RemoveAsync(Identifier role, IEnumerable<Member> members, CancellationToken cancellation = default)
	{
		if(role.IsEmpty || members == null)
			return 0;

		var count = 0;

		foreach(var member in members)
		{
			var criteria = this.GetCriteria(role, member);

			if(criteria != null)
				count += await this.Accessor.DeleteAsync(this.Name, criteria, cancellation: cancellation);
		}

		return count;
	}

	public async ValueTask<int> RemoveAsync(IEnumerable<TMember> members, CancellationToken cancellation = default)
	{
		if(members == null)
			return 0;

		var count = 0;

		foreach(var member in members)
		{
			var criteria = this.GetCriteria(member);

			if(criteria != null)
				count += await this.Accessor.DeleteAsync(this.Name, criteria, cancellation: cancellation);
		}

		return count;
	}
	#endregion

	#region 虚拟方法
	protected abstract TMember Create(Identifier role, Member member);

	protected virtual ICondition GetCriteria(Identifier role, Member member)
	{
		if(role.IsEmpty)
			return null;

		return Condition.Equal(nameof(IMember<TRole>.RoleId), role.Value) &
			Condition.Equal(nameof(IMember<TRole>.MemberId), member.MemberId) &
			Condition.Equal(nameof(IMember<TRole>.MemberType), member.MemberType);
	}

	protected virtual ICondition GetCriteria(TMember member)
	{
		if(member == null)
			return null;

		return Condition.Equal(nameof(IMember<TRole>.RoleId), member.RoleId.Value) &
			Condition.Equal(nameof(IMember<TRole>.MemberId), member.MemberId) &
			Condition.Equal(nameof(IMember<TRole>.MemberType), member.MemberType);
	}

	protected virtual ICondition GetCriteria(Identifier identifier)
	{
		if(identifier.IsEmpty)
			return null;

		if(identifier.Validate<Member>(out var member))
			return Condition.Equal(nameof(IMember<TRole>.MemberId), member.MemberId) &
			       Condition.Equal(nameof(IMember<TRole>.MemberType), member.MemberType);

		if(identifier.Validate<IRole, Identifier>(out var roleId))
			return Condition.Equal(nameof(IMember<TRole>.RoleId), roleId.Value);

		if(identifier.Validate<IRole, string>(out var qualifiedName))
		{
			var parts = qualifiedName.Split(':');

			if(parts.Length == 2)
				return Condition.Equal(nameof(IRole.Namespace), parts[0]) & Condition.Equal(nameof(IRole.Name), parts[1]);
			else
				return Condition.Equal(nameof(IRole.Name), parts[0]);
		}

		throw OperationException.Argument();
	}
	#endregion
}
