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
using System.ComponentModel;

namespace Zongsoft.Data;

/// <summary>表示数据排序的设置项。</summary>
[TypeConverter(typeof(SortingConverter))]
public readonly struct Sorting : IEquatable<Sorting>, ISpanParsable<Sorting>
{
	#region 成员字段
	private readonly SortingMode _mode;
	private readonly string _name;
	#endregion

	#region 构造函数
	public Sorting(string name, SortingMode mode = SortingMode.Ascending)
	{
		if(string.IsNullOrWhiteSpace(name))
			throw new ArgumentNullException(nameof(name));

		_name = name;
		_mode = mode;
	}
	#endregion

	#region 公共属性
	/// <summary>获取一个值，指示排序方式。</summary>
	public SortingMode Mode => _mode;

	/// <summary>获取排序项名称（即排序的成员名）。</summary>
	public string Name => _name;
	#endregion

	#region 静态方法
	/// <summary>创建一个正序的排序设置项。</summary>
	/// <param name="name">指定的排序项名称。</param>
	/// <returns>返回创建成果的排序设置项实例。</returns>
	public static Sorting Ascending(string name) => new(name, SortingMode.Ascending);

	/// <summary>创建一个倒序的排序设置项。</summary>
	/// <param name="name">指定的排序项名称。</param>
	/// <returns>返回创建成果的排序设置项实例。</returns>
	public static Sorting Descending(string name) => new(name, SortingMode.Descending);
	#endregion

	#region 解析方法
	/// <summary>将排序设置规则的字符串表示形式解析为其等效的<see cref="Sorting"/>。</summary>
	/// <param name="text">待解析的排序设置规则文本。</param>
	/// <returns>返回解析成功的<see cref="Sorting"/>排序设置。</returns>
	/// <exception cref="ArgumentException">当指定的<paramref name="text"/>参数值不符合排序设置格式。</exception>
	/// <exception cref="ArgumentNullException">当指定的<paramref name="text"/>参数值为空(<c>null</c>)或空字符串。</exception>
	public static Sorting Parse(string text) => Parse(text.AsSpan());
	public static Sorting Parse(ReadOnlySpan<char> text)
	{
		if(text.IsEmpty)
			throw new ArgumentNullException(nameof(text));

		if(TryParse(text, out var result))
			return result;

		throw new ArgumentException(Properties.Resources.Sorting_InvalidFormat_Message);
	}

	/// <summary>尝试将排序设置规则的字符串表示形式解析为其等效的<see cref="Sorting"/>。</summary>
	/// <param name="text">待解析的排序设置规则文本。</param>
	/// <param name="value">输出参数，表示解析成功的<see cref="Sorting"/>排序设置。</param>
	/// <returns>如果解析成功则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
	public static bool TryParse(string text, out Sorting value) => TryParse(text.AsSpan(), out value);
	public static bool TryParse(ReadOnlySpan<char> text, out Sorting value)
	{
		const int NONE_STATE = 0;       //未开始
		const int GAPS_STATE = 1;       //符号与名字的间隔
		const int NAME_STATE = 2;       //名字区
		const int WHITESPACE_STATE = 3; //空白字符(仅限名字中间或尾部)

		value = default;

		if(text.IsEmpty)
			return false;

		var mode = SortingMode.Ascending;
		var state = NONE_STATE;
		int index = 0, length = 0;

		for(int i = 0; i < text.Length; i++)
		{
			if(text[i] is ' ' or '\t')
			{
				if(state == NAME_STATE)
					state = WHITESPACE_STATE;

				continue;
			}

			if(text[i] == '+')
			{
				state = GAPS_STATE;
				continue;
			}

			if(text[i] is '-' or '~')
			{
				mode = SortingMode.Descending;
				state = GAPS_STATE;
				continue;
			}

			if(state == WHITESPACE_STATE)
				return false;

			if(state is NONE_STATE or GAPS_STATE)
				index = i;

			length++;
			state = NAME_STATE;
		}

		if(length == 0 || index > text.Length - length || text.Slice(index, length).IsWhiteSpace())
			return false;

		value = new Sorting(text.Slice(index, length).ToString(), mode);
		return true;
	}

	static Sorting IParsable<Sorting>.Parse(string text, IFormatProvider provider) => Parse(text);
	static bool IParsable<Sorting>.TryParse(string text, IFormatProvider provider, out Sorting result) => TryParse(text, out result);
	static Sorting ISpanParsable<Sorting>.Parse(ReadOnlySpan<char> text, IFormatProvider provider) => Parse(text);
	static bool ISpanParsable<Sorting>.TryParse(ReadOnlySpan<char> text, IFormatProvider provider, out Sorting result) => TryParse(text, out result);
	#endregion

	#region 重写方法
	public bool Equals(Sorting other) => _mode == other._mode && string.Equals(_name, other._name, StringComparison.OrdinalIgnoreCase);
	public override bool Equals(object obj) => obj is Sorting other && this.Equals(other);
	public override int GetHashCode() => HashCode.Combine(_mode, _name.ToUpperInvariant());
	public override string ToString() => _mode == SortingMode.Descending ? "-" + _name : _name;
	#endregion

	#region 符号重载
	public static bool operator ==(Sorting left, Sorting right) => left.Equals(right);
	public static bool operator !=(Sorting left, Sorting right) => !(left == right);

	public static Sorting[] operator +(Sorting[] sortings, Sorting value)
	{
		if(sortings == null || sortings.Length == 0)
			return [value];

		foreach(var sorting in sortings)
		{
			if(string.Equals(sorting._name, value._name, StringComparison.OrdinalIgnoreCase))
				return sortings;
		}

		var result = new Sorting[sortings.Length + 1];
		Array.Copy(sortings, 0, result, 0, sortings.Length);
		result[^1] = value;

		return result;
	}

	public static Sorting[] operator +(Sorting a, Sorting b)
	{
		if(string.Equals(a._name, b._name, StringComparison.OrdinalIgnoreCase))
			return [b];
		else
			return [a, b];
	}
	#endregion
}

/// <summary>表示排序方式的枚举。</summary>
public enum SortingMode
{
	/// <summary>正序</summary>
	Ascending,
	/// <summary>倒序</summary>
	Descending,
}
