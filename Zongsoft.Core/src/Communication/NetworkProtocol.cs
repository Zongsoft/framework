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
using System.Net.Sockets;
using System.Collections.Generic;

namespace Zongsoft.Communication;

/// <summary>表示网络服务声明的一项协议组合，保留上层语义及其承载。</summary>
/// <remarks>默认值表示空协议。此类型不验证服务实现是否支持所声明的协议。</remarks>
public readonly struct NetworkProtocol : IEquatable<NetworkProtocol>, ISpanParsable<NetworkProtocol>
{
	#region 构造函数
	private NetworkProtocol(string name, string carrier)
	{
		this.Name = name;
		this.Carrier = carrier;
	}
	#endregion

	#region 公共属性
	/// <summary>获取规范化的上层协议名称；只声明承载时为空引用。</summary>
	public string Name { get; }

	/// <summary>获取承载名称：<c>tcp</c>、<c>udp</c>、<c>http</c>、<c>http1</c>、<c>http2</c> 或 <c>http3</c>；未指定时为空引用。</summary>
	public string Carrier { get; }

	/// <summary>获取是否为空协议。</summary>
	public bool IsEmpty => this.Name == null && this.Carrier == null;

	/// <summary>获取是否采用 HTTP 承载。</summary>
	public bool IsHttp => this.Carrier is "http" or "http1" or "http2" or "http3";

	/// <summary>获取 HTTP 主版本号；未声明版本或不采用 HTTP 承载时返回零。</summary>
	public int HttpVersion => this.Carrier switch
	{
		"http1" => 1,
		"http2" => 2,
		"http3" => 3,
		_ => 0,
	};

	/// <summary>获取传输协议；承载或 HTTP 版本未声明时返回 <see cref="ProtocolType.Unspecified"/>。</summary>
	public ProtocolType Transport => this.Carrier switch
	{
		"tcp" or "http1" or "http2" => ProtocolType.Tcp,
		"udp" or "http3" => ProtocolType.Udp,
		_ => ProtocolType.Unspecified,
	};
	#endregion

	#region 解析方法
	/// <summary>解析单项协议组合。</summary>
	/// <param name="text">协议文本，不包含逗号分隔的并列项。</param>
	/// <returns>返回规范化的协议值。</returns>
	/// <exception cref="FormatException">文本为空或组合角色不明确。</exception>
	public static NetworkProtocol Parse(string text) => Parse(text.AsSpan());

	/// <summary>解析单项协议组合。</summary>
	/// <param name="text">协议文本，不包含并列项；不自动补全上层协议的默认承载。</param>
	/// <returns>返回规范化的协议值。</returns>
	/// <exception cref="FormatException">文本为空或组合角色不明确。</exception>
	public static NetworkProtocol Parse(ReadOnlySpan<char> text) => TryParse(text, out var result) ? result : throw new FormatException(Properties.Resources.InvalidFormat_Message);

	/// <summary>尝试解析单项协议组合。</summary>
	/// <param name="text">协议文本；名称不区分大小写，加号两侧顺序无关。</param>
	/// <param name="result">成功时为规范化的协议值，失败时为默认值。</param>
	/// <returns>如果解析成功则返回真(<see langword="true"/>)，否则返回假(<see langword="false"/>)。</returns>
	public static bool TryParse(string text, out NetworkProtocol result) => TryParse(text.AsSpan(), out result);

	/// <summary>尝试解析单项协议组合。</summary>
	/// <param name="text">协议文本；单项允许仅声明名称或承载，组合必须包含一个名称与一个承载。</param>
	/// <param name="result">成功时为规范化的协议值，失败时为默认值。</param>
	/// <returns>如果解析成功则返回真(<see langword="true"/>)，否则返回假(<see langword="false"/>)。</returns>
	public static bool TryParse(ReadOnlySpan<char> text, out NetworkProtocol result)
	{
		result = default;
		text = text.Trim();

		if(text.IsEmpty)
			return false;

		var separator = text.IndexOf('+');

		if(separator < 0)
		{
			if(!IsName(text))
				return false;

			var carrier = GetCarrier(text);
			result = carrier == null ? new(text.ToString().ToLowerInvariant(), null) : new(null, carrier);
			return true;
		}

		var left = text[..separator].Trim();
		var right = text[(separator + 1)..].Trim();

		if(!IsName(left) || !IsName(right))
			return false;

		var leftCarrier = GetCarrier(left);
		var rightCarrier = GetCarrier(right);

		if((leftCarrier == null) == (rightCarrier == null))
			return false;

		result = leftCarrier == null ? new(left.ToString().ToLowerInvariant(), rightCarrier) : new(right.ToString().ToLowerInvariant(), leftCarrier);
		return true;
	}

	/// <summary>解析并规范化以指定字符分隔的协议集合。</summary>
	/// <param name="text">已完成模板求值的协议列表。</param>
	/// <param name="separator">分隔字符；不能是协议标识字符、加号、空白、控制字符或代理项。</param>
	/// <returns>返回去重并按规范字符串序数排序的协议数组。</returns>
	/// <exception cref="FormatException">列表为空、分隔字符无效或任一项无效。</exception>
	public static NetworkProtocol[] Parse(string text, char separator) => Parse(text.AsSpan(), separator);

	/// <summary>解析并规范化以指定字符分隔的协议集合。</summary>
	/// <param name="text">已完成模板求值的协议列表。</param>
	/// <param name="separator">分隔字符；不能是协议标识字符、加号、空白、控制字符或代理项。</param>
	/// <returns>返回去重并按规范字符串序数排序的协议数组。</returns>
	/// <exception cref="FormatException">列表为空、分隔字符无效或任一项无效。</exception>
	public static NetworkProtocol[] Parse(ReadOnlySpan<char> text, char separator) => TryParse(text, separator, out var result) ? result : throw new FormatException(Properties.Resources.InvalidFormat_Message);

	/// <summary>尝试解析并规范化协议集合，不跨上层语义归并 HTTP 泛称与具体版本。</summary>
	/// <param name="text">已完成模板求值的协议列表。</param>
	/// <param name="separator">分隔字符；不能是协议标识字符、加号、空白、控制字符或代理项。</param>
	/// <param name="result">成功时为去重、排序后的数组，失败时为空数组。</param>
	/// <returns>如果分隔字符及所有项都有效则返回真(<see langword="true"/>)，否则返回假(<see langword="false"/>)；失败时不返回部分结果。</returns>
	public static bool TryParse(string text, char separator, out NetworkProtocol[] result) => TryParse(text.AsSpan(), separator, out result);

	/// <summary>尝试解析并规范化协议集合，不跨上层语义归并 HTTP 泛称与具体版本。</summary>
	/// <param name="text">已完成模板求值的协议列表。</param>
	/// <param name="separator">分隔字符；不能是协议标识字符、加号、空白、控制字符或代理项。</param>
	/// <param name="result">成功时为去重、排序后的数组，失败时为空数组。</param>
	/// <returns>如果分隔字符及所有项都有效则返回真(<see langword="true"/>)，否则返回假(<see langword="false"/>)；失败时不返回部分结果。</returns>
	public static bool TryParse(ReadOnlySpan<char> text, char separator, out NetworkProtocol[] result)
	{
		result = [];

		if(text.IsEmpty || IsNameCharacter(separator) || separator == '+' || char.IsWhiteSpace(separator) || char.IsControl(separator) || char.IsSurrogate(separator))
			return false;

		var protocols = new HashSet<NetworkProtocol>();

		while(true)
		{
			var index = text.IndexOf(separator);

			if(!TryParse(index < 0 ? text : text[..index], out var protocol))
				return false;

			protocols.Add(protocol);

			if(index < 0)
				break;

			text = text[(index + 1)..];
		}

		// 只有相同上层语义的具体 HTTP 版本才能消除其泛称。
		protocols.RemoveWhere(protocol => protocol.Carrier == "http" &&
			(protocols.Contains(new(protocol.Name, "http1")) ||
			 protocols.Contains(new(protocol.Name, "http2")) ||
			 protocols.Contains(new(protocol.Name, "http3"))));

		result = new NetworkProtocol[protocols.Count];
		protocols.CopyTo(result);
		Array.Sort(result, (left, right) => string.Compare(left.ToString(), right.ToString(), StringComparison.Ordinal));
		return true;
	}

	static NetworkProtocol IParsable<NetworkProtocol>.Parse(string text, IFormatProvider provider) => Parse(text.AsSpan());
	static bool IParsable<NetworkProtocol>.TryParse(string text, IFormatProvider provider, out NetworkProtocol result) => TryParse(text.AsSpan(), out result);
	static NetworkProtocol ISpanParsable<NetworkProtocol>.Parse(ReadOnlySpan<char> text, IFormatProvider provider) => Parse(text);
	static bool ISpanParsable<NetworkProtocol>.TryParse(ReadOnlySpan<char> text, IFormatProvider provider, out NetworkProtocol result) => TryParse(text, out result);
	#endregion

	#region 私有方法
	private static string GetCarrier(ReadOnlySpan<char> text)
	{
		if(text.Equals("tcp", StringComparison.OrdinalIgnoreCase))
			return "tcp";

		if(text.Equals("udp", StringComparison.OrdinalIgnoreCase))
			return "udp";

		if(text.Equals("http", StringComparison.OrdinalIgnoreCase))
			return "http";

		if(text.Equals("http1", StringComparison.OrdinalIgnoreCase))
			return "http1";

		if(text.Equals("http2", StringComparison.OrdinalIgnoreCase))
			return "http2";

		return text.Equals("http3", StringComparison.OrdinalIgnoreCase) ? "http3" : null;
	}

	private static bool IsName(ReadOnlySpan<char> text)
	{
		if(text.IsEmpty || !char.IsAsciiLetter(text[0]))
			return false;

		foreach(var character in text[1..])
		{
			if(!IsNameCharacter(character))
				return false;
		}

		return true;
	}

	private static bool IsNameCharacter(char character) => char.IsAsciiLetterOrDigit(character) || character is '-' or '.' or '_';
	#endregion

	#region 重写方法
	/// <summary>判断当前协议与另一协议是否具有相同上层语义和承载。</summary>
	/// <param name="other">待比较的协议。</param>
	/// <returns>如果两者相等则返回真(<see langword="true"/>)，否则返回假(<see langword="false"/>)。</returns>
	public bool Equals(NetworkProtocol other) => this.Name == other.Name && this.Carrier == other.Carrier;

	/// <summary>判断指定对象是否为相等的协议。</summary>
	/// <param name="obj">待比较的对象。</param>
	/// <returns>如果对象为相等协议则返回真(<see langword="true"/>)，否则返回假(<see langword="false"/>)。</returns>
	public override bool Equals(object obj) => obj is NetworkProtocol other && this.Equals(other);

	/// <summary>获取协议的哈希码。</summary>
	/// <returns>返回基于上层语义与承载计算的哈希码。</returns>
	public override int GetHashCode() => HashCode.Combine(this.Name, this.Carrier);

	/// <summary>获取规范化的协议文本。</summary>
	/// <returns>返回小写的规范组合；空协议返回空字符串。</returns>
	public override string ToString() => this.Name == null ? this.Carrier ?? string.Empty : this.Carrier == null ? this.Name : $"{this.Name}+{this.Carrier}";
	#endregion

	#region 运算符号
	/// <summary>判断两个协议是否相等。</summary>
	/// <param name="left">左侧协议。</param>
	/// <param name="right">右侧协议。</param>
	/// <returns>如果相等则返回真(<see langword="true"/>)，否则返回假(<see langword="false"/>)。</returns>
	public static bool operator ==(NetworkProtocol left, NetworkProtocol right) => left.Equals(right);

	/// <summary>判断两个协议是否不相等。</summary>
	/// <param name="left">左侧协议。</param>
	/// <param name="right">右侧协议。</param>
	/// <returns>如果不相等则返回真(<see langword="true"/>)，否则返回假(<see langword="false"/>)。</returns>
	public static bool operator !=(NetworkProtocol left, NetworkProtocol right) => !left.Equals(right);
	#endregion
}
