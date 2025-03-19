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

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;

using Zongsoft.Web;
using Zongsoft.Web.Http;
using Zongsoft.Services;
using Zongsoft.Collections;

namespace Zongsoft.Security.Privileges.Web;

[Service<IAuthorizationHandler>]
public class AuthorizationHandler : IAuthorizationHandler
{
	public async Task HandleAsync(AuthorizationHandlerContext context)
	{
		if(context.User.IsAnonymous())
			return;

		var authorizer = GetAuthorizer(context.User.Identity);

		if(authorizer != null)
		{
			var parameters = context.Resource is HttpContext http ? new Parameters(http.Request.GetParameters()) : null;
			await this.AuthorizeAsync(context, authorizer, context.User.Identity as ClaimsIdentity, parameters);
		}

		foreach(var identity in context.User.Identities)
		{
			authorizer = GetAuthorizer(identity);

			if(authorizer != null)
			{
				var parameters = context.Resource is HttpContext http ? new Parameters(http.Request.GetParameters()) : null;
				await this.AuthorizeAsync(context, authorizer, identity, parameters);
			}
		}
	}

	#region 私有方法
	private async ValueTask AuthorizeAsync(AuthorizationHandlerContext context, IAuthorizer authorizer, ClaimsIdentity identity, Parameters parameters)
	{
		if(authorizer == null || identity == null)
			return;

		foreach(var requirement in context.Requirements)
		{
			if(requirement is OperationAuthorizationRequirement operation)
			{
				if(await authorizer.AuthorizeAsync(identity, operation.Name, parameters))
					context.Succeed(requirement);
				else
					context.Fail(new AuthorizationFailureReason(this, $"The '{operation.Name}' operation is not authorized."));
			}
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
