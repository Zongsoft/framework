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
using System.Collections.Generic;

using Zongsoft.Data;

namespace Zongsoft.Serialization.Json.Converters;

public class DataDictionaryConverterFactory : JsonConverterFactory
{
	public override bool CanConvert(Type type) => typeof(IDataDictionary).IsAssignableFrom(type);
	public override JsonConverter CreateConverter(Type type, JsonSerializerOptions options) =>
		Common.TypeExtension.IsAssignableFrom(typeof(IDataDictionary<>), type, out var contracts) ?
		(JsonConverter)Activator.CreateInstance(typeof(DataDictionaryConverter<>).MakeGenericType(contracts[0].GenericTypeArguments)) :
		new DataDictionaryConverter();

	private class DataDictionaryConverter<T> : JsonConverter<IDataDictionary<T>>
	{
		public override IDataDictionary<T> Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
		{
			var data = ObjectConverter.Default.Read(ref reader, typeof(IDictionary<string, object>), options);
			return data == null ? null : DataDictionary.GetDictionary<T>(data);
		}

		public override void Write(Utf8JsonWriter writer, IDataDictionary<T> value, JsonSerializerOptions options)
		{
			if(value == null || value.Data == null)
				writer.WriteNullValue();
			else
				ObjectConverter.Default.Write(writer, value.Data, options);
		}
	}

	private class DataDictionaryConverter : JsonConverter<IDataDictionary>
	{
		public override IDataDictionary Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
		{
			var data = ObjectConverter.Default.Read(ref reader, typeof(IDictionary<string, object>), options);
			return data == null ? null : DataDictionary.GetDictionary(data);
		}

		public override void Write(Utf8JsonWriter writer, IDataDictionary value, JsonSerializerOptions options)
		{
			if(value == null || value.Data == null)
				writer.WriteNullValue();
			else
				ObjectConverter.Default.Write(writer, value.Data, options);
		}
	}
}
