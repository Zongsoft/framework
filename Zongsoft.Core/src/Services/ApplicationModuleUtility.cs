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

namespace Zongsoft.Services;

internal static class ApplicationModuleUtility
{
	extension(IApplicationModule module)
	{
		public ApplicationIdentifier Identifier => module == null ? default : new(module.Name, module.Edition, module.Version);
	}

	internal static string GetTitle(this IApplicationModule module)
	{
		if(module != null && module.Assembly != null && module.Name != null)
			return Resources.ResourceUtility.GetResourceString(module.Assembly, [$"{module.Name}.{nameof(module.Title)}", module.Name]);

		return null;
	}

	internal static string GetDescription(this IApplicationModule module)
	{
		if(module != null && module.Assembly != null && module.Name != null)
			return Resources.ResourceUtility.GetResourceString(module.Assembly, $"{module.Name}.{nameof(module.Description)}");

		return null;
	}
}
