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

namespace Zongsoft.Web;

public partial class ControllerDescriptor(ControllerModel controller) : ICommonModel, IFilterModel, IApiExplorerModel
{
	#region 成员字段
	private ServiceControllerDescriptor _service;
	private readonly ControllerModel _controller = controller;
	#endregion

	#region 公共属性
	public ServiceControllerDescriptor Service => _service ??= ServiceControllerDescriptor.Get(_controller.ControllerType);
	string ICommonModel.Name => _controller.ControllerName;
	public string DisplayName => _controller.DisplayName;
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

	#region 隐式转换
	public static implicit operator ControllerModel(ControllerDescriptor descriptor) => descriptor._controller;
	public static implicit operator ControllerDescriptor(ControllerModel controller) => new(controller);
	#endregion
}
