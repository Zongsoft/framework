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
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
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

/// <summary>表示一个由透明度及红、绿、蓝三原色组成的颜色。</summary>
[TypeConverter(typeof(Color.TypeConverter))]
[JsonConverter(typeof(Color.JsonConverter))]
public readonly struct Color : IEquatable<Color>, IParsable<Color>
{
	#region 静态变量
	private static readonly (string Name, Color Value)[] _colors;
	#endregion

	#region 静态构造
	static Color()
	{
		_colors =
		[
			(nameof(Transparent), Transparent),
			(nameof(Black), Black),
			(nameof(White), White),
			(nameof(Red), Red),
			(nameof(Green), Green),
			(nameof(Blue), Blue),

			(nameof(Silver), Silver),
			(nameof(Gray), Gray),
			(nameof(DarkGray), DarkGray),
			(nameof(LightGray), LightGray),

			(nameof(Maroon), Maroon),
			(nameof(DarkRed), DarkRed),
			(nameof(Crimson), Crimson),
			(nameof(Pink), Pink),
			(nameof(Coral), Coral),
			(nameof(Salmon), Salmon),
			(nameof(Orange), Orange),
			(nameof(Brown), Brown),
			(nameof(Chocolate), Chocolate),

			(nameof(Yellow), Yellow),
			(nameof(Gold), Gold),
			(nameof(Olive), Olive),
			(nameof(Beige), Beige),

			(nameof(Lime), Lime),
			(nameof(DarkGreen), DarkGreen),

			(nameof(Navy), Navy),
			(nameof(DarkBlue), DarkBlue),
			(nameof(Teal), Teal),
			(nameof(Aqua), Aqua),
			(nameof(Cyan), Cyan),
			(nameof(Turquoise), Turquoise),

			(nameof(Purple), Purple),
			(nameof(Indigo), Indigo),
			(nameof(Violet), Violet),
			(nameof(Lavender), Lavender),
			(nameof(Fuchsia), Fuchsia),
			(nameof(Magenta), Magenta),
		];
	}
	#endregion

	#region 实例字段
	private readonly uint _value;
	#endregion

	#region 构造函数
	public Color(uint value) => _value = value;
	public Color(byte red, byte green, byte blue) => _value = 0xFF000000u | (uint)red << 16 | (uint)green << 8 | blue;
	public Color(byte alpha, byte red, byte green, byte blue) => _value = (uint)alpha << 24 | (uint)red << 16 | (uint)green << 8 | blue;
	#endregion

	#region 公共属性
	/// <summary>获取三十二位 ARGB 颜色值。</summary>
	public uint Value => _value;
	/// <summary>获取透明通道分量。</summary>
	public byte Alpha => (byte)(_value >> 24);
	#endregion

	#region 公共方法
	/// <summary>获取当前颜色的红、绿、蓝三原色分量。</summary>
	/// <param name="red">返回红色分量。</param>
	/// <param name="green">返回绿色分量。</param>
	/// <param name="blue">返回蓝色分量。</param>
	public void GetRgb(out byte red, out byte green, out byte blue)
	{
		red = (byte)(_value >> 16);
		green = (byte)(_value >> 8);
		blue = (byte)_value;
	}
	#endregion

	#region 标准颜色
	/// <summary>透明色。</summary>
	public static readonly Color Transparent = new(0x00, 0x00, 0x00, 0x00);
	/// <summary>黑色。</summary>
	public static readonly Color Black = new(0x00, 0x00, 0x00);
	/// <summary>白色。</summary>
	public static readonly Color White = new(0xFF, 0xFF, 0xFF);
	/// <summary>红色。</summary>
	public static readonly Color Red = new(0xFF, 0x00, 0x00);
	/// <summary>绿色。</summary>
	public static readonly Color Green = new(0x00, 0x80, 0x00);
	/// <summary>蓝色。</summary>
	public static readonly Color Blue = new(0x00, 0x00, 0xFF);

	/// <summary>银色。</summary>
	public static readonly Color Silver = new(0xC0, 0xC0, 0xC0);
	/// <summary>灰色。</summary>
	public static readonly Color Gray = new(0x80, 0x80, 0x80);
	/// <summary>深灰色。</summary>
	public static readonly Color DarkGray = new(0xA9, 0xA9, 0xA9);
	/// <summary>浅灰色。</summary>
	public static readonly Color LightGray = new(0xD3, 0xD3, 0xD3);

	/// <summary>栗色。</summary>
	public static readonly Color Maroon = new(0x80, 0x00, 0x00);
	/// <summary>深红色。</summary>
	public static readonly Color DarkRed = new(0x8B, 0x00, 0x00);
	/// <summary>绯红色。</summary>
	public static readonly Color Crimson = new(0xDC, 0x14, 0x3C);
	/// <summary>粉红色。</summary>
	public static readonly Color Pink = new(0xFF, 0xC0, 0xCB);
	/// <summary>珊瑚色。</summary>
	public static readonly Color Coral = new(0xFF, 0x7F, 0x50);
	/// <summary>鲑鱼色。</summary>
	public static readonly Color Salmon = new(0xFA, 0x80, 0x72);
	/// <summary>橙色。</summary>
	public static readonly Color Orange = new(0xFF, 0xA5, 0x00);
	/// <summary>棕色。</summary>
	public static readonly Color Brown = new(0xA5, 0x2A, 0x2A);
	/// <summary>巧克力色。</summary>
	public static readonly Color Chocolate = new(0xD2, 0x69, 0x1E);

	/// <summary>黄色。</summary>
	public static readonly Color Yellow = new(0xFF, 0xFF, 0x00);
	/// <summary>金色。</summary>
	public static readonly Color Gold = new(0xFF, 0xD7, 0x00);
	/// <summary>橄榄色。</summary>
	public static readonly Color Olive = new(0x80, 0x80, 0x00);
	/// <summary>米色。</summary>
	public static readonly Color Beige = new(0xF5, 0xF5, 0xDC);

	/// <summary>亮绿色。</summary>
	public static readonly Color Lime = new(0x00, 0xFF, 0x00);
	/// <summary>深绿色。</summary>
	public static readonly Color DarkGreen = new(0x00, 0x64, 0x00);

	/// <summary>藏青色。</summary>
	public static readonly Color Navy = new(0x00, 0x00, 0x80);
	/// <summary>深蓝色。</summary>
	public static readonly Color DarkBlue = new(0x00, 0x00, 0x8B);
	/// <summary>蓝绿色。</summary>
	public static readonly Color Teal = new(0x00, 0x80, 0x80);
	/// <summary>水绿色。</summary>
	public static readonly Color Aqua = new(0x00, 0xFF, 0xFF);
	/// <summary>青色。</summary>
	public static readonly Color Cyan = new(0x00, 0xFF, 0xFF);
	/// <summary>绿松石色。</summary>
	public static readonly Color Turquoise = new(0x40, 0xE0, 0xD0);

	/// <summary>紫色。</summary>
	public static readonly Color Purple = new(0x80, 0x00, 0x80);
	/// <summary>靛青色。</summary>
	public static readonly Color Indigo = new(0x4B, 0x00, 0x82);
	/// <summary>紫罗兰色。</summary>
	public static readonly Color Violet = new(0xEE, 0x82, 0xEE);
	/// <summary>薰衣草色。</summary>
	public static readonly Color Lavender = new(0xE6, 0xE6, 0xFA);
	/// <summary>紫红色。</summary>
	public static readonly Color Fuchsia = new(0xFF, 0x00, 0xFF);
	/// <summary>洋红色。</summary>
	public static readonly Color Magenta = new(0xFF, 0x00, 0xFF);
	#endregion

	#region 静态方法
	public static Color Parse(ReadOnlySpan<char> text) => TryParse(text, out var result) ? result : throw new FormatException($"The specified '{text.ToString()}' is not a valid color.");
	public static bool TryParse(ReadOnlySpan<char> text, out Color result)
	{
		text = text.Trim();

		if(text.IsEmpty)
		{
			result = default;
			return false;
		}

		for(int i = 0; i < _colors.Length; i++)
		{
			if(text.Equals(_colors[i].Name, StringComparison.OrdinalIgnoreCase))
			{
				result = _colors[i].Value;
				return true;
			}
		}

		if(text.Length == 7 && text[0] == '#' && uint.TryParse(text[1..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
		{
			result = new Color(0xFF000000 | value);
			return true;
		}

		if(text.Length == 9 && text[0] == '#' && uint.TryParse(text[1..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value))
		{
			result = new Color(value);
			return true;
		}

		result = default;
		return false;
	}

	static Color IParsable<Color>.Parse(string text, IFormatProvider provider) => Parse(text);
	static bool IParsable<Color>.TryParse(string text, IFormatProvider provider, out Color result) => TryParse(text, out result);
	#endregion

	#region 重写方法
	public bool Equals(Color other) => _value == other._value;
	public override bool Equals(object obj) => obj is Color other && this.Equals(other);
	public override int GetHashCode() => _value.GetHashCode();
	public override string ToString() => this.Alpha == 0xFF ? $"#{this.Value & 0xFFFFFF:X6}" : $"#{this.Value:X8}";
	#endregion

	#region 符号重写
	public static bool operator ==(Color left, Color right) => left.Equals(right);
	public static bool operator !=(Color left, Color right) => !left.Equals(right);

	public static implicit operator Color(uint value) => new(value);
	public static implicit operator uint(Color color) => color.Value;
	public static implicit operator string(Color color) => color.ToString();
	#endregion

	#region 嵌套子类
	private sealed class TypeConverter : System.ComponentModel.TypeConverter
	{
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) => value switch
		{
			string text => Parse(text),
			_ => base.ConvertFrom(context, culture, value),
		};

		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) => value is Color color ? destinationType switch
		{
			Type type when type == typeof(string) => color.ToString(),
			_ => base.ConvertTo(context, culture, value, destinationType),
		} : base.ConvertTo(context, culture, value, destinationType);
	}

	private sealed class JsonConverter : JsonConverter<Color>
	{
		public override Color Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) => reader.TokenType switch
		{
			JsonTokenType.Null => default,
			JsonTokenType.String => Parse(reader.GetString()),
			_ => throw new JsonException(),
		};

		public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
			=> writer.WriteStringValue(value.ToString());
	}
	#endregion
}
