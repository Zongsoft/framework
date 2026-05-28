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
 * Copyright (C) 2010-2022 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Globalization;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Zongsoft.Components;

partial struct Version
{
	private sealed class JsonConverter : JsonConverter<Version>
	{
		public override Version Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) => reader.TokenType switch
		{
			JsonTokenType.Null => default,
			JsonTokenType.String => Version.Parse(reader.GetString()),
			JsonTokenType.Number => reader.TryGetUInt64(out var uint64) ? new Version(uint64) : reader.TryGetInt64(out var int64) ? new Version(int64) : throw new JsonException(),
			_ => throw new JsonException(),
		};

		public override void Write(Utf8JsonWriter writer, Version value, JsonSerializerOptions options) => writer.WriteStringValue(value.ToString());
	}

	private sealed class TypeConverter : VersionConverter
	{
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(long) || sourceType == typeof(ulong) || base.CanConvertFrom(context, sourceType);
		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => destinationType == typeof(long) || destinationType == typeof(ulong) || base.CanConvertTo(context, destinationType);

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			if(value is long int64)
				return new Version(int64);
			if(value is ulong uint64)
				return new Version(uint64);
			if(value is string text)
				return Version.Parse(text, culture);
			if(value is System.Version version)
				return (Version)version;

			return base.ConvertFrom(context, culture, value);
		}

		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			if(value is Version version)
			{
				if(destinationType == typeof(long))
					return (long)version;
				if(destinationType == typeof(ulong))
					return (ulong)version;
				if(destinationType == typeof(string))
					return version.ToString();
				if(destinationType == typeof(System.Version))
					return (System.Version)version;
			}

			return base.ConvertTo(context, culture, value, destinationType);
		}

		public override bool IsValid(ITypeDescriptorContext context, object value) => value is Version || base.IsValid(context, value);
	}
}
