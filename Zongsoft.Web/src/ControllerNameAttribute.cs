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
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Zongsoft.Web;

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
		controller.RouteValues.TryGetValue("area", out var area);

		if(controller.ControllerType.IsNested)
		{
			//命名空间表示嵌套类的祖先控制器名称以斜杆符组合而成
			var @namespace = controller.RouteValues["namespace"] = GetNamespace(controller.ControllerType, '/');
			var module = Zongsoft.Services.ApplicationModuleAttribute.Find(controller.ControllerType)?.Name;

			//嵌套类的区域名称为最顶层控制器的区域名称加上嵌套类自生定义的区域名
			area = Combine(GetTopmost(controller.ControllerType), area);

			if(string.IsNullOrEmpty(module))
			{
				controller.RouteValues["module"] = string.Empty;
				controller.RouteValues["area"] = Combine(area, @namespace);
			}
			else
			{
				controller.RouteValues["module"] = module;
				controller.RouteValues["area"] = Combine(area, $"{module}/{@namespace}");
			}
		}
		else if(this.IsModular)
		{
			var module = Zongsoft.Services.ApplicationModuleAttribute.Find(controller.ControllerType)?.Name;

			if(string.IsNullOrEmpty(module))
			{
				controller.RouteValues["module"] = string.Empty;

				if(!controller.RouteValues.ContainsKey("area"))
					controller.RouteValues["area"] = string.Empty;
			}
			else
			{
				controller.RouteValues["module"] = module;
				controller.RouteValues["area"] = Combine(area, module);
			}
		}

		if(!string.IsNullOrEmpty(this.Name))
			controller.ControllerName = controller.RouteValues["controller"] = this.Name;

		var hasRouteAttribute = controller.Selectors.Any(selector => selector.AttributeRouteModel != null);
		if(!hasRouteAttribute)
		{
			controller.Selectors[0].AttributeRouteModel = new AttributeRouteModel()
			{
				Template = $"[area]/[controller]",
			};
		}
	}
	#endregion

	#region 私有方法
	private static string GetTopmost(TypeInfo type)
	{
		if(type.IsNested)
			return GetTopmost(type.DeclaringType.GetTypeInfo());

		var attribute = type.GetCustomAttribute<AreaAttribute>(true);
		return attribute?.RouteValue;
	}

	private static string Combine(string part1, string part2)
	{
		if(string.IsNullOrEmpty(part1))
			return part2 ?? string.Empty;

		if(string.IsNullOrEmpty(part2))
			return part1 ?? string.Empty;

		var span1 = part1.AsSpan().Trim('/');
		var span2 = part2.AsSpan().Trim('/');

		return span2.StartsWith(span1, StringComparison.OrdinalIgnoreCase) ? span2.ToString() : $"{span1}/{span2}";
	}

	private static string GetNamespace(Type controllerType, char separator)
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

		static string GetName(Type controllerType)
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
	}
	#endregion
}