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
 * Copyright (C) 2010-2023 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
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