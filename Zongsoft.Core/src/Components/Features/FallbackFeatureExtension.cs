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

public static class FallbackFeatureExtension
{
	public static IFeatureBuilder Fallback(this IFeatureBuilder builder, Func<Argument, CancellationToken, ValueTask> fallback, Common.IPredication<Argument> predicator = null)
	{
		if(builder == null)
			return new FeatureBuilder(new FallbackFeature(fallback, predicator));

		if(builder is FeatureBuilder appender)
		{
			appender.Features.Add(new FallbackFeature(fallback, predicator));
			return appender;
		}

		return new FeatureBuilder([.. builder.Build(), new FallbackFeature(fallback, predicator)]);
	}

	public static IFeatureBuilder Fallback<T>(this IFeatureBuilder builder, Func<Argument<T>, CancellationToken, ValueTask> fallback, Common.IPredication<Argument<T>> predicator = null)
	{
		if(builder == null)
			return new FeatureBuilder(new FallbackFeature<T>(fallback, predicator));

		if(builder is FeatureBuilder appender)
		{
			appender.Features.Add(new FallbackFeature<T>(fallback, predicator));
			return appender;
		}

		return new FeatureBuilder([.. builder.Build(), new FallbackFeature<T>(fallback, predicator)]);
	}

	public static IFeatureBuilder Fallback<T, TResult>(this IFeatureBuilder builder, Func<Argument<T, TResult>, CancellationToken, ValueTask<TResult>> fallback, Common.IPredication<Argument<T, TResult>> predicator = null)
	{
		if(builder == null)
			return new FeatureBuilder(new FallbackFeature<T, TResult>(fallback, predicator));

		if(builder is FeatureBuilder appender)
		{
			appender.Features.Add(new FallbackFeature<T, TResult>(fallback, predicator));
			return appender;
		}

		return new FeatureBuilder([.. builder.Build(), new FallbackFeature<T, TResult>(fallback, predicator)]);
	}
}
