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
using Polly.CircuitBreaker;

using Zongsoft.Components;
using Zongsoft.Components.Features;

namespace Zongsoft.Externals.Polly;

partial class FeatureExtension
{
	public static CircuitBreakerStrategyOptions ToStrategy(this BreakerFeature feature)
	{
		var options = new CircuitBreakerStrategyOptions
		{
			BreakDuration = feature.Duration,
			FailureRatio = feature.FailureRatio,
			SamplingDuration = feature.FailurePeriod,
			MinimumThroughput = feature.Threshold,
		};

		if(feature.DurationFactory != null)
			options.BreakDurationGenerator = args => ValueTask.FromResult(feature.DurationFactory(feature, args.FailureCount, args.FailureRate));

		if(feature.Predicator != null)
			options.ShouldHandle = args => feature.Predicator.PredicateAsync(new(args.Outcome.Exception.Wrap()), args.Context.CancellationToken);

		if(feature.Closed != null)
			options.OnClosed = args => feature.Closed(new(args.Outcome.Exception.Wrap()), args.Context.CancellationToken);

		if(feature.Opened != null)
		{
			options.OnOpened = args => feature.Opened(new(false, args.Outcome.Exception.Wrap()), args.Context.CancellationToken);
			options.OnHalfOpened = args => feature.Opened(new(true), args.Context.CancellationToken);
		}

		return options;
	}

	public static CircuitBreakerStrategyOptions ToStrategy<T>(this BreakerFeature<T> feature)
	{
		var options = new CircuitBreakerStrategyOptions
		{
			BreakDuration = feature.Duration,
			FailureRatio = feature.FailureRatio,
			SamplingDuration = feature.FailurePeriod,
			MinimumThroughput = feature.Threshold,
		};

		if(feature.DurationFactory != null)
			options.BreakDurationGenerator = args => ValueTask.FromResult(feature.DurationFactory(feature, args.FailureCount, args.FailureRate));

		if(feature.Predicator != null)
			options.ShouldHandle = args => feature.Predicator.PredicateAsync(new(args.Outcome.Exception.Wrap()), args.Context.CancellationToken);

		if(feature.Closed != null)
			options.OnClosed = args => feature.Closed(new(args.Outcome.Exception.Wrap()), args.Context.CancellationToken);

		if(feature.Opened != null)
		{
			options.OnOpened = args => feature.Opened(new(false, args.Outcome.Exception.Wrap()), args.Context.CancellationToken);
			options.OnHalfOpened = args => feature.Opened(new(true), args.Context.CancellationToken);
		}

		return options;
	}

	public static CircuitBreakerStrategyOptions<TResult> ToStrategy<T, TResult>(this BreakerFeature<T, TResult> feature)
	{
		var options = new CircuitBreakerStrategyOptions<TResult>
		{
			BreakDuration = feature.Duration,
			FailureRatio = feature.FailureRatio,
			SamplingDuration = feature.FailurePeriod,
			MinimumThroughput = feature.Threshold,
		};

		if(feature.DurationFactory != null)
			options.BreakDurationGenerator = args => ValueTask.FromResult(feature.DurationFactory(feature, args.FailureCount, args.FailureRate));

		if(feature.Predicator != null)
			options.ShouldHandle = args => feature.Predicator.PredicateAsync(GetArgument(args.Outcome), args.Context.CancellationToken);

		if(feature.Closed != null)
			options.OnClosed = args => feature.Closed(GetClosedArgument(args.Outcome), args.Context.CancellationToken);

		if(feature.Opened != null)
		{
			options.OnOpened = args => feature.Opened(GetOpenedArgument(args.Outcome, false), args.Context.CancellationToken);
			options.OnHalfOpened = args => feature.Opened(GetOpenedArgument(default, true), args.Context.CancellationToken);
		}

		return options;

		static Argument<T, TResult> GetArgument(Outcome<TResult> outcome) => new(default, outcome.Result is TResult result ? result : default, outcome.Exception.Wrap());
		static BreakerClosedArgument<T, TResult> GetClosedArgument(Outcome<TResult> outcome) => new(default, outcome.Result is TResult result ? result : default, outcome.Exception.Wrap());
		static BreakerOpenedArgument<T, TResult> GetOpenedArgument(Outcome<TResult> outcome, bool isHalf) => new(isHalf, default, outcome.Result is TResult result ? result : default, outcome.Exception.Wrap());
	}
}
