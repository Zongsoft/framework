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
