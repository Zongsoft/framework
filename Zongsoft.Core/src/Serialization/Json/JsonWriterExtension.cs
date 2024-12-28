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
using System.Collections.Generic;

namespace Zongsoft.Serialization.Json;

public static class JsonWriterExtension
{
	public static void WritePropertyName(this Utf8JsonWriter writer, string name, JsonSerializerOptions options)
	{
		if(options != null && options.PropertyNamingPolicy != null)
			name = options.PropertyNamingPolicy.ConvertName(name);

		writer.WritePropertyName(name);
	}

	public static void WriteObject(this Utf8JsonWriter writer, object value, JsonSerializerOptions options)
	{
		var type = Data.Model.GetModelType(value);

		//如果属性值是异步流，则必须将其作为同步流处理（因为JSON序列化器的Serialize方法只支持同步流）
		if(Collections.Enumerable.IsAsyncEnumerable(value, out var elementType))
		{
			type = typeof(IEnumerable<>).MakeGenericType(elementType);

			//如果属性值未实现同步流接口，则必须将其用同步器进行包装
			if(!type.IsAssignableFrom(value.GetType()))
				value = Collections.Enumerable.Enumerate(value, elementType);
		}

		JsonSerializer.Serialize(writer, value, type, options);
	}
}
