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
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Components.Features;

public static class RetryFeatureExtension
{
	public static IFeatureBuilder Retry(this IFeatureBuilder builder, Common.IPredication<RetryArgument> predicator, RetryLatency latency, int attempts = 0, Func<RetryArgument, CancellationToken, ValueTask> onRetry = null) =>
		Retry(builder, RetryBackoff.None, latency, true, attempts, predicator);
	public static IFeatureBuilder Retry(this IFeatureBuilder builder, Common.IPredication<RetryArgument> predicator, RetryLatency latency, bool jitterable, int attempts = 0, Func<RetryArgument, CancellationToken, ValueTask> onRetry = null) =>
		Retry(builder, RetryBackoff.None, latency, jitterable, attempts, predicator);
	public static IFeatureBuilder Retry(this IFeatureBuilder builder, Common.IPredication<RetryArgument> predicator, RetryBackoff backoff, RetryLatency latency, int attempts = 0, Func<RetryArgument, CancellationToken, ValueTask> onRetry = null) =>
		Retry(builder, backoff, latency, true, attempts, predicator);
	public static IFeatureBuilder Retry(this IFeatureBuilder builder, Common.IPredication<RetryArgument> predicator, RetryBackoff backoff, RetryLatency latency, bool jitterable, int attempts = 0, Func<RetryArgument, CancellationToken, ValueTask> onRetry = null) =>
		Retry(builder, backoff, latency, jitterable, attempts, predicator);

	public static IFeatureBuilder Retry(this IFeatureBuilder builder, RetryLatency latency, int attempts = 0, Common.IPredication<RetryArgument> predicator = null, Func<RetryArgument, CancellationToken, ValueTask> onRetry = null) =>
		Retry(builder, RetryBackoff.None, latency, true, attempts, predicator);
	public static IFeatureBuilder Retry(this IFeatureBuilder builder, RetryLatency latency, bool jitterable, int attempts = 0, Common.IPredication<RetryArgument> predicator = null, Func<RetryArgument, CancellationToken, ValueTask> onRetry = null) =>
		Retry(builder, RetryBackoff.None, latency, jitterable, attempts, predicator);
	public static IFeatureBuilder Retry(this IFeatureBuilder builder, RetryBackoff backoff, RetryLatency latency, int attempts = 0, Common.IPredication<RetryArgument> predicator = null, Func<RetryArgument, CancellationToken, ValueTask> onRetry = null) =>
		Retry(builder, backoff, latency, true, attempts, predicator, onRetry);
	public static IFeatureBuilder Retry(this IFeatureBuilder builder, RetryBackoff backoff, RetryLatency latency, bool jitterable, int attempts = 0, Common.IPredication<RetryArgument> predicator = null, Func<RetryArgument, CancellationToken, ValueTask> onRetry = null)
	{
		if(builder == null)
			return new FeatureBuilder(new RetryFeature(backoff, latency, jitterable, attempts, predicator) { OnRetry = onRetry });

		if(builder is FeatureBuilder appender)
		{
			appender.Features.Add(new RetryFeature(backoff, latency, jitterable, attempts, predicator) { OnRetry = onRetry });
			return appender;
		}

		return new FeatureBuilder([.. builder.Build(), new RetryFeature(backoff, latency, jitterable, attempts, predicator) { OnRetry = onRetry }]);
	}

	public static IFeatureBuilder Retry<T>(this IFeatureBuilder builder, Common.IPredication<RetryArgument<T>> predicator, RetryLatency latency, int attempts = 0) =>
		Retry(builder, RetryBackoff.None, latency, true, attempts, predicator);
	public static IFeatureBuilder Retry<T>(this IFeatureBuilder builder, Common.IPredication<RetryArgument<T>> predicator, RetryLatency latency, bool jitterable, int attempts = 0) =>
		Retry(builder, RetryBackoff.None, latency, jitterable, attempts, predicator);
	public static IFeatureBuilder Retry<T>(this IFeatureBuilder builder, Common.IPredication<RetryArgument<T>> predicator, RetryBackoff backoff, RetryLatency latency, int attempts = 0) =>
		Retry(builder, backoff, latency, true, attempts, predicator);
	public static IFeatureBuilder Retry<T>(this IFeatureBuilder builder, Common.IPredication<RetryArgument<T>> predicator, RetryBackoff backoff, RetryLatency latency, bool jitterable, int attempts = 0) =>
		Retry(builder, backoff, latency, jitterable, attempts, predicator);

