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
using System.Linq;
using System.Text;
using System.Reflection;
using System.Globalization;
using System.ComponentModel;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Zongsoft.Components;

partial struct Identifier
{
	#region 名称验证
	internal static bool IsName(string name)
	{
		if(string.IsNullOrEmpty(name) || name[0] == '.')
			return false;

		foreach(var character in name)
		{
			if(!char.IsAsciiLetterOrDigit(character) && character is not ('_' or '.'))
				return false;
		}

		return true;
	}
	#endregion

	#region 类型转换
	internal static Type[] GetKeyTypes(Type type) => IsTuple(type) ? type.GenericTypeArguments : [type];
	private static bool IsTuple(Type type)
	{
		if(!type.IsGenericType)
			return false;

		var definition = type.GetGenericTypeDefinition();
		return definition == typeof(ValueTuple<,>) || definition == typeof(ValueTuple<,,>) ||
		       definition == typeof(ValueTuple<,,,>) || definition == typeof(ValueTuple<,,,,>);
	}

	internal static bool CanConvert(Type type)
	{
		if(type.IsEnum ||
		   type == typeof(string) || type == typeof(Guid) ||
		   type == typeof(DateTime) || type == typeof(DateTimeOffset) ||
		   type == typeof(DateOnly) || type == typeof(TimeOnly) || type == typeof(TimeSpan))
			return true;

		var converter = TypeDescriptor.GetConverter(type);
		return converter.CanConvertFrom(typeof(string)) && converter.CanConvertTo(typeof(string));
	}

	internal static bool TryConvert(object value, Type type, out object result)
	{
		result = null;
		if(value == null)
			return false;

		if(type.IsInstanceOfType(value))
		{
			result = value;
			return true;
		}

		if(IsTuple(type) && value is ITuple tuple && tuple.Length == type.GenericTypeArguments.Length)
		{
			var values = new object[tuple.Length];

			for(int i = 0; i < tuple.Length; i++)
			{
				if(!TryConvert(tuple[i], type.GenericTypeArguments[i], out values[i]))
					return false;
			}

			result = Activator.CreateInstance(type, values);
			return true;
		}

		return Common.Convert.TryConvertValue(value, type, out result);
	}

	private static Type ResolveType(string text)
	{
		var assemblies = AppDomain.CurrentDomain.GetAssemblies();

		return Common.TypeAlias.Parse(text,
			name => assemblies.FirstOrDefault(assembly => AssemblyName.ReferenceMatchesDefinition(assembly.GetName(), name)),
			(assembly, name, ignoreCase) => assembly != null ? assembly.GetType(name, false, ignoreCase) :
				assemblies.Select(candidate => candidate.GetType(name, false, ignoreCase)).FirstOrDefault(type => type != null), false);
	}

	private static string GetTypeName(Type type) => type.IsEnum ? Common.TypeAlias.GetAlias(type) : System.Type.GetTypeCode(type) switch
	{
		TypeCode.Boolean => "bool",
		TypeCode.Char => "char",
		TypeCode.Byte => "byte",
		TypeCode.SByte => "sbyte",
		TypeCode.Int16 => "short",
		TypeCode.UInt16 => "ushort",
		TypeCode.Int32 => "int",
		TypeCode.UInt32 => "uint",
		TypeCode.Int64 => "long",
		TypeCode.UInt64 => "ulong",
		TypeCode.Single => "float",
		TypeCode.Double => "double",
		TypeCode.Decimal => "decimal",
		TypeCode.String => "string",
		TypeCode.DateTime => "DateTime",
		_ => Common.TypeAlias.GetAlias(type),
	};

	private static string FormatValue(object value)
	{
		if(value == null)
			throw new FormatException(Zongsoft.Properties.Resources.Identifier_InvalidFormat_Message);

		return value switch
		{
			string text => text,
			float number => number.ToString("R", CultureInfo.InvariantCulture),
			double number => number.ToString("R", CultureInfo.InvariantCulture),
			DateTime date => date.ToString("O", CultureInfo.InvariantCulture),
			DateTimeOffset date => date.ToString("O", CultureInfo.InvariantCulture),
			DateOnly date => date.ToString("O", CultureInfo.InvariantCulture),
			TimeOnly time => time.ToString("O", CultureInfo.InvariantCulture),
			TimeSpan time => time.ToString("c", CultureInfo.InvariantCulture),
			Guid guid => guid.ToString("D"),
			Enum enumeration => enumeration.ToString("D"),
			_ => CanConvert(value.GetType()) ? TypeDescriptor.GetConverter(value.GetType()).ConvertToInvariantString(value) :
				throw new FormatException(Zongsoft.Properties.Resources.Identifier_InvalidFormat_Message),
		};
	}

