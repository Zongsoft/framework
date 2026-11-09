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
		var options = new Strategies.FallbackStrategyOptions<T, TResult>
		{
			Fallback = feature.Fallback,
		};

		if(feature.Predicator != null)
			options.ShouldHandle = feature.Predicator.PredicateAsync;

		return options;
	}
}
