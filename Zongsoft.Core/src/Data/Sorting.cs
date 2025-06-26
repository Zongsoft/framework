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
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
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

namespace Zongsoft.Data;

/// <summary>
/// 表示数据排序的设置项。
/// </summary>
[TypeConverter(typeof(SortingConverter))]
public readonly struct Sorting : IEquatable<Sorting>
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

	/// <summary>将排序设置规则的字符串表示形式解析为其等效的<see cref="Sorting"/>。</summary>
	/// <param name="text">待解析的排序设置规则文本。</param>
	/// <returns>返回解析成功的<see cref="Sorting"/>排序设置。</returns>
	/// <exception cref="ArgumentException">当指定的<paramref name="text"/>参数值不符合排序设置格式。</exception>
	/// <exception cref="ArgumentNullException">当指定的<paramref name="text"/>参数值为空(<c>null</c>)或空字符串。</exception>
	public static Sorting Parse(string text)
	{
		if(string.IsNullOrEmpty(text))
			throw new ArgumentNullException(nameof(text));

		if(TryParse(text, out var result))
			return result;

		throw new ArgumentException("Invalid sorting format.");
	}

	/// <summary>尝试将排序设置规则的字符串表示形式解析为其等效的<see cref="Sorting"/>。</summary>
	/// <param name="text">待解析的排序设置规则文本。</param>
	/// <param name="value">输出参数，表示解析成功的<see cref="Sorting"/>排序设置。</param>
	/// <returns>如果解析成功则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
	public static bool TryParse(string text, out Sorting value)
	{
		const int NONE_STATE = 0;       //未开始
		const int GAPS_STATE = 1;       //符号与名字的间隔
		const int NAME_STATE = 2;       //名字区
		const int WHITESPACE_STATE = 3; //空白字符(仅限名字中间或尾部)

		value = default;

		if(string.IsNullOrEmpty(text))
			return false;

		var span = text.AsSpan();
		var mode = SortingMode.Ascending;
		int state = NONE_STATE;
		int index = 0, length = 0;

		for(int i = 0; i < span.Length; i++)
		{
			if(span[i] == ' ' || span[i] == '\t')
			{
				if(state == NAME_STATE)
					state = WHITESPACE_STATE;

				continue;
			}

			if(span[i] == '+')
			{
				state = GAPS_STATE;
				continue;
			}

			if(span[i] == '-' || span[i] == '~')
			{
				mode = SortingMode.Descending;
				state = GAPS_STATE;
				continue;
			}

			if(state == WHITESPACE_STATE)
				return false;

			if(state == NONE_STATE || state == GAPS_STATE)
				index = i;

			length++;
			state = NAME_STATE;
		}

		value = new Sorting(span.Slice(index, length).ToString(), mode);
		return true;
	}
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

/// <summary>
/// 表示排序方式的枚举。
/// </summary>
public enum SortingMode
{
	/// <summary>正序</summary>
	Ascending,
	/// <summary>倒序</summary>
	Descending,
}
