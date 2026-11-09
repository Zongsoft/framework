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
using System.Reflection;
using System.Linq.Expressions;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Zongsoft.Serialization.Json;

internal static class JsonConverterInvoker
{
	private static readonly ConcurrentDictionary<Type, ConverterInvoker> _converters = new();

	public static object Read(this JsonConverter converter, ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
	{
		if(converter == null)
			throw new ArgumentNullException(nameof(converter));

		var invoker = _converters.GetOrAdd(converter.GetType(), Compile);
		return invoker.Read(converter, ref reader, type, options);
	}

	public static void Write(this JsonConverter converter, Utf8JsonWriter writer, object value, JsonSerializerOptions options)
	{
		if(converter == null)
			throw new ArgumentNullException(nameof(converter));

		if(value == null || Convert.IsDBNull(value))
		{
			writer.WriteNullValue();
			return;
		}

		var invoker = _converters.GetOrAdd(converter.GetType(), Compile);
		invoker.Write(converter, writer, value, options);
	}

	private static ConverterInvoker Compile(Type converterType)
	{
		var conversionType = GetConversionType(converterType);
		if(conversionType == null)
			return null;

		(var readMethod, var writeMethod) = GetConverterMethod(converterType);

		var converter = Expression.Parameter(typeof(JsonConverter), "converter");
		var options = Expression.Parameter(typeof(JsonSerializerOptions), "options");
		var convert = Expression.Convert(converter, converterType);

		/*
		 * The dynamic generated Read(...) method:
		 * _________________________________________________________________________________________________________
		 * object Read(JsonConverter converter, ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
		 * {
		 *     return ((XzzzConverter)converter).Read(ref reader, type, options);
		 * }
		 */

		var pReader = Expression.Parameter(typeof(Utf8JsonReader).MakeByRefType(), "reader");
		var pType = Expression.Parameter(typeof(Type), "type");
		var read = Expression.Call(convert, readMethod, [pReader, pType, options]);
		var reader = Expression.Lambda<Reader>(Expression.Convert(read, typeof(object)), converter, pReader, pType, options);

		/*
		 * The dynamic generated Write(...) method:
		 * _______________________________________________________________________________________________________
		 * void Write(JsonConverter converter, Utf8JsonWriter writer, object value, JsonSerializerOptions options)
		 * {
		 *     ((XzzzConverter)converter).Write(writer, (Xzzz)value, options);
		 * }
		 */

		var pWriter = Expression.Parameter(typeof(Utf8JsonWriter), "writer");
		var pValue = Expression.Parameter(typeof(object), "value");
		var write = Expression.Call(convert, writeMethod, pWriter, Expression.Convert(pValue, conversionType), options);
		var writer = Expression.Lambda<Writer>(write, converter, pWriter, pValue, options);

		return new(reader.Compile(), writer.Compile());
	}

	private static Type GetConversionType(Type converterType)
	{
		if(!typeof(JsonConverter).IsAssignableFrom(converterType))
			return null;

		while(converterType != null && converterType != typeof(object))
		{
			if(converterType.IsGenericType)
				return converterType.GenericTypeArguments[0];

			converterType = converterType.BaseType;
		}

		return null;
	}

	private static (MethodInfo reader, MethodInfo writer) GetConverterMethod(Type type) =>
	(
		type.GetMethod(nameof(JsonConverter<object>.Read), BindingFlags.Public | BindingFlags.Instance),
		type.GetMethod(nameof(JsonConverter<object>.Write), BindingFlags.Public | BindingFlags.Instance)
	);

	private delegate object Reader(JsonConverter converter, ref Utf8JsonReader reader, Type type, JsonSerializerOptions options);
	private delegate void Writer(JsonConverter converter, Utf8JsonWriter writer, object value, JsonSerializerOptions options);

	private sealed class ConverterInvoker(Reader reader, Writer writer)
	{
		private readonly Reader _reader = reader;
		private readonly Writer _writer = writer;

		public object Read(JsonConverter converter, ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) => _reader.Invoke(converter, ref reader, type, options);
		public void Write(JsonConverter converter, Utf8JsonWriter writer, object value, JsonSerializerOptions options) => _writer(converter, writer, value, options);
	}
}
