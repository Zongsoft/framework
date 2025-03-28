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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.AspNetCore.Mvc;

using Zongsoft.Web;
using Zongsoft.Web.Http;
using Zongsoft.Components;
using Zongsoft.Collections;
using Zongsoft.Security.Privileges;

namespace Zongsoft.Security.Web.Controllers;

[Area(Module.NAME)]
[ControllerName]
public class AuthorizationController : ControllerBase
{
	/// <summary>获取指定授权方案的权限定义集。</summary>
	/// <param name="scheme">指定的授权方案。</param>
	/// <returns>返回的权限定义集，如果指定的授权方案不存在则返回 <c>NotFound</c> 状态码。</returns>
	[HttpGet("{scheme?}")]
	public IActionResult Get(string scheme = null)
	{
		var authorizer = string.IsNullOrEmpty(scheme) ?
			Authorization.Authorizer :
			Authorization.Authorizers.TryGetValue(scheme, out var value) ? value : null;

		if(authorizer == null)
			return this.NotFound();

		return this.Ok(new
		{
			authorizer.Privileger.Categories,
			authorizer.Privileger.Privileges,
		});
	}

	/// <summary>获取指定授权方案的最终授权集。</summary>
	/// <param name="scheme">指定的授权方案。</param>
	/// <returns>返回的最终授权集，如果指定的授权方案不存在则返回 <c>NotFound</c> 状态码。</returns>
	[ActionName("Privileges")]
	[HttpGet("[action]/{id?}")]
	[HttpGet("{scheme:required}/[action]/{id?}")]
	public IActionResult GetPrivileges(string id, string scheme = null, CancellationToken cancellation = default)
	{
		var authorizer = string.IsNullOrEmpty(scheme) ?
			Authorization.Authorizer :
			Authorization.Authorizers.TryGetValue(scheme, out var value) ? value : null;

		if(authorizer == null)
			return this.NotFound();

		if(string.IsNullOrEmpty(id))
			return this.Ok(authorizer.Evaluator.EvaluateAsync(default, new Parameters(this.Request.GetParameters()), cancellation));

		if(!Member.TryParse(id, out var member))
			return this.BadRequest();

		return this.Ok(authorizer.Evaluator.EvaluateAsync(member, new Parameters(this.Request.GetParameters()), cancellation));
	}
}
