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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Threading;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Collections.Generic;

using Zongsoft.Services;
using Zongsoft.Components;
using Zongsoft.Collections;

namespace Zongsoft.Security.Privileges;

/// <summary>
/// 提供身份验证的平台类。
/// </summary>
[System.Reflection.DefaultMember(nameof(Authenticators))]
public static partial class Authentication
{
	#region 事件定义
	public static event EventHandler<AuthenticatedEventArgs> Authenticated;
	public static event EventHandler<AuthenticatingEventArgs> Authenticating;
	#endregion

	#region 成员字段
	private static ICredentialProvider _authority;
	private static readonly AuthenticatorCollection _authenticators;
	private static readonly List<IChallenger> _challengers;
	private static IClaimsPrincipalTransformer _transformer;
	#endregion

	#region 静态构造
	static Authentication()
	{
		_authenticators = new AuthenticatorCollection();
		_challengers = new List<IChallenger>();
		_transformer = ClaimsPrincipalTransformer.Default;
		Servicer = new();
		Attempter = new Attempter();
	}
	#endregion

	#region 公共属性
	/// <summary>获取或设置凭证主体的提供程序。</summary>
	public static ICredentialProvider Authority
	{
		get => _authority ??= ApplicationContext.Current?.Services.Resolve<ICredentialProvider>();
		set => _authority = value ?? throw new ArgumentNullException();
	}

	/// <summary>获取一个身份验证器集合。</summary>
	public static AuthenticatorCollection Authenticators => _authenticators;

	/// <summary>获取一个身份验证质询器集合。</summary>
	public static ICollection<IChallenger> Challengers => _challengers;

	/// <summary>获取一个安全主体的转换器。</summary>
	public static IClaimsPrincipalTransformer Transformer { get => _transformer; set => _transformer = value ?? throw new ArgumentNullException(); }

	/// <summary>获取或设置验证失败阻止器。</summary>
	public static IAttempter Attempter { get; set; }

	/// <summary>获取身份验证服务提供程序。</summary>
	public static AuthenticationServicer Servicer { get; }
	#endregion

	#region 公共方法
	public static async ValueTask<CredentialPrincipal> AuthenticateAsync(string scheme, string key, object data, string scenario, Parameters parameters, CancellationToken cancellation = default)
	{
		//激发“Authenticating”事件
		OnAuthenticating(new AuthenticatingEventArgs(data, scenario, parameters));

		//进行身份验证
		var identity = await OnAuthenticateAsync(scheme, key, data, scenario, parameters, cancellation);

		//生成安全主体
		var principal = CreatePrincipal(identity, scenario);

		//遍历安全质询器集合并依次质询
		foreach(var challenger in _challengers)
		{
			challenger.Challenge(principal, scenario);
		}

		//通知验证完成
		OnAuthenticated(principal, scenario, parameters);

		//返回成功
		return principal;
	}
	#endregion

	#region 虚拟方法
	private static async ValueTask<ClaimsIdentity> OnAuthenticateAsync(string scheme, string key, object data, string scenario, Parameters parameters, CancellationToken cancellation)
	{
		//获取身份验证器
		var authenticator = GetAuthenticator(scheme, key, data, scenario) ??
			throw new AuthenticationException(SecurityReasons.InvalidArgument, $"Invalid authenticator scheme.");

		//校验身份
		var result = await authenticator.VerifyAsync(key, data, scenario, parameters, cancellation);

		//签发身份
		var identity = await authenticator.IssueAsync(result, scenario, parameters, cancellation) ??
			throw new AuthenticationException(SecurityReasons.InvalidIdentity);

		return identity;
	}

	private static void OnAuthenticated(CredentialPrincipal principal, string scenario, Parameters parameters)
	{
		var authority = Authority ?? throw new AuthenticationException("NoAuthority", $"Missing the required credential provider.");

		//注册凭证
		authority.Register(principal);

		//激发“Authenticated”事件
		OnAuthenticated(new AuthenticatedEventArgs(principal, scenario, parameters));
	}

	private static IAuthenticator GetAuthenticator(string scheme, string key, object data, string scenario) =>
		_authenticators.TryGetValue(scheme ?? string.Empty, out var authenticator) ? authenticator : null;

	private static CredentialPrincipal CreatePrincipal(ClaimsIdentity identity, string scenario) => new CredentialPrincipal(identity, scenario);
	#endregion

	#region 激发事件
	private static void OnAuthenticated(AuthenticatedEventArgs args) => Authenticated?.Invoke(null, args);
	private static void OnAuthenticating(AuthenticatingEventArgs args) => Authenticating?.Invoke(null, args);
	#endregion

	#region 嵌套子类
	public sealed class AuthenticationServicer
	{
		internal AuthenticationServicer() { }
		public IRoleService Roles { get; set; }
		public IUserService Users { get; set; }
		public IMemberService Members { get; set; }
	}
	#endregion
}
