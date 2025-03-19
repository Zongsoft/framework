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

using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;

using Zongsoft.Web;
using Zongsoft.Web.Http;
using Zongsoft.Services;
using Zongsoft.Collections;

namespace Zongsoft.Security.Privileges.Web;

[Service<IAuthorizationPolicyProvider>]
public class AuthorizationPolicyProvider : IAuthorizationPolicyProvider
{
	public AuthorizationPolicyProvider(IOptions<AuthorizationOptions> options)
	{
		this.FallbackPolicyProvider = new(options);
	}

	public DefaultAuthorizationPolicyProvider FallbackPolicyProvider { get; }

	public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
	{
		var accessor = ApplicationContext.Current?.Services.Resolve<IHttpContextAccessor>();
		if(accessor != null && accessor.HttpContext != null)
			accessor.HttpContext.Request.RouteValues.TryGetValue("controller", out var value);

		return this.FallbackPolicyProvider.GetDefaultPolicyAsync();
	}

	public Task<AuthorizationPolicy> GetFallbackPolicyAsync() => this.FallbackPolicyProvider.GetFallbackPolicyAsync();
	public Task<AuthorizationPolicy> GetPolicyAsync(string policyName) => this.FallbackPolicyProvider.GetPolicyAsync(policyName);
}
