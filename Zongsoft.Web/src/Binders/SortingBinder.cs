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
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

using Zongsoft.Data;
using Zongsoft.Common;

namespace Zongsoft.Web.Binders;

public class SortingBinder : IModelBinder
{
	public Task BindModelAsync(ModelBindingContext bindingContext)
	{
		var modelName = bindingContext.ModelName;
		var valueProviderResult = bindingContext.ValueProvider.GetValue(modelName);

		if(valueProviderResult == ValueProviderResult.None)
			return Task.CompletedTask;

		bindingContext.ModelState.SetModelValue(modelName, valueProviderResult);
		var value = valueProviderResult.FirstValue;

		if(string.IsNullOrEmpty(value))
			return Task.CompletedTask;

		var sortings = value.Slice<Sorting>(',', Sorting.TryParse).ToArray();
		bindingContext.Result = ModelBindingResult.Success(sortings);
		return Task.CompletedTask;
	}
}
