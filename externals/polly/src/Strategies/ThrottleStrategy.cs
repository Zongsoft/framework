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
using System.Threading.RateLimiting;

using Polly;
using Polly.Telemetry;
using Polly.RateLimiting;

namespace Zongsoft.Externals.Polly.Strategies;

internal sealed class ThrottleStrategy : ResilienceStrategy, IDisposable, IAsyncDisposable
{
	private readonly ResilienceStrategyTelemetry _telemetry;

	public ThrottleStrategy(
		Func<RateLimiterArguments, ValueTask<RateLimitLease>> limiter,
		Func<OnRateLimiterRejectedArguments, ValueTask<bool>> onRejected,
		ResilienceStrategyTelemetry telemetry, RateLimiter wrapper)
	{
		this.Limiter = limiter;
		this.Wrapper = wrapper;
		this.OnLeaseRejected = onRejected;
		_telemetry = telemetry;
	}

	public RateLimiter Wrapper { get; }
	public Func<RateLimiterArguments, ValueTask<RateLimitLease>> Limiter { get; }
	public Func<OnRateLimiterRejectedArguments, ValueTask<bool>> OnLeaseRejected { get; }

	public void Dispose() => this.Wrapper?.Dispose();
	public ValueTask DisposeAsync()
	{
		if(this.Wrapper is not null)
			return this.Wrapper.DisposeAsync();
		return ValueTask.CompletedTask;
	}

	protected override async ValueTask<Outcome<TResult>> ExecuteCore<TResult, TState>(
		Func<ResilienceContext, TState, ValueTask<Outcome<TResult>>> callback,
		ResilienceContext context, TState state)
	{
		using var lease = await this.Limiter(new RateLimiterArguments(context)).ConfigureAwait(context.ContinueOnCapturedContext);

		if(lease.IsAcquired)
			return await callback(context, state).ConfigureAwait(context.ContinueOnCapturedContext);

		TimeSpan? retryAfter = null;

		if(lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfterValue))
			retryAfter = retryAfterValue;

		var args = new OnRateLimiterRejectedArguments(context, lease);
		_telemetry.Report(new(ResilienceEventSeverity.Error, "OnRateLimiterRejected"), context, args);

		if(this.OnLeaseRejected != null)
		{
			var handled = await this.OnLeaseRejected(new OnRateLimiterRejectedArguments(context, lease)).ConfigureAwait(context.ContinueOnCapturedContext);
			if(handled)
				return Outcome.FromResult<TResult>(default);
		}

		var innerException = retryAfter.HasValue
			? new RateLimiterRejectedException(retryAfterValue)
			: new RateLimiterRejectedException();

		var exception = retryAfter.HasValue
			? new Components.Features.ThrottleException(context.OperationKey, retryAfterValue, innerException.Message, innerException)
			: new Components.Features.ThrottleException(context.OperationKey, TimeSpan.MinValue, innerException.Message, innerException);

		TrySetStackTrace(innerException);
		_telemetry.SetTelemetrySource(innerException);

		return Outcome.FromException<TResult>(exception);
	}

	private static TException TrySetStackTrace<TException>(TException exception) where TException : Exception
	{
		if(string.IsNullOrWhiteSpace(exception.StackTrace))
			System.Runtime.ExceptionServices.ExceptionDispatchInfo.SetCurrentStackTrace(exception);

		return exception;
	}
}

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
}