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
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Net;
using System.Net.Sockets;
using System.Globalization;

namespace Zongsoft.Communication;

/// <summary>表示网络服务声明的不可变地址，保留监听主机、地址族和路径。</summary>
/// <remarks>仅解析地址，不解析 DNS、不建立连接、不推断 IPv6 通配覆盖 IPv4。</remarks>
public sealed class NetworkAddress : IEquatable<NetworkAddress>, ISpanParsable<NetworkAddress>
{
	#region 构造函数
	private NetworkAddress(string scheme, string host, UriHostNameType hostType, string zone, int port, string path)
	{
		this.Scheme = scheme;
		this.Host = host;
		this.HostType = hostType;
		this.Zone = zone;
		this.Port = port;
		this.Path = path;
	}
	#endregion

	#region 公共属性
	/// <summary>获取小写的地址方案，不限定应用协议名称，也不从其名称推断传输。</summary>
	public string Scheme { get; }

	/// <summary>获取规范化的主机名或 IP 地址，不包含 IPv6 方括号和区域标识。</summary>
	public string Host { get; }

	/// <summary>获取主机种类，分别为 DNS、IPv4 或 IPv6。</summary>
	public UriHostNameType HostType { get; }

	/// <summary>获取 IP 地址族；DNS 主机返回 <see cref="AddressFamily.Unspecified"/>。</summary>
	public AddressFamily AddressFamily => this.HostType switch
	{
		UriHostNameType.IPv4 => AddressFamily.InterNetwork,
		UriHostNameType.IPv6 => AddressFamily.InterNetworkV6,
		_ => AddressFamily.Unspecified,
	};

	/// <summary>获取区分大小写的 IPv6 区域标识；未声明时为空引用。</summary>
	public string Zone { get; }

	/// <summary>获取监听端口，范围为 1 至 65535。</summary>
	public int Port { get; }

	/// <summary>获取区分大小写并保留尾部斜杠的路径；未声明时为根路径。</summary>
	public string Path { get; }

	/// <summary>获取是否为当前地址族的通配监听地址。</summary>
	public bool IsAny => this.Host is "0.0.0.0" or "::";

	/// <summary>获取是否为 localhost 或 IP 回环地址。</summary>
	public bool IsLoopback => this.HostType == UriHostNameType.Dns ? this.Host == "localhost" : IPAddress.IsLoopback(IPAddress.Parse(this.Host));
	#endregion

	#region 解析方法
	/// <summary>解析网络服务地址。</summary>
	/// <param name="text">具有方案、主机及可选路径的完整地址。</param>
	/// <returns>返回规范化的网络地址。</returns>
	/// <exception cref="FormatException">地址为空或不符合声明格式。</exception>
	public static NetworkAddress Parse(string text) => Parse(text.AsSpan());

	/// <summary>解析网络服务地址。</summary>
	/// <param name="text">具有方案、主机及可选路径的完整地址。</param>
	/// <returns>返回规范化的网络地址。</returns>
	/// <exception cref="FormatException">地址为空或不符合声明格式。</exception>
	public static NetworkAddress Parse(ReadOnlySpan<char> text) => TryParse(text, out var result) ? result : throw new FormatException(Properties.Resources.InvalidFormat_Message);

	/// <summary>尝试解析网络服务地址。</summary>
	/// <param name="text">地址文本；IPv6 必须使用方括号，区域标识以 <c>%25</c> 分隔。</param>
	/// <param name="result">成功时为规范化的地址，失败时为空引用。</param>
	/// <returns>如果解析成功则返回真(<see langword="true"/>)，否则返回假(<see langword="false"/>)。</returns>
	public static bool TryParse(string text, out NetworkAddress result) => TryParse(text.AsSpan(), out result);

