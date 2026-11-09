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

public static class BreakerFeatureExtension
{
	#region 静态方法
	public static IFeatureBuilder Breaker(this IFeatureBuilder builder, TimeSpan duration, double ratio, TimeSpan period, int threshold, Common.IPredication<Argument> predicator = null) => Breaker(builder, duration, ratio, period, threshold, predicator, out _);
	public static IFeatureBuilder Breaker(this IFeatureBuilder builder, TimeSpan duration, double ratio, TimeSpan period, int threshold, out BreakerFeature feature) => Breaker(builder, duration, ratio, period, threshold, null, out feature);
	public static IFeatureBuilder Breaker(this IFeatureBuilder builder, TimeSpan duration, double ratio, TimeSpan period, int threshold, Common.IPredication<Argument> predicator, out BreakerFeature feature)
	{
		if(builder == null)
			return new FeatureBuilder(feature = new BreakerFeature(duration, ratio, period, threshold, predicator));

		if(builder is FeatureBuilder appender)
		{
			appender.Features.Add(feature = new BreakerFeature(duration, ratio, period, threshold, predicator));
			return appender;
		}

		return new FeatureBuilder([.. builder.Build(), feature = new BreakerFeature(duration, ratio, period, threshold, predicator)]);
	}

	public static IFeatureBuilder Breaker(this IFeatureBuilder builder, Func<BreakerFeature, int, double, TimeSpan> durationFactory, double ratio, TimeSpan period, int threshold, Common.IPredication<Argument> predicator = null) => Breaker(builder, durationFactory, ratio, period, threshold, predicator, out _);
	public static IFeatureBuilder Breaker(this IFeatureBuilder builder, Func<BreakerFeature, int, double, TimeSpan> durationFactory, double ratio, TimeSpan period, int threshold, out BreakerFeature feature) => Breaker(builder, durationFactory, ratio, period, threshold, null, out feature);
	public static IFeatureBuilder Breaker(this IFeatureBuilder builder, Func<BreakerFeature, int, double, TimeSpan> durationFactory, double ratio, TimeSpan period, int threshold, Common.IPredication<Argument> predicator, out BreakerFeature feature)
	{
		if(builder == null)
			return new FeatureBuilder(feature = new BreakerFeature(durationFactory, ratio, period, threshold, predicator));

		if(builder is FeatureBuilder appender)
		{
			appender.Features.Add(feature = new BreakerFeature(durationFactory, ratio, period, threshold, predicator));
			return appender;
		}

		return new FeatureBuilder([.. builder.Build(), feature = new BreakerFeature(durationFactory, ratio, period, threshold, predicator)]);
	}

	public static IFeatureBuilder Breaker<T>(this IFeatureBuilder builder, TimeSpan duration, double ratio, TimeSpan period, int threshold, Common.IPredication<Argument<T>> predicator = null) => Breaker<T>(builder, duration, ratio, period, threshold, predicator, out _);
	public static IFeatureBuilder Breaker<T>(this IFeatureBuilder builder, TimeSpan duration, double ratio, TimeSpan period, int threshold, out BreakerFeature<T> feature) => Breaker<T>(builder, duration, ratio, period, threshold, null, out feature);
	public static IFeatureBuilder Breaker<T>(this IFeatureBuilder builder, TimeSpan duration, double ratio, TimeSpan period, int threshold, Common.IPredication<Argument<T>> predicator, out BreakerFeature<T> feature)
	{
		if(builder == null)
			return new FeatureBuilder(feature = new BreakerFeature<T>(duration, ratio, period, threshold, predicator));

		if(builder is FeatureBuilder appender)
		{
			appender.Features.Add(feature = new BreakerFeature<T>(duration, ratio, period, threshold, predicator));
			return appender;
		}

		return new FeatureBuilder([.. builder.Build(), feature = new BreakerFeature<T>(duration, ratio, period, threshold, predicator)]);
	}

