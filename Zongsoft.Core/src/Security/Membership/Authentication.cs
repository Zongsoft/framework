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
using System.Collections.ObjectModel;

using Zongsoft.Common;
using Zongsoft.Services;

namespace Zongsoft.Security.Membership
{
	/// <summary>
	/// 提供身份验证的平台类。
	/// </summary>
	[System.Reflection.DefaultMember(nameof(Authenticators))]
	public class Authentication
	{
		#region 单例字段
		private static Authentication _instance = new Authentication();
		#endregion

		#region 事件定义
		public event EventHandler<AuthenticatedEventArgs> Authenticated;
		public event EventHandler<AuthenticatingEventArgs> Authenticating;
		#endregion

		#region 成员字段
		private ICredentialProvider _authority;
		private IClaimsPrincipalTransformer _transformer;
		#endregion

		#region 构造函数
		protected Authentication()
		{
			_transformer = ClaimsPrincipalTransformer.Default;
		}
		#endregion

		#region 单例属性
		public static Authentication Instance
		{
			get => _instance;
			set => _instance = value ?? throw new ArgumentNullException();
		}
		#endregion

		#region 公共属性
		/// <summary>获取当前验证器的标识。</summary>
		public virtual string Scheme { get => "Zongsoft.Authentication"; }

		/// <summary>获取或设置凭证主体的提供程序。</summary>
		public ICredentialProvider Authority
		{
			get => _authority??= ApplicationContext.Current?.Services.Resolve<ICredentialProvider>();
			set => _authority = value ?? throw new ArgumentNullException();
		}

		/// <summary>获取或设置主体转换器。</summary>
		public IClaimsPrincipalTransformer Transformer
		{
			get => _transformer;
			set => _transformer = value ?? throw new ArgumentNullException();
		}

		/// <summary>获取一个身份验证器集合。</summary>
		public KeyedCollection<string, IAuthenticator> Authenticators { get; }

		/// <summary>获取一个身份验证质询器集合。</summary>
		public ICollection<IChallenger> Challengers { get; }

		/// <summary>获取或设置验证失败阻止器。</summary>
		public IAttempter Attempter { get; set; }
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
					result = challenger.Challenge(principal, scenario);

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
		protected virtual OperationResult<ClaimsIdentity> OnAuthenticate(string scheme, string key, object data, string scenario, IDictionary<string, object> parameters)
		{
			var authenticator = this.GetAuthenticator(scheme, key, data, scenario);

			if(authenticator == null)
				return OperationResult.Fail("InvalidAuthenticator");

			//校验身份
			var result = authenticator.Verify(key, data, scenario);

			if(result.Failed)
				return result;

			//签发身份
			var identity = authenticator.Issue(result.Value, scenario, parameters);

			if(identity == null)
				return OperationResult.Fail(SecurityReasons.InvalidIdentity);

			return OperationResult.Success(identity);
		}
		#endregion

		#region 虚拟方法
		protected virtual IAuthenticator GetAuthenticator(string scheme, string key, object data, string scenario)
		{
			var authenticators = Authenticators;

			if(authenticators != null && authenticators.Count > 0)
				return authenticators.TryGetValue(scheme, out var authenticator) ? authenticator : null;

			return ApplicationContext.Current?.Services.Resolve<IAuthenticator>(scheme);
		}

		protected virtual void OnAuthenticated(CredentialPrincipal principal, string scenario, IDictionary<string, object> parameters)
		{
			var authority = this.Authority ?? throw new AuthenticationException("NoAuthority", $"Missing the required credential provider.");

			//注册凭证
			authority.Register(principal);

			//激发“Authenticated”事件
			this.OnAuthenticated(new AuthenticatedEventArgs(this, principal, scenario, parameters));
		}

		protected virtual IEnumerable<IChallenger> GetChallengers()
		{
			var challengers = Challengers;
			return challengers != null && challengers.Count > 0 ? challengers : ApplicationContext.Current?.Services.ResolveAll<IChallenger>() ?? Array.Empty<IChallenger>();
		}

		protected virtual CredentialPrincipal CreateCredential(ClaimsIdentity identity, string scenario) => new CredentialPrincipal(scenario, identity);
		#endregion

		#region 激发事件
		protected virtual void OnAuthenticated(AuthenticatedEventArgs args) => this.Authenticated?.Invoke(this, args);
		protected virtual void OnAuthenticating(AuthenticatingEventArgs args) => this.Authenticating?.Invoke(this, args);
		#endregion
	}
}