	/// <summary>尝试解析网络服务地址。</summary>
	/// <param name="text">地址文本；IPv6 必须使用方括号，区域标识以 <c>%25</c> 分隔。</param>
	/// <param name="result">成功时为规范化的地址，失败时为空引用。</param>
	/// <returns>如果解析成功则返回真(<see langword="true"/>)，否则返回假(<see langword="false"/>)。</returns>
	public static bool TryParse(ReadOnlySpan<char> text, out NetworkAddress result)
	{
		result = null;
		text = text.Trim();

		if(text.IsEmpty)
			return false;

		foreach(var character in text)
		{
			if(char.IsWhiteSpace(character) || char.IsControl(character) || character is '\\' or '?' or '#')
				return false;
		}

		var separator = text.IndexOf("://", StringComparison.Ordinal);

		if(separator < 1 || !IsScheme(text[..separator]))
			return false;

		var scheme = text[..separator];
		var remaining = text[(separator + 3)..];
		var slash = remaining.IndexOf('/');
		var authority = slash < 0 ? remaining : remaining[..slash];
		ReadOnlySpan<char> path = slash < 0 ? "/" : remaining[slash..];

		if(authority.IsEmpty || authority.Contains('@') || !IsPath(path))
			return false;

		ReadOnlySpan<char> host;
		ReadOnlySpan<char> portText = default;
		var hasPort = false;
		string normalizedHost;
		string zone = null;
		UriHostNameType hostType;

		if(authority[0] == '[')
		{
			var end = authority.IndexOf(']');

			if(end < 2)
				return false;

			if(end < authority.Length - 1)
			{
				if(authority[end + 1] != ':')
					return false;

				portText = authority[(end + 2)..];
				hasPort = true;
			}

			host = authority[1..end];
			var percent = host.IndexOf('%');

			if(percent >= 0)
			{
				if(!host[percent..].StartsWith("%25", StringComparison.Ordinal))
					return false;

				var zoneText = host[(percent + 3)..];

				if(zoneText.IsEmpty)
					return false;

				foreach(var character in zoneText)
				{
					if(!IsUnreserved(character))
						return false;
				}

				zone = zoneText.ToString();
				host = host[..percent];
			}

			if(!IPAddress.TryParse(host, out var address) || address.AddressFamily != AddressFamily.InterNetworkV6)
				return false;

			normalizedHost = address.ToString();
			hostType = UriHostNameType.IPv6;
		}
		else
		{
			var colon = authority.IndexOf(':');
			host = colon < 0 ? authority : authority[..colon];

			if(colon >= 0)
			{
				portText = authority[(colon + 1)..];
				hasPort = true;
			}

			if(!TryGetHost(host, out normalizedHost, out hostType))
				return false;
		}

		var port = scheme.Equals("http", StringComparison.OrdinalIgnoreCase) ? 80 : scheme.Equals("https", StringComparison.OrdinalIgnoreCase) ? 443 : 0;

		if(hasPort && !int.TryParse(portText, NumberStyles.None, CultureInfo.InvariantCulture, out port) || port is < 1 or > 65535)
			return false;

		result = new(scheme.ToString().ToLowerInvariant(), normalizedHost, hostType, zone, port, path.ToString());
		return true;
	}

	static NetworkAddress IParsable<NetworkAddress>.Parse(string text, IFormatProvider provider) => Parse(text.AsSpan());
	static bool IParsable<NetworkAddress>.TryParse(string text, IFormatProvider provider, out NetworkAddress result) => TryParse(text.AsSpan(), out result);
	static NetworkAddress ISpanParsable<NetworkAddress>.Parse(ReadOnlySpan<char> text, IFormatProvider provider) => Parse(text);
	static bool ISpanParsable<NetworkAddress>.TryParse(ReadOnlySpan<char> text, IFormatProvider provider, out NetworkAddress result) => TryParse(text, out result);
	#endregion

	#region 私有方法
	private static bool TryGetHost(ReadOnlySpan<char> text, out string host, out UriHostNameType type)
	{
		host = null;
		type = UriHostNameType.Unknown;

		if(text.SequenceEqual("*"))
		{
			host = "0.0.0.0";
			type = UriHostNameType.IPv4;
			return true;
		}

		if(text.IsEmpty)
			return false;

		if(IPAddress.TryParse(text, out var address))
		{
			if(address.AddressFamily != AddressFamily.InterNetwork)
				return false;

			host = address.ToString();
			type = UriHostNameType.IPv4;
			return true;
		}

		try
		{
			host = new IdnMapping().GetAscii(text.ToString()).ToLowerInvariant();
		}
		catch(ArgumentException)
		{
			return false;
		}

		if(host.EndsWith('.'))
			host = host[..^1];

		if(host.Length is < 1 or > 253)
			return false;

		var numeric = true;

		foreach(var label in host.Split('.'))
		{
			if(label.Length is < 1 or > 63 || label[0] == '-' || label[^1] == '-')
				return false;

			foreach(var character in label)
			{
				if(!char.IsAsciiLetterOrDigit(character) && character != '-')
					return false;

				numeric &= char.IsAsciiDigit(character);
			}
		}

		// 无效的纯数字 IP 地址不能回退成 DNS 名称。
		if(numeric)
			return false;

		type = UriHostNameType.Dns;
		return true;
	}

