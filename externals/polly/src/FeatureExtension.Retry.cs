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

using Polly;
using Polly.Retry;

using Zongsoft.Components;
using Zongsoft.Components.Features;

namespace Zongsoft.Externals.Polly;

partial class FeatureExtension
{
	public static RetryStrategyOptions ToStrategy(this RetryFeature feature)
	{
		var options = new RetryStrategyOptions
		{
			Delay = feature.Latency.Value,
			MaxDelay = feature.Latency.Limit > TimeSpan.Zero ? feature.Latency.Limit : null,
			MaxRetryAttempts = feature.Attempts > 0 ? feature.Attempts : int.MaxValue,
			UseJitter = feature.Jitterable,
			BackoffType = GetBackoffType(feature.Backoff),
		};

		if(feature.Latency.Generator != null)
			options.DelayGenerator = args =>
			{
				var result = feature.Latency.Generator(feature, args.AttemptNumber);
				return ValueTask.FromResult<TimeSpan?>(result > TimeSpan.Zero ? result : null);
			};

		if(feature.Predicator != null)
			options.ShouldHandle = args => feature.Predicator.PredicateAsync(new RetryArgument(args.AttemptNumber, args.Outcome.Exception.Wrap()), args.Context.CancellationToken);

		if(feature.OnRetry != null)
			options.OnRetry = args => feature.OnRetry(new(args.AttemptNumber, args.Outcome.Exception.Wrap()), args.Context.CancellationToken);

		return options;
	}

	public static RetryStrategyOptions ToStrategy<TArgument>(this RetryFeature<TArgument> feature)
	{
		var options = new RetryStrategyOptions
		{
			Delay = feature.Latency.Value,
			MaxDelay = feature.Latency.Limit > TimeSpan.Zero ? feature.Latency.Limit : null,
			MaxRetryAttempts = feature.Attempts > 0 ? feature.Attempts : int.MaxValue,
			UseJitter = feature.Jitterable,
			BackoffType = GetBackoffType(feature.Backoff),
		};

		if(feature.Latency.Generator != null)
			options.DelayGenerator = args =>
			{
				var result = feature.Latency.Generator(feature, args.AttemptNumber);
				return ValueTask.FromResult<TimeSpan?>(result > TimeSpan.Zero ? result : null);
			};

		if(feature.Predicator != null)
			options.ShouldHandle = args => feature.Predicator.PredicateAsync(new RetryArgument<TArgument>(args.AttemptNumber, default, args.Outcome.Exception.Wrap()), args.Context.CancellationToken);

		if(feature.OnRetry != null)
			options.OnRetry = args => feature.OnRetry(new(args.AttemptNumber, default, args.Outcome.Exception.Wrap()), args.Context.CancellationToken);

		return options;
	}

	public static RetryStrategyOptions<TResult> ToStrategy<TArgument, TResult>(this RetryFeature<TArgument, TResult> feature)
	{
		var options = new RetryStrategyOptions<TResult>
		{
			Delay = feature.Latency.Value,
			MaxDelay = feature.Latency.Limit > TimeSpan.Zero ? feature.Latency.Limit : null,
			MaxRetryAttempts = feature.Attempts > 0 ? feature.Attempts : int.MaxValue,
			UseJitter = feature.Jitterable,
			BackoffType = GetBackoffType(feature.Backoff),
		};

		if(feature.Latency.Generator != null)
			options.DelayGenerator = args =>
			{
				var result = feature.Latency.Generator(feature, args.AttemptNumber);
				return ValueTask.FromResult<TimeSpan?>(result > TimeSpan.Zero ? result : null);
			};

		if(feature.Predicator != null)
			options.ShouldHandle = args => feature.Predicator.PredicateAsync(new RetryArgument<TResult>(args.AttemptNumber, args.Outcome.Result, args.Outcome.Exception.Wrap()), args.Context.CancellationToken);

		if(feature.OnRetry != null)
			options.OnRetry = args => feature.OnRetry(new(args.AttemptNumber, default, args.Outcome.Result, args.Outcome.Exception.Wrap()), args.Context.CancellationToken);

		return options;
	}

	private static DelayBackoffType GetBackoffType(RetryBackoff backoff) => backoff switch
	{
		RetryBackoff.None => DelayBackoffType.Constant,
		RetryBackoff.Linear => DelayBackoffType.Linear,
		RetryBackoff.Exponential => DelayBackoffType.Exponential,
		_ => throw new ArgumentOutOfRangeException(nameof(backoff), backoff, null)
	};
}
