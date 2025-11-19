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
using System.Net.Http;
using System.Collections.Generic;

using Microsoft.OpenApi;

namespace Zongsoft.Web.OpenApi;

internal static class Utility
{
	public static IReadOnlyCollection<HttpMethod> GetHttpMethods(this ControllerServiceDescriptor.ControllerOperationDescriptor descriptor)
	{
		var result = new HashSet<HttpMethod>();

		for(int i = 0; i < descriptor.Action.Selectors.Count; i++)
		{
			foreach(var method in Find(descriptor.Action.Selectors[i].EndpointMetadata))
				result.Add(method);
		}

		foreach(var method in Find(descriptor.Action.Attributes))
			result.Add(method);

		return result;

		static IEnumerable<HttpMethod> Find(IEnumerable<object> metadatas)
		{
			foreach(var metadata in metadatas)
			{
				if(metadata is HttpMethod method)
					yield return method;

				if(metadata is Microsoft.AspNetCore.Mvc.Routing.HttpMethodAttribute attribute)
					foreach(var text in attribute.HttpMethods)
						yield return HttpMethod.Parse(text);

				if(metadata is Microsoft.AspNetCore.Routing.HttpMethodMetadata methodMetadata)
					foreach(var text in methodMetadata.HttpMethods)
						yield return HttpMethod.Parse(text);
			}
		}
	}

	public static OpenApiSchema GetSchema(Type type) => GetSchema(type, new());
	private static OpenApiSchema GetSchema(Type type, HashSet<Type> stack)
	{
		if(type == null)
			return null;

		if(stack.Contains(type))
			return null;

		stack.Add(type);

		var schema = new OpenApiSchema()
		{
			Type = GetJsonType(type, out var isArray, out var nullable),
		};

		if(schema.Type == JsonSchemaType.Object)
		{
			var properties = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
			if(properties.Length > 0)
			{
				schema.Properties ??= new Dictionary<string, IOpenApiSchema>(properties.Length);

				for(int i = 0; i < properties.Length; i++)
				{
					var property = properties[i];
					schema.Properties[property.Name] = GetSchema(property.PropertyType, stack);
				}
			}

			var fields = type.GetFields(System.Reflection.BindingFlags.Public |System.Reflection.BindingFlags.Instance);
			if(fields.Length > 0)
			{
				schema.Properties ??= new Dictionary<string, IOpenApiSchema>(fields.Length);

				for(int i = 0; i < fields.Length; i++)
				{
					var field = fields[i];
					schema.Properties[field.Name] = GetSchema(field.FieldType, stack);
				}
			}
		}

		return schema;
	}

	public static JsonSchemaType GetJsonType(Type type, out bool isArray, out bool nullable)
	{
		isArray = type.IsArray;

		if(isArray)
			type = type.GetElementType();

		nullable = Zongsoft.Common.TypeExtension.IsNullable(type, out var underlyingType);
		if(nullable && underlyingType != null)
			type = underlyingType;

		switch(Type.GetTypeCode(type))
		{
			case TypeCode.Boolean:
				return JsonSchemaType.Boolean;
			case TypeCode.Char:
			case TypeCode.String:
			case TypeCode.DateTime:
				return JsonSchemaType.String;
			case TypeCode.Byte:
			case TypeCode.SByte:
			case TypeCode.Int16:
			case TypeCode.Int32:
			case TypeCode.Int64:
			case TypeCode.UInt16:
			case TypeCode.UInt32:
			case TypeCode.UInt64:
				return JsonSchemaType.Integer;
			case TypeCode.Single:
			case TypeCode.Double:
			case TypeCode.Decimal:
				return JsonSchemaType.Number;
			case TypeCode.Empty:
			case TypeCode.DBNull:
				return JsonSchemaType.Null;
		}

		if(IsParsable(type))
			return JsonSchemaType.String;

		var converter = Zongsoft.Common.Convert.GetTypeConverter(type, true);
		if(converter != null && converter.CanConvertFrom(typeof(string)))
			return JsonSchemaType.String;

		return JsonSchemaType.Object;

		static bool IsParsable(Type type)
		{
			var contracts = type.GetInterfaces();

			for(int i = 0; i < contracts.Length; i++)
			{
				if(contracts[i].IsGenericType && contracts[i].GetGenericTypeDefinition() == typeof(IParsable<>))
					return true;
			}

			return false;
		}
	}
}