	private static bool IsScheme(ReadOnlySpan<char> text)
	{
		if(text.IsEmpty || !char.IsAsciiLetter(text[0]))
			return false;

		foreach(var character in text[1..])
		{
			if(!char.IsAsciiLetterOrDigit(character) && character is not ('+' or '-' or '.'))
				return false;
		}

		return true;
	}

	private static bool IsPath(ReadOnlySpan<char> path)
	{
		for(var i = 0; i < path.Length; i++)
		{
			var character = path[i];

			if(character == '%')
			{
				if(i + 2 >= path.Length || !char.IsAsciiHexDigit(path[i + 1]) || !char.IsAsciiHexDigit(path[i + 2]))
					return false;

				i += 2;
			}
			else if(char.IsHighSurrogate(character))
			{
				if(i + 1 >= path.Length || !char.IsLowSurrogate(path[i + 1]))
					return false;

				i++;
			}
			else if(char.IsLowSurrogate(character) ||
				(character <= 0x7F && !IsUnreserved(character) && character is not ('/' or ':' or '@' or '!' or '$' or '&' or '\'' or '(' or ')' or '*' or '+' or ',' or ';' or '=')))
				return false;
		}

		return true;
	}

	private static bool IsUnreserved(char character) => char.IsAsciiLetterOrDigit(character) || character is '-' or '.' or '_' or '~';
	#endregion

	#region 重写方法
	/// <summary>判断另一地址是否具有相同的方案、主机、区域、端口和路径。</summary>
	/// <param name="other">待比较的地址。</param>
	/// <returns>如果相等则返回真(<see langword="true"/>)，否则返回假(<see langword="false"/>)。</returns>
	public bool Equals(NetworkAddress other) => other is not null && this.Scheme == other.Scheme && this.Host == other.Host && this.HostType == other.HostType && this.Zone == other.Zone && this.Port == other.Port && this.Path == other.Path;

	/// <summary>判断指定对象是否为相等的网络地址。</summary>
	/// <param name="obj">待比较的对象。</param>
	/// <returns>如果对象为相等地址则返回真(<see langword="true"/>)，否则返回假(<see langword="false"/>)。</returns>
	public override bool Equals(object obj) => obj is NetworkAddress other && this.Equals(other);

	/// <summary>获取地址的哈希码。</summary>
	/// <returns>返回基于地址所有身份字段计算的哈希码。</returns>
	public override int GetHashCode() => HashCode.Combine(this.Scheme, this.Host, this.HostType, this.Zone, this.Port, this.Path);

	/// <summary>获取规范化的地址文本。</summary>
	/// <returns>返回包含显式端口及路径的地址；IPv6 区域以 <c>%25</c> 分隔。</returns>
	public override string ToString()
	{
		var host = this.HostType == UriHostNameType.IPv6 ? $"[{this.Host}{(this.Zone == null ? string.Empty : "%25" + this.Zone)}]" : this.Host;
		return $"{this.Scheme}://{host}:{this.Port.ToString(CultureInfo.InvariantCulture)}{this.Path}";
	}
	#endregion

	#region 运算符号
	/// <summary>判断两个地址是否相等。</summary>
	/// <param name="left">左侧地址。</param>
	/// <param name="right">右侧地址。</param>
	/// <returns>如果相等则返回真(<see langword="true"/>)，否则返回假(<see langword="false"/>)；两个空引用相等。</returns>
	public static bool operator ==(NetworkAddress left, NetworkAddress right) => ReferenceEquals(left, right) || left is not null && left.Equals(right);

	/// <summary>判断两个地址是否不相等。</summary>
	/// <param name="left">左侧地址。</param>
	/// <param name="right">右侧地址。</param>
	/// <returns>如果不相等则返回真(<see langword="true"/>)，否则返回假(<see langword="false"/>)。</returns>
	public static bool operator !=(NetworkAddress left, NetworkAddress right) => !(left == right);
	#endregion
}
