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
using System.Linq;
using System.Collections.Generic;

using Zongsoft.Communication;

namespace Zongsoft.Web.Hosting;

/// <summary>表示一项与服务器实现无关的 Web 监听范围。</summary>
public sealed class WebEndpoint
{
	#region 构造函数
	/// <summary>初始化指定名称、地址及协议的逻辑端点。</summary>
	/// <param name="name">端点名称。</param>
	/// <param name="address">保留基路径的 Web 地址。</param>
	/// <param name="protocols">协议声明；默认承载由消费方的协议规则补全。</param>
	public WebEndpoint(string name, NetworkAddress address, IEnumerable<NetworkProtocol> protocols)
	{
		ArgumentException.ThrowIfNullOrEmpty(name);
		ArgumentNullException.ThrowIfNull(protocols);

		if(address.Scheme is not ("http" or "https"))
			throw WebEndpointUtility.Invalid(name + ":Url", "InvalidAddress");
		if(!WebEndpointUtility.IsPath(address.Path))
			throw WebEndpointUtility.Invalid(name + ":Url", "InvalidBasePath");

		var declarations = protocols.ToArray();
		if(declarations.Length == 0 || declarations.Any(protocol => protocol.IsEmpty))
			throw WebEndpointUtility.Invalid(name + ":Protocols", "InvalidProtocols");

		this.Name = name;
		this.Address = address;
		this.Path = address.Path.Length > 1 ? address.Path.TrimEnd('/') : address.Path;
		this.Protocols = Array.AsReadOnly(NetworkProtocol.Parse(string.Join(',', declarations), ','));
		this.IPAddress = address.HostType == UriHostNameType.Dns ? null : System.Net.IPAddress.Parse(address.Zone == null ? address.Host : $"{address.Host}%{address.Zone}");
	}
	#endregion

	#region 公共属性
	/// <summary>获取端点名称。</summary>
	public string Name { get; }
	/// <summary>获取用于范围匹配的基路径；除根路径外不含尾斜杠，比较时忽略大小写。</summary>
	public string Path { get; }
	/// <summary>获取保留基路径的地址。</summary>
	public NetworkAddress Address { get; }
	/// <summary>获取只读协议声明。</summary>
	public IReadOnlyList<NetworkProtocol> Protocols { get; }
	#endregion

	#region 公共方法
	/// <summary>判断是否声明了指定 HTTP 承载版本。</summary>
	/// <param name="httpVersion">HTTP 主版本号。</param>
	/// <returns>如果已声明则返回真(<see langword="true"/>)，否则返回假(<see langword="false"/>)。</returns>
	public bool Supports(int httpVersion) => httpVersion > 0 && this.Protocols.Any(protocol => protocol.HttpVersion == httpVersion);
	#endregion

	#region 内部成员
	internal IPAddress IPAddress { get; }
	internal bool Contains(IPAddress address, int port) => address != null && port == this.Address.Port &&
		(this.IPAddress == null ? this.Address.Host == "localhost" && (address.Equals(System.Net.IPAddress.Loopback) || address.Equals(System.Net.IPAddress.IPv6Loopback)) :
		 this.Address.IsAny ? address.AddressFamily == this.IPAddress.AddressFamily : address.Equals(this.IPAddress));
	#endregion
}
