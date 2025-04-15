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

namespace Zongsoft.Reflection.Expressions;

/// <summary>
/// 表示成员表达式元素的基类。
/// </summary>
public abstract class MemberExpression : IMemberExpression
{
	#region 构造函数
	protected MemberExpression() { }
	#endregion

	#region 公共属性
	public abstract MemberExpressionType ExpressionType { get; }
	public IMemberExpression Previous { get; private set; }
	public IMemberExpression Next { get; private set; }
	#endregion

	#region 公共方法
	public T Append<T>(T expression) where T : MemberExpression
	{
		this.Next = expression ?? throw new ArgumentNullException(nameof(expression));
		expression.Previous = this;
		return expression;
	}

	public T Prepend<T>(T expression) where T : MemberExpression
	{
		this.Previous = expression ?? throw new ArgumentNullException(nameof(expression));
		expression.Next = this;
		return expression;
	}
	#endregion

	#region 解析方法
	public static bool TryParse(ReadOnlySpan<char> text, out IMemberExpression expression) => (expression = MemberExpressionParser.Parse(text, null)) != null;
	public static IMemberExpression Parse(ReadOnlySpan<char> text) => MemberExpressionParser.Parse(text, message => throw new InvalidOperationException(message));
	public static IMemberExpression Parse(ReadOnlySpan<char> text, Action<string> onError) => MemberExpressionParser.Parse(text, onError);
	#endregion

	#region 静态方法
	public static bool IsNull(object value) => value == null || (value is ConstantExpression constant && constant.Value == null);
	public static ConstantExpression Constant(object value) => value == null ? ConstantExpression.Null : new ConstantExpression(value);
	public static ConstantExpression Constant(string literal, TypeCode type)
	{
		if(literal == null)
			return ConstantExpression.Null;

		return type switch
		{
			TypeCode.Boolean => new ConstantExpression(bool.Parse(literal)),
			TypeCode.Int16 => new ConstantExpression(short.Parse(literal)),
			TypeCode.Int32 => new ConstantExpression(int.Parse(literal)),
			TypeCode.Int64 => new ConstantExpression(long.Parse(literal)),
			TypeCode.UInt16 => new ConstantExpression(ushort.Parse(literal)),
			TypeCode.UInt32 => new ConstantExpression(uint.Parse(literal)),
			TypeCode.UInt64 => new ConstantExpression(ulong.Parse(literal)),
			TypeCode.Double => new ConstantExpression(double.Parse(literal)),
			TypeCode.Single => new ConstantExpression(float.Parse(literal)),
			TypeCode.Decimal => new ConstantExpression(decimal.Parse(literal)),
			TypeCode.Byte => new ConstantExpression(byte.Parse(literal)),
			TypeCode.Char => new ConstantExpression(string.IsNullOrEmpty(literal) ? '\0' : literal[0]),
			TypeCode.SByte => new ConstantExpression(sbyte.Parse(literal)),
			TypeCode.DateTime => new ConstantExpression(DateTime.Parse(literal)),
			TypeCode.Object => string.IsNullOrEmpty(literal) || literal.Equals("null", StringComparison.OrdinalIgnoreCase) ? ConstantExpression.Null : new ConstantExpression(literal),
			_ => new ConstantExpression(literal),
		};
	}

	public static MethodExpression Method(string name, params IMemberExpression[] arguments) => new(name, arguments);
	public static IndexerExpression Indexer(params IMemberExpression[] arguments) => new(arguments);
	public static IdentifierExpression Identifier(string name) => new(name);
	#endregion
}
