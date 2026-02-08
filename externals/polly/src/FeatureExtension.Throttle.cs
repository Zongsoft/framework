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
using Zongsoft.Externals.Polly.Strategies;

namespace Zongsoft.Externals.Polly;

partial class FeatureExtension
{
	#region 常量定义
	private const int TOKENS_VALUE = 1;
	private const int WINDOW_SEGMENTS = 3;
	private const int WINDOW_SECONDS = 1;
	#endregion

	#region 公共方法
	public static ThrottleStrategyOptions ToStrategy(this ThrottleFeature feature)
	{
		if(!feature.Usable())
			return null;

		var options = new ThrottleStrategyOptions();

		if(feature.Rejected != null)
			options.OnRejected = feature.Rejected.HandleAsync;

		SetLimiter(options, feature);

		return options;
	}

	public static ThrottleStrategyOptions<T> ToStrategy<T>(this ThrottleFeature<T> feature)
	{
		if(!feature.Usable())
			return null;

		var options = new ThrottleStrategyOptions<T>();

		if(feature.Rejected != null)
			options.OnRejected = feature.Rejected.HandleAsync;

		SetLimiter(options, feature);

		return options;
	}

	public static ThrottleStrategyOptions<T, TResult> ToStrategy<T, TResult>(this ThrottleFeature<T, TResult> feature)
	{
		if(!feature.Usable())
			return null;

		var options = new ThrottleStrategyOptions<T, TResult>();

		if(feature.Rejected != null)
			options.OnRejected = feature.Rejected.HandleAsync;

		SetLimiter(options, feature);

		return options;
	}
	#endregion

	#region 私有方法
	private static void SetLimiter(ThrottleStrategyOptionsBase options, ThrottleFeatureBase feature)
	{
		switch(feature.Limiter)
		{
			case ThrottleLimiter.TokenBucket limiter:
				var tokenLimiter = new TokenBucketRateLimiter(new()
				{
					TokenLimit = GetPermitLimit(feature),
					QueueLimit = GetQueueLimit(feature),
					QueueProcessingOrder = GetQueueOrder(feature.QueueOrder),
					TokensPerPeriod = limiter.Value > 0 ? limiter.Value : TOKENS_VALUE,
					ReplenishmentPeriod = limiter.Period > TimeSpan.Zero ? limiter.Period : TimeSpan.FromSeconds(WINDOW_SECONDS),
				});

				options.Name = nameof(TokenBucketRateLimiter);
				options.RateLimiter = argument => tokenLimiter.AcquireAsync(1, argument.Context.CancellationToken);
				break;
			case ThrottleLimiter.FixedWindown limiter:
				var fixedLimiter = new FixedWindowRateLimiter(new()
				{
					PermitLimit = GetPermitLimit(feature),
					QueueLimit = GetQueueLimit(feature),
					QueueProcessingOrder = GetQueueOrder(feature.QueueOrder),
					Window = limiter.Window > TimeSpan.Zero ? limiter.Window : TimeSpan.FromSeconds(WINDOW_SECONDS),
				});

				options.Name = nameof(FixedWindowRateLimiter);
				options.RateLimiter = argument => fixedLimiter.AcquireAsync(1, argument.Context.CancellationToken);
				break;
			case ThrottleLimiter.SlidingWindown limiter:
				var slidingLimiter = new SlidingWindowRateLimiter(new()
				{
					PermitLimit = GetPermitLimit(feature),
					QueueLimit = GetQueueLimit(feature),
					QueueProcessingOrder = GetQueueOrder(feature.QueueOrder),
					Window = limiter.Window > TimeSpan.Zero ? limiter.Window : TimeSpan.FromSeconds(WINDOW_SECONDS),
					SegmentsPerWindow = limiter.WindowSegments > 0 ? limiter.WindowSegments : WINDOW_SEGMENTS,
				});

				options.Name = nameof(SlidingWindowRateLimiter);
				options.RateLimiter = argument => slidingLimiter.AcquireAsync(1, argument.Context.CancellationToken);
				break;
			default:
				options.DefaultRateLimiterOptions.PermitLimit = GetPermitLimit(feature);
				options.DefaultRateLimiterOptions.QueueLimit = GetQueueLimit(feature);
				options.DefaultRateLimiterOptions.QueueProcessingOrder = GetQueueOrder(feature.QueueOrder);
				break;
		}
	}

	private static int GetPermitLimit(ThrottleFeatureBase feature) => feature != null && feature.PermitLimit > 0 ? feature.PermitLimit : ThrottleStrategyOptions.PERMIT_LIMIT;
	private static int GetQueueLimit(ThrottleFeatureBase feature) => feature != null && feature.QueueLimit > 0 ? feature.QueueLimit : ThrottleStrategyOptions.QUEUE_LIMIT;
	private static QueueProcessingOrder GetQueueOrder(ThrottleQueueOrder value) => value switch
	{
		ThrottleQueueOrder.Oldest => QueueProcessingOrder.OldestFirst,
		ThrottleQueueOrder.Newest => QueueProcessingOrder.NewestFirst,
		_ => QueueProcessingOrder.OldestFirst,
	};
	#endregion

	#region 嵌套子类
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
	#endregion
}
