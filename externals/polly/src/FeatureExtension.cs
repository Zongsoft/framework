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
using System.Threading;
using System.Threading.Tasks;

using Polly;
using Polly.Retry;
using Polly.Timeout;
using Polly.Fallback;
using Polly.RateLimiting;
using Polly.CircuitBreaker;

using Zongsoft.Components;
using Zongsoft.Components.Features;

namespace Zongsoft.Externals.Polly;

internal static class FeatureExtension
{
	public static ResilienceStrategyOptions GetStrategy(this IFeature feature) => feature switch
	{
		RetryFeature retry => retry.ToStrategy(),
		TimeoutFeature timeout => timeout.ToStrategy(),
		BreakerFeature breaker => breaker.ToStrategy(),
		ThrottleFeature throttle => throttle.ToStrategy(),
		FallbackFeature fallback => fallback.ToStrategy(),
		_ => null
	};

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

	public static TimeoutStrategyOptions ToStrategy(this TimeoutFeature feature) =>
		feature.Usable(feature => feature.Timeout > TimeSpan.Zero) ? new TimeoutStrategyOptions() { Timeout = feature.Timeout } : null;

	public static RetryStrategyOptions ToStrategy(this RetryFeature feature)
	{
		if(!feature.Usable(feature => feature.Latency.HasValue))
			return null;

		var strategy = new RetryStrategyOptions
		{
			Delay = feature.Latency.Value,
			MaxDelay = feature.Latency.Limit,
			MaxRetryAttempts = feature.Attempts,
			UseJitter = feature.Jitterable,
			BackoffType = GetBackoffType(feature.Mode),
		};

		if(feature.Latency.Generator != null)
			strategy.DelayGenerator = argument => ValueTask.FromResult<TimeSpan?>(feature.Latency.Generator(feature, argument.AttemptNumber));

		return strategy;

		static DelayBackoffType GetBackoffType(RetryFeature.BackoffMode mode) => mode switch
		{
			RetryFeature.BackoffMode.None => DelayBackoffType.Constant,
			RetryFeature.BackoffMode.Linear => DelayBackoffType.Linear,
			RetryFeature.BackoffMode.Exponential => DelayBackoffType.Exponential,
			_ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
		};
	}

	public static RetryStrategyOptions<TResult> ToStrategy<TResult>(this RetryFeature feature)
	{
		if(!feature.Usable(feature => feature.Latency.HasValue))
			return null;

		var strategy = new RetryStrategyOptions<TResult>
		{
			Delay = feature.Latency.Value,
			MaxDelay = feature.Latency.Limit,
			MaxRetryAttempts = feature.Attempts,
			UseJitter = feature.Jitterable,
			BackoffType = GetBackoffType(feature.Mode),
		};

		if(feature.Latency.Generator != null)
			strategy.DelayGenerator = argument => ValueTask.FromResult<TimeSpan?>(feature.Latency.Generator(feature, argument.AttemptNumber));

		return strategy;

		static DelayBackoffType GetBackoffType(RetryFeature.BackoffMode mode) => mode switch
		{
			RetryFeature.BackoffMode.None => DelayBackoffType.Constant,
			RetryFeature.BackoffMode.Linear => DelayBackoffType.Linear,
			RetryFeature.BackoffMode.Exponential => DelayBackoffType.Exponential,
			_ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
		};
	}

	public static CircuitBreakerStrategyOptions ToStrategy(this BreakerFeature feature)
	{
		if(!feature.Usable(feature => feature.Duration > TimeSpan.Zero && feature.Threshold > 0))
			return null;

		var strategy = new CircuitBreakerStrategyOptions
		{
			BreakDuration = feature.Duration,
			FailureRatio = feature.FailureRatio,
			SamplingDuration = feature.FailurePeriod,
			MinimumThroughput = feature.Threshold,
		};

		if(feature.DurationFactory != null)
			strategy.BreakDurationGenerator = argument => ValueTask.FromResult(feature.DurationFactory(feature, argument.FailureCount, argument.FailureRate));

		return strategy;
	}

	public static CircuitBreakerStrategyOptions<TResult> ToStrategy<TResult>(this BreakerFeature feature)
	{
		if(!feature.Usable(feature => feature.Duration > TimeSpan.Zero && feature.Threshold > 0))
			return null;

		var strategy = new CircuitBreakerStrategyOptions<TResult>
		{
			BreakDuration = feature.Duration,
			FailureRatio = feature.FailureRatio,
			SamplingDuration = feature.FailurePeriod,
			MinimumThroughput = feature.Threshold,
		};

		if(feature.DurationFactory != null)
			strategy.BreakDurationGenerator = argument => ValueTask.FromResult(feature.DurationFactory(feature, argument.FailureCount, argument.FailureRate));

		return strategy;
	}

	public static RateLimiterStrategyOptions ToStrategy(this ThrottleFeature feature)
	{
		if(!feature.Usable())
			return null;

		var strategy = new RateLimiterStrategyOptions
		{
			DefaultRateLimiterOptions = new()
			{
			},
		};

		return strategy;
	}

	public static FallbackStrategyOptions<object> ToStrategy(this FallbackFeature feature)
	{
		if(!feature.Usable(feature => feature.Fallback != null))
			return null;

		var strategy = new FallbackStrategyOptions<object>
		{
			FallbackAction = async argument =>
			{
				await feature.Fallback(null);
				return Outcome.FromResult<object>(null);
			},
			OnFallback = argument => feature.Fallback(null),
		};

		return strategy;
	}

	public static FallbackStrategyOptions<TResult> ToStrategy<TResult>(this FallbackFeature<TResult> feature)
	{
		if(!feature.Usable(feature => feature.Fallback != null))
			return null;

		var strategy = new FallbackStrategyOptions<TResult>
		{
			FallbackAction = async argument =>
			{
				await feature.Fallback(null);
				return Outcome.FromResult<TResult>(default);
			},
		};

		return strategy;
	}
}
