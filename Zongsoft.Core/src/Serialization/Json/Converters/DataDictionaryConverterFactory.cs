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
using System.Collections.Generic;

using Zongsoft.Data;

namespace Zongsoft.Serialization.Json.Converters;

public class DataDictionaryConverterFactory : JsonConverterFactory
{
	public override bool CanConvert(Type type) => typeof(IDataDictionary).IsAssignableFrom(type);
	public override JsonConverter CreateConverter(Type type, JsonSerializerOptions options) => new DataDictionaryConverter();

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
