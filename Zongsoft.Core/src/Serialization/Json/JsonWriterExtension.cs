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
