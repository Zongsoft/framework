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

namespace Zongsoft.Components.Features;

public static class RetryFeatureExtension
{
	#region 静态方法
	public static IFeatureBuilder Retry(this IFeatureBuilder builder, RetryLatency latency, int attempts = 0, Func<RetryArgument, CancellationToken, ValueTask> onRetry = null) =>
		Retry(builder, RetryBackoff.None, latency, true, attempts, null, onRetry);
	public static IFeatureBuilder Retry(this IFeatureBuilder builder, RetryLatency latency, bool jitterable, int attempts = 0, Func<RetryArgument, CancellationToken, ValueTask> onRetry = null) =>
		Retry(builder, RetryBackoff.None, latency, jitterable, attempts, null, onRetry);
	public static IFeatureBuilder Retry(this IFeatureBuilder builder, RetryBackoff backoff, RetryLatency latency, int attempts = 0, Func<RetryArgument, CancellationToken, ValueTask> onRetry = null) =>
		Retry(builder, backoff, latency, true, attempts, null, onRetry);
	public static IFeatureBuilder Retry(this IFeatureBuilder builder, RetryBackoff backoff, RetryLatency latency, bool jitterable, int attempts = 0, Func<RetryArgument, CancellationToken, ValueTask> onRetry = null) =>
		Retry(builder, backoff, latency, jitterable, attempts, null, onRetry);

	public static IFeatureBuilder Retry(this IFeatureBuilder builder, RetryLatency latency, int attempts, Common.IPredication<RetryArgument> predicator, Func<RetryArgument, CancellationToken, ValueTask> onRetry = null) =>
		Retry(builder, RetryBackoff.None, latency, true, attempts, predicator, onRetry);
	public static IFeatureBuilder Retry(this IFeatureBuilder builder, RetryLatency latency, bool jitterable, int attempts, Common.IPredication<RetryArgument> predicator, Func<RetryArgument, CancellationToken, ValueTask> onRetry = null) =>
		Retry(builder, RetryBackoff.None, latency, jitterable, attempts, predicator, onRetry);
	public static IFeatureBuilder Retry(this IFeatureBuilder builder, RetryBackoff backoff, RetryLatency latency, int attempts, Common.IPredication<RetryArgument> predicator, Func<RetryArgument, CancellationToken, ValueTask> onRetry = null) =>
		Retry(builder, backoff, latency, true, attempts, predicator, onRetry);
	public static IFeatureBuilder Retry(this IFeatureBuilder builder, RetryBackoff backoff, RetryLatency latency, bool jitterable, int attempts, Common.IPredication<RetryArgument> predicator, Func<RetryArgument, CancellationToken, ValueTask> onRetry = null)
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

	public static IFeatureBuilder Retry<T>(this IFeatureBuilder builder, RetryLatency latency, int attempts = 0, Func<RetryArgument<T>, CancellationToken, ValueTask> onRetry = null) =>
		Retry(builder, RetryBackoff.None, latency, true, attempts, null, onRetry);
	public static IFeatureBuilder Retry<T>(this IFeatureBuilder builder, RetryLatency latency, bool jitterable, int attempts = 0, Func<RetryArgument<T>, CancellationToken, ValueTask> onRetry = null) =>
		Retry(builder, RetryBackoff.None, latency, jitterable, attempts, null, onRetry);
	public static IFeatureBuilder Retry<T>(this IFeatureBuilder builder, RetryBackoff backoff, RetryLatency latency, int attempts = 0, Func<RetryArgument<T>, CancellationToken, ValueTask> onRetry = null) =>
		Retry(builder, backoff, latency, true, attempts, null, onRetry);
	public static IFeatureBuilder Retry<T>(this IFeatureBuilder builder, RetryBackoff backoff, RetryLatency latency, bool jitterable, int attempts = 0, Func<RetryArgument<T>, CancellationToken, ValueTask> onRetry = null) =>
		Retry(builder, backoff, latency, jitterable, attempts, null, onRetry);

