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

internal static class FeatureUtility
{
	private static readonly MethodInfo Method;

	static FeatureUtility()
	{
		var methods = typeof(FeatureExtension).GetMethods(BindingFlags.Static | BindingFlags.Public);

		for(int i = 0; i < methods.Length; i++)
		{
			var method = methods[i];

			if(method.IsGenericMethod && method.Name == nameof(FeatureExtension.AddFallback))
			{
				var parameters = method.GetParameters();
				if(parameters.Length == 2 && parameters[1].ParameterType.IsGenericType && parameters[1].ParameterType.GetGenericTypeDefinition() == typeof(FallbackFeature<>))
				{
					Method = method;
					break;
				}
			}
		}
	}

	public static ResiliencePipelineBuilder AddFallback(this ResiliencePipelineBuilder builder, IFeature feature)
	{
		if(feature == null)
			return builder;

		var featureType = feature.GetType();
		if(featureType.IsGenericType && featureType.GetGenericTypeDefinition() == typeof(FallbackFeature<>))
		{
			var method = Method.MakeGenericMethod(featureType.GetGenericArguments());
			method.Invoke(null, [builder, feature]);
		}

		return builder;
	}

}
