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

using Polly;
using Polly.Timeout;

using Zongsoft.Components;
using Zongsoft.Components.Features;

namespace Zongsoft.Externals.Polly;

internal static partial class FeatureExtension
{
	public static void AddStrategy(this ResiliencePipelineBuilder builder, IFeature feature)
	{
		if(builder == null || feature == null)
			return;

		switch(feature)
		{
			case RetryFeature retry:
				var retryOptions = retry.ToStrategy();
				if(retryOptions != null)
					builder.AddRetry(retryOptions);
				break;
			case TimeoutFeature timeout:
				var timeoutOptions = timeout.ToStrategy();
				if(timeoutOptions != null)
					builder.AddTimeout(timeoutOptions);
				break;
			case BreakerFeature breaker:
				var breakerOptions = breaker.ToStrategy();
				if(breakerOptions != null)
					builder.AddCircuitBreaker(breakerOptions);
				break;
			case ThrottleFeature throttle:
				var throttleOptions = throttle.ToStrategy();
				if(throttleOptions != null)
					Strategies.ThrottleStrategyExtension.AddThrottle(builder, throttleOptions);
				break;
			case FallbackFeature fallback:
				var fallbackOptions = fallback.ToStrategy();
				if(fallbackOptions != null)
					builder.AddFallback(fallbackOptions);
				break;
		}
	}

	public static void AddStrategy<TArgument>(this ResiliencePipelineBuilder builder, IFeature feature)
	{
		if(builder == null || feature == null)
			return;

		switch(feature)
		{
			case RetryFeature<TArgument> retry:
				var retryOptions = retry.ToStrategy();
				if(retryOptions != null)
					builder.AddRetry(retryOptions);
				break;
			case TimeoutFeature<TArgument> timeout:
				var timeoutOptions = timeout.ToStrategy();
				if(timeoutOptions != null)
					builder.AddTimeout(timeoutOptions);
				break;
			case BreakerFeature<TArgument> breaker:
				var breakerOptions = breaker.ToStrategy();
				if(breakerOptions != null)
					builder.AddCircuitBreaker(breakerOptions);
				break;
			case ThrottleFeature<TArgument> throttle:
				var throttleOptions = throttle.ToStrategy();
				if(throttleOptions != null)
					Strategies.ThrottleStrategyExtension.AddThrottle(builder, throttleOptions);
				break;
			case FallbackFeature<TArgument> fallback:
				var fallbackOptions = fallback.ToStrategy();
				if(fallbackOptions != null)
					builder.AddFallback(fallbackOptions);
				break;
			default:
				AddStrategy(builder, feature);
				break;
		}
	}

	public static void AddStrategy<TArgument, TResult>(this ResiliencePipelineBuilder<TResult> builder, IFeature feature)
	{
		if(builder == null || feature == null)
			return;

		switch(feature)
		{
			case RetryFeature<TArgument, TResult> retry:
				var retryOptions = retry.ToStrategy();
				if(retryOptions != null)
					builder.AddRetry(retryOptions);
				break;
			case TimeoutFeature<TArgument, TResult> timeout:
				var timeoutOptions = timeout.ToStrategy();
				if(timeoutOptions != null)
					builder.AddTimeout(timeoutOptions);
				break;
			case BreakerFeature<TArgument, TResult> breaker:
				var breakerOptions = breaker.ToStrategy();
				if(breakerOptions != null)
					builder.AddCircuitBreaker(breakerOptions);
				break;
			case ThrottleFeature<TArgument, TResult> throttle:
				var throttleOptions = throttle.ToStrategy();
				if(throttleOptions != null)
					Strategies.ThrottleStrategyExtension.AddThrottle(builder, throttleOptions);
				break;
			case FallbackFeature<TArgument, TResult> fallback:
				var fallbackOptions = fallback.ToStrategy();
				if(fallbackOptions != null)
					builder.AddFallback(fallbackOptions);
				break;
		}
	}

	internal static Exception Wrap(this Exception exception) => exception switch
	{
		TimeoutRejectedException => new TimeoutException(),
		_ => exception,
	};
}
