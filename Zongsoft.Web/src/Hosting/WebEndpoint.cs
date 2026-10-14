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
using System.Net;
using System.Collections.Generic;

using Microsoft.AspNetCore.Server.Kestrel.Core;

using Zongsoft.Communication;

namespace Zongsoft.Web.Hosting;

/// <summary>表示启动时声明的一项 Web 监听范围。</summary>
public sealed class WebEndpoint
{
	#region 私有字段
	private readonly Dictionary<string, HttpProtocols> _versions = new(StringComparer.OrdinalIgnoreCase);
	#endregion

	#region 构造函数
	internal WebEndpoint(string name, NetworkAddress address, NetworkProtocol[] protocols, IReadOnlyDictionary<string, WebEndpointProtocol> policies)
	{
		this.Name = name;
		this.Address = address;
		this.Protocols = Array.AsReadOnly(protocols);
		this.IPAddress = address.HostType == UriHostNameType.Dns ? null : System.Net.IPAddress.Parse(address.Zone == null ? address.Host : $"{address.Host}%{address.Zone}");

		foreach(var protocol in protocols)
		{
			var version = protocol.HttpVersion switch
			{
				1 => HttpProtocols.Http1,
				2 => HttpProtocols.Http2,
				3 => HttpProtocols.Http3,
				_ => HttpProtocols.None,
			};

			var kind = policies[protocol.Name ?? "http"].Kind;
			_versions[kind] = this.GetProtocols(kind) | version;
			this.HttpProtocols |= version;
		}
	}
	#endregion

	#region 公共属性
	/// <summary>获取端点的配置名称。</summary>
	public string Name { get; }
	/// <summary>获取保留原始基路径的地址。</summary>
	public NetworkAddress Address { get; }
	/// <summary>获取已应用默认简写的只读协议集合。</summary>
	public IReadOnlyList<NetworkProtocol> Protocols { get; }
	/// <summary>获取所有请求类别声明的 HTTP 承载版本。</summary>
	public HttpProtocols HttpProtocols { get; }
	#endregion

	#region 公共方法
	/// <summary>获取指定请求类别声明的 HTTP 承载版本。</summary>
	/// <param name="kind">请求类别，默认普通 HTTP。</param>
	/// <returns>返回该类别的版本集合；未声明则返回 <see cref="HttpProtocols.None"/>。</returns>
	public HttpProtocols GetProtocols(string kind = "http") => kind != null && _versions.TryGetValue(kind, out var versions) ? versions : HttpProtocols.None;
	#endregion

	#region 内部成员
	internal IEnumerable<string> Kinds => _versions.Keys;
	internal IPAddress IPAddress { get; }
	internal HttpProtocols ListeningProtocols { get; set; }
	internal bool Contains(IPAddress address, int port) => address != null && port == this.Address.Port &&
		(this.IPAddress == null ? address.Equals(System.Net.IPAddress.Loopback) || address.Equals(System.Net.IPAddress.IPv6Loopback) :
		 this.Address.IsAny ? address.AddressFamily == this.IPAddress.AddressFamily : address.Equals(this.IPAddress));
	#endregion
}
