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

namespace Zongsoft.Components.Features;

public static class ThrottleFeatureExtension
{
	#region 静态方法
	public static IFeatureBuilder Throttle(this IFeatureBuilder builder, int permitLimit, IHandler<ThrottleArgument, bool> rejected = null) => Throttle(builder, permitLimit, 0, ThrottleQueueOrder.Oldest, null, rejected);
	public static IFeatureBuilder Throttle(this IFeatureBuilder builder, int permitLimit, ThrottleQueueOrder queueOrder, IHandler<ThrottleArgument, bool> rejected = null) => Throttle(builder, permitLimit, 0, queueOrder, null, rejected);
	public static IFeatureBuilder Throttle(this IFeatureBuilder builder, int permitLimit, int queueLimit, IHandler<ThrottleArgument, bool> rejected = null) => Throttle(builder, permitLimit, queueLimit, ThrottleQueueOrder.Oldest, null, rejected);
	public static IFeatureBuilder Throttle(this IFeatureBuilder builder, int permitLimit, int queueLimit, ThrottleQueueOrder queueOrder, IHandler<ThrottleArgument, bool> rejected = null) => Throttle(builder, permitLimit, queueLimit, queueOrder, null, rejected);

	public static IFeatureBuilder Throttle(this IFeatureBuilder builder, int permitLimit, ThrottleLimiter limiter, IHandler<ThrottleArgument, bool> rejected = null) => Throttle(builder, permitLimit, 0, ThrottleQueueOrder.Oldest, limiter, rejected);
	public static IFeatureBuilder Throttle(this IFeatureBuilder builder, int permitLimit, ThrottleQueueOrder queueOrder, ThrottleLimiter limiter, IHandler<ThrottleArgument, bool> rejected = null) => Throttle(builder, permitLimit, 0, queueOrder, limiter, rejected);
	public static IFeatureBuilder Throttle(this IFeatureBuilder builder, int permitLimit, int queueLimit, ThrottleLimiter limiter, IHandler<ThrottleArgument, bool> rejected = null) => Throttle(builder, permitLimit, queueLimit, ThrottleQueueOrder.Oldest, limiter, rejected);
	public static IFeatureBuilder Throttle(this IFeatureBuilder builder, int permitLimit, int queueLimit, ThrottleQueueOrder queueOrder, ThrottleLimiter limiter, IHandler<ThrottleArgument, bool> rejected = null)
	{
		if(builder == null)
			return new FeatureBuilder(new ThrottleFeature(permitLimit, queueLimit, queueOrder, limiter, rejected));

		if(builder is FeatureBuilder appender)
		{
			appender.Features.Add(new ThrottleFeature(permitLimit, queueLimit, queueOrder, limiter, rejected));
			return appender;
		}

		return new FeatureBuilder([.. builder.Build(), new ThrottleFeature(permitLimit, queueLimit, queueOrder, limiter, rejected)]);
	}

	public static IFeatureBuilder Throttle<T>(this IFeatureBuilder builder, int permitLimit, IHandler<ThrottleArgument<T>, bool> rejected = null) => Throttle(builder, permitLimit, 0, ThrottleQueueOrder.Oldest, null, rejected);
	public static IFeatureBuilder Throttle<T>(this IFeatureBuilder builder, int permitLimit, ThrottleQueueOrder queueOrder, IHandler<ThrottleArgument<T>, bool> rejected = null) => Throttle(builder, permitLimit, 0, queueOrder, null, rejected);
	public static IFeatureBuilder Throttle<T>(this IFeatureBuilder builder, int permitLimit, int queueLimit, IHandler<ThrottleArgument<T>, bool> rejected = null) => Throttle(builder, permitLimit, queueLimit, ThrottleQueueOrder.Oldest, null, rejected);
	public static IFeatureBuilder Throttle<T>(this IFeatureBuilder builder, int permitLimit, int queueLimit, ThrottleQueueOrder queueOrder, IHandler<ThrottleArgument<T>, bool> rejected = null) => Throttle(builder, permitLimit, queueLimit, queueOrder, null, rejected);

