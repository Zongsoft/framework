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
using System.ComponentModel;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Zongsoft.Components;

/// <summary>表示具有类型化键的对象标识。</summary>
[TypeConverter(typeof(IdentifierConverter))]
[JsonConverter(typeof(IdentifierJsonConverter))]
public readonly struct Identifier<T> : IEquatable<Identifier<T>>, ISpanParsable<Identifier<T>>, IFormattable
{
	#region 私有字段
	private readonly Identifier _identifier;
	#endregion

	#region 构造函数
	public Identifier(string name, T value, string label = null, string description = null, string alias = null, IDictionary<string, string> properties = null, string code = null) =>
		_identifier = new(name, value, label, description, alias, properties, code);
	public Identifier(Type type, T value, string label = null, string description = null, string alias = null, IDictionary<string, string> properties = null, string code = null) =>
		_identifier = new(type, value, label, description, alias, properties, code);
	internal Identifier(Identifier identifier)
	{
		if(identifier.IsEmpty)
			_identifier = default;
		else if(identifier.Validate<T>(out var value))
			_identifier = new(identifier.Name, identifier.Type, value, identifier.Label, identifier.Description, identifier.Alias, identifier.Code, identifier.Properties, false);
		else
			throw new InvalidCastException(Zongsoft.Properties.Resources.Identifier_InvalidFormat_Message);
	}
	#endregion

	#region 公共属性
	public string Name => _identifier.Name;
	public Type Type => _identifier.Type;
	public T Value => this.IsEmpty ? default : (T)_identifier.Value;
	public string Code => _identifier.Code;
	public string Label => _identifier.Label;
	public string Alias => _identifier.Alias;
	public string Description => _identifier.Description;
	public IDictionary<string, string> Properties => _identifier.Properties;
	public bool IsEmpty => _identifier.IsEmpty;
	public bool HasValue => _identifier.HasValue;
	#endregion

	#region 验证方法
	public bool Validate(out T value) => _identifier.Validate(out value);
	public bool Validate<TType>(out T value) => _identifier.Validate<TType, T>(out value);
	public bool Validate(Type type, out T value) => _identifier.Validate(type, out value);
	public bool Validate(string name, out T value) => _identifier.Validate(name, out value);
	public bool Validate(string name) => _identifier.Validate(name);

	/// <summary>验证标识的逻辑名称及键值条件。</summary>
	/// <param name="name">指定的逻辑名称。</param>
	/// <param name="predicate">类别匹配后执行的键值判断。</param>
	/// <returns>类别匹配且键值条件成立则返回真，否则返回假。</returns>
	public bool Validate(string name, Func<T, bool> predicate)
	{
		ArgumentNullException.ThrowIfNull(predicate);
		return this.Validate(name, out var value) && predicate(value);
	}

	/// <summary>验证标识的 CLR 类型及键值条件。</summary>
	/// <param name="type">指定的 CLR 类型。</param>
	/// <param name="predicate">类别匹配后执行的键值判断。</param>
	/// <returns>类别匹配且键值条件成立则返回真，否则返回假。</returns>
	public bool Validate(Type type, Func<T, bool> predicate)
	{
		ArgumentNullException.ThrowIfNull(predicate);
		return this.Validate(type, out var value) && predicate(value);
	}
	#endregion

	#region 重写方法
	public bool Equals(Identifier<T> other) => _identifier.Equals(other._identifier);
	public override bool Equals(object obj) => obj is Identifier<T> other && this.Equals(other);
	public override int GetHashCode() => _identifier.GetHashCode();
	public override string ToString() => _identifier.ToString();
	public string ToString(string format) => _identifier.ToString(format);
	public string ToString(string format, IFormatProvider provider) => _identifier.ToString(format, provider);
	#endregion

	#region 类型转换
	public static implicit operator T(Identifier<T> identifier) => identifier.Value;
	public static implicit operator Identifier(Identifier<T> identifier) => identifier._identifier;
	public static explicit operator Identifier<T>(Identifier identifier) => new(identifier);
	public static bool operator ==(Identifier<T> left, Identifier<T> right) => left.Equals(right);
	public static bool operator !=(Identifier<T> left, Identifier<T> right) => !left.Equals(right);
	#endregion

	#region 解析方法
	public static Identifier<T> Parse(string text) => Parse(text, null);
	public static Identifier<T> Parse(string text, IFormatProvider provider) => text == null ? throw new ArgumentNullException(nameof(text)) : Parse(text.AsSpan(), provider);
	public static Identifier<T> Parse(ReadOnlySpan<char> text) => Parse(text, null);
	public static Identifier<T> Parse(ReadOnlySpan<char> text, IFormatProvider provider) => TryParse(text, provider, out var result) ? result : throw new FormatException(Zongsoft.Properties.Resources.Identifier_InvalidFormat_Message);
	public static bool TryParse(string text, out Identifier<T> result) => TryParse(text, null, out result);
	public static bool TryParse(string text, IFormatProvider provider, out Identifier<T> result)
	{
		result = default;
		return text != null && TryParse(text.AsSpan(), provider, out result);
	}
	public static bool TryParse(ReadOnlySpan<char> text, out Identifier<T> result) => TryParse(text, null, out result);
	public static bool TryParse(ReadOnlySpan<char> text, IFormatProvider provider, out Identifier<T> result)
	{
		result = default;
		if(!Identifier.TryParseCore(text, Identifier.GetKeyTypes(typeof(T)), out var identifier))
			return false;

		result = new(identifier);
		return true;
	}
	#endregion
}

