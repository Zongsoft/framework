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
using System.Collections.Generic;

using Microsoft.Extensions.Configuration;

namespace Zongsoft.Configuration;

public static class ModelConfigurationExtension
{
	public static IConfigurationBuilder AddModels<TModel>(this IConfigurationBuilder builder, IEnumerable<TModel> models, Action<Models.ModelConfigurationSource<TModel>> configureSource = null)
	{
		if(builder == null)
			throw new ArgumentNullException(nameof(builder));

		var source = new Models.ModelConfigurationSource<TModel>() { Models = models };
		configureSource?.Invoke(source);
		return builder.Add(source);
	}
}
