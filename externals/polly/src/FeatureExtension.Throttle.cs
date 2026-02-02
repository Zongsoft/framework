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
using System.Threading.RateLimiting;

using Polly;
using Polly.RateLimiting;

using Zongsoft.Components;
using Zongsoft.Components.Features;

namespace Zongsoft.Externals.Polly;

partial class FeatureExtension
{
	private const int PERMIT_LIMIT = 1000;
	private const int TOKEN_THRESHOLD = 1;
	private const int WINDOW_SIZE = 1;
	private const int WINDOW_SECONDS = 1;

	public static RateLimiterStrategyOptions ToStrategy(this ThrottleFeature feature)
	{
		if(!feature.Usable())
			return null;

		var strategy = new RateLimiterStrategyOptions();

		if(feature.Rejected != null)
			strategy.OnRejected = arguments => feature.Rejected.HandleAsync(new PollyThrottleArgument(arguments), arguments.Context.CancellationToken);

		if(feature.PermitLimit > 0)
			strategy.DefaultRateLimiterOptions.PermitLimit = feature.PermitLimit > 0 ? feature.PermitLimit : PERMIT_LIMIT;
		if(feature.QueueLimit > 0)
			strategy.DefaultRateLimiterOptions.QueueLimit = Math.Max(feature.QueueLimit, 0);

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
					TokensPerPeriod = limiter.Threshold > 0 ? limiter.Threshold : TOKEN_THRESHOLD,
					ReplenishmentPeriod = limiter.Period > TimeSpan.Zero ? limiter.Period : TimeSpan.FromSeconds(WINDOW_SECONDS),
				};

				strategy.RateLimiter = argument => (new TokenBucketRateLimiter(tokenOptions)).AcquireAsync(cancellationToken: argument.Context.CancellationToken);
				break;
			case ThrottleLimiter.FixedWindown limiter:
				var fixedOptions = new FixedWindowRateLimiterOptions()
				{
					PermitLimit = strategy.DefaultRateLimiterOptions.PermitLimit,
					QueueLimit = strategy.DefaultRateLimiterOptions.QueueLimit,
					QueueProcessingOrder = strategy.DefaultRateLimiterOptions.QueueProcessingOrder,
					Window = limiter.Window > TimeSpan.Zero ? limiter.Window : TimeSpan.FromSeconds(WINDOW_SECONDS),
				};

				strategy.RateLimiter = argument => (new FixedWindowRateLimiter(fixedOptions)).AcquireAsync(cancellationToken: argument.Context.CancellationToken);
				break;
			case ThrottleLimiter.SlidingWindown limiter:
				var slidingOptions = new SlidingWindowRateLimiterOptions()
				{
					PermitLimit = strategy.DefaultRateLimiterOptions.PermitLimit,
					QueueLimit = strategy.DefaultRateLimiterOptions.QueueLimit,
					QueueProcessingOrder = strategy.DefaultRateLimiterOptions.QueueProcessingOrder,
					Window = limiter.Window > TimeSpan.Zero ? limiter.Window : TimeSpan.FromSeconds(WINDOW_SECONDS),
					SegmentsPerWindow = limiter.WindowSize > 0 ? limiter.WindowSize : WINDOW_SIZE,
				};

				strategy.RateLimiter = argument => (new SlidingWindowRateLimiter(slidingOptions)).AcquireAsync(cancellationToken: argument.Context.CancellationToken);
				break;
		}

		return strategy;
	}

	private sealed class PollyThrottleArgument(OnRateLimiterRejectedArguments arguments) : ThrottleArgument(arguments.Context.OperationKey, new PollyThrottleLease(arguments.Lease))
	{
	}

	private sealed class PollyThrottleLease(RateLimitLease lease) : ThrottleLease
	{
		private RateLimitLease _lease = lease;
		private MetadataCollection _metadata = new(lease);

		public override bool IsLeased => _lease?.IsAcquired ?? throw new ObjectDisposedException(nameof(PollyThrottleLease));
		public override IMetadataCollection Metadata => _metadata ?? throw new ObjectDisposedException(nameof(PollyThrottleLease));

		protected override void Dispose(bool disposing)
		{
			_lease?.Dispose();
			_lease = null;
			_metadata = null;
		}

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
