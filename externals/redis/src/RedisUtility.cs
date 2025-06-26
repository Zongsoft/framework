﻿/*
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
 * Copyright (C) 2010-2024 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.Redis library.
 *
 * The Zongsoft.Externals.Redis is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Redis is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Redis library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;

using StackExchange.Redis;

namespace Zongsoft.Externals.Redis;

public static class RedisUtility
{
	public static RedisQueuePendingMessageInfo[] GetPendingMessages(this IDatabase database, string key, string group, TimeSpan idle, int count = 100, string minimum = "-", string maximum = "+") =>
		GetPendingMessages(database, key, group, null, idle > TimeSpan.Zero ? (long)idle.TotalMilliseconds : 0L, count, minimum, maximum);

	public static RedisQueuePendingMessageInfo[] GetPendingMessages(this IDatabase database, string key, string group, string consumer = null, int count = 100, string minimum = "-", string maximum = "+") =>
		GetPendingMessages(database, key, group, consumer, 0L, count, minimum, maximum);

	public static RedisQueuePendingMessageInfo[] GetPendingMessages(this IDatabase database, string key, string group, string consumer, TimeSpan idle, int count = 100, string minimum = "-", string maximum = "+") =>
		GetPendingMessages(database, key, group, consumer, idle > TimeSpan.Zero ? (long)idle.TotalMilliseconds : 0L, count, minimum, maximum);

	private static RedisQueuePendingMessageInfo[] GetPendingMessages(this IDatabase database, string key, string group, string consumer, long idle = 0, int count = 100, string minimum = "-", string maximum = "+")
	{
		if(database == null)
			throw new ArgumentNullException(nameof(database));
		if(string.IsNullOrEmpty(key))
			throw new ArgumentNullException(nameof(key));
		if(string.IsNullOrEmpty(group))
			throw new ArgumentNullException(nameof(group));

		if(count < 1)
			count = 100;
		if(string.IsNullOrEmpty(minimum))
			minimum = "-";
		if(string.IsNullOrEmpty(maximum))
			maximum = "+";

		object[] args;

		if(idle > 0)
			args = string.IsNullOrEmpty(consumer) ?
				[key, group, "IDLE", idle, minimum, maximum, count] :
				[key, group, "IDLE", idle, minimum, maximum, count, consumer];
		else
			args = string.IsNullOrEmpty(consumer) ?
				[key, group, minimum, maximum, count] :
				[key, group, minimum, maximum, count, consumer];

		var result = database.Execute(@"XPENDING", args);

		if(result == null || result.IsNull || result.Resp2Type != ResultType.Array)
			return [];

		var infos = (RedisResult[])result;
		if(infos == null || infos.Length == 0)
			return [];

		var pendings = new RedisQueuePendingMessageInfo[infos.Length];

		for(int i = 0; i < infos.Length; i++)
		{
			var values = (RedisValue[])infos[i];

			if(values == null || values.Length == 0)
				continue;

			switch(values.Length)
			{
				case 1:
					pendings[i] = new RedisQueuePendingMessageInfo(values[0], RedisValue.Null);
					break;
				case 2:
					pendings[i] = new RedisQueuePendingMessageInfo(values[0], values[1]);
					break;
				case 3:
					pendings[i] = new RedisQueuePendingMessageInfo(values[0], values[1], (long)values[2]);
					break;
				case 4:
					pendings[i] = new RedisQueuePendingMessageInfo(values[0], values[1], (long)values[2], (int)values[3]);
					break;
			}
		}

		return pendings;
	}

	internal static RedisValue GetValue(this StreamEntry entry, string name) => GetValue(entry.Values, name);
	internal static RedisValue GetValue(this NameValueEntry[] values, string name)
	{
		if(values == null || values.Length == 0 || name == null)
			return RedisValue.Null;

		for(int i = 0; i < values.Length; i++)
		{
			if(string.Equals(values[i].Name, name))
				return values[i].Value;
		}

		return RedisValue.Null;
	}

	internal static IEnumerable<RedisKey> Scan(this IServer server, int database, string pattern)
	{
		var cursor = 0L;
		var offset = 0;

		do
		{
			var keys = server.Keys(database, pattern, cursor: cursor, pageOffset: offset);

			foreach(var key in keys)
				yield return key;

			var scanning = (IScanningCursor)keys;
			cursor = scanning.Cursor;
			offset = scanning.PageOffset;
		} while(cursor != 0);
	}

	internal static async IAsyncEnumerable<RedisKey> ScanAsync(this IServer server, int database, string pattern)
	{
		var cursor = 0L;
		var offset = 0;

		do
		{
			var keys = server.KeysAsync(database, pattern, cursor: cursor, pageOffset: offset);

			await foreach(var key in keys)
				yield return key;

			var scanning = (IScanningCursor)keys;
			cursor = scanning.Cursor;
			offset = scanning.PageOffset;
		} while(cursor != 0);
	}
}

public readonly struct RedisQueuePendingMessageInfo
{
	public RedisQueuePendingMessageInfo(RedisValue messageId, RedisValue consumer, long idleTimeInMs = 0L, int deliveryCount = 0)
	{
		this.MessageId = messageId;
		this.Consumer = consumer;
		this.IdledDuration = idleTimeInMs > 0 ? TimeSpan.FromMilliseconds(idleTimeInMs) : TimeSpan.Zero;
		this.DeliveryCount = deliveryCount;
	}

	public RedisValue MessageId { get; }
	public RedisValue Consumer { get; }
	public TimeSpan IdledDuration { get; }
	public int DeliveryCount { get; }
}