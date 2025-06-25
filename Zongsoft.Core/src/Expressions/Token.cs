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

namespace Zongsoft.Expressions;

/// <summary>
/// 表示词素的类。
/// </summary>
public class Token : IEquatable<Token>
{
	#region 静态字段
	/// <summary>表示空(null)的词素。</summary>
	public static readonly Token Null = new(TokenType.Constant, null);

	/// <summary>表示逻辑真(true)的词素。</summary>
	public static readonly Token True = new(TokenType.Constant, true);

	/// <summary>表示逻辑假(false)的词素。</summary>
	public static readonly Token False = new(TokenType.Constant, false);
	#endregion

	#region 成员字段
	private readonly TokenType _type;
	private readonly object _value;
	#endregion

	#region 构造函数
	public Token(TokenType type, object value)
	{
		_type = type;
		_value = value;
	}
	#endregion

	#region 公共属性
	/// <summary>获取词素的类型。</summary>
	public TokenType Type => _type;
	/// <summary>获取词素的值。</summary>
	public object Value => _value;
	#endregion

	#region 重写方法
	public bool Equals(Token other) => other.Type == _type && other.Value == _value;
	public override bool Equals(object obj) => obj is Token other && this.Equals(other);
	public override int GetHashCode() => _value == null ? _type.GetHashCode() : HashCode.Combine(_type, _value);
	public override string ToString() => $"[{_type}]{_value}";
	#endregion
}
