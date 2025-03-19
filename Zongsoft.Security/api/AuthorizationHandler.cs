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
using Microsoft.AspNetCore.Authentication;

using Zongsoft.Web;
using Zongsoft.Web.Http;
using Zongsoft.Services;
using Zongsoft.Collections;

namespace Zongsoft.Security.Privileges.Web;

[Service<IAuthorizationHandler>]
public class AuthorizationHandler : IAuthorizationHandler
{
	public Task HandleAsync(AuthorizationHandlerContext context)
	{
		if(context.User.IsAnonymous())
			return Task.CompletedTask;

		var authorizer = GetAuthorizer(context.User.Identity);

		if(authorizer != null && context.Resource is HttpContext http)
		{
			var parameters = new Parameters(http.Request.GetParameters());
		}

		return Task.CompletedTask;
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
}
