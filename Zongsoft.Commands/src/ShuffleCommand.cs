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
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Components;

namespace Zongsoft.Commands;

public class ShuffleCommand : CommandBase<CommandContext>
{
	static readonly Lazy<MethodInfo> _ShuffleMethod_ = new(() =>
	{
		var methods = typeof(Random).GetMethods(BindingFlags.Public | BindingFlags.Instance);

		for(int i = 0; i < methods.Length; i++)
		{
			if(methods[i].IsGenericMethod && methods[i].Name == nameof(Random.Shuffle))
			{
				var parameters = methods[i].GetParameters();

				if(parameters.Length == 1 && parameters[0].ParameterType.IsArray)
					return methods[i];
			}
		}

		return null;
	}, true);

	protected override ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		if(context.Value == null)
			return default;

		if(context.Value.GetType().IsArray && _ShuffleMethod_.Value != null)
		{
			var method = _ShuffleMethod_.Value.MakeGenericMethod(context.Value.GetType().GetElementType());
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
