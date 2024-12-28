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
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Zongsoft.Serialization.Json.Converters;

public class MixtureConverterFactory : JsonConverterFactory
{
	public override bool CanConvert(Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Data.Mixture<>);
	public override JsonConverter CreateConverter(Type type, JsonSerializerOptions options)
	{
		var converterType = typeof(MixtureConverter<>).MakeGenericType(type.GenericTypeArguments);
		return (JsonConverter)Activator.CreateInstance(converterType);
	}

	private class MixtureConverter<T> : JsonConverter<Data.Mixture<T>> where T : struct, IEquatable<T>, IComparable<T>
	{
		private static readonly Common.StringExtension.TryParser<T> _parser = Type.GetTypeCode(typeof(T)) switch
		{
			TypeCode.Byte => (Common.StringExtension.TryParser<T>)(object)(Common.StringExtension.TryParser<byte>)byte.TryParse,
			TypeCode.SByte => (Common.StringExtension.TryParser<T>)(object)(Common.StringExtension.TryParser<sbyte>)sbyte.TryParse,
			TypeCode.Int16 => (Common.StringExtension.TryParser<T>)(object)(Common.StringExtension.TryParser<short>)short.TryParse,
			TypeCode.UInt16 => (Common.StringExtension.TryParser<T>)(object)(Common.StringExtension.TryParser<ushort>)ushort.TryParse,
			TypeCode.Int32 => (Common.StringExtension.TryParser<T>)(object)(Common.StringExtension.TryParser<int>)int.TryParse,
			TypeCode.UInt32 => (Common.StringExtension.TryParser<T>)(object)(Common.StringExtension.TryParser<uint>)uint.TryParse,
			TypeCode.Int64 => (Common.StringExtension.TryParser<T>)(object)(Common.StringExtension.TryParser<long>)long.TryParse,
			TypeCode.UInt64 => (Common.StringExtension.TryParser<T>)(object)(Common.StringExtension.TryParser<ulong>)ulong.TryParse,
			TypeCode.Single => (Common.StringExtension.TryParser<T>)(object)(Common.StringExtension.TryParser<float>)float.TryParse,
			TypeCode.Double => (Common.StringExtension.TryParser<T>)(object)(Common.StringExtension.TryParser<double>)double.TryParse,
			TypeCode.Decimal => (Common.StringExtension.TryParser<T>)(object)(Common.StringExtension.TryParser<decimal>)decimal.TryParse,
			TypeCode.Boolean => (Common.StringExtension.TryParser<T>)(object)(Common.StringExtension.TryParser<bool>)bool.TryParse,
			TypeCode.DateTime => (Common.StringExtension.TryParser<T>)(object)(Common.StringExtension.TryParser<DateTime>)DateTime.TryParse,
			_ => null,
		};

		public override Data.Mixture<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if(reader.TokenType == JsonTokenType.Number)
				return new Data.Mixture<T>(reader.GetValue<T>());

			if(reader.TokenType == JsonTokenType.String)
			{
				var parts = reader.GetString()?.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

				if(parts == null || parts.Length == 0)
					return default;

				if(parts.Length == 1)
				{
					if(_parser(parts[0], out var value))
						return new Data.Mixture<T>(value);

					if(Data.Range.TryParse<T>(parts[0], out var range))
						return new Data.Mixture<T>(range);

					throw new JsonException();
				}

				var array = new T[parts.Length];

				for(int i = 0; i < parts.Length; i++)
				{
					if(_parser(parts[i], out var value))
						array[i] = value;
					else
						throw new JsonException();
				}

				return new Data.Mixture<T>(array);
			}

			if(reader.TokenType == JsonTokenType.StartArray)
			{
				var list = new List<T>();

				while(reader.Read())
				{
					if(reader.TokenType == JsonTokenType.EndArray)
						break;

					if(reader.TokenType == JsonTokenType.Number)
						list.Add(reader.GetValue<T>());
					else
						throw new JsonException();
				}

				return new Data.Mixture<T>(list.ToArray());
			}

			throw new JsonException();
		}

		public override void Write(Utf8JsonWriter writer, Data.Mixture<T> value, JsonSerializerOptions options)
		{
			writer.WriteStringValue(value.ToString());
		}
	}
}