	public static IFeatureBuilder Breaker<T>(this IFeatureBuilder builder, Func<BreakerFeature<T>, int, double, TimeSpan> durationFactory, double ratio, TimeSpan period, int threshold, Common.IPredication<Argument<T>> predicator = null) => Breaker<T>(builder, durationFactory, ratio, period, threshold, predicator, out _);
	public static IFeatureBuilder Breaker<T>(this IFeatureBuilder builder, Func<BreakerFeature<T>, int, double, TimeSpan> durationFactory, double ratio, TimeSpan period, int threshold, out BreakerFeature<T> feature) => Breaker<T>(builder, durationFactory, ratio, period, threshold, null, out feature);
	public static IFeatureBuilder Breaker<T>(this IFeatureBuilder builder, Func<BreakerFeature<T>, int, double, TimeSpan> durationFactory, double ratio, TimeSpan period, int threshold, Common.IPredication<Argument<T>> predicator, out BreakerFeature<T> feature)
	{
		if(builder == null)
			return new FeatureBuilder(feature = new BreakerFeature<T>(durationFactory, ratio, period, threshold, predicator));

		if(builder is FeatureBuilder appender)
		{
			appender.Features.Add(feature = new BreakerFeature<T>(durationFactory, ratio, period, threshold, predicator));
			return appender;
		}

		return new FeatureBuilder([.. builder.Build(), feature = new BreakerFeature<T>(durationFactory, ratio, period, threshold, predicator)]);
	}

	public static IFeatureBuilder Breaker<T, TResult>(this IFeatureBuilder builder, TimeSpan duration, double ratio, TimeSpan period, int threshold, Common.IPredication<Argument<T, TResult>> predicator) => Breaker<T, TResult>(builder, duration, ratio, period, threshold, predicator, out _);
	public static IFeatureBuilder Breaker<T, TResult>(this IFeatureBuilder builder, TimeSpan duration, double ratio, TimeSpan period, int threshold, out BreakerFeature<T, TResult> feature) => Breaker<T, TResult>(builder, duration, ratio, period, threshold, null, out feature);
	public static IFeatureBuilder Breaker<T, TResult>(this IFeatureBuilder builder, TimeSpan duration, double ratio, TimeSpan period, int threshold, Common.IPredication<Argument<T, TResult>> predicator, out BreakerFeature<T, TResult> feature)
	{
		if(builder == null)
			return new FeatureBuilder(feature = new BreakerFeature<T, TResult>(duration, ratio, period, threshold, predicator));

		if(builder is FeatureBuilder appender)
		{
			appender.Features.Add(feature = new BreakerFeature<T, TResult>(duration, ratio, period, threshold, predicator));
			return appender;
		}

		return new FeatureBuilder([.. builder.Build(), feature = new BreakerFeature<T, TResult>(duration, ratio, period, threshold, predicator)]);
	}

	public static IFeatureBuilder Breaker<T, TResult>(this IFeatureBuilder builder, Func<BreakerFeature<T, TResult>, int, double, TimeSpan> durationFactory, double ratio, TimeSpan period, int threshold, Common.IPredication<Argument<T, TResult>> predicator) => Breaker<T, TResult>(builder, durationFactory, ratio, period, threshold, predicator, out _);
	public static IFeatureBuilder Breaker<T, TResult>(this IFeatureBuilder builder, Func<BreakerFeature<T, TResult>, int, double, TimeSpan> durationFactory, double ratio, TimeSpan period, int threshold, out BreakerFeature<T, TResult> feature) => Breaker<T, TResult>(builder, durationFactory, ratio, period, threshold, null, out feature);
	public static IFeatureBuilder Breaker<T, TResult>(this IFeatureBuilder builder, Func<BreakerFeature<T, TResult>, int, double, TimeSpan> durationFactory, double ratio, TimeSpan period, int threshold, Common.IPredication<Argument<T, TResult>> predicator, out BreakerFeature<T, TResult> feature)
	{
		if(builder == null)
			return new FeatureBuilder(feature = new BreakerFeature<T, TResult>(durationFactory, ratio, period, threshold, predicator));

		if(builder is FeatureBuilder appender)
		{
			appender.Features.Add(feature = new BreakerFeature<T, TResult>(durationFactory, ratio, period, threshold, predicator));
			return appender;
		}

		return new FeatureBuilder([.. builder.Build(), feature = new BreakerFeature<T, TResult>(durationFactory, ratio, period, threshold, predicator)]);
	}
	#endregion
}
