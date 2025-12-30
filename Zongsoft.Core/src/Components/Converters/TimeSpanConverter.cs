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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
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

using Zongsoft.Common;

namespace Zongsoft.Components.Converters;

public class TimeSpanConverter : TypeConverter
{
	public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string);
	public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) => value is string text ? TimeSpanUtility.Parse(text) : null;

	public sealed class Days : TypeConverter
	{
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType.IsNumeric();
		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => destinationType.IsNumeric();

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) => value switch
		{
			byte byteValue => TimeSpan.FromDays(byteValue),
			sbyte sbyteValue => TimeSpan.FromDays(sbyteValue),
			short int16Value => TimeSpan.FromDays(int16Value),
			int int32Value => TimeSpan.FromDays(int32Value),
			long int64Value => TimeSpan.FromDays(int64Value),
			ushort uint16Value => TimeSpan.FromDays(uint16Value),
			uint uint32Value => TimeSpan.FromDays(uint32Value),
			ulong uint64Value => TimeSpan.FromDays(uint64Value),
			float singleValue => TimeSpan.FromDays(singleValue),
			double doubleValue => TimeSpan.FromDays(doubleValue),
			decimal decimalValue => TimeSpan.FromDays((double)decimalValue),
			_ => null,
		};

		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) => Type.GetTypeCode(destinationType) switch
		{
			TypeCode.Byte => (byte)((TimeSpan)value).TotalDays,
			TypeCode.SByte => (sbyte)((TimeSpan)value).TotalDays,
			TypeCode.Int16 => (short)((TimeSpan)value).TotalDays,
			TypeCode.Int32 => (int)((TimeSpan)value).TotalDays,
			TypeCode.Int64 => (long)((TimeSpan)value).TotalDays,
			TypeCode.UInt16 => (ushort)((TimeSpan)value).TotalDays,
			TypeCode.UInt32 => (uint)((TimeSpan)value).TotalDays,
			TypeCode.UInt64 => (ulong)((TimeSpan)value).TotalDays,
			TypeCode.Single => (float)((TimeSpan)value).TotalDays,
			TypeCode.Double => (double)((TimeSpan)value).TotalDays,
			TypeCode.Decimal => (decimal)((TimeSpan)value).TotalDays,
			_ => null,
		};
	}

	public sealed class Hours : TypeConverter
	{
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType.IsNumeric();
		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => destinationType.IsNumeric();

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) => value switch
		{
			byte byteValue => TimeSpan.FromHours(byteValue),
			sbyte sbyteValue => TimeSpan.FromHours(sbyteValue),
			short int16Value => TimeSpan.FromHours(int16Value),
			int int32Value => TimeSpan.FromHours(int32Value),
			long int64Value => TimeSpan.FromHours(int64Value),
			ushort uint16Value => TimeSpan.FromHours(uint16Value),
			uint uint32Value => TimeSpan.FromHours(uint32Value),
			ulong uint64Value => TimeSpan.FromHours(uint64Value),
			float singleValue => TimeSpan.FromHours(singleValue),
			double doubleValue => TimeSpan.FromHours(doubleValue),
			decimal decimalValue => TimeSpan.FromHours((double)decimalValue),
			_ => null,
		};

		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) => Type.GetTypeCode(destinationType) switch
		{
			TypeCode.Byte => (byte)((TimeSpan)value).TotalHours,
			TypeCode.SByte => (sbyte)((TimeSpan)value).TotalHours,
			TypeCode.Int16 => (short)((TimeSpan)value).TotalHours,
			TypeCode.Int32 => (int)((TimeSpan)value).TotalHours,
			TypeCode.Int64 => (long)((TimeSpan)value).TotalHours,
			TypeCode.UInt16 => (ushort)((TimeSpan)value).TotalHours,
			TypeCode.UInt32 => (uint)((TimeSpan)value).TotalHours,
			TypeCode.UInt64 => (ulong)((TimeSpan)value).TotalHours,
			TypeCode.Single => (float)((TimeSpan)value).TotalHours,
			TypeCode.Double => (double)((TimeSpan)value).TotalHours,
			TypeCode.Decimal => (decimal)((TimeSpan)value).TotalHours,
			_ => null,
		};
	}

	public sealed class Minutes : TypeConverter
	{
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType.IsNumeric();
		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => destinationType.IsNumeric();

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) => value switch
		{
			byte byteValue => TimeSpan.FromMinutes(byteValue),
			sbyte sbyteValue => TimeSpan.FromMinutes(sbyteValue),
			short int16Value => TimeSpan.FromMinutes(int16Value),
			int int32Value => TimeSpan.FromMinutes(int32Value),
			long int64Value => TimeSpan.FromMinutes(int64Value),
			ushort uint16Value => TimeSpan.FromMinutes(uint16Value),
			uint uint32Value => TimeSpan.FromMinutes(uint32Value),
			ulong uint64Value => TimeSpan.FromMinutes(uint64Value),
			float singleValue => TimeSpan.FromMinutes(singleValue),
			double doubleValue => TimeSpan.FromMinutes(doubleValue),
			decimal decimalValue => TimeSpan.FromMinutes((double)decimalValue),
			_ => null,
		};

		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) => Type.GetTypeCode(destinationType) switch
		{
			TypeCode.Byte => (byte)((TimeSpan)value).TotalMinutes,
			TypeCode.SByte => (sbyte)((TimeSpan)value).TotalMinutes,
			TypeCode.Int16 => (short)((TimeSpan)value).TotalMinutes,
			TypeCode.Int32 => (int)((TimeSpan)value).TotalMinutes,
			TypeCode.Int64 => (long)((TimeSpan)value).TotalMinutes,
			TypeCode.UInt16 => (ushort)((TimeSpan)value).TotalMinutes,
			TypeCode.UInt32 => (uint)((TimeSpan)value).TotalMinutes,
			TypeCode.UInt64 => (ulong)((TimeSpan)value).TotalMinutes,
			TypeCode.Single => (float)((TimeSpan)value).TotalMinutes,
			TypeCode.Double => (double)((TimeSpan)value).TotalMinutes,
			TypeCode.Decimal => (decimal)((TimeSpan)value).TotalMinutes,
			_ => null,
		};
	}

	public sealed class Seconds : TypeConverter
	{
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType.IsNumeric();
		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => destinationType.IsNumeric();

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) => value switch
		{
			byte byteValue => TimeSpan.FromSeconds(byteValue),
			sbyte sbyteValue => TimeSpan.FromSeconds(sbyteValue),
			short int16Value => TimeSpan.FromSeconds(int16Value),
			int int32Value => TimeSpan.FromSeconds(int32Value),
			long int64Value => TimeSpan.FromSeconds(int64Value),
			ushort uint16Value => TimeSpan.FromSeconds(uint16Value),
			uint uint32Value => TimeSpan.FromSeconds(uint32Value),
			ulong uint64Value => TimeSpan.FromSeconds(uint64Value),
			float singleValue => TimeSpan.FromSeconds(singleValue),
			double doubleValue => TimeSpan.FromSeconds(doubleValue),
			decimal decimalValue => TimeSpan.FromSeconds((double)decimalValue),
			_ => null,
		};

		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) => Type.GetTypeCode(destinationType) switch
		{
			TypeCode.Byte => (byte)((TimeSpan)value).TotalSeconds,
			TypeCode.SByte => (sbyte)((TimeSpan)value).TotalSeconds,
			TypeCode.Int16 => (short)((TimeSpan)value).TotalSeconds,
			TypeCode.Int32 => (int)((TimeSpan)value).TotalSeconds,
			TypeCode.Int64 => (long)((TimeSpan)value).TotalSeconds,
			TypeCode.UInt16 => (ushort)((TimeSpan)value).TotalSeconds,
			TypeCode.UInt32 => (uint)((TimeSpan)value).TotalSeconds,
			TypeCode.UInt64 => (ulong)((TimeSpan)value).TotalSeconds,
			TypeCode.Single => (float)((TimeSpan)value).TotalSeconds,
			TypeCode.Double => (double)((TimeSpan)value).TotalSeconds,
			TypeCode.Decimal => (decimal)((TimeSpan)value).TotalSeconds,
			_ => null,
		};
	}

	public sealed class Milliseconds : TypeConverter
	{
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType.IsNumeric();
		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => destinationType.IsNumeric();

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) => value switch
		{
			byte byteValue => TimeSpan.FromMilliseconds(byteValue),
			sbyte sbyteValue => TimeSpan.FromMilliseconds(sbyteValue),
			short int16Value => TimeSpan.FromMilliseconds(int16Value),
			int int32Value => TimeSpan.FromMilliseconds(int32Value),
			long int64Value => TimeSpan.FromMilliseconds(int64Value),
			ushort uint16Value => TimeSpan.FromMilliseconds(uint16Value),
			uint uint32Value => TimeSpan.FromMilliseconds(uint32Value),
			ulong uint64Value => TimeSpan.FromMilliseconds(uint64Value),
			float singleValue => TimeSpan.FromMilliseconds(singleValue),
			double doubleValue => TimeSpan.FromMilliseconds(doubleValue),
			decimal decimalValue => TimeSpan.FromMilliseconds((double)decimalValue),
			_ => null,
		};

		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) => Type.GetTypeCode(destinationType) switch
		{
			TypeCode.Byte => (byte)((TimeSpan)value).TotalMilliseconds,
			TypeCode.SByte => (sbyte)((TimeSpan)value).TotalMilliseconds,
			TypeCode.Int16 => (short)((TimeSpan)value).TotalMilliseconds,
			TypeCode.Int32 => (int)((TimeSpan)value).TotalMilliseconds,
			TypeCode.Int64 => (long)((TimeSpan)value).TotalMilliseconds,
			TypeCode.UInt16 => (ushort)((TimeSpan)value).TotalMilliseconds,
			TypeCode.UInt32 => (uint)((TimeSpan)value).TotalMilliseconds,
			TypeCode.UInt64 => (ulong)((TimeSpan)value).TotalMilliseconds,
			TypeCode.Single => (float)((TimeSpan)value).TotalMilliseconds,
			TypeCode.Double => (double)((TimeSpan)value).TotalMilliseconds,
			TypeCode.Decimal => (decimal)((TimeSpan)value).TotalMilliseconds,
			_ => null,
		};
	}
}
