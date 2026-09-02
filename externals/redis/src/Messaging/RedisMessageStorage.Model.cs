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
using System.IO;
using System.Text.Json;

using Zongsoft.Messaging;

namespace Zongsoft.Externals.Redis.Messaging;

partial class RedisMessageStorage
{
	internal sealed class MessageModel
	{
		#region 常量定义
		internal const int VERSION = 1;
		#endregion

		#region 静态字段
		private static readonly JsonSerializerOptions _options = new()
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		};
		#endregion

		#region 公共属性
		public int Version { get; set; } = VERSION;
		public string Identity { get; set; }
		public string Identifier { get; set; }
		public string Topic { get; set; }
		public byte[] Data { get; set; }
		public string Tags { get; set; }
		public DateTime Timestamp { get; set; }
		#endregion

		#region 公共方法
		public static MessageModel Create(in Message message) => new()
		{
			Identifier = message.Identifier,
			Topic = message.Topic ?? string.Empty,
			Identity = message.Identity,
			Tags = message.Tags,
			Timestamp = Normalize(message.Timestamp),
			Data = message.Data == null ? null : (byte[])message.Data.Clone(),
		};

		public static byte[] Serialize(in Message message) => JsonSerializer.SerializeToUtf8Bytes(Create(message), _options);

		public static MessageModel Deserialize(ReadOnlySpan<byte> data)
		{
			if(data.IsEmpty)
				throw new InvalidDataException(Properties.Resources.RedisMessageStorageRecordEmpty_Message);

			MessageModel snapshot;
			try
			{
				snapshot = JsonSerializer.Deserialize<MessageModel>(data, _options);
			}
			catch(JsonException exception)
			{
				throw new InvalidDataException(Properties.Resources.RedisMessageStorageRecordMalformed_Message, exception);
			}

			if(snapshot == null)
				throw new InvalidDataException(Properties.Resources.RedisMessageStorageRecordMalformed_Message);
			if(snapshot.Version != VERSION)
				throw new InvalidDataException(string.Format(Properties.Resources.RedisMessageStorageRecordVersionUnsupported_Message, snapshot.Version));
			if(string.IsNullOrWhiteSpace(snapshot.Identifier))
				throw new InvalidDataException(Properties.Resources.RedisMessageStorageRecordIdentifierMissing_Message);

			snapshot.Topic ??= string.Empty;
			snapshot.Timestamp = Normalize(snapshot.Timestamp);
			return snapshot;
		}

		public Message ToMessage() => new(this.Identifier, this.Topic, this.Data == null ? null : (byte[])this.Data.Clone())
		{
			Identity = this.Identity,
			Tags = this.Tags,
			Timestamp = Normalize(this.Timestamp),
		};
		#endregion

		#region 重写方法
		public override string ToString() => string.IsNullOrEmpty(this.Tags) ?
			$"[{this.Identifier}@{this.Timestamp}]{this.Topic}" :
			$"[{this.Identifier}@{this.Timestamp}]{this.Topic}({this.Tags})";
		#endregion

		#region 私有方法
		private static DateTime Normalize(DateTime timestamp) => timestamp.Kind switch
		{
			DateTimeKind.Utc => timestamp,
			DateTimeKind.Local => timestamp.ToUniversalTime(),
			_ => DateTime.SpecifyKind(timestamp, DateTimeKind.Utc),
		};
		#endregion
	}
}
