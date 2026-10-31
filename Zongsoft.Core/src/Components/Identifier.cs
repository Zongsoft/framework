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
using System.ComponentModel;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Zongsoft.Components;

/// <summary>表示具有逻辑名称或对象类型的类型化标识。</summary>
[TypeConverter(typeof(IdentifierConverter))]
[JsonConverter(typeof(IdentifierJsonConverter))]
public readonly partial struct Identifier : IEquatable<Identifier>, IConvertible, ISpanParsable<Identifier>, IFormattable
{
	#region 构造函数
	public Identifier(string name, object value, string label = null, string description = null, string alias = null, IDictionary<string, string> properties = null, string code = null) :
		this(name, null, value, label, description, alias, code, properties, true) { }
	public Identifier(Type type, object value, string label = null, string description = null, string alias = null, IDictionary<string, string> properties = null, string code = null) :
		this(null, type ?? throw new ArgumentNullException(nameof(type)), value, label, description, alias, code, properties, true) { }

	internal Identifier(string name, Type type, object value, string label, string description, string alias, string code, IDictionary<string, string> properties, bool copy)
	{
		if(type == null && !IsName(name))
			throw new ArgumentException(Zongsoft.Properties.Resources.Identifier_InvalidFormat_Message, nameof(name));

		this.Name = name;
		this.Type = type;
		this.Value = value;
		this.Code = code;
		this.Alias = alias;
		this.Label = label;
		this.Description = description;
		this.Properties = copy && properties != null ? new Dictionary<string, string>(properties, StringComparer.OrdinalIgnoreCase) : properties;
	}
	#endregion

	#region 公共字段
	public readonly string Name;
	public readonly Type Type;
	public readonly object Value;
	public readonly string Code;
	public readonly string Label;
	public readonly string Alias;
	public readonly string Description;
	#endregion

	#region 公共属性
	public IDictionary<string, string> Properties { get; }
	public bool IsEmpty => (this.Type == null && this.Name == null) || this.Value == null;
	public bool HasValue => !this.IsEmpty;
	#endregion

	#region 公共方法
	public Identifier Cast<TType>() => this.IsEmpty ? default : new(null, typeof(TType), this.Value, this.Label, this.Description, this.Alias, this.Code, this.Properties, false);
	public Identifier<TValue> Cast<TType, TValue>() => (Identifier<TValue>)this.Cast<TType>();
	public bool Validate<TValue>(out TValue value)
	{
		if(TryConvert(this.Value, typeof(TValue), out var converted))
		{
			value = (TValue)converted;
			return true;
		}

		value = default;
		return false;
	}
	public bool Validate(string name) => this.HasValue && string.Equals(this.Name, name, StringComparison.OrdinalIgnoreCase);
	public bool Validate<TType>() => this.HasValue && this.Type != null && typeof(TType).IsAssignableFrom(this.Type);
	public bool Validate<TType, TValue>(out TValue value) => this.Validate(typeof(TType), out value);
	public bool Validate<TValue>(string name, out TValue value)
	{
		value = default;
		return this.Validate(name) && this.Validate(out value);
	}
	public bool Validate<TValue>(Type type, out TValue value)
	{
		value = default;
		return type != null && this.Type != null && type.IsAssignableFrom(this.Type) && this.Validate(out value);
	}
	#endregion

	#region 重写方法
	public bool Equals(Identifier other) => this.IsEmpty ? other.IsEmpty : !other.IsEmpty && this.Type == other.Type &&
		string.Equals(this.Name, other.Name, StringComparison.OrdinalIgnoreCase) && Equals(this.Value, other.Value);
	public override bool Equals(object obj) => obj is Identifier other && this.Equals(other);
	public override int GetHashCode() => this.IsEmpty ? 0 : HashCode.Combine(this.Type, this.Name == null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(this.Name), this.Value);
	public override string ToString() => this.ToString(null, null);
	public string ToString(string format) => this.ToString(format, null);
	public string ToString(string format, IFormatProvider provider) => Format(this, format);
	#endregion

	#region 解析方法
	public static Identifier Parse(string text) => Parse(text, null);
	public static Identifier Parse(string text, IFormatProvider provider) => text == null ? throw new ArgumentNullException(nameof(text)) : Parse(text.AsSpan(), provider);
	public static Identifier Parse(ReadOnlySpan<char> text) => Parse(text, null);
	public static Identifier Parse(ReadOnlySpan<char> text, IFormatProvider provider) => TryParse(text, provider, out var result) ? result : throw new FormatException(Zongsoft.Properties.Resources.Identifier_InvalidFormat_Message);
	public static bool TryParse(string text, out Identifier result) => TryParse(text, null, out result);
	public static bool TryParse(string text, IFormatProvider provider, out Identifier result)
	{
		result = default;
		return text != null && TryParse(text.AsSpan(), provider, out result);
	}
	public static bool TryParse(ReadOnlySpan<char> text, out Identifier result) => TryParse(text, null, out result);
	public static bool TryParse(ReadOnlySpan<char> text, IFormatProvider provider, out Identifier result) => TryParseCore(text, null, out result);
	#endregion

	#region 符号重写
	public static bool operator ==(Identifier left, Identifier right) => left.Equals(right);
	public static bool operator !=(Identifier left, Identifier right) => !left.Equals(right);
	#endregion

	#region 类型转换
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
}
