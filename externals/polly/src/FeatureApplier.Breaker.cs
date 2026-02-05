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
using System.Threading;
using System.Threading.Tasks;

using Polly;
using Polly.CircuitBreaker;

using Zongsoft.Components;
using Zongsoft.Components.Features;

namespace Zongsoft.Externals.Polly;

partial class FeatureApplier
{
	public readonly IFeatureApplier Breaker = new BreakerApplier();

	private sealed class BreakerApplier : IFeatureApplier<BreakerFeature>
	{
		bool IFeatureApplier.Apply(ResiliencePipelineBuilder builder, IFeature feature) => this.Apply(builder, feature as BreakerFeature);
		public bool Apply(ResiliencePipelineBuilder builder, BreakerFeature feature)
		{
			if(feature == null)
				return false;

			if(!feature.Usable(feature => feature.Duration > TimeSpan.Zero && feature.Threshold > 0))
				return true;

			var strategy = new CircuitBreakerStrategyOptions
			{
				BreakDuration = feature.Duration,
				FailureRatio = feature.FailureRatio,
				SamplingDuration = feature.FailurePeriod,
				MinimumThroughput = feature.Threshold,
			};

			if(feature.DurationFactory != null)
				strategy.BreakDurationGenerator = argument => ValueTask.FromResult(feature.DurationFactory(feature, argument.FailureCount, argument.FailureRate));

			if(feature.Predicator != null)
				strategy.ShouldHandle = argument => feature.Predicator.PredicateAsync(argument.Outcome.GetArgument(), argument.Context.CancellationToken);

			return builder.AddCircuitBreaker(strategy) != null;
		}
	}
}
