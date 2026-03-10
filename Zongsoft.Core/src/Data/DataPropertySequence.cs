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
using System.Text.RegularExpressions;

namespace Zongsoft.Data;

/// <summary>表示属性序列的描述类。</summary>
/// <remarks>
/// 	<list type="table">
/// 		<listheader>
/// 			<term>序列表达式</term>
/// 			<description>表达式说明</description>
/// 		</listheader>
/// 		<item>
/// 			<term>*</term>
/// 			<description>数据库内置默认序列器，通常对应自增型型字段。</description>
/// 		</item>
/// 		<item>
/// 			<term>*SequenceName</term>
/// 			<description>指定名称的数据库内置序列。</description>
/// 		</item>
/// 		<item>
/// 			<term>#</term>
/// 			<description>默认的外部序列器。</description>
/// 		</item>
/// 		<item>
/// 			<term>#SequenceName</term>
/// 			<description>指定名称的外部序列。</description>
/// 		</item>
/// 		<item>
/// 			<term>#SequenceName@seed/interval</term>
/// 			<description>指定名称的外部序列，同时通过 <c>@</c> 符号指定 <see cref="Seed"/> 值，通过 <c>/</c> 符号指定 <see cref="Interval"/> 值。</description>
/// 		</item>
/// 		<item>
/// 			<term>#(Member1,Member2)</term>
/// 			<description>指定引用成员的外部序列，指定的 <see cref="References"/> 为 <c>Member1</c> 和 <c>Member2</c>。</description>
/// 		</item>
/// 	</list>
/// </remarks>
[TypeConverter(typeof(TypeConverter))]
[JsonConverter(typeof(JsonConverter))]
public readonly struct DataPropertySequence : IParsable<DataPropertySequence>
{
	#region 静态字段
	private static readonly Regex _regex = new(@"
		^(?<kind>[\#\*])?
		(?<name>\w+([\:\.\-]\w+)*)?
		(\(\s*
			(?<refs>\w+(\s*\,\s*\w+)*)
		\s*\))?
		(\@(?<seed>\d+))?
		(\/(?<interval>\d+))?$",
		RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
	#endregion

	#region 构造函数
	public DataPropertySequence(string name, params string[] references) : this(name, 0, 1, references)	{ }
	public DataPropertySequence(string name, int seed, int interval, params string[] references)
	{
		this.Name = name?.Trim();
		this.Seed = seed;
		this.Interval = interval == 0 ? 1 : interval;
		this.References = references;
	}
	#endregion

	#region 公共属性
	/// <summary>获取序列名称。</summary>
	public string Name { get; }
	/// <summary>获取序列种子值，默认值为 <c>0</c>。</summary>
	public int Seed { get; }
	/// <summary>获取序列递增步长值，默认值为 <c>1</c>。</summary>
	public int Interval { get; }
	/// <summary>获取引用成员标识数组。</summary>
	public string[] References { get; }

	/// <summary>获取一个值，指示是否为数据库内置序列器。</summary>
	public bool IsBuiltin => this.Name != null && this.Name.Length > 0 && this.Name[0] == '*';
	/// <summary>获取一个值，指示是否为外部序列器。</summary>
	public bool IsExternal => this.Name != null && this.Name.Length > 0 && this.Name[0] == '#';

	/// <summary>获取一个值，指示是否有引用。</summary>
	[Serialization.SerializationMember(Ignored = true)]
	public bool HasReferences => this.References != null && this.References.Length > 0;
	#endregion

	#region 解析方法
	public static DataPropertySequence Parse(string text, IFormatProvider provider = null) => Parse(text.AsSpan());
	public static DataPropertySequence Parse(ReadOnlySpan<char> text) => TryParse(text, out var sequence) ? sequence : default;

	public static bool TryParse(string text, IFormatProvider provider, out DataPropertySequence result) => TryParse(text.AsSpan(), out result);
	public static bool TryParse(ReadOnlySpan<char> text, out DataPropertySequence result)
	{
		if(text.IsEmpty || text.IsWhiteSpace())
		{
			result = default;
			return false;
		}

		var match = _regex.Match(text.Trim().ToString());

		if(!match.Success)
		{
			result = default;
			return false;
		}

		var kind = match.Groups["kind"].Value;
		var name = match.Groups["name"].Value;

		if(string.IsNullOrEmpty(kind) && string.IsNullOrEmpty(name))
		{
			result = default;
			return false;
		}

		int seed = 0, interval = 1;
		string[] references = null;

		if(match.Groups["seed"].Success)
			seed = int.Parse(match.Groups["seed"].Value);

		if(match.Groups["interval"].Success)
			interval = int.Parse(match.Groups["interval"].Value);

		if(match.Groups["refs"].Success)
			references = match.Groups["refs"].Value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

		result = new($"{kind}{name}", seed, interval, references);
		return true;
	}
	#endregion

	#region 符号重写
	public static implicit operator string(DataPropertySequence sequence) => sequence.ToString();
	#endregion

	#region 重写方法
	public override string ToString()
	{
		if(string.IsNullOrEmpty(this.Name))
			return string.Empty;

		if(this.HasReferences)
		{
			if(this.Seed == 0)
				return this.Interval == 1 ?
					$"{this.Name}({string.Join(',', this.References)})" :
					$"{this.Name}({string.Join(',', this.References)})/{this.Interval}";
			else
				return this.Interval == 1 ?
					$"{this.Name}({string.Join(',', this.References)})@{this.Seed}" :
					$"{this.Name}({string.Join(',', this.References)})@{this.Seed}/{this.Interval}";
		}
		else
		{
			if(this.Seed == 0)
				return this.Interval == 1 ?
					this.Name :
					$"{this.Name}/{this.Interval}";
			else
				return this.Interval == 1 ?
					$"{this.Name}@{this.Seed}" :
					$"{this.Name}@{this.Seed}/{this.Interval}";
		}
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
			if(value is DataPropertySequence sequence)
				return string.IsNullOrEmpty(sequence.Name) ? null : sequence.ToString();

			return base.ConvertTo(context, culture, value, destinationType);
		}
	}

	private class JsonConverter : JsonConverter<DataPropertySequence>
	{
		public override DataPropertySequence Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) => reader.TokenType switch
		{
			JsonTokenType.Null => default,
			JsonTokenType.String => Parse(reader.GetString()),
			_ => throw new JsonException(),
		};

		public override void Write(Utf8JsonWriter writer, DataPropertySequence value, JsonSerializerOptions options)
		{
			if(string.IsNullOrEmpty(value.Name))
				writer.WriteNullValue();
			else
				writer.WriteStringValue(value.ToString());
		}
	}
	#endregion
}
