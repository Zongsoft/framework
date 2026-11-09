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
using System.Threading;
using System.Threading.Tasks;
using System.Security.Claims;

using Zongsoft.Collections;

namespace Zongsoft.Security.Privileges;

/// <summary>表示身份验证器的接口。</summary>
public interface IAuthenticator
{
	#region 属性定义
	/// <summary>获取验证器的方案名。</summary>
	string Name { get; }
	#endregion

	#region 方法定义
	/// <summary>认证身份并返回认证票证。</summary>
	/// <param name="key">指定的认证键值。</param>
	/// <param name="data">指定的认证材料。</param>
	/// <param name="scenario">指定的验证场景。</param>
	/// <param name="parameters">指定的参数集。</param>
	/// <param name="cancellation">异步操作的取消标记。</param>
	/// <returns>返回认证通过后的身份票证。</returns>
	ValueTask<object> AuthenticateAsync(string key, object data, string scenario, Parameters parameters, CancellationToken cancellation = default);

	/// <summary>签发声明身份。</summary>
	/// <param name="ticket">指定的身份票证。</param>
	/// <param name="scenario">指定的验证场景。</param>
	/// <param name="parameters">指定的参数集。</param>
	/// <param name="cancellation">异步操作的取消标记。</param>
	/// <returns>返回签发的声明身份。</returns>
	ValueTask<ClaimsIdentity> IssueAsync(object ticket, string scenario, Parameters parameters, CancellationToken cancellation = default);
	#endregion
}

/// <summary>表示身份验证器的接口。</summary>
public interface IAuthenticator<in TRequirement, TTicket> : IAuthenticator
{
	#region 方法定义
	/// <summary>认证身份并返回认证票证。</summary>
	/// <param name="key">指定的认证键值。</param>
	/// <param name="data">指定的认证材料。</param>
	/// <param name="scenario">指定的验证场景。</param>
	/// <param name="parameters">指定的参数集。</param>
	/// <param name="cancellation">异步操作的取消标记。</param>
	/// <returns>返回认证通过后的身份票证。</returns>
	ValueTask<TTicket> AuthenticateAsync(string key, TRequirement data, string scenario, Parameters parameters, CancellationToken cancellation = default);

	/// <summary>签发声明身份。</summary>
	/// <param name="ticket">指定的身份票证。</param>
	/// <param name="scenario">指定的验证场景。</param>
	/// <param name="parameters">指定的参数集。</param>
	/// <param name="cancellation">异步操作的取消标记。</param>
	/// <returns>返回签发的声明身份。</returns>
	ValueTask<ClaimsIdentity> IssueAsync(TTicket ticket, string scenario, Parameters parameters, CancellationToken cancellation = default);
	#endregion
}