	private static object ParseValue(string text, Type type)
	{
		if(type == typeof(string))
			return text;
		if(type == typeof(DateTime))
			return DateTime.ParseExact(text, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
		if(type == typeof(DateTimeOffset))
			return DateTimeOffset.ParseExact(text, "O", CultureInfo.InvariantCulture, DateTimeStyles.None);
		if(type == typeof(DateOnly))
			return DateOnly.ParseExact(text, "O", CultureInfo.InvariantCulture);
		if(type == typeof(TimeOnly))
			return TimeOnly.ParseExact(text, "O", CultureInfo.InvariantCulture);
		if(type == typeof(TimeSpan))
			return TimeSpan.ParseExact(text, "c", CultureInfo.InvariantCulture);
		if(type == typeof(Guid))
			return Guid.ParseExact(text, "D");
		if(type.IsEnum)
			return Enum.Parse(type, text);
		if(!CanConvert(type))
			throw new FormatException(Zongsoft.Properties.Resources.Identifier_InvalidFormat_Message);

		return TypeDescriptor.GetConverter(type).ConvertFromInvariantString(text);
	}

	private static object CreateValue(Type[] types, object[] values) => types.Length == 1 ? values[0] :
		Activator.CreateInstance((types.Length switch
		{
			2 => typeof(ValueTuple<,>),
			3 => typeof(ValueTuple<,,>),
			4 => typeof(ValueTuple<,,,>),
			5 => typeof(ValueTuple<,,,,>),
			_ => throw new FormatException(Zongsoft.Properties.Resources.Identifier_InvalidFormat_Message),
		}).MakeGenericType(types), values);
	#endregion

	#region 文本输出
	private static string Format(Identifier identifier, string format)
	{
		if(!string.IsNullOrEmpty(format) && format is not ("F" or "G"))
			throw new FormatException(Zongsoft.Properties.Resources.Identifier_InvalidFormat_Message);
		if(identifier.IsEmpty)
			return string.Empty;

		var types = GetKeyTypes(identifier.Value.GetType());
		var rule = Formatting.Find(identifier.Name, identifier.Type);
		var compact = format != "F" && rule != null && rule.Types.SequenceEqual(types);
		var separator = compact ? rule.Separator : ',';
		var result = new StringBuilder(identifier.Type == null ? identifier.Name : "$" + Common.TypeAlias.GetAlias(identifier.Type));

		result.Append(':');

		if(!compact && (types.Length != 1 || types[0] != typeof(string) || rule != null))
			result.Append('<').AppendJoin(',', types.Select(GetTypeName)).Append('>');

		if(IsTuple(identifier.Value.GetType()) && identifier.Value is ITuple tuple)
		{
			for(int i = 0; i < tuple.Length; i++)
			{
				if(i > 0)
					result.Append(separator);
				Escape(result, FormatValue(tuple[i]), separator);
			}
		}
		else
			Escape(result, FormatValue(identifier.Value), separator);

		if(!string.IsNullOrEmpty(identifier.Code))
		{
			result.Append('#');
			Escape(result, identifier.Code);
		}

		if(!string.IsNullOrEmpty(identifier.Label) || !string.IsNullOrEmpty(identifier.Alias))
		{
			result.Append('?');
			Escape(result, identifier.Label);

			if(!string.IsNullOrEmpty(identifier.Alias))
			{
				result.Append('|');
				Escape(result, identifier.Alias);
			}
		}

		if(!string.IsNullOrEmpty(identifier.Description))
		{
			result.Append('(');
			Escape(result, identifier.Description);
			result.Append(')');
		}

		if(identifier.Properties?.Count > 0)
		{
			result.Append('{');
			var count = 0;

			foreach(var entry in identifier.Properties.OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase))
			{
				if(count++ > 0)
					result.Append(';');

				Escape(result, entry.Key);
				result.Append('=');
				Escape(result, entry.Value);
			}

			result.Append('}');
		}

		return result.ToString();
	}

	private static void Escape(StringBuilder buffer, string text, char separator = '\0')
	{
		if(text == null)
			return;

		foreach(var character in text)
		{
			switch(character)
			{
				case '\n':
					buffer.Append("\\n");
					break;
				case '\r':
					buffer.Append("\\r");
					break;
				case '\t':
					buffer.Append("\\t");
					break;
				default:
					if(char.IsControl(character))
						buffer.Append("\\u").Append(((int)character).ToString("X4", CultureInfo.InvariantCulture));
					else
					{
						if(character == separator || "\\:,?|#{};=()$<>".Contains(character))
							buffer.Append('\\');

						buffer.Append(character);
					}
					break;
			}
		}
	}
	#endregion
}
