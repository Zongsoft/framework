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
	public abstract class AuthenticationBase : IAuthentication
	{
		#region 事件定义
		public event EventHandler<AuthenticatedEventArgs> Authenticated;
		public event EventHandler<AuthenticatingEventArgs> Authenticating;
		#endregion

		#region 构造函数
		protected AuthenticationBase(IServiceProvider serviceProvider)
		{
			this.ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		}
		#endregion

		#region 公共属性
		/// <summary>获取当前验证器的标识。</summary>
		public virtual string Scheme { get => "Zongsoft.Authentication"; }

		/// <summary>获取或设置验证失败阻止器。</summary>
		[ServiceDependency(Mapping.Security)]
		public IAttempter Attempter { get; set; }

		/// <summary>获取服务提供程序。</summary>
		public IServiceProvider ServiceProvider { get; }
		#endregion

		#region 公共方法
		public OperationResult<CredentialPrincipal> Authenticate(string scheme, string key, object data, string scenario, IDictionary<string, object> parameters)
		{
			//激发“Authenticating”事件
			this.OnAuthenticating(new AuthenticatingEventArgs(this, data, scenario, parameters));

			//进行身份验证
			var result = this.OnAuthenticate(scheme, key, data, scenario, parameters);

			if(result.Failed)
				return (OperationResult)result.Failure;

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
						return (OperationResult)result.Failure;
				}
			}

			//通知验证完成
			this.OnAuthenticated(principal, scenario, parameters);

			//返回成功
			return OperationResult.Success(principal);
		}
		#endregion

		#region 抽象方法
		protected abstract OperationResult<ClaimsIdentity> OnAuthenticate(string scheme, string key, object data, string scenario, IDictionary<string, object> parameters);
		#endregion

		#region 虚拟方法
		protected virtual void OnAuthenticated(CredentialPrincipal principal, string scenario, IDictionary<string, object> parameters)
		{
			//注册凭证
			Authentication.Authority.Register(principal);

			//激发“Authenticated”事件
			this.OnAuthenticated(new AuthenticatedEventArgs(this, principal, scenario, parameters));
		}

		protected virtual IEnumerable<IIdentityChallenger> GetChallengers() => this.ServiceProvider.ResolveAll<IIdentityChallenger>();
		protected virtual CredentialPrincipal CreateCredential(ClaimsIdentity identity, string scenario) => new CredentialPrincipal(scenario, identity);
		#endregion

		#region 激发事件
		protected virtual void OnAuthenticated(AuthenticatedEventArgs args) => this.Authenticated?.Invoke(this, args);
		protected virtual void OnAuthenticating(AuthenticatingEventArgs args) => this.Authenticating?.Invoke(this, args);
		#endregion
	}
}