	public static IFeatureBuilder Retry<T>(this IFeatureBuilder builder, RetryLatency latency, int attempts, Common.IPredication<RetryArgument<T>> predicator, Func<RetryArgument<T>, CancellationToken, ValueTask> onRetry = null) =>
		Retry(builder, RetryBackoff.None, latency, true, attempts, predicator, onRetry);
	public static IFeatureBuilder Retry<T>(this IFeatureBuilder builder, RetryLatency latency, bool jitterable, int attempts, Common.IPredication<RetryArgument<T>> predicator, Func<RetryArgument<T>, CancellationToken, ValueTask> onRetry = null) =>
		Retry(builder, RetryBackoff.None, latency, jitterable, attempts, predicator, onRetry);
	public static IFeatureBuilder Retry<T>(this IFeatureBuilder builder, RetryBackoff backoff, RetryLatency latency, int attempts, Common.IPredication<RetryArgument<T>> predicator, Func<RetryArgument<T>, CancellationToken, ValueTask> onRetry = null) =>
		Retry(builder, backoff, latency, true, attempts, predicator, onRetry);
	public static IFeatureBuilder Retry<T>(this IFeatureBuilder builder, RetryBackoff backoff, RetryLatency latency, bool jitterable, int attempts, Common.IPredication<RetryArgument<T>> predicator, Func<RetryArgument<T>, CancellationToken, ValueTask> onRetry = null)
	{
		if(builder == null)
			return new FeatureBuilder(new RetryFeature<T>(backoff, latency, jitterable, attempts, predicator) { OnRetry = onRetry });

		if(builder is FeatureBuilder appender)
		{
			appender.Features.Add(new RetryFeature<T>(backoff, latency, jitterable, attempts, predicator) { OnRetry = onRetry });
			return appender;
		}

		return new FeatureBuilder([.. builder.Build(), new RetryFeature<T>(backoff, latency, jitterable, attempts, predicator) { OnRetry = onRetry }]);
	}

	public static IFeatureBuilder Retry<T, TResult>(this IFeatureBuilder builder, RetryLatency latency, int attempts = 0, Func<RetryArgument<T, TResult>, CancellationToken, ValueTask> onRetry = null) =>
		Retry(builder, RetryBackoff.None, latency, true, attempts, null, onRetry);
	public static IFeatureBuilder Retry<T, TResult>(this IFeatureBuilder builder, RetryLatency latency, bool jitterable, int attempts = 0, Func<RetryArgument<T, TResult>, CancellationToken, ValueTask> onRetry = null) =>
		Retry(builder, RetryBackoff.None, latency, jitterable, attempts, null, onRetry);
	public static IFeatureBuilder Retry<T, TResult>(this IFeatureBuilder builder, RetryBackoff backoff, RetryLatency latency, int attempts = 0, Func<RetryArgument<T, TResult>, CancellationToken, ValueTask> onRetry = null) =>
		Retry(builder, backoff, latency, true, attempts, null, onRetry);
	public static IFeatureBuilder Retry<T, TResult>(this IFeatureBuilder builder, RetryBackoff backoff, RetryLatency latency, bool jitterable, int attempts = 0, Func<RetryArgument<T, TResult>, CancellationToken, ValueTask> onRetry = null) =>
		Retry(builder, backoff, latency, jitterable, attempts, null, onRetry);

	public static IFeatureBuilder Retry<T, TResult>(this IFeatureBuilder builder, RetryLatency latency, int attempts, Common.IPredication<RetryArgument<T, TResult>> predicator, Func<RetryArgument<T, TResult>, CancellationToken, ValueTask> onRetry = null) =>
		Retry(builder, RetryBackoff.None, latency, true, attempts, predicator, onRetry);
	public static IFeatureBuilder Retry<T, TResult>(this IFeatureBuilder builder, RetryLatency latency, bool jitterable, int attempts, Common.IPredication<RetryArgument<T, TResult>> predicator, Func<RetryArgument<T, TResult>, CancellationToken, ValueTask> onRetry = null) =>
		Retry(builder, RetryBackoff.None, latency, jitterable, attempts, predicator, onRetry);
	public static IFeatureBuilder Retry<T, TResult>(this IFeatureBuilder builder, RetryBackoff backoff, RetryLatency latency, int attempts, Common.IPredication<RetryArgument<T, TResult>> predicator, Func<RetryArgument<T, TResult>, CancellationToken, ValueTask> onRetry = null) =>
		Retry(builder, backoff, latency, true, attempts, predicator, onRetry);
	public static IFeatureBuilder Retry<T, TResult>(this IFeatureBuilder builder, RetryBackoff backoff, RetryLatency latency, bool jitterable, int attempts, Common.IPredication<RetryArgument<T, TResult>> predicator, Func<RetryArgument<T, TResult>, CancellationToken, ValueTask> onRetry = null)
	{
		if(builder == null)
			return new FeatureBuilder(new RetryFeature<T, TResult>(backoff, latency, jitterable, attempts, predicator) { OnRetry = onRetry });

		if(builder is FeatureBuilder appender)
		{
			appender.Features.Add(new RetryFeature<T, TResult>(backoff, latency, jitterable, attempts, predicator) { OnRetry = onRetry });
			return appender;
		}

		return new FeatureBuilder([.. builder.Build(), new RetryFeature<T, TResult>(backoff, latency, jitterable, attempts, predicator) { OnRetry = onRetry }]);
	}
	#endregion
}
