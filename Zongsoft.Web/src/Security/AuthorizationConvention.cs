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

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

using Zongsoft.Services;

namespace Zongsoft.Web.Security;

public class AuthorizationConvention : IApplicationModelConvention
{
	#region 公共方法
	public void Apply(ApplicationModel application)
	{
		if(!ApplicationContext.Current.Properties.TryGetValue<ControllerDescriptorCollection>(out var controllers))
			return;

		foreach(var controller in controllers)
		{
			var controllerAuthorizable = IsAuthorizable(controller.Attributes, false);

			foreach(var action in controller.Actions)
			{
				var authorizable = IsAuthorizable(action.Attributes, controllerAuthorizable);
				if(!authorizable)
					continue;

				for(int i = 0; i < action.Selectors.Count; i++)
					action.Selectors[i].EndpointMetadata.Add(new RequirementData(controller, action));
			}
		}
	}
	#endregion

	#region 私有方法
	private static bool IsAuthorizable(IEnumerable<object> attributes, bool inheritedValue)
	{
		if(attributes == null)
			return inheritedValue;

		var result = inheritedValue;

		foreach(var attribute in attributes)
		{
			if(typeof(IAllowAnonymous).IsAssignableFrom(attribute.GetType()))
				return false;
			if(typeof(IAuthorizeData).IsAssignableFrom(attribute.GetType()))
				result = true;
		}

		return result;
	}
	#endregion

	#region 嵌套子类
	#if NET8_0_OR_GREATER
	partial class RequirementData : IAuthorizationRequirementData { }
	#endif

	partial class RequirementData(ControllerDescriptor controller, ActionModel action)
	{
		private readonly ControllerDescriptor _controller = controller;
		private readonly ActionModel _action = action;

		public IEnumerable<IAuthorizationRequirement> GetRequirements()
		{
			yield return new OperationAuthorizationRequirement()
			{
				Name = $"{_controller.Service.QualifiedName}:{_action.ActionName}"
			};
		}
	}
	#endregion
}
