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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

using Zongsoft.Services;
using Zongsoft.Security.Membership;

namespace Zongsoft.Security.Web.Controllers
{
	[ApiController]
	[Area(Modules.Security)]
	[Route("{area}/{controller}/{action}/{id?}")]
	public class AuthenticationController : ControllerBase
	{
		#region 公共方法
		[HttpPost]
		public Task<IActionResult> SigninAsync(string id, [FromBody]AuthenticationRequest request)
		{
			if(string.IsNullOrWhiteSpace(id))
				return Task.FromResult((IActionResult)this.BadRequest());

			var scenario = id.Trim();
			var parameters = request.Parameters;

			//处理头部参数
			this.FillParameters(ref parameters);

			//进行身份验证
			var result = string.IsNullOrEmpty(request.Secret) ?
				Authentication.Instance.Authenticate(request.Identity, request.Password, request.Namespace, scenario, parameters) :
				Authentication.Instance.AuthenticateSecret(request.Identity, request.Secret, request.Namespace, scenario, parameters);

			return result.Succeed ?
				Task.FromResult((IActionResult)this.Ok(result.Principal.ToDictionary())) :
				Task.FromResult((IActionResult)this.BadRequest(new AuthenticationFailure(result)));
		}

		[HttpPost]
		[Authorize]
		public void Signout()
		{
			if(this.User is CredentialPrincipal credential)
				Authentication.Instance.Authority.Unregister(credential.CredentialId);
		}

		[HttpPost]
		[Authorize]
		public Task<IActionResult> Renew(string id)
		{
			if(string.IsNullOrWhiteSpace(id))
				return Task.FromResult((IActionResult)this.BadRequest());

			if(this.User is CredentialPrincipal credential)
			{
				var principal = Authentication.Instance.Authority.Renew(credential.CredentialId, id);

				return principal == null ?
					Task.FromResult((IActionResult)this.BadRequest()) :
					Task.FromResult((IActionResult)this.Ok(principal.ToDictionary()));
			}

			return Task.FromResult((IActionResult)this.Unauthorized());
		}

		[HttpPost]
		public IActionResult Secret(string id)
		{
			if(string.IsNullOrWhiteSpace(id))
				return this.BadRequest();

			var parts = id.Split(':');

			if(parts.Length > 1)
				Authentication.Instance.Authenticator.Secret(parts[1], parts[0]);
			else
				Authentication.Instance.Authenticator.Secret(parts[0], null);

			return this.NoContent();
		}
		#endregion

		#region 私有方法
		private void FillParameters(ref IDictionary<string, object> parameters)
		{
			const string X_PARAMETER_PREFIX = "x-parameter-";

			if(parameters == null)
				parameters = new Dictionary<string, object>();

			foreach(var header in this.Request.Headers)
			{
				if(header.Key.Length > X_PARAMETER_PREFIX.Length &&
				   header.Key.StartsWith(X_PARAMETER_PREFIX, StringComparison.OrdinalIgnoreCase))
				{
					parameters.Add(header.Key.Substring(X_PARAMETER_PREFIX.Length), string.Join("|", header.Value));
				}
			}
		}
		#endregion

		#region 嵌套子类
		public struct AuthenticationRequest
		{
			#region 成员字段
			private string _identity;
			#endregion

			#region 公共属性
			public string Identity
			{
				get
				{
					return _identity;
				}
				set
				{
					if(string.IsNullOrWhiteSpace(value))
						throw new ArgumentNullException();

					_identity = value.Trim();
				}
			}

			public string Password
			{
				get; set;
			}

			public string Secret
			{
				get; set;
			}

			public string Namespace
			{
				get; set;
			}

			public IDictionary<string, object> Parameters
			{
				get;
				set;
			}
			#endregion
		}

		public struct AuthenticationFailure
		{
			#region 构造函数
			public AuthenticationFailure(AuthenticationResult result)
			{
				this.Reason = result.Reason;
				this.Message = result.Exception != null ?
					result.Exception.Message :
					Common.EnumUtility.GetEnumDescription(result.Reason);
			}
			#endregion

			#region 公共属性
			public AuthenticationReason Reason { get; }
			public string Message { get; }
			#endregion
		}
		#endregion
	}
}
