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
using System.Globalization;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;

using Zongsoft.Common;

namespace Zongsoft.Components;

/// <summary>
/// 表示对象标识的结构。
/// </summary>
[TypeConverter(typeof(TypeConverter))]
[JsonConverter(typeof(JsonConverter))]
public readonly struct Identifier(Type type, object value, string label = null, string description = null) : IEquatable<Identifier>, IConvertible
{
	#region 公共字段
	/// <summary>获取标识的对象类型。</summary>
	public readonly Type Type = type ?? throw new ArgumentNullException(nameof(type));
	/// <summary>获取标识键值。</summary>
	public readonly object Value = value;
	/// <summary>获取标识的标签。</summary>
	public readonly string Label = label;
	/// <summary>获取标识的描述。</summary>
	public readonly string Description = description;
	#endregion

	#region 公共属性
	/// <summary>获取一个值，指示当前对象标识是否为一个空标识。</summary>
	public bool IsEmpty => this.Type == null || this.Value == null;

	/// <summary>获取一个值，指示当前对象标识是否具有键值。</summary>
	public bool HasValue => this.Type != null && this.Value != null;
	#endregion

	#region 公共方法
	public bool Validate<TValue>(out TValue value) => Common.Convert.TryConvertValue<TValue>(this.Value, out value);
	public bool Validate<TType, TValue>(out TValue value) => this.Validate(typeof(TType), out value);
	public bool Validate<TValue>(Type type, out TValue value)
	{
		if(type != null && type.IsAssignableFrom(this.Type))
			return Common.Convert.TryConvertValue<TValue>(this.Value, out value);

		value = default;
		return false;
	}
	#endregion

	#region 重写方法
	public bool Equals(Identifier other) => this.Type == other.Type && this.Value == other.Value;
	public override bool Equals(object obj) => obj is Identifier other && this.Equals(other);
	public override int GetHashCode() => this.Value is string text ? HashCode.Combine(this.Type, text.ToUpperInvariant()) : HashCode.Combine(this.Type, this.Value);
	public override string ToString()
	{
		if(this.IsEmpty)
			return string.Empty;

		string text = null;

		if(!string.IsNullOrEmpty(this.Label))
			text += $"{nameof(this.Label)}={this.Label};";
		if(!string.IsNullOrEmpty(this.Description))
			text += $"{nameof(this.Description)}={this.Description};";

		//对标识值文本进行字符转义
		var value = this.Value?.ToString()
			.Replace("{", @"\{")
			.Replace("}", @"\}");

		if(string.IsNullOrEmpty(text))
			return $"{TypeAlias.GetAlias(this.Type)}:{value}";
		else
			return $"{TypeAlias.GetAlias(this.Type)}:{value}" + '{' + text.Trim(';') + '}';
	}
	#endregion

	#region 符号重载
	public static bool operator ==(Identifier left, Identifier right) => left.Equals(right);
	public static bool operator !=(Identifier left, Identifier right) => !(left == right);

	public static explicit operator bool(Identifier identifier) => Common.Convert.ConvertValue<bool>(identifier.Value);
	public static explicit operator char(Identifier identifier) => Common.Convert.ConvertValue<char>(identifier.Value);
	public static explicit operator byte(Identifier identifier) => Common.Convert.ConvertValue<byte>(identifier.Value);
	public static explicit operator sbyte(Identifier identifier) => Common.Convert.ConvertValue<sbyte>(identifier.Value);
	public static explicit operator short(Identifier identifier) => Common.Convert.ConvertValue<short>(identifier.Value);
	public static explicit operator int(Identifier identifier) => Common.Convert.ConvertValue<int>(identifier.Value);
	public static explicit operator long(Identifier identifier) => Common.Convert.ConvertValue<long>(identifier.Value);
	public static explicit operator ushort(Identifier identifier) => Common.Convert.ConvertValue<ushort>(identifier.Value);
	public static explicit operator uint(Identifier identifier) => Common.Convert.ConvertValue<uint>(identifier.Value);
	public static explicit operator ulong(Identifier identifier) => Common.Convert.ConvertValue<ulong>(identifier.Value);
	public static explicit operator float(Identifier identifier) => Common.Convert.ConvertValue<float>(identifier.Value);
	public static explicit operator double(Identifier identifier) => Common.Convert.ConvertValue<double>(identifier.Value);
	public static explicit operator decimal(Identifier identifier) => Common.Convert.ConvertValue<decimal>(identifier.Value);
	public static explicit operator string(Identifier identifier) => Common.Convert.ConvertValue<string>(identifier.Value);
	public static explicit operator DateTime(Identifier identifier) => Common.Convert.ConvertValue<DateTime>(identifier.Value);
	public static explicit operator DateTimeOffset(Identifier identifier) => Common.Convert.ConvertValue<DateTimeOffset>(identifier.Value);
	public static explicit operator DateOnly(Identifier identifier) => Common.Convert.ConvertValue<DateOnly>(identifier.Value);
	public static explicit operator TimeOnly(Identifier identifier) => Common.Convert.ConvertValue<TimeOnly>(identifier.Value);
	public static explicit operator TimeSpan(Identifier identifier) => Common.Convert.ConvertValue<TimeSpan>(identifier.Value);
	public static explicit operator Guid(Identifier identifier) => Common.Convert.ConvertValue<Guid>(identifier.Value);
	#endregion

	#region 转换方法
	bool IConvertible.ToBoolean(IFormatProvider provider) => Common.Convert.ConvertValue<bool>(this.Value);
	char IConvertible.ToChar(IFormatProvider provider) => Common.Convert.ConvertValue<char>(this.Value);
	byte IConvertible.ToByte(IFormatProvider provider) => Common.Convert.ConvertValue<byte>(this.Value);
	sbyte IConvertible.ToSByte(IFormatProvider provider) => Common.Convert.ConvertValue<sbyte>(this.Value);
	short IConvertible.ToInt16(IFormatProvider provider) => Common.Convert.ConvertValue<short>(this.Value);
	int IConvertible.ToInt32(IFormatProvider provider) => Common.Convert.ConvertValue<int>(this.Value);
	long IConvertible.ToInt64(IFormatProvider provider) => Common.Convert.ConvertValue<long>(this.Value);
	ushort IConvertible.ToUInt16(IFormatProvider provider) => Common.Convert.ConvertValue<ushort>(this.Value);
	uint IConvertible.ToUInt32(IFormatProvider provider) => Common.Convert.ConvertValue<uint>(this.Value);
	ulong IConvertible.ToUInt64(IFormatProvider provider) => Common.Convert.ConvertValue<ulong>(this.Value);
	float IConvertible.ToSingle(IFormatProvider provider) => Common.Convert.ConvertValue<float>(this.Value);
	double IConvertible.ToDouble(IFormatProvider provider) => Common.Convert.ConvertValue<double>(this.Value);
	decimal IConvertible.ToDecimal(IFormatProvider provider) => Common.Convert.ConvertValue<decimal>(this.Value);
	string IConvertible.ToString(IFormatProvider provider) => Common.Convert.ConvertValue<string>(this.Value);
	DateTime IConvertible.ToDateTime(IFormatProvider provider) => Common.Convert.ConvertValue<DateTime>(this.Value);
	object IConvertible.ToType(Type conversionType, IFormatProvider provider) => Common.Convert.ConvertValue(this.Value, conversionType);
	TypeCode IConvertible.GetTypeCode() => this.Value == null ? TypeCode.Empty : Type.GetTypeCode(this.Value.GetType());
	#endregion

	#region 解析方法
	public static Identifier Parse(ReadOnlySpan<char> text) => TryParse(text, out var result) ? result : throw new InvalidOperationException($"Invalid format of the identifier.");
	public static bool TryParse(ReadOnlySpan<char> text, out Identifier result)
	{
		if(text.IsEmpty)
		{
			result = default;
			return false;
		}

		result = default;
		return false;
	}
	#endregion

	#region 嵌套子类
	private sealed class TypeConverter : System.ComponentModel.TypeConverter
	{
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
		{
			return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
		}

		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
		{
			return destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
		}

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			if(value is string text && Identifier.TryParse(text, out var result))
				return result;

			return base.ConvertFrom(context, culture, value);
		}

		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			if(value is Identifier identifier && destinationType == typeof(string))
				return identifier.ToString();

			return base.ConvertTo(context, culture, value, destinationType);
		}
	}

	private sealed class JsonConverter : JsonConverter<Identifier>
	{
		public override Identifier Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			return reader.TokenType switch
			{
				JsonTokenType.String => new(typeof(object), reader.GetString()),
				JsonTokenType.Number => reader.TryGetInt32(out var integer) ? new(typeof(object), integer) : new(typeof(object), reader.GetDouble()),
				JsonTokenType.True => new(typeof(object), true),
				JsonTokenType.False => new(typeof(object), false),
				JsonTokenType.Null => default,
				_ => default,
			};
		}

		public override void Write(Utf8JsonWriter writer, Identifier value, JsonSerializerOptions options)
		{
			writer.WriteStringValue(value.ToString());
		}
	}
	#endregion
}

