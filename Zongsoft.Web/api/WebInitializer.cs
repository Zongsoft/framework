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
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Scalar.AspNetCore;

using Zongsoft.Common;
using Zongsoft.Services;

namespace Zongsoft.Web.OpenApi;

[Service<IApplicationInitializer<IApplicationBuilder>>]
public class WebInitializer : IApplicationInitializer<IApplicationBuilder>
{
	#region 初始方法
	public void Initialize(IApplicationBuilder builder)
	{
		if(builder is IEndpointRouteBuilder app)
		{
			app.UseOpenApi();
			app.MapScalarApiReference(options =>
			{
				options.WithDefaultHttpClient(ScalarTarget.Http, ScalarClient.Http);
				options.Authentication = new() { PreferredSecuritySchemes = ["Credential"] };
			});
		}
	}
	#endregion

	#region 服务注册
	public sealed class Registration : IServiceRegistration
	{
		public void Register(IServiceCollection services, IConfiguration configuration)
		{
			//services.Configure<JsonOptions>(options =>
			//{
			//	options.JsonSerializerOptions.TypeInfoResolverChain.Insert(0, new JsonTypeResolver());
			//	options.JsonSerializerOptions.MaxDepth = 10;
			//	options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
			//});

			//services.AddOpenApi(options =>
			//{
			//	options.ShouldInclude = OnShouldInclude;
			//	options.CreateSchemaReferenceId = CreateReference;
			//	options.AddSchemaTransformer(new Transformers.SchemaTransformer());
			//	options.AddDocumentTransformer(new Transformers.DocumentTransformer());
			//	options.AddOperationTransformer(new Transformers.OperationTransformer());
			//});
		}

		private static string CreateReference(JsonTypeInfo info)
		{
			if(!info.Options.IsReadOnly)
			{
				info.Options.MaxDepth = 32;
				info.Options.ReferenceHandler = ReferenceHandler.IgnoreCycles;
			}

			return info.Kind == JsonTypeInfoKind.Enumerable ? info.ElementType.GetAlias() : info.Type.GetAlias();
		}

		private static bool OnShouldInclude(ApiDescription api)
		{
			if(api.ActionDescriptor is Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor action)
			{
				if(action.MethodInfo.ReturnType.IsGenericType)
				{
					if(action.MethodInfo.ReturnType.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>) || action.MethodInfo.ReturnType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
					{
						if(action.MethodInfo.ReturnType.GenericTypeArguments[0].IsAbstract)
							return false;
					}
					else if(action.MethodInfo.ReturnType.GetGenericTypeDefinition() == typeof(IDictionary<,>))
					{
						if(action.MethodInfo.ReturnType.GenericTypeArguments[1].IsAbstract)
							return false;
					}
				}
			}

			if(api.ActionDescriptor != null && api.ActionDescriptor.Parameters != null)
			{
				foreach(var parameter in api.ActionDescriptor.Parameters)
				{
					if(parameter.ParameterType.IsAbstract)
						return false;
				}
			}

			return true;
		}

		public class JsonTypeResolver : IJsonTypeInfoResolver
		{
			public JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
			{
				options.MaxDepth = 10;
				options.ReferenceHandler = ReferenceHandler.IgnoreCycles;
				return JsonTypeInfo.CreateJsonTypeInfo(type, options);
			}
		}
	}
	#endregion
}
