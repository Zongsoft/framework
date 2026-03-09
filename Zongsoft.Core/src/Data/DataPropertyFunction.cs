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

[TypeConverter(typeof(TypeConverter))]
[JsonConverter(typeof(JsonConverter))]
public readonly struct DataPropertyFunction : IParsable<DataPropertyFunction>
{
	#region 构造函数
	public DataPropertyFunction(string name, params string[] arguments)
	{
		this.Name = name?.Trim();
		this.Arguments = arguments;
	}
	#endregion

	#region 公共属性
	/// <summary>获取函数名称。</summary>
	public string Name { get; }
	/// <summary>获取函数参数集。</summary>
	public string[] Arguments { get; }

	/// <summary>获取一个值，指示函数是否有参数。</summary>
	[System.Text.Json.Serialization.JsonIgnore]
	[Serialization.SerializationMember(Ignored = true)]
	public bool HasArguments => this.Arguments != null && this.Arguments.Length > 0;
	#endregion

	#region 解析方法
	public static DataPropertyFunction Parse(string text, IFormatProvider provider = null) => Parse(text.AsSpan());
	public static DataPropertyFunction Parse(ReadOnlySpan<char> text) => TryParse(text, out var result) ? result : default;

	public static bool TryParse(string text, IFormatProvider provider, out DataPropertyFunction result) => TryParse(text.AsSpan(), out result);
	public static bool TryParse(ReadOnlySpan<char> text, out DataPropertyFunction result)
	{
		if(text.IsEmpty || text.IsWhiteSpace())
		{
			result = default;
			return false;
		}

		text = text.Trim();
		var index = text.IndexOf('(');

		if(index < 0 || text[^1] != ')')
		{
			result = default;
			return false;
		}

		var name = text[..index].Trim();
		var arguments = text.Slice(index + 1, text.Length - index - 2).ToString()
			.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

		result = new(name.ToString(), arguments);
		return true;
	}
	#endregion

	#region 重写方法
	public override string ToString()
	{
		if(string.IsNullOrEmpty(this.Name))
			return string.Empty;

		return this.HasArguments ? $"{this.Name}({string.Join(',', this.Arguments)})" : $"{this.Name}()";
	}
	#endregion

	#region 嵌套子类
	private class TypeConverter : System.ComponentModel.TypeConverter
	{
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			if(value is string text && TryParse(text, out var result))
				return result;

			return base.ConvertFrom(context, culture, value);
		}

		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			if(value is DataPropertyFunction function)
				return string.IsNullOrEmpty(function.Name) ? null : function.ToString();

			return base.ConvertTo(context, culture, value, destinationType);
		}
	}

	private class JsonConverter : JsonConverter<DataPropertyFunction>
	{
		public override DataPropertyFunction Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) => reader.TokenType switch
		{
			JsonTokenType.Null => default,
			JsonTokenType.String => Parse(reader.GetString()),
			_ => throw new JsonException(),
		};

		public override void Write(Utf8JsonWriter writer, DataPropertyFunction value, JsonSerializerOptions options)
		{
			if(string.IsNullOrEmpty(value.Name))
				writer.WriteNullValue();
			else
				writer.WriteStringValue(value.ToString());
		}
	}
	#endregion
}
