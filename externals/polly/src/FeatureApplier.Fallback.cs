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
using System.Reflection;

using Polly;
using Polly.Fallback;

using Zongsoft.Common;
using Zongsoft.Components;
using Zongsoft.Components.Features;

namespace Zongsoft.Externals.Polly;

partial class FeatureApplier
{
    private sealed class FallbackApplier : IFeatureApplier
	{
		public bool Apply(ResiliencePipelineBuilder builder, IFeature feature) => false;
		public bool Apply<TResult>(ResiliencePipelineBuilder builder, FallbackFeature<TResult> feature)
		{
			if(!feature.Usable(feature => feature.Fallback != null))
				return false;

			var strategy = new FallbackStrategyOptions<TResult>
			{
				FallbackAction = async argument =>
				{
					try
					{
						var result = await feature.Fallback(argument.Outcome.GetArgument(), argument.Context.CancellationToken);
						return Outcome.FromResult(result);
					}
					catch(Exception ex)
					{
						return Outcome.FromException<TResult>(ex);
					}
				},
			};

			if(feature.Predicator != null)
				strategy.ShouldHandle = argument => feature.Predicator.PredicateAsync(argument.Outcome.GetArgument(), argument.Context.CancellationToken);

			var b = new ResiliencePipelineBuilder<TResult>()
			{
				Name = builder.Name,
				InstanceName = builder.InstanceName,
				ContextPool = builder.ContextPool,
				TimeProvider = builder.TimeProvider,
			};

			return b.AddFallback(strategy) != null;
		}
    }
}
