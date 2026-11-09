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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;
using System.Security.Claims;

using Zongsoft.Collections;

namespace Zongsoft.Security.Privileges;

public class AuthenticatedEventArgs : EventArgs
{
	#region 构造函数
	public AuthenticatedEventArgs(string scheme, ClaimsPrincipal principal, string scenario, Parameters parameters = null) : this(null, scheme, principal, scenario, parameters) { }
	public AuthenticatedEventArgs(Exception exception, string scheme, ClaimsPrincipal principal, string scenario, Parameters parameters = null)
	{
		this.Exception = exception;
		this.Principal = principal;
		this.Scheme = scheme;
		this.Scenario = scenario;
		this.Parameters = parameters;
	}
	#endregion

	#region 公共属性
	/// <summary>获取验证方案。</summary>
	public string Scheme { get; }

	/// <summary>获取身份验证的应用场景。</summary>
	public string Scenario { get; }

	/// <summary>获取身份验证的用户身份。</summary>
	public ClaimsPrincipal Principal { get; }

	/// <summary>获取验证失败的异常对象。</summary>
	public Exception Exception { get; }

	/// <summary>获取身份验证是否通过。</summary>
	public bool IsAuthenticated => this.Principal?.Identity != null &&
		this.Principal.Identity.IsAuthenticated && !string.IsNullOrEmpty(this.Principal.Identity.Name);

	/// <summary>获取或设置验证结果的扩展参数集。</summary>
	public Parameters Parameters { get; set; }
	#endregion

	#region 公共方法
	public bool HasException(out Exception exception)
	{
		exception = this.Exception;
		return exception != null;
	}
	#endregion
}

public class AuthenticatingEventArgs : EventArgs
{
	#region 构造函数
	public AuthenticatingEventArgs(string scheme, object ticket, string scenario, Parameters parameters = null)
	{
		this.Scheme = scheme;
		this.Ticket = ticket;
		this.Scenario = scenario;
		this.Parameters = parameters;
	}
	#endregion

	#region 公共属性
	/// <summary>获取验证方案。</summary>
	public string Scheme { get; }

	/// <summary>获取待验证的票券数据。</summary>
	public object Ticket { get; }

	/// <summary>获取身份验证的应用场景。</summary>
	public string Scenario { get; }

	/// <summary>获取或设置验证结果的扩展参数集。</summary>
	public Parameters Parameters { get; set; }
	#endregion
}
