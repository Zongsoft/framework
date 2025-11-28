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
using Zongsoft.Web.Routing;

namespace Zongsoft.Web.OpenApi;

partial class DocumentGenerator
{
	internal static void GeneratePaths(this DocumentContext context, ControllerServiceDescriptorCollection descriptors)
	{
		foreach(var descriptor in descriptors)
		{
			if(!string.IsNullOrEmpty(descriptor.Module))
				context.Document.Tags.Add(Tags.Tag(descriptor.Module));

			if(!string.IsNullOrEmpty(descriptor.Namespace))
				context.Document.Tags.Add(Tags.Tag(descriptor.Namespace, descriptor.Module));

			context.Document.Tags.Add(Tags
				.Tag(descriptor.QualifiedName, descriptor.Module, descriptor.Namespace)
				.Caption(descriptor.Title)
				.Description(descriptor.Description));

			SetPaths(context, descriptor, context.Document.Paths);
		}
	}

	private static void SetPaths(DocumentContext context, ControllerServiceDescriptor descriptor, IDictionary<string, IOpenApiPathItem> paths)
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

					path.Operations.TryAdd(method, GetOperation(context, descriptor, operation, method, pattern));
				}
			}
		}

		foreach(var path in paths)
		{
			if(IsEmpty(path.Value))
				paths.Remove(path.Key);
		}

		static bool IsEmpty(IOpenApiPathItem path)
		{
			return (path.Operations == null || path.Operations.Count == 0) &&
			       (path.Extensions == null || path.Extensions.Count == 0) &&
			       (path.Parameters == null || path.Parameters.Count == 0);
		}
	}

	private static OpenApiOperation GetOperation(DocumentContext context, ControllerServiceDescriptor service, ControllerServiceDescriptor.ControllerOperationDescriptor descriptor, HttpMethod method, RoutePattern pattern)
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
			operation.Parameters ??= new List<IOpenApiParameter>(descriptor.Action.Parameters.Count);

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
								Schema = GenerateSchema(context, parameterModel.ParameterType),
							}
						}
					};
				}
				else
				{
					var parameter = GetParameter(context, parameterModel, pattern);

					if(parameter != null)
						operation.Parameters.Add(parameter);
				}
			}
		}

		foreach(var provider in context.Services.ResolveAll<Services.IHeaderProvider>())
		{
			foreach(var header in provider.GetHeaders(context, descriptor, method))
			{
				operation.Parameters ??= [];
				operation.Parameters.Add(new OpenApiParameter()
				{
					Name = header.Key,
					In = ParameterLocation.Header,
					Schema = new OpenApiSchema() { Type = JsonSchemaType.String },
					Example = System.Text.Json.Nodes.JsonValue.Create(header.Value),
				});
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

	private static OpenApiParameter GetParameter(DocumentContext context, ParameterModel model, RoutePattern pattern)
	{
		if(model.ParameterType == typeof(CancellationToken))
			return null;

		//获取参数位置
		var location = Utility.GetLocation(model);

		//如果是路径参数但路由模板中不包含该参数名称则忽略它
		if(location == ParameterLocation.Path && !pattern.Contains(model.Name))
			return null;

		var nullable = Common.TypeExtension.IsNullable(model.ParameterType);

		return new OpenApiParameter()
		{
			Name = model.Name,
			In = location,
			AllowEmptyValue = nullable,
			Description = model.DisplayName,
			Example = GetValue(model, pattern),
			Required = IsRequired(model, pattern, nullable),
			Schema = GenerateSchema(context, model.ParameterType),
		};

		static string GetValue(ParameterModel parameter, RoutePattern pattern)
		{
			var entry = pattern[parameter.Name];
			return entry == null ? null : (string.IsNullOrEmpty(entry.Value) ? entry.Default : entry.Value);
		}

		static bool IsRequired(ParameterModel parameter, RoutePattern pattern, bool nullable)
		{
			if(parameter.ParameterInfo.HasDefaultValue)
				return false;

			var entry = pattern[parameter.Name];
			if(entry != null && entry.Constraints.Contains("required"))
				return true;

			if(parameter.ParameterType.IsValueType && !nullable)
				return true;

			return false;
		}
	}
}
