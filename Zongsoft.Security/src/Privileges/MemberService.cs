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
 * This file is part of Zongsoft.Security library.
 *
 * The Zongsoft.Security is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Security is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Security library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Data;
using Zongsoft.Services;
using Zongsoft.Components;

using Zongsoft.Security.Privileges.Models;

namespace Zongsoft.Security.Privileges;

[Service<IMemberService<IRole, IMember<IRole>>>]
public class MemberService : MemberServiceBase<RoleModel, MemberModel>, IMemberService<IRole, IMember<IRole>>
{
	#region 重写方法
	protected override IDataAccess Accessor => Module.Current.Accessor;
	protected override MemberModel Create(Identifier roleId, Member member) => Model.Build<MemberModel>(model =>
	{
		model.RoleId = (uint)roleId;
		model.MemberId = (uint)member.MemberId;
		model.MemberType = member.MemberType;
	});
	#endregion

	#region 显式实现
	IAsyncEnumerable<IRole> IMemberService<IRole, IMember<IRole>>.GetAncestorsAsync(Member member, CancellationToken cancellation) => this.GetAncestorsAsync(member, cancellation);
	IAsyncEnumerable<IRole> IMemberService<IRole, IMember<IRole>>.GetRolesAsync(Member member, CancellationToken cancellation) => this.GetRolesAsync(member, cancellation);
	IAsyncEnumerable<IMember<IRole>> IMemberService<IRole, IMember<IRole>>.GetAsync(Identifier role, CancellationToken cancellation) => this.GetAsync(role, cancellation);
	IAsyncEnumerable<IMember<IRole>> IMemberService<IRole, IMember<IRole>>.GetAsync(Identifier role, string schema, CancellationToken cancellation) => this.GetAsync(role, schema, cancellation);
	ValueTask<int> IMemberService<IRole, IMember<IRole>>.SetAsync(IEnumerable<IMember<IRole>> members, CancellationToken cancellation) => this.SetAsync(members.Cast<MemberModel>(), cancellation);
	ValueTask<int> IMemberService<IRole, IMember<IRole>>.RemoveAsync(IEnumerable<IMember<IRole>> members, CancellationToken cancellation) => this.RemoveAsync(members.Cast<MemberModel>(), cancellation);
	#endregion
}
