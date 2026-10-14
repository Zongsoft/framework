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
using System.Collections;
using System.Collections.Generic;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;

using Zongsoft.Communication;

namespace Zongsoft.Web.Hosting;

/// <summary>提供 Web 端点的不可变启动快照及独立的 Kestrel 配置投影。</summary>
public sealed class WebEndpointCollection : IReadOnlyList<WebEndpoint>
{
	#region 成员字段
	private readonly WebEndpoint[] _endpoints;
	private readonly Dictionary<string, string> _source;
	private readonly Dictionary<string, string> _projection;
	private readonly Dictionary<string, WebEndpointProtocol> _protocols;
	private readonly WebEndpointProtocol[] _kinds;
	#endregion

	#region 构造函数
	/// <summary>从最终的 Kestrel 配置节创建快照。</summary>
	/// <param name="configuration">Kestrel 配置节，不是整个应用配置根。</param>
	/// <param name="protocols">已注册的协议规则；未指定时仅支持普通 HTTP。</param>
	public WebEndpointCollection(IConfiguration configuration, IEnumerable<WebEndpointProtocol> protocols = null)
	{
		ArgumentNullException.ThrowIfNull(configuration);

		_protocols = new(StringComparer.Ordinal) { ["http"] = WebEndpointProtocol.Http };
		var kinds = new Dictionary<string, WebEndpointProtocol>(StringComparer.Ordinal);

		foreach(var protocol in protocols ?? [])
		{
			ArgumentNullException.ThrowIfNull(protocol);

			if(!_protocols.TryAdd(protocol.Name, protocol))
				throw WebEndpointUtility.Invalid(protocol.Name, "DuplicateProtocol");
			if(protocol.Kind != "http" && !kinds.TryAdd(protocol.Kind, protocol))
				throw WebEndpointUtility.Invalid(protocol.Kind, "DuplicateKind");
		}

		_kinds = kinds.Values.OrderBy(protocol => protocol.Kind, StringComparer.Ordinal).ToArray();
		_source = Read(configuration);
		_projection = new(_source, StringComparer.OrdinalIgnoreCase);

		using var snapshot = (ConfigurationRoot)new ConfigurationBuilder().AddInMemoryCollection(_source).Build();
		var endpoints = new List<WebEndpoint>();
		var listeners = new List<Listener>();
		var defaults = snapshot["EndpointDefaults:Protocols"];

		foreach(var section in snapshot.GetSection("Endpoints").GetChildren())
		{
			if(!NetworkAddress.TryParse(section["Url"], out var address) ||
			   address.Scheme is not ("http" or "https") ||
			   address.HostType == UriHostNameType.Dns && address.Host != "localhost")
				throw WebEndpointUtility.Invalid(section.Path + ":Url", "InvalidAddress");

			if(!WebEndpointUtility.IsPath(address.Path))
				throw WebEndpointUtility.Invalid(section.Path + ":Url", "InvalidBasePath");

			var declarations = this.ParseProtocols(section["Protocols"] ?? defaults, section.Path + ":Protocols");
			var endpoint = new WebEndpoint(section.Key, address, declarations, _protocols);
			var settings = Read(section);
			settings.Remove("Url");
			settings.Remove("Protocols");

			foreach(var other in endpoints)
			{
				if(string.Equals(address.Path, other.Address.Path, StringComparison.OrdinalIgnoreCase))
				{
					foreach(var kind in endpoint.Kinds.Intersect(other.Kinds))
					{
						if(address.Path != other.Address.Path || SameSocket(endpoint, other) && SharedTransport(endpoint.GetProtocols(kind), other.GetProtocols(kind)))
							throw WebEndpointUtility.Invalid(section.Path, "ConflictingRange");
					}
				}
			}

			var listener = listeners.Find(item => SameSocket(endpoint, item.Endpoint) && address.Scheme == item.Endpoint.Address.Scheme);
			if(listener == null)
				listeners.Add(new(endpoint, settings));
			else
			{
				if(!Equal(settings, listener.Settings))
					throw WebEndpointUtility.Invalid(section.Path, "ConflictingListenerOptions");

				listener.Versions |= endpoint.HttpProtocols;
			}

			endpoints.Add(endpoint);
		}

		foreach(var key in _projection.Keys.Where(key => key.StartsWith("Endpoints:", StringComparison.OrdinalIgnoreCase)).ToArray())
			_projection.Remove(key);

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

				_projection[prefix + setting.Key] = setting.Value;
			}

			var host = endpoint.IPAddress?.ToString() ?? address.Host;
			if(endpoint.IPAddress?.AddressFamily == AddressFamily.InterNetworkV6)
				host = $"[{host}]";

			_projection[prefix + "Url"] = $"{address.Scheme}://{host}:{address.Port}";
			_projection[prefix + "Protocols"] = listener.Versions.ToString();

