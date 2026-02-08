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
using Polly.Telemetry;
using Polly.RateLimiting;

using Zongsoft.Components.Features;

namespace Zongsoft.Externals.Polly.Strategies;

internal abstract class ThrottleStrategyBase : ResilienceStrategy, IDisposable, IAsyncDisposable
{
	private readonly ResilienceStrategyTelemetry _telemetry;

	protected ThrottleStrategyBase(
		Func<RateLimiterArguments, ValueTask<RateLimitLease>> limiter,
		ResilienceStrategyTelemetry telemetry, RateLimiter wrapper)
	{
		this.Limiter = limiter;
		this.Wrapper = wrapper;
		_telemetry = telemetry;
	}

	public RateLimiter Wrapper { get; }
	public Func<RateLimiterArguments, ValueTask<RateLimitLease>> Limiter { get; }

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

		var handled = await this.OnRejected(context.OperationKey, state, lease, retryAfter, context.CancellationToken).ConfigureAwait(context.ContinueOnCapturedContext);
		if(handled)
			return Outcome.FromResult<TResult>(default);

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

	protected abstract ValueTask<bool> OnRejected<T>(string name, T state, RateLimitLease lease, TimeSpan? retryAfter, CancellationToken cancellation);

	private static TException TrySetStackTrace<TException>(TException exception) where TException : Exception
	{
		if(string.IsNullOrWhiteSpace(exception.StackTrace))
			System.Runtime.ExceptionServices.ExceptionDispatchInfo.SetCurrentStackTrace(exception);

		return exception;
	}
}

internal sealed class ThrottleStrategy : ThrottleStrategyBase
{
	private readonly Func<ThrottleArgument, CancellationToken, ValueTask<bool>> _rejected;

	public ThrottleStrategy(
		Func<RateLimiterArguments, ValueTask<RateLimitLease>> limiter,
		Func<ThrottleArgument, CancellationToken, ValueTask<bool>> rejected,
		ResilienceStrategyTelemetry telemetry, RateLimiter wrapper) : base(limiter, telemetry, wrapper)
	{
		_rejected = rejected;
	}

	protected override ValueTask<bool> OnRejected<T>(string name, T state, RateLimitLease lease, TimeSpan? retryAfter, CancellationToken cancellation) =>
		_rejected(new(name, new ThrottleLeaseWrapper(lease)), cancellation);
}

internal sealed class ThrottleStrategy<TArgument> : ThrottleStrategyBase
{
	private readonly Func<ThrottleArgument<TArgument>, CancellationToken, ValueTask<bool>> _rejected;

	public ThrottleStrategy(
		Func<RateLimiterArguments, ValueTask<RateLimitLease>> limiter,
		Func<ThrottleArgument<TArgument>, CancellationToken, ValueTask<bool>> rejected,
		ResilienceStrategyTelemetry telemetry, RateLimiter wrapper) : base(limiter, telemetry, wrapper)
	{
		_rejected = rejected;
	}

	protected override ValueTask<bool> OnRejected<T>(string name, T state, RateLimitLease lease, TimeSpan? retryAfter, CancellationToken cancellation) =>
		_rejected(new(name, new ThrottleLeaseWrapper(lease), state is TArgument argument ? argument : default), cancellation);
}

internal sealed class ThrottleStrategy<TArgument, TResult> : ThrottleStrategyBase
{
	private readonly Func<ThrottleArgument<TArgument, TResult>, CancellationToken, ValueTask<bool>> _rejected;

	public ThrottleStrategy(
		Func<RateLimiterArguments, ValueTask<RateLimitLease>> limiter,
		Func<ThrottleArgument<TArgument, TResult>, CancellationToken, ValueTask<bool>> rejected,
		ResilienceStrategyTelemetry telemetry, RateLimiter wrapper) : base(limiter, telemetry, wrapper)
	{
		_rejected = rejected;
	}

	protected override ValueTask<bool> OnRejected<T>(string name, T state, RateLimitLease lease, TimeSpan? retryAfter, CancellationToken cancellation) =>
		_rejected(new(name, new ThrottleLeaseWrapper(lease), state is TArgument argument ? argument : default), cancellation);
}

internal sealed class ThrottleLeaseWrapper(RateLimitLease lease) : ThrottleLease
{
	private readonly RateLimitLease _lease = lease;
	private readonly MetadataCollection _metadata = new(lease);

	public override bool IsLeased => _lease.IsAcquired;
	public override IMetadataCollection Metadata => _metadata;
	protected override void Dispose(bool disposing) => _lease.Dispose();

	private sealed class MetadataCollection(RateLimitLease lease) : IMetadataCollection
	{
		private readonly RateLimitLease _lease = lease;
		public int Count => _lease.MetadataNames.Count();
		public bool TryGetValue<T>(string key, out T value) => _lease.TryGetMetadata(new MetadataName<T>(key), out value);
		public bool TryGetValue(string key, out object value) => _lease.TryGetMetadata(key, out value);
		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
		{
			foreach(var metadata in _lease.GetAllMetadata())
				yield return metadata;
		}
	}
}