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

public class TypeConverter : JsonConverter<Type>
{
	public static readonly JsonConverterFactory Factory = new TypeConverterFactory();

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006")]
	private class TypeConverterFactory : JsonConverterFactory
	{
		private static readonly TypeConverter Default = new();
		public override bool CanConvert(Type type) => typeof(Type).IsAssignableFrom(type);
		public override JsonConverter CreateConverter(Type type, JsonSerializerOptions options) => Default;
	}

	public override Type Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if(reader.TokenType == JsonTokenType.Null)
			return null;

		if(reader.TokenType == JsonTokenType.String)
			return Zongsoft.Common.TypeAlias.Parse(reader.GetString());

		return null;
	}

	public override void Write(Utf8JsonWriter writer, Type value, JsonSerializerOptions options)
	{
		if(value == null)
			writer.WriteNullValue();
		else
			writer.WriteStringValue(Zongsoft.Common.TypeAlias.GetAlias(value));
	}
}
