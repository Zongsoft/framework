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
using System.Threading.RateLimiting;

using Polly;
using Polly.RateLimiting;

using Zongsoft.Components.Features;

namespace Zongsoft.Externals.Polly.Strategies;

internal abstract class ThrottleStrategyOptionsBase : ResilienceStrategyOptions
{
	#region 常量定义
	internal const int QUEUE_LIMIT = 0;
	internal const int PERMIT_LIMIT = 1000;
	#endregion

	#region 构造函数
	protected ThrottleStrategyOptionsBase()
	{
		this.Name = "RateLimiter";
		this.DefaultRateLimiterOptions = new()
		{
			QueueLimit = QUEUE_LIMIT,
			PermitLimit = PERMIT_LIMIT,
		};
	}
	#endregion

	#region 公共属性
	public ConcurrencyLimiterOptions DefaultRateLimiterOptions { get; set; }
	public Func<RateLimiterArguments, ValueTask<RateLimitLease>> RateLimiter { get; set; }
	#endregion
}

internal sealed class ThrottleStrategyOptions : ThrottleStrategyOptionsBase
{
	#region 公共属性
	public Func<ThrottleArgument, CancellationToken, ValueTask<bool>> OnRejected { get; set; }
	#endregion
}

internal sealed class ThrottleStrategyOptions<TArgument> : ThrottleStrategyOptionsBase
{
	#region 公共属性
	public Func<ThrottleArgument<TArgument>, CancellationToken, ValueTask<bool>> OnRejected { get; set; }
	#endregion
}

internal sealed class ThrottleStrategyOptions<TArgument, TResult> : ThrottleStrategyOptionsBase
{
	#region 公共属性
	public Func<ThrottleArgument<TArgument, TResult>, CancellationToken, ValueTask<bool>> OnRejected { get; set; }
	#endregion
}
