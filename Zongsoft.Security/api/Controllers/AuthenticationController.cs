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

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;

using Zongsoft.Web;
using Zongsoft.Services;
using Zongsoft.Web.Http;
using Zongsoft.Security.Privileges;

namespace Zongsoft.Security.Web.Controllers;

[Area(Module.NAME)]
[ControllerName("Authentication")]
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
	[HttpPost("[action]/{scheme?}/{key?}")]
	public async ValueTask<IActionResult> Signin(string scheme, string key, [FromQuery]string scenario, CancellationToken cancellation = default)
	{
		if(string.IsNullOrWhiteSpace(scenario))
			return this.BadRequest();

		try
		{
			object data = await GetDataAsync(this.Request, cancellation);
			var principal = await Authentication.AuthenticateAsync(scheme, key, data, scenario, new(this.Request.GetParameters()), cancellation);

			return principal != null ?
				this.Ok(Transform(principal)) :
				this.StatusCode(StatusCodes.Status403Forbidden, new { Reason = SecurityReasons.Unknown });
		}
		catch(AuthenticationException ex)
		{
			return this.StatusCode(StatusCodes.Status403Forbidden, new { ex.Reason, ex.Message });
		}

		static async ValueTask<object> GetDataAsync(HttpRequest request, CancellationToken cancellation)
		{
			if(request.ContentLength == null || request.ContentLength == 0)
				return null;

			if(request.HasFormContentType)
				return request.Form.ToDictionary();

			return await request.ReadAsStringAsync(cancellation) ?? (object)request.Body;
		}
	}

	[HttpPost("[action]")]
	public async ValueTask Signout(CancellationToken cancellation = default)
	{
		if(this.User is CredentialPrincipal credential && credential.CredentialId != null)
			await Authentication.Authority.UnregisterAsync(credential.CredentialId, cancellation);
	}

	[Authorize]
	[HttpPost("[action]/{id:required}")]
	public async ValueTask<IActionResult> Renew(string id, CancellationToken cancellation = default)
	{
		if(string.IsNullOrWhiteSpace(id))
			return this.BadRequest();

		if(this.User is CredentialPrincipal credential)
		{
			var principal = await Authentication.Authority.RenewAsync(credential.CredentialId, id, cancellation);
			return principal == null ? this.NotFound() : this.Ok(Transform(principal));
		}

		return this.Unauthorized();
	}

	[HttpPost("[action]/{scheme}:{destination}")]
	public async ValueTask<IActionResult> SecretAsync(string scheme, string destination, [FromQuery]string scenario, [FromQuery]string channel = null, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(scheme) || string.IsNullOrEmpty(destination))
			return this.BadRequest();

		if(destination.IndexOfAny(InvalidDestinationCharacters) >= 0)
			return this.BadRequest($"The specified destination parameter contains illegal characters.");

		var captcha = this.Request.Headers.TryGetValue(Headers.Captcha, out var text) ? text.ToString() : null;
		var result = await this.Secretor.Transmitter.TransmitAsync(scheme, destination, "Authentication", "Singin:" + scenario, captcha, channel, destination, cancellation);
		return this.Content(result);
	}

	[HttpPost("[action]/{token}")]
	public async ValueTask<IActionResult> VerifyAsync(string token, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(token))
			return this.BadRequest();
		if(this.Request.ContentLength < 1)
			return this.BadRequest();

		string secret;

		using(var reader = new System.IO.StreamReader(this.Request.Body))
		{
			#if NET7_0_OR_GREATER
			secret = await reader.ReadToEndAsync(cancellation);
			#else
			secret = await reader.ReadToEndAsync();
			#endif

			if(string.IsNullOrEmpty(secret))
				return this.BadRequest();
		}

		(var succeed, var _) = await this.Secretor.VerifyAsync(token, secret, cancellation);
		return succeed ? this.NoContent() : this.BadRequest();
	}
	#endregion

	#region 私有方法
	private static object Transform(System.Security.Claims.ClaimsPrincipal principal) => Authentication.Transformer.Transform(principal);
	#endregion
}
