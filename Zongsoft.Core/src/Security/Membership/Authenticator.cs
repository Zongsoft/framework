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
using System.Collections.Generic;
using System.Collections.Concurrent;

using Zongsoft.Common;
using Zongsoft.Services;

namespace Zongsoft.Security.Membership
{
	[Service]
	public class Authenticator
	{
		#region 静态变量
		private static readonly DateTime EPOCH = new DateTime(2000, 1, 1);
		#endregion

		#region 私有变量
		private readonly ConcurrentDictionary<string, AuthenticatorSuiter> _cache = new ConcurrentDictionary<string, AuthenticatorSuiter>(StringComparer.OrdinalIgnoreCase);
		#endregion

		#region 构造函数
		public Authenticator(IServiceProvider serviceProvider)
		{
			this.ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		}
		#endregion

		#region 公共属性
		/// <summary>获取当前验证器的标识。</summary>
		public string Scheme { get => "Zongsoft.Authenticator"; }

		/// <summary>获取或设置验证失败阻止器。</summary>
		[ServiceDependency]
		public IAttempter Attempter { get; set; }

		/// <summary>获取或设置凭证提供程序。</summary>
		[ServiceDependency]
		public ICredentialProvider Authority { get; set; }

		/// <summary>获取或设置安全主体转换器。</summary>
		[ServiceDependency]
		public IClaimsPrincipalTransformer Transformer { get; set; }

		/// <summary>获取服务提供程序。</summary>
		public IServiceProvider ServiceProvider { get; }
		#endregion

		#region 公共方法
		public OperationResult<CredentialPrincipal> Authenticate(string scheme, string token, object data, string scenario, IDictionary<string, object> parameters)
		{
			var suiter = this.GetSuiter(scheme);
			var instance = new Common.InstanceData(data);

			//校验身份
			var result = suiter.Verifier.Verify(token, instance);

			if(result.Failed)
				return result;

			//签发身份
			var identity = suiter.Issuer.Issue(instance, TimeSpan.Zero, parameters);

			if(identity == null)
				return OperationResult.Fail(SecurityReasons.InvalidIdentity);

			//生成安全主体
			var principal = CreateCredential(identity, scenario);

			//获取安全质询器
			var challengers = this.ServiceProvider.ResolveAll<IIdentityChallenger>();

			if(challengers != null)
			{
				foreach(var challenger in challengers)
				{
					result = challenger.Challenge(principal);

					//质询失败则返回失败
					if(result.Failed)
						return result;
				}
			}

			//注册凭证
			this.Authority.Register(principal);

			//返回成功
			return OperationResult.Success(principal);
		}
		#endregion

		#region 虚拟方法
		protected virtual CredentialPrincipal CreateCredential(System.Security.Claims.ClaimsIdentity identity, string scenario)
		{
			return new CredentialPrincipal(
				((ulong)(DateTime.UtcNow - EPOCH).TotalSeconds).ToString() + Common.Randomizer.GenerateString(8),
				((ulong)(DateTime.UtcNow - EPOCH).TotalDays).ToString() + Environment.TickCount64.ToString("X") + Common.Randomizer.GenerateString(8),
				scenario,
				identity);
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
				return new AuthenticatorSuiter(
					this.ServiceProvider.ResolveRequired<DefaultIdentityIssuer>(),
					this.ServiceProvider.ResolveRequired<DefaultIdentityVerifier>());

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
