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

using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Zongsoft.Web;

public static class ControllerUtility
{
	public static string GetName(Type controllerType)
	{
		var attribute = controllerType.GetCustomAttribute<ControllerNameAttribute>(true);

		if(attribute != null && !string.IsNullOrEmpty(attribute.Name))
			return attribute.Name;

		const string CONTROLLER_SUFFIX = "Controller";
		const string CONTROLLER_BASE_SUFFIX = "ControllerBase";

		if(controllerType.Name.Length > CONTROLLER_SUFFIX.Length && controllerType.Name.EndsWith(CONTROLLER_SUFFIX, StringComparison.OrdinalIgnoreCase))
			return controllerType.Name[..^CONTROLLER_SUFFIX.Length];

		if(controllerType.Name.Length > CONTROLLER_BASE_SUFFIX.Length && controllerType.Name.EndsWith(CONTROLLER_BASE_SUFFIX, StringComparison.OrdinalIgnoreCase))
			return controllerType.Name[..^CONTROLLER_BASE_SUFFIX.Length];

		return controllerType.Name;
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

	public static object Serializable(this ControllerModel controller)
	{
		if(controller == null)
			return null;

		controller.Properties.TryGetValue("Model", out var model);
		controller.Properties.TryGetValue("QualifiedName", out var qualifiedName);

		return new
		{
			Name = controller.ControllerName,
			QualifiedName = qualifiedName,
			Type = controller.ControllerType,
			Model = model,
			Actions = controller.Actions.Select(action => action.Serializable()),
		};
	}

	public static object Serializable(this ActionModel action)
	{
		if(action == null)
			return null;

		return new
		{
			Name = action.ActionName,
			Parameters = action.Parameters
				.Where(parameter => IsRequestParameter(parameter))
				.Select(parameter => parameter.Serializable()),
		};

		static bool IsRequestParameter(ParameterModel parameter) =>
			parameter != null &&
			parameter.BindingInfo != null &&
			parameter.BindingInfo.BindingSource != null &&
			parameter.BindingInfo.BindingSource.IsFromRequest;
	}

	public static object Serializable(this ParameterModel parameter)
	{
		if(parameter == null)
			return null;

		return new
		{
			parameter.Name,
			Type = parameter.ParameterType,
			Source = parameter.BindingInfo.BindingSource.Id,
		};
	}
}
