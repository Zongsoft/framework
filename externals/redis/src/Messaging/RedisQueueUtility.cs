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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;

using StackExchange.Redis;

namespace Zongsoft.Externals.Redis.Messaging;

public static class RedisQueueUtility
{
	public static string IncreaseId(ReadOnlySpan<char> id)
	{
		if(id.IsEmpty || (id.Length == 1 && (id[0] == '0' || id[0] == '-')))
			return "0-1";

		long value;
		var index = id.LastIndexOf('-');

		if(index < 0) //没有分隔符
			return long.TryParse(id, out value) ? $"{value}-1" : throw IllegalId(id);

		if(index == 0) //分隔符位于首字符
			return long.TryParse(id[1..], out value) ? $"0-{value + 1}" : throw IllegalId(id);

		if(index == id.Length - 1) //分隔符位于最末尾
			return $"{id}1";

		return long.TryParse(id[(index + 1)..], out value) ? $"{id[..index]}-{value + 1}" : throw IllegalId(id);
	}

	public static string DecreaseId(ReadOnlySpan<char> id)
	{
		if(id.IsEmpty || (id.Length == 1 && (id[0] == '0' || id[0] == '-')) || id == "0-0")
			return "0";

		long value;
		var index = id.LastIndexOf('-');

		if(index < 0) //没有分隔符
			return long.TryParse(id, out value) ? $"{value - 1}-{long.MaxValue}" : throw IllegalId(id);

		if(index == 0) //分隔符位于首字符
			return long.TryParse(id[1..], out value) ? value > 1 ? $"0-{value - 1}" : "0" : throw IllegalId(id);

		if(index == id.Length - 1) //分隔符位于最末尾
			return long.TryParse(id[0..index], out value) ? (value > 0 ? $"{value - 1}-{long.MaxValue}" : "0") : throw IllegalId(id);

		if(long.TryParse(id[(index + 1)..], out value))
		{
			if(value > 0)
				return $"{id[..index]}-{value - 1}";

			if(long.TryParse(id[0..index], out value))
				return value > 0 ? $"{value - 1}-{long.MaxValue}" : "0";
		}

		throw IllegalId(id);
	}

	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
	private static ArgumentException IllegalId(ReadOnlySpan<char> id) => new ArgumentException(string.Format(Properties.Resources.InvalidMessageId_Message, id.ToString()));

	internal static string GetQueueName(string name, string topic) => string.IsNullOrWhiteSpace(topic) ? $"Zongsoft.Queue:{name}" : $"Zongsoft.Queue:{name}:{topic}";

	internal static RedisValue GetMessageData(this StreamEntry entry) => RedisUtility.GetValue(entry, "Data");
	internal static RedisValue GetMessageTags(this StreamEntry entry) => RedisUtility.GetValue(entry, "Tags");
	internal static RedisValue GetMessageCompression(this StreamEntry entry) => RedisUtility.GetValue(entry, "Compression");
	internal static NameValueEntry[] GetMessagePayload(ReadOnlyMemory<byte> data, string tags, string compression)
	{
		var tagged = !string.IsNullOrEmpty(tags);
		var compressed = !string.IsNullOrEmpty(compression);
		var result = new NameValueEntry[1 + (tagged ? 1 : 0) + (compressed ? 1 : 0)];
		result[0] = new("Data", data);

		var index = 1;
		if(tagged)
			result[index++] = new("Tags", tags);
		if(compressed)
			result[index] = new("Compression", compression);

		return result;
	}
}
