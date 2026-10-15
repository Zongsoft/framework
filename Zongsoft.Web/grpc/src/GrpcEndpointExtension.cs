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
using System.Threading.Tasks;

using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

using Zongsoft.Communication;
using Zongsoft.Web.Hosting;

namespace Zongsoft.Web.Grpc;

/// <summary>提供原生 gRPC 的 Web 端点协议接入。</summary>
public static class GrpcEndpointExtension
{
	#region 静态字段
	private static readonly WebEndpointProtocol _protocol = new GrpcEndpointProtocol();
	#endregion

	#region 公共方法
	/// <summary>在创建 Web 端点快照之前注册 gRPC 声明、请求分类及路由规则。</summary>
	/// <param name="services">服务集合。</param>
	/// <returns>返回原服务集合；服务类型与方法映射仍由 gRPC 初始化器负责。</returns>
	public static IServiceCollection AddGrpcWebEndpoints(this IServiceCollection services) => services.AddWebEndpointProtocol(_protocol);
	#endregion

	#region 嵌套类型
	private sealed class GrpcEndpointProtocol() : WebEndpointProtocol("grpc", "http2", "grpc")
	{
		public override NetworkProtocol Normalize(NetworkProtocol protocol)
		{
			var normalized = base.Normalize(protocol);
			return normalized.HttpVersion is 2 or 3 ? normalized : default;
		}

		public override bool Matches(HttpContext context)
		{
			if(!MediaTypeHeaderValue.TryParse(context.Request.ContentType, out var mediaType))
				return false;

			var value = mediaType.MediaType.Value;
			return string.Equals(value, "application/grpc", StringComparison.OrdinalIgnoreCase) ||
				value.Length > 17 && value.StartsWith("application/grpc+", StringComparison.OrdinalIgnoreCase);
		}

		public override int Validate(HttpContext context)
		{
			if(context.Request.Protocol is not ("HTTP/2" or "HTTP/3"))
				return StatusCodes.Status400BadRequest;
			if(context.Request.Method == "POST")
				return 0;

			context.Response.Headers.Allow = "POST";
			return StatusCodes.Status405MethodNotAllowed;
		}

		public override bool Accepts(Endpoint endpoint, WebEndpointFeature feature)
		{
			if(endpoint is not RouteEndpoint route || route.RoutePattern.Parameters.Count != 0)
				return true;

			//基路径按 Web 范围关联；原生服务名和方法名仍须逐字匹配
			var offset = feature.Endpoint.Path == "/" ? 0 : feature.Endpoint.Path.Length;
			return route.RoutePattern.RawText?.Length > offset && feature.Path.Length > offset &&
				route.RoutePattern.RawText.AsSpan(offset).Equals(feature.Path.AsSpan(offset), StringComparison.Ordinal);
		}

		public override Task RejectAsync(HttpContext context)
		{
			context.Response.StatusCode = StatusCodes.Status200OK;
			context.Response.ContentType = "application/grpc";
			context.Response.Headers["grpc-status"] = "12";
			return Task.CompletedTask;
		}
	}
	#endregion
}
