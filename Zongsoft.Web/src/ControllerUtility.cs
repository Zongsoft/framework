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
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace Zongsoft.Web;

public static class ControllerUtility
{
	public static string GetName(Type controllerType)
	{
		var attribute = controllerType.GetCustomAttribute<ControllerNameAttribute>(true);

		if(attribute != null && !string.IsNullOrEmpty(attribute.Name))
			return attribute.Name;

		return controllerType.Name.Length > 10 && controllerType.Name.EndsWith("Controller", StringComparison.OrdinalIgnoreCase) ?
			controllerType.Name[..^10] : controllerType.Name;
	}

	public static string GetNamespace(Type controllerType, char separator)
	{
		if(controllerType == null || !controllerType.IsNested)
			return null;

		var stack = new Stack<Type>();
		var type = controllerType.DeclaringType;

		while(type != null)
		{
			if(ControllerFeatureProvider.IsControllerType(type))
				stack.Push(type);

			type = type.DeclaringType;
		}

		return string.Join(separator, stack.Select(GetName));
	}

	public static string GetQualifiedName(Type controllerType, char separator)
	{
		var @namespace = GetNamespace(controllerType, separator);

		return string.IsNullOrEmpty(@namespace) ?
			GetName(controllerType) :
			$"{@namespace}{separator}{GetName(controllerType)}";
	}
}
