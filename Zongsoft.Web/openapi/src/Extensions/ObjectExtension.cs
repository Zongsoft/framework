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
using System.Collections;

using Microsoft.OpenApi;

namespace Zongsoft.Web.OpenApi.Extensions;

static partial class Helper
{
	public static IOpenApiExtension Object(object value) => new ObjectExtension(value);
}

public class ObjectExtension(object value) : IOpenApiExtension
{
	public void Write(IOpenApiWriter writer, OpenApiSpecVersion version)
	{
		if(value == null)
		{
			writer.WriteNull();
			return;
		}

		if(Common.TypeExtension.IsScalarType(value.GetType()))
		{
			writer.WriteValue(value);
			return;
		}

		writer.WriteStartObject();

		if(value is IEnumerable enumerable)
		{
			foreach(var item in enumerable)
			{
				this.OnWrite(writer, version, item);
			}
		}
		else
		{
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
		}

		writer.WriteEndObject();
	}

	protected virtual void OnWrite(IOpenApiWriter writer, OpenApiSpecVersion version, object value)
	{
		switch(value)
		{
			case Configuration.EnvironmentOption environment:
				Helper.Environment(environment).Write(writer, version);
				break;
			case Configuration.VariableOption variable:
				Helper.Variable(variable).Write(writer, version);
				break;
			default:
				writer.WriteValue(value);
				break;
		}
	}
}