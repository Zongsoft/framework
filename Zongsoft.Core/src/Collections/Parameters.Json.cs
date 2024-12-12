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

namespace Zongsoft.Collections
{
	[JsonConverter(typeof(ParametersConverter))]
	partial class Parameters
	{
		private class ParametersConverter : JsonConverter<Parameters>
		{
			#region 公共方法
			public override Parameters Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
			{
				if(reader.TokenType != JsonTokenType.StartObject)
					return null;

				var root = reader.CurrentDepth;
				var parameters = new Parameters();

				Type type = null;
				string name = null;

				while(reader.Read() && reader.CurrentDepth > root)
				{
					switch(reader.TokenType)
					{
						case JsonTokenType.PropertyName:
							var key = reader.GetString();

							if(key != null && key.Length > 1 && key[0] == '$')
							{
								name = null;
								type = Common.TypeAlias.Parse(key[1..]);
							}
							else
							{
								name = key;
								type = null;
							}
							break;
						case JsonTokenType.Null:
							if(type == null)
								parameters.SetValue(name, null);
							else
								parameters.SetValue(type, null);
							break;
						case JsonTokenType.True:
						case JsonTokenType.False:
							if(type == null)
								parameters.SetValue(name, reader.GetBoolean());
							else
								parameters.SetValue(type, reader.GetBoolean());
							break;
						case JsonTokenType.Number:
							if(type == null)
								parameters.SetValue(name, reader.TryGetInt32(out var integer) ? integer : reader.GetDouble());
							else
								parameters.SetValue(type, reader.TryGetInt32(out var integer) ? integer : reader.GetDouble());
							break;
						case JsonTokenType.String:
							if(type == null)
								parameters.SetValue(name, reader.GetString());
							else
								parameters.SetValue(type, reader.GetString());
							break;
						case JsonTokenType.StartObject:
							if(type == null)
								parameters.SetValue(name, GetParameterValue(ref reader, options));
							else
								parameters.SetValue(type, GetParameterValue(ref reader, options));
							break;
					}

					if(reader.CurrentDepth > root + 1)
						reader.Skip();
				}

				return parameters;
			}

			public override void Write(Utf8JsonWriter writer, Parameters parameters, JsonSerializerOptions options)
			{
				writer.WriteStartObject();

				foreach(var parameter in parameters)
				{
					if(parameter.Key is Type type)
						writer.WritePropertyName($"${GetTypeName(type)}");
					else
						writer.WritePropertyName(parameter.Key.ToString());

					Serialization.Json.ObjectConverter.Default.Write(writer, parameter.Value, options);
				}

				writer.WriteEndObject();
			}
			#endregion

			#region 私有方法
			[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
			private static string GetTypeName(Type type) => Common.TypeAlias.GetAlias(Data.Model.GetModelType(type));
			private static object GetParameterValue(ref Utf8JsonReader reader, JsonSerializerOptions options)
			{
				Type type = null;
				var root = reader.CurrentDepth;

				while(reader.Read() && reader.CurrentDepth > root)
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
										else if(type == typeof(DateOnly))
											return reader.TokenType == JsonTokenType.Number ? DateOnly.FromDayNumber(reader.GetInt32()) : DateOnly.Parse(reader.GetString());
										else if(type == typeof(TimeOnly))
											return reader.TokenType == JsonTokenType.Number ? new TimeOnly(reader.GetInt64()) : TimeOnly.Parse(reader.GetString());
										else if(type == typeof(Guid))
											return reader.TryGetGuid(out var guid) ? guid : Guid.Empty;
										else if(type == typeof(byte[]))
											return reader.TryGetBytesFromBase64(out var bytes) ? bytes : null;
										else if(type == typeof(ReadOnlyMemory<byte>))
											return reader.TryGetBytesFromBase64(out var buffer) ? new ReadOnlyMemory<byte>(buffer) : ReadOnlyMemory<byte>.Empty;

										return JsonSerializer.Deserialize(ref reader, type, options);
								}
							}
						}
					}
				}

				return null;
			}
			#endregion
		}
	}
}
