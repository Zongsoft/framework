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
using System.Net.Http;
using System.Threading;
using System.Collections.Generic;

using Microsoft.OpenApi;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

using Zongsoft.Services;
using Zongsoft.Web.Routing;

namespace Zongsoft.Web.OpenApi;

partial class DocumentGenerator
{
	private static List<ControllerServiceDescriptor> GeneratePaths(this DocumentContext context, ControllerServiceDescriptorCollection descriptors)
	{
		var paths = context.Document.Paths;
		var services = new List<ControllerServiceDescriptor>(descriptors.Count);

		foreach(var descriptor in descriptors)
		{
			if(SetPaths(context, descriptor, paths))
				services.Add(descriptor);
		}

		foreach(var path in paths)
		{
			if((path.Value.Operations == null || path.Value.Operations.Count == 0) &&
			   (path.Value.Extensions == null || path.Value.Extensions.Count == 0) &&
			   (path.Value.Parameters == null || path.Value.Parameters.Count == 0))
				paths.Remove(path.Key);
		}

		EnsureUniqueOperationIds(paths);
		return services;
	}

	private static void EnsureUniqueOperationIds(OpenApiPaths paths)
	{
		var operations = new List<(string Path, string Method, OpenApiOperation Operation)>();

		foreach(var path in paths)
		{
			foreach(var operation in path.Value.Operations)
				operations.Add((path.Key, operation.Key.Method, operation.Value));
		}

		var groups = operations.ToLookup(item => item.Operation.OperationId, StringComparer.Ordinal);
		var used = groups.Select(group => group.Key).ToHashSet(StringComparer.Ordinal);
		var duplicates = groups.Where(group => group.Count() > 1).OrderBy(group => group.Key, StringComparer.Ordinal);

		foreach(var group in duplicates)
		{
			var ordered = group.OrderBy(item => item.Path, StringComparer.Ordinal).ThenBy(item => item.Method, StringComparer.Ordinal);

			//按路径和 HTTP 方法排序，首项保留原始标识
			foreach(var item in ordered.Skip(1))
			{
				var prefix = $"{group.Key}.{item.Method}.{Uri.EscapeDataString(item.Path)}";
				var id = prefix;

				for(var index = 2; !used.Add(id); index++)
					id = $"{prefix}.{index}";

				item.Operation.OperationId = id;
			}
		}
	}

