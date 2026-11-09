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

public class RangeConverterFactory : JsonConverterFactory
{
	public override bool CanConvert(Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Data.Range<>);
	public override JsonConverter CreateConverter(Type type, JsonSerializerOptions options) => (JsonConverter)Activator.CreateInstance(typeof(RangeConverter<>).MakeGenericType(new Type[] { type }));

	private class RangeConverter<T> : JsonConverter<T> where T : struct
	{
		public override T Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
		{
			switch(reader.TokenType)
			{
				case JsonTokenType.Number:
					return (T)Data.Range.Create(type, this.GetValue(ref reader, type.GenericTypeArguments[0]));
				case JsonTokenType.String:
					return (T)Common.Convert.ConvertValue(reader.GetString(), type);
				case JsonTokenType.StartObject:
					object minimum = null, maximum = null;

					while(reader.Read())
					{
						if(reader.TokenType == JsonTokenType.EndObject)
							break;

						if(reader.TokenType != JsonTokenType.PropertyName)
							throw new JsonException();

						var name = reader.GetString();

						if(string.Equals(name, "minimum", StringComparison.OrdinalIgnoreCase))
							minimum = this.GetValue(ref reader, type.GenericTypeArguments[0]);
						else if(string.Equals(name, "maximum", StringComparison.OrdinalIgnoreCase))
							maximum = this.GetValue(ref reader, type.GenericTypeArguments[0]);
					}

					return (T)Data.Range.Create(type, minimum, maximum);
			}

			return default;
		}

		public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
		{
			writer.WriteStringValue(value.ToString());
		}

		private object GetValue(ref Utf8JsonReader reader, Type type)
		{
			if(reader.TokenType == JsonTokenType.PropertyName)
				reader.Read();

			switch(reader.TokenType)
			{
				case JsonTokenType.None:
				case JsonTokenType.Null:
					return null;
				case JsonTokenType.True:
					return true;
				case JsonTokenType.False:
					return false;
				case JsonTokenType.String:
					return reader.GetString();
				case JsonTokenType.Number:
					return Type.GetTypeCode(type) switch
					{
						TypeCode.Byte => reader.GetByte(),
						TypeCode.SByte => reader.GetSByte(),
						TypeCode.Int16 => reader.GetInt16(),
						TypeCode.UInt16 => reader.GetUInt16(),
						TypeCode.Int32 => reader.GetInt32(),
						TypeCode.UInt32 => reader.GetUInt32(),
						TypeCode.Int64 => reader.GetInt64(),
						TypeCode.UInt64 => reader.GetUInt64(),
						TypeCode.Single => reader.GetSingle(),
						TypeCode.Double => reader.GetDouble(),
						TypeCode.Decimal => reader.GetDecimal(),
						_ => throw new JsonException(),
					};
				default:
					throw new JsonException();
			}
		}
	}
}
