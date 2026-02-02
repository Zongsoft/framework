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

using Polly;
using Polly.Timeout;

using Zongsoft.Components;
using Zongsoft.Components.Features;

namespace Zongsoft.Externals.Polly;

internal static partial class FeatureExtension
{
	public static void AddStrategy(this ResiliencePipelineBuilder builder, IFeature feature)
	{
		if(builder == null || !feature.Usable())
			return;

		switch(feature)
		{
			case RetryFeature retry:
				builder.AddRetry(retry.ToStrategy());
				break;
			case TimeoutFeature timeout:
				builder.AddTimeout(timeout.ToStrategy());
				break;
			case ThrottleFeature throttle:
				builder.AddRateLimiter(throttle.ToStrategy());
				break;
			case BreakerFeature breaker:
				builder.AddCircuitBreaker(breaker.ToStrategy());
				break;
		}
	}

	public static void AddStrategy<TResult>(this ResiliencePipelineBuilder<TResult> builder, IFeature feature)
	{
		if(builder == null || !feature.Usable())
			return;

		switch(feature)
		{
			case RetryFeature retry:
				builder.AddRetry(retry.ToStrategy<TResult>());
				break;
			case TimeoutFeature timeout:
				builder.AddTimeout(timeout.ToStrategy());
				break;
			case ThrottleFeature throttle:
				builder.AddRateLimiter(throttle.ToStrategy());
				break;
			case BreakerFeature breaker:
				builder.AddCircuitBreaker(breaker.ToStrategy<TResult>());
				break;
			case FallbackFeature<TResult> fallback:
				builder.AddFallback(fallback.ToStrategy());
				break;
		}
	}

	private static Argument GetArgument(this Outcome<object> outcome) => new(outcome.Result, outcome.Exception.GetException());
	private static Argument<T> GetArgument<T>(this Outcome<T> outcome) => new(outcome.Result, outcome.Exception.GetException());
	private static Exception GetException(this Exception exception) => exception switch
	{
		TimeoutRejectedException => new TimeoutException(),
		_ => exception,
	};
}
