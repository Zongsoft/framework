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
using System.Runtime.CompilerServices;

using Zongsoft.Collections;

namespace Zongsoft.Externals.Polly;

internal static class FeaturePipelineArgument
{
	public static TArgument GetArgument<TState, TArgument>(TState state, out Parameters parameters)
	{
		if(state is null)
		{
			parameters = null;
			return default;
		}

		if(state is TArgument result)
		{
			parameters = null;
			return result;
		}

		if(state is ITuple tuple)
		{
			for(int i = 0; i < tuple.Length; i++)
			{
				if(tuple[i] is TArgument ax)
				{
					parameters = null;
					return ax;
				}

				if(tuple[i] is ITuple args && args.Length == 2 && args[0] is TArgument a1 && args[1] is Parameters ps)
				{
					parameters = ps;
					return a1;
				}

				if(tuple[i] is IFeaturePipelineArgument arg)
				{
					parameters = arg.Parameters;
					return arg.Value is TArgument value ? value : default;
				}
			}
		}

		parameters = null;
		return default;
	}
}

internal interface IFeaturePipelineArgument
{
	object Value { get; }
	Parameters Parameters { get; }
}

internal readonly struct FeaturePipelineArgument<T>(T value, Parameters parameters) : IFeaturePipelineArgument
{
	public readonly T Value = value;
	public readonly Parameters Parameters = parameters;

	object IFeaturePipelineArgument.Value => this.Value;
	Parameters IFeaturePipelineArgument.Parameters => this.Parameters;

	public override string ToString() => this.Value?.ToString();
}
