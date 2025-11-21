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
using System.Net.Http;
using System.Collections.Generic;

using Microsoft.AspNetCore.Mvc.Routing;
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
			descriptor.Title,
			descriptor.Description,
			Controllers = descriptor.Controllers.Select(controller => new
			{
				controller.ControllerName,
				controller.ControllerType,
				controller.ServiceType,
				controller.Model,
				Routes = controller.Selectors.Select(selector => Serializable(selector)),
			}),
			Operations = descriptor.Operations.Select(operation => new
			{
				operation.Name,
				operation.Title,
				operation.Aliases,
				operation.Description,
				Action = operation.Action.Serializable(),
			}),
		};
	}

	public static object Serializable(this ActionModel action)
	{
		if(action == null)
			return null;

		return new
		{
			Name = action.ActionName,
			Routes = action.Selectors.Select(selector => Serializable(selector)),
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

	public static object Serializable(this SelectorModel selector)
	{
		if(selector == null || selector.AttributeRouteModel == null)
			return null;

		var method = GetMethod(selector.AttributeRouteModel.Attribute);

		return string.IsNullOrEmpty(method) ?
			selector.AttributeRouteModel.Template :
			$"{method} {selector.AttributeRouteModel.Template}";

		static string GetMethod(object metadata)
		{
			if(metadata is IActionHttpMethodProvider provider && provider.HttpMethods.Any())
				return string.Join(",", provider.HttpMethods);

			return null;
		}
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

	public static IReadOnlyCollection<HttpMethod> GetHttpMethods(this ActionModel action)
	{
		if(action == null)
			return null;

		var result = new HashSet<HttpMethod>();

		for(int i = 0; i < action.Selectors.Count; i++)
		{
			foreach(var method in Find(action.Selectors[i].EndpointMetadata))
				result.Add(method);
		}

		foreach(var method in Find(action.Attributes))
			result.Add(method);

		return result;

		static IEnumerable<HttpMethod> Find(IEnumerable<object> metadatas)
		{
			foreach(var metadata in metadatas)
			{
				if(metadata is HttpMethod method)
					yield return method;

				if(metadata is HttpMethodAttribute attribute)
					foreach(var text in attribute.HttpMethods)
						yield return HttpMethod.Parse(text);

				if(metadata is Microsoft.AspNetCore.Routing.HttpMethodMetadata methodMetadata)
					foreach(var text in methodMetadata.HttpMethods)
						yield return HttpMethod.Parse(text);
			}
		}
	}
}
