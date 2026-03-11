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
		[TypeConverter(typeof(LinkTypeConverter))]
		[JsonConverter(typeof(LinkJsonConverter))]
		public readonly struct Link : IEquatable<Link>, IParsable<Link>
		{
			#region 私有变量
			private readonly int _hashcode;
			#endregion

			#region 构造函数
			public Link(string port, string anchor = null)
			{
				ArgumentException.ThrowIfNullOrEmpty(port);

				this.Port = port;
				this.Anchor = string.IsNullOrEmpty(anchor) ? port : anchor;

				_hashcode = HashCode.Combine(port.ToUpperInvariant(), anchor?.ToUpperInvariant());
			}
			#endregion

			#region 公共字段
			public readonly string Port;
			public readonly string Anchor;
			#endregion

			#region 解析方法
			public static Link Parse(string text, IFormatProvider format) => string.IsNullOrEmpty(text) ? default : Parse(text.AsSpan());
			public static Link Parse(ReadOnlySpan<char> text) => TryParse(text, out var result) ? result : default;
			public static bool TryParse(string text, IFormatProvider format, out Link result) => TryParse(text, out result);
			public static bool TryParse(ReadOnlySpan<char> text, out Link result)
			{
				text = text.Trim();

				if(text.IsEmpty)
				{
					result = default;
					return false;
				}

				var index = text.IndexOfAny([':', '-']);

				if(index > 0)
				{
					var port = text[(index + 1)..].Trim().TrimStart('>');

					if(port.IsEmpty)
					{
						result = default;
						return false;
					}
					else
					{
						result = new(port.ToString(), text[..index].Trim().ToString());
						return true;
					}
				}

				result = new(text.ToString());
				return true;
			}
			#endregion

			#region 重写方法
			public bool Equals(Link other) => string.Equals(this.Port, other.Port, StringComparison.OrdinalIgnoreCase) &&
			(
				(string.IsNullOrEmpty(this.Anchor) && string.IsNullOrEmpty(other.Anchor)) || string.Equals(this.Anchor, other.Anchor, StringComparison.OrdinalIgnoreCase)
			);

			public override bool Equals(object obj) => obj is Link other && this.Equals(other);
			public override int GetHashCode() => _hashcode;
			public override string ToString() => string.IsNullOrEmpty(this.Anchor) || string.Equals(this.Anchor, this.Port) ? this.Port : $"{this.Anchor}->{this.Port}";
			#endregion

			#region 符号重写
			public static bool operator ==(Link left, Link right) => left.Equals(right);
			public static bool operator !=(Link left, Link right) => !(left == right);
			#endregion
		}

		private class LinkTypeConverter : TypeConverter
		{
			public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
			public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

			public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
			{
				if(value is string text)
					return Link.Parse(text);

				return base.ConvertFrom(context, culture, value);
			}

			public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
			{
				if(value is Link link)
					return string.IsNullOrEmpty(link.Port) ? null : link.ToString();

				return base.ConvertTo(context, culture, value, destinationType);
			}
		}

		private class LinkJsonConverter : JsonConverter<Link>
		{
			public override Link Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) => reader.TokenType switch
			{
				JsonTokenType.Null => default,
				JsonTokenType.String => Link.Parse(reader.GetString()),
				_ => throw new JsonException(),
			};

			public override void Write(Utf8JsonWriter writer, Link value, JsonSerializerOptions options)
			{
				if(string.IsNullOrEmpty(value.Port))
					writer.WriteNullValue();
				else
					writer.WriteStringValue(value.ToString());
			}
		}
	}
}