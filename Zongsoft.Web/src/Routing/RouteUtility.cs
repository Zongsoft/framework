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
using System.Linq;
using System.Collections.Generic;

using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Zongsoft.Web.Routing;

public static class RouteUtility
{
	public static IEnumerable<RoutePattern> GetRoutePatterns(this ControllerServiceDescriptor descriptor, params IEnumerable<KeyValuePair<string, string>> parameters)
	{
		if(descriptor == null)
			yield break;

		foreach(var controller in descriptor.Controllers)
		{
			foreach(var pattern in GetRoutePatterns(controller.Controller, parameters))
				yield return pattern;
		}
	}

	public static IEnumerable<RoutePattern> GetRoutePatterns(this ControllerServiceDescriptor.ControllerDescriptor descriptor, params IEnumerable<KeyValuePair<string, string>> parameters)
	{
		if(descriptor == null)
			return [];

		return GetRoutePatterns(descriptor.Controller);
	}

	public static IEnumerable<RoutePattern> GetRoutePatterns(this ControllerModel controller, params IEnumerable<KeyValuePair<string, string>> parameters)
	{
		if(controller == null)
			yield break;

		var templates = GetRouteTemplates(controller.Selectors);

		foreach(var template in templates)
		{
			var pattern = RoutePattern.Resolve(template);

			if(parameters == null)
				pattern.Map(controller.RouteValues);
			else
				pattern.Map(parameters.Concat(controller.RouteValues));

			yield return pattern;
		}
	}

	public static IEnumerable<RoutePattern> GetRoutePatterns(this ControllerServiceDescriptor.ControllerOperationDescriptor descriptor, params IEnumerable<KeyValuePair<string, string>> parameters)
	{
		if(descriptor == null)
			return [];

		return descriptor.Action.GetRoutePatterns(parameters);
	}

	public static IEnumerable<RoutePattern> GetRoutePatterns(this ActionModel action, params IEnumerable<KeyValuePair<string, string>> parameters)
	{
		if(action == null)
			yield break;

		var prefixes = GetRouteTemplates(action.Controller.Selectors);
		var templates = GetRouteTemplates(action.Selectors);

		foreach(var prefix in prefixes)
		{
			foreach(var template in templates)
			{
				var url = template ?? string.Empty;
				if(!url.StartsWith('/') && !url.StartsWith("~/"))
					url = $"{prefix}{(prefix.EndsWith('/') ? null : '/')}{url}";

				var pattern = RoutePattern.Resolve(url);

				if(parameters == null)
					pattern.Map(action.RouteValues);
				else
					pattern.Map(parameters.Concat(action.RouteValues));

				yield return pattern;
			}
		}
	}

	private static IEnumerable<string> GetRouteTemplates(IList<SelectorModel> selectors)
	{
		if(selectors == null)
			yield break;

		for(int i = 0; i < selectors.Count; i++)
		{
			var selector = selectors[i];

			if(selector.AttributeRouteModel != null)
				yield return selector.AttributeRouteModel.Template;
		}
	}
}
