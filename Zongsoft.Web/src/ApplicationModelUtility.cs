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
 * Copyright (C) 2010-2024 Zongsoft Studio <http://www.zongsoft.com>
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

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

using Zongsoft.Data;
using Zongsoft.Services;

namespace Zongsoft.Web
{
	public static class ApplicationModelUtility
	{
		public static ICollection<ControllerModel> GetControllers(string module) => ApplicationContext.Current.Modules.TryGetValue(module ?? string.Empty, out var m) ? GetControllers(m) : null;
		public static ICollection<ControllerModel> GetControllers(IApplicationModule module)
		{
			if(module == null)
				return null;
			if(!ApplicationContext.Current.Properties.TryGetValue(nameof(ApplicationModel), out var value) || value is not ApplicationModel applicationModel)
				return null;

			var controllers = new List<ControllerModel>();

			foreach(var controller in applicationModel.Controllers)
			{
				if(controller.RouteValues.TryGetValue("module", out var moduleName) && string.Equals(module.Name, moduleName))
					controllers.Add(controller);
			}

			return controllers;
		}

		public static ModelDescriptor GetModel(ControllerModel controller)
		{
			if(Zongsoft.Common.TypeExtension.IsAssignableFrom(typeof(ServiceControllerBase<,>), controller.ControllerType, out var genericTypes) && genericTypes.Count > 0)
			{
				var modelType = genericTypes[0].GenericTypeArguments[0];
				var serviceType = genericTypes[0].GenericTypeArguments[1];

				if(ApplicationContext.Current.Services.GetService(serviceType) is IDataService service)
					return Model.GetDescriptor(service, modelType);
			}

			return null;
		}

		public static PropertyInfo[] GetCriteria(ControllerModel controller)
		{
			if(Zongsoft.Common.TypeExtension.IsAssignableFrom(typeof(ServiceControllerBase<,>), controller.ControllerType, out var genericTypes) && genericTypes.Count > 0)
			{
				var attribute = genericTypes[0].GenericTypeArguments[1].GetCustomAttribute<DataServiceAttribute>(true);
				return (attribute != null && attribute.Criteria != null) ? attribute.Criteria.GetProperties(BindingFlags.Public | BindingFlags.Instance) : [];
			}

			return [];
		}
	}
}