	public static IFeatureBuilder Retry<T>(this IFeatureBuilder builder, RetryLatency latency, int attempts = 0, Common.IPredication<RetryArgument<T>> predicator = null) =>
		Retry(builder, RetryBackoff.None, latency, true, attempts, predicator);
	public static IFeatureBuilder Retry<T>(this IFeatureBuilder builder, RetryLatency latency, bool jitterable, int attempts = 0, Common.IPredication<RetryArgument<T>> predicator = null) =>
		Retry(builder, RetryBackoff.None, latency, jitterable, attempts, predicator);
	public static IFeatureBuilder Retry<T>(this IFeatureBuilder builder, RetryBackoff backoff, RetryLatency latency, int attempts = 0, Common.IPredication<RetryArgument<T>> predicator = null) =>
		Retry(builder, backoff, latency, true, attempts, predicator);
	public static IFeatureBuilder Retry<T>(this IFeatureBuilder builder, RetryBackoff backoff, RetryLatency latency, bool jitterable, int attempts = 0, Common.IPredication<RetryArgument<T>> predicator = null)
	{
		if(builder == null)
			return new FeatureBuilder(new RetryFeature<T>(backoff, latency, jitterable, attempts, predicator));

		if(builder is FeatureBuilder appender)
		{
			appender.Features.Add(new RetryFeature<T>(backoff, latency, jitterable, attempts, predicator));
			return appender;
		}

		return new FeatureBuilder([.. builder.Build(), new RetryFeature<T>(backoff, latency, jitterable, attempts, predicator)]);
	}

	public static IFeatureBuilder Retry<T, TResult>(this IFeatureBuilder builder, Common.IPredication<RetryArgument<T, TResult>> predicator, RetryLatency latency, int attempts = 0) =>
		Retry(builder, RetryBackoff.None, latency, true, attempts, predicator);
	public static IFeatureBuilder Retry<T, TResult>(this IFeatureBuilder builder, Common.IPredication<RetryArgument<T, TResult>> predicator, RetryLatency latency, bool jitterable, int attempts = 0) =>
		Retry(builder, RetryBackoff.None, latency, jitterable, attempts, predicator);
	public static IFeatureBuilder Retry<T, TResult>(this IFeatureBuilder builder, Common.IPredication<RetryArgument<T, TResult>> predicator, RetryBackoff backoff, RetryLatency latency, int attempts = 0) =>
		Retry(builder, backoff, latency, true, attempts, predicator);
	public static IFeatureBuilder Retry<T, TResult>(this IFeatureBuilder builder, Common.IPredication<RetryArgument<T, TResult>> predicator, RetryBackoff backoff, RetryLatency latency, bool jitterable, int attempts = 0) =>
		Retry(builder, backoff, latency, jitterable, attempts, predicator);

	public static IFeatureBuilder Retry<T, TResult>(this IFeatureBuilder builder, RetryLatency latency, int attempts = 0, Common.IPredication<RetryArgument<T, TResult>> predicator = null) =>
		Retry(builder, RetryBackoff.None, latency, true, attempts, predicator);
	public static IFeatureBuilder Retry<T, TResult>(this IFeatureBuilder builder, RetryLatency latency, bool jitterable, int attempts = 0, Common.IPredication<RetryArgument<T, TResult>> predicator = null) =>
		Retry(builder, RetryBackoff.None, latency, jitterable, attempts, predicator);
	public static IFeatureBuilder Retry<T, TResult>(this IFeatureBuilder builder, RetryBackoff backoff, RetryLatency latency, int attempts = 0, Common.IPredication<RetryArgument<T, TResult>> predicator = null) =>
		Retry(builder, backoff, latency, true, attempts, predicator);
	public static IFeatureBuilder Retry<T, TResult>(this IFeatureBuilder builder, RetryBackoff backoff, RetryLatency latency, bool jitterable, int attempts = 0, Common.IPredication<RetryArgument<T, TResult>> predicator = null)
	{
		if(builder == null)
			return new FeatureBuilder(new RetryFeature<T, TResult>(backoff, latency, jitterable, attempts, predicator));

		if(builder is FeatureBuilder appender)
		{
			appender.Features.Add(new RetryFeature<T, TResult>(backoff, latency, jitterable, attempts, predicator));
			return appender;
		}

		return new FeatureBuilder([.. builder.Build(), new RetryFeature<T, TResult>(backoff, latency, jitterable, attempts, predicator)]);
	}
}
