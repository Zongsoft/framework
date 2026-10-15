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
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;

using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;

using Zongsoft.Communication;

namespace Zongsoft.Web.Hosting;

/// <summary>提供 Web 声明与 Kestrel 配置之间的转换方法。</summary>
public static class KestrelUtility
{
	#region 公共方法
	/// <summary>将指定配置节转换为独立的 Kestrel 配置，不修改源配置或注册运行服务。</summary>
	/// <param name="configuration">包含 Endpoints 及原生服务器选项的配置节。</param>
	/// <param name="protocols">可选的协议规范化规则；显式 HTTP 承载不要求注册应用协议。</param>
	/// <returns>返回调用方负责释放的独立内存配置。</returns>
	public static ConfigurationRoot Convert(IConfiguration configuration, IEnumerable<WebEndpointProtocol> protocols = null) => Convert(configuration, out _, protocols);

	/// <summary>从同一份输入生成物理监听配置及保留路径的逻辑端点。</summary>
	/// <param name="configuration">包含 Endpoints 及原生服务器选项的配置节。</param>
	/// <param name="endpoints">返回规范化后的只读逻辑端点；不检查请求范围及应用处理器。</param>
	/// <param name="protocols">可选的协议规范化规则。</param>
	/// <returns>返回调用方负责释放的独立内存配置。</returns>
	public static ConfigurationRoot Convert(IConfiguration configuration, out IReadOnlyList<WebEndpoint> endpoints, IEnumerable<WebEndpointProtocol> protocols = null)
	{
		ArgumentNullException.ThrowIfNull(configuration);

		var projection = Read(configuration);
		var listeners = new List<Listener>();
		var declarations = new List<WebEndpoint>();
		using var snapshot = (ConfigurationRoot)new ConfigurationBuilder().AddInMemoryCollection(projection).Build();

		foreach(var section in snapshot.GetSection("Endpoints").GetChildren())
		{
			if(!NetworkAddress.TryParse(section["Url"], out var address) ||
			   address.Scheme is not ("http" or "https") ||
			   address.HostType == UriHostNameType.Dns && address.Host != "localhost")
				throw WebEndpointUtility.Invalid(section.Path + ":Url", "InvalidAddress");

			if(!NetworkProtocol.TryParse(section["Protocols"] ?? snapshot["EndpointDefaults:Protocols"], ',', out var values))
				throw WebEndpointUtility.Invalid(section.Path + ":Protocols", "InvalidProtocols");

			values = WebEndpointUtility.Normalize(values, WebEndpointUtility.GetProtocols(protocols), section.Path + ":Protocols", false);
			var endpoint = new WebEndpoint(section.Key, address, values);
			var versions = values.Aggregate(HttpProtocols.None, (result, protocol) => result | GetProtocols(protocol.HttpVersion));
			var listener = listeners.Find(item => WebEndpointUtility.SameSocket(endpoint, item.Endpoint) && address.Scheme == item.Endpoint.Address.Scheme);

			var settings = Read(section);
			settings.Remove("Url");
			settings.Remove("Protocols");

			if(listener == null)
				listeners.Add(new(endpoint, settings, versions));
			else
			{
				if(!Equal(settings, listener.Settings))
					throw WebEndpointUtility.Invalid(section.Path, "ConflictingListenerOptions");

				listener.Versions |= versions;
			}

			declarations.Add(endpoint);
		}

		foreach(var key in projection.Keys.Where(key => key.StartsWith("Endpoints:", StringComparison.OrdinalIgnoreCase)).ToArray())
			projection.Remove(key);

		foreach(var listener in listeners)
		{
			var endpoint = listener.Endpoint;
			var address = endpoint.Address;
			var prefix = "Endpoints:" + endpoint.Name + ":";

			if(address.Scheme == "http" && listener.Versions is not (HttpProtocols.Http1 or HttpProtocols.Http2))
				throw WebEndpointUtility.Invalid(prefix + "Protocols", "CleartextProtocolConflict");

			foreach(var other in listeners)
			{
				if(!ReferenceEquals(listener, other) && Overlaps(endpoint, other.Endpoint) && SharedTransport(listener.Versions, other.Versions))
					throw WebEndpointUtility.Invalid(prefix + "Url", "OverlappingListeners");
			}

			foreach(var setting in listener.Settings)
			{
				if(setting.Key.StartsWith("Sni:", StringComparison.OrdinalIgnoreCase) && setting.Key.EndsWith(":Protocols", StringComparison.OrdinalIgnoreCase) &&
					(!Enum.TryParse<HttpProtocols>(setting.Value, true, out var versions) || versions == HttpProtocols.None || (versions & ~listener.Versions) != 0))
					throw WebEndpointUtility.Invalid(prefix + setting.Key, "ConflictingSniProtocols");

				projection[prefix + setting.Key] = setting.Value;
			}

			var host = endpoint.IPAddress?.ToString() ?? address.Host;
			if(endpoint.IPAddress?.AddressFamily == AddressFamily.InterNetworkV6)
				host = $"[{host}]";

			projection[prefix + "Url"] = $"{address.Scheme}://{host}:{address.Port}";
			projection[prefix + "Protocols"] = listener.Versions.ToString();
		}

		if(listeners.Count > 0)
			projection.Remove("EndpointDefaults:Protocols");

		endpoints = declarations.AsReadOnly();
		return (ConfigurationRoot)new ConfigurationBuilder().AddInMemoryCollection(projection).Build();
	}

