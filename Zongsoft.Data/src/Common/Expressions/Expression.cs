/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
 *
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Data library.
 *
 * The Zongsoft.Data is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;

namespace Zongsoft.Data.Common.Expressions
{
	/// <summary>
	/// 表达式的基类。
	/// </summary>
	public abstract class Expression : IExpression
	{
		#region 构造函数
		protected Expression()
		{
		}
		#endregion

		#region 静态方法
		/// <summary>
		/// 判断指定的表达式是否为空(null)或空常量表达式（即值为空的常量表达式）。
		/// </summary>
		/// <param name="expression">指定要判断的表达式。</param>
		/// <returns>如果指定的表达式为空(null)或是一个空常量表达式则返回真(True)，否则返回假(False)。</returns>
		public static bool IsNull(IExpression expression)
		{
			return expression == null || (expression is ConstantExpression constant && constant.Value == null);
		}

		/// <summary>
		/// 创建一个块级表达式。
		/// </summary>
		/// <param name="delimiter">指定的块级表达式元素分割符。</param>
		/// <returns>返回新建的块级表达式对象。</returns>
		public static BlockExpression Block(BlockExpressionDelimiter delimiter = BlockExpressionDelimiter.None)
		{
			return new BlockExpression(delimiter);
		}

		/// <summary>
		/// 创建一个字面量表达式。
		/// </summary>
		/// <param name="text">指定的字面量文本。</param>
		/// <returns>返回新建的字面量表达式对象。</returns>
		public static LiteralExpression Literal(string text)
		{
			return new LiteralExpression(text);
		}

		/// <summary>
		/// 创建一个注释表达式。
		/// </summary>
		/// <param name="text">指定的注释文本。</param>
		/// <returns>返回新建的注释表达式对象。</returns>
		public static CommentExpression Comment(string text)
		{
			return new CommentExpression(text);
		}

		/// <summary>
		/// 创建一个常量表达式。
		/// </summary>
		/// <param name="value">指定的常量值。</param>
		/// <returns>返回新建的常量表达式对象。</returns>
		public static ConstantExpression Constant(object value)
		{
			if(value == null)
				return ConstantExpression.Null;

			return new ConstantExpression(value);
		}

		/// <summary>
		/// 创建一个变量标识表达式。
		/// </summary>
		/// <param name="name">指定的变量名。</param>
		/// <param name="isGlobal">指定一个值，指示该变量是否为全局变量。</param>
		/// <returns>返回新建的变量标识表达式。</returns>
		public static VariableIdentifier Variable(string name, bool isGlobal = false)
		{
			return new VariableIdentifier(name, isGlobal);
		}

		/// <summary>
		/// 创建一个参数表达式。
		/// </summary>
		/// <param name="name">指定的参数名，如果为问号，则表示该参数将由所属参数集自动命名。</param>
		/// <param name="type">指定的参数的数据类型。</param>
		/// <param name="direction">指定的参数方向，默认为输入参数。</param>
		/// <returns></returns>
		public static ParameterExpression Parameter(string name, System.Data.DbType type, System.Data.ParameterDirection direction = System.Data.ParameterDirection.Input)
		{
			return new ParameterExpression(name, type, direction);
		}

		/// <summary>
		/// 创建一个参数表达式。
		/// </summary>
		/// <param name="name">指定的参数名，如果为问号，则表示该参数将由所属参数集自动命名。</param>
		/// <param name="type">指定的参数的数据类型。</param>
		/// <param name="value">指定的参数值。</param>
		/// <returns></returns>
		public static ParameterExpression Parameter(string name, System.Data.DbType type, object value)
		{
			return new ParameterExpression(name, type, value);
		}

		/// <summary>
		/// 创建一个参数表达式。
		/// </summary>
		/// <param name="type">指定的参数的数据类型。</param>
		/// <param name="value">指定的参数值。</param>
		/// <returns></returns>
		public static ParameterExpression Parameter(System.Data.DbType type, object value)
		{
			return new ParameterExpression(ParameterExpression.Anonymous, type, value);
		}

		/// <summary>
		/// 创建一个参数表达式。
		/// </summary>
		/// <param name="value">指定的参数值。</param>
		/// <returns></returns>
		public static ParameterExpression Parameter(object value)
		{
			if(value == null)
				throw new ArgumentNullException(nameof(value));

			return new ParameterExpression(ParameterExpression.Anonymous, Utility.GetDbType(value), value);
		}

		/// <summary>
		/// 创建一个参数表达式，参数名将由所属参数集合自动命名。
		/// </summary>
		/// <param name="field">指定参数关联的字段标识。</param>
		/// <param name="value">指定的参数值。</param>
		/// <returns>返回新建的参数表达式。</returns>
		public static ParameterExpression Parameter(FieldIdentifier field, object value)
		{
			return new ParameterExpression(field, value);
		}

		/// <summary>
		/// 创建一个参数表达式，参数名将由所属参数集合自动命名。
		/// </summary>
		/// <param name="field">指定参数关联的字段标识。</param>
		/// <param name="schema">指定的参数对应的模式。</param>
		/// <returns>返回新建的参数表达式。</returns>
		public static ParameterExpression Parameter(FieldIdentifier field, SchemaMember schema)
		{
			return new ParameterExpression(field, schema);
		}

