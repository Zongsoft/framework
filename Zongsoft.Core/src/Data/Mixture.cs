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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;
using System.Collections.Generic;

namespace Zongsoft.Data;

/// <summary>表示单值、多值选择或范围。</summary>
/// <typeparam name="T">选择值类型。范围操作要求支持默认顺序比较，相等性比较使用默认值比较器。</typeparam>
public readonly struct Mixture<T> : IEquatable<Mixture<T>>, ISpanParsable<Mixture<T>> where T : struct
{
	#region 构造函数
	public Mixture(T value)
	{
		this.Array = null;
		this.Range = new Range<T>(value);
	}

	public Mixture(T[] array)
	{
		this.Array = array;
		this.Range = default;
	}

	public Mixture(Range<T> range)
	{
		this.Array = null;
		this.Range = range;
	}
	#endregion

	#region 公共属性
	public T Value => this.Range.Minimum ?? default;
	public readonly T[] Array;
	public readonly Range<T> Range;

	public bool HasValue => (this.Array != null && this.Array.Length > 0) || this.Range.HasValue;
	public bool IsEmpty => (this.Array == null || this.Array.Length == 0) && this.Range.IsEmpty;
	#endregion

	#region 重写方法
	public bool Equals(Mixture<T> other)
	{
		if(this.Array != null && this.Array.Length > 0)
		{
			if(other.Array == null || other.Array.Length != this.Array.Length)
				return false;

			for(int i = 0; i < this.Array.Length; i++)
			{
				if(!EqualityComparer<T>.Default.Equals(this.Array[i], other.Array[i]))
					return false;
			}

			return true;
		}

		return (other.Array == null || other.Array.Length == 0) && this.Range.Equals(other.Range);
	}
	public override bool Equals(object obj) => obj is Mixture<T> other && this.Equals(other);

	public override int GetHashCode()
	{
		if(this.Array != null && this.Array.Length > 0)
		{
			var hashcode = new HashCode();

			for(int i = 0; i < this.Array.Length; i++)
				hashcode.Add(this.Array[i]);

			return hashcode.ToHashCode();
		}

		return this.Range.GetHashCode();
	}

	public override string ToString() => this.Array != null && this.Array.Length > 0 ?
		string.Join(',', this.Array) :
		this.Range.ToString();
	#endregion

	#region 解析方法
	public static Mixture<T> Parse(string text) => text == null ? throw new ArgumentNullException(nameof(text)) : Parse(text.AsSpan());
	public static Mixture<T> Parse(ReadOnlySpan<char> text) => TryParse(text, out var result) ? result : throw new FormatException(Properties.Resources.InvalidFormat_Message);
	public static bool TryParse(string text, out Mixture<T> result) => TryParse(text.AsSpan(), out result);
	public static bool TryParse(ReadOnlySpan<char> text, out Mixture<T> result)
	{
		result = default;
		var depth = 0;
		var multiple = false;

		foreach(var character in text)
		{
			switch(character)
			{
				case '(':
					depth++;
					break;
				case ')':
					if(--depth < 0)
						return false;
					break;
				case ',':
					multiple |= depth == 0;
					break;
			}
		}

		if(depth != 0)
			return false;

		if(!multiple)
		{
			if(Range<T>.TryParse(text, out var range))
				result = new Mixture<T>(range);

			return result.HasValue;
		}

		var values = new List<T>();

		foreach(var part in text.Split(','))
		{
			var token = text[part].Trim();

			if(token.IsEmpty)
				continue;

			if(!Common.Convert.TryConvertValue(token.ToString(), out T value))
				return false;

			values.Add(value);
		}

		if(values.Count == 0)
			return false;

		result = new Mixture<T>([.. values]);
		return result.HasValue;
	}

	static Mixture<T> IParsable<Mixture<T>>.Parse(string text, IFormatProvider provider) => Parse(text);
	static bool IParsable<Mixture<T>>.TryParse(string text, IFormatProvider provider, out Mixture<T> result) => TryParse(text, out result);
	static Mixture<T> ISpanParsable<Mixture<T>>.Parse(ReadOnlySpan<char> text, IFormatProvider provider) => Parse(text);
	static bool ISpanParsable<Mixture<T>>.TryParse(ReadOnlySpan<char> text, IFormatProvider provider, out Mixture<T> result) => TryParse(text, out result);
	#endregion

	#region 符号重写
	public static bool operator ==(Mixture<T> left, Mixture<T> right) => left.Equals(right);
	public static bool operator !=(Mixture<T> left, Mixture<T> right) => !(left == right);
	#endregion
}

public static class MixtureUtility
{
	#region 静态方法
	public static Condition ToCondition<T>(this Mixture<T> mixture, string name) where T : struct
	{
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		if(mixture.Array != null && mixture.Array.Length > 0)
			return Condition.In(name, mixture.Array);

		if(mixture.Range.HasValue)
			return Condition.Between(name, mixture.Range);

		return null;
	}
	#endregion
}

public class MixtureConverter<T> : IConditionConverter where T : struct
{
	#region 公共方法
	public ICondition Convert(ConditionConverterContext context) => context.Value is Mixture<T> mixture ? mixture.ToCondition(context.GetFullName()) : null;
	#endregion
}
