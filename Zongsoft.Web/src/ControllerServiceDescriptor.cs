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
using System.Reflection;
using System.Collections.Generic;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

using Zongsoft.Data;
using Zongsoft.Common;
using Zongsoft.Services;

namespace Zongsoft.Web;

public class ControllerServiceDescriptor : IEquatable<ControllerServiceDescriptor>
{
	#region 私有构造
	private ControllerServiceDescriptor(ControllerModel controller, string module, string @namespace, string serviceName, Type serviceType, ModelDescriptor model)
	{
		if(string.IsNullOrEmpty(serviceName))
			throw new ArgumentNullException(nameof(serviceName));

		this.Controller = controller ?? throw new ArgumentNullException(nameof(controller));
		this.Module = module;
		this.Namespace = @namespace;
		this.Name = serviceName;
		this.Type = serviceType;
		this.Model = model;

		if(string.IsNullOrEmpty(module))
			this.QualifiedName = string.IsNullOrEmpty(@namespace) ? serviceName : $"{@namespace}{Type.Delimiter}{serviceName}";
		else
			this.QualifiedName = string.IsNullOrEmpty(@namespace) ? $"{module}:{serviceName}" : $"{module}:{@namespace}{Type.Delimiter}{serviceName}";
	}
	#endregion

	#region 公共属性
	/// <summary>获取服务名称。</summary>
	public string Name { get; }
	/// <summary>获取服务类型。</summary>
	public Type Type { get; }
	/// <summary>获取模块名称。</summary>
	public string Module { get; }
	/// <summary>获取服务命名空间。</summary>
	public string Namespace { get; }
	/// <summary>获取服务限定名称。</summary>
	public string QualifiedName { get; }
	/// <summary>获取服务模型描述。</summary>
	public ModelDescriptor Model { get; }
	/// <summary>获取控制器描述器。</summary>
	public ControllerModel Controller { get; }
	#endregion

	#region 重写方法
	public bool Equals(ControllerServiceDescriptor other) => other is not null && string.Equals(this.QualifiedName, other.QualifiedName, StringComparison.OrdinalIgnoreCase);
	public override bool Equals(object obj) => this.Equals(obj as ControllerServiceDescriptor);
	public override int GetHashCode() => this.QualifiedName.GetHashCode();
	public override string ToString() => this.QualifiedName;
	#endregion

	#region 静态方法
	public static ControllerServiceDescriptor Get(ControllerModel controller)
	{
		if(controller == null)
			throw new ArgumentNullException(nameof(controller));

		var @namespace = GetNamespace(controller.ControllerType);
		(var modelType, var serviceType) = GetServiceInfo(controller.ControllerType);

		if(serviceType != null)
		{
			var model = Zongsoft.Data.Model.GetDescriptor(modelType);
			return new ControllerServiceDescriptor(controller, ApplicationModuleAttribute.Find(serviceType)?.Name, @namespace, GetServiceName(serviceType), serviceType, model);
		}

		return new ControllerServiceDescriptor(controller, ApplicationModuleAttribute.Find(controller.ControllerType)?.Name, @namespace, GetControllerName(controller.ControllerType), null, null);
	}
	#endregion

	#region 私有方法
	private static string GetServiceName(Type serviceType)
	{
		const string SERVICE_SUFFIX = "Service";
		const string SERVICE_BASE_SUFFIX = "ServiceBase";

		if(serviceType.Name.Length > SERVICE_SUFFIX.Length && serviceType.Name.EndsWith(SERVICE_SUFFIX))
			return serviceType.Name[..^SERVICE_SUFFIX.Length];

		if(serviceType.Name.Length > SERVICE_BASE_SUFFIX.Length && serviceType.Name.EndsWith(SERVICE_BASE_SUFFIX))
			return serviceType.Name[..^SERVICE_BASE_SUFFIX.Length];

		return serviceType.Name;
	}

	private static string GetControllerName(Type controllerType)
	{
		const string CONTROLLER_SUFFIX = "Controller";
		const string CONTROLLER_BASE_SUFFIX = "ControllerBase";

		if(controllerType.Name.Length > CONTROLLER_SUFFIX.Length && controllerType.Name.EndsWith(CONTROLLER_SUFFIX, StringComparison.OrdinalIgnoreCase))
			return controllerType.Name[..^CONTROLLER_SUFFIX.Length];

		if(controllerType.Name.Length > CONTROLLER_BASE_SUFFIX.Length && controllerType.Name.EndsWith(CONTROLLER_BASE_SUFFIX, StringComparison.OrdinalIgnoreCase))
			return controllerType.Name[..^CONTROLLER_BASE_SUFFIX.Length];

		return controllerType.Name;
	}

	private static string GetTopmost(Type controllerType)
	{
		return string.IsNullOrEmpty(ApplicationModuleAttribute.Find(controllerType)?.Name) ? GetControllerArea(controllerType) : null;

		static string GetControllerArea(Type controllerType)
		{
			var area = controllerType?.GetCustomAttribute<AreaAttribute>(true)?.RouteValue;
			return string.IsNullOrEmpty(area) ? null : area.Replace('/', Type.Delimiter);
		}
	}

	private static string GetNamespace(Type controllerType)
	{
		if(controllerType == null)
			return null;

		if(!controllerType.IsNested)
			return GetTopmost(controllerType);

		var stack = new Stack<string>();
		var type = controllerType.DeclaringType;

		while(type != null)
		{
			(_, var serviceType) = GetServiceInfo(type);

			if(serviceType != null)
				stack.Push(GetServiceName(serviceType));
			else
			{
				stack.Push(GetControllerName(type));

				if(type.DeclaringType == null)
				{
					var area = GetTopmost(type);

					if(!string.IsNullOrEmpty(area))
						stack.Push(area);
				}
			}

			type = type.DeclaringType;
		}

		return string.Join(Type.Delimiter, stack);
	}

	private static (Type modelType, Type serviceType) GetServiceInfo(Type controllerType)
	{
		if(controllerType != null && TypeExtension.IsAssignableFrom(typeof(ServiceControllerBase<,>), controllerType, out var genericTypes) && genericTypes.Count > 0)
			return (genericTypes[0].GenericTypeArguments[0], genericTypes[0].GenericTypeArguments[1]);
		else
			return default;
	}
	#endregion
}
