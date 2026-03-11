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
 * Copyright (C) 2020-2026 Zongsoft Studio <http://www.zongsoft.com>
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

namespace Zongsoft.Data;

partial class ModelPropertyDescriptor
{
	partial class ComplexPropertyDescriptor : ModelPropertyDescriptor
	{
		[TypeConverter(typeof(ConstraintTypeConverter))]
		[JsonConverter(typeof(ConstraintJsonConverter))]
		public readonly struct Constraint : IParsable<Constraint>
		{
			#region 构造函数
			public Constraint(string actor, string name, string value)
			{
				ArgumentException.ThrowIfNullOrEmpty(name);

				this.Name = name;
				this.Value = string.IsNullOrEmpty(value) ? null : value;
				this.Actor = string.IsNullOrEmpty(actor) ? null : actor;
			}
			#endregion

			#region 公共字段
			public readonly string Name;
			public readonly string Value;
			public readonly string Actor;
			#endregion

			#region 解析方法
			public static Constraint Parse(string text, IFormatProvider format) => string.IsNullOrEmpty(text) ? default : Parse(text.AsSpan());
			public static Constraint Parse(ReadOnlySpan<char> text) => TryParse(text, out var result) ? result : default;
			public static bool TryParse(string text, IFormatProvider format, out Constraint result) => TryParse(text, out result);
			public static bool TryParse(ReadOnlySpan<char> text, out Constraint result)
			{
				text = text.Trim();

				if(text.IsEmpty)
				{
					result = default;
					return false;
				}

				var actor = ReadOnlySpan<char>.Empty;
				var index = text.IndexOf(':');

				if(index > 0)
				{
					actor = text[..index];
					text = text[(index + 1)..];
				}

				index = text.IndexOf('=');
				result = index > 0 ?
					new(actor.ToString(), text[..index].ToString(), text[(index + 1)..].ToString()) :
					new(actor.ToString(), text.ToString(), null);

				return true;
			}
			#endregion

			#region 重写方法
			public override string ToString() =>
				string.IsNullOrEmpty(this.Actor) ? $"{this.Name}={this.Value}" : $"{this.Actor}:{this.Name}={this.Value}";
			#endregion
		}

		private class ConstraintTypeConverter : TypeConverter
		{
			public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
			public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

			public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
			{
				if(value is string text)
					return Constraint.Parse(text);

				return base.ConvertFrom(context, culture, value);
			}

			public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
			{
				if(value is Constraint constraint)
					return string.IsNullOrEmpty(constraint.Name) ? null : constraint.ToString();

				return base.ConvertTo(context, culture, value, destinationType);
			}
		}

		private class ConstraintJsonConverter : JsonConverter<Constraint>
		{
			public override Constraint Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) => reader.TokenType switch
			{
				JsonTokenType.Null => default,
				JsonTokenType.String => Constraint.Parse(reader.GetString()),
				_ => throw new JsonException(),
			};

			public override void Write(Utf8JsonWriter writer, Constraint value, JsonSerializerOptions options)
			{
				if(string.IsNullOrEmpty(value.Name))
					writer.WriteNullValue();
				else
					writer.WriteStringValue(value.ToString());
			}
		}
	}
}