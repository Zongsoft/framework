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

namespace Zongsoft.Versioning;

partial class Version
{
	/// <summary>表示版本号的结构体，适用于版本号的数值比较和转换。</summary>
	[TypeConverter(typeof(NumberTypeConverter))]
	[JsonConverter(typeof(NumberJsonConverter))]
	public readonly struct Number : IEquatable<Number>, IComparable<Number>, IParsable<Number>
	{
		#region 构造函数
		public Number(long value) : this(unchecked((ulong)value)) { }
		public Number(ulong value)
		{
			this.Major = (ushort)((value & 0xFFFF_0000_0000_0000) >> (3 * sizeof(ushort) * 8));
			this.Minor = (ushort)((value & 0x0000_FFFF_0000_0000) >> (2 * sizeof(ushort) * 8));
			this.Patch = (ushort)((value & 0x0000_0000_FFFF_0000) >> (1 * sizeof(ushort) * 8));
			this.Revision = (ushort)(value & 0x0000_0000_0000_FFFF);
		}

		public Number(ushort major, ushort minor, ushort patch = 0, ushort revision = 0)
		{
			this.Major = major;
			this.Minor = minor;
			this.Patch = patch;
			this.Revision = revision;
		}
		#endregion

		#region 公共字段
		public readonly ushort Major;
		public readonly ushort Minor;
		public readonly ushort Patch;
		public readonly ushort Revision;
		#endregion

		#region 公共属性
		public bool IsZero => this.Major == 0 && this.Minor == 0 && this.Patch == 0 && this.Revision == 0;
		#endregion

		#region 解析方法
		public static Number Parse(string text, IFormatProvider provider = null) => TryParse(text, provider, out var result) ? result : throw new FormatException();
		public static bool TryParse(string text, out Number result) => TryParse(text, null, out result);
		public static bool TryParse(string text, IFormatProvider provider, out Number result)
		{
			if(string.IsNullOrEmpty(text))
			{
				result = default;
				return false;
			}

			var parts = text.Split('.');

			if(parts.Length > 1 && ushort.TryParse(parts[0], out var major) && ushort.TryParse(parts[1], out var minor))
			{
				if(parts.Length == 2)
				{
					result = new(major, minor);
					return true;
				}

				if(parts.Length == 3 && ushort.TryParse(parts[2], out var patch))
				{
					result = new(major, minor, patch);
					return true;
				}

				if(parts.Length == 4 && ushort.TryParse(parts[2], out patch) && ushort.TryParse(parts[3], out var revision))
				{
					result = new(major, minor, patch, revision);
					return true;
				}
			}

			result = default;
			return false;
		}
		#endregion

		#region 重写方法
		public bool Equals(Number other) =>
			this.Major == other.Major &&
			this.Minor == other.Minor &&
			this.Patch == other.Patch &&
			this.Revision == other.Revision;

		public int CompareTo(Number other)
		{
			var result = this.Major.CompareTo(other.Major);
			if(result != 0) return result;

			result = this.Minor.CompareTo(other.Minor);
			if(result != 0) return result;

			result = this.Patch.CompareTo(other.Patch);
			if(result != 0) return result;

			return this.Revision.CompareTo(other.Revision);
		}

		public override bool Equals(object obj) => obj is Number other && this.Equals(other);
		public override int GetHashCode() => HashCode.Combine(this.Major, this.Minor, this.Patch, this.Revision);
		public override string ToString() => this.Revision == 0 ? $"{this.Major}.{this.Minor}.{this.Patch}" : $"{this.Major}.{this.Minor}.{this.Patch}.{this.Revision}";
		#endregion

		#region 符号重写
		public static bool operator ==(Number left, Number right) => left.Equals(right);
		public static bool operator !=(Number left, Number right) => !(left == right);
		public static bool operator >(Number left, Number right) => left.CompareTo(right) > 0;
		public static bool operator >=(Number left, Number right) => left.CompareTo(right) >= 0;
		public static bool operator <(Number left, Number right) => left.CompareTo(right) < 0;
		public static bool operator <=(Number left, Number right) => left.CompareTo(right) <= 0;

		public static implicit operator Number(long value) => new(value);
		public static implicit operator Number(ulong value) => new(value);

		public static implicit operator long(Number number) => (long)(ulong)number;
		public static implicit operator ulong(Number number) =>
			((ulong)number.Major << (3 * sizeof(ushort) * 8)) +
			((ulong)number.Minor << (2 * sizeof(ushort) * 8)) +
			((ulong)number.Patch << (1 * sizeof(ushort) * 8)) + number.Revision;

		public static implicit operator Number(System.Version version) => version == null ? default : new((ushort)version.Major, (ushort)version.Minor, (ushort)Math.Max(version.Build, 0), (ushort)Math.Max(version.Revision, 0));
		public static implicit operator System.Version(Number version) => version.Revision == 0 ? new(version.Major, version.Minor, version.Patch) : new(version.Major, version.Minor, version.Patch, version.Revision);
		#endregion
	}

	private sealed class NumberJsonConverter : JsonConverter<Number>
	{
		public override Number Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) => reader.TokenType switch
		{
			JsonTokenType.Null => default,
			JsonTokenType.String => Number.Parse(reader.GetString()),
			JsonTokenType.Number => reader.TryGetUInt64(out var uint64) ? new Number(uint64) : reader.TryGetInt64(out var int64) ? new Number(int64) : throw new JsonException(),
			_ => throw new JsonException(),
		};

		public override void Write(Utf8JsonWriter writer, Number value, JsonSerializerOptions options) => writer.WriteStringValue(value.ToString());
	}

	private sealed class NumberTypeConverter : VersionConverter
	{
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(long) || sourceType == typeof(ulong) || base.CanConvertFrom(context, sourceType);
		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => destinationType == typeof(long) || destinationType == typeof(ulong) || base.CanConvertTo(context, destinationType);

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			if(value is long int64)
				return new Number(int64);
			if(value is ulong uint64)
				return new Number(uint64);
			if(value is string text)
				return Number.Parse(text, culture);
			if(value is System.Version version)
				return (Number)version;

			return base.ConvertFrom(context, culture, value);
		}

		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			if(value is Number number)
			{
				if(destinationType == typeof(long))
					return (long)number;
				if(destinationType == typeof(ulong))
					return (ulong)number;
				if(destinationType == typeof(string))
					return number.ToString();
				if(destinationType == typeof(System.Version))
					return (System.Version)number;
			}

			return base.ConvertTo(context, culture, value, destinationType);
		}

		public override bool IsValid(ITypeDescriptorContext context, object value) => value is Number || base.IsValid(context, value);
	}
}