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
 * Copyright (C) 2020-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Web.Grpc library.
 *
 * The Zongsoft.Web.Grpc is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Web.Grpc is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Web.Grpc library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Linq;
using System.Reflection;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;

using Grpc.Core;
using Grpc.Reflection;
using Grpc.Reflection.V1Alpha;
using Grpc.AspNetCore.Server;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Zongsoft.Services;
using Zongsoft.Components;
using Zongsoft.Web.Hosting;

using ProtocolDescriptor = Google.Protobuf.Reflection.ServiceDescriptor;

namespace Zongsoft.Web.Grpc;

[Service<IApplicationInitializer<IApplicationBuilder>>]
public class GrpcInitializer : IApplicationInitializer<IApplicationBuilder>
{
	#region 私有变量
	private static readonly MethodInfo _MapGrpcServiceMethod_ = typeof(GrpcInitializer).GetMethod(nameof(MapGrpcService), BindingFlags.NonPublic | BindingFlags.Static);
	#endregion

	#region 初始方法
	public void Initialize(IApplicationBuilder builder)
	{
		ArgumentNullException.ThrowIfNull(builder);
		if(builder is not IEndpointRouteBuilder app)
			return;

		var services = app.ServiceProvider.GetTags("gRPC").Distinct().ToArray();
		var endpoints = app.ServiceProvider.GetService<WebEndpointCollection>();
		var regions = (endpoints ?? Enumerable.Empty<WebEndpoint>())
			.Where(endpoint => endpoint.Protocols.Any(protocol => protocol.Name == "grpc"))
			.Select(endpoint => endpoint.Path).Distinct(StringComparer.OrdinalIgnoreCase)
			.ToDictionary(path => path, path => new Region(path), StringComparer.OrdinalIgnoreCase);

		if(services.Length == 0 && regions.Count == 0)
			return;
		if(regions.Count == 0)
			throw new InvalidOperationException(Properties.Resources.Grpc_EndpointsMissing_Message);
		if(app.ServiceProvider.GetRequiredService<IOptions<GrpcServiceOptions>>().Value.IgnoreUnknownServices != true)
			throw new InvalidOperationException(Properties.Resources.Grpc_FallbackConflict_Message);

		foreach(var service in services)
		{
			if(!service.IsClass || service.IsAbstract || service.ContainsGenericParameters)
				throw Invalid(Properties.Resources.Grpc_ServiceInvalid_Message, service);

			var descriptor = GetDescriptor(service);
			if(descriptor == null || descriptor.FullName == ServerReflection.Descriptor.FullName)
				throw Invalid(Properties.Resources.Grpc_ServiceInvalid_Message, service);

			var handlers = service.GetCustomAttributes<HandlerAttribute>(true).ToArray();
			if(handlers.Length == 0)
				throw Invalid(Properties.Resources.Grpc_HandlerMissing_Message, service);

			foreach(var handler in handlers)
			{
				if(handler.Order != 0)
					throw Invalid(Properties.Resources.Grpc_HandlerOrder_Message, service);

				//使用公共范围元数据校验字面路径，不另建一套路径规则
				var metadata = new WebEndpointMetadata(handler.Template, "grpc");
				if(!regions.TryGetValue(metadata.Path, out var region))
					throw Invalid(Properties.Resources.Grpc_HandlerUnmatched_Message, service, metadata.Path);

				if(region.Services.TryGetValue(descriptor.FullName, out var existing))
				{
					if(existing.Type != service)
						throw Invalid(Properties.Resources.Grpc_ServiceConflict_Message, metadata.Path, descriptor.FullName);
				}
				else
					region.Services.Add(descriptor.FullName, (service, descriptor));
			}
		}

		foreach(var region in regions.Values)
		{
			if(region.Services.Count == 0)
				throw Invalid(Properties.Resources.Grpc_RegionEmpty_Message, region.Metadata.Path);
		}

		foreach(var region in regions.Values)
			MapRegion(app, region);
	}
	#endregion

