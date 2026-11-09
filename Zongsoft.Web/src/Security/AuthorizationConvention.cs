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
		if(!ApplicationContext.Current.Properties.TryGetValue<ControllerServiceDescriptorCollection>(out var descriptors))
			return;

		foreach(var descriptor in descriptors)
		{
			foreach(var controller in descriptor.Controllers)
			{
				var controllerAuthorizable = IsAuthorizable(controller.Attributes, false);

				foreach(var action in controller.Actions)
				{
					var authorizable = IsAuthorizable(action.Attributes, controllerAuthorizable);
					if(!authorizable)
						continue;

					for(int i = 0; i < action.Selectors.Count; i++)
						action.Selectors[i].EndpointMetadata.Add(new RequirementData(action));
				}
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

			if(attribute is AuthorizationAttribute authorization)
			{
				if(authorization.Suppressed)
					return false;

				result = true;
			}
		}

		return result;
	}
	#endregion

	#region 嵌套子类
	#if NET8_0_OR_GREATER
	partial class RequirementData : IAuthorizationRequirementData { }
	#endif

	partial class RequirementData(ActionModel action)
	{
		#region 成员字段
		private readonly ActionModel _action = action;
		#endregion

		#region 公共方法
		public IEnumerable<IAuthorizationRequirement> GetRequirements()
		{
			var descriptor = _action.Controller.GetService();

			yield return new OperationAuthorizationRequirement()
			{
				Name = descriptor == null ? _action.ActionName : $"{descriptor.QualifiedName}:{_action.ActionName}"
			};
		}
		#endregion
	}
	#endregion
}
