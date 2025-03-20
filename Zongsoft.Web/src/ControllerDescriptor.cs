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
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

using Zongsoft.Data;
using Zongsoft.Services;
using Zongsoft.Components;

namespace Zongsoft.Web;

public partial class ControllerDescriptor(ControllerModel controller) : ICommonModel, IFilterModel, IApiExplorerModel
{
	#region 成员字段
	private bool _initialized;
	private string _qualifiedName;
	private ModelDescriptor _model;
	private ServiceDescriptor _service;
	private readonly ControllerModel _controller = controller;
	#endregion

	#region 公共属性
	public ModelDescriptor Model
	{
		get
		{
			this.Initialize();
			return _model;
		}
	}

	public ServiceDescriptor Service
	{
		get
		{
			this.Initialize();
			return _service;
		}
	}

	string ICommonModel.Name => _controller.ControllerName;
	public string DisplayName => _controller.DisplayName;
	public string QualifiedName => _qualifiedName ??= this.GetQualifiedName();
	public string ControllerName => _controller.ControllerName;
	public Type ControllerType => _controller.ControllerType;
	public IList<PropertyModel> ControllerProperties => _controller.ControllerProperties;
	public ApplicationModel Application => _controller.Application;
	public IList<ActionModel> Actions => _controller.Actions;
	public IReadOnlyList<object> Attributes => _controller.Attributes;
	public IDictionary<object, object> Properties => _controller.Properties;
	public IList<IFilterMetadata> Filters => _controller.Filters;
	public IDictionary<string, string> RouteValues => _controller.RouteValues;
	public IList<SelectorModel> Selectors => _controller.Selectors;
	MemberInfo ICommonModel.MemberInfo => _controller.ControllerType;
	public ApiExplorerModel ApiExplorer { get => _controller.ApiExplorer; set => _controller.ApiExplorer = value; }
	#endregion

	#region 私有方法
	private void Initialize()
	{
		if(_initialized)
			return;

		lock(_controller)
		{
			if(_initialized)
				return;

			(_service, _model) = Resolve(_controller);
			_initialized = true;
		}
	}

	private static (ServiceDescriptor, ModelDescriptor) Resolve(ControllerModel controller)
	{
		if(Common.TypeExtension.IsAssignableFrom(typeof(ServiceControllerBase<,>), controller.ControllerType, out var genericTypes) && genericTypes.Count > 0)
		{
			var modelType = genericTypes[0].GenericTypeArguments[0];
			var serviceType = genericTypes[0].GenericTypeArguments[1];

			return (
				ServiceDescriptor.Get(serviceType),
				Zongsoft.Data.Model.GetDescriptor(modelType)
			);
		}

		return default;
	}

	private string GetQualifiedName()
	{
		var qualifiedName = this.Service?.QualifiedName;

		if(string.IsNullOrEmpty(qualifiedName))
		{
			var attribute = ApplicationModuleAttribute.Find(this.ControllerType);

			if(attribute == null || string.IsNullOrEmpty(attribute.Name))
				qualifiedName = GetFullName();
			else
				qualifiedName = $"{attribute.Name}:{GetFullName()}";
		}

		return qualifiedName;

		string GetFullName()
		{
			var path = GetAncestorPath(this.ControllerType);
			return string.IsNullOrEmpty(path) ? this.ControllerName : $"{path}.{this.ControllerName}";
		}
	}

	private static string GetAncestorPath(Type controllerType)
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

		return string.Join(Type.Delimiter, stack.Select(type => type.Name));
	}
	#endregion
}