	public static IFeatureBuilder Throttle<T>(this IFeatureBuilder builder, int permitLimit, ThrottleLimiter limiter, IHandler<ThrottleArgument<T>, bool> rejected = null) => Throttle(builder, permitLimit, 0, ThrottleQueueOrder.Oldest, limiter, rejected);
	public static IFeatureBuilder Throttle<T>(this IFeatureBuilder builder, int permitLimit, ThrottleQueueOrder queueOrder, ThrottleLimiter limiter, IHandler<ThrottleArgument<T>, bool> rejected = null) => Throttle(builder, permitLimit, 0, queueOrder, limiter, rejected);
	public static IFeatureBuilder Throttle<T>(this IFeatureBuilder builder, int permitLimit, int queueLimit, ThrottleLimiter limiter, IHandler<ThrottleArgument<T>, bool> rejected = null) => Throttle(builder, permitLimit, queueLimit, ThrottleQueueOrder.Oldest, limiter, rejected);
	public static IFeatureBuilder Throttle<T>(this IFeatureBuilder builder, int permitLimit, int queueLimit, ThrottleQueueOrder queueOrder, ThrottleLimiter limiter, IHandler<ThrottleArgument<T>, bool> rejected = null)
	{
		if(builder == null)
			return new FeatureBuilder(new ThrottleFeature<T>(permitLimit, queueLimit, queueOrder, limiter, rejected));

		if(builder is FeatureBuilder appender)
		{
			appender.Features.Add(new ThrottleFeature<T>(permitLimit, queueLimit, queueOrder, limiter, rejected));
			return appender;
		}

		return new FeatureBuilder([.. builder.Build(), new ThrottleFeature<T>(permitLimit, queueLimit, queueOrder, limiter, rejected)]);
	}

	public static IFeatureBuilder Throttle<T, TResult>(this IFeatureBuilder builder, int permitLimit, IHandler<ThrottleArgument<T, TResult>, bool> rejected = null) => Throttle(builder, permitLimit, 0, ThrottleQueueOrder.Oldest, null, rejected);
	public static IFeatureBuilder Throttle<T, TResult>(this IFeatureBuilder builder, int permitLimit, ThrottleQueueOrder queueOrder, IHandler<ThrottleArgument<T, TResult>, bool> rejected = null) => Throttle(builder, permitLimit, 0, queueOrder, null, rejected);
	public static IFeatureBuilder Throttle<T, TResult>(this IFeatureBuilder builder, int permitLimit, int queueLimit, IHandler<ThrottleArgument<T, TResult>, bool> rejected = null) => Throttle(builder, permitLimit, queueLimit, ThrottleQueueOrder.Oldest, null, rejected);
	public static IFeatureBuilder Throttle<T, TResult>(this IFeatureBuilder builder, int permitLimit, int queueLimit, ThrottleQueueOrder queueOrder, IHandler<ThrottleArgument<T, TResult>, bool> rejected = null) => Throttle(builder, permitLimit, queueLimit, queueOrder, null, rejected);

	public static IFeatureBuilder Throttle<T, TResult>(this IFeatureBuilder builder, int permitLimit, ThrottleLimiter limiter, IHandler<ThrottleArgument<T, TResult>, bool> rejected = null) => Throttle(builder, permitLimit, 0, ThrottleQueueOrder.Oldest, limiter, rejected);
	public static IFeatureBuilder Throttle<T, TResult>(this IFeatureBuilder builder, int permitLimit, ThrottleQueueOrder queueOrder, ThrottleLimiter limiter, IHandler<ThrottleArgument<T, TResult>, bool> rejected = null) => Throttle(builder, permitLimit, 0, queueOrder, limiter, rejected);
	public static IFeatureBuilder Throttle<T, TResult>(this IFeatureBuilder builder, int permitLimit, int queueLimit, ThrottleLimiter limiter, IHandler<ThrottleArgument<T, TResult>, bool> rejected = null) => Throttle(builder, permitLimit, queueLimit, ThrottleQueueOrder.Oldest, limiter, rejected);
	public static IFeatureBuilder Throttle<T, TResult>(this IFeatureBuilder builder, int permitLimit, int queueLimit, ThrottleQueueOrder queueOrder, ThrottleLimiter limiter, IHandler<ThrottleArgument<T, TResult>, bool> rejected = null)
	{
		if(builder == null)
			return new FeatureBuilder(new ThrottleFeature<T, TResult>(permitLimit, queueLimit, queueOrder, limiter, rejected));

		if(builder is FeatureBuilder appender)
		{
			appender.Features.Add(new ThrottleFeature<T, TResult>(permitLimit, queueLimit, queueOrder, limiter, rejected));
			return appender;
		}

		return new FeatureBuilder([.. builder.Build(), new ThrottleFeature<T, TResult>(permitLimit, queueLimit, queueOrder, limiter, rejected)]);
	}
	#endregion
}
