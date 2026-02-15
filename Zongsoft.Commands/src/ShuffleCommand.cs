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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Commands library.
 *
 * The Zongsoft.Commands is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Commands is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Commands library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Components;

namespace Zongsoft.Commands;

public class ShuffleCommand : CommandBase<CommandContext>
{
	static readonly MethodInfo ShuffleMethod = null;

	static ShuffleCommand()
	{
		var methods = typeof(Random).GetMethods(BindingFlags.Public | BindingFlags.Instance);

		for(int i = 0; i < methods.Length; i++)
		{
			if(methods[i].IsGenericMethod && methods[i].Name == nameof(Random.Shuffle))
			{
				var parameters = methods[i].GetParameters();

				if(parameters.Length == 1 && parameters[0].ParameterType.IsArray)
				{
					ShuffleMethod = methods[i];
					break;
				}
			}
		}
	}

	protected override ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		if(context.Value == null)
			return default;

		if(context.Value.GetType().IsArray)
		{
			var method = ShuffleMethod.MakeGenericMethod(context.Value.GetType().GetElementType());
			method.Invoke(Random.Shared, [context.Value]);
			return ValueTask.FromResult(context.Value);
		}

		object[] array = null;

		if(context.Value is ICollection collection)
		{
			var index = 0;
			array = new object[collection.Count];

			foreach(var element in collection)
				array[index++] = element;
		}
		else if(context.Value is IEnumerable items)
		{
			var list = new List<object>();

			foreach(var item in items)
				list.Add(item);

			array = [.. list];
		}

		if(array != null)
			Random.Shared.Shuffle(array);

		return ValueTask.FromResult<object>(array);
	}
}