		/// <summary>
		/// 创建一个参数表达式，参数名将由所属参数集合自动命名。
		/// </summary>
		/// <param name="field">指定参数关联的字段标识。</param>
		/// <param name="schema">指定的参数对应的模式。</param>
		/// <param name="value">指定的参数值。</param>
		/// <returns>返回新建的参数表达式。</returns>
		public static ParameterExpression Parameter(FieldIdentifier field, SchemaMember schema, object value)
		{
			return new ParameterExpression(field, schema, value);
		}

		/// <summary>
		/// 创建一个表示算术求反运算的单目表达式。
		/// </summary>
		/// <param name="operand">指定的要求反运算的算术表达式。</param>
		/// <returns>返回的求反后的算术表达式。</returns>
		public static UnaryExpression Negate(IExpression operand)
		{
			return new UnaryExpression(Operator.Negate, operand);
		}

		/// <summary>
		/// 创建一个表示逻辑求非运算的单目表达式。
		/// </summary>
		/// <param name="operand">指定的要求非运算的逻辑表达式。</param>
		/// <returns>返回的求非后的逻辑表达式。</returns>
		public static UnaryExpression Not(IExpression operand)
		{
			return new UnaryExpression(Operator.Not, operand);
		}

		/// <summary>
		/// 创建一个赋值的二元表达式。
		/// </summary>
		/// <param name="left">指定的赋值表达式的左路部分，即赋值目标。</param>
		/// <param name="right">指定的赋值表达式的右路部分，即取值来源。</param>
		/// <returns>返回的赋值二元表达式。</returns>
		public static BinaryExpression Assign(IExpression left, IExpression right)
		{
			return new BinaryExpression(Operator.Assign, left, right);
		}

		public static BinaryExpression Add(IExpression left, IExpression right)
		{
			return new BinaryExpression(Operator.Add, left, right);
		}

		public static BinaryExpression Subtract(IExpression left, IExpression right)
		{
			return new BinaryExpression(Operator.Subtract, left, right);
		}

		public static BinaryExpression Multiply(IExpression left, IExpression right)
		{
			return new BinaryExpression(Operator.Multiply, left, right);
		}

		public static BinaryExpression Divide(IExpression left, IExpression right)
		{
			return new BinaryExpression(Operator.Divide, left, right);
		}

		public static BinaryExpression Modulo(IExpression left, IExpression right)
		{
			return new BinaryExpression(Operator.Modulo, left, right);
		}

		public static BinaryExpression And(IExpression left, IExpression right)
		{
			return new BinaryExpression(Operator.And, left, right);
		}

		public static BinaryExpression AndAlso(IExpression left, IExpression right)
		{
			return new BinaryExpression(Operator.AndAlso, left, right);
		}

		public static BinaryExpression Or(IExpression left, IExpression right)
		{
			return new BinaryExpression(Operator.Or, left, right);
		}

		public static BinaryExpression OrElse(IExpression left, IExpression right)
		{
			return new BinaryExpression(Operator.OrElse, left, right);
		}

		public static BinaryExpression Xor(IExpression left, IExpression right)
		{
			return new BinaryExpression(Operator.Xor, left, right);
		}

		public static UnaryExpression Exists(IExpression operand)
		{
			return new UnaryExpression(Operator.Exists, operand);
		}

		public static UnaryExpression NotExists(IExpression operand)
		{
			return new UnaryExpression(Operator.NotExists, operand);
		}

		public static BinaryExpression In(IExpression left, IExpression right)
		{
			return new BinaryExpression(Operator.In, left, right);
		}

		public static BinaryExpression NotIn(IExpression left, IExpression right)
		{
			return new BinaryExpression(Operator.NotIn, left, right);
		}

		public static BinaryExpression Like(IExpression left, IExpression right)
		{
			return new BinaryExpression(Operator.Like, left, right ?? ConstantExpression.Null);
		}

		public static BinaryExpression Between(IExpression operand, RangeExpression range)
		{
			return new BinaryExpression(Operator.Between, operand, range);
		}

		public static BinaryExpression Between(IExpression operand, IExpression minimum, IExpression maximum)
		{
			return new BinaryExpression(Operator.Between, operand, new RangeExpression(minimum, maximum));
		}

		public static BinaryExpression Equal(IExpression left, IExpression right)
		{
			return new BinaryExpression(Operator.Equal, left, right ?? ConstantExpression.Null);
		}

		public static BinaryExpression NotEqual(IExpression left, IExpression right)
		{
			return new BinaryExpression(Operator.NotEqual, left, right ?? ConstantExpression.Null);
		}

		public static BinaryExpression LessThan(IExpression left, IExpression right)
		{
			return new BinaryExpression(Operator.LessThan, left, right);
		}

		public static BinaryExpression LessThanOrEqual(IExpression left, IExpression right)
		{
			return new BinaryExpression(Operator.LessThanOrEqual, left, right);
		}

		public static BinaryExpression GreaterThan(IExpression left, IExpression right)
		{
			return new BinaryExpression(Operator.GreaterThan, left, right);
		}

		public static BinaryExpression GreaterThanOrEqual(IExpression left, IExpression right)
		{
			return new BinaryExpression(Operator.GreaterThanOrEqual, left, right);
		}
		#endregion

		#region 访问方法
		string IExpression.Accept(IExpressionVisitor visitor)
		{
			return this.Accept(visitor);
		}

		internal protected virtual string Accept(IExpressionVisitor visitor)
		{
			return visitor.Visit(this);
		}
		#endregion
	}
}
