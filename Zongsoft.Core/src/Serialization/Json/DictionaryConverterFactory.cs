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

public class DictionaryConverterFactory(TextSerializationOptions options) : JsonConverterFactory
{
	private readonly TextSerializationOptions _options = options;

	public override bool CanConvert(Type type) => type == typeof(Hashtable) || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>));
	public override JsonConverter CreateConverter(Type type, JsonSerializerOptions options) => type.IsGenericType ?
		(JsonConverter)Activator.CreateInstance(typeof(GenericDictionaryConverter<,>).MakeGenericType(type.GenericTypeArguments[0], type.GenericTypeArguments[1]), _options) :
		new ClassicDictionaryConverter(_options);

	private class ClassicDictionaryConverter(TextSerializationOptions options) : JsonConverter<IDictionary>
	{
		private readonly TextSerializationOptions _options = options;

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
					case JsonTokenType.Number:
						dictionary[key] = ObjectConverter.GetNumber(ref reader);
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
					case JsonTokenType.StartArray:
						dictionary[key] = ObjectConverter.Default.Read(ref reader, typeof(object[]), options);
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
				writer.WritePropertyName(entry.Key.ToString(), options);

				if(entry.Value == null || Convert.IsDBNull(entry.Value))
					writer.WriteNullValue();
				else if(_options.Typified)
					ObjectConverter.Default.Write(writer, entry.Value, options);
				else
				{
					var converter = options.GetConverter(Data.Model.GetModelType(entry.Value.GetType()));

					if(converter == null)
						ObjectConverter.Default.Write(writer, entry.Value, options);
					else
						converter.Write(writer, entry.Value, options);
				}
			}

			writer.WriteEndObject();
		}
	}

	private class GenericDictionaryConverter<TKey, TValue>(TextSerializationOptions options) : JsonConverter<IDictionary<TKey, TValue>>
	{
		private readonly TextSerializationOptions _options = options;

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
					case JsonTokenType.Number:
						dictionary[key] = Common.Convert.ConvertValue(ObjectConverter.GetNumber(ref reader), default(TValue));
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
					case JsonTokenType.StartArray:
						dictionary[key] = Common.Convert.ConvertValue(ObjectConverter.Default.Read(ref reader, typeof(object[]), options), default(TValue));
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
				writer.WritePropertyName(entry.Key.ToString(), options);

				if(entry.Value == null || Convert.IsDBNull(entry.Value))
					writer.WriteNullValue();
				else if(_options.Typified)
					ObjectConverter.Default.Write(writer, entry.Value, options);
				else
				{
					var converter = options.GetConverter(Data.Model.GetModelType(entry.Value.GetType()));

					if(converter == null)
						ObjectConverter.Default.Write(writer, entry.Value, options);
					else
						converter.Write(writer, entry.Value, options);
				}
			}

			writer.WriteEndObject();
		}
	}
}
