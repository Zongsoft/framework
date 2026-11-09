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
using Polly.Timeout;

using Zongsoft.Components;
using Zongsoft.Components.Features;

namespace Zongsoft.Externals.Polly;

partial class FeatureExtension
{
	public static TimeoutStrategyOptions ToStrategy(this TimeoutFeature feature)
	{
		var options = new TimeoutStrategyOptions() { Timeout = feature.Timeout };
		if(feature.TimeoutGenerator != null)
			options.TimeoutGenerator = args => feature.TimeoutGenerator(new(options.Timeout), args.Context.CancellationToken);
		if(feature.OnTimeout != null)
			options.OnTimeout = args => feature.OnTimeout(new(args.Timeout), args.Context.CancellationToken);

		return options;
	}

	public static TimeoutStrategyOptions ToStrategy<TArgument>(this TimeoutFeature<TArgument> feature)
	{
		var options = new TimeoutStrategyOptions() { Timeout = feature.Timeout };
		if(feature.TimeoutGenerator != null)
			options.TimeoutGenerator = args => feature.TimeoutGenerator(new(options.Timeout), args.Context.CancellationToken);
		if(feature.OnTimeout != null)
			options.OnTimeout = args => feature.OnTimeout(new(args.Timeout), args.Context.CancellationToken);

		return options;
	}

	public static TimeoutStrategyOptions ToStrategy<TArgument, TResult>(this TimeoutFeature<TArgument, TResult> feature)
	{
		var options = new TimeoutStrategyOptions() { Timeout = feature.Timeout };
		if(feature.TimeoutGenerator != null)
			options.TimeoutGenerator = args => feature.TimeoutGenerator(new(options.Timeout), args.Context.CancellationToken);
		if(feature.OnTimeout != null)
			options.OnTimeout = args => feature.OnTimeout(new(args.Timeout), args.Context.CancellationToken);

		return options;
	}
}
