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
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
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
					return (T)Data.Range.Create(type, GetValue(ref reader, type.GenericTypeArguments[0]));
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
							minimum = GetValue(ref reader, type.GenericTypeArguments[0]);
						else if(string.Equals(name, "maximum", StringComparison.OrdinalIgnoreCase))
							maximum = GetValue(ref reader, type.GenericTypeArguments[0]);
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
