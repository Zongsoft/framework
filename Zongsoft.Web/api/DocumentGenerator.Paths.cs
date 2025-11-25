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
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Collections.Generic;

using Microsoft.OpenApi;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

using Zongsoft.Services;
using Zongsoft.Components;
using Zongsoft.Web.Routing;

namespace Zongsoft.Web.OpenApi;

partial class DocumentGenerator
{
	internal static void GeneratePaths(this OpenApiDocument document, ControllerServiceDescriptorCollection descriptors)
	{
		foreach(var descriptor in descriptors)
		{
			if(!string.IsNullOrEmpty(descriptor.Module))
				document.Tags.Add(Extensions.Tag(descriptor.Module));

			if(!string.IsNullOrEmpty(descriptor.Namespace))
				document.Tags.Add(Extensions.Tag(descriptor.Namespace, descriptor.Module));

			document.Tags.Add(Extensions
				.Tag(descriptor.QualifiedName, descriptor.Module, descriptor.Namespace)
				.Caption(descriptor.Title)
				.Description(descriptor.Description));

			SetPaths(document, descriptor, document.Paths);
		}
	}

	private static void SetPaths(OpenApiDocument document, ControllerServiceDescriptor descriptor, IDictionary<string, IOpenApiPathItem> paths)
	{
		foreach(var controller in descriptor.Controllers)
		{
			foreach(var pattern in controller.GetRoutePatterns())
			{
				var url = pattern.GetUrl();

				if(!paths.ContainsKey(url))
				{
					paths.TryAdd(url, new OpenApiPathItem()
					{
						Summary = descriptor.Title,
						Description = descriptor.Description,
						Operations = new Dictionary<HttpMethod, OpenApiOperation>(),
					});
				}
			}
		}

		foreach(var operation in descriptor.Operations)
		{
			var methods = operation.Action.GetHttpMethods();

			if(methods == null || methods.Count == 0)
				continue;

			foreach(var method in methods)
			{
				foreach(var pattern in operation.GetRoutePatterns())
				{
					var url = pattern.GetUrl();

					if(!paths.TryGetValue(url, out var path))
						paths.Add(url, path = new OpenApiPathItem()
						{
							Summary = operation.Title,
							Description = operation.Description,
							Operations = new Dictionary<HttpMethod, OpenApiOperation>(),
						});

					path.Operations.TryAdd(method, GetOperation(document, descriptor, operation, method));
				}
			}
		}
	}

	private static OpenApiOperation GetOperation(OpenApiDocument document, ControllerServiceDescriptor service, ControllerServiceDescriptor.ControllerOperationDescriptor descriptor, HttpMethod method)
	{
		var operation = new OpenApiOperation()
		{
			OperationId = $"{service.QualifiedName}.{descriptor.Name}",
			Summary = GetSummary(descriptor),
			Description = descriptor.Description,
		};

		if(operation.Tags == null)
			operation.Tags = new HashSet<OpenApiTagReference>() { new(service.QualifiedName) };
		else
			operation.Tags.Add(new(service.QualifiedName));

		if(descriptor.Action.Parameters.Count > 0)
		{
			operation.Parameters = new List<IOpenApiParameter>(descriptor.Action.Parameters.Count);

			foreach(var parameterModel in descriptor.Action.Parameters)
			{
				if(parameterModel.IsBody())
				{
					operation.RequestBody = new OpenApiRequestBody()
					{
						Description = parameterModel.DisplayName,
						Required = !parameterModel.ParameterInfo.HasDefaultValue,

						Content = new Dictionary<string, OpenApiMediaType>()
						{
							["application/json"] = new OpenApiMediaType()
							{
								Schema = GenerateSchema(document, parameterModel.ParameterType),
							}
						}
					};
				}
				else
				{
					var parameter = GetParameter(document, parameterModel);

					if(parameter != null)
						operation.Parameters.Add(parameter);
				}
			}
		}

		return operation;

		static string GetSummary(ControllerServiceDescriptor.ControllerOperationDescriptor descriptor)
		{
			var signature = string.Join(',',
				descriptor.Action.Parameters
					.Where(parameter => parameter.BindingInfo?.BindingSource == BindingSource.Path)
					.Select(parameter => parameter.Name)
			);

			return string.IsNullOrEmpty(signature) ? descriptor.Name : $"{descriptor.Name}({signature})";
		}
	}

	private static OpenApiParameter GetParameter(OpenApiDocument document, ParameterModel model)
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
			In = Utility.GetLocation(model),
			Schema = GenerateSchema(document, model.ParameterType),
		};
	}
}
