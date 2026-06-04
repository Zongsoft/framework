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
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Zongsoft.Versioning;

/// <summary>表示语义化版本的类，包含主版本号、次版本号、修订号、标签和额外信息等属性。格式：<c>major.minor.patch-label+extra</c>。</summary>
[TypeConverter(typeof(VersionTypeConverter))]
[JsonConverter(typeof(VersionJsonConverter))]
public partial class Version : IFormattable, IComparable, IComparable<Version>, IEquatable<Version>, IParsable<Version>
{
	#region 成员字段
	private readonly int _hashcode;
	#endregion

	#region 构造函数
	public Version(int major, int minor, int patch, string label = null, string extra = null)
	{
		if(major < 0)
			throw new ArgumentOutOfRangeException(nameof(major));
		if(minor < 0)
			throw new ArgumentOutOfRangeException(nameof(minor));
		if(patch < 0)
			throw new ArgumentOutOfRangeException(nameof(patch));

		this.Major = major;
		this.Minor = minor;
		this.Patch = patch;
		this.Label = string.IsNullOrWhiteSpace(label) ? null : label.Trim();
		this.Extra = string.IsNullOrWhiteSpace(extra) ? null : extra.Trim();

		var hashCode = new HashCode();

		hashCode.Add(major);
		hashCode.Add(minor);
		hashCode.Add(patch);

		if(!string.IsNullOrWhiteSpace(label))
			hashCode.Add(label.Trim().ToLowerInvariant());

		_hashcode = hashCode.ToHashCode();
	}
	#endregion

	#region 公共属性
	/// <summary>获取主版本号。</summary>
	public int Major { get; }
	/// <summary>获取次版本号。</summary>
	public int Minor { get; }
	/// <summary>获取修订号。</summary>
	public int Patch { get; }
	/// <summary>获取标签。</summary>
	public string Label { get; }
	/// <summary>获取额外信息。</summary>
	public string Extra { get; }
	/// <summary>获取一个值，指示当前版本是否为空。</summary>
	public bool IsEmpty => this.Major == 0 && this.Minor == 0 && this.Patch == 0 && string.IsNullOrEmpty(this.Label) && string.IsNullOrEmpty(this.Extra);
	#endregion

	#region 公共方法
	/// <summary>检查当前版本是否有标签。</summary>
	/// <returns>如果当前版本有标签，则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
	public bool HasLabel() => !string.IsNullOrEmpty(this.Label);

	/// <summary>获取标签。</summary>
	/// <param name="label">输出参数，用于接收标签值。</param>
	/// <returns>如果当前版本有标签，则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
	public bool HasLabel(out string label)
	{
		if(string.IsNullOrEmpty(this.Label))
		{
			label = null;
			return false;
		}

		label = this.Label;
		return true;
	}

	/// <summary>检查当前版本是否有额外信息。</summary>
	/// <returns>如果当前版本有额外信息，则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
	public bool HasExtra() => !string.IsNullOrEmpty(this.Extra);

	/// <summary>获取额外信息。</summary>
	/// <param name="extra">输出参数，用于接收额外信息值。</param>
	/// <returns>如果当前版本有额外信息，则返回 <c>true</c>；否则返回 <c>false</c>。</returns>
	public bool HasExtra(out string extra)
	{
		if(string.IsNullOrEmpty(this.Extra))
		{
			extra = null;
			return false;
		}

		extra = this.Extra;
		return true;
	}

	/// <summary>将指定的字符串解析成版本对象。</summary>
	public static Version Parse(string text, IFormatProvider provider = null) => TryParse(text, provider, out var result) ? result : throw new FormatException();

	/// <summary>尝试将指定的字符串解析成版本对象。</summary>
	public static bool TryParse(string text, out Version result) => TryParse(text, null, out result);

	/// <summary>尝试将指定的字符串解析成版本对象。</summary>
	public static bool TryParse(ReadOnlySpan<char> text, out Version result) => TryParse(text.IsEmpty ? null : text.ToString(), null, out result);

	/// <summary>尝试将指定的字符串解析成版本对象。</summary>
	public static bool TryParse(string text, IFormatProvider provider, out Version result)
	{
		return Parser.TryParse(text, out result);
	}
	#endregion

