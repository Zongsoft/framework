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

public class DateOnlyConverter : JsonConverter<DateOnly>
{
	public static readonly DateOnlyConverter Instance = new();

	public override DateOnly Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) => reader.TokenType switch
	{
		JsonTokenType.Number => DateOnly.FromDayNumber(reader.GetInt32()),
		JsonTokenType.String => DateOnly.Parse(reader.GetString()),
		_ => throw new JsonException(string.Format(Properties.Resources.Serialization_JsonNodeConversion_Message, reader.TokenType, nameof(DateOnly))),
	};

	public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options) => writer.WriteStringValue(value.ToString("yyyy-MM-dd"));
}