	#region 私有方法
	private static void MapRegion(IEndpointRouteBuilder app, Region region)
	{
		var group = app.MapGroup(region.Metadata.Path.TrimEnd('/')).WithMetadata(region.Metadata);

		foreach(var service in region.Services.Values)
		{
			try
			{
				_MapGrpcServiceMethod_.MakeGenericMethod(service.Type).Invoke(null, [group]);
			}
			catch(TargetInvocationException exception) when(exception.InnerException != null)
			{
				ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
			}
		}

		//核对真正生成的路由，禁止内置全局兜底或自定义提供程序产生歧义
		var methods = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		foreach(var endpoint in ((IEndpointRouteBuilder)group).DataSources.SelectMany(source => source.Endpoints))
		{
			var method = endpoint.Metadata.GetMetadata<GrpcMethodMetadata>();

			if(method == null)
				throw new InvalidOperationException(Properties.Resources.Grpc_FallbackConflict_Message);

			if(endpoint is not RouteEndpoint route || route.RoutePattern.RawText != method.Method.FullName || route.RoutePattern.Parameters.Count != 0 ||
			   !region.Services.TryGetValue(method.Method.ServiceName, out var service) || service.Type != method.ServiceType ||
			   !service.Descriptor.Methods.Any(candidate => candidate.Name == method.Method.Name) || !methods.Add(method.Method.FullName))
				throw Invalid(Properties.Resources.Grpc_ServiceConflict_Message, region.Metadata.Path, method.Method.FullName);
		}

		if(methods.Count != region.Services.Values.Sum(service => service.Descriptor.Methods.Count))
			throw Invalid(Properties.Resources.Grpc_MethodsInvalid_Message, region.Metadata.Path);

		group.WithMetadata(new ReflectionServiceImpl(region.Services.Values.Select(service => service.Descriptor).Append(ServerReflection.Descriptor)));
		MapGrpcService<ReflectionService>(group);

		group.MapFallback("{**path}", context =>
		{
			context.RequestAborted.ThrowIfCancellationRequested();

			context.Response.StatusCode = StatusCodes.Status200OK;
			context.Response.ContentType = "application/grpc";
			context.Response.Headers["grpc-status"] = "12";

			return Task.CompletedTask;
		});
	}

	private static void MapGrpcService<TService>(IEndpointRouteBuilder routes) where TService : class
	{
		if(routes.ServiceProvider.GetRequiredService<IOptions<GrpcServiceOptions<TService>>>().Value.IgnoreUnknownServices == false)
			throw new InvalidOperationException(Properties.Resources.Grpc_FallbackConflict_Message);

		routes.MapGrpcService<TService>();
	}

	private static ProtocolDescriptor GetDescriptor(Type service)
	{
		for(var type = service; type != null; type = type.BaseType)
		{
			var declaring = type.GetCustomAttribute<BindServiceMethodAttribute>(false)?.BindType ?? type.DeclaringType;

			if(declaring?.GetProperty("Descriptor", BindingFlags.Public | BindingFlags.Static)?.GetValue(null) is ProtocolDescriptor descriptor)
				return descriptor;
		}

		return null;
	}

	private static InvalidOperationException Invalid(string message, params object[] arguments) => new(string.Format(CultureInfo.CurrentCulture, message, arguments));
	#endregion

	#region 服务注册
	private sealed class Registration : IServiceRegistration
	{
		public void Register(IServiceCollection services, IConfiguration configuration)
		{
			services.AddGrpcWebEndpoints();
			services.AddGrpc(options => options.IgnoreUnknownServices = true);
			services.TryAddSingleton<ReflectionService>(_ => new ReflectionService());
		}
	}
	#endregion

	#region 嵌套类型
	private sealed class Region(string path)
	{
		public readonly WebEndpointMetadata Metadata = new(path, "grpc");
		public readonly Dictionary<string, (Type Type, ProtocolDescriptor Descriptor)> Services = new(StringComparer.OrdinalIgnoreCase);
	}

	private sealed class ReflectionService : ServerReflection.ServerReflectionBase
	{
		public override Task ServerReflectionInfo(IAsyncStreamReader<ServerReflectionRequest> requestStream, IServerStreamWriter<ServerReflectionResponse> responseStream, ServerCallContext context)
		{
			var reflection = context.GetHttpContext().GetEndpoint()?.Metadata.GetMetadata<ReflectionServiceImpl>() ??
				throw new InvalidOperationException(Properties.Resources.Grpc_EndpointsMissing_Message);
			return reflection.ServerReflectionInfo(requestStream, responseStream, context);
		}
	}
	#endregion
}
