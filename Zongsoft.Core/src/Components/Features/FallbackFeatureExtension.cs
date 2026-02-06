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

public static class FallbackFeatureExtension
{
	public static IFeatureBuilder Fallback(this IFeatureBuilder builder, Func<Argument, CancellationToken, ValueTask> fallback, bool enabled = true) => Fallback(builder, fallback, null, enabled);
	public static IFeatureBuilder Fallback(this IFeatureBuilder builder, Func<Argument, CancellationToken, ValueTask> fallback, Common.IPredication<Argument> predicator, bool enabled = true)
	{
		if(builder == null)
			return new FeatureBuilder(new FallbackFeature(fallback, predicator, enabled));

		if(builder is FeatureBuilder appender)
		{
			appender.Features.Add(new FallbackFeature(fallback, predicator, enabled));
			return appender;
		}

		return new FeatureBuilder([.. builder.Build(), new FallbackFeature(fallback, predicator, enabled)]);
	}

	public static IFeatureBuilder Fallback<T>(this IFeatureBuilder builder, Func<Argument<T>, CancellationToken, ValueTask> fallback, bool enabled = true) => Fallback(builder, fallback, null, enabled);
	public static IFeatureBuilder Fallback<T>(this IFeatureBuilder builder, Func<Argument<T>, CancellationToken, ValueTask> fallback, Common.IPredication<Argument<T>> predicator, bool enabled = true)
	{
		if(builder == null)
			return new FeatureBuilder(new FallbackFeature<T>(fallback, predicator, enabled));

		if(builder is FeatureBuilder appender)
		{
			appender.Features.Add(new FallbackFeature<T>(fallback, predicator, enabled));
			return appender;
		}

		return new FeatureBuilder([.. builder.Build(), new FallbackFeature<T>(fallback, predicator, enabled)]);
	}

	public static IFeatureBuilder Fallback<T, TResult>(this IFeatureBuilder builder, Func<Argument<T, TResult>, CancellationToken, ValueTask<TResult>> fallback, bool enabled = true) => Fallback(builder, fallback, null, enabled);
	public static IFeatureBuilder Fallback<T, TResult>(this IFeatureBuilder builder, Func<Argument<T, TResult>, CancellationToken, ValueTask<TResult>> fallback, Common.IPredication<Argument<T, TResult>> predicator, bool enabled = true)
	{
		if(builder == null)
			return new FeatureBuilder(new FallbackFeature<T, TResult>(fallback, predicator, enabled));

		if(builder is FeatureBuilder appender)
		{
			appender.Features.Add(new FallbackFeature<T, TResult>(fallback, predicator, enabled));
			return appender;
		}

		return new FeatureBuilder([.. builder.Build(), new FallbackFeature<T, TResult>(fallback, predicator, enabled)]);
	}
}
