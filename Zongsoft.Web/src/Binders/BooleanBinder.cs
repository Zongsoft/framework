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
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Zongsoft.Web.Binders;

public class BooleanBinder : IModelBinder
{
	public Task BindModelAsync(ModelBindingContext context)
	{
		var modelName = context.ModelName;
		var providerResult = context.ValueProvider.GetValue(modelName);

		if(providerResult == ValueProviderResult.None)
			return Task.CompletedTask;

		context.ModelState.SetModelValue(modelName, providerResult);
		var value = providerResult.FirstValue;

		if(string.IsNullOrEmpty(value))
		{
			context.Result = ModelBindingResult.Success(true);
			return Task.CompletedTask;
		}

		if(TryParse(value, out var result))
			context.Result = ModelBindingResult.Success(result);
		else
			context.Result = ModelBindingResult.Failed();

		return Task.CompletedTask;
	}

	private static bool TryParse(ReadOnlySpan<char> text, out bool result)
	{
		if(text.IsEmpty || text.Length > 10)
		{
			result = false;
			return false;
		}

		unsafe
		{
			Span<char> span = stackalloc char[text.Length];
			text.ToLowerInvariant(span);

			switch(span)
			{
				case "1":
				case "true":
				case "yes":
				case "on":
				case "enable":
				case "enabled":
					result = true;
					return true;
				case "0":
				case "false":
				case "no":
				case "off":
				case "disable":
				case "disabled":
					result = false;
					return true;
			}
		}

		result = false;
		return false;
	}
}
