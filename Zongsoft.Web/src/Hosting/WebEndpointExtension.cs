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
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing.Matching;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Zongsoft.Web.Hosting;

/// <summary>提供端点范围服务及请求管线的注册方法。</summary>
public static class WebEndpointExtension
{
	#region 公共方法
	/// <summary>在创建端点快照之前注册应用协议规则。</summary>
	/// <param name="services">服务集合。</param>
	/// <param name="protocol">共享的协议规则实例；启动后必须保持线程安全。</param>
	/// <returns>返回原服务集合。</returns>
	public static IServiceCollection AddWebEndpointProtocol(this IServiceCollection services, WebEndpointProtocol protocol)
	{
		ArgumentNullException.ThrowIfNull(services);
		ArgumentNullException.ThrowIfNull(protocol);

		if(services.Any(descriptor => descriptor.ImplementationInstance is WebEndpointCollection))
			throw WebEndpointUtility.Invalid(protocol.Name, "SnapshotAlreadyCreated");

		foreach(var descriptor in services.Where(descriptor => descriptor.ServiceType == typeof(WebEndpointProtocol)))
		{
			if(descriptor.ImplementationInstance is not WebEndpointProtocol existing)
				throw WebEndpointUtility.Invalid(protocol.Name, "ProtocolInstanceRequired");
			if(ReferenceEquals(existing, protocol))
				return services;
			if(existing.Name == protocol.Name || protocol.Kind != "http" && existing.Kind == protocol.Kind)
				throw WebEndpointUtility.Invalid(protocol.Name, "DuplicateProtocol");
		}

		if(protocol.Name == "http")
			throw WebEndpointUtility.Invalid(protocol.Name, "ReservedProtocol");

		services.AddSingleton(protocol);
		return services;
	}

	/// <summary>注册逻辑端点快照及路由约束。</summary>
	/// <param name="services">服务集合。</param>
	/// <param name="declarations">与服务器配置无关的逻辑端点声明。</param>
	/// <returns>返回注册的逻辑端点快照。</returns>
	public static WebEndpointCollection AddWebEndpoints(this IServiceCollection services, IEnumerable<WebEndpoint> declarations)
	{
		ArgumentNullException.ThrowIfNull(services);

		if(services.Any(descriptor => descriptor.ServiceType == typeof(WebEndpointCollection)))
			throw WebEndpointUtility.Invalid("Endpoints", "AlreadyRegistered");

		var protocols = services
			.Where(descriptor => descriptor.ServiceType == typeof(WebEndpointProtocol))
			.Select(descriptor => descriptor.ImplementationInstance as WebEndpointProtocol ?? throw WebEndpointUtility.Invalid("Protocols", "ProtocolInstanceRequired"));
		var endpoints = new WebEndpointCollection(declarations, protocols);

		services.AddSingleton(endpoints);
		services.TryAddEnumerable(ServiceDescriptor.Singleton<MatcherPolicy, WebEndpointMatcherPolicy>());

		return endpoints;
	}

	/// <summary>延迟到最终服务提供程序中创建逻辑端点快照。</summary>
	/// <param name="services">服务集合。</param>
	/// <param name="factory">逻辑端点工厂；以最终注册的协议规则规范化声明。</param>
	/// <returns>返回原服务集合。</returns>
	public static IServiceCollection AddWebEndpoints(this IServiceCollection services, Func<IServiceProvider, IEnumerable<WebEndpoint>> factory)
	{
		ArgumentNullException.ThrowIfNull(services);
		ArgumentNullException.ThrowIfNull(factory);

		if(services.Any(descriptor => descriptor.ServiceType == typeof(WebEndpointCollection)))
			throw WebEndpointUtility.Invalid("Endpoints", "AlreadyRegistered");

		services.AddSingleton(provider => new WebEndpointCollection(factory(provider), provider.GetServices<WebEndpointProtocol>()));
		services.TryAddEnumerable(ServiceDescriptor.Singleton<MatcherPolicy, WebEndpointMatcherPolicy>());
		return services;
	}

	/// <summary>通过启动过滤器在应用管线之前安装请求范围中间件。</summary>
	/// <param name="services">服务集合；须另行注册逻辑端点。</param>
	/// <returns>返回原服务集合；重复调用不会重复安装。</returns>
	public static IServiceCollection AddWebEndpointPipeline(this IServiceCollection services)
	{
		ArgumentNullException.ThrowIfNull(services);
		services.TryAddEnumerable(ServiceDescriptor.Singleton<IStartupFilter, WebEndpointStartupFilter>());
		return services;
	}

