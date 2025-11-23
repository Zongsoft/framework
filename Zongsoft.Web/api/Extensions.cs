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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Web.OpenApi library.
 *
 * The Zongsoft.Web.OpenApi is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Web.OpenApi is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Web.OpenApi library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

using Microsoft.OpenApi;

namespace Zongsoft.Web.OpenApi;

public static class Extensions
{
	public static OpenApiTag Tag(string name, params string[] ancestors)
	{
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		var tag = new OpenApiTag() { Name = name };

		for(int i = ancestors.Length - 1; i >= 0; i--)
		{
			if(!string.IsNullOrEmpty(ancestors[i]))
			{
				tag.Extensions ??= new Dictionary<string, IOpenApiExtension>(StringComparer.OrdinalIgnoreCase);
				tag.Extensions["x-parent"] = Text(ancestors[i]);
				break;
			}
		}

		return tag;
	}

	public static OpenApiTag Caption(this OpenApiTag tag, string caption)
	{
		const string EXTENSION_KEY = "x-displayName";

		if(tag == null)
			return tag;

		if(string.IsNullOrEmpty(caption))
			tag.Extensions?.Remove(EXTENSION_KEY);
		else
		{
			if(tag.Extensions == null)
				tag.Extensions = new Dictionary<string, IOpenApiExtension>()
				{
					{ EXTENSION_KEY, Text(caption) },
				};
			else
				tag.Extensions.Add(EXTENSION_KEY, Text(caption));
		}

		return tag;
	}

	public static OpenApiTag Description(this OpenApiTag tag, string description)
	{
		tag?.Description = string.IsNullOrEmpty(description) ? null : description;
		return tag;
	}

	public static IOpenApiExtension Text(string value) => new StringExtension(value);
	public static IOpenApiExtension Object(object value) => new ObjectExtension(value);
	public static IOpenApiExtension Array(IEnumerable values) => new ArrayExtension(values);

	private sealed class StringExtension(string value) : IOpenApiExtension
	{
		public void Write(IOpenApiWriter writer, OpenApiSpecVersion version) => writer.WriteValue(value);
	}

	private sealed class ArrayExtension(IEnumerable values) : IOpenApiExtension
	{
		public void Write(IOpenApiWriter writer, OpenApiSpecVersion specVersion)
		{
			if(values == null)
			{
				writer.WriteNull();
				return;
			}

			writer.WriteStartArray();

			foreach(var value in values)
				writer.WriteValue(value);

			writer.WriteEndArray();
		}
	}

	private sealed class ObjectExtension(object value) : IOpenApiExtension
	{
		public void Write(IOpenApiWriter writer, OpenApiSpecVersion version)
		{
			if(value == null)
			{
				writer.WriteNull();
				return;
			}

			writer.WriteStartObject();

			var fields = value.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);
			for(int i = 0; i < fields.Length; i++)
			{
				var field = fields[i];
				var fieldValue = field.GetValue(value);
				writer.WritePropertyName(field.Name);
				if(fieldValue == null)
					writer.WriteNull();
				else
					writer.WriteValue(fieldValue);
			}

			var properties = value.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
			for(int i = 0; i < properties.Length; i++)
			{
				var property = properties[i];

				if(property.CanRead && property.GetIndexParameters().Length == 0)
				{
					var propertyValue = property.GetValue(value);
					writer.WritePropertyName(property.Name);

					if(propertyValue == null)
						writer.WriteNull();
					else
						writer.WriteValue(propertyValue);
				}
			}

			writer.WriteEndObject();
		}
	}
}