	#region 重写方法
	public virtual int CompareTo(object obj) => this.CompareTo(obj as Version);
	public virtual int CompareTo(Version other) => Comparer.Compare(this, other);
	public override bool Equals(object obj) => this.Equals(obj as Version);
	public virtual bool Equals(Version other) => Comparer.Equals(this, other);
	public override int GetHashCode() => _hashcode;
	public override string ToString() => this.ToString("N");
	public virtual string ToString(string format, IFormatProvider provider = null)
	{
		if(provider != null && provider.GetFormat(this.GetType()) is ICustomFormatter formatter)
			return formatter.Format(format, this, provider);

		if(string.IsNullOrEmpty(format))
			format = "N";

		var builder = new StringBuilder();

		foreach(var character in format)
			Append(builder, character, this);

		return builder.ToString();

		static void Append(StringBuilder builder, char format, Version version)
		{
			switch(format)
			{
				case 'N':
					AppendVersion();
					AppendLabel();
					break;
				case 'V':
					AppendVersion();
					break;
				case 'F':
					AppendVersion();
					AppendLabel();
					AppendExtra();
					break;
				case 'R':
					builder.Append(version.Label);
					break;
				case 'M':
					builder.Append(version.Extra);
					break;
				case 'x':
					builder.Append(version.Major);
					break;
				case 'y':
					builder.Append(version.Minor);
					break;
				case 'z':
					builder.Append(version.Patch);
					break;
				case 'r':
					builder.Append(0);
					break;
				default:
					builder.Append(format);
					break;
			}

			void AppendVersion()
			{
				builder.Append(version.Major);
				builder.Append('.');
				builder.Append(version.Minor);
				builder.Append('.');
				builder.Append(version.Patch);
			}

			void AppendLabel()
			{
				if(version.HasLabel(out var label))
				{
					builder.Append('-');
					builder.Append(label);
				}
			}

			void AppendExtra()
			{
				if(version.HasExtra(out var extra))
				{
					builder.Append('+');
					builder.Append(extra);
				}
			}
		}
	}
	#endregion

	#region 符号重写
	public static bool operator ==(Version left, Version right) => Comparer.Equals(left, right);
	public static bool operator !=(Version left, Version right) => !Comparer.Equals(left, right);
	public static bool operator >(Version left, Version right) => Comparer.Compare(left, right) > 0;
	public static bool operator >=(Version left, Version right) => Comparer.Compare(left, right) >= 0;
	public static bool operator <(Version left, Version right) => Comparer.Compare(left, right) < 0;
	public static bool operator <=(Version left, Version right) => Comparer.Compare(left, right) <= 0;
	#endregion

	#region 嵌套子类
	private static class Parser
	{
		public static bool TryParse(string text, out Version result)
		{
			result = null;

			if(string.IsNullOrWhiteSpace(text))
				return false;

			text = text.Trim();

			if(!TryParseSections(text, out var version, out var label, out var extra))
				return false;

			var parts = version.Split('.');

			if(parts.Length != 3 ||
			   !TryParseNumber(parts[0], out var major) ||
			   !TryParseNumber(parts[1], out var minor) ||
			   !TryParseNumber(parts[2], out var patch) ||
			   label != null && !IsValidIdentifiers(label, false) ||
			   extra != null && !IsValidIdentifiers(extra, true))
				return false;

			result = new Version(major, minor, patch, label, extra);
			return true;
		}

		private static bool TryParseSections(string text, out string version, out string label, out string extra)
		{
			version = null;
			label = null;
			extra = null;

			var dash = text.IndexOf('-');
			var plus = text.IndexOf('+');

			if(plus >= 0 && text.IndexOf('+', plus + 1) >= 0)
				return false;

			var hasLabel = dash >= 0 && (plus < 0 || dash < plus);
			var versionEnd = hasLabel ? dash : plus >= 0 ? plus : text.Length;

			if(versionEnd <= 0)
				return false;

			version = text[..versionEnd];

			if(hasLabel)
			{
				var labelEnd = plus >= 0 ? plus : text.Length;

				if(labelEnd <= dash + 1)
					return false;

				label = text[(dash + 1)..labelEnd];
			}

			if(plus >= 0)
			{
				if(plus == text.Length - 1)
					return false;

				extra = text[(plus + 1)..];
			}

			return true;
		}

		private static bool TryParseNumber(string text, out int number)
		{
			number = default;

			return IsNumeric(text, false) && int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out number);
		}

