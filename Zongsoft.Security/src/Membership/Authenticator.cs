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
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Security.Claims;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Zongsoft.Common;
using Zongsoft.Services;
using Zongsoft.Configuration.Options;

namespace Zongsoft.Security.Membership
{
	[Service]
	public class Authenticator : AuthenticatorBase
	{
		#region 私有变量
		private readonly ConcurrentDictionary<string, AuthenticatorSuiter> _cache = new ConcurrentDictionary<string, AuthenticatorSuiter>(StringComparer.OrdinalIgnoreCase);
		#endregion

		#region 构造函数
		public Authenticator(IServiceProvider serviceProvider) : base(serviceProvider) { }
		#endregion

		#region 公共属性
		[Options("Security/Membership/Authentication")]
		public Configuration.AuthenticationOptions Options { get; set; }
		#endregion

		#region 身份验证
		protected override OperationResult<ClaimsIdentity> OnAuthenticate(string scheme, string token, object data, string scenario, IDictionary<string, object> parameters)
		{
			var suiter = this.GetSuiter(scheme);

			//校验身份
			var result = suiter.Verifier.Verify(token, data, out var ticket);

			if(result.Failed)
				return result;

			//定义凭证有效期
			var period = TimeSpan.Zero;

			//获取指定场景对应的凭证有效期
			if(this.Options != null && this.Options.Expiration.TryGet(scenario, out var option))
				period = option.Period;

			//签发身份
			var identity = suiter.Issuer.Issue(ticket, period, parameters);

			if(identity == null)
				return OperationResult.Fail(SecurityReasons.InvalidIdentity);

			return OperationResult.Success(identity);
		}
		#endregion

		#region 私有方法
		private AuthenticatorSuiter GetSuiter(string scheme)
		{
			if(_cache.IsEmpty)
			{
				var verifiers = this.ServiceProvider.ResolveAll<IIdentityVerifier>();

				foreach(var verifier in verifiers)
					_cache.TryAdd(verifier.Name, new AuthenticatorSuiter(verifier));

				var issuers = this.ServiceProvider.ResolveAll<IIdentityIssuer>();

				foreach(var issuer in issuers)
					_cache.AddOrUpdate(issuer.Name, _ => new AuthenticatorSuiter(issuer), (_, value) => new AuthenticatorSuiter(issuer, value.Verifier));
			}

			if(string.IsNullOrEmpty(scheme))
				scheme = this.Options.Scheme ?? string.Empty;

			return _cache.TryGetValue(scheme, out var token) ? token : throw new SecurityException($"Invalid '{scheme}' authentication scheme.");
		}
		#endregion

		#region 嵌套结构
		private readonly struct AuthenticatorSuiter
		{
			public AuthenticatorSuiter(IIdentityVerifier verifier, IIdentityIssuer issuer = null)
			{
				this.Verifier = verifier;
				this.Issuer = issuer;
			}

			public AuthenticatorSuiter(IIdentityIssuer issuer, IIdentityVerifier verifier = null)
			{
				this.Issuer = issuer;
				this.Verifier = verifier;
			}

			public readonly IIdentityVerifier Verifier;
			public readonly IIdentityIssuer Issuer;
		}
		#endregion
	}
}
