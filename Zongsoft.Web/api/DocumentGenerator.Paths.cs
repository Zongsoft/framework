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
			GeneratePath(document, descriptor);
	}

	private static void GeneratePath(OpenApiDocument document, ControllerServiceDescriptor descriptor)
	{
		var path = new OpenApiPathItem()
		{
			Description = descriptor.Description,
			Summary = string.IsNullOrEmpty(descriptor.Title) ? descriptor.Name : descriptor.Title,
		};

		document.Paths.Add(descriptor.QualifiedName, path);

		foreach(var operation in descriptor.Operations)
		{
			var methods = GetHttpMethods(operation);

			foreach(var method in methods)
				path.AddOperation(method, GetOperation(operation));
		}
	}

	private static OpenApiOperation GetOperation(ControllerServiceDescriptor.ControllerOperationDescriptor descriptor)
	{
		var operation = new OpenApiOperation()
		{
			Description = descriptor.Description,
			Summary = string.IsNullOrEmpty(descriptor.Title) ? descriptor.Name : descriptor.Title,
		};

		if(descriptor.Action.Parameters.Count > 0)
			operation.Parameters = new List<IOpenApiParameter>(descriptor.Action.Parameters.Count);

		foreach(var parameter in descriptor.Action.Parameters)
			operation.Parameters.Add(GetParameter(parameter));

		return operation;
	}

	private static IOpenApiParameter GetParameter(ParameterModel model)
	{
		return new OpenApiParameter()
		{
			Name = model.Name,
			Description = model.DisplayName,
			In = GetLocation(model.BindingInfo),
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

	private static IReadOnlyCollection<HttpMethod> GetHttpMethods(ControllerServiceDescriptor.ControllerOperationDescriptor descriptor)
	{
		var result = new HashSet<HttpMethod>();

		for(int i = 0; i < descriptor.Action.Selectors.Count; i++)
		{
			foreach(var method in Find(descriptor.Action.Selectors[i].EndpointMetadata))
				result.Add(method);
		}

		foreach(var method in Find(descriptor.Action.Attributes))
			result.Add(method);

		return result;

		static IEnumerable<HttpMethod> Find(IEnumerable<object> metadatas)
		{
			foreach(var metadata in metadatas)
			{
				if(metadata is HttpMethod method)
					yield return method;

				if(metadata is HttpMethodAttribute attribute)
					foreach(var text in attribute.HttpMethods)
						yield return HttpMethod.Parse(text);

				if(metadata is Microsoft.AspNetCore.Routing.HttpMethodMetadata methodMetadata)
					foreach(var text in methodMetadata.HttpMethods)
						yield return HttpMethod.Parse(text);
			}
		}
	}
}
