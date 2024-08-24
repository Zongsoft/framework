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
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Zongsoft.Serialization.Json;

public class ModelConverterFactory : JsonConverterFactory
{
	public override bool CanConvert(Type type) => (type.IsInterface || type.IsAbstract) && !Common.TypeExtension.IsEnumerable(type);
	public override JsonConverter CreateConverter(Type type, JsonSerializerOptions options) => (JsonConverter)Activator.CreateInstance(typeof(ModelConverter<>).MakeGenericType(type));

	private class ModelConverter<T> : JsonConverter<T> where T : class
	{
		private static readonly ConcurrentDictionary<Type, Type> _mapping_ = new ConcurrentDictionary<Type, Type>();

		public override T Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
		{
			var actualType = _mapping_.GetOrAdd(type, key => Data.Model.Build<T>().GetType());
			return (T)JsonSerializer.Deserialize(ref reader, actualType, options);
		}

		public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
		{
			if(value == null)
				writer.WriteNullValue();
			else
				JsonSerializer.Serialize(writer, value, typeof(object), options);
		}
	}
}