	/// <summary>使指定端点的 IPv6 TCP 监听仅绑定 IPv6；不影响其它监听工厂。</summary>
	/// <param name="options">待配置的 TCP socket 传输选项。</param>
	/// <param name="endpoints">需要适配的逻辑端点。</param>
	public static void Configure(SocketTransportOptions options, IEnumerable<WebEndpoint> endpoints)
	{
		ArgumentNullException.ThrowIfNull(options);
		ArgumentNullException.ThrowIfNull(endpoints);

		var declarations = endpoints.ToArray();
		var factory = options.CreateBoundListenSocket;
		options.CreateBoundListenSocket = endpoint =>
		{
			if(endpoint is not IPEndPoint ip || ip.AddressFamily != AddressFamily.InterNetworkV6 ||
				!declarations.Any(item => item.Contains(ip.Address, ip.Port) && item.Protocols.Any(protocol => protocol.Transport == ProtocolType.Tcp)))
				return factory(endpoint);

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
		};
	}
	#endregion

	#region 私有方法
	private static HttpProtocols GetProtocols(int version) => version switch
	{
		1 => HttpProtocols.Http1,
		2 => HttpProtocols.Http2,
		3 => HttpProtocols.Http3,
		_ => HttpProtocols.None,
	};

	private static Dictionary<string, string> Read(IConfiguration configuration) => configuration.AsEnumerable(makePathsRelative: true)
		.Where(entry => !string.IsNullOrEmpty(entry.Key) && entry.Value != null).ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.OrdinalIgnoreCase);

	private static bool Equal(Dictionary<string, string> left, Dictionary<string, string> right) => left.Count == right.Count &&
		left.All(entry => right.TryGetValue(entry.Key, out var value) && entry.Value == value);

	private static bool Overlaps(WebEndpoint left, WebEndpoint right)
	{
		if(left.Address.Port != right.Address.Port)
			return false;
		if(left.IPAddress == null)
			return right.IPAddress == null || right.Address.IsAny || right.IPAddress.Equals(IPAddress.Loopback) || right.IPAddress.Equals(IPAddress.IPv6Loopback);
		if(right.IPAddress == null)
			return left.Address.IsAny || left.IPAddress.Equals(IPAddress.Loopback) || left.IPAddress.Equals(IPAddress.IPv6Loopback);

		return left.IPAddress.AddressFamily == right.IPAddress.AddressFamily && (left.Address.IsAny || right.Address.IsAny || left.IPAddress.Equals(right.IPAddress));
	}

	private static bool SharedTransport(HttpProtocols left, HttpProtocols right) =>
		(left & HttpProtocols.Http1AndHttp2) != 0 && (right & HttpProtocols.Http1AndHttp2) != 0 ||
		(left & HttpProtocols.Http3) != 0 && (right & HttpProtocols.Http3) != 0;
	#endregion

	#region 嵌套类型
	private sealed class Listener(WebEndpoint endpoint, Dictionary<string, string> settings, HttpProtocols versions)
	{
		public readonly WebEndpoint Endpoint = endpoint;
		public readonly Dictionary<string, string> Settings = settings;
		public HttpProtocols Versions { get; set; } = versions;
	}
	#endregion
}
