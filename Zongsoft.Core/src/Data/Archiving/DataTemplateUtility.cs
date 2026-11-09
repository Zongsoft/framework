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

using Zongsoft.Services;

namespace Zongsoft.Data.Archiving;

internal static class DataTemplateUtility
{
	public static IDataTemplate GetTemplate(this IServiceProvider serviceProvider, string name, string format = null)
	{
		if(serviceProvider == null)
			throw new ArgumentNullException(nameof(serviceProvider));

		foreach(var provider in serviceProvider.ResolveAll<IDataTemplateProvider>())
		{
			var template = provider.GetTemplate(name, format);

			if(template != null)
				return template;
		}

		return null;
	}
}