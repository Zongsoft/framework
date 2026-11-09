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

namespace Zongsoft.Serialization;

partial class TextSerializationOptions
{
	private JsonSerializerOptions _jsonOptions = null;

	public JsonSerializerOptions JsonOptions
	{
		get
		{
			if(_jsonOptions != null)
				return _jsonOptions;

			lock(this)
			{
				return _jsonOptions ??= this.ToJsonOptions();
			}
		}
	}

	private JsonSerializerOptions ToJsonOptions()
	{
		JsonNamingPolicy naming = _naming switch
		{
			SerializationNamingConvention.None => null,
			SerializationNamingConvention.Camel => JsonNamingPolicy.CamelCase,
			SerializationNamingConvention.Kebab => JsonNamingPolicy.KebabCaseLower,
			SerializationNamingConvention.Snake => JsonNamingPolicy.SnakeCaseLower,
			SerializationNamingConvention.Pascal => Json.NamingConvention.Pascal,
			_ => null,
		};

		var ignores = JsonIgnoreCondition.Never;

		if(this.IgnoreNull)
			ignores = JsonIgnoreCondition.WhenWritingNull;
		else if(this.IgnoreZero)
			ignores = JsonIgnoreCondition.WhenWritingDefault;

		var result = new JsonSerializerOptions()
		{
			Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
			PropertyNameCaseInsensitive = true,
			MaxDepth = this.MaximumDepth,
			WriteIndented = this.Indented,
			NumberHandling = JsonNumberHandling.AllowReadingFromString,
			DefaultIgnoreCondition = ignores,
			IgnoreReadOnlyProperties = false,
			PropertyNamingPolicy = naming,
			DictionaryKeyPolicy = naming,
			IncludeFields = this.IncludeFields,
			Converters =
			{
				Json.Converters.TypeConverter.Factory,
				Json.Converters.DateOnlyConverter.Instance,
				Json.Converters.TimeOnlyConverter.Instance,
				Json.Converters.TimeSpanConverter.Instance,
				new JsonStringEnumConverter(naming),
				new Json.Converters.ModelConverterFactory(),
				new Json.Converters.RangeConverterFactory(),
				new Json.Converters.MixtureConverterFactory(),
				new Json.Converters.DataDictionaryConverterFactory(),
				new Json.Converters.DictionaryConverterFactory(this),
			},
		};

		//设置全局转换器
		FillConverters(result);

		if(_typified)
			result.Converters.Add(Json.Converters.ObjectConverter.Factory);

		#if NET8_0_OR_GREATER
		if(this.Immutable)
			result.MakeReadOnly(true);
		#endif

		//进行选项配置
		this.Configure?.Invoke(result);

		return result;

		static void FillConverters(JsonSerializerOptions options)
		{
			foreach(var value in Serializer.Json.Options.Converters)
			{
				if(value is JsonConverter converter && !options.Converters.Contains(converter))
					options.Converters.Add(converter);
			}
		}
	}
}

internal static class TextSerializationOptionsUtility
{
	internal static JsonSerializerOptions ToOptions(this TextSerializationOptions options) => (options ?? TextSerializationOptions.Default).JsonOptions;
}