/// <summary>表示具有类型化键的对象标识。</summary>
[TypeConverter(typeof(IdentifierConverter))]
[JsonConverter(typeof(IdentifierJsonConverter))]
public readonly struct Identifier<T1, T2> : IEquatable<Identifier<T1, T2>>, ISpanParsable<Identifier<T1, T2>>, IFormattable
{
	#region 私有字段
	private readonly Identifier _identifier;
	#endregion

	#region 构造函数
	public Identifier(string name, T1 value1, T2 value2, string label = null, string description = null, string alias = null, IDictionary<string, string> properties = null, string code = null) =>
		_identifier = new(name, (value1, value2), label, description, alias, properties, code);
	public Identifier(Type type, T1 value1, T2 value2, string label = null, string description = null, string alias = null, IDictionary<string, string> properties = null, string code = null) =>
		_identifier = new(type, (value1, value2), label, description, alias, properties, code);
	public Identifier(string name, (T1, T2) value, string label = null, string description = null, string alias = null, IDictionary<string, string> properties = null, string code = null) =>
		_identifier = new(name, value, label, description, alias, properties, code);
	public Identifier(Type type, (T1, T2) value, string label = null, string description = null, string alias = null, IDictionary<string, string> properties = null, string code = null) =>
		_identifier = new(type, value, label, description, alias, properties, code);
	internal Identifier(Identifier identifier)
	{
		if(identifier.IsEmpty)
			_identifier = default;
		else if(identifier.Validate<(T1, T2)>(out var value))
			_identifier = new(identifier.Name, identifier.Type, value, identifier.Label, identifier.Description, identifier.Alias, identifier.Code, identifier.Properties, false);
		else
			throw new InvalidCastException(Zongsoft.Properties.Resources.Identifier_InvalidFormat_Message);
	}
	#endregion

	#region 公共属性
	public string Name => _identifier.Name;
	public Type Type => _identifier.Type;
	public (T1, T2) Value => this.IsEmpty ? default : ((T1, T2))_identifier.Value;
	public string Code => _identifier.Code;
	public string Label => _identifier.Label;
	public string Alias => _identifier.Alias;
	public string Description => _identifier.Description;
	public IDictionary<string, string> Properties => _identifier.Properties;
	public bool IsEmpty => _identifier.IsEmpty;
	public bool HasValue => _identifier.HasValue;
	#endregion

	#region 公共方法
	/// <summary>将复合标识解构为各个键分量。</summary>
	/// <param name="value1">第 1 个键分量。</param>
	/// <param name="value2">第 2 个键分量。</param>
	public void Deconstruct(out T1 value1, out T2 value2) => (value1, value2) = this.Value;
	#endregion

	#region 验证方法
	public bool Validate(out (T1, T2) value) => _identifier.Validate(out value);
	public bool Validate<TType>(out (T1, T2) value) => _identifier.Validate<TType, (T1, T2)>(out value);
	public bool Validate(Type type, out (T1, T2) value) => _identifier.Validate(type, out value);
	public bool Validate(string name, out (T1, T2) value) => _identifier.Validate(name, out value);
	public bool Validate(string name) => _identifier.Validate(name);

	/// <summary>验证标识的逻辑名称及键值条件。</summary>
	/// <param name="name">指定的逻辑名称。</param>
	/// <param name="predicate">类别匹配后执行的键值判断。</param>
	/// <returns>类别匹配且键值条件成立则返回真，否则返回假。</returns>
	public bool Validate(string name, Func<T1, T2, bool> predicate)
	{
		ArgumentNullException.ThrowIfNull(predicate);
		return this.Validate(name, out var value) && predicate(value.Item1, value.Item2);
	}

	/// <summary>验证标识的 CLR 类型及键值条件。</summary>
	/// <param name="type">指定的 CLR 类型。</param>
	/// <param name="predicate">类别匹配后执行的键值判断。</param>
	/// <returns>类别匹配且键值条件成立则返回真，否则返回假。</returns>
	public bool Validate(Type type, Func<T1, T2, bool> predicate)
	{
		ArgumentNullException.ThrowIfNull(predicate);
		return this.Validate(type, out var value) && predicate(value.Item1, value.Item2);
	}
	#endregion

	#region 重写方法
	public bool Equals(Identifier<T1, T2> other) => _identifier.Equals(other._identifier);
	public override bool Equals(object obj) => obj is Identifier<T1, T2> other && this.Equals(other);
	public override int GetHashCode() => _identifier.GetHashCode();
	public override string ToString() => _identifier.ToString();
	public string ToString(string format) => _identifier.ToString(format);
	public string ToString(string format, IFormatProvider provider) => _identifier.ToString(format, provider);
	#endregion

	#region 类型转换
	public static implicit operator (T1, T2)(Identifier<T1, T2> identifier) => identifier.Value;
	public static implicit operator Identifier(Identifier<T1, T2> identifier) => identifier._identifier;
	public static explicit operator Identifier<T1, T2>(Identifier identifier) => new(identifier);
	public static implicit operator Identifier<(T1, T2)>(Identifier<T1, T2> identifier) => new(identifier._identifier);
	public static implicit operator Identifier<T1, T2>(Identifier<(T1, T2)> identifier) => new((Identifier)identifier);
	public static bool operator ==(Identifier<T1, T2> left, Identifier<T1, T2> right) => left.Equals(right);
	public static bool operator !=(Identifier<T1, T2> left, Identifier<T1, T2> right) => !left.Equals(right);
	#endregion

	#region 解析方法
	public static Identifier<T1, T2> Parse(string text) => Parse(text, null);
	public static Identifier<T1, T2> Parse(string text, IFormatProvider provider) => text == null ? throw new ArgumentNullException(nameof(text)) : Parse(text.AsSpan(), provider);
	public static Identifier<T1, T2> Parse(ReadOnlySpan<char> text) => Parse(text, null);
	public static Identifier<T1, T2> Parse(ReadOnlySpan<char> text, IFormatProvider provider) => TryParse(text, provider, out var result) ? result : throw new FormatException(Zongsoft.Properties.Resources.Identifier_InvalidFormat_Message);
	public static bool TryParse(string text, out Identifier<T1, T2> result) => TryParse(text, null, out result);
	public static bool TryParse(string text, IFormatProvider provider, out Identifier<T1, T2> result)
	{
		result = default;
		return text != null && TryParse(text.AsSpan(), provider, out result);
	}
	public static bool TryParse(ReadOnlySpan<char> text, out Identifier<T1, T2> result) => TryParse(text, null, out result);
	public static bool TryParse(ReadOnlySpan<char> text, IFormatProvider provider, out Identifier<T1, T2> result)
	{
		result = default;
		if(!Identifier.TryParseCore(text, [typeof(T1), typeof(T2)], out var identifier))
			return false;

		result = new(identifier);
		return true;
	}
	#endregion
}

