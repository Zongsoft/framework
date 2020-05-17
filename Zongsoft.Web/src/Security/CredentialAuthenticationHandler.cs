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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Zongsoft.Web.Security
{
	public class CredentialAuthenticationHandler : SignInAuthenticationHandler<CredentialAuthenticationOptions>
	{
		#region 私有变量
		private string _credentialId;
		#endregion

		#region 构造函数
		public CredentialAuthenticationHandler(IOptionsMonitor<CredentialAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder, clock)
		{
		}
		#endregion

		#region 重写方法
		protected override Task InitializeHandlerAsync()
		{
			if(this.Request.Headers.TryGetValue(HeaderNames.Authorization, out var header))
			{
				if(header.Count > 0 && header[0].StartsWith(this.Scheme.Name + " ", StringComparison.OrdinalIgnoreCase))
					_credentialId = header[0].Substring(this.Scheme.Name.Length).Trim();
			}

			return Task.CompletedTask;
		}

		protected override Task<AuthenticateResult> HandleAuthenticateAsync()
		{
			if(string.IsNullOrEmpty(_credentialId))
				return Task.FromResult(AuthenticateResult.NoResult());

			var principal = this.Options.Authority.GetPrincipal(_credentialId);

			return principal == null ?
				Task.FromResult(AuthenticateResult.Fail("Invalid credential Id.")) :
				Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, this.Scheme.Name)));
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
