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
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Zongsoft.Serialization.Json;

public class DictionaryConverterFactory : JsonConverterFactory
{
	public override bool CanConvert(Type type) => typeof(IDictionary).IsAssignableFrom(type) || Zongsoft.Common.TypeExtension.IsAssignableFrom(typeof(IDictionary<,>), type);
	public override JsonConverter CreateConverter(Type type, JsonSerializerOptions options)
	{
		if(Zongsoft.Common.TypeExtension.IsAssignableFrom(typeof(IDictionary<,>), type, out var genericTypes))
			return (JsonConverter)Activator.CreateInstance(
				typeof(GenericDictionaryConverter<,>).MakeGenericType([genericTypes[0].GenericTypeArguments[0], genericTypes[0].GenericTypeArguments[1]]));

		return ClassicDictionaryConverter.Instance;
	}

	private class ClassicDictionaryConverter : JsonConverter<IDictionary>
	{
		public static readonly ClassicDictionaryConverter Instance = new();

		public override IDictionary Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
		{
			string key = null;
			IDictionary dictionary = type.IsAbstract ? new Dictionary<string, object>() : (IDictionary)Activator.CreateInstance(type);

			while(reader.Read())
			{
				switch(reader.TokenType)
				{
					case JsonTokenType.Null:
						dictionary[key] = null;
						break;
					case JsonTokenType.True:
					case JsonTokenType.False:
						dictionary[key] = reader.GetBoolean();
						break;
					case JsonTokenType.String:
						dictionary[key] = reader.GetString();
						break;
					case JsonTokenType.PropertyName:
						key = reader.GetString();
						break;
					case JsonTokenType.StartObject:
						dictionary[key] = ObjectConverter.Default.Read(ref reader, typeof(object), options);
						break;
				}
			}

			return dictionary;
		}

		public override void Write(Utf8JsonWriter writer, IDictionary dictionary, JsonSerializerOptions options)
		{
			if(dictionary == null)
			{
				writer.WriteNullValue();
				return;
			}

			writer.WriteStartObject();
			foreach(DictionaryEntry entry in dictionary)
			{
				writer.WritePropertyName(entry.Key.ToString());
				ObjectConverter.Default.Write(writer, entry.Value, options);
			}
			writer.WriteEndObject();
		}
	}

	private class GenericDictionaryConverter<TKey, TValue> : JsonConverter<IDictionary<TKey, TValue>>
	{
		private delegate TValue Reader(ref Utf8JsonReader reader, JsonSerializerOptions options);

		private readonly Reader _reader = typeof(TValue) == typeof(object) ?
			(ref Utf8JsonReader reader, JsonSerializerOptions options) => (TValue)ObjectConverter.Default.Read(ref reader, typeof(TValue), options) :
			JsonSerializer.Deserialize<TValue>;

		public override IDictionary<TKey, TValue> Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
		{
			TKey key = default;
			var dictionary = new Dictionary<TKey, TValue>();

			while(reader.Read())
			{
				switch(reader.TokenType)
				{
					case JsonTokenType.Null:
						dictionary[key] = default;
						break;
					case JsonTokenType.True:
					case JsonTokenType.False:
						dictionary[key] = Common.Convert.ConvertValue(reader.GetBoolean(), default(TValue));
						break;
					case JsonTokenType.String:
						dictionary[key] = Common.Convert.ConvertValue(reader.GetString(), default(TValue));
						break;
					case JsonTokenType.PropertyName:
						key = Common.Convert.ConvertValue(reader.GetString(), default(TKey));
						break;
					case JsonTokenType.StartObject:
						dictionary[key] = _reader(ref reader, options);
						break;
				}
			}

			return dictionary;
		}

		public override void Write(Utf8JsonWriter writer, IDictionary<TKey, TValue> dictionary, JsonSerializerOptions options)
		{
			if(dictionary == null)
			{
				writer.WriteNullValue();
				return;
			}

			writer.WriteStartObject();
			foreach(var entry in dictionary)
			{
				writer.WritePropertyName(entry.Key.ToString());
				ObjectConverter.Default.Write(writer, entry.Value, options);
			}
			writer.WriteEndObject();
		}
	}
}
