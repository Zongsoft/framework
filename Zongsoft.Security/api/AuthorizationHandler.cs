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
 * This file is part of Zongsoft.Security.Web library.
 *
 * The Zongsoft.Security.Web is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Security.Web is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Security.Web library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Collections.Generic;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;

using Zongsoft.Web;
using Zongsoft.Web.Http;
using Zongsoft.Services;
using Zongsoft.Collections;

namespace Zongsoft.Security.Privileges.Web;

[Service<IAuthorizationHandler>]
public class AuthorizationHandler : AuthorizationHandler<OperationAuthorizationRequirement>
{
	#region 验证处理
	protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, OperationAuthorizationRequirement requirement)
	{
		if(context.User.IsAnonymous())
			return;

		var authorizer = GetAuthorizer(context.User.Identity);

		if(authorizer != null)
		{
			var parameters = context.Resource is HttpContext http ? new Parameters(http.Request.GetParameters()) : null;
			await this.AuthorizeAsync(context, authorizer, context.User.Identity as ClaimsIdentity, requirement, parameters);
		}

		foreach(var identity in context.User.Identities)
		{
			if(identity.Equals(context.User.Identity))
				continue;

			authorizer = GetAuthorizer(identity);

			if(authorizer != null)
			{
				var parameters = context.Resource is HttpContext http ? new Parameters(http.Request.GetParameters()) : null;
				await this.AuthorizeAsync(context, authorizer, identity, requirement, parameters);
			}
		}
	}
	#endregion

	#region 私有方法
	private async ValueTask AuthorizeAsync(AuthorizationHandlerContext context, IAuthorizer authorizer, ClaimsIdentity identity, OperationAuthorizationRequirement requirement, Parameters parameters)
	{
		if(authorizer == null || identity == null)
			return;

		//获取指定操作请求对应的多个权限定义
		var privileges = authorizer.Privileger.FindAll(requirement.Name);

		//执行授权验证（默认采用 Any 模式）
		if(await Any(privileges, authorizer, identity, parameters))
			context.Succeed(requirement);
		else
			context.Fail(new AuthorizationFailureReason(this, $"The '{requirement.Name}' operation is not authorized."));

		async ValueTask<bool> Any(IEnumerable<Privilege> privileges, IAuthorizer authorizer, ClaimsIdentity identity, Parameters parameters)
		{
			foreach(var privilege in privileges)
			{
				if(await authorizer.AuthorizeAsync(identity, privilege.Name, parameters))
					return true;
			}

			return false;
		}

		async ValueTask<bool> All(IEnumerable<Privilege> privileges, IAuthorizer authorizer, ClaimsIdentity identity, Parameters parameters)
		{
			foreach(var privilege in privileges)
			{
				if(!await authorizer.AuthorizeAsync(identity, privilege.Name, parameters))
					return false;
			}

			return true;
		}
	}

	private static IAuthorizer GetAuthorizer(System.Security.Principal.IIdentity identity)
	{
		if(identity == null || !identity.IsAuthenticated)
			return null;

		if(string.IsNullOrEmpty(identity.AuthenticationType) || string.Equals(identity.AuthenticationType, "Default", StringComparison.OrdinalIgnoreCase))
			return Authorization.Authorizers[string.Empty] ?? Authorization.Authorizers["Default"];
		else
			return Authorization.Authorizers[identity.AuthenticationType];
	}
	#endregion
}
