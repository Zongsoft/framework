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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.Polly library.
 *
 * The Zongsoft.Externals.Polly is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Polly is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Polly library. If not, see <http://www.gnu.org/licenses/>.
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
