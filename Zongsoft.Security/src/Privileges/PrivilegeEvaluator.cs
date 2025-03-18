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
using Zongsoft.Collections;

using Zongsoft.Security.Privileges.Models;

namespace Zongsoft.Security.Privileges;

[Service<IPrivilegeEvaluator>]
public class PrivilegeEvaluator : PrivilegeEvaluatorBase
{
	protected override async IAsyncEnumerable<IPrivilegeEvaluatorResult> OnEvaluateAsync(Context context, [System.Runtime.CompilerServices.EnumeratorCancellation]CancellationToken cancellation)
	{
		if(!TryGetMember(context.Identifier, out var memberId, out var memberType))
			yield break;

		var members = Module.Current.Services.ResolveRequired<IMemberService<IRole, IMember<IRole>>>();
		var ancestors = members.GetAncestorsAsync(Member.Create(memberType, memberId), -1, cancellation);

		await foreach(var ancestor in ancestors)
		{
			//依次获取每层上级角色的授权集
			var privileges = Module.Current.Accessor.SelectAsync<PrivilegeModel>(
				Condition.In(nameof(PrivilegeModel.MemberId), ancestor) &
				Condition.Equal(nameof(PrivilegeModel.MemberType), MemberType.Role),
				cancellation);

			context.Statements.Add(new StatementCollection(privileges.Synchronize(cancellation).Select(privilege => new Statement(privilege.PrivilegeName, privilege.PrivilegeMode))));
		}

		//获取当前成员的授权集
		var currents = Module.Current.Accessor.SelectAsync<PrivilegeModel>(
			Condition.Equal(nameof(PrivilegeModel.MemberId), memberId) &
			Condition.Equal(nameof(PrivilegeModel.MemberType), memberType),
			cancellation);

		context.Statements.Add(new StatementCollection(currents.Synchronize(cancellation).Select(privilege => new Statement(privilege.PrivilegeName, privilege.PrivilegeMode))));

		await foreach(var result in base.OnEvaluateAsync(context, cancellation))
			yield return result;
	}

	private static bool TryGetMember(Identifier identifier, out uint memberId, out MemberType memberType)
	{
		if(identifier.IsUser(out memberId))
		{
			memberType = MemberType.User;
			return true;
		}

		if(identifier.IsRole(out memberId))
		{
			memberType = MemberType.Role;
			return true;
		}

		memberId = 0;
		memberType = 0;
		return false;
	}
}
