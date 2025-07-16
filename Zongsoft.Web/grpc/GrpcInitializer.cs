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
using System.Reflection;
using System.Collections.Generic;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Zongsoft.Common;
using Zongsoft.Services;

namespace Zongsoft.Web.Grpc;

[Service<IApplicationInitializer<IApplicationBuilder>>]
public class GrpcInitializer : IApplicationInitializer<IApplicationBuilder>
{
	#region 私有变量
	private static readonly MethodInfo MapGrpcServiceMethod = typeof(GrpcEndpointRouteBuilderExtensions).GetMethod
	(
		nameof(GrpcEndpointRouteBuilderExtensions.MapGrpcService),
		1, BindingFlags.Public | BindingFlags.Static,
		null, [typeof(IEndpointRouteBuilder)], null
	);
	#endregion

	#region 公共属性
	public static ICollection<object> Services { get; } = [];
	#endregion

	#region 初始方法
	public void Initialize(IApplicationBuilder builder)
	{
		if(builder is IEndpointRouteBuilder app)
		{
			foreach(var service in Services)
			{
				if(service == null)
					continue;

				if(service is Type type)
					MapGrpcService(app, type);
				else
					MapGrpcService(app, service.GetType());
			}

			app.MapGrpcReflectionService();
		}
	}

	static void MapGrpcService(IEndpointRouteBuilder app, Type serviceType)
	{
		if(serviceType == null || serviceType.IsValueType || serviceType.IsAbstract)
			return;

		//Make the MapGrpcService method generic with the type of service.
		var method = MapGrpcServiceMethod.MakeGenericMethod(serviceType);

		//Invoke the MapGrpcService method to map gRPC services.
		method.Invoke(null, [app]);
	}
	#endregion

	#region 服务注册
	public sealed class Registration : IServiceRegistration
	{
		public void Register(IServiceCollection services, IConfiguration configuration)
		{
			services.AddGrpc();
			services.AddGrpcReflection();
		}
	}
	#endregion
}
