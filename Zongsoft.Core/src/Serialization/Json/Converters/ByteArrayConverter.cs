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
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Zongsoft.Serialization.Json.Converters;

public class ByteArrayConverter : JsonConverter<byte[]>
{
	public override byte[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if(reader.TokenType == JsonTokenType.Null)
			return null;

		if(reader.TokenType == JsonTokenType.String)
			return reader.GetBytesFromBase64();

		if(reader.TokenType == JsonTokenType.StartArray)
		{
			var list = new System.Collections.Generic.List<byte>();

			while(reader.Read() && reader.TokenType != JsonTokenType.EndArray)
			{
				list.Add(reader.GetByte());
			}

			return list.ToArray();
		}

		return null;
	}

	public override void Write(Utf8JsonWriter writer, byte[] value, JsonSerializerOptions options)
	{
		if(value == null)
			writer.WriteNullValue();
		else
		{
			writer.WriteStartArray();

			for(int i = 0; i < value.Length; i++)
				writer.WriteNumberValue(value[i]);

			writer.WriteEndArray();
		}
	}
}
