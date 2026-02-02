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
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.RateLimiting;

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
			MaxDelay = feature.Latency.Limit > TimeSpan.Zero ? feature.Latency.Limit : null,
			MaxRetryAttempts = feature.Attempts > 0 ? feature.Attempts : int.MaxValue,
			UseJitter = feature.Jitterable,
			BackoffType = GetBackoffType(feature.Backoff),
		};

		if(feature.Latency.Generator != null)
			strategy.DelayGenerator = argument =>
			{
				var result = feature.Latency.Generator(feature, argument.AttemptNumber);
				return ValueTask.FromResult<TimeSpan?>(result > TimeSpan.Zero ? result : null);
			};

		if(feature.Predicator != null)
			strategy.ShouldHandle = argument => feature.Predicator.PredicateAsync(new RetryArgument(argument.AttemptNumber, argument.Outcome.Result, argument.Outcome.Exception.GetException()), argument.Context.CancellationToken);

		return strategy;
	}

	public static RetryStrategyOptions<TResult> ToStrategy<TResult>(this RetryFeature feature)
	{
		if(!feature.Usable(feature => feature.Latency.HasValue))
			return null;

		var strategy = new RetryStrategyOptions<TResult>
		{
			Delay = feature.Latency.Value,
			MaxDelay = feature.Latency.Limit > TimeSpan.Zero ? feature.Latency.Limit : null,
			MaxRetryAttempts = feature.Attempts > 0 ? feature.Attempts : int.MaxValue,
			UseJitter = feature.Jitterable,
			BackoffType = GetBackoffType(feature.Backoff),
		};

		if(feature.Latency.Generator != null)
			strategy.DelayGenerator = argument =>
			{
				var result = feature.Latency.Generator(feature, argument.AttemptNumber);
				return ValueTask.FromResult<TimeSpan?>(result > TimeSpan.Zero ? result : null);
			};

		if(feature.Predicator != null)
			strategy.ShouldHandle = argument => feature.Predicator.PredicateAsync(new RetryArgument<TResult>(argument.AttemptNumber, argument.Outcome.Result, argument.Outcome.Exception.GetException()), argument.Context.CancellationToken);

		return strategy;
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

		if(feature.Predicator != null)
			strategy.ShouldHandle = argument => feature.Predicator.PredicateAsync(argument.Outcome.GetArgument(), argument.Context.CancellationToken);

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

		if(feature.Predicator != null)
			strategy.ShouldHandle = argument => feature.Predicator.PredicateAsync(argument.Outcome.GetArgument(), argument.Context.CancellationToken);

		return strategy;
	}

	public static RateLimiterStrategyOptions ToStrategy(this ThrottleFeature feature)
	{
		if(!feature.Usable())
			return null;

		var strategy = new RateLimiterStrategyOptions();

		if(feature.Rejected != null)
			strategy.OnRejected = arguments => feature.Rejected.HandleAsync(new PollyThrottleArgument(arguments), arguments.Context.CancellationToken);

		if(feature.PermitLimit > 0)
			strategy.DefaultRateLimiterOptions.PermitLimit = feature.PermitLimit;
		if(feature.QueueLimit > 0)
			strategy.DefaultRateLimiterOptions.QueueLimit = feature.QueueLimit;

		strategy.DefaultRateLimiterOptions.QueueProcessingOrder = feature.QueueOrder switch
		{
			ThrottleQueueOrder.Oldest => QueueProcessingOrder.OldestFirst,
			ThrottleQueueOrder.Newest => QueueProcessingOrder.NewestFirst,
			_ => QueueProcessingOrder.OldestFirst,
		};

		switch(feature.Limiter)
		{
			case ThrottleLimiter.TokenBucket limiter:
				var tokenOptions = new TokenBucketRateLimiterOptions()
				{
					TokenLimit = strategy.DefaultRateLimiterOptions.PermitLimit,
					QueueLimit = strategy.DefaultRateLimiterOptions.QueueLimit,
					QueueProcessingOrder = strategy.DefaultRateLimiterOptions.QueueProcessingOrder,
					TokensPerPeriod = limiter.Threshold,
					ReplenishmentPeriod = limiter.Period,
				};

				strategy.RateLimiter = argument => (new TokenBucketRateLimiter(tokenOptions)).AcquireAsync(cancellationToken: argument.Context.CancellationToken);
				break;
			case ThrottleLimiter.FixedWindown limiter:
				var fixedOptions = new FixedWindowRateLimiterOptions()
				{
					PermitLimit = strategy.DefaultRateLimiterOptions.PermitLimit,
					QueueLimit = strategy.DefaultRateLimiterOptions.QueueLimit,
					QueueProcessingOrder = strategy.DefaultRateLimiterOptions.QueueProcessingOrder,
					Window = limiter.Window,
				};

				strategy.RateLimiter = argument => (new FixedWindowRateLimiter(fixedOptions)).AcquireAsync(cancellationToken: argument.Context.CancellationToken);
				break;
			case ThrottleLimiter.SlidingWindown limiter:
				var slidingOptions = new SlidingWindowRateLimiterOptions()
				{
					PermitLimit = strategy.DefaultRateLimiterOptions.PermitLimit,
					QueueLimit = strategy.DefaultRateLimiterOptions.QueueLimit,
					QueueProcessingOrder = strategy.DefaultRateLimiterOptions.QueueProcessingOrder,
					Window = limiter.Window,
					SegmentsPerWindow = limiter.WindowSize,
				};

				strategy.RateLimiter = argument => (new SlidingWindowRateLimiter(slidingOptions)).AcquireAsync(cancellationToken: argument.Context.CancellationToken);
				break;
		}

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
				try
				{
					var result = await feature.Fallback(argument.Outcome.GetArgument(), argument.Context.CancellationToken);
					return Outcome.FromResult(result);
				}
				catch(Exception ex)
				{
					return Outcome.FromException<TResult>(ex);
				}
			},
		};

		if(feature.Predicator != null)
			strategy.ShouldHandle = argument => feature.Predicator.PredicateAsync(argument.Outcome.GetArgument(), argument.Context.CancellationToken);

		return strategy;
	}

	private static Argument GetArgument(this Outcome<object> outcome) => new(outcome.Result, outcome.Exception.GetException());
	private static Argument<T> GetArgument<T>(this Outcome<T> outcome) => new(outcome.Result, outcome.Exception.GetException());
	private static Exception GetException(this Exception exception) => exception switch
	{
		TimeoutRejectedException => new TimeoutException(),
		_ => exception,
	};

	private static DelayBackoffType GetBackoffType(RetryBackoff backoff) => backoff switch
	{
		RetryBackoff.None => DelayBackoffType.Constant,
		RetryBackoff.Linear => DelayBackoffType.Linear,
		RetryBackoff.Exponential => DelayBackoffType.Exponential,
		_ => throw new ArgumentOutOfRangeException(nameof(backoff), backoff, null)
	};

	private sealed class PollyThrottleArgument(OnRateLimiterRejectedArguments arguments) : ThrottleArgument(arguments.Context.OperationKey, new PollyThrottleLease(arguments.Lease))
	{
		private sealed class PollyThrottleLease(RateLimitLease lease) : ThrottleLease(lease.IsAcquired, new MetadataCollection(lease))
		{
			public sealed class MetadataCollection(RateLimitLease lease) : IMetadataCollection
			{
				private readonly RateLimitLease _lease = lease;
				public int Count => _lease.MetadataNames.Count();

				public bool TryGetValue<T>(string key, out T value) => _lease.TryGetMetadata(new MetadataName<T>(key), out value);
				public bool TryGetValue(string key, out object value) => _lease.TryGetMetadata(key, out value);

				IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
				public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
				{
					foreach(var metadata in _lease.GetAllMetadata())
						yield return new KeyValuePair<string, object>(metadata.Key, metadata.Value);
				}
			}
		}
	}
}
