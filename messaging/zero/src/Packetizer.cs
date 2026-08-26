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
using System.Text;
using System.Collections.Generic;

namespace Zongsoft.Messaging.ZeroMQ;

internal static class Packetizer
{
	#region 私有常量
	private const char Delimiter = '\n';
	#endregion

	#region 公共方法
	public static string Pack(string topic) => $"{topic}{Delimiter}{Protocol.Headers.Version}:{Protocol.Version}";
	public static string Pack(string identity, string identifier, string topic, string tags, string compression)
	{
		Validate(topic, nameof(topic));
		Validate(identity, nameof(identity));
		Validate(identifier, nameof(identifier));
		Validate(tags, nameof(tags));
		Validate(compression, nameof(compression));
		if(Encoding.UTF8.GetByteCount(topic) > Protocol.MaxTopicSize)
			throw new ArgumentOutOfRangeException(nameof(topic));
		if(identifier?.Length > Protocol.MaxIdentifierSize)
			throw new ArgumentOutOfRangeException(nameof(identifier));

		var result = $"{topic}{Delimiter}{Protocol.Headers.Version}:{Protocol.Version}{Delimiter}{Protocol.Headers.Identifier}:{identifier}{Delimiter}{Protocol.Headers.Identity}:{identity}";
		if(!string.IsNullOrEmpty(tags))
			result += $"{Delimiter}{Protocol.Headers.Tags}:{tags}";
		if(!string.IsNullOrEmpty(compression))
			result += $"{Delimiter}{Protocol.Headers.Compression}:{compression}";
		return result;
	}

	public static bool TryUnpack(ReadOnlySpan<char> header, out string topic, out IReadOnlyList<KeyValuePair<string, string>> options)
	{
		topic = null;
		options = [];

		if(header.IsEmpty || header.Length > Protocol.MaxHeaderSize || header.IndexOf('\r') >= 0)
			return false;

		var delimiter = header.IndexOf(Delimiter);
		if(delimiter < 0)
			return false;

		topic = header[..delimiter].ToString();
		if(Encoding.UTF8.GetByteCount(topic) > Protocol.MaxTopicSize)
			return false;

		var result = new List<KeyValuePair<string, string>>();
		var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var text = header[(delimiter + 1)..];

		while(!text.IsEmpty)
		{
			if(result.Count >= Protocol.MaxOptionCount)
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
		return TryGetValue(result, Protocol.Headers.Version, out var version) && string.Equals(version, Protocol.Version, StringComparison.Ordinal);
	}

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
	#endregion

	#region 私有方法
	private static void Validate(string value, string name)
	{
		if(value?.IndexOfAny(['\r', '\n']) >= 0)
			throw new ArgumentException(Properties.Resources.ZeroQueue_HeaderValueInvalid_Message, name);
	}
	#endregion
}
