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

using Zongsoft.Collections;

namespace Zongsoft.Security.Privileges;

/// <summary>
/// 表示身份验证器的接口。
/// </summary>
public interface IAuthenticator
{
	#region 属性定义
	/// <summary>获取验证器的方案名。</summary>
	string Name { get; }
	#endregion

	#region 方法定义
	/// <summary>校验身份。</summary>
	/// <param name="key">指定的校验键值。</param>
	/// <param name="data">指定的校验数据。</param>
	/// <param name="scenario">指定的验证场景。</param>
	/// <param name="parameters">指定的参数集。</param>
	/// <param name="cancellation">异步操作的取消标记。</param>
	/// <returns>返回的校验结果。</returns>
	ValueTask<object> VerifyAsync(string key, object data, string scenario, Parameters parameters, CancellationToken cancellation = default);

	/// <summary>签发身份凭证。</summary>
	/// <param name="token">指定的身份令牌。</param>
	/// <param name="scenario">指定的验证场景。</param>
	/// <param name="parameters">指定的参数集。</param>
	/// <param name="cancellation">异步操作的取消标记。</param>
	/// <returns></returns>
	ValueTask<ClaimsIdentity> IssueAsync(object token, string scenario, Parameters parameters, CancellationToken cancellation = default);
	#endregion
}

/// <summary>
/// 表示身份验证器的接口。
/// </summary>
public interface IAuthenticator<in TRequirement, TTicket> : IAuthenticator
{
	#region 方法定义
	/// <summary>校验身份。</summary>
	/// <param name="key">指定的校验键值。</param>
	/// <param name="data">指定的校验数据。</param>
	/// <param name="scenario">指定的验证场景。</param>
	/// <param name="parameters">指定的参数集。</param>
	/// <param name="cancellation">异步操作的取消标记。</param>
	/// <returns>返回的校验票证。</returns>
	ValueTask<TTicket> VerifyAsync(string key, TRequirement data, string scenario, Parameters parameters, CancellationToken cancellation = default);

	/// <summary>签发身份凭证。</summary>
	/// <param name="ticket">指定的身份票证。</param>
	/// <param name="scenario">指定的验证场景。</param>
	/// <param name="parameters">指定的参数集。</param>
	/// <param name="cancellation">异步操作的取消标记。</param>
	/// <returns></returns>
	ValueTask<ClaimsIdentity> IssueAsync(TTicket ticket, string scenario, Parameters parameters, CancellationToken cancellation = default);
	#endregion
}
