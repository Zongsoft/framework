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

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Zongsoft.Services;
using Zongsoft.Security.Membership;

namespace Zongsoft.Security.Web.Controllers
{
	public class AuthenticationController : ControllerBase
	{
		#region 成员字段
		private IAuthenticator _authenticator;
		private ICredentialProvider _credentialProvider;
		#endregion

		#region 公共属性
		[ServiceDependency]
		public IAuthenticator Authenticator
		{
			get => _authenticator;
			set => _authenticator = value ?? throw new ArgumentNullException();
		}

		[ServiceDependency]
		public ICredentialProvider CredentialProvider
		{
			get => _credentialProvider;
			set => _credentialProvider = value ?? throw new ArgumentNullException();
		}
		#endregion

		#region 公共方法
		[HttpPost]
		[ActionName("Signin")]
		public Task<IActionResult> SigninAsync(string id, AuthenticationRequest request)
		{
			if(string.IsNullOrWhiteSpace(id))
				return Task.FromResult((IActionResult)this.BadRequest());

			var scene = id.Trim();
			var parameters = request.Parameters;

			//处理头部参数
			this.FillParameters(ref parameters);

			//进行身份验证
			var user = string.IsNullOrEmpty(request.Secret) ?
				_authenticator.Authenticate(request.Identity, request.Password, request.Namespace, scene, ref parameters) :
				_authenticator.AuthenticateSecret(request.Identity, request.Secret, request.Namespace, scene, ref parameters);

			//设置凭证的默认有效期为2小时
			var duration = TimeSpan.FromHours(2);

			//尝试通过验证上下文的参数集获取其他程序指定的凭证配置项
			if(parameters != null && parameters.TryGetValue("Credential:Option", out var value) && value is Membership.Options.ICredentialOption option)
			{
				if(option.Policies.TryGet(scene, out var period))
					duration = period.Period;
				else
					duration = option.Period;

				//用完即从参数集中移除掉凭证配置项
				parameters.Remove("Credential:Option");
			}

			//尝试通过验证上下文的参数集获取其他程序指定的凭证有效期时长
			if(parameters != null && parameters.TryGetValue("Credential:Period", out value) && value != null)
			{
				if(value is TimeSpan period)
					duration = period;
				else if(value is int integer && integer > 0)
					duration = TimeSpan.FromMinutes(integer);
				else if(value is string text && TimeSpan.TryParse(text, out period))
					duration = period;

				//用完即从参数集中移除掉凭证有效期时长
				parameters.Remove("Credential:Period");
			}

			//创建用户凭证
			var credential = new Credential(user, scene, duration, parameters);

			//注册用户凭证
			_credentialProvider.Register(credential);

			//返回注册的凭证
			return Task.FromResult((IActionResult)this.Ok(credential));
		}

		[HttpGet]
		[Authorization]
		public void Signout(string id)
		{
			if(id != null && id.Length > 0)
				_credentialProvider.Unregister(id);
		}

		[HttpGet]
		public IActionResult Secret(string id)
		{
			if(string.IsNullOrWhiteSpace(id))
				return this.BadRequest();

			var parts = id.Split(':');

			if(parts.Length > 1)
				_authenticator.Secret(parts[1], parts[0]);
			else
				_authenticator.Secret(parts[0], null);

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
		#endregion
	}
}
