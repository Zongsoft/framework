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
 * Copyright (C) 2010-2024 Zongsoft Studio <http://www.zongsoft.com>
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

namespace Zongsoft.Serialization.Json;

public class ObjectConverter : JsonConverter<object>
{
	public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if(reader.TokenType == JsonTokenType.Null)
			return null;
		if(reader.TokenType == JsonTokenType.String)
			return reader.GetString();
		if(reader.TokenType == JsonTokenType.True || reader.TokenType == JsonTokenType.False)
			return reader.GetBoolean();
		if(reader.TokenType == JsonTokenType.Number)
			return reader.TryGetInt32(out var integer) ? integer : reader.GetDouble();

		if(reader.TokenType == JsonTokenType.StartObject)
		{
			Type type = null;
			object value = null;
			var depth = reader.CurrentDepth;

			while(reader.Read() && reader.CurrentDepth > depth)
			{
				if(reader.TokenType == JsonTokenType.PropertyName)
				{
					var name = reader.GetString();

					if(name == "$type")
					{
						if(reader.Read())
						{
							type = Common.TypeAlias.Parse(reader.GetString());
							if(type == null)
								return null;
						}
					}
					else if(name == "value")
					{
						if(reader.Read())
							value = GetValue(ref reader, type);
					}
				}
			}

			return value;
		}

		return JsonSerializer.Deserialize(ref reader, typeToConvert, options);
	}

	public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
	{
		if(value == null || System.Convert.IsDBNull(value))
			writer.WriteNullValue();
		else
		{
			var type = Common.TypeExtension.IsNullable(value.GetType(), out var underlyingType) ? underlyingType : value.GetType();

			switch(Type.GetTypeCode(type))
			{
				case TypeCode.String:
					writer.WriteStringValue((string)value);
					break;
				case TypeCode.Boolean:
					writer.WriteBooleanValue((bool)value);
					break;
				default:
					writer.WriteStartObject();

					writer.WritePropertyName("$type");
					writer.WriteStringValue(GetTypeName(type));
					writer.WritePropertyName("value");
					JsonSerializer.Serialize(writer, value);

					writer.WriteEndObject();

					break;
			}
		}
	}

	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
	private static string GetTypeName(Type type) => Common.TypeAlias.GetAlias(Data.Model.GetModelType(type));
	private static object GetValue(ref Utf8JsonReader reader, Type type)
	{
		switch(Type.GetTypeCode(type))
		{
			case TypeCode.Byte:
				return reader.TryGetByte(out var vByte) ? vByte : (byte)0;
			case TypeCode.SByte:
				return reader.TryGetSByte(out var vSByte) ? vSByte : (sbyte)0;
			case TypeCode.Int16:
				return reader.TryGetInt16(out var vInt16) ? vInt16 : (short)0;
			case TypeCode.Int32:
				return reader.TryGetInt32(out var vInt32) ? vInt32 : 0;
			case TypeCode.Int64:
				return reader.TryGetInt64(out var vInt64) ? vInt64 : 0L;
			case TypeCode.UInt16:
				return reader.TryGetUInt16(out var vUInt16) ? vUInt16 : (ushort)0;
			case TypeCode.UInt32:
				return reader.TryGetUInt32(out var vUInt32) ? vUInt32 : 0U;
			case TypeCode.UInt64:
				return reader.TryGetUInt64(out var vUInt64) ? vUInt64 : 0UL;
			case TypeCode.Single:
				return reader.TryGetSingle(out var vSingle) ? vSingle : 0f;
			case TypeCode.Double:
				return reader.TryGetDouble(out var vDouble) ? vDouble : 0d;
			case TypeCode.Decimal:
				return reader.TryGetDecimal(out var vDecimal) ? vDecimal : 0m;
			case TypeCode.DateTime:
				return reader.TryGetDateTime(out var datetime) ? datetime : DateTime.MinValue;
			case TypeCode.Char:
				var text = reader.GetString();
				return string.IsNullOrEmpty(text) ? '\0' : text[0];
			default:
				if(type == typeof(DateTimeOffset))
					return reader.TryGetDateTimeOffset(out var dateTimeOffset) ? dateTimeOffset : DateTimeOffset.MinValue;
				else if(type == typeof(Guid))
					return reader.TryGetGuid(out var guid) ? guid : Guid.Empty;
				else if(type == typeof(byte[]))
					return reader.TryGetBytesFromBase64(out var bytes) ? bytes : null;
				else if(type == typeof(ReadOnlyMemory<byte>))
					return reader.TryGetBytesFromBase64(out var buffer) ? new ReadOnlyMemory<byte>(buffer) : ReadOnlyMemory<byte>.Empty;

				return JsonSerializer.Deserialize(ref reader, type, Serialization.SerializerExtension.GetOptions());
		}
	}
}