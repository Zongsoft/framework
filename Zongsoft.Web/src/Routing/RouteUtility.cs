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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Collections.Generic;

using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Zongsoft.Web.Routing;

public static class RouteUtility
{
	public static BindingSource GetParameterSource(this ParameterModel parameter)
	{
		if(parameter == null)
			return null;

		if(parameter.BindingInfo == null || parameter.BindingInfo.BindingSource == null)
		{
			var patterns = parameter.Action.GetRoutePatterns();

			foreach(var pattern in patterns)
			{
				if(pattern.Contains(parameter.Name))
					return BindingSource.Path;
			}

			return null;
		}

		return parameter.BindingInfo.BindingSource;
	}

	public static IEnumerable<RoutePattern> GetRoutePatterns(this ControllerServiceDescriptor descriptor)
	{
		if(descriptor == null)
			yield break;

		foreach(var controller in descriptor.Controllers)
		{
			foreach(var pattern in GetRoutePatterns(controller.Controller))
				yield return pattern;
		}
	}

	public static IEnumerable<RoutePattern> GetRoutePatterns(this ControllerServiceDescriptor.ControllerDescriptor descriptor)
	{
		return descriptor == null ? [] : GetRoutePatterns(descriptor.Controller);
	}

	public static IEnumerable<RoutePattern> GetRoutePatterns(this ControllerModel controller)
	{
		var templates = GetRouteTemplates(controller?.Selectors);

		foreach(var template in templates)
			yield return RoutePattern.Resolve(template).Map(controller.RouteValues);
	}

	public static IEnumerable<RoutePattern> GetRoutePatterns(this ControllerServiceDescriptor.ControllerOperationDescriptor descriptor)
	{
		return descriptor == null ? [] : descriptor.Action.GetRoutePatterns();
	}

	public static IEnumerable<RoutePattern> GetRoutePatterns(this ActionModel action)
	{
		if(action == null)
			yield break;

		var prefixes = GetRouteTemplates(action.Controller.Selectors);
		var templates = GetRouteTemplates(action.Selectors);

		if(prefixes.Count == 0)
		{
			foreach(var template in templates)
				yield return GetRouteTemplate(template, null)
					.Map(action.Controller.RouteValues)
					.Map(action.RouteValues);
		}
		else
		{
			foreach(var prefix in prefixes)
			{
				foreach(var template in templates)
					yield return GetRouteTemplate(template, prefix)
						.Map(action.Controller.RouteValues)
						.Map(action.RouteValues);
			}
		}

		static RoutePattern GetRouteTemplate(string template, string prefix)
		{
			string url;

			if(string.IsNullOrEmpty(template))
				url = prefix;
			else if(template.StartsWith('/') || template.StartsWith("~/"))
				url = template;
			else
				url = string.IsNullOrEmpty(prefix) ? template : $"{prefix}{(prefix.EndsWith('/') ? null : '/')}{template}";

			return RoutePattern.Resolve(url);
		}
	}

	private static RoutePattern Map(this RoutePattern pattern, IEnumerable<KeyValuePair<string, string>> parameters)
	{
		if(pattern == null || parameters == null)
			return pattern;

		foreach(var parameter in parameters)
			pattern[parameter.Key]?.Value = parameter.Value;

		return pattern;
	}

	private static IReadOnlyCollection<string> GetRouteTemplates(IList<SelectorModel> selectors)
	{
		if(selectors == null || selectors.Count == 0)
			return [];

		var result = new HashSet<string>(selectors.Count, StringComparer.OrdinalIgnoreCase);

		for(int i = 0; i < selectors.Count; i++)
		{
			var selector = selectors[i];

			if(selector.AttributeRouteModel != null)
				result.Add(selector.AttributeRouteModel.Template);

			foreach(var metadata in selector.EndpointMetadata)
			{
				if(metadata is IRouteTemplateProvider route)
					result.Add(route.Template ?? string.Empty);
			}
		}

		return result;
	}
}
