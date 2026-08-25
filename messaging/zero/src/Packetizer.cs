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
 * This file is part of Zongsoft.Messaging.ZeroMQ library.
 *
 * The Zongsoft.Messaging.ZeroMQ is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Messaging.ZeroMQ is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Messaging.ZeroMQ library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace Zongsoft.Messaging.ZeroMQ;

internal static class Packetizer
{
	private const char Delimiter = '\n';
	public const string ProtocolVersion = "2.0";
	public const int MaxHeaderSize = 16 * 1024;
	public const int MaxTopicSize = 1024;
	public const int MaxIdentifierSize = 256;
	public const int MaxOptionCount = 32;
	public const int MaxPayloadSize = 64 * 1024 * 1024;

	public static int GetCompressionThreshold(MessageEnqueueOptions options, int length)
	{
		if(options != null && options.Properties.TryGetValue(Options.Compressive, out var value) && Zongsoft.Common.Convert.TryConvertValue<int>(value, out var threshold) && threshold > 0 && length > threshold)
			return threshold;

		return 0;
	}

	public static string Pack(string topic) => $"{topic}@{Delimiter}{Options.ProtocolVersion}:{ProtocolVersion}";
	public static string Pack(string identity, string identifier, string topic, string compressor) => string.IsNullOrEmpty(compressor) ?
		$"{topic}@{identity}{Delimiter}{Options.ProtocolVersion}:{ProtocolVersion}{Delimiter}{Options.Identifier}:{identifier}" :
		$"{topic}@{identity}{Delimiter}{Options.ProtocolVersion}:{ProtocolVersion}{Delimiter}{Options.Identifier}:{identifier}{Delimiter}{Options.Compressor}:{compressor}";

	public static bool TryUnpack(ReadOnlySpan<char> header, out string identifier, out string topic, out IReadOnlyList<KeyValuePair<string, string>> options)
	{
		identifier = null;
		topic = null;
		options = [];

		if(header.IsEmpty || header.Length > MaxHeaderSize)
			return false;

		var delimiter = header.IndexOf(Delimiter);
		var address = delimiter < 0 ? header : header[..delimiter];
		var separator = address.LastIndexOf('@');

		if(separator <= 0)
			return false;

		topic = address[..separator].ToString();
		identifier = address[(separator + 1)..].ToString();
		if(Encoding.UTF8.GetByteCount(topic) > MaxTopicSize || Encoding.UTF8.GetByteCount(identifier) > MaxIdentifierSize)
			return false;

		if(delimiter < 0)
			return false;

		var result = new List<KeyValuePair<string, string>>();
		var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var text = header[(delimiter + 1)..];

		while(!text.IsEmpty)
		{
			if(result.Count >= MaxOptionCount)
				return false;

			var end = text.IndexOf(Delimiter);
			var entry = end < 0 ? text : text[..end];
			entry = entry.Trim();

			if(entry.IsEmpty)
				return false;

			var index = entry.IndexOf(':');
			if(index <= 0 || index == entry.Length - 1)
				return false;

			var name = entry[..index].ToString();
			if(!names.Add(name))
				return false;

			result.Add(new(name, entry[(index + 1)..].ToString()));
			if(end < 0)
				break;

			text = text[(end + 1)..];
		}

		options = result;
		return Options.TryGetValue(result, Options.ProtocolVersion, out var version) && string.Equals(version, ProtocolVersion, StringComparison.Ordinal);
	}

	public sealed class Options
	{
		/// <summary>协议版本的选项。</summary>
		public const string ProtocolVersion = "Protocol-Version";
		/// <summary>消息标识的选项。</summary>
		public const string Identifier = nameof(Identifier);
		/// <summary>压缩器名称的选项。</summary>
		public const string Compressor = nameof(Compressor);
		/// <summary>压缩阈值的选项，单位为字节。</summary>
		public const string Compressive = nameof(Compressive);

		public static bool TryGetValue(IEnumerable<KeyValuePair<string, string>> options, string name, out string value)
		{
			if(options != null)
			{
				foreach(var option in options)
				{
					if(string.Equals(option.Key, name, StringComparison.OrdinalIgnoreCase))
					{
						value = option.Value;
						return true;
					}
				}
			}

			value = null;
			return false;
		}
	}
}
