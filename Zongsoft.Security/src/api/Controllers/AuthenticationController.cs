/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
 *
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

using Zongsoft.Web;
using Zongsoft.Services;
using Zongsoft.Security.Membership;

namespace Zongsoft.Security.Web.Controllers
{
	[ApiController]
	[Area(Modules.Security)]
	[Route("{area}/{controller}/{action}")]
	public class AuthenticationController : ControllerBase
	{
		#region 公共属性
		[ServiceDependency]
		public Authenticator Authenticator { get; set; }

		[ServiceDependency]
		public ISecretor Secretor { get; set; }
		#endregion

		#region 公共方法
		[HttpPost("{scheme?}/{token?}")]
		public IActionResult Signin(string scheme, string token, [FromQuery]string scenario)
		{
			if(string.IsNullOrWhiteSpace(scenario))
				return this.BadRequest();
			if(this.Request.ContentLength == null || this.Request.ContentLength == 0)
				return this.BadRequest();

			static IDictionary<string, object> GetParameters(Microsoft.AspNetCore.Http.IQueryCollection query)
			{
				if(query == null || query.Count == 0)
					return null;

				var parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

				foreach(var entry in query)
					parameters.Add(entry.Key, entry.Value.ToString());

				return parameters;
			}

			var feature = this.HttpContext.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpBodyControlFeature>();
			if(feature != null)
				feature.AllowSynchronousIO = true;

			var result = this.Authenticator.Authenticate(scheme, token, this.Request.Body, scenario, GetParameters(this.Request.Query));

			return result.Succeed ?
				this.Ok(this.Transform(result.Value)) :
				this.StatusCode(403, new { result.Reason, result.Message });
		}

		[HttpPost]
		[Authorize]
		public void Signout()
		{
			if(this.User is CredentialPrincipal credential)
				this.Authenticator.Authority.Unregister(credential.CredentialId);
		}

		[Authorize]
		[HttpPost("{id:required}")]
		public Task<IActionResult> Renew(string id)
		{
			if(string.IsNullOrWhiteSpace(id))
				return Task.FromResult((IActionResult)this.BadRequest());

			if(this.User is CredentialPrincipal credential)
			{
				var principal = this.Authenticator.Authority.Renew(credential.CredentialId, id);

				return principal == null ?
					Task.FromResult((IActionResult)this.NotFound()) :
					Task.FromResult((IActionResult)this.Ok(this.Transform(principal)));
			}

			return Task.FromResult((IActionResult)this.Unauthorized());
		}

		[HttpPost("{destination}")]
		public IActionResult Secret(string destination, [FromQuery]string channel)
		{
			if(string.IsNullOrEmpty(destination))
				return this.BadRequest();

			return this.Content(this.Secretor.Transmitter.Transmit(destination, "Authentication", channel, destination));
		}
		#endregion

		#region 私有方法
		private object Transform(System.Security.Claims.ClaimsPrincipal principal)
		{
			return (this.Authenticator.Transformer ?? ClaimsPrincipalTransformer.Default).Transform(principal);
		}
		#endregion
	}
}
