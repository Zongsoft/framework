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

/// <summary>表示由逻辑名称限定的类型化复合标识。</summary>
/// <typeparam name="T1">第 1 个键分量的类型。</typeparam>
/// <typeparam name="T2">第 2 个键分量的类型。</typeparam>
/// <typeparam name="T3">第 3 个键分量的类型。</typeparam>
/// <typeparam name="T4">第 4 个键分量的类型。</typeparam>
/// <remarks>身份比较仅包含类别和类型化键；逻辑名称忽略大小写，元数据不参与比较。结构复制及无损转换共享扩展属性字典。</remarks>
[TypeConverter(typeof(IdentifierTypeConverter))]
[JsonConverter(typeof(IdentifierJsonConverter))]
public readonly struct Identifier<T1, T2, T3, T4> : IEquatable<Identifier<T1, T2, T3, T4>>, ISpanParsable<Identifier<T1, T2, T3, T4>>, IFormattable
{
	#region 私有字段
	private readonly Identifier _identifier;
	#endregion

	#region 构造函数
	/// <summary>初始化以逻辑名称为类别的标识。</summary>
	/// <param name="name">逻辑名称，仅允许 ASCII 字母、数字、下划线和点，且不能以点开头。</param>
	/// <param name="value1">第 1 个键分量。</param>
	/// <param name="value2">第 2 个键分量。</param>
	/// <param name="value3">第 3 个键分量。</param>
	/// <param name="value4">第 4 个键分量。</param>
	/// <param name="label">可选的展示名称。</param>
	/// <param name="description">可选的说明。</param>
	/// <param name="properties">可选的扩展属性；构造时复制字典，副本的键不区分大小写。</param>
	/// <exception cref="ArgumentException">逻辑名称不符合命名规则。</exception>
	/// <remarks>构造函数复制传入的扩展属性字典；不会验证键值是否满足业务约束，也不会预先验证其能否序列化。</remarks>
	public Identifier(string name, T1 value1, T2 value2, T3 value3, T4 value4, string label = null, string description = null, IDictionary<string, string> properties = null) =>
		_identifier = new(name, (value1, value2, value3, value4), label, description, properties);

	/// <summary>初始化以逻辑名称为类别的标识。</summary>
	/// <param name="name">逻辑名称，仅允许 ASCII 字母、数字、下划线和点，且不能以点开头。</param>
	/// <param name="value1">第 1 个键分量。</param>
	/// <param name="value2">第 2 个键分量。</param>
	/// <param name="value3">第 3 个键分量。</param>
	/// <param name="value4">第 4 个键分量。</param>
	/// <param name="extra">业务编号和展示别名组成的元组；未提供的字段使用空引用。</param>
	/// <param name="label">可选的展示名称。</param>
	/// <param name="description">可选的说明。</param>
	/// <param name="properties">可选的扩展属性；构造时复制字典，副本的键不区分大小写。</param>
	/// <exception cref="ArgumentException">逻辑名称不符合命名规则。</exception>
	/// <remarks>构造函数复制传入的扩展属性字典；不会验证键值是否满足业务约束，也不会预先验证其能否序列化。</remarks>
	public Identifier(string name, T1 value1, T2 value2, T3 value3, T4 value4, (string code, string alias) extra, string label = null, string description = null, IDictionary<string, string> properties = null) =>
		_identifier = new(name, (value1, value2, value3, value4), extra, label, description, properties);

	/// <summary>初始化以逻辑名称为类别的标识。</summary>
	/// <param name="name">逻辑名称，仅允许 ASCII 字母、数字、下划线和点，且不能以点开头。</param>
	/// <param name="value1">第 1 个键分量。</param>
	/// <param name="value2">第 2 个键分量。</param>
	/// <param name="value3">第 3 个键分量。</param>
	/// <param name="value4">第 4 个键分量。</param>
	/// <param name="extra">业务编号、展示别名和展示名称组成的元组；未提供的字段使用空引用。</param>
	/// <param name="description">可选的说明。</param>
	/// <param name="properties">可选的扩展属性；构造时复制字典，副本的键不区分大小写。</param>
	/// <exception cref="ArgumentException">逻辑名称不符合命名规则。</exception>
	/// <remarks>构造函数复制传入的扩展属性字典；不会验证键值是否满足业务约束，也不会预先验证其能否序列化。</remarks>
	public Identifier(string name, T1 value1, T2 value2, T3 value3, T4 value4, (string code, string alias, string label) extra, string description = null, IDictionary<string, string> properties = null) =>
		_identifier = new(name, (value1, value2, value3, value4), extra, description, properties);

	/// <summary>初始化以逻辑名称为类别的标识。</summary>
	/// <param name="name">逻辑名称，仅允许 ASCII 字母、数字、下划线和点，且不能以点开头。</param>
	/// <param name="value">标识的完整键值，保留其实际类型。</param>
	/// <param name="label">可选的展示名称。</param>
	/// <param name="description">可选的说明。</param>
	/// <param name="properties">可选的扩展属性；构造时复制字典，副本的键不区分大小写。</param>
	/// <exception cref="ArgumentException">逻辑名称不符合命名规则。</exception>
	/// <remarks>构造函数复制传入的扩展属性字典；不会验证键值是否满足业务约束，也不会预先验证其能否序列化。</remarks>
	public Identifier(string name, (T1, T2, T3, T4) value, string label = null, string description = null, IDictionary<string, string> properties = null) =>
		_identifier = new(name, value, label, description, properties);

	/// <summary>初始化以逻辑名称为类别的标识。</summary>
	/// <param name="name">逻辑名称，仅允许 ASCII 字母、数字、下划线和点，且不能以点开头。</param>
	/// <param name="value">标识的完整键值，保留其实际类型。</param>
	/// <param name="extra">业务编号和展示别名组成的元组；未提供的字段使用空引用。</param>
	/// <param name="label">可选的展示名称。</param>
	/// <param name="description">可选的说明。</param>
	/// <param name="properties">可选的扩展属性；构造时复制字典，副本的键不区分大小写。</param>
	/// <exception cref="ArgumentException">逻辑名称不符合命名规则。</exception>
	/// <remarks>构造函数复制传入的扩展属性字典；不会验证键值是否满足业务约束，也不会预先验证其能否序列化。</remarks>
	public Identifier(string name, (T1, T2, T3, T4) value, (string code, string alias) extra, string label = null, string description = null, IDictionary<string, string> properties = null) =>
		_identifier = new(name, value, extra, label, description, properties);

	/// <summary>初始化以逻辑名称为类别的标识。</summary>
	/// <param name="name">逻辑名称，仅允许 ASCII 字母、数字、下划线和点，且不能以点开头。</param>
	/// <param name="value">标识的完整键值，保留其实际类型。</param>
	/// <param name="extra">业务编号、展示别名和展示名称组成的元组；未提供的字段使用空引用。</param>
	/// <param name="description">可选的说明。</param>
	/// <param name="properties">可选的扩展属性；构造时复制字典，副本的键不区分大小写。</param>
	/// <exception cref="ArgumentException">逻辑名称不符合命名规则。</exception>
	/// <remarks>构造函数复制传入的扩展属性字典；不会验证键值是否满足业务约束，也不会预先验证其能否序列化。</remarks>
	public Identifier(string name, (T1, T2, T3, T4) value, (string code, string alias, string label) extra, string description = null, IDictionary<string, string> properties = null) =>
		_identifier = new(name, value, extra, description, properties);

	internal Identifier(Identifier identifier)
	{
		if(identifier.IsEmpty)
			_identifier = default;
		else if(identifier.TryGetValue<(T1, T2, T3, T4)>(out var value))
			_identifier = new(identifier.Name, value, identifier.Code, identifier.Label, identifier.Alias, identifier.Description, identifier.Properties, false);
		else
			throw new InvalidCastException(Zongsoft.Properties.Resources.Identifier_InvalidFormat_Message);
	}
	#endregion

	#region 公共属性
	/// <summary>获取标识的逻辑名称；使用 CLR 类型作为类别时为空。</summary>
	public string Name => _identifier.Name;
	/// <summary>获取或初始化类型化复合键；空标识返回各分量均为默认值的元组。</summary>
	public (T1, T2, T3, T4) Value
	{
		get => this.IsEmpty ? default : ((T1, T2, T3, T4))_identifier.Value;
		init => _identifier = new(_identifier.Name, value, _identifier.Code, _identifier.Label, _identifier.Alias, _identifier.Description, _identifier.Properties, false);
	}
	/// <summary>获取或初始化可选的业务编号；不从键值推导，不参与身份比较。</summary>
	public string Code
	{
		get => _identifier.Code;
		init => _identifier = new(_identifier.Name, _identifier.Value, value, _identifier.Label, _identifier.Alias, _identifier.Description, _identifier.Properties, false);
	}
	/// <summary>获取或初始化可选的展示名称，不参与身份比较。</summary>
	public string Label
	{
		get => _identifier.Label;
		init => _identifier = new(_identifier.Name, _identifier.Value, _identifier.Code, value, _identifier.Alias, _identifier.Description, _identifier.Properties, false);
	}
	/// <summary>获取或初始化可选的展示别名或缩写，不参与身份比较。</summary>
	public string Alias
	{
		get => _identifier.Alias;
		init => _identifier = new(_identifier.Name, _identifier.Value, _identifier.Code, _identifier.Label, value, _identifier.Description, _identifier.Properties, false);
	}
	/// <summary>获取或初始化可选的说明，不参与身份比较。</summary>
	public string Description
	{
		get => _identifier.Description;
		init => _identifier = new(_identifier.Name, _identifier.Value, _identifier.Code, _identifier.Label, _identifier.Alias, value, _identifier.Properties, false);
	}
	/// <summary>获取或初始化可选的可变扩展属性字典，不参与身份比较。</summary>
	/// <remarks>未提供扩展属性时为空；初始化新字典时复制一次。结构复制、无损转换及 With 返回值共享字典；并发修改由调用方同步。</remarks>
	public IDictionary<string, string> Properties
	{
		get => _identifier.Properties;
		init => _identifier = new(_identifier.Name, _identifier.Value, _identifier.Code, _identifier.Label, _identifier.Alias, _identifier.Description, value, true);
	}
	/// <summary>获取一个值，指示类别或键值缺失。</summary>
	public bool IsEmpty => _identifier.IsEmpty;
	/// <summary>获取一个值，指示类别和键值均已提供；零键值仍视为有值。</summary>
	public bool HasValue => _identifier.HasValue;
	#endregion

	#region 公共方法
	/// <summary>将复合标识解构为类型化键分量。</summary>
	/// <param name="value1">第 1 个键分量的输出值；空标识输出默认值。</param>
	/// <param name="value2">第 2 个键分量的输出值；空标识输出默认值。</param>
	/// <param name="value3">第 3 个键分量的输出值；空标识输出默认值。</param>
	/// <param name="value4">第 4 个键分量的输出值；空标识输出默认值。</param>
	public void Deconstruct(out T1 value1, out T2 value2, out T3 value3, out T4 value4) => (value1, value2, value3, value4) = this.Value;
	#endregion

	#region 取值验证
	/// <summary>尝试获取完整键值。</summary>
	/// <param name="value">成功时返回转换后的完整键值，失败时返回默认值。</param>
	/// <returns>完整键值可转换为指定类型时返回真，否则返回假。</returns>
	public bool TryGetValue(out (T1, T2, T3, T4) value) => _identifier.TryGetValue(out value);
	/// <summary>验证逻辑名称并尝试获取完整键值。</summary>
	/// <param name="name">标识的逻辑名称，按不区分大小写的方式匹配。</param>
	/// <param name="value">成功时返回转换后的完整键值，失败时返回默认值。</param>
	/// <returns>类别匹配、完整键值可转换为指定类型时返回真，否则返回假。</returns>
	public bool TryGetValue(string name, out (T1, T2, T3, T4) value) => _identifier.TryGetValue(name, out value);
	/// <summary>尝试获取指定序号的键分量。</summary>
	/// <typeparam name="TValue">输出的键值类型。</typeparam>
	/// <param name="ordinal">从零开始的键分量序号；单键只支持零。</param>
	/// <param name="value">成功时返回转换后的键分量，失败时返回默认值。</param>
	/// <returns>分量存在且可转换为指定类型时返回真，否则返回假。</returns>
	public bool TryGetValue<TValue>(int ordinal, out TValue value) => _identifier.TryGetValue(ordinal, out value);
	/// <summary>验证逻辑名称并尝试获取指定序号的键分量。</summary>
	/// <typeparam name="TValue">输出的键值类型。</typeparam>
	/// <param name="name">标识的逻辑名称，按不区分大小写的方式匹配。</param>
	/// <param name="ordinal">从零开始的键分量序号；单键只支持零。</param>
	/// <param name="value">成功时返回转换后的键分量，失败时返回默认值。</param>
	/// <returns>类别匹配、分量存在且可转换为指定类型时返回真，否则返回假。</returns>
	public bool TryGetValue<TValue>(string name, int ordinal, out TValue value) => _identifier.TryGetValue(name, ordinal, out value);
	/// <summary>替换指定序号的键分量，返回保留类别及元数据的新标识。</summary>
	/// <param name="ordinal">从零开始的键分量序号；单键只支持零。</param>
	/// <param name="value">替换值，按原键分量类型转换。</param>
	/// <returns>替换后的标识，与原标识共享扩展属性字典。</returns>
	/// <exception cref="InvalidOperationException">当前标识为空。</exception>
	/// <exception cref="ArgumentOutOfRangeException">分量序号越界。</exception>
	/// <exception cref="InvalidCastException">替换值无法转换为原分量类型。</exception>
	public Identifier<T1, T2, T3, T4> With(int ordinal, object value) => new(_identifier.With(ordinal, value));

	/// <summary>验证非空标识的逻辑名称。</summary>
	/// <param name="name">标识的逻辑名称，按不区分大小写的方式匹配。</param>
	/// <returns>标识非空、类别匹配时返回真，否则返回假。</returns>
	public bool Validate(string name) => _identifier.Validate(name);

	/// <summary>验证非空标识的逻辑名称及键值条件。</summary>
	/// <param name="name">标识的逻辑名称，按不区分大小写的方式匹配。</param>
	/// <param name="predicate">类别验证成功后执行一次的键值条件；复合键按顺序传入各分量。</param>
	/// <returns>标识非空、类别匹配且条件成立时返回真，否则返回假。</returns>
	/// <exception cref="ArgumentNullException">条件委托为空。</exception>
	/// <remarks>类别不符或标识为空时不执行条件委托；委托抛出的异常直接传播。</remarks>
	public bool Validate(string name, Func<T1, T2, T3, T4, bool> predicate)
	{
		ArgumentNullException.ThrowIfNull(predicate);
		return this.TryGetValue(name, out var value) && predicate(value.Item1, value.Item2, value.Item3, value.Item4);
	}
	#endregion

	#region 重写方法
	/// <summary>比较标识的类别及类型化键值。</summary>
	/// <param name="other">参与比较的另一个标识。</param>
	/// <returns>类别及类型化键相等时返回真，否则返回假；两个空标识相等。</returns>
	public bool Equals(Identifier<T1, T2, T3, T4> other) => _identifier.Equals(other._identifier);
	/// <summary>比较标识的类别及类型化键值。</summary>
	/// <param name="obj">参与比较的对象。</param>
	/// <returns>类别及类型化键相等时返回真，否则返回假；两个空标识相等。</returns>
	public override bool Equals(object obj) => obj is Identifier<T1, T2, T3, T4> other && this.Equals(other);
	/// <summary>获取由类别和类型化键生成的哈希代码。</summary>
	/// <returns>身份的哈希代码；空标识返回零，元数据不参与计算。</returns>
	public override int GetHashCode() => _identifier.GetHashCode();
	/// <summary>将标识格式化为统一的文本协议。</summary>
	/// <returns>包含类别、键值及非空元数据的标识文本；空标识返回空字符串。</returns>
	/// <exception cref="FormatException">键值不支持协议要求的文本转换。</exception>
	public override string ToString() => _identifier.ToString();
	/// <summary>将标识格式化为统一的文本协议。</summary>
	/// <param name="format">格式标记：空值、空字符串或 G 使用匹配的已登记简写；F 强制完整格式。</param>
	/// <returns>包含类别、键值及非空元数据的标识文本；空标识返回空字符串。</returns>
	/// <exception cref="FormatException">格式标记无效，或者键值不支持协议要求的文本转换。</exception>
	public string ToString(string format) => _identifier.ToString(format);
	/// <summary>将标识格式化为统一的文本协议。</summary>
	/// <param name="format">格式标记：空值、空字符串或 G 使用匹配的已登记简写；F 强制完整格式。</param>
	/// <param name="provider">保留的格式提供程序参数；标识协议始终使用固定文化格式。</param>
	/// <returns>包含类别、键值及非空元数据的标识文本；空标识返回空字符串。</returns>
	/// <exception cref="FormatException">格式标记无效，或者键值不支持协议要求的文本转换。</exception>
	public string ToString(string format, IFormatProvider provider) => _identifier.ToString(format, provider);
	#endregion

	#region 类型转换
	/// <summary>将标识转换为 <c>(T1, T2, T3, T4)</c>。</summary>
	/// <param name="identifier">待转换的标识。</param>
	/// <returns>从标识中取得或转换的键值。</returns>
	/// <remarks>仅提取键值；不返回包含类别和元数据的标识协议文本。</remarks>
	public static implicit operator (T1, T2, T3, T4)(Identifier<T1, T2, T3, T4> identifier) => identifier.Value;
	/// <summary>将标识转换为 <c>Identifier</c>。</summary>
	/// <param name="identifier">待转换的标识。</param>
	/// <returns>保留类别及元数据的标识；空标识转换为默认值。</returns>
	/// <remarks>扩展属性字典共享；需要改变键类型时复用键值转换规则。</remarks>
	public static implicit operator Identifier(Identifier<T1, T2, T3, T4> identifier) => identifier._identifier;
	/// <summary>将标识转换为 <c>Identifier&lt;T1, T2, T3, T4&gt;</c>。</summary>
	/// <param name="identifier">待转换的标识。</param>
	/// <returns>保留类别及元数据的标识；空标识转换为默认值。</returns>
	/// <remarks>扩展属性字典共享；需要改变键类型时复用键值转换规则。</remarks>
	/// <exception cref="InvalidCastException">键值无法转换为目标类型。</exception>
	public static explicit operator Identifier<T1, T2, T3, T4>(Identifier identifier) => new(identifier);
	/// <summary>将标识转换为 <c>Identifier&lt;(T1, T2, T3, T4)&gt;</c>。</summary>
	/// <param name="identifier">待转换的标识。</param>
	/// <returns>保留类别及元数据的标识；空标识转换为默认值。</returns>
	/// <remarks>扩展属性字典共享；需要改变键类型时复用键值转换规则。</remarks>
	public static implicit operator Identifier<(T1, T2, T3, T4)>(Identifier<T1, T2, T3, T4> identifier) => new(identifier._identifier);
	/// <summary>将标识转换为 <c>Identifier&lt;T1, T2, T3, T4&gt;</c>。</summary>
	/// <param name="identifier">待转换的标识。</param>
	/// <returns>保留类别及元数据的标识；空标识转换为默认值。</returns>
	/// <remarks>扩展属性字典共享；需要改变键类型时复用键值转换规则。</remarks>
	public static implicit operator Identifier<T1, T2, T3, T4>(Identifier<(T1, T2, T3, T4)> identifier) => new((Identifier)identifier);
	/// <summary>比较两个标识的身份是否相等。</summary>
	/// <param name="left">比较运算左侧的标识。</param>
	/// <param name="right">比较运算右侧的标识。</param>
	/// <returns>比较结果；元数据不参与身份比较。</returns>
	public static bool operator ==(Identifier<T1, T2, T3, T4> left, Identifier<T1, T2, T3, T4> right) => left.Equals(right);
	/// <summary>比较两个标识的身份是否不相等。</summary>
	/// <param name="left">比较运算左侧的标识。</param>
	/// <param name="right">比较运算右侧的标识。</param>
	/// <returns>比较结果；元数据不参与身份比较。</returns>
	public static bool operator !=(Identifier<T1, T2, T3, T4> left, Identifier<T1, T2, T3, T4> right) => !left.Equals(right);
	#endregion

	#region 解析方法
	/// <summary>解析标识文本。</summary>
	/// <param name="text">待解析的标识文本；空文本表示默认标识。空引用不被接受。</param>
	/// <returns>解析得到的标识；空文本返回默认值。</returns>
	/// <exception cref="ArgumentNullException">待解析文本为空引用。</exception>
	/// <exception cref="FormatException">文本格式、类别或键值类型不符合标识协议。</exception>
	/// <remarks>单键接受可赋值给泛型参数的实际键类型；复合键要求分量类型匹配。解析不执行数值类型转换。</remarks>
	public static Identifier<T1, T2, T3, T4> Parse(string text) => Parse(text, null);
	/// <summary>解析标识文本。</summary>
	/// <param name="text">待解析的标识文本；空文本表示默认标识。空引用不被接受。</param>
	/// <param name="provider">保留的格式提供程序参数；标识协议始终使用固定文化格式。</param>
	/// <returns>解析得到的标识；空文本返回默认值。</returns>
	/// <exception cref="ArgumentNullException">待解析文本为空引用。</exception>
	/// <exception cref="FormatException">文本格式、类别或键值类型不符合标识协议。</exception>
	/// <remarks>单键接受可赋值给泛型参数的实际键类型；复合键要求分量类型匹配。解析不执行数值类型转换。</remarks>
	public static Identifier<T1, T2, T3, T4> Parse(string text, IFormatProvider provider) => text == null ? throw new ArgumentNullException(nameof(text)) : Parse(text.AsSpan(), provider);
	/// <summary>解析标识文本。</summary>
	/// <param name="text">待解析的标识文本；空文本表示默认标识。</param>
	/// <returns>解析得到的标识；空文本返回默认值。</returns>
	/// <exception cref="FormatException">文本格式、类别或键值类型不符合标识协议。</exception>
	/// <remarks>单键接受可赋值给泛型参数的实际键类型；复合键要求分量类型匹配。解析不执行数值类型转换。</remarks>
	public static Identifier<T1, T2, T3, T4> Parse(ReadOnlySpan<char> text) => Parse(text, null);
	/// <summary>解析标识文本。</summary>
	/// <param name="text">待解析的标识文本；空文本表示默认标识。</param>
	/// <param name="provider">保留的格式提供程序参数；标识协议始终使用固定文化格式。</param>
	/// <returns>解析得到的标识；空文本返回默认值。</returns>
	/// <exception cref="FormatException">文本格式、类别或键值类型不符合标识协议。</exception>
	/// <remarks>单键接受可赋值给泛型参数的实际键类型；复合键要求分量类型匹配。解析不执行数值类型转换。</remarks>
	public static Identifier<T1, T2, T3, T4> Parse(ReadOnlySpan<char> text, IFormatProvider provider) => TryParse(text, provider, out var result) ? result : throw new FormatException(Zongsoft.Properties.Resources.Identifier_InvalidFormat_Message);
	/// <summary>尝试解析标识文本。</summary>
	/// <param name="text">待解析的标识文本；空文本表示默认标识。空引用返回失败。</param>
	/// <param name="result">解析成功时返回标识，失败时返回默认值。</param>
	/// <returns>文本可解析时返回真，包括空文本；非法输入返回假。</returns>
	/// <remarks>单键接受可赋值给泛型参数的实际键类型；复合键要求分量类型匹配。解析不执行数值类型转换。</remarks>
	public static bool TryParse(string text, out Identifier<T1, T2, T3, T4> result) => TryParse(text, null, out result);
	/// <summary>尝试解析标识文本。</summary>
	/// <param name="text">待解析的标识文本；空文本表示默认标识。空引用返回失败。</param>
	/// <param name="provider">保留的格式提供程序参数；标识协议始终使用固定文化格式。</param>
	/// <param name="result">解析成功时返回标识，失败时返回默认值。</param>
	/// <returns>文本可解析时返回真，包括空文本；非法输入返回假。</returns>
	/// <remarks>单键接受可赋值给泛型参数的实际键类型；复合键要求分量类型匹配。解析不执行数值类型转换。</remarks>
	public static bool TryParse(string text, IFormatProvider provider, out Identifier<T1, T2, T3, T4> result)
	{
		result = default;
		return text != null && TryParse(text.AsSpan(), provider, out result);
	}
	/// <summary>尝试解析标识文本。</summary>
	/// <param name="text">待解析的标识文本；空文本表示默认标识。</param>
	/// <param name="result">解析成功时返回标识，失败时返回默认值。</param>
	/// <returns>文本可解析时返回真，包括空文本；非法输入返回假。</returns>
	/// <remarks>单键接受可赋值给泛型参数的实际键类型；复合键要求分量类型匹配。解析不执行数值类型转换。</remarks>
	public static bool TryParse(ReadOnlySpan<char> text, out Identifier<T1, T2, T3, T4> result) => TryParse(text, null, out result);
	/// <summary>尝试解析标识文本。</summary>
	/// <param name="text">待解析的标识文本；空文本表示默认标识。</param>
	/// <param name="provider">保留的格式提供程序参数；标识协议始终使用固定文化格式。</param>
	/// <param name="result">解析成功时返回标识，失败时返回默认值。</param>
	/// <returns>文本可解析时返回真，包括空文本；非法输入返回假。</returns>
	/// <remarks>单键接受可赋值给泛型参数的实际键类型；复合键要求分量类型匹配。解析不执行数值类型转换。</remarks>
	public static bool TryParse(ReadOnlySpan<char> text, IFormatProvider provider, out Identifier<T1, T2, T3, T4> result)
	{
		result = default;
		if(!Identifier.TryParseCore(text, [typeof(T1), typeof(T2), typeof(T3), typeof(T4)], out var identifier))
			return false;

		result = new(identifier);
		return true;
	}
	#endregion
}
