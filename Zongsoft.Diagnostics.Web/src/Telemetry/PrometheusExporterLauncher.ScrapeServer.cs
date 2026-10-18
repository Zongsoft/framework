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
 * This file is part of Zongsoft.Diagnostics library.
 *
 * The Zongsoft.Diagnostics is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Diagnostics is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Diagnostics library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Net;
using System.Linq;
using System.Threading;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;

using OpenTelemetry;

using Zongsoft.Services;
using Zongsoft.Communication;

namespace Zongsoft.Diagnostics.Telemetry;

public partial class PrometheusExporterLauncher
{
	private sealed class ScrapeServer : IDisposable
	{
		#region 成员字段
		private IHost _host;
		#endregion

		#region 构造函数
		public ScrapeServer(IServiceProvider services, global::OpenTelemetry.Metrics.MeterProvider provider, NetworkAddress[] addresses, string path)
		{
			var endpoints = addresses.SelectMany(GetEndpoints).Distinct().ToArray();
			var configuration = services.GetService<IConfiguration>() ?? ApplicationContext.Current?.Configuration;
			var handler = CreateHandler(services, provider, endpoints, path);

			var host = new HostBuilder().ConfigureWebHost(web => web
				.UseSetting(WebHostDefaults.PreventHostingStartupKey, "true")
				.ConfigureAppConfiguration((_, builder) => ConfigureCertificates(builder, configuration))
				.UseKestrel((context, options) => ConfigureEndpoints(options, context.Configuration, endpoints))
				.ConfigureServices(collection => collection.Configure<SocketTransportOptions>(ConfigureSockets))
				.Configure(application => application.Run(handler))).Build();

			try
			{
				host.Start();
				_host = host;
			}
			catch
			{
				host.Dispose();
				throw;
			}
		}
		#endregion

		#region 资源释放
		public void Dispose() => Interlocked.Exchange(ref _host, null)?.Dispose();
		#endregion

		#region 监听配置
		private static IEnumerable<Endpoint> GetEndpoints(NetworkAddress address)
		{
			if(address.HostType != UriHostNameType.Dns)
			{
				var ip = IPAddress.Parse(address.Zone == null ? address.Host : $"{address.Host}%{address.Zone}");
				yield return new(new(ip, address.Port), address.Scheme, null);
				yield break;
			}

			//域名不参与 DNS 解析；通配监听接收请求后仍须匹配声明的主机名。
			var loopback = address.Host == "localhost";
			var host = loopback ? null : address.Host;
			yield return new(new(loopback ? IPAddress.Loopback : IPAddress.Any, address.Port), address.Scheme, host);

			if(Socket.OSSupportsIPv6)
				yield return new(new(loopback ? IPAddress.IPv6Loopback : IPAddress.IPv6Any, address.Port), address.Scheme, host);
		}

		private static void ConfigureEndpoints(KestrelServerOptions options, IConfiguration configuration, Endpoint[] endpoints)
		{
			options.Configure(configuration, reloadOnChange: false);
			var listeners = endpoints.DistinctBy(endpoint => (endpoint.Address, endpoint.Scheme)).ToArray();

			foreach(var listener in listeners)
			{
				//只合并物理监听；请求范围仍按各条原始声明分别匹配。
				if(!listener.IsAny && listeners.Any(
				   other => other.IsAny &&
				   other.Scheme == listener.Scheme &&
				   other.Address.Port == listener.Address.Port &&
				   other.Address.AddressFamily == listener.Address.AddressFamily))
					continue;

				options.Listen(listener.Address, endpoint =>
				{
					endpoint.Protocols = HttpProtocols.Http1;

					if(listener.Scheme == "https")
						endpoint.UseHttps();
				});
			}
		}