/// <summary>表示具有类型化键的对象标识。</summary>
[TypeConverter(typeof(IdentifierConverter))]
[JsonConverter(typeof(IdentifierJsonConverter))]
public readonly struct Identifier<T1, T2, T3> : IEquatable<Identifier<T1, T2, T3>>, ISpanParsable<Identifier<T1, T2, T3>>, IFormattable
{
	#region 私有字段
	private readonly Identifier _identifier;
	#endregion

	#region 构造函数
	public Identifier(string name, T1 value1, T2 value2, T3 value3, string label = null, string description = null, string alias = null, IDictionary<string, string> properties = null, string code = null) =>
		_identifier = new(name, (value1, value2, value3), label, description, alias, properties, code);
	public Identifier(Type type, T1 value1, T2 value2, T3 value3, string label = null, string description = null, string alias = null, IDictionary<string, string> properties = null, string code = null) =>
		_identifier = new(type, (value1, value2, value3), label, description, alias, properties, code);
	public Identifier(string name, (T1, T2, T3) value, string label = null, string description = null, string alias = null, IDictionary<string, string> properties = null, string code = null) =>
		_identifier = new(name, value, label, description, alias, properties, code);
	public Identifier(Type type, (T1, T2, T3) value, string label = null, string description = null, string alias = null, IDictionary<string, string> properties = null, string code = null) =>
		_identifier = new(type, value, label, description, alias, properties, code);
	internal Identifier(Identifier identifier)
	{
		if(identifier.IsEmpty)
			_identifier = default;
		else if(identifier.Validate<(T1, T2, T3)>(out var value))
			_identifier = new(identifier.Name, identifier.Type, value, identifier.Label, identifier.Description, identifier.Alias, identifier.Code, identifier.Properties, false);
		else
			throw new InvalidCastException(Zongsoft.Properties.Resources.Identifier_InvalidFormat_Message);
	}
	#endregion

	#region 公共属性
	public string Name => _identifier.Name;
	public Type Type => _identifier.Type;
	public (T1, T2, T3) Value => this.IsEmpty ? default : ((T1, T2, T3))_identifier.Value;
	public string Code => _identifier.Code;
	public string Label => _identifier.Label;
	public string Alias => _identifier.Alias;
	public string Description => _identifier.Description;
	public IDictionary<string, string> Properties => _identifier.Properties;
	public bool IsEmpty => _identifier.IsEmpty;
	public bool HasValue => _identifier.HasValue;
	#endregion

	#region 公共方法
	/// <summary>将复合标识解构为各个键分量。</summary>
	/// <param name="value1">第 1 个键分量。</param>
	/// <param name="value2">第 2 个键分量。</param>
	/// <param name="value3">第 3 个键分量。</param>
	public void Deconstruct(out T1 value1, out T2 value2, out T3 value3) => (value1, value2, value3) = this.Value;
	#endregion

	#region 验证方法
	public bool Validate(out (T1, T2, T3) value) => _identifier.Validate(out value);
	public bool Validate<TType>(out (T1, T2, T3) value) => _identifier.Validate<TType, (T1, T2, T3)>(out value);
	public bool Validate(Type type, out (T1, T2, T3) value) => _identifier.Validate(type, out value);
	public bool Validate(string name, out (T1, T2, T3) value) => _identifier.Validate(name, out value);
	public bool Validate(string name) => _identifier.Validate(name);

	/// <summary>验证标识的逻辑名称及键值条件。</summary>
	/// <param name="name">指定的逻辑名称。</param>
	/// <param name="predicate">类别匹配后执行的键值判断。</param>
	/// <returns>类别匹配且键值条件成立则返回真，否则返回假。</returns>
	public bool Validate(string name, Func<T1, T2, T3, bool> predicate)
	{
		ArgumentNullException.ThrowIfNull(predicate);
		return this.Validate(name, out var value) && predicate(value.Item1, value.Item2, value.Item3);
	}

	/// <summary>验证标识的 CLR 类型及键值条件。</summary>
	/// <param name="type">指定的 CLR 类型。</param>
	/// <param name="predicate">类别匹配后执行的键值判断。</param>
	/// <returns>类别匹配且键值条件成立则返回真，否则返回假。</returns>
	public bool Validate(Type type, Func<T1, T2, T3, bool> predicate)
	{
		ArgumentNullException.ThrowIfNull(predicate);
		return this.Validate(type, out var value) && predicate(value.Item1, value.Item2, value.Item3);
	}
	#endregion

	#region 重写方法
	public bool Equals(Identifier<T1, T2, T3> other) => _identifier.Equals(other._identifier);
	public override bool Equals(object obj) => obj is Identifier<T1, T2, T3> other && this.Equals(other);
	public override int GetHashCode() => _identifier.GetHashCode();
	public override string ToString() => _identifier.ToString();
	public string ToString(string format) => _identifier.ToString(format);
	public string ToString(string format, IFormatProvider provider) => _identifier.ToString(format, provider);
	#endregion

	#region 类型转换
	public static implicit operator (T1, T2, T3)(Identifier<T1, T2, T3> identifier) => identifier.Value;
	public static implicit operator Identifier(Identifier<T1, T2, T3> identifier) => identifier._identifier;
	public static explicit operator Identifier<T1, T2, T3>(Identifier identifier) => new(identifier);
	public static implicit operator Identifier<(T1, T2, T3)>(Identifier<T1, T2, T3> identifier) => new(identifier._identifier);
	public static implicit operator Identifier<T1, T2, T3>(Identifier<(T1, T2, T3)> identifier) => new((Identifier)identifier);
	public static bool operator ==(Identifier<T1, T2, T3> left, Identifier<T1, T2, T3> right) => left.Equals(right);
	public static bool operator !=(Identifier<T1, T2, T3> left, Identifier<T1, T2, T3> right) => !left.Equals(right);
	#endregion

	#region 解析方法
	public static Identifier<T1, T2, T3> Parse(string text) => Parse(text, null);
	public static Identifier<T1, T2, T3> Parse(string text, IFormatProvider provider) => text == null ? throw new ArgumentNullException(nameof(text)) : Parse(text.AsSpan(), provider);
	public static Identifier<T1, T2, T3> Parse(ReadOnlySpan<char> text) => Parse(text, null);
	public static Identifier<T1, T2, T3> Parse(ReadOnlySpan<char> text, IFormatProvider provider) => TryParse(text, provider, out var result) ? result : throw new FormatException(Zongsoft.Properties.Resources.Identifier_InvalidFormat_Message);
	public static bool TryParse(string text, out Identifier<T1, T2, T3> result) => TryParse(text, null, out result);
	public static bool TryParse(string text, IFormatProvider provider, out Identifier<T1, T2, T3> result)
	{
		result = default;
		return text != null && TryParse(text.AsSpan(), provider, out result);
	}
	public static bool TryParse(ReadOnlySpan<char> text, out Identifier<T1, T2, T3> result) => TryParse(text, null, out result);
	public static bool TryParse(ReadOnlySpan<char> text, IFormatProvider provider, out Identifier<T1, T2, T3> result)
	{
		result = default;
		if(!Identifier.TryParseCore(text, [typeof(T1), typeof(T2), typeof(T3)], out var identifier))
			return false;

		result = new(identifier);
		return true;
	}
	#endregion
}

