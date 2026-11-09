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

public class TimeOnlyConverter : JsonConverter<TimeOnly>
{
	public static readonly TimeOnlyConverter Instance = new();

	public override TimeOnly Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) => reader.TokenType switch
	{
		JsonTokenType.Number => new TimeOnly(reader.GetInt64()),
		JsonTokenType.String => TimeOnly.Parse(reader.GetString()),
		_ => throw new JsonException(string.Format(Properties.Resources.Serialization_JsonNodeConversion_Message, reader.TokenType, nameof(TimeOnly))),
	};

	public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options) => writer.WriteStringValue(value.ToString("HH:mm:ss.fffffff"));
}
