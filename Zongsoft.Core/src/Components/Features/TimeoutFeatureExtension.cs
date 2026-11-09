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

public static class TimeoutFeatureExtension
{
	public static IFeatureBuilder Timeout(this IFeatureBuilder builder, int timeout, Func<TimeoutArgument, CancellationToken, ValueTask> onTimeout = null) => builder.Timeout(TimeSpan.FromMilliseconds(timeout), onTimeout);
	public static IFeatureBuilder Timeout(this IFeatureBuilder builder, TimeSpan timeout, Func<TimeoutArgument, CancellationToken, ValueTask> onTimeout = null)
	{
		if(builder == null)
			return new FeatureBuilder(new TimeoutFeature(timeout) { OnTimeout = onTimeout });

		if(builder is FeatureBuilder appender)
		{
			appender.Features.Add(new TimeoutFeature(timeout) { OnTimeout = onTimeout });
			return appender;
		}

		return new FeatureBuilder([.. builder.Build(), new TimeoutFeature(timeout) { OnTimeout = onTimeout }]);
	}
	public static IFeatureBuilder Timeout(this IFeatureBuilder builder, Func<TimeoutArgument, CancellationToken, ValueTask<TimeSpan>> timeout, Func<TimeoutArgument, CancellationToken, ValueTask> onTimeout = null)
	{
		if(builder == null)
			return new FeatureBuilder(new TimeoutFeature(timeout) { OnTimeout = onTimeout });

		if(builder is FeatureBuilder appender)
		{
			appender.Features.Add(new TimeoutFeature(timeout) { OnTimeout = onTimeout });
			return appender;
		}

		return new FeatureBuilder([.. builder.Build(), new TimeoutFeature(timeout) { OnTimeout = onTimeout }]);
	}

	public static IFeatureBuilder Timeout<T>(this IFeatureBuilder builder, int timeout, Func<TimeoutArgument<T>, CancellationToken, ValueTask> onTimeout = null) => builder.Timeout(TimeSpan.FromMilliseconds(timeout), onTimeout);
	public static IFeatureBuilder Timeout<T>(this IFeatureBuilder builder, TimeSpan timeout, Func<TimeoutArgument<T>, CancellationToken, ValueTask> onTimeout = null)
	{
		if(builder == null)
			return new FeatureBuilder(new TimeoutFeature<T>(timeout) { OnTimeout = onTimeout });

		if(builder is FeatureBuilder appender)
		{
			appender.Features.Add(new TimeoutFeature<T>(timeout) { OnTimeout = onTimeout });
			return appender;
		}

		return new FeatureBuilder([.. builder.Build(), new TimeoutFeature<T>(timeout) { OnTimeout = onTimeout }]);
	}
	public static IFeatureBuilder Timeout<T>(this IFeatureBuilder builder, Func<TimeoutArgument<T>, CancellationToken, ValueTask<TimeSpan>> timeout, Func<TimeoutArgument<T>, CancellationToken, ValueTask> onTimeout = null)
	{
		if(builder == null)
			return new FeatureBuilder(new TimeoutFeature<T>(timeout) { OnTimeout = onTimeout });

		if(builder is FeatureBuilder appender)
		{
			appender.Features.Add(new TimeoutFeature<T>(timeout) { OnTimeout = onTimeout });
			return appender;
		}

		return new FeatureBuilder([.. builder.Build(), new TimeoutFeature<T>(timeout) { OnTimeout = onTimeout }]);
	}

	public static IFeatureBuilder Timeout<T, TResult>(this IFeatureBuilder builder, int timeout, Func<TimeoutArgument<T, TResult>, CancellationToken, ValueTask> onTimeout = null) => builder.Timeout(TimeSpan.FromMilliseconds(timeout), onTimeout);
	public static IFeatureBuilder Timeout<T, TResult>(this IFeatureBuilder builder, TimeSpan timeout, Func<TimeoutArgument<T, TResult>, CancellationToken, ValueTask> onTimeout = null)
	{
		if(builder == null)
			return new FeatureBuilder(new TimeoutFeature<T, TResult>(timeout) { OnTimeout = onTimeout });

		if(builder is FeatureBuilder appender)
		{
			appender.Features.Add(new TimeoutFeature<T, TResult>(timeout) { OnTimeout = onTimeout });
			return appender;
		}

		return new FeatureBuilder([.. builder.Build(), new TimeoutFeature<T, TResult>(timeout) { OnTimeout = onTimeout }]);
	}
	public static IFeatureBuilder Timeout<T, TResult>(this IFeatureBuilder builder, Func<TimeoutArgument<T, TResult>, CancellationToken, ValueTask<TimeSpan>> timeout, Func<TimeoutArgument<T, TResult>, CancellationToken, ValueTask> onTimeout = null)
	{
		if(builder == null)
			return new FeatureBuilder(new TimeoutFeature<T, TResult>(timeout) { OnTimeout = onTimeout });

		if(builder is FeatureBuilder appender)
		{
			appender.Features.Add(new TimeoutFeature<T, TResult>(timeout) { OnTimeout = onTimeout });
			return appender;
		}

		return new FeatureBuilder([.. builder.Build(), new TimeoutFeature<T, TResult>(timeout) { OnTimeout = onTimeout }]);
	}
}
