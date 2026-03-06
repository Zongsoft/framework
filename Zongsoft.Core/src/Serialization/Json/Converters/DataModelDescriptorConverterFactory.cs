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
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
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

using Zongsoft.Data;

namespace Zongsoft.Serialization.Json.Converters;

public sealed class DataModelDescriptorConverterFactory : JsonConverterFactory
{
	public override bool CanConvert(Type type) => typeof(ModelPropertyDescriptor).IsAssignableFrom(type);
	public override JsonConverter CreateConverter(Type type, JsonSerializerOptions options) => DataModelPropertyDescriptorConverter.Instance;

	private class DataModelPropertyDescriptorConverter : JsonConverter<ModelPropertyDescriptor>
	{
		public static readonly DataModelPropertyDescriptorConverter Instance = new();

		public override ModelPropertyDescriptor Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
		{
			ModelPropertyDescriptor result = null;

			while(reader.Read())
			{
				switch(reader.TokenType)
				{
					case JsonTokenType.PropertyName:
						if(reader.GetString() == "$type")
						{
							reader.Read();
							var discriminator = reader.GetString().ToLowerInvariant();

							return discriminator switch
							{
								"simplex" => JsonSerializer.Deserialize<ModelPropertyDescriptor.SimplexPropertyDescriptor>(ref reader, options),
								"complex" => JsonSerializer.Deserialize<ModelPropertyDescriptor.ComplexPropertyDescriptor>(ref reader, options),
								_ => throw new JsonException($"Unsupported model property descriptor type: {discriminator}")
							};
						}
						break;
					case JsonTokenType.EndObject:
						throw new JsonException("Missing $type property for model property descriptor.");
					default:
						break;
				}
			}

			return result;
		}

		public override void Write(Utf8JsonWriter writer, ModelPropertyDescriptor value, JsonSerializerOptions options)
		{
			if(writer.CurrentDepth > 2)
				return;

			writer.WriteStartObject();

			switch(value)
			{
				case ModelPropertyDescriptor.SimplexPropertyDescriptor simplex:
					writer.WriteString("$type", nameof(simplex));
					writer.WriteObject(simplex, options);
					break;
				case ModelPropertyDescriptor.ComplexPropertyDescriptor complex:
					writer.WriteString("$type", nameof(complex));
					writer.WriteObject(complex, options);
					break;
				default:
					throw new JsonException($"Unsupported model property descriptor type: {value.GetType().FullName}");
			}

			writer.WriteEndObject();
		}
	}
}