/// <summary>表示具有类型化键的对象标识。</summary>
[TypeConverter(typeof(IdentifierConverter))]
[JsonConverter(typeof(IdentifierJsonConverter))]
public readonly struct Identifier<T1, T2, T3, T4> : IEquatable<Identifier<T1, T2, T3, T4>>, ISpanParsable<Identifier<T1, T2, T3, T4>>, IFormattable
{
	#region 私有字段
	private readonly Identifier _identifier;
	#endregion

	#region 构造函数
	public Identifier(string name, T1 value1, T2 value2, T3 value3, T4 value4, string label = null, string description = null, string alias = null, IDictionary<string, string> properties = null, string code = null) =>
		_identifier = new(name, (value1, value2, value3, value4), label, description, alias, properties, code);
	public Identifier(Type type, T1 value1, T2 value2, T3 value3, T4 value4, string label = null, string description = null, string alias = null, IDictionary<string, string> properties = null, string code = null) =>
		_identifier = new(type, (value1, value2, value3, value4), label, description, alias, properties, code);
	public Identifier(string name, (T1, T2, T3, T4) value, string label = null, string description = null, string alias = null, IDictionary<string, string> properties = null, string code = null) =>
		_identifier = new(name, value, label, description, alias, properties, code);
	public Identifier(Type type, (T1, T2, T3, T4) value, string label = null, string description = null, string alias = null, IDictionary<string, string> properties = null, string code = null) =>
		_identifier = new(type, value, label, description, alias, properties, code);
	internal Identifier(Identifier identifier)
	{
		if(identifier.IsEmpty)
			_identifier = default;
		else if(identifier.Validate<(T1, T2, T3, T4)>(out var value))
			_identifier = new(identifier.Name, identifier.Type, value, identifier.Label, identifier.Description, identifier.Alias, identifier.Code, identifier.Properties, false);
		else
			throw new InvalidCastException(Zongsoft.Properties.Resources.Identifier_InvalidFormat_Message);
	}
	#endregion

	#region 公共属性
	public string Name => _identifier.Name;
	public Type Type => _identifier.Type;
	public (T1, T2, T3, T4) Value => this.IsEmpty ? default : ((T1, T2, T3, T4))_identifier.Value;
	public string Code => _identifier.Code;
	public string Label => _identifier.Label;
	public string Alias => _identifier.Alias;
	public string Description => _identifier.Description;
	public IDictionary<string, string> Properties => _identifier.Properties;
	public bool IsEmpty => _identifier.IsEmpty;
	public bool HasValue => _identifier.HasValue;
	#endregion

	#region 公共方法
	/// <summary>将复合标识解构为各个键分量。</summary>
	/// <param name="value1">第 1 个键分量。</param>
	/// <param name="value2">第 2 个键分量。</param>
	/// <param name="value3">第 3 个键分量。</param>
	/// <param name="value4">第 4 个键分量。</param>
	public void Deconstruct(out T1 value1, out T2 value2, out T3 value3, out T4 value4) => (value1, value2, value3, value4) = this.Value;
	#endregion

	#region 验证方法
	public bool Validate(out (T1, T2, T3, T4) value) => _identifier.Validate(out value);
	public bool Validate<TType>(out (T1, T2, T3, T4) value) => _identifier.Validate<TType, (T1, T2, T3, T4)>(out value);
	public bool Validate(Type type, out (T1, T2, T3, T4) value) => _identifier.Validate(type, out value);
	public bool Validate(string name, out (T1, T2, T3, T4) value) => _identifier.Validate(name, out value);
	public bool Validate(string name) => _identifier.Validate(name);

	/// <summary>验证标识的逻辑名称及键值条件。</summary>
	/// <param name="name">指定的逻辑名称。</param>
	/// <param name="predicate">类别匹配后执行的键值判断。</param>
	/// <returns>类别匹配且键值条件成立则返回真，否则返回假。</returns>
	public bool Validate(string name, Func<T1, T2, T3, T4, bool> predicate)
	{
		ArgumentNullException.ThrowIfNull(predicate);
		return this.Validate(name, out var value) && predicate(value.Item1, value.Item2, value.Item3, value.Item4);
	}

	/// <summary>验证标识的 CLR 类型及键值条件。</summary>
	/// <param name="type">指定的 CLR 类型。</param>
	/// <param name="predicate">类别匹配后执行的键值判断。</param>
	/// <returns>类别匹配且键值条件成立则返回真，否则返回假。</returns>
	public bool Validate(Type type, Func<T1, T2, T3, T4, bool> predicate)
	{
		ArgumentNullException.ThrowIfNull(predicate);
		return this.Validate(type, out var value) && predicate(value.Item1, value.Item2, value.Item3, value.Item4);
	}
	#endregion

	#region 重写方法
	public bool Equals(Identifier<T1, T2, T3, T4> other) => _identifier.Equals(other._identifier);
	public override bool Equals(object obj) => obj is Identifier<T1, T2, T3, T4> other && this.Equals(other);
	public override int GetHashCode() => _identifier.GetHashCode();
	public override string ToString() => _identifier.ToString();
	public string ToString(string format) => _identifier.ToString(format);
	public string ToString(string format, IFormatProvider provider) => _identifier.ToString(format, provider);
	#endregion

	#region 类型转换
	public static implicit operator (T1, T2, T3, T4)(Identifier<T1, T2, T3, T4> identifier) => identifier.Value;
	public static implicit operator Identifier(Identifier<T1, T2, T3, T4> identifier) => identifier._identifier;
	public static explicit operator Identifier<T1, T2, T3, T4>(Identifier identifier) => new(identifier);
	public static implicit operator Identifier<(T1, T2, T3, T4)>(Identifier<T1, T2, T3, T4> identifier) => new(identifier._identifier);
	public static implicit operator Identifier<T1, T2, T3, T4>(Identifier<(T1, T2, T3, T4)> identifier) => new((Identifier)identifier);
	public static bool operator ==(Identifier<T1, T2, T3, T4> left, Identifier<T1, T2, T3, T4> right) => left.Equals(right);
	public static bool operator !=(Identifier<T1, T2, T3, T4> left, Identifier<T1, T2, T3, T4> right) => !left.Equals(right);
	#endregion

	#region 解析方法
	public static Identifier<T1, T2, T3, T4> Parse(string text) => Parse(text, null);
	public static Identifier<T1, T2, T3, T4> Parse(string text, IFormatProvider provider) => text == null ? throw new ArgumentNullException(nameof(text)) : Parse(text.AsSpan(), provider);
	public static Identifier<T1, T2, T3, T4> Parse(ReadOnlySpan<char> text) => Parse(text, null);
	public static Identifier<T1, T2, T3, T4> Parse(ReadOnlySpan<char> text, IFormatProvider provider) => TryParse(text, provider, out var result) ? result : throw new FormatException(Zongsoft.Properties.Resources.Identifier_InvalidFormat_Message);
	public static bool TryParse(string text, out Identifier<T1, T2, T3, T4> result) => TryParse(text, null, out result);
	public static bool TryParse(string text, IFormatProvider provider, out Identifier<T1, T2, T3, T4> result)
	{
		result = default;
		return text != null && TryParse(text.AsSpan(), provider, out result);
	}
	public static bool TryParse(ReadOnlySpan<char> text, out Identifier<T1, T2, T3, T4> result) => TryParse(text, null, out result);
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

/// <summary>表示具有类型化键的对象标识。</summary>
[TypeConverter(typeof(IdentifierConverter))]
[JsonConverter(typeof(IdentifierJsonConverter))]
public readonly struct Identifier<T1, T2, T3, T4, T5> : IEquatable<Identifier<T1, T2, T3, T4, T5>>, ISpanParsable<Identifier<T1, T2, T3, T4, T5>>, IFormattable
{
	#region 私有字段
	private readonly Identifier _identifier;
	#endregion

	#region 构造函数
	public Identifier(string name, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, string label = null, string description = null, string alias = null, IDictionary<string, string> properties = null, string code = null) =>
		_identifier = new(name, (value1, value2, value3, value4, value5), label, description, alias, properties, code);
	public Identifier(Type type, T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, string label = null, string description = null, string alias = null, IDictionary<string, string> properties = null, string code = null) =>
		_identifier = new(type, (value1, value2, value3, value4, value5), label, description, alias, properties, code);
	public Identifier(string name, (T1, T2, T3, T4, T5) value, string label = null, string description = null, string alias = null, IDictionary<string, string> properties = null, string code = null) =>
		_identifier = new(name, value, label, description, alias, properties, code);
	public Identifier(Type type, (T1, T2, T3, T4, T5) value, string label = null, string description = null, string alias = null, IDictionary<string, string> properties = null, string code = null) =>
		_identifier = new(type, value, label, description, alias, properties, code);
	internal Identifier(Identifier identifier)
	{
		if(identifier.IsEmpty)
			_identifier = default;
		else if(identifier.Validate<(T1, T2, T3, T4, T5)>(out var value))
			_identifier = new(identifier.Name, identifier.Type, value, identifier.Label, identifier.Description, identifier.Alias, identifier.Code, identifier.Properties, false);
		else
			throw new InvalidCastException(Zongsoft.Properties.Resources.Identifier_InvalidFormat_Message);
	}
	#endregion

	#region 公共属性
	public string Name => _identifier.Name;
	public Type Type => _identifier.Type;
	public (T1, T2, T3, T4, T5) Value => this.IsEmpty ? default : ((T1, T2, T3, T4, T5))_identifier.Value;
	public string Code => _identifier.Code;
	public string Label => _identifier.Label;
	public string Alias => _identifier.Alias;
	public string Description => _identifier.Description;
	public IDictionary<string, string> Properties => _identifier.Properties;
	public bool IsEmpty => _identifier.IsEmpty;
	public bool HasValue => _identifier.HasValue;
	#endregion

	#region 公共方法
	/// <summary>将复合标识解构为各个键分量。</summary>
	/// <param name="value1">第 1 个键分量。</param>
	/// <param name="value2">第 2 个键分量。</param>
	/// <param name="value3">第 3 个键分量。</param>
	/// <param name="value4">第 4 个键分量。</param>
	/// <param name="value5">第 5 个键分量。</param>
	public void Deconstruct(out T1 value1, out T2 value2, out T3 value3, out T4 value4, out T5 value5) => (value1, value2, value3, value4, value5) = this.Value;
	#endregion

	#region 验证方法
	public bool Validate(out (T1, T2, T3, T4, T5) value) => _identifier.Validate(out value);
	public bool Validate<TType>(out (T1, T2, T3, T4, T5) value) => _identifier.Validate<TType, (T1, T2, T3, T4, T5)>(out value);
	public bool Validate(Type type, out (T1, T2, T3, T4, T5) value) => _identifier.Validate(type, out value);
	public bool Validate(string name, out (T1, T2, T3, T4, T5) value) => _identifier.Validate(name, out value);
	public bool Validate(string name) => _identifier.Validate(name);

	/// <summary>验证标识的逻辑名称及键值条件。</summary>
	/// <param name="name">指定的逻辑名称。</param>
	/// <param name="predicate">类别匹配后执行的键值判断。</param>
	/// <returns>类别匹配且键值条件成立则返回真，否则返回假。</returns>
	public bool Validate(string name, Func<T1, T2, T3, T4, T5, bool> predicate)
	{
		ArgumentNullException.ThrowIfNull(predicate);
		return this.Validate(name, out var value) && predicate(value.Item1, value.Item2, value.Item3, value.Item4, value.Item5);
	}

	/// <summary>验证标识的 CLR 类型及键值条件。</summary>
	/// <param name="type">指定的 CLR 类型。</param>
	/// <param name="predicate">类别匹配后执行的键值判断。</param>
	/// <returns>类别匹配且键值条件成立则返回真，否则返回假。</returns>
	public bool Validate(Type type, Func<T1, T2, T3, T4, T5, bool> predicate)
	{
		ArgumentNullException.ThrowIfNull(predicate);
		return this.Validate(type, out var value) && predicate(value.Item1, value.Item2, value.Item3, value.Item4, value.Item5);
	}
	#endregion

	#region 重写方法
	public bool Equals(Identifier<T1, T2, T3, T4, T5> other) => _identifier.Equals(other._identifier);
	public override bool Equals(object obj) => obj is Identifier<T1, T2, T3, T4, T5> other && this.Equals(other);
	public override int GetHashCode() => _identifier.GetHashCode();
	public override string ToString() => _identifier.ToString();
	public string ToString(string format) => _identifier.ToString(format);
	public string ToString(string format, IFormatProvider provider) => _identifier.ToString(format, provider);
	#endregion

	#region 类型转换
	public static implicit operator (T1, T2, T3, T4, T5)(Identifier<T1, T2, T3, T4, T5> identifier) => identifier.Value;
	public static implicit operator Identifier(Identifier<T1, T2, T3, T4, T5> identifier) => identifier._identifier;
	public static explicit operator Identifier<T1, T2, T3, T4, T5>(Identifier identifier) => new(identifier);
	public static implicit operator Identifier<(T1, T2, T3, T4, T5)>(Identifier<T1, T2, T3, T4, T5> identifier) => new(identifier._identifier);
	public static implicit operator Identifier<T1, T2, T3, T4, T5>(Identifier<(T1, T2, T3, T4, T5)> identifier) => new((Identifier)identifier);
	public static bool operator ==(Identifier<T1, T2, T3, T4, T5> left, Identifier<T1, T2, T3, T4, T5> right) => left.Equals(right);
	public static bool operator !=(Identifier<T1, T2, T3, T4, T5> left, Identifier<T1, T2, T3, T4, T5> right) => !left.Equals(right);
	#endregion

	#region 解析方法
	public static Identifier<T1, T2, T3, T4, T5> Parse(string text) => Parse(text, null);
	public static Identifier<T1, T2, T3, T4, T5> Parse(string text, IFormatProvider provider) => text == null ? throw new ArgumentNullException(nameof(text)) : Parse(text.AsSpan(), provider);
	public static Identifier<T1, T2, T3, T4, T5> Parse(ReadOnlySpan<char> text) => Parse(text, null);
	public static Identifier<T1, T2, T3, T4, T5> Parse(ReadOnlySpan<char> text, IFormatProvider provider) => TryParse(text, provider, out var result) ? result : throw new FormatException(Zongsoft.Properties.Resources.Identifier_InvalidFormat_Message);
	public static bool TryParse(string text, out Identifier<T1, T2, T3, T4, T5> result) => TryParse(text, null, out result);
	public static bool TryParse(string text, IFormatProvider provider, out Identifier<T1, T2, T3, T4, T5> result)
	{
		result = default;
		return text != null && TryParse(text.AsSpan(), provider, out result);
	}
	public static bool TryParse(ReadOnlySpan<char> text, out Identifier<T1, T2, T3, T4, T5> result) => TryParse(text, null, out result);
	public static bool TryParse(ReadOnlySpan<char> text, IFormatProvider provider, out Identifier<T1, T2, T3, T4, T5> result)
	{
		result = default;
		if(!Identifier.TryParseCore(text, [typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5)], out var identifier))
			return false;

		result = new(identifier);
		return true;
	}
	#endregion
}
