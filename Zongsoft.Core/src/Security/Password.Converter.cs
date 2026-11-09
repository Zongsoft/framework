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
using System.Globalization;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Zongsoft.Security;

[JsonConverter(typeof(PasswordJsonConverter))]
[TypeConverter(typeof(PasswordTypeConverter))]
partial struct Password
{
	private class PasswordTypeConverter : TypeConverter
	{
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) =>
			sourceType == typeof(string) || sourceType == typeof(byte[]);
		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) =>
			destinationType == typeof(string) || destinationType == typeof(byte[]);

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			if(value == null || Convert.IsDBNull(value))
				return default(Password);

			return value switch
			{
				string text => Password.TryParse(text, out var result) ? result : throw new InvalidCastException(),
				byte[] data => Password.TryParse(data, out var result) ? result : throw new InvalidCastException(),
				_ => throw new InvalidCastException()
			};
		}

		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			if(value is not Password password)
				throw new InvalidOperationException();

			if(destinationType == typeof(string))
				return password.IsEmpty ? null : password.ToString();
			if(destinationType == typeof(byte[]))
				return password.IsEmpty ? null : (byte[])password;

			throw new InvalidCastException();
		}
	}

	private class PasswordJsonConverter : JsonConverter<Password>
	{
		public override Password Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
		{
			switch(reader.TokenType)
			{
				case JsonTokenType.Null:
					return default;
				case JsonTokenType.String:
					if(reader.TryGetBytesFromBase64(out var data))
						return Password.TryParse(data, out var result) ? result : throw new JsonException();
					else
						return Password.TryParse(reader.GetString(), out var result) ? result : throw new JsonException();
				default:
					throw new JsonException();
			}
		}

		public override void Write(Utf8JsonWriter writer, Password value, JsonSerializerOptions options)
		{
			if(value.IsEmpty)
				writer.WriteNullValue();
			else
				writer.WriteBase64StringValue((byte[])value);
		}
	}
}
