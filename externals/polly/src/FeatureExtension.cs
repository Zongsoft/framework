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
using System.Collections.Generic;

using Polly;
using Polly.Timeout;
using Polly.Fallback;

using Zongsoft.Common;
using Zongsoft.Components;
using Zongsoft.Components.Features;

namespace Zongsoft.Externals.Polly;

internal static class FeatureExtension
{
	public static TimeoutStrategyOptions ToStrategy(this TimeoutFeature feature) =>
		feature.Usable() ? new TimeoutStrategyOptions() { Timeout = feature.Timeout } : null;

	public static FallbackStrategyOptions<object> ToStrategy(this FallbackFeature feature)
	{
		if(!feature.Usable(feature => feature.Fallback != null))
			return null;

		var strategy = new FallbackStrategyOptions<object>
		{
			FallbackAction = async argument =>
			{
				await feature.Fallback(null);
				return Outcome.FromResult<object>(null);
			},
			OnFallback = argument => feature.Fallback(null),
		};

		return strategy;
	}
}
