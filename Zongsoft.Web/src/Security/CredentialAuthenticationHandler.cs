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
 * This file is part of Zongsoft.Web library.
 *
 * The Zongsoft.Web is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Web is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Web library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Text.Encodings.Web;

using Microsoft.Net.Http.Headers;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;

namespace Zongsoft.Web.Security
{
	public class CredentialAuthenticationHandler : SignInAuthenticationHandler<CredentialAuthenticationOptions>
	{
		#region 私有变量
		private string _credentialId;
		#endregion

		#region 构造函数
#if NET8_0_OR_GREATER
		public CredentialAuthenticationHandler(IOptionsMonitor<CredentialAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder) : base(options, logger, encoder) { }
#else
		public CredentialAuthenticationHandler(IOptionsMonitor<CredentialAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder, clock) { }
#endif
		#endregion

		#region 重写方法
		protected override Task InitializeHandlerAsync()
		{
			if(this.Request.Headers.TryGetValue(HeaderNames.Authorization, out var header))
			{
				if(header.Count > 0 && header[0].StartsWith(this.Scheme.Name + " ", StringComparison.OrdinalIgnoreCase))
					_credentialId = header[0][this.Scheme.Name.Length..].Trim();
			}

			return Task.CompletedTask;
		}

		protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
		{
			if(string.IsNullOrEmpty(_credentialId))
				return AuthenticateResult.NoResult();

			var authority = this.Options.Authority;

			if(authority == null)
				return AuthenticateResult.Fail("Missing the required credential authority.");

			var principal = await authority.GetPrincipalAsync(_credentialId);

			return principal == null ?
				AuthenticateResult.Fail("Invalid credential Id.") :
				AuthenticateResult.Success(new AuthenticationTicket(principal, this.Scheme.Name));
		}

		protected override async Task HandleForbiddenAsync(AuthenticationProperties properties)
		{
			if(properties != null && properties.Parameters.Count > 0)
			{
				if(properties.Parameters.TryGetValue("Reason", out var reason) && reason != null)
					this.Response.Headers.Append("X-Security-Reason", reason.ToString());

				if(properties.Parameters.TryGetValue("Message", out var message) && message != null)
				{
					this.Response.ContentType = "text/plain; charset=utf-8";
					await this.Response.WriteAsync(message.ToString(), System.Text.Encoding.UTF8);
				}
			}

			await base.HandleForbiddenAsync(properties);
		}

		protected override Task HandleSignInAsync(ClaimsPrincipal user, AuthenticationProperties properties)
		{
			throw new NotImplementedException();
		}

		protected override Task HandleSignOutAsync(AuthenticationProperties properties)
		{
			throw new NotImplementedException();
		}
		#endregion
	}
}