	private static bool SetPaths(DocumentContext context, ControllerServiceDescriptor descriptor, IDictionary<string, IOpenApiPathItem> paths)
	{
		var included = false;

		foreach(var controller in descriptor.Controllers)
		{
			foreach(var pattern in controller.GetRoutePatterns())
			{
				var pathKey = GetPathKey(pattern);

				if(!paths.ContainsKey(pathKey))
				{
					paths.TryAdd(pathKey, new OpenApiPathItem()
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
				var patterns = operation.GetRoutePatterns().ToList();

				//约定式路由的接口没有属性路由模板，需从运行时路由表反查其真实路径
				if(patterns.Count == 0)
					patterns = GetConventionalPatterns(context, operation, method);

				foreach(var pattern in patterns)
				{
					var pathKey = GetPathKey(pattern);

					if(!paths.TryGetValue(pathKey, out var path))
						paths.Add(pathKey, path = new OpenApiPathItem()
						{
							Summary = operation.Title,
							Description = operation.Description,
							Operations = new Dictionary<HttpMethod, OpenApiOperation>(),
						});

					included |= path.Operations.TryAdd(method, GetOperation(context, descriptor, operation, method, pattern));
				}
			}
		}

		return included;

		static string GetPathKey(RoutePattern pattern)
		{
			var path = pattern.GetUrl();

			if(path.StartsWith("~/", StringComparison.Ordinal))
				return path[1..];

			return path.StartsWith('/') ? path : "/" + path;
		}
	}

	private static OpenApiOperation GetOperation(DocumentContext context, ControllerServiceDescriptor service, ControllerServiceDescriptor.ControllerOperationDescriptor descriptor, HttpMethod method, RoutePattern pattern)
	{
		var operation = new OpenApiOperation()
		{
			OperationId = $"{service.QualifiedName}.{descriptor.Name}",
			Summary = GetSummary(descriptor),
			Description = descriptor.Description,
			Responses = new OpenApiResponses()
			{
				["default"] = new OpenApiResponse() { Description = Properties.Resources.ResponseDefault_Description },
			},
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

						Content = new Dictionary<string, IOpenApiMediaType>()
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
			var parameters = descriptor.Action.Parameters
				.Where(parameter => parameter.BindingInfo?.BindingSource == BindingSource.Path)
				.Select(parameter => parameter.Name);
			var signature = string.Join(',', parameters);

			return string.IsNullOrEmpty(signature) ? descriptor.Name : $"{descriptor.Name}({signature})";
		}
	}

	private static OpenApiParameter GetParameter(DocumentContext context, ParameterModel model, RoutePattern pattern)
	{
		if(model.ParameterType == typeof(CancellationToken))
			return null;

		//获取参数位置
		var location = Utility.GetLocation(model);

		//约定式路由的路径参数没有显式绑定源，需根据路由模板识别
		if(location != ParameterLocation.Path && pattern.Contains(model.Name))
			location = ParameterLocation.Path;

		//无显式绑定源的标量参数按查询参数处理（约定式路由的接口参数通常没有 [From*] 特性）
		if(location == null && Utility.IsScalarType(model.ParameterType))
			location = ParameterLocation.Query;

		//如果是路径参数但路由模板中不包含该参数名称则忽略它
		if(location == ParameterLocation.Path && !pattern.Contains(model.Name))
			return null;

		var nullable = Common.TypeExtension.IsNullable(model.ParameterType);

		return new OpenApiParameter()
		{
			Name = model.Name,
			In = location,
			Description = model.DisplayName,
			Example = GetValue(model, pattern),
			Required = location == ParameterLocation.Path || IsRequired(model, pattern, nullable),
			Schema = GenerateSchema(context, model.ParameterType),
			#pragma warning disable CS0618 // Preserve OpenAPI 3.2 query parameters that accept an empty value.
			AllowEmptyValue = location == ParameterLocation.Query && nullable,
			#pragma warning restore CS0618
		};

		static string GetValue(ParameterModel parameter, RoutePattern pattern)
		{
			if(parameter.ParameterInfo.HasDefaultValue)
				return Common.Convert.ConvertValue<string>(parameter.ParameterInfo.DefaultValue);

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

	/// <summary>从运行时路由表中反查约定式路由接口的路径模板。</summary>
	/// <param name="context">包含运行时路由表的文档生成上下文。</param>
	/// <param name="operation">要查找路由的控制器操作。</param>
	/// <param name="method">用于筛选路由端点的 HTTP 方法。</param>
	/// <returns>返回匹配操作及 HTTP 方法、且已替换控制器路由值的路径模板列表。</returns>
	private static List<RoutePattern> GetConventionalPatterns(DocumentContext context, ControllerServiceDescriptor.ControllerOperationDescriptor operation, HttpMethod method)
	{
		if(context.Routing == null)
			return [];

		var result = new List<RoutePattern>();
		var controllerName = operation.Action.Controller.ControllerName;
		var actionName = operation.Action.ActionName;

		foreach(var endpoint in context.Routing.Endpoints)
		{
			if(endpoint is not RouteEndpoint routeEndpoint)
				continue;

			var descriptor = routeEndpoint.Metadata.GetMetadata<ControllerActionDescriptor>();
			if(descriptor == null)
				continue;

			if(!string.Equals(descriptor.ControllerName, controllerName, StringComparison.OrdinalIgnoreCase) ||
			   !string.Equals(descriptor.ActionName, actionName, StringComparison.OrdinalIgnoreCase))
				continue;

			var httpMethods = routeEndpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods;
			if(httpMethods != null && !httpMethods.Any(item => string.Equals(item, method.Method, StringComparison.OrdinalIgnoreCase)))
				continue;

			var rawText = routeEndpoint.RoutePattern.RawText;
			if(string.IsNullOrWhiteSpace(rawText))
				continue;

			var pattern = RoutePattern.Resolve(rawText);
			if(pattern == null)
				continue;

			//用控制器的实际路由值替换 {controller}/{action}/{area} 等占位符
			foreach(var parameter in descriptor.RouteValues)
				pattern[parameter.Key]?.Value = parameter.Value;

			//若模板含控制器或操作占位符但未能替换为具体值，则该模板无法生成有效的接口路径
			if(pattern.Contains("controller") && string.IsNullOrEmpty(pattern["controller"].Value))
				continue;
			if(pattern.Contains("action") && string.IsNullOrEmpty(pattern["action"].Value))
				continue;

			result.Add(pattern);
		}

		return result;
	}
}
