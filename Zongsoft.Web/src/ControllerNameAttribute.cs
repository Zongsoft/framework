/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
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

namespace Zongsoft.Web
{
	[AttributeUsage(AttributeTargets.Class, Inherited = true)]
	public class ControllerNameAttribute : Attribute, IControllerModelConvention
	{
		#region 构造函数
		public ControllerNameAttribute(bool isModular = true) : this(null, isModular) { }
		public ControllerNameAttribute(string name, bool isModular = true)
		{
			this.Name = name;
			this.IsModular = isModular;
		}
		#endregion

		#region 公共属性
		/// <summary>获取或设置控制器的名称。</summary>
		public string Name { get; set; }

		/// <summary>获取或设置一个值，指示是否为模块化的控制器。</summary>
		/// <remarks>如果为模块化控制器，将会查找该控制器所在程序集的 <see cref="Zongsoft.Services.ApplicationModuleAttribute.Name"/> 特性作为路由的前导。</remarks>
		public bool IsModular { get; set; }
		#endregion

		#region 公共方法
		public void Apply(ControllerModel controller)
		{
			if(controller.ControllerType.IsNested)
			{
				var ancestor = controller.RouteValues["ancestor"] = GetAncestorPath(controller.ControllerType, controller.ControllerName);
				var module = Zongsoft.Services.ServiceUtility.Modular.GetModuleName(controller.ControllerType);

				if(string.IsNullOrEmpty(module))
				{
					controller.RouteValues["area"] = ancestor;
				}
				else
				{
					controller.RouteValues["module"] = module;
					controller.RouteValues["area"] = $"{module}/{ancestor}";
				}
			}
			else if(this.IsModular)
			{
				var module = Zongsoft.Services.ServiceUtility.Modular.GetModuleName(controller.ControllerType);

				if(!string.IsNullOrEmpty(module))
				{
					controller.RouteValues["module"] = module;
					controller.RouteValues["area"] = module;
				}
			}

			controller.RouteValues["controller"] = string.IsNullOrEmpty(this.Name) ? controller.ControllerName : this.Name;
		}
		#endregion

		#region 私有方法
		private static string GetAncestorPath(Type controllerType, string controllerName)
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

			return string.Join('/', stack.Select(type => GetControllerName(type, controllerName)));
		}

		private static string GetControllerName(Type controllerType, string defaultName)
		{
			var attribute = controllerType.GetCustomAttribute<ControllerNameAttribute>(true);

			if(attribute != null && !string.IsNullOrEmpty(attribute.Name))
				return attribute.Name;
			else
				return defaultName;
		}
		#endregion
	}
}