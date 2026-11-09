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
using Zongsoft.Resources;

namespace Zongsoft.Data.Metadata;

public static class DataEntityPropertyUtility
{
	public static bool IsPrimaryKey(this IDataEntityProperty property) => property is IDataEntitySimplexProperty simplex && simplex.IsPrimaryKey;

	public static bool IsSimplex(this IDataEntityProperty property, out IDataEntitySimplexProperty simplex)
	{
		if(property != null && property.IsSimplex)
		{
			simplex = (IDataEntitySimplexProperty)property;
			return true;
		}

		simplex = null;
		return false;
	}

	public static bool IsComplex(this IDataEntityProperty property, out IDataEntityComplexProperty complex)
	{
		if(property != null && property.IsComplex)
		{
			complex = (IDataEntityComplexProperty)property;
			return true;
		}

		complex = null;
		return false;
	}

	public static string GetLabel(this IDataEntityProperty property)
	{
		if(property == null || property.Entity == null || ApplicationContext.Current == null)
			return null;

		IApplicationModule module;

		if(string.IsNullOrEmpty(property.Entity.Namespace))
		{
			if(ApplicationContext.Current.Modules.TryGetValue(string.Empty, out module))
				return ResourceUtility.GetResourceString(module.Assembly, [$"{property.Entity.Name}.{property.Name}.Label", $"{property.Entity.Name}.{property.Name}", $"{property.Name}.Label", property.Name]);

			if(ApplicationContext.Current.Modules.TryGetValue("Common", out module))
				return ResourceUtility.GetResourceString(module.Assembly, [$"{property.Entity.Name}.{property.Name}.Label", $"{property.Entity.Name}.{property.Name}", $"{property.Name}.Label", property.Name]);
		}

		if(ApplicationContext.Current.Modules.TryGetValue(property.Entity.Namespace, out module))
			return ResourceUtility.GetResourceString(module.Assembly, [$"{property.Entity.Name}.{property.Name}.Label", $"{property.Entity.Name}.{property.Name}", $"{property.Name}.Label", property.Name]);

		return null;
	}

	public static string GetDescription(this IDataEntityProperty property)
	{
		if(property == null || property.Entity == null || ApplicationContext.Current == null)
			return null;

		IApplicationModule module;

		if(string.IsNullOrEmpty(property.Entity.Namespace))
		{
			if(ApplicationContext.Current.Modules.TryGetValue(string.Empty, out module))
				return ResourceUtility.GetResourceString(module.Assembly, [$"{property.Entity.Name}.{property.Name}.Description", $"{property.Name}.Description"]);

			if(ApplicationContext.Current.Modules.TryGetValue("Common", out module))
				return ResourceUtility.GetResourceString(module.Assembly, [$"{property.Entity.Name}.{property.Name}.Description", $"{property.Name}.Description"]);
		}

		if(ApplicationContext.Current.Modules.TryGetValue(property.Entity.Namespace, out module))
			return ResourceUtility.GetResourceString(module.Assembly, [$"{property.Entity.Name}.{property.Name}.Description", $"{property.Name}.Description"]);

		return null;
	}
}