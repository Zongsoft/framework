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
using System.Reflection;
using System.Text.Json;
using System.Globalization;
using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Runtime.ExceptionServices;

namespace Zongsoft.Components;

internal sealed class IdentifierConverter(Type type) : TypeConverter
{
	#region 私有字段
	private readonly MethodInfo _parse = type.GetMethod(nameof(Identifier.Parse), [typeof(string)]);
	#endregion

	#region 类型转换
	public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
	public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
	public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
	{
		if(value is not string text)
			return base.ConvertFrom(context, culture, value);

		try
		{
			return _parse.Invoke(null, [text]);
		}
		catch(TargetInvocationException exception) when(exception.InnerException != null)
		{
			ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
			throw;
		}
	}

	public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) =>
		destinationType == typeof(string) && type.IsInstanceOfType(value) ? value.ToString() : base.ConvertTo(context, culture, value, destinationType);
	#endregion
}

internal sealed class IdentifierJsonConverter : JsonConverterFactory
{
	#region 转换工厂
	public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(Identifier) || typeToConvert.IsGenericType &&
		typeToConvert.Namespace == typeof(Identifier).Namespace && typeToConvert.Assembly == typeof(Identifier).Assembly &&
		typeToConvert.Name.StartsWith("Identifier`", StringComparison.Ordinal);

	public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
		(JsonConverter)Activator.CreateInstance(typeof(Converter<>).MakeGenericType(typeToConvert));
	#endregion

	#region 序列转换
	private sealed class Converter<T> : JsonConverter<T> where T : struct, ISpanParsable<T>
	{
		public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if(reader.TokenType == JsonTokenType.Null)
				return default;
			if(reader.TokenType == JsonTokenType.String && T.TryParse(reader.GetString(), null, out var value))
				return value;

			throw new JsonException(Properties.Resources.Identifier_InvalidFormat_Message);
		}

		public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
		{
			var text = value.ToString();

			if(string.IsNullOrEmpty(text))
				writer.WriteNullValue();
			else
				writer.WriteStringValue(text);
		}

		public override T ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
			T.TryParse(reader.GetString(), null, out var value) ? value : throw new JsonException(Properties.Resources.Identifier_InvalidFormat_Message);
		public override void WriteAsPropertyName(Utf8JsonWriter writer, T value, JsonSerializerOptions options) => writer.WritePropertyName(value.ToString());
	}
	#endregion
}
