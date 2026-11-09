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
using System.Threading.RateLimiting;

using Polly;

namespace Zongsoft.Externals.Polly.Strategies;

internal static class ThrottleStrategyExtension
{
	public static ResiliencePipelineBuilderBase AddThrottle(this ResiliencePipelineBuilderBase builder, ThrottleStrategyOptions options)
	{
		ArgumentNullException.ThrowIfNull(builder);
		ArgumentNullException.ThrowIfNull(options);

		return builder.AddStrategy(context =>
		{
			RateLimiter wrapper = default;
			var limiter = options.RateLimiter;

			if(limiter is null)
			{
				var defaultLimiter = new ConcurrencyLimiter(options.DefaultRateLimiterOptions);
				wrapper = defaultLimiter;
				limiter = args => defaultLimiter.AcquireAsync(1, args.Context.CancellationToken);
			}

			return new ThrottleStrategy(
				limiter,
				options.OnRejected,
				context.Telemetry,
				wrapper);
		}, options);
	}

	public static ResiliencePipelineBuilderBase AddThrottle<T>(this ResiliencePipelineBuilderBase builder, ThrottleStrategyOptions<T> options)
	{
		ArgumentNullException.ThrowIfNull(builder);
		ArgumentNullException.ThrowIfNull(options);

		return builder.AddStrategy(context =>
		{
			RateLimiter wrapper = default;
			var limiter = options.RateLimiter;

			if(limiter is null)
			{
				var defaultLimiter = new ConcurrencyLimiter(options.DefaultRateLimiterOptions);
				wrapper = defaultLimiter;
				limiter = args => defaultLimiter.AcquireAsync(1, args.Context.CancellationToken);
			}

			return new ThrottleStrategy<T>(
				limiter,
				options.OnRejected,
				context.Telemetry,
				wrapper);
		}, options);
	}

	public static ResiliencePipelineBuilderBase AddThrottle<T, TResult>(this ResiliencePipelineBuilderBase builder, ThrottleStrategyOptions<T, TResult> options)
	{
		ArgumentNullException.ThrowIfNull(builder);
		ArgumentNullException.ThrowIfNull(options);

		return builder.AddStrategy(context =>
		{
			RateLimiter wrapper = default;
			var limiter = options.RateLimiter;

			if(limiter is null)
			{
				var defaultLimiter = new ConcurrencyLimiter(options.DefaultRateLimiterOptions);
				wrapper = defaultLimiter;
				limiter = args => defaultLimiter.AcquireAsync(1, args.Context.CancellationToken);
			}

			return new ThrottleStrategy<T, TResult>(
				limiter,
				options.OnRejected,
				context.Telemetry,
				wrapper);
		}, options);
	}
}
