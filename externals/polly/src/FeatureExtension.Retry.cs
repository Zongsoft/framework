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

using Zongsoft.Components;
using Zongsoft.Components.Features;

namespace Zongsoft.Externals.Polly;

partial class FeatureExtension
{
	public static RetryStrategyOptions ToStrategy(this RetryFeature feature)
	{
		if(!feature.Usable(feature => feature.Latency.HasValue))
			return null;

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
		if(!feature.Usable(feature => feature.Latency.HasValue))
			return null;

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
		if(!feature.Usable(feature => feature.Latency.HasValue))
			return null;

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
