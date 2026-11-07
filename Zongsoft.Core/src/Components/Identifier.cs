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
using System.Runtime.CompilerServices;

namespace Zongsoft.Components;

/// <summary>表示由逻辑名称或类型限定的类型化标识。</summary>
/// <remarks>身份比较仅包含类别和类型化键；逻辑名称忽略大小写，元数据不参与比较。结构复制及无损转换共享扩展属性字典。</remarks>
[TypeConverter(typeof(IdentifierTypeConverter))]
[JsonConverter(typeof(IdentifierJsonConverter))]
public readonly partial struct Identifier : IEquatable<Identifier>, IConvertible, ISpanParsable<Identifier>, IFormattable
{
	#region 构造函数
	/// <summary>初始化以逻辑名称为类别的标识。</summary>
	/// <param name="name">逻辑名称，仅允许 ASCII 字母、数字、下划线和点，且不能以点开头。</param>
	/// <param name="value">标识的完整键值，保留其实际类型。</param>
	/// <param name="label">可选的展示名称。</param>
	/// <param name="description">可选的说明。</param>
	/// <param name="properties">可选的扩展属性；构造时复制字典，副本的键不区分大小写。</param>
	/// <exception cref="ArgumentException">逻辑名称不符合命名规则。</exception>
	/// <remarks>构造函数复制传入的扩展属性字典；不会验证键值是否满足业务约束，也不会预先验证其能否序列化。</remarks>
	public Identifier(string name, object value, string label = null, string description = null, IDictionary<string, string> properties = null) :
		this(name, null, value, null, label, null, description, properties, true) { }

	/// <summary>初始化以逻辑名称为类别的标识。</summary>
	/// <param name="name">逻辑名称，仅允许 ASCII 字母、数字、下划线和点，且不能以点开头。</param>
	/// <param name="value">标识的完整键值，保留其实际类型。</param>
	/// <param name="extra">业务编号和展示别名组成的元组；未提供的字段使用空引用。</param>
	/// <param name="label">可选的展示名称。</param>
	/// <param name="description">可选的说明。</param>
	/// <param name="properties">可选的扩展属性；构造时复制字典，副本的键不区分大小写。</param>
	/// <exception cref="ArgumentException">逻辑名称不符合命名规则。</exception>
	/// <remarks>构造函数复制传入的扩展属性字典；不会验证键值是否满足业务约束，也不会预先验证其能否序列化。</remarks>
	public Identifier(string name, object value, (string code, string alias) extra, string label = null, string description = null, IDictionary<string, string> properties = null) :
		this(name, null, value, extra.code, label, extra.alias, description, properties, true) { }

	/// <summary>初始化以逻辑名称为类别的标识。</summary>
	/// <param name="name">逻辑名称，仅允许 ASCII 字母、数字、下划线和点，且不能以点开头。</param>
	/// <param name="value">标识的完整键值，保留其实际类型。</param>
	/// <param name="extra">业务编号、展示别名和展示名称组成的元组；未提供的字段使用空引用。</param>
	/// <param name="description">可选的说明。</param>
	/// <param name="properties">可选的扩展属性；构造时复制字典，副本的键不区分大小写。</param>
	/// <exception cref="ArgumentException">逻辑名称不符合命名规则。</exception>
	/// <remarks>构造函数复制传入的扩展属性字典；不会验证键值是否满足业务约束，也不会预先验证其能否序列化。</remarks>
	public Identifier(string name, object value, (string code, string alias, string label) extra, string description = null, IDictionary<string, string> properties = null) :
		this(name, null, value, extra.code, extra.label, extra.alias, description, properties, true) { }

	/// <summary>初始化以 CLR 类型为类别的标识。</summary>
	/// <param name="type">作为类别的 CLR 类型，不能为空。</param>
	/// <param name="value">标识的完整键值，保留其实际类型。</param>
	/// <param name="label">可选的展示名称。</param>
	/// <param name="description">可选的说明。</param>
	/// <param name="properties">可选的扩展属性；构造时复制字典，副本的键不区分大小写。</param>
	/// <exception cref="ArgumentNullException">类别类型为空。</exception>
	/// <remarks>构造函数复制传入的扩展属性字典；不会验证键值是否满足业务约束，也不会预先验证其能否序列化。</remarks>
	public Identifier(Type type, object value, string label = null, string description = null, IDictionary<string, string> properties = null) :
		this(null, type ?? throw new ArgumentNullException(nameof(type)), value, null, label, null, description, properties, true) { }

	/// <summary>初始化以 CLR 类型为类别的标识。</summary>
	/// <param name="type">作为类别的 CLR 类型，不能为空。</param>
	/// <param name="value">标识的完整键值，保留其实际类型。</param>
	/// <param name="extra">业务编号和展示别名组成的元组；未提供的字段使用空引用。</param>
	/// <param name="label">可选的展示名称。</param>
	/// <param name="description">可选的说明。</param>
	/// <param name="properties">可选的扩展属性；构造时复制字典，副本的键不区分大小写。</param>
	/// <exception cref="ArgumentNullException">类别类型为空。</exception>
	/// <remarks>构造函数复制传入的扩展属性字典；不会验证键值是否满足业务约束，也不会预先验证其能否序列化。</remarks>
	public Identifier(Type type, object value, (string code, string alias) extra, string label = null, string description = null, IDictionary<string, string> properties = null) :
		this(null, type ?? throw new ArgumentNullException(nameof(type)), value, extra.code, label, extra.alias, description, properties, true) { }

	/// <summary>初始化以 CLR 类型为类别的标识。</summary>
	/// <param name="type">作为类别的 CLR 类型，不能为空。</param>
	/// <param name="value">标识的完整键值，保留其实际类型。</param>
	/// <param name="extra">业务编号、展示别名和展示名称组成的元组；未提供的字段使用空引用。</param>
	/// <param name="description">可选的说明。</param>
	/// <param name="properties">可选的扩展属性；构造时复制字典，副本的键不区分大小写。</param>
	/// <exception cref="ArgumentNullException">类别类型为空。</exception>
	/// <remarks>构造函数复制传入的扩展属性字典；不会验证键值是否满足业务约束，也不会预先验证其能否序列化。</remarks>
	public Identifier(Type type, object value, (string code, string alias, string label) extra, string description = null, IDictionary<string, string> properties = null) :
		this(null, type ?? throw new ArgumentNullException(nameof(type)), value, extra.code, extra.label, extra.alias, description, properties, true) { }

	internal Identifier(string name, Type type, object value, string code, string label, string alias, string description, IDictionary<string, string> properties, bool copy)
	{
		if(type == null && !IsName(name))
			throw new ArgumentException(Zongsoft.Properties.Resources.Identifier_InvalidFormat_Message, nameof(name));

		this.Name = name;
		this.Type = type;
		this.Code = code;
		this.Value = value;
		this.Alias = alias;
		this.Label = label;
		this.Description = description;
		this.Properties = copy && properties != null ? new Dictionary<string, string>(properties, StringComparer.OrdinalIgnoreCase) : properties;
	}
	#endregion

	#region 公共字段
	/// <summary>获取标识的逻辑名称；使用 <see cref="Type"/> 类型作为类别时该属性为空。</summary>
	public readonly string Name;
	/// <summary>获取标识的类别；使用逻辑名称（<see cref="Name"/>）作为类别时该属性为空。</summary>
	public readonly Type Type;
	/// <summary>获取可选的业务编号；不从键值推导，不参与身份比较。</summary>
	public readonly string Code;
	/// <summary>获取保持实际类型的身份键值，即标识符值。</summary>
	public readonly object Value;
	/// <summary>获取可选的展示名称，不参与身份比较。</summary>
	public readonly string Label;
	/// <summary>获取可选的展示别名或缩写，不参与身份比较。</summary>
	public readonly string Alias;
	/// <summary>获取可选的说明，不参与身份比较。</summary>
	public readonly string Description;
	#endregion

	#region 公共属性
	/// <summary>获取可选的可变扩展属性字典，不参与身份比较。</summary>
	/// <remarks>未提供扩展属性时为空。结构复制、无损转换及 With 返回值共享字典；并发修改由调用方同步。</remarks>
	public IDictionary<string, string> Properties { get; }
	/// <summary>获取一个值，指示类别或键值缺失。</summary>
	public bool IsEmpty => (this.Type == null && this.Name == null) || this.Value == null;
	/// <summary>获取一个值，指示类别和键值均已提供；零键值仍视为有值。</summary>
	public bool HasValue => !this.IsEmpty;
	#endregion

	#region 公共方法
	/// <summary>将标识的类别改为指定 CLR 类型。</summary>
	/// <typeparam name="TType">目标 CLR 类别。</typeparam>
	/// <returns>指定类别的标识，保留元数据并共享扩展属性；空标识返回默认值。</returns>
	public Identifier Cast<TType>() => this.IsEmpty ? default : new(null, typeof(TType), this.Value, this.Code, this.Label, this.Alias, this.Description, this.Properties, false);
	/// <summary>将标识的类别改为指定 CLR 类型，并转换键值类型。</summary>
	/// <typeparam name="TType">目标 CLR 类别。</typeparam>
	/// <typeparam name="TValue">输出的键值类型。</typeparam>
	/// <returns>指定类别的标识，保留元数据并共享扩展属性；空标识返回默认值。</returns>
	/// <exception cref="InvalidCastException">键值无法转换为目标类型。</exception>
	public Identifier<TValue> Cast<TType, TValue>() => (Identifier<TValue>)this.Cast<TType>();

	/// <summary>验证非空标识的逻辑名称。</summary>
	/// <param name="name">标识的逻辑名称，按不区分大小写的方式匹配。</param>
	/// <returns>标识非空、类别匹配时返回真，否则返回假。</returns>
	public bool Validate(string name) => this.HasValue && string.Equals(this.Name, name, StringComparison.OrdinalIgnoreCase);
	/// <summary>验证非空标识的 CLR 类别。</summary>
	/// <typeparam name="TType">目标 CLR 类别。</typeparam>
	/// <returns>标识非空、类别匹配时返回真，否则返回假。</returns>
	public bool Validate<TType>() => this.HasValue && this.Type != null && typeof(TType).IsAssignableFrom(this.Type);

	/// <summary>尝试获取完整键值。</summary>
	/// <typeparam name="TValue">输出的键值类型。</typeparam>
	/// <param name="value">成功时返回转换后的完整键值，失败时返回默认值。</param>
	/// <returns>完整键值可转换为指定类型时返回真，否则返回假。</returns>
	public bool TryGetValue<TValue>(out TValue value)
	{
		if(TryConvert(this.Value, typeof(TValue), out var converted))
		{
			value = (TValue)converted;
			return true;
		}

		value = default;
		return false;
	}

	/// <summary>验证 CLR 类别并尝试获取完整键值。</summary>
	/// <typeparam name="TType">目标 CLR 类别。</typeparam>
	/// <typeparam name="TValue">输出的键值类型。</typeparam>
	/// <param name="value">成功时返回转换后的完整键值，失败时返回默认值。</param>
	/// <returns>类别匹配、完整键值可转换为指定类型时返回真，否则返回假。</returns>
	public bool TryGetValue<TType, TValue>(out TValue value) => this.TryGetValue(typeof(TType), out value);
	/// <summary>验证 CLR 类别并尝试获取完整键值。</summary>
	/// <typeparam name="TValue">输出的键值类型。</typeparam>
	/// <param name="type">标识的 CLR 类别；验证时接受该类型及其派生或实现类型。</param>
	/// <param name="value">成功时返回转换后的完整键值，失败时返回默认值。</param>
	/// <returns>类别匹配、完整键值可转换为指定类型时返回真，否则返回假。</returns>
	public bool TryGetValue<TValue>(Type type, out TValue value)
	{
		value = default;
		return type != null && this.Type != null && type.IsAssignableFrom(this.Type) && this.TryGetValue(out value);
	}
	/// <summary>验证逻辑名称并尝试获取完整键值。</summary>
	/// <typeparam name="TValue">输出的键值类型。</typeparam>
	/// <param name="name">标识的逻辑名称，按不区分大小写的方式匹配。</param>
	/// <param name="value">成功时返回转换后的完整键值，失败时返回默认值。</param>
	/// <returns>类别匹配、完整键值可转换为指定类型时返回真，否则返回假。</returns>
	public bool TryGetValue<TValue>(string name, out TValue value)
	{
		value = default;
		return this.Validate(name) && this.TryGetValue(out value);
	}

	/// <summary>尝试获取指定序号的键分量。</summary>
	/// <typeparam name="TValue">输出的键值类型。</typeparam>
	/// <param name="ordinal">从零开始的键分量序号；单键只支持零。</param>
	/// <param name="value">成功时返回转换后的键分量，失败时返回默认值。</param>
	/// <returns>分量存在且可转换为指定类型时返回真，否则返回假。</returns>
	public bool TryGetValue<TValue>(int ordinal, out TValue value)
	{
		value = default;
		if(this.IsEmpty || ordinal < 0)
			return false;

		var component = this.Value;
		if(IsTuple(component.GetType()) && component is ITuple tuple)
		{
			if(ordinal >= tuple.Length)
				return false;

			component = tuple[ordinal];
		}
		else if(ordinal != 0)
			return false;

		if(!TryConvert(component, typeof(TValue), out var converted))
			return false;

		value = (TValue)converted;
		return true;
	}

	/// <summary>验证逻辑名称并尝试获取指定序号的键分量。</summary>
	/// <typeparam name="TValue">输出的键值类型。</typeparam>
	/// <param name="name">标识的逻辑名称，按不区分大小写的方式匹配。</param>
	/// <param name="ordinal">从零开始的键分量序号；单键只支持零。</param>
	/// <param name="value">成功时返回转换后的键分量，失败时返回默认值。</param>
	/// <returns>类别匹配、分量存在且可转换为指定类型时返回真，否则返回假。</returns>
	public bool TryGetValue<TValue>(string name, int ordinal, out TValue value)
	{
		value = default;
		return this.Validate(name) && this.TryGetValue(ordinal, out value);
	}

	/// <summary>验证 CLR 类别并尝试获取指定序号的键分量。</summary>
	/// <typeparam name="TValue">输出的键值类型。</typeparam>
	/// <param name="type">标识的 CLR 类别；验证时接受该类型及其派生或实现类型。</param>
	/// <param name="ordinal">从零开始的键分量序号；单键只支持零。</param>
	/// <param name="value">成功时返回转换后的键分量，失败时返回默认值。</param>
	/// <returns>类别匹配、分量存在且可转换为指定类型时返回真，否则返回假。</returns>
	public bool TryGetValue<TValue>(Type type, int ordinal, out TValue value)
	{
		value = default;
		return type != null && this.Type != null && type.IsAssignableFrom(this.Type) && this.TryGetValue(ordinal, out value);
	}

	/// <summary>替换指定序号的键分量，返回保留类别及元数据的新标识。</summary>
	/// <param name="ordinal">从零开始的键分量序号；单键只支持零。</param>
	/// <param name="value">替换值，按原键分量类型转换。</param>
	/// <returns>替换后的标识，与原标识共享扩展属性字典。</returns>
	/// <exception cref="InvalidOperationException">当前标识为空。</exception>
	/// <exception cref="ArgumentOutOfRangeException">分量序号越界。</exception>
	/// <exception cref="InvalidCastException">替换值无法转换为原分量类型。</exception>
	public Identifier With(int ordinal, object value)
	{
		if(this.IsEmpty)
			throw new InvalidOperationException(Zongsoft.Properties.Resources.Identifier_InvalidFormat_Message);

		ArgumentOutOfRangeException.ThrowIfNegative(ordinal);

		var type = this.Value.GetType();
		object result;

		if(IsTuple(type) && this.Value is ITuple tuple)
		{
			ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(ordinal, tuple.Length);

			if(!TryConvert(value, type.GenericTypeArguments[ordinal], out var component))
				throw new InvalidCastException(Zongsoft.Properties.Resources.Identifier_InvalidFormat_Message);

			var values = new object[tuple.Length];
			for(int i = 0; i < values.Length; i++)
				values[i] = i == ordinal ? component : tuple[i];

			result = Activator.CreateInstance(type, values);
		}
		else
		{
			ArgumentOutOfRangeException.ThrowIfNotEqual(ordinal, 0);

			if(!TryConvert(value, type, out result))
				throw new InvalidCastException(Zongsoft.Properties.Resources.Identifier_InvalidFormat_Message);
		}

		return new(this.Name, this.Type, result, this.Code, this.Label, this.Alias, this.Description, this.Properties, false);
	}
	#endregion

	#region 重写方法
	/// <summary>比较标识的类别及类型化键值。</summary>
	/// <param name="other">参与比较的另一个标识。</param>
	/// <returns>类别及类型化键相等时返回真，否则返回假；两个空标识相等。</returns>
	public bool Equals(Identifier other) => this.IsEmpty ? other.IsEmpty : !other.IsEmpty && this.Type == other.Type &&
		string.Equals(this.Name, other.Name, StringComparison.OrdinalIgnoreCase) && Equals(this.Value, other.Value);
	/// <summary>比较标识的类别及类型化键值。</summary>
	/// <param name="obj">参与比较的对象。</param>
	/// <returns>类别及类型化键相等时返回真，否则返回假；两个空标识相等。</returns>
	public override bool Equals(object obj) => obj is Identifier other && this.Equals(other);
	/// <summary>获取由类别和类型化键生成的哈希代码。</summary>
	/// <returns>身份的哈希代码；空标识返回零，元数据不参与计算。</returns>
	public override int GetHashCode() => this.IsEmpty ? 0 : HashCode.Combine(this.Type, this.Name == null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(this.Name), this.Value);
	/// <summary>将标识格式化为统一的文本协议。</summary>
	/// <returns>包含类别、键值及非空元数据的标识文本；空标识返回空字符串。</returns>
	/// <exception cref="FormatException">键值不支持协议要求的文本转换。</exception>
	public override string ToString() => this.ToString(null, null);
	/// <summary>将标识格式化为统一的文本协议。</summary>
	/// <param name="format">格式标记：空值、空字符串或 G 使用匹配的已登记简写；F 强制完整格式。</param>
	/// <returns>包含类别、键值及非空元数据的标识文本；空标识返回空字符串。</returns>
	/// <exception cref="FormatException">格式标记无效，或者键值不支持协议要求的文本转换。</exception>
	public string ToString(string format) => this.ToString(format, null);
	/// <summary>将标识格式化为统一的文本协议。</summary>
	/// <param name="format">格式标记：空值、空字符串或 G 使用匹配的已登记简写；F 强制完整格式。</param>
	/// <param name="provider">保留的格式提供程序参数；标识协议始终使用固定文化格式。</param>
	/// <returns>包含类别、键值及非空元数据的标识文本；空标识返回空字符串。</returns>
	/// <exception cref="FormatException">格式标记无效，或者键值不支持协议要求的文本转换。</exception>
	public string ToString(string format, IFormatProvider provider) => Format(this, format);
	#endregion

	#region 解析方法
	/// <summary>解析标识文本。</summary>
	/// <param name="text">待解析的标识文本；空文本表示默认标识。空引用不被接受。</param>
	/// <returns>解析得到的标识；空文本返回默认值。</returns>
	/// <exception cref="ArgumentNullException">待解析文本为空引用。</exception>
	/// <exception cref="FormatException">文本格式、类别或键值类型不符合标识协议。</exception>
	public static Identifier Parse(string text) => Parse(text, null);
	/// <summary>解析标识文本。</summary>
	/// <param name="text">待解析的标识文本；空文本表示默认标识。空引用不被接受。</param>
	/// <param name="provider">保留的格式提供程序参数；标识协议始终使用固定文化格式。</param>
	/// <returns>解析得到的标识；空文本返回默认值。</returns>
	/// <exception cref="ArgumentNullException">待解析文本为空引用。</exception>
	/// <exception cref="FormatException">文本格式、类别或键值类型不符合标识协议。</exception>
	public static Identifier Parse(string text, IFormatProvider provider) => text == null ? throw new ArgumentNullException(nameof(text)) : Parse(text.AsSpan(), provider);
	/// <summary>解析标识文本。</summary>
	/// <param name="text">待解析的标识文本；空文本表示默认标识。</param>
	/// <returns>解析得到的标识；空文本返回默认值。</returns>
	/// <exception cref="FormatException">文本格式、类别或键值类型不符合标识协议。</exception>
	public static Identifier Parse(ReadOnlySpan<char> text) => Parse(text, null);
	/// <summary>解析标识文本。</summary>
	/// <param name="text">待解析的标识文本；空文本表示默认标识。</param>
	/// <param name="provider">保留的格式提供程序参数；标识协议始终使用固定文化格式。</param>
	/// <returns>解析得到的标识；空文本返回默认值。</returns>
	/// <exception cref="FormatException">文本格式、类别或键值类型不符合标识协议。</exception>
	public static Identifier Parse(ReadOnlySpan<char> text, IFormatProvider provider) => TryParse(text, provider, out var result) ? result : throw new FormatException(Zongsoft.Properties.Resources.Identifier_InvalidFormat_Message);
	/// <summary>尝试解析标识文本。</summary>
	/// <param name="text">待解析的标识文本；空文本表示默认标识。空引用返回失败。</param>
	/// <param name="result">解析成功时返回标识，失败时返回默认值。</param>
	/// <returns>文本可解析时返回真，包括空文本；非法输入返回假。</returns>
	public static bool TryParse(string text, out Identifier result) => TryParse(text, null, out result);
	/// <summary>尝试解析标识文本。</summary>
	/// <param name="text">待解析的标识文本；空文本表示默认标识。空引用返回失败。</param>
	/// <param name="provider">保留的格式提供程序参数；标识协议始终使用固定文化格式。</param>
	/// <param name="result">解析成功时返回标识，失败时返回默认值。</param>
	/// <returns>文本可解析时返回真，包括空文本；非法输入返回假。</returns>
	public static bool TryParse(string text, IFormatProvider provider, out Identifier result)
	{
		result = default;
		return text != null && TryParse(text.AsSpan(), provider, out result);
	}
	/// <summary>尝试解析标识文本。</summary>
	/// <param name="text">待解析的标识文本；空文本表示默认标识。</param>
	/// <param name="result">解析成功时返回标识，失败时返回默认值。</param>
	/// <returns>文本可解析时返回真，包括空文本；非法输入返回假。</returns>
	public static bool TryParse(ReadOnlySpan<char> text, out Identifier result) => TryParse(text, null, out result);
	/// <summary>尝试解析标识文本。</summary>
	/// <param name="text">待解析的标识文本；空文本表示默认标识。</param>
	/// <param name="provider">保留的格式提供程序参数；标识协议始终使用固定文化格式。</param>
	/// <param name="result">解析成功时返回标识，失败时返回默认值。</param>
	/// <returns>文本可解析时返回真，包括空文本；非法输入返回假。</returns>
	public static bool TryParse(ReadOnlySpan<char> text, IFormatProvider provider, out Identifier result) => TryParseCore(text, null, out result);
	#endregion

	#region 符号重写
	/// <summary>比较两个标识的身份是否相等。</summary>
	/// <param name="left">比较运算左侧的标识。</param>
	/// <param name="right">比较运算右侧的标识。</param>
	/// <returns>比较结果；元数据不参与身份比较。</returns>
	public static bool operator ==(Identifier left, Identifier right) => left.Equals(right);
	/// <summary>比较两个标识的身份是否不相等。</summary>
	/// <param name="left">比较运算左侧的标识。</param>
	/// <param name="right">比较运算右侧的标识。</param>
	/// <returns>比较结果；元数据不参与身份比较。</returns>
	public static bool operator !=(Identifier left, Identifier right) => !left.Equals(right);
	#endregion

	#region 类型转换
	/// <summary>将标识转换为 <c>bool</c>。</summary>
	/// <param name="identifier">待转换的标识。</param>
	/// <returns>从标识中取得或转换的键值。</returns>
	/// <remarks>仅提取键值；不返回包含类别和元数据的标识协议文本。</remarks>
	public static explicit operator bool(Identifier identifier) => Common.Convert.ConvertValue<bool>(identifier.Value);
	/// <summary>将标识转换为 <c>char</c>。</summary>
	/// <param name="identifier">待转换的标识。</param>
	/// <returns>从标识中取得或转换的键值。</returns>
	/// <remarks>仅提取键值；不返回包含类别和元数据的标识协议文本。</remarks>
	public static explicit operator char(Identifier identifier) => Common.Convert.ConvertValue<char>(identifier.Value);
	/// <summary>将标识转换为 <c>byte</c>。</summary>
	/// <param name="identifier">待转换的标识。</param>
	/// <returns>从标识中取得或转换的键值。</returns>
	/// <remarks>仅提取键值；不返回包含类别和元数据的标识协议文本。</remarks>
	public static explicit operator byte(Identifier identifier) => Common.Convert.ConvertValue<byte>(identifier.Value);
	/// <summary>将标识转换为 <c>sbyte</c>。</summary>
	/// <param name="identifier">待转换的标识。</param>
	/// <returns>从标识中取得或转换的键值。</returns>
	/// <remarks>仅提取键值；不返回包含类别和元数据的标识协议文本。</remarks>
	public static explicit operator sbyte(Identifier identifier) => Common.Convert.ConvertValue<sbyte>(identifier.Value);
	/// <summary>将标识转换为 <c>short</c>。</summary>
	/// <param name="identifier">待转换的标识。</param>
	/// <returns>从标识中取得或转换的键值。</returns>
	/// <remarks>仅提取键值；不返回包含类别和元数据的标识协议文本。</remarks>
	public static explicit operator short(Identifier identifier) => Common.Convert.ConvertValue<short>(identifier.Value);
	/// <summary>将标识转换为 <c>int</c>。</summary>
	/// <param name="identifier">待转换的标识。</param>
	/// <returns>从标识中取得或转换的键值。</returns>
	/// <remarks>仅提取键值；不返回包含类别和元数据的标识协议文本。</remarks>
	public static explicit operator int(Identifier identifier) => Common.Convert.ConvertValue<int>(identifier.Value);
	/// <summary>将标识转换为 <c>long</c>。</summary>
	/// <param name="identifier">待转换的标识。</param>
	/// <returns>从标识中取得或转换的键值。</returns>
	/// <remarks>仅提取键值；不返回包含类别和元数据的标识协议文本。</remarks>
	public static explicit operator long(Identifier identifier) => Common.Convert.ConvertValue<long>(identifier.Value);
	/// <summary>将标识转换为 <c>ushort</c>。</summary>
	/// <param name="identifier">待转换的标识。</param>
	/// <returns>从标识中取得或转换的键值。</returns>
	/// <remarks>仅提取键值；不返回包含类别和元数据的标识协议文本。</remarks>
	public static explicit operator ushort(Identifier identifier) => Common.Convert.ConvertValue<ushort>(identifier.Value);
	/// <summary>将标识转换为 <c>uint</c>。</summary>
	/// <param name="identifier">待转换的标识。</param>
	/// <returns>从标识中取得或转换的键值。</returns>
	/// <remarks>仅提取键值；不返回包含类别和元数据的标识协议文本。</remarks>
	public static explicit operator uint(Identifier identifier) => Common.Convert.ConvertValue<uint>(identifier.Value);
	/// <summary>将标识转换为 <c>ulong</c>。</summary>
	/// <param name="identifier">待转换的标识。</param>
	/// <returns>从标识中取得或转换的键值。</returns>
	/// <remarks>仅提取键值；不返回包含类别和元数据的标识协议文本。</remarks>
	public static explicit operator ulong(Identifier identifier) => Common.Convert.ConvertValue<ulong>(identifier.Value);
	/// <summary>将标识转换为 <c>float</c>。</summary>
	/// <param name="identifier">待转换的标识。</param>
	/// <returns>从标识中取得或转换的键值。</returns>
	/// <remarks>仅提取键值；不返回包含类别和元数据的标识协议文本。</remarks>
	public static explicit operator float(Identifier identifier) => Common.Convert.ConvertValue<float>(identifier.Value);
	/// <summary>将标识转换为 <c>double</c>。</summary>
	/// <param name="identifier">待转换的标识。</param>
	/// <returns>从标识中取得或转换的键值。</returns>
	/// <remarks>仅提取键值；不返回包含类别和元数据的标识协议文本。</remarks>
	public static explicit operator double(Identifier identifier) => Common.Convert.ConvertValue<double>(identifier.Value);
	/// <summary>将标识转换为 <c>decimal</c>。</summary>
	/// <param name="identifier">待转换的标识。</param>
	/// <returns>从标识中取得或转换的键值。</returns>
	/// <remarks>仅提取键值；不返回包含类别和元数据的标识协议文本。</remarks>
	public static explicit operator decimal(Identifier identifier) => Common.Convert.ConvertValue<decimal>(identifier.Value);
	/// <summary>将标识转换为 <c>string</c>。</summary>
	/// <param name="identifier">待转换的标识。</param>
	/// <returns>从标识中取得或转换的键值。</returns>
	/// <remarks>仅提取键值；不返回包含类别和元数据的标识协议文本。</remarks>
	public static explicit operator string(Identifier identifier) => Common.Convert.ConvertValue<string>(identifier.Value);
	/// <summary>将标识转换为 <c>DateTime</c>。</summary>
	/// <param name="identifier">待转换的标识。</param>
	/// <returns>从标识中取得或转换的键值。</returns>
	/// <remarks>仅提取键值；不返回包含类别和元数据的标识协议文本。</remarks>
	public static explicit operator DateTime(Identifier identifier) => Common.Convert.ConvertValue<DateTime>(identifier.Value);
	/// <summary>将标识转换为 <c>DateTimeOffset</c>。</summary>
	/// <param name="identifier">待转换的标识。</param>
	/// <returns>从标识中取得或转换的键值。</returns>
	/// <remarks>仅提取键值；不返回包含类别和元数据的标识协议文本。</remarks>
	public static explicit operator DateTimeOffset(Identifier identifier) => Common.Convert.ConvertValue<DateTimeOffset>(identifier.Value);
	/// <summary>将标识转换为 <c>DateOnly</c>。</summary>
	/// <param name="identifier">待转换的标识。</param>
	/// <returns>从标识中取得或转换的键值。</returns>
	/// <remarks>仅提取键值；不返回包含类别和元数据的标识协议文本。</remarks>
	public static explicit operator DateOnly(Identifier identifier) => Common.Convert.ConvertValue<DateOnly>(identifier.Value);
	/// <summary>将标识转换为 <c>TimeOnly</c>。</summary>
	/// <param name="identifier">待转换的标识。</param>
	/// <returns>从标识中取得或转换的键值。</returns>
	/// <remarks>仅提取键值；不返回包含类别和元数据的标识协议文本。</remarks>
	public static explicit operator TimeOnly(Identifier identifier) => Common.Convert.ConvertValue<TimeOnly>(identifier.Value);
	/// <summary>将标识转换为 <c>TimeSpan</c>。</summary>
	/// <param name="identifier">待转换的标识。</param>
	/// <returns>从标识中取得或转换的键值。</returns>
	/// <remarks>仅提取键值；不返回包含类别和元数据的标识协议文本。</remarks>
	public static explicit operator TimeSpan(Identifier identifier) => Common.Convert.ConvertValue<TimeSpan>(identifier.Value);
	/// <summary>将标识转换为 <c>Guid</c>。</summary>
	/// <param name="identifier">待转换的标识。</param>
	/// <returns>从标识中取得或转换的键值。</returns>
	/// <remarks>仅提取键值；不返回包含类别和元数据的标识协议文本。</remarks>
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
