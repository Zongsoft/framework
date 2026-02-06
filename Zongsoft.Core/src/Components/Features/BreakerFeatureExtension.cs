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

namespace Zongsoft.Components.Features;

public static class BreakerFeatureExtension
{
	public static IFeatureBuilder Breaker(this IFeatureBuilder builder, Common.IPredication<Argument> predicator = null) =>
		Breaker(builder, TimeSpan.Zero, 0, TimeSpan.Zero, 0, predicator);
	public static IFeatureBuilder Breaker(this IFeatureBuilder builder, Common.IPredication<Argument> predicator, TimeSpan duration, int threshold = 0) =>
		Breaker(builder, duration, 0, TimeSpan.Zero, threshold, predicator);
	public static IFeatureBuilder Breaker(this IFeatureBuilder builder, Common.IPredication<Argument> predicator, TimeSpan duration, double ratio, int threshold = 0) =>
		Breaker(builder, duration, ratio, TimeSpan.Zero, threshold, predicator);
	public static IFeatureBuilder Breaker(this IFeatureBuilder builder, Common.IPredication<Argument> predicator, TimeSpan duration, double ratio, TimeSpan period, int threshold = 0) =>
		Breaker(builder, duration, ratio, period, threshold, predicator);

	public static IFeatureBuilder Breaker(this IFeatureBuilder builder, TimeSpan duration, int threshold = 0, Common.IPredication<Argument> predicator = null) =>
		Breaker(builder, duration, 0, TimeSpan.Zero, threshold, predicator);
	public static IFeatureBuilder Breaker(this IFeatureBuilder builder, TimeSpan duration, double ratio, int threshold = 0, Common.IPredication<Argument> predicator = null) =>
		Breaker(builder, duration, ratio, TimeSpan.Zero, threshold, predicator);
	public static IFeatureBuilder Breaker(this IFeatureBuilder builder, TimeSpan duration, double ratio, TimeSpan period, int threshold = 0, Common.IPredication<Argument> predicator = null)
	{
		if(builder == null)
			return new FeatureBuilder(new BreakerFeature(duration, ratio, period, threshold, predicator));

		if(builder is FeatureBuilder appender)
		{
			appender.Features.Add(new BreakerFeature(duration, ratio, period, threshold, predicator));
			return appender;
		}

		return new FeatureBuilder([.. builder.Build(), new BreakerFeature(duration, ratio, period, threshold, predicator)]);
	}

	public static IFeatureBuilder Breaker<T>(this IFeatureBuilder builder, Common.IPredication<Argument<T>> predicator = null) =>
		Breaker(builder, TimeSpan.Zero, 0, TimeSpan.Zero, 0, predicator);
	public static IFeatureBuilder Breaker<T>(this IFeatureBuilder builder, Common.IPredication<Argument<T>> predicator, TimeSpan duration, int threshold = 0) =>
		Breaker(builder, duration, 0, TimeSpan.Zero, threshold, predicator);
	public static IFeatureBuilder Breaker<T>(this IFeatureBuilder builder, Common.IPredication<Argument<T>> predicator, TimeSpan duration, double ratio, int threshold = 0) =>
		Breaker(builder, duration, ratio, TimeSpan.Zero, threshold, predicator);
	public static IFeatureBuilder Breaker<T>(this IFeatureBuilder builder, Common.IPredication<Argument<T>> predicator, TimeSpan duration, double ratio, TimeSpan period, int threshold = 0) =>
		Breaker(builder, duration, ratio, period, threshold, predicator);

	public static IFeatureBuilder Breaker<T>(this IFeatureBuilder builder, TimeSpan duration, int threshold = 0, Common.IPredication<Argument<T>> predicator = null) =>
		Breaker(builder, duration, 0, TimeSpan.Zero, threshold, predicator);
	public static IFeatureBuilder Breaker<T>(this IFeatureBuilder builder, TimeSpan duration, double ratio, int threshold = 0, Common.IPredication<Argument<T>> predicator = null) =>
		Breaker(builder, duration, ratio, TimeSpan.Zero, threshold, predicator);
	public static IFeatureBuilder Breaker<T>(this IFeatureBuilder builder, TimeSpan duration, double ratio, TimeSpan period, int threshold = 0, Common.IPredication<Argument<T>> predicator = null)
	{
		if(builder == null)
			return new FeatureBuilder(new BreakerFeature<T>(duration, ratio, period, threshold, predicator));

		if(builder is FeatureBuilder appender)
		{
			appender.Features.Add(new BreakerFeature<T>(duration, ratio, period, threshold, predicator));
			return appender;
		}

		return new FeatureBuilder([.. builder.Build(), new BreakerFeature<T>(duration, ratio, period, threshold, predicator)]);
	}

	public static IFeatureBuilder Breaker<T, TResult>(this IFeatureBuilder builder, Common.IPredication<Argument<T, TResult>> predicator = null) =>
		Breaker(builder, TimeSpan.Zero, 0, TimeSpan.Zero, 0, predicator);
	public static IFeatureBuilder Breaker<T, TResult>(this IFeatureBuilder builder, Common.IPredication<Argument<T, TResult>> predicator, TimeSpan duration, int threshold = 0) =>
		Breaker(builder, duration, 0, TimeSpan.Zero, threshold, predicator);
	public static IFeatureBuilder Breaker<T, TResult>(this IFeatureBuilder builder, Common.IPredication<Argument<T, TResult>> predicator, TimeSpan duration, double ratio, int threshold = 0) =>
		Breaker(builder, duration, ratio, TimeSpan.Zero, threshold, predicator);
	public static IFeatureBuilder Breaker<T, TResult>(this IFeatureBuilder builder, Common.IPredication<Argument<T, TResult>> predicator, TimeSpan duration, double ratio, TimeSpan period, int threshold = 0) =>
		Breaker(builder, duration, ratio, period, threshold, predicator);

	public static IFeatureBuilder Breaker<T, TResult>(this IFeatureBuilder builder, TimeSpan duration, int threshold = 0, Common.IPredication<Argument<T, TResult>> predicator = null) =>
		Breaker(builder, duration, 0, TimeSpan.Zero, threshold, predicator);
	public static IFeatureBuilder Breaker<T, TResult>(this IFeatureBuilder builder, TimeSpan duration, double ratio, int threshold = 0, Common.IPredication<Argument<T, TResult>> predicator = null) =>
		Breaker(builder, duration, ratio, TimeSpan.Zero, threshold, predicator);
	public static IFeatureBuilder Breaker<T, TResult>(this IFeatureBuilder builder, TimeSpan duration, double ratio, TimeSpan period, int threshold = 0, Common.IPredication<Argument<T, TResult>> predicator = null)
	{
		if(builder == null)
			return new FeatureBuilder(new BreakerFeature<T, TResult>(duration, ratio, period, threshold, predicator));

		if(builder is FeatureBuilder appender)
		{
			appender.Features.Add(new BreakerFeature<T, TResult>(duration, ratio, period, threshold, predicator));
			return appender;
		}

		return new FeatureBuilder([.. builder.Build(), new BreakerFeature<T, TResult>(duration, ratio, period, threshold, predicator)]);
	}
}
