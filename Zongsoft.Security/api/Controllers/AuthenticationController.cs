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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;

using Zongsoft.Web;
using Zongsoft.Services;
using Zongsoft.Security.Membership;

namespace Zongsoft.Security.Web.Controllers;

[Area(Module.NAME)]
[Route("[area]/[controller]/[action]")]
public class AuthenticationController : ControllerBase
{
	#region 常量定义
	private static readonly char[] InvalidDestinationCharacters = [',', ';', '|', '/', '\\'];
	#endregion

	#region 公共属性
	[ServiceDependency]
	public ISecretor Secretor { get; set; }
	#endregion

	#region 公共方法
	[HttpPost("{scheme?}/{key?}")]
	public async ValueTask<IActionResult> Signin(string scheme, string key, [FromQuery]string scenario)
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

		try
		{
			var principal = await Authentication.Instance.AuthenticateAsync(scheme, key, this.Request.Body, scenario, GetParameters(this.Request.Query));

			return principal != null ?
				this.Ok(this.Transform(principal)) :
				this.StatusCode(403, new { Reason = SecurityReasons.Unknown });
		}
		catch(AuthenticationException ex)
		{
			return this.StatusCode(StatusCodes.Status403Forbidden, new { ex.Reason, ex.Message });
		}
	}

	[HttpPost]
	[Authorize]
	public void Signout()
	{
		if(this.User is CredentialPrincipal credential)
			Authentication.Instance.Authority?.Unregister(credential.CredentialId);
	}

	[Authorize]
	[HttpPost("{id:required}")]
	public Task<IActionResult> Renew(string id)
	{
		if(string.IsNullOrWhiteSpace(id))
			return Task.FromResult((IActionResult)this.BadRequest());

		if(this.User is CredentialPrincipal credential)
		{
			var principal = Authentication.Instance.Authority.Renew(credential.CredentialId, id);

			return principal == null ?
				Task.FromResult((IActionResult)this.NotFound()) :
				Task.FromResult((IActionResult)this.Ok(this.Transform(principal)));
		}

		return Task.FromResult((IActionResult)this.Unauthorized());
	}

	[HttpPost("{scheme}:{destination}")]
	public async ValueTask<IActionResult> SecretAsync(string scheme, string destination, [FromQuery]string scenario, [FromQuery]string channel = null, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(scheme) || string.IsNullOrEmpty(destination))
			return this.BadRequest();

		if(destination.IndexOfAny(InvalidDestinationCharacters) >= 0)
			return this.BadRequest($"The specified destination parameter contains illegal characters.");

		var captcha = this.Request.Headers.TryGetValue(Zongsoft.Web.Http.Headers.Captcha, out var text) ? text.ToString() : null;
		var result = await this.Secretor.Transmitter.TransmitAsync(scheme, destination, "Authentication", "Singin:" + scenario, captcha, channel, destination, cancellation);
		return this.Content(result);
	}

	[HttpPost("{token}")]
	public async ValueTask<IActionResult> VerifyAsync(string token, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(token))
			return this.BadRequest();
		if(this.Request.ContentLength < 1)
			return this.BadRequest();

		string secret;

		using(var reader = new System.IO.StreamReader(this.Request.Body))
		{
			secret = await reader.ReadToEndAsync();

			if(string.IsNullOrEmpty(secret))
				return this.BadRequest();
		}

		return this.Secretor.Verify(token, secret) ? this.NoContent() : this.BadRequest();
	}
	#endregion

	#region 私有方法
	private object Transform(System.Security.Claims.ClaimsPrincipal principal) => Authentication.Instance.Transformer.Transform(principal);
	#endregion
}
