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
 * This file is part of Zongsoft.Web library.
 *
 * The Zongsoft.Web is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Web is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Web library. If not, see <http://www.gnu.org/licenses/>.
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
