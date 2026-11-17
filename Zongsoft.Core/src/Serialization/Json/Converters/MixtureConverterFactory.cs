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

	private class MixtureConverter<T> : JsonConverter<Data.Mixture<T>> where T : struct
	{
		public override Data.Mixture<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if(reader.TokenType == JsonTokenType.Number)
				return new Data.Mixture<T>(ReadValue(ref reader, options));

			if(reader.TokenType == JsonTokenType.String)
			{
				var text = reader.GetString();

				if(Data.Mixture<T>.TryParse(text, out var result))
					return result;

				if(text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Length == 0 || Data.Range<T>.TryParse(text, out var range) && range.IsEmpty)
					return default;

				throw new JsonException();
			}

			if(reader.TokenType == JsonTokenType.StartArray)
			{
				var list = new List<T>();

				while(reader.Read())
				{
					if(reader.TokenType == JsonTokenType.EndArray)
						break;

					if(reader.TokenType is JsonTokenType.Number or JsonTokenType.String)
						list.Add(ReadValue(ref reader, options));
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

		private static T ReadValue(ref Utf8JsonReader reader, JsonSerializerOptions options)
		{
			if(reader.TokenType == JsonTokenType.String)
				return Common.Convert.TryConvertValue(reader.GetString(), out T value) ? value : throw new JsonException();

			return JsonSerializer.Deserialize<T>(ref reader, options);
		}
	}
}