		private static bool IsValidIdentifiers(string text, bool allowLeadingZeros)
		{
			foreach(var part in text.Split('.'))
			{
				if(string.IsNullOrEmpty(part) ||
				   !allowLeadingZeros && IsNumeric(part, true) && !IsNumeric(part, false))
					return false;

				foreach(var character in part)
				{
					if(character is not (>= '0' and <= '9' or >= 'A' and <= 'Z' or >= 'a' and <= 'z' or '-'))
						return false;
				}
			}

			return true;
		}

		private static bool IsNumeric(string text, bool allowLeadingZeros)
		{
			if(string.IsNullOrEmpty(text) || !allowLeadingZeros && text.Length > 1 && text[0] == '0')
				return false;

			foreach(var character in text)
			{
				if(character is not (>= '0' and <= '9'))
					return false;
			}

			return true;
		}
	}

	private static class Comparer
	{
		public static bool Equals(Version left, Version right)
		{
			if(ReferenceEquals(left, right))
				return true;
			if(left is null || right is null)
				return false;

			return left.Major == right.Major &&
			       left.Minor == right.Minor &&
			       left.Patch == right.Patch &&
			       LabelsEqual(left.Label, right.Label);
		}

		public static int Compare(Version left, Version right)
		{
			if(ReferenceEquals(left, right))
				return 0;
			if(right is null)
				return 1;
			if(left is null)
				return -1;

			var result = left.Major.CompareTo(right.Major);
			if(result != 0)
				return result;

			result = left.Minor.CompareTo(right.Minor);
			if(result != 0)
				return result;

			result = left.Patch.CompareTo(right.Patch);
			if(result != 0)
				return result;

			var leftHasLabel = left.HasLabel(out var leftLabel);
			var rightHasLabel = right.HasLabel(out var rightLabel);

			if(leftHasLabel != rightHasLabel)
				return leftHasLabel ? -1 : 1;

			return leftHasLabel ? CompareLabels(leftLabel, rightLabel) : 0;
		}

		private static bool LabelsEqual(string left, string right)
		{
			var leftHasLabel = !string.IsNullOrEmpty(left);
			var rightHasLabel = !string.IsNullOrEmpty(right);

			if(!leftHasLabel || !rightHasLabel)
				return leftHasLabel == rightHasLabel;

			var leftLabels = left.Split('.');
			var rightLabels = right.Split('.');

			if(leftLabels.Length != rightLabels.Length)
				return false;

			for(var i = 0; i < leftLabels.Length; i++)
			{
				if(!StringComparer.OrdinalIgnoreCase.Equals(leftLabels[i], rightLabels[i]))
					return false;
			}

			return true;
		}

		private static int CompareLabels(string left, string right)
		{
			var leftLabels = left.Split('.');
			var rightLabels = right.Split('.');
			var count = Math.Max(leftLabels.Length, rightLabels.Length);

			for(var i = 0; i < count; i++)
			{
				if(i >= leftLabels.Length)
					return -1;
				if(i >= rightLabels.Length)
					return 1;

				var leftIsNumber = int.TryParse(leftLabels[i], NumberStyles.None, CultureInfo.InvariantCulture, out var leftNumber);
				var rightIsNumber = int.TryParse(rightLabels[i], NumberStyles.None, CultureInfo.InvariantCulture, out var rightNumber);
				var result = leftIsNumber && rightIsNumber ?
					leftNumber.CompareTo(rightNumber) :
					leftIsNumber != rightIsNumber ?
						leftIsNumber ? -1 : 1 :
						StringComparer.OrdinalIgnoreCase.Compare(leftLabels[i], rightLabels[i]);

				if(result != 0)
					return result;
			}

			return 0;
		}
	}

	private sealed class VersionTypeConverter : TypeConverter
	{
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) => value is string text ? Parse(text, culture) : base.ConvertFrom(context, culture, value);
		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) => value is Version version && destinationType == typeof(string) ? version.ToString() : base.ConvertTo(context, culture, value, destinationType);
		public override bool IsValid(ITypeDescriptorContext context, object value) => value is Version || value is string text && TryParse(text, out _);
	}

	private sealed class VersionJsonConverter : JsonConverter<Version>
	{
		public override Version Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => reader.TokenType switch
		{
			JsonTokenType.Null => null,
			JsonTokenType.String => TryParse(reader.GetString(), out var result) ? result : throw new JsonException(),
			_ => throw new JsonException(),
		};

		public override void Write(Utf8JsonWriter writer, Version value, JsonSerializerOptions options)
		{
			if(value == null)
				writer.WriteNullValue();
			else
				writer.WriteStringValue(value.ToString("F"));
		}
	}
	#endregion
}