/// <summary>
/// 表示对象标识的结构。
/// </summary>
public readonly struct Identifier<T>(Type type, T value, string label = null, string description = null) : IEquatable<Identifier<T>> where T : IEquatable<T>
{
	#region 公共字段
	/// <summary>获取标识的对象类型。</summary>
	public readonly Type Type = type ?? throw new ArgumentNullException(nameof(type));
	/// <summary>获取标识键值。</summary>
	public readonly T Value = value;
	/// <summary>获取标识的标签。</summary>
	public readonly string Label = label;
	/// <summary>获取标识的描述。</summary>
	public readonly string Description = description;
	#endregion

	#region 公共属性
	/// <summary>获取一个值，指示当前对象标识是否为一个空标识。</summary>
	public bool IsEmpty => this.Type == null || this.Value == null;

	/// <summary>获取一个值，指示当前对象标识是否具有键值。</summary>
	public bool HasValue => this.Type != null && this.Value != null;
	#endregion

	#region 公共方法
	public bool Validate(out T value) => Common.Convert.TryConvertValue<T>(this.Value, out value);
	public bool Validate<TType>(out T value) => this.Validate(typeof(TType), out value);
	public bool Validate(Type type, out T value)
	{
		if(type != null && type.IsAssignableFrom(this.Type))
			return Common.Convert.TryConvertValue<T>(this.Value, out value);

		value = default;
		return false;
	}
	#endregion

	#region 重写方法
	public bool Equals(Identifier<T> other) => this.Type == other.Type && object.Equals(this.Value, other.Value);
	public override bool Equals(object obj) => obj is Identifier<T> other && this.Equals(other);
	public override int GetHashCode() => this.Value is string text ? HashCode.Combine(this.Type, text.ToUpperInvariant()) : HashCode.Combine(this.Type, this.Value);
	public override string ToString()
	{
		if(this.IsEmpty)
			return string.Empty;

		string text = null;

		if(!string.IsNullOrEmpty(this.Label))
			text += $"{nameof(this.Label)}={this.Label};";
		if(!string.IsNullOrEmpty(this.Description))
			text += $"{nameof(this.Description)}={this.Description};";

		//对标识值文本进行字符转义
		var value = this.Value?.ToString()
			.Replace("{", @"\{")
			.Replace("}", @"\}");

		if(string.IsNullOrEmpty(text))
			return $"{TypeAlias.GetAlias(this.Type)}:{value}";
		else
			return $"{TypeAlias.GetAlias(this.Type)}:{value}" + '{' + text.Trim(';') + '}';
	}
	#endregion

	#region 符号重写
	public static bool operator ==(Identifier<T> left, Identifier<T> right) => left.Equals(right);
	public static bool operator !=(Identifier<T> left, Identifier<T> right) => !(left == right);
	#endregion

	#region 类型转换
	public static implicit operator T(Identifier<T> identifier) => identifier.Value;
	public static implicit operator Identifier(Identifier<T> identifier) => new(identifier.Type, identifier.Value, identifier.Label, identifier.Description);
	public static explicit operator Identifier<T>(Identifier identifier) => new(identifier.Type, Common.Convert.ConvertValue<T>(identifier.Value), identifier.Label, identifier.Description);
	#endregion

	#region 解析方法
	public static Identifier<T> Parse(ReadOnlySpan<char> text) => TryParse(text, out var result) ? result : throw new InvalidOperationException($"Invalid format of the identifier.");
	public static bool TryParse(ReadOnlySpan<char> text, out Identifier<T> result)
	{
		result = default;
		return false;
	}
	#endregion
}
