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
using Polly.Fallback;
using Polly.Telemetry;

using Zongsoft.Components.Features;

namespace Zongsoft.Externals.Polly.Strategies;

internal sealed class FallbackStrategy : ResilienceStrategy
{
	private readonly ResilienceStrategyTelemetry _telemetry;
	private readonly Func<Argument, CancellationToken, ValueTask<bool>> _predicate;
	private readonly Func<Argument, CancellationToken, ValueTask> _fallback;

	public FallbackStrategy(FallbackStrategyOptions options, ResilienceStrategyTelemetry telemetry)
	{
		_fallback = options.Fallback;
		_predicate = options.ShouldHandle;
		_telemetry = telemetry;
	}

	protected override async ValueTask<Outcome<TResult>> ExecuteCore<TResult, TState>(Func<ResilienceContext, TState, ValueTask<Outcome<TResult>>> callback, ResilienceContext context, TState state)
	{
		Outcome<TResult> outcome;

		try
		{
			outcome = await callback(context, state).ConfigureAwait(context.ContinueOnCapturedContext);
		}
		catch(Exception ex)
		{
			outcome = Outcome.FromException<TResult>(ex);
		}

		var argument = new Argument(outcome.Exception.Wrap());
		var predicate = _predicate;
		if(predicate != null && !await predicate(argument, context.CancellationToken).ConfigureAwait(context.ContinueOnCapturedContext))
			return outcome;

		_telemetry.Report(
			new(ResilienceEventSeverity.Warning, "OnFallback"), context, new OnFallbackArguments<TResult>(context, outcome));

		try
		{
			await _fallback(argument, context.CancellationToken).ConfigureAwait(context.ContinueOnCapturedContext);
			return outcome;
		}
		catch(Exception ex)
		{
			return Outcome.FromException<TResult>(ex);
		}
	}
}

internal sealed class FallbackStrategy<TArgument> : ResilienceStrategy
{
	private readonly ResilienceStrategyTelemetry _telemetry;
	private readonly Func<Argument<TArgument>, CancellationToken, ValueTask<bool>> _predicate;
	private readonly Func<Argument<TArgument>, CancellationToken, ValueTask> _fallback;

	public FallbackStrategy(FallbackStrategyOptions<TArgument> options, ResilienceStrategyTelemetry telemetry)
	{
		_fallback = options.Fallback;
		_predicate = options.ShouldHandle;
		_telemetry = telemetry;
	}

	protected override async ValueTask<Outcome<TResult>> ExecuteCore<TResult, TState>(Func<ResilienceContext, TState, ValueTask<Outcome<TResult>>> callback, ResilienceContext context, TState state)
	{
		Outcome<TResult> outcome;

		try
		{
			outcome = await callback(context, state).ConfigureAwait(context.ContinueOnCapturedContext);
		}
		catch(Exception ex)
		{
			outcome = Outcome.FromException<TResult>(ex);
		}

		var argument = new Argument<TArgument>(FeatureUtility.GetArgument<TState, TArgument>(state), outcome.Exception.Wrap());
		var predicate = _predicate;
		if(predicate != null && !await predicate(argument, context.CancellationToken).ConfigureAwait(context.ContinueOnCapturedContext))
			return outcome;

		_telemetry.Report(
			new(ResilienceEventSeverity.Warning, "OnFallback"), context, new OnFallbackArguments<TResult>(context, outcome));

		try
		{
			await _fallback(argument, context.CancellationToken).ConfigureAwait(context.ContinueOnCapturedContext);
			return outcome;
		}
		catch(Exception ex)
		{
			return Outcome.FromException<TResult>(ex);
		}
	}
}

internal sealed class FallbackStrategy<TArgument, TResult> : ResilienceStrategy<TResult>
{
	private readonly ResilienceStrategyTelemetry _telemetry;
	private readonly Func<Argument<TArgument, TResult>, CancellationToken, ValueTask<bool>> _predicate;
	private readonly Func<Argument<TArgument, TResult>, CancellationToken, ValueTask<TResult>> _fallback;

	public FallbackStrategy(FallbackStrategyOptions<TArgument, TResult> options, ResilienceStrategyTelemetry telemetry)
	{
		_fallback = options.Fallback;
		_predicate = options.ShouldHandle;
		_telemetry = telemetry;
	}

	protected override async ValueTask<Outcome<TResult>> ExecuteCore<TState>(Func<ResilienceContext, TState, ValueTask<Outcome<TResult>>> callback, ResilienceContext context, TState state)
	{
		Outcome<TResult> outcome;

		try
		{
			outcome = await callback(context, state).ConfigureAwait(context.ContinueOnCapturedContext);
		}
		catch(Exception ex)
		{
			outcome = Outcome.FromException<TResult>(ex);
		}

		var argument = new Argument<TArgument, TResult>(FeatureUtility.GetArgument<TState, TArgument>(state), outcome.Result, outcome.Exception.Wrap());
		var predicate = _predicate;
		if(predicate != null && !await predicate(argument, context.CancellationToken).ConfigureAwait(context.ContinueOnCapturedContext))
			return outcome;

		_telemetry.Report(
			new(ResilienceEventSeverity.Warning, "OnFallback"), context, new OnFallbackArguments<TResult>(context, outcome));

		try
		{
			var result = await _fallback(argument, context.CancellationToken).ConfigureAwait(context.ContinueOnCapturedContext);
			return Outcome.FromResult(result);
		}
		catch(Exception ex)
		{
			return Outcome.FromException<TResult>(ex);
		}
	}
}
