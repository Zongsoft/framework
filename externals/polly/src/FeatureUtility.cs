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
using System.Runtime.CompilerServices;

using Zongsoft.Collections;

namespace Zongsoft.Externals.Polly;

internal static class FeatureUtility
{
	public static TArgument GetArgument<TState, TArgument>(TState state) => GetArgument<TState, TArgument>(state, out _);
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
				var entry = tuple[i];

				if(entry is TArgument value)
				{
					parameters = null;
					return value;
				}

				if(entry is ITuple args && args.Length == 2 &&
				   args[0] is TArgument arg1 && args[1] is Parameters arg2)
				{
					parameters = arg2;
					return arg1;
				}
			}
		}

		parameters = null;
		return default;
	}
}
