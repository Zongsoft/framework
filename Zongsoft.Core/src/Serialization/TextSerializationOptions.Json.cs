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
		JsonNamingPolicy naming = null;

		switch(_naming)
		{
			case SerializationNamingConvention.Camel:
				naming = JsonNamingPolicy.CamelCase;
				break;
			case SerializationNamingConvention.Pascal:
				naming = Json.NamingConvention.Pascal;
				break;
		}

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

		if(_typified)
			result.Converters.Add(Json.Converters.ObjectConverter.Factory);

		#if NET8_0_OR_GREATER
		if(this.Immutable)
			result.MakeReadOnly(true);
		#endif

		//进行选项配置
		this.Configure?.Invoke(result);

		return result;
	}
}

internal static class TextSerializationOptionsUtility
{
	internal static JsonSerializerOptions ToOptions(this TextSerializationOptions options) => (options ?? TextSerializationOptions.Default).JsonOptions;
}