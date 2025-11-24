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
using System.Collections.Generic;

using Microsoft.OpenApi;

namespace Zongsoft.Web.OpenApi;

internal static class Utility
{
	public static string GetUrl(this Routing.RoutePattern pattern)
	{
		if(pattern == null)
			return null;

		foreach(var entry in pattern)
		{
			pattern.Map(entry.Name, $"{{{entry.Name}}}");
			//if(entry.Optional || entry.Captured || entry.HasValue)
			//	pattern.Map(entry.Name, null);
			//else
			//	pattern.Map(entry.Name, entry.Name);
		}

		return pattern.Value.TrimEnd('/');
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
			Format = GetFormat(type),
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

	/*
	 * https://spec.openapis.org/registry/format
	 */
	private static string GetFormat(Type type)
	{
		switch(Type.GetTypeCode(type))
		{
			case TypeCode.DateTime:
				return "date-time";
			case TypeCode.Byte:
				return "byte";
			case TypeCode.Int16:
				return "int16";
			case TypeCode.Int32:
				return "int32";
			case TypeCode.Int64:
				return "int64";
			case TypeCode.UInt16:
				return "uint16";
			case TypeCode.UInt32:
				return "uint32";
			case TypeCode.UInt64:
				return "uint64";
			case TypeCode.Single:
				return "float";
			case TypeCode.Double:
				return "double";
			case TypeCode.Decimal:
				return "decimal";
		}

		if(type == typeof(byte[]))
			return "byte";
		if(type == typeof(Uri))
			return "uri";
		if(type == typeof(Guid))
			return "uuid";
		if(type == typeof(DateTimeOffset))
			return "date-time";
		if(type == typeof(DateOnly))
			return "date";
		if(type == typeof(TimeOnly))
			return "time";
		if(type == typeof(TimeSpan))
			return "duration";

		return null;
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