	/// <summary>在路由、转发头及静态文件中间件之前确定请求范围。</summary>
	/// <param name="application">应用管线构建器。</param>
	/// <returns>返回原应用管线构建器。</returns>
	public static IApplicationBuilder UseWebEndpoints(this IApplicationBuilder application)
	{
		ArgumentNullException.ThrowIfNull(application);

		if(application.Properties.ContainsKey(typeof(WebEndpointMiddleware).FullName) || application.Properties.ContainsKey("__EndpointRouteBuilder"))
			throw WebEndpointUtility.Invalid("Pipeline", "InvalidMiddlewareOrder");

		application.Properties.Add(typeof(WebEndpointMiddleware).FullName, true);
		return application.UseMiddleware<WebEndpointMiddleware>();
	}
	#endregion

	#region 嵌套类型
	private sealed class WebEndpointStartupFilter : IStartupFilter
	{
		public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => application =>
		{
			application.UseWebEndpoints();
			next(application);
		};
	}

	private sealed class WebEndpointMiddleware(RequestDelegate next, WebEndpointCollection endpoints)
	{
		public Task InvokeAsync(HttpContext context)
		{
			if(context.GetEndpoint() != null)
				throw WebEndpointUtility.Invalid("Pipeline", "InvalidMiddlewareOrder");

			var protocol = endpoints.SelectProtocol(context);
			var version = context.Request.Protocol switch
			{
				"HTTP/1.0" or "HTTP/1.1" => 1,
				"HTTP/2" => 2,
				"HTTP/3" => 3,
				_ => 0,
			};

			var status = protocol.Validate(context);
			if(status != 0)
				return Reject(context, status);

			var target = context.Features.Get<IHttpRequestFeature>()?.RawTarget;

			if(target == "*" && context.Request.Method == "OPTIONS")
				return Reject(context, StatusCodes.Status404NotFound);
			if(!TryGetPath(target, out var path))
				return Reject(context, StatusCodes.Status400BadRequest);

			var endpoint = endpoints.Find(context, path, protocol.Kind, version);

			if(endpoint != null)
			{
				if(!endpoints.Supports(endpoint, protocol.Kind, version))
					return Reject(context, StatusCodes.Status400BadRequest);

				//已解析路径不能穿越逻辑范围；这里只拒绝，不重新分派请求
				if(!ReferenceEquals(endpoint, endpoints.Find(context, context.Request.PathBase.Add(context.Request.Path).Value, protocol.Kind, version)))
					return Reject(context, StatusCodes.Status404NotFound);

				context.Features.Set(new WebEndpointFeature(endpoint, path, protocol));
				return next(context);
			}

			return protocol.RejectAsync(context);
		}

		private static Task Reject(HttpContext context, int status)
		{
			context.Response.StatusCode = status;
			return Task.CompletedTask;
		}

		private static bool TryGetPath(string target, out string path)
		{
			path = null;
			if(string.IsNullOrEmpty(target))
				return false;

			foreach(var character in target)
			{
				if(char.IsWhiteSpace(character) || char.IsControl(character) || character is '\\' or '#')
					return false;
			}

			var text = target.AsSpan();
			if(text[0] != '/')
			{
				var start = text.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ? 7 : text.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ? 8 : 0;
				if(start == 0 || !Uri.TryCreate(target, UriKind.Absolute, out var uri) || !string.IsNullOrEmpty(uri.UserInfo))
					return false;

				var end = text[start..].IndexOfAny('/', '?');
				if(end == 0 || text.Length == start)
					return false;

				text = end < 0 ? "/" : text[(start + end)..];
			}

			var query = text.IndexOf('?');

			if(query >= 0)
				text = text[..query];
			if(text.IsEmpty)
				text = "/";

			for(var i = 0; i < text.Length; i++)
			{
				if(text[i] != '%')
					continue;
				if(i + 2 >= text.Length || !char.IsAsciiHexDigit(text[i + 1]) || !char.IsAsciiHexDigit(text[i + 2]))
					return false;

				i += 2;
			}

			path = text.ToString();
			return true;
		}
	}

	private sealed class WebEndpointMatcherPolicy : MatcherPolicy, IEndpointSelectorPolicy
	{
		public override int Order => -2000;
		public bool AppliesToEndpoints(IReadOnlyList<Endpoint> endpoints) => true;
		public Task ApplyAsync(HttpContext httpContext, CandidateSet candidates)
		{
			var feature = httpContext.Features.Get<WebEndpointFeature>();

			if(feature == null)
				throw WebEndpointUtility.Invalid("Pipeline", "MissingEndpointFeature");

			for(var i = 0; i < candidates.Count; i++)
			{
				if(!candidates.IsValidCandidate(i))
					continue;

				var endpoint = candidates[i].Endpoint;
				var metadata = endpoint.Metadata.GetMetadata<WebEndpointMetadata>();
				var valid = metadata == null ? feature.Kind == "http" : metadata.Kind == feature.Kind && metadata.Path == feature.Endpoint.Address.Path;
				valid = valid && feature.Protocol.Accepts(endpoint, feature);

				if(!valid)
					candidates.SetValidity(i, false);
			}

			return Task.CompletedTask;
		}
	}
	#endregion
}