			foreach(var logical in endpoints.Where(item => SameSocket(item, endpoint) && item.Address.Scheme == address.Scheme))
				logical.ListeningProtocols = listener.Versions;
		}

		// 每个投影端点已有明确版本；原生默认值不参与声明能力推断。
		_projection.Remove("EndpointDefaults:Protocols");
		_endpoints = endpoints.OrderByDescending(endpoint => endpoint.Address.Path.Length).ThenBy(endpoint => endpoint.Name, StringComparer.Ordinal).ToArray();
	}
	#endregion

	#region 公共属性
	/// <summary>获取逻辑端点数量。</summary>
	public int Count => _endpoints.Length;
	/// <summary>获取指定索引的逻辑端点。</summary>
	public WebEndpoint this[int index] => _endpoints[index];
	#endregion

	#region 公共方法
	/// <summary>判断配置是否与启动快照不同，不改变当前监听或路由。</summary>
	/// <param name="configuration">当前 Kestrel 配置节。</param>
	/// <returns>如果不同则返回真(<see langword="true"/>)，否则返回假(<see langword="false"/>)。</returns>
	public bool HasChanged(IConfiguration configuration)
	{
		ArgumentNullException.ThrowIfNull(configuration);
		return !Equal(_source, Read(configuration));
	}

	/// <summary>以独立内存配置替换尚未加载的 Kestrel 配置源，并禁用网络热重载。</summary>
	/// <param name="options">服务器选项；应在宿主最终配置阶段调用。</param>
	public void Configure(KestrelServerOptions options)
	{
		ArgumentNullException.ThrowIfNull(options);
		options.Configure(new ConfigurationBuilder().AddInMemoryCollection(_projection).Build(), reloadOnChange: false);
	}

	/// <summary>使管理的 IPv6 TCP 监听仅绑定 IPv6，不覆盖 IPv4。</summary>
	/// <param name="options">TCP socket 传输选项。</param>
	public void Configure(SocketTransportOptions options)
	{
		ArgumentNullException.ThrowIfNull(options);
		var factory = options.CreateBoundListenSocket;

		options.CreateBoundListenSocket = endpoint =>
		{
			if(endpoint is not IPEndPoint ip ||
			   ip.AddressFamily != AddressFamily.InterNetworkV6 ||
			   !_endpoints.Any(item => item.Contains(ip.Address, ip.Port)))
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

	/// <summary>获取逻辑端点枚举器。</summary>
	/// <returns>返回只读快照的枚举器。</returns>
	public IEnumerator<WebEndpoint> GetEnumerator() => ((IEnumerable<WebEndpoint>)_endpoints).GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => _endpoints.GetEnumerator();
	#endregion

	#region 私有方法
	internal WebEndpointProtocol SelectProtocol(HttpContext context)
	{
		WebEndpointProtocol selected = null;

		foreach(var protocol in _kinds)
		{
			if(!protocol.Matches(context))
				continue;
			if(selected != null)
				throw WebEndpointUtility.Invalid("Pipeline", "AmbiguousProtocol");

			selected = protocol;
		}

		return selected ?? WebEndpointProtocol.Http;
	}

	private NetworkProtocol[] ParseProtocols(string text, string path)
	{
		if(!NetworkProtocol.TryParse(text, ',', out var protocols))
			throw WebEndpointUtility.Invalid(path, "InvalidProtocols");

		for(var i = 0; i < protocols.Length; i++)
		{
			if(!_protocols.TryGetValue(protocols[i].Name ?? "http", out var policy))
				throw WebEndpointUtility.Invalid(path, "UnregisteredProtocol");

			var normalized = policy.Normalize(protocols[i]);
			if(normalized.IsEmpty || normalized.Name != protocols[i].Name)
				throw WebEndpointUtility.Invalid(path, "UnsupportedProtocols");

			protocols[i] = normalized;
		}

		protocols = NetworkProtocol.Parse(string.Join(',', protocols.Select(protocol => protocol.ToString())), ',');
		if(protocols.Any(protocol => !protocol.IsHttp || protocol.HttpVersion == 0))
			throw WebEndpointUtility.Invalid(path, "UnsupportedProtocols");

		return protocols;
	}

	private static Dictionary<string, string> Read(IConfiguration configuration) => configuration.AsEnumerable(makePathsRelative: true)
		.Where(entry => !string.IsNullOrEmpty(entry.Key) && entry.Value != null).ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.OrdinalIgnoreCase);

	private static bool Equal(Dictionary<string, string> left, Dictionary<string, string> right) => left.Count == right.Count &&
		left.All(entry => right.TryGetValue(entry.Key, out var value) && entry.Value == value);

	private static bool SameSocket(WebEndpoint left, WebEndpoint right) => left.Address.Port == right.Address.Port &&
		(left.IPAddress == null ? right.IPAddress == null : left.IPAddress.Equals(right.IPAddress));

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

	#region 嵌套子类
	private sealed class Listener(WebEndpoint endpoint, Dictionary<string, string> settings)
	{
		public WebEndpoint Endpoint { get; } = endpoint;
		public Dictionary<string, string> Settings { get; } = settings;
		public HttpProtocols Versions { get; set; } = endpoint.HttpProtocols;
	}
	#endregion
}
