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
using System.Collections;
using System.Collections.Generic;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

using Zongsoft.Data;
using Zongsoft.Common;
using Zongsoft.Services;
using Zongsoft.Components;

namespace Zongsoft.Web;

public class ControllerServiceDescriptor : ServiceDescriptor<ControllerServiceDescriptor.ControllerOperationDescriptorCollection>, IEquatable<ControllerServiceDescriptor>
{
	#region 私有构造
	private ControllerServiceDescriptor(string module, string @namespace, string serviceName, Type serviceType) : base(serviceType, serviceName, GetQualifiedName(module, @namespace, serviceName))
	{
		if(string.IsNullOrEmpty(serviceName))
			throw new ArgumentNullException(nameof(serviceName));

		this.Module = module;
		this.Namespace = @namespace;
		this.Controllers = new List<ControllerDescriptor>();
		this.Operations = new ControllerOperationDescriptorCollection(this);
	}
	#endregion

	#region 公共属性
	/// <summary>获取模块名称。</summary>
	public string Module { get; }
	/// <summary>获取服务命名空间。</summary>
	public string Namespace { get; }
	/// <summary>获取服务模型描述。</summary>
	public ModelDescriptor Model { get; }
	/// <summary>获取控制器描述器集合。</summary>
	public ICollection<ControllerDescriptor> Controllers { get; }
	#endregion

	#region 重写方法
	public bool Equals(ControllerServiceDescriptor other) => other is not null && string.Equals(this.QualifiedName, other.QualifiedName, StringComparison.OrdinalIgnoreCase);
	#endregion

	#region 静态方法
	private static string GetQualifiedName(string module, string @namespace, string serviceName)
	{
		if(string.IsNullOrEmpty(module))
			return string.IsNullOrEmpty(@namespace) ? serviceName : $"{@namespace}{Type.Delimiter}{serviceName}";
		else
			return string.IsNullOrEmpty(@namespace) ? $"{module}:{serviceName}" : $"{module}:{@namespace}{Type.Delimiter}{serviceName}";
	}

	internal static string GetQualifiedName(ControllerModel controller)
	{
		var @namespace = GetNamespace(controller.ControllerType);
		(var _, var serviceType) = GetServiceInfo(controller.ControllerType);

		return serviceType != null ?
			GetQualifiedName(ApplicationModuleAttribute.Find(serviceType)?.Name, @namespace, GetServiceName(serviceType)) :
			GetQualifiedName(ApplicationModuleAttribute.Find(controller.ControllerType)?.Name, @namespace, GetControllerName(controller.ControllerType));
	}

	public static ControllerServiceDescriptor Create(ControllerModel controller)
	{
		if(controller == null)
			throw new ArgumentNullException(nameof(controller));

		var @namespace = GetNamespace(controller.ControllerType);
		(_, var serviceType) = GetServiceInfo(controller.ControllerType);

		var result = serviceType != null ?
			new ControllerServiceDescriptor(ApplicationModuleAttribute.Find(serviceType)?.Name, @namespace, GetServiceName(serviceType), serviceType) :
			new ControllerServiceDescriptor(ApplicationModuleAttribute.Find(controller.ControllerType)?.Name, @namespace, GetControllerName(controller.ControllerType), null);

		result.Controllers.Add(new ControllerDescriptor(controller));

		return result;
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

	#region 嵌套子类
	public sealed class ControllerDescriptor : ControllerModel
	{
		public ControllerDescriptor(ControllerModel controller) : base(controller)
		{
			(var modelType, var serviceType) = GetServiceInfo(controller.ControllerType);
			this.Model = modelType == null ? null : Zongsoft.Data.Model.GetDescriptor(modelType);
			this.ServiceType = serviceType;
		}

		/// <summary>获取服务模型描述。</summary>
		public ModelDescriptor Model { get; }

		/// <summary>获取服务类型。</summary>
		public Type ServiceType { get; }
	}

	public sealed class ControllerOperationDescriptor : Operation
	{
		public ControllerOperationDescriptor(ControllerServiceDescriptor service, ActionModel action) : base(service, action.ActionMethod)
		{
			this.Action = action;
			this.Name = action.ActionName;
		}

		public ActionModel Action { get; }
	}

	public sealed class ControllerOperationDescriptorCollection(ControllerServiceDescriptor service) : IReadOnlyCollection<ControllerOperationDescriptor>
	{
		private readonly ControllerServiceDescriptor _service = service ?? throw new ArgumentNullException(nameof(service));

		public int Count => _service.Controllers.Sum(controller => controller.Actions.Count);

		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		public IEnumerator<ControllerOperationDescriptor> GetEnumerator()
		{
			foreach(var controller in _service.Controllers)
			{
				foreach(var action in controller.Actions)
					yield return new ControllerOperationDescriptor(_service, action);
			}
		}
	}
	#endregion
}
