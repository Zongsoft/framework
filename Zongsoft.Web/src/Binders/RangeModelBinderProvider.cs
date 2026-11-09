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

using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Zongsoft.Web.Binders;

[Zongsoft.Services.Service]
public class RangeModelBinderProvider : IModelBinderProvider
{
	public IModelBinder GetBinder(ModelBinderProviderContext context)
	{
		var modelType = context.Metadata.UnderlyingOrModelType;

		if(modelType.IsGenericType && modelType.GenericTypeArguments.Length == 1 && modelType.GetGenericTypeDefinition() == typeof(Zongsoft.Data.Range<>))
			return (IModelBinder)Activator.CreateInstance(typeof(RangeModelBinder<>).MakeGenericType(modelType.GenericTypeArguments[0]));

		return null;
	}
}
