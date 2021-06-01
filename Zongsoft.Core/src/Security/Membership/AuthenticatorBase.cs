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
using System.Security.Claims;

using Zongsoft.Common;
using Zongsoft.Services;

namespace Zongsoft.Security.Membership
{
	public abstract class AuthenticatorBase
	{
		#region 静态变量
		private static readonly DateTime EPOCH = new DateTime(2000, 1, 1);
		#endregion

		#region 构造函数
		protected AuthenticatorBase(IServiceProvider serviceProvider)
		{
			this.ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		}
		#endregion

		#region 公共属性
		/// <summary>获取当前验证器的标识。</summary>
		public virtual string Scheme { get => "Zongsoft.Authenticator"; }

		/// <summary>获取或设置验证失败阻止器。</summary>
		[ServiceDependency(Mapping.Security)]
		public IAttempter Attempter { get; set; }

		/// <summary>获取或设置凭证提供程序。</summary>
		[ServiceDependency(Mapping.Security, IsRequired = true)]
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
			//进行身份验证
			var result = this.OnAuthenticate(scheme, token, data, scenario, parameters);

			if(result.Failed)
				return OperationResult.Fail(result.Reason, result.Message);

			//生成安全主体
			var principal = CreateCredential(result.Value, scenario);

			//获取安全质询器
			var challengers = this.GetChallengers();

			if(challengers != null)
			{
				foreach(var challenger in challengers)
				{
					//如果当前质询器无法处理则跳过
					if(challenger == null || !challenger.CanChallenge(principal))
						continue;

					result = challenger.Challenge(principal);

					//质询失败则返回失败
					if(result.Failed)
						return OperationResult.Fail(result.Reason, result.Message);
				}
			}

			//注册凭证
			this.Authority.Register(principal);

			//返回成功
			return OperationResult.Success(principal);
		}
		#endregion

		#region 抽象方法
		protected abstract OperationResult<ClaimsIdentity> OnAuthenticate(string scheme, string token, object data, string scenario, IDictionary<string, object> parameters);
		#endregion

		#region 虚拟方法
		protected virtual IEnumerable<IIdentityChallenger> GetChallengers() => this.ServiceProvider.ResolveAll<IIdentityChallenger>();

		protected virtual CredentialPrincipal CreateCredential(ClaimsIdentity identity, string scenario)
		{
			return new CredentialPrincipal(
				((ulong)(DateTime.UtcNow - EPOCH).TotalSeconds).ToString() + Randomizer.GenerateString(8),
				((ulong)(DateTime.UtcNow - EPOCH).TotalDays).ToString() + Environment.TickCount64.ToString("X") + Randomizer.GenerateString(8),
				scenario,
				identity);
		}
		#endregion
	}
}
