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

using Polly;

using Zongsoft.Components;
using Zongsoft.Components.Features;

namespace Zongsoft.Externals.Polly;

partial class FeatureExtension
{
	public static void AddFallback(this ResiliencePipelineBuilder builder, Strategies.FallbackStrategyOptions options)
	{
		if(options == null)
			return;

		builder.AddStrategy(context => new Strategies.FallbackStrategy(options, context.Telemetry), options);
	}

	public static void AddFallback<T>(this ResiliencePipelineBuilder builder, Strategies.FallbackStrategyOptions<T> options)
	{
		if(options == null)
			return;

		builder.AddStrategy(context => new Strategies.FallbackStrategy<T>(options, context.Telemetry), options);
	}

	public static void AddFallback<T, TResult>(this ResiliencePipelineBuilder<TResult> builder, Strategies.FallbackStrategyOptions<T, TResult> options)
	{
		if(options == null)
			return;

		builder.AddStrategy(context => new Strategies.FallbackStrategy<T, TResult>(options, context.Telemetry), options);
	}

	public static Strategies.FallbackStrategyOptions ToStrategy(this FallbackFeature feature)
	{
		if(!feature.Usable(feature => feature.Fallback != null))
			return null;

		var options = new Strategies.FallbackStrategyOptions
		{
			Fallback = feature.Fallback,
		};

		if(feature.Predicator != null)
			options.ShouldHandle = feature.Predicator.PredicateAsync;

		return options;
	}

	public static Strategies.FallbackStrategyOptions<T> ToStrategy<T>(this FallbackFeature<T> feature)
	{
		if(!feature.Usable(feature => feature.Fallback != null))
			return null;

		var options = new Strategies.FallbackStrategyOptions<T>
		{
			Fallback = feature.Fallback,
		};

		if(feature.Predicator != null)
			options.ShouldHandle = feature.Predicator.PredicateAsync;

		return options;
	}

	public static Strategies.FallbackStrategyOptions<T, TResult> ToStrategy<T, TResult>(this FallbackFeature<T, TResult> feature)
	{
		if(!feature.Usable(feature => feature.Fallback != null))
			return null;

		var options = new Strategies.FallbackStrategyOptions<T, TResult>
		{
			Fallback = feature.Fallback,
		};

		if(feature.Predicator != null)
			options.ShouldHandle = feature.Predicator.PredicateAsync;

		return options;
	}
}