		private static void ConfigureCertificates(IConfigurationBuilder builder, IConfiguration configuration)
		{
			//仅共享证书配置，独立抓取服务器不读取宿主的监听端点。
			if(configuration != null)
				builder.AddInMemoryCollection(configuration.GetSection("Kestrel:Certificates").AsEnumerable(true)
					.Select(item => new KeyValuePair<string, string>("Certificates:" + item.Key, item.Value)));
		}

		private static void ConfigureSockets(SocketTransportOptions options)
		{
			var factory = options.CreateBoundListenSocket;

			options.CreateBoundListenSocket = endpoint => endpoint.AddressFamily == AddressFamily.InterNetworkV6 ?
				CreateIpv6Socket(endpoint) : factory(endpoint);
		}

		private static Socket CreateIpv6Socket(EndPoint endpoint)
		{
			//IPv6 声明不隐式覆盖 IPv4；域名的双栈监听由地址展开明确建立。
			var socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);

			try
			{
				socket.DualMode = false;
				socket.Bind(endpoint);
				return socket;
			}
			catch
			{
				socket.Dispose();
				throw;
			}
		}
		#endregion

		#region 请求处理
		private static RequestDelegate CreateHandler(IServiceProvider services, global::OpenTelemetry.Metrics.MeterProvider provider, Endpoint[] endpoints, string path)
		{
			//提供程序构建完成后再创建官方中间件，避免解析尚在构建中的指标读取器。
			var pipeline = new Lazy<RequestDelegate>(() =>
			{
				var application = new ApplicationBuilder(services);
				application.UseOpenTelemetryPrometheusScrapingEndpoint(provider, _ => true, null, null, null);
				return application.Build();
			}, LazyThreadSafetyMode.PublicationOnly);

			return context =>
			{
				var target = context.Features.Get<IHttpRequestFeature>()?.RawTarget;
				if(MatchesPath(target, path) && MatchesPath(context.Request.Path.Value, path) && MatchesEndpoint(context, endpoints))
					return pipeline.Value(context);

				context.Response.StatusCode = StatusCodes.Status404NotFound;
				return Task.CompletedTask;
			};
		}

		private static bool MatchesEndpoint(HttpContext context, Endpoint[] endpoints)
		{
			//HostString 可包含 Unicode 域名；统一为 URI 使用的 ASCII 形式后再比较。
			var host = new HostString(context.Request.Host.ToUriComponent()).Host;
			if(host.EndsWith('.'))
				host = host[..^1];

			var connection = context.Connection;

			foreach(var endpoint in endpoints)
			{
				if(endpoint.Address.Port != connection.LocalPort ||
				   endpoint.Scheme != context.Request.Scheme ||
				   endpoint.Address.AddressFamily != connection.LocalIpAddress?.AddressFamily)
					continue;

				if(!endpoint.IsAny && !endpoint.Address.Address.Equals(connection.LocalIpAddress))
					continue;

				if(endpoint.Host == null || string.Equals(endpoint.Host, host, StringComparison.OrdinalIgnoreCase))
					return true;
			}

			return false;
		}

		private static bool MatchesPath(string target, string path)
		{
			if(string.IsNullOrEmpty(target))
				return false;

			var request = target.AsSpan();
			var query = request.IndexOf('?');

			if(query >= 0)
				request = request[..query];

			if(!request.StartsWith("/", StringComparison.Ordinal))
			{
				var authority = request.IndexOf("://", StringComparison.Ordinal);
				if(authority < 0)
					return false;

				var offset = request[(authority + 3)..].IndexOf('/');
				request = offset < 0 ? "/" : request[(authority + 3 + offset)..];
			}

			return request.StartsWith(path, StringComparison.OrdinalIgnoreCase) &&
				(path == "/" || request.Length == path.Length || request[path.Length] == '/');
		}
		#endregion

		#region 嵌套结构
		private readonly record struct Endpoint(IPEndPoint Address, string Scheme, string Host)
		{
			public bool IsAny => this.Address.Address.Equals(IPAddress.Any) || this.Address.Address.Equals(IPAddress.IPv6Any);
		}
		#endregion
	}
}
