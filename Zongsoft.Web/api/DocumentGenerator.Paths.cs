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
 * This file is part of Zongsoft.Web.OpenApi library.
 *
 * The Zongsoft.Web.OpenApi is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Web.OpenApi is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Web.OpenApi library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Net.Http;
using System.Threading;
using System.Collections.Generic;

using Microsoft.OpenApi;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

using Zongsoft.Services;
using Zongsoft.Components;

namespace Zongsoft.Web.OpenApi;

partial class DocumentGenerator
{
	internal static void GeneratePaths(this OpenApiDocument document, ControllerServiceDescriptorCollection descriptors)
	{
		foreach(var descriptor in descriptors)
		{
			if(!string.IsNullOrEmpty(descriptor.Module))
				document.Tags.Add(new() { Name = descriptor.Module });

			if(!string.IsNullOrEmpty(descriptor.Namespace))
				document.Tags.Add(Extensions.Tag(descriptor.Namespace, descriptor.Module));

			document.Tags.Add(Extensions.Tag(descriptor.QualifiedName, descriptor.Module, descriptor.Namespace));

			foreach(var controller in descriptor.Controllers)
				document.Paths.Add(controller.GetUrl(), GetPath(descriptor, controller));
		}
	}

	private static OpenApiPathItem GetPath(ControllerServiceDescriptor descriptor, ControllerServiceDescriptor.ControllerDescriptor controller)
	{
		var path = new OpenApiPathItem()
		{
			Summary = descriptor.Title,
			Description = descriptor.Description,
		};

		foreach(var operation in descriptor.Operations)
		{
			var methods = Utility.GetHttpMethods(operation);

			foreach(var method in methods)
				path.AddOperation(method, GetOperation(descriptor, controller, operation));
		}

		return path;
	}

	private static OpenApiOperation GetOperation(ControllerServiceDescriptor service, ControllerServiceDescriptor.ControllerDescriptor controller, ControllerServiceDescriptor.ControllerOperationDescriptor descriptor)
	{
		var operation = new OpenApiOperation()
		{
			OperationId = $"{service.QualifiedName}.{descriptor.Name}",
			Summary = descriptor.Title,
			Description = descriptor.Description,
		};

		//if(!string.IsNullOrEmpty(service.Module))
		//{
		//	if(operation.Tags == null)
		//		operation.Tags = new HashSet<OpenApiTagReference>() { new(service.Module) };
		//	else
		//		operation.Tags.Add(new(service.Module));
		//}

		//if(!string.IsNullOrEmpty(service.Namespace))
		//{
		//	if(operation.Tags == null)
		//		operation.Tags = new HashSet<OpenApiTagReference>() { new(service.Namespace) };
		//	else
		//		operation.Tags.Add(new OpenApiTagReference(service.Namespace));
		//}

		if(operation.Tags == null)
			operation.Tags = new HashSet<OpenApiTagReference>() { new(service.QualifiedName) };
		else
			operation.Tags.Add(new(service.QualifiedName));

		if(descriptor.Action.Parameters.Count > 0)
		{
			operation.Parameters = new List<IOpenApiParameter>(descriptor.Action.Parameters.Count);

			foreach(var parameterModel in descriptor.Action.Parameters)
			{
				var parameter = GetParameter(parameterModel);

				if(parameter != null)
					operation.Parameters.Add(parameter);
			}
		}

		return operation;
	}

	private static OpenApiParameter GetParameter(ParameterModel model)
	{
		if(model.ParameterType == typeof(CancellationToken))
			return null;

		var required = !model.ParameterInfo.HasDefaultValue;
		var nullable = Zongsoft.Common.TypeExtension.IsNullable(model.ParameterType);

		return new OpenApiParameter()
		{
			Name = model.Name,
			Required = required,
			AllowEmptyValue = nullable,
			Description = model.DisplayName,
			In = GetLocation(model.BindingInfo),
			Schema = Utility.GetSchema(model.ParameterType),
		};

		static ParameterLocation? GetLocation(BindingInfo info)
		{
			if(info == null || info.BindingSource == null)
				return null;

			if(info.BindingSource == BindingSource.Path)
				return ParameterLocation.Path;
			if(info.BindingSource == BindingSource.Query)
				return ParameterLocation.Query;
			if(info.BindingSource == BindingSource.Header)
				return ParameterLocation.Header;

			return null;
		}
	}
}
