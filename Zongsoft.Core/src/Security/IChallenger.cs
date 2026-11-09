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

namespace Zongsoft.Security;

/// <summary>提供身份质询的接口。</summary>
public interface IChallenger
{
	/// <summary>身份质询。</summary>
	/// <param name="principal">指定的质询身份主体。</param>
	/// <param name="scenario">指定的质询场景。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回的异步操作任务。</returns>
	ValueTask ChallengeAsync(ClaimsPrincipal principal, string scenario, CancellationToken cancellation = default);
}
