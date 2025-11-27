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
using System.Linq;
using System.Text;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Collections.Generic;

using Microsoft.OpenApi;

using Zongsoft.Common;

namespace Zongsoft.Web.OpenApi;

partial class DocumentGenerator
{
	internal static IOpenApiSchema GenerateSchema(DocumentContext context, Type type)
	{
		var schema = new OpenApiSchema()
		{
			Type = GetJsonType(type, out var nullable, out var underlyingType),
			Format = GetFormat(underlyingType ?? type),
		};

		if((underlyingType != null && underlyingType.IsEnum) || type.IsEnum)
		{
			if(TryAdd(context.Document, schema, underlyingType ?? type, out var reference))
				SetSchemaEnumeration(schema, underlyingType ?? type);

			return reference;
		}

		if((underlyingType != null && underlyingType.IsScalarType()) || type.IsScalarType())
		{
			return schema;
		}

		if(schema.Type == JsonSchemaType.Array)
		{
			var elementType = TypeExtension.GetElementType(type);

			if(elementType.IsDictionaryEntry())
				schema.Type = JsonSchemaType.Object;
			else
				schema.Items = GenerateSchema(context, elementType);

			return schema;
		}

		if(TryAdd(context.Document, schema, underlyingType ?? type, out var result))
			SetSchemaObject(context, schema, underlyingType ?? type);

		return result;

		static bool TryAdd(OpenApiDocument document, OpenApiSchema schema, Type type, out IOpenApiSchema result)
		{
			if(type.IsDictionary())
			{
				result = new OpenApiSchema() { Type = JsonSchemaType.Object };
				return false;
			}

			var key = GetSchemaKey(type);

			if(document.Components.Schemas.TryGetValue(key, out var value))
			{
				result = value is OpenApiSchemaReference reference ? reference : new(key);
				return false;
			}

			document.Components.Schemas.TryAdd(key, schema);
			result = new OpenApiSchemaReference(key);
			return true;
		}
	}

	private static void SetSchemaObject(DocumentContext context, OpenApiSchema schema, Type type)
	{
		if(schema.Type != JsonSchemaType.Object)
			return;

		if(type.IsDictionary() || type.IsDictionaryEntry())
			return;

		var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
		if(properties.Length > 0)
		{
			schema.Properties ??= new Dictionary<string, IOpenApiSchema>(properties.Length);

			for(int i = 0; i < properties.Length; i++)
			{
				var property = properties[i];

				if(property.CanRead && property.CanWrite && !IsIgnored(property))
					schema.Properties[property.Name] = GenerateSchema(context, property.PropertyType);
			}
		}

		var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
		if(fields.Length > 0)
		{
			schema.Properties ??= new Dictionary<string, IOpenApiSchema>(fields.Length);

			for(int i = 0; i < fields.Length; i++)
			{
				var field = fields[i];

				if(!field.IsInitOnly && !IsIgnored(field))
					schema.Properties[field.Name] = GenerateSchema(context, field.FieldType);
			}
		}
	}

	private static void SetSchemaEnumeration(OpenApiSchema schema, Type type)
	{
		var underlyingType = Enum.GetUnderlyingType(type);
		schema.Type = JsonSchemaType.Integer;
		schema.Format = GetFormat(underlyingType);

		var entries = EnumUtility.GetEnumEntries(type, true);

		schema.Enum = [.. entries.Select(entry => CreateNode(entry.Value))];
		schema.AddExtension("x-enum-varnames", Extensions.Helper.Array(entries.Select(entry => entry.Name)));
		schema.AddExtension("x-enum-descriptions", Extensions.Helper.Array(entries.Select(entry => string.IsNullOrEmpty(entry.Description) ? entry.Name : entry.Description)));
	}

	private static JsonNode CreateNode(object value)
	{
		var method = GetCreateMethod()
			.MakeGenericMethod(value.GetType());

		return (JsonNode)method.Invoke(null, [value, null]);

		static MethodInfo GetCreateMethod()
		{
			return typeof(JsonValue)
				.GetMethods(BindingFlags.Public | BindingFlags.Static)
				.First(method => method.IsGenericMethodDefinition && method.Name == nameof(JsonValue.Create) && method.GetParameters().Length == 2);
		}
	}

	private static string GetSchemaKey(Type type) => type.FullName;

	private static bool IsIgnored(MemberInfo member)
	{
		if(member.GetCustomAttribute<System.Text.Json.Serialization.JsonIgnoreAttribute>(true) != null)
			return true;

		var attribute = member.GetCustomAttribute<Zongsoft.Serialization.SerializationMemberAttribute>(true);
		return attribute != null && attribute.Ignored;
	}

	/*
	 * https://spec.openapis.org/registry/format
	 */
	private static string GetFormat(Type type)
	{
		switch(Type.GetTypeCode(type))
		{
			case TypeCode.String:
				return null;
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

	private static JsonSchemaType GetJsonType(Type type, out bool nullable, out Type underlyingType)
	{
		if(type == typeof(string) || type == typeof(StringBuilder) ||
		   type == typeof(ReadOnlySpan<char>) || type == typeof(Span<char>) ||
		   type == typeof(ReadOnlyMemory<char>) || type == typeof(Memory<char>))
		{
			nullable = true;
			underlyingType = null;
			return JsonSchemaType.String;
		}

		underlyingType = null;
		nullable = type.IsClass || type.IsInterface || type.IsNullable(out underlyingType);

		if(type.IsArray || type.IsEnumerable())
			return JsonSchemaType.Array;

		if(nullable && underlyingType != null)
			type = underlyingType;

		if(type.IsEnum)
			return JsonSchemaType.String;

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
