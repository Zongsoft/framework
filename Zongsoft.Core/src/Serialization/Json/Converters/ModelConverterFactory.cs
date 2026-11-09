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
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Zongsoft.Serialization.Json.Converters;

public class ModelConverterFactory : JsonConverterFactory
{
	private static readonly ConcurrentDictionary<Type, Type> _mapping = new();
	private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _properties = new();

	public override JsonConverter CreateConverter(Type type, JsonSerializerOptions options) => (JsonConverter)Activator.CreateInstance(typeof(ModelConverter<>).MakeGenericType(type));
	public override bool CanConvert(Type type)
	{
		if(Attribute.IsDefined(type, typeof(JsonConverterAttribute), true))
			return false;
		if(Attribute.IsDefined(type, typeof(JsonDerivedTypeAttribute), true))
			return false;
		if(Attribute.IsDefined(type, typeof(JsonPolymorphicAttribute), true))
			return false;

		return (type.IsInterface || type.IsAbstract) && !Common.TypeExtension.IsEnumerable(type);
	}

	private class ModelConverter<T> : JsonConverter<T> where T : class
	{
		public override T Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
		{
			var actualType = _mapping.GetOrAdd(type, key => Data.Model.Build<T>().GetType());
			return (T)JsonSerializer.Deserialize(ref reader, actualType, options);
		}

		public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
		{
			if(value == null)
			{
				writer.WriteNullValue();
				return;
			}

			if(value is Data.IModel model)
			{
				var changes = model.GetChanges();
				var properties = GetProperties(value, options);

				if(changes == null)
					WriteModel(writer, properties, options);
				else
					WriteModel(writer, changes.Concat(properties), options);
			}
			else
				JsonSerializer.Serialize(writer, value, typeof(object), options);

			static void WriteModel(Utf8JsonWriter writer, IEnumerable<KeyValuePair<string, object>> properties, JsonSerializerOptions options)
			{
				if(properties == null)
				{
					writer.WriteNullValue();
					return;
				}

				writer.WriteStartObject();

				foreach(var property in properties)
				{
					writer.WritePropertyName(property.Key, options);

					if(property.Value == null || Convert.IsDBNull(property.Value))
						writer.WriteNullValue();
					else
						writer.WriteObject(property.Value, options);
				}

				writer.WriteEndObject();
			}
		}

		private static PropertyInfo[] GetProperties(Type type)
		{
			if(type.IsInterface)
				return [];

			return _properties.GetOrAdd(type, key =>
				key.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
				.Where(property => property.CanRead && !property.GetMethod.IsAbstract && property.GetIndexParameters().Length == 0)
				.ToArray());
		}

		private static IEnumerable<KeyValuePair<string, object>> GetProperties(T model, JsonSerializerOptions options)
		{
			var properties = GetProperties(Data.Model.GetModelType(model));

			for(int i = 0; i < properties.Length; i++)
			{
				var value = Zongsoft.Reflection.Reflector.GetValue(properties[i], ref model);

				if(value != null || (options.DefaultIgnoreCondition & JsonIgnoreCondition.WhenWritingNull) != JsonIgnoreCondition.WhenWritingNull)
					yield return new KeyValuePair<string, object>(properties[i].Name, value);
			}
		}
	}
}