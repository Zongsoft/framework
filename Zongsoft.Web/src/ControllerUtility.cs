﻿/*
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
using System.Collections.Generic;

using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Zongsoft.Web;

public static class ControllerUtility
{
	public static ControllerServiceDescriptor GetService(this ControllerModel controller)
	{
		return controller != null && controller.Properties.TryGetValue("Service", out var value) ? value as ControllerServiceDescriptor : null;
	}

	public static bool SetService(this ControllerModel controller, ControllerServiceDescriptor service)
	{
		return controller != null && service != null && controller.Properties.TryAdd("Service", service);
	}

	public static object Serializable(this ControllerServiceDescriptor descriptor)
	{
		if(descriptor == null)
			return null;

		return new
		{
			descriptor.Name,
			descriptor.Module,
			descriptor.Namespace,
			descriptor.QualifiedName,
			descriptor.Type,
			descriptor.Model,
			Actions = descriptor.Controller.Actions.Select(action => action.Serializable()),
		};
	}

	public static object Serializable(this ActionModel action)
	{
		if(action == null)
			return null;

		return new
		{
			Name = action.ActionName,
			Routes = GetRoutes(action),
			Parameters = action.Parameters
				.Where(parameter => IsRequestParameter(parameter))
				.Select(parameter => parameter.Serializable()),
		};

		static IEnumerable<object> GetRoutes(ActionModel action)
		{
			if(action == null)
				yield break;

			foreach(var selector in action.Selectors)
			{
				if(selector.AttributeRouteModel != null)
					yield return new
					{
						selector.AttributeRouteModel.Name,
						selector.AttributeRouteModel.Order,
						selector.AttributeRouteModel.Template,
					};
			}
		}

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
			Kind = parameter.BindingInfo?.BindingSource?.Id,
			Optional = parameter.ParameterInfo?.IsOptional ?? false,
		};
	}
}
