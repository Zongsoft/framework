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

namespace Zongsoft.Messaging.ZeroMQ;

internal static class Packetizer
{
	private const char Delimiter = '\n';

	public static int GetCompressionThreshold(MessageEnqueueOptions options, int length)
	{
		if(options != null && options.Properties.TryGetValue(Options.Compressive, out var value) && Zongsoft.Common.Convert.TryConvertValue<int>(value, out var threshold) && threshold > 0 && length > threshold)
			return threshold;

		return 0;
	}

	public static string Pack(string topic) => $"{topic}@";
	public static string Pack(string identifier, string topic, string compressor) => string.IsNullOrEmpty(compressor) ?
		$"{topic}@{identifier}" :
		$"{topic}@{identifier}{Delimiter}{Options.Compressor}:{compressor}";

	public static string Pack(string identifier, string topic, ReadOnlyMemory<byte> data, MessageEnqueueOptions options, out string compressor)
	{
		if(options != null && options.Properties.TryGetValue(Options.Compressive, out var value) && Zongsoft.Common.Convert.TryConvertValue<int>(value, out var integer) && integer > 0 && data.Length > integer)
		{
			compressor = nameof(IO.Compression.Compressor.Brotli);
			return $"{topic}@{identifier}{Delimiter}{Options.Compressor}:{compressor}";
		}

		compressor = null;
		return $"{topic}@{identifier}";
	}

	public static bool TryUnpack(ReadOnlySpan<char> header, out string identifier, out string topic, out IReadOnlyList<KeyValuePair<string, string>> options)
	{
		identifier = null;
		topic = null;
		options = [];

		if(header.IsEmpty)
			return false;

		var delimiter = header.IndexOf(Delimiter);
		var address = delimiter < 0 ? header : header[..delimiter];
		var separator = address.LastIndexOf('@');

		if(separator <= 0)
			return false;

		topic = address[..separator].ToString();
		identifier = address[(separator + 1)..].ToString();

		if(delimiter < 0)
			return true;

		var result = new List<KeyValuePair<string, string>>();
		var text = header[(delimiter + 1)..];

		while(!text.IsEmpty)
		{
			var end = text.IndexOf(Delimiter);
			var entry = end < 0 ? text : text[..end];
			entry = entry.Trim();

			if(entry.IsEmpty)
				return false;

			var index = entry.IndexOf(':');
			if(index <= 0 || index == entry.Length - 1)
				return false;

			result.Add(new(entry[..index].ToString(), entry[(index + 1)..].ToString()));
			if(end < 0)
				break;

			text = text[(end + 1)..];
		}

		options = result;
		return true;
	}

	public static string Unpack(ReadOnlySpan<char> header, out string topic, out IEnumerable<KeyValuePair<string, string>> options)
	{
		if(!header.IsEmpty)
		{
			var index = header.IndexOf(Delimiter);

			if(index < 0)
			{
				options = [];
				return Parse(header, out topic);
			}
			else
			{
				options = ParseOptions(header[(index + 1)..].ToString());
				return Parse(header[..index], out topic);
			}
		}

		topic = header.ToString();
		options = [];
		return null;
	}

	private static string Parse(ReadOnlySpan<char> url, out string topic)
	{
		if(!url.IsEmpty)
		{
			var index = url.LastIndexOf('@');

			if(index > 0 && index < url.Length)
			{
				topic = url[..index].ToString();
				return url[(index + 1)..].ToString();
			}
		}

		topic = url.ToString();
		return null;
	}

	private static IEnumerable<KeyValuePair<string, string>> ParseOptions(string text)
	{
		if(string.IsNullOrEmpty(text))
			yield break;

		var parts = text.Split(Delimiter, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

		foreach(var part in parts)
		{
			var index = part.IndexOf(':');

			switch(index)
			{
				case 0:
					yield return new KeyValuePair<string, string>(string.Empty, part[(index + 1)..]);
					break;
				case < 0:
					yield return new KeyValuePair<string, string>(part, null);
					break;
				case > 0:
					yield return new KeyValuePair<string, string>(part[..index], part[(index + 1)..]);
					break;
			}
		}
	}

	public sealed class Options
	{
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
