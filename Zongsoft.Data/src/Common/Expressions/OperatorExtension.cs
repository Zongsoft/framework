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
using System.ComponentModel;

namespace Zongsoft.Data.Common.Expressions
{
	public static class OperatorExtension
	{
		/// <summary>
		/// 计算运算符的优先级。
		/// </summary>
		/// <param name="operator">指定的运算符。</param>
		/// <returns>返回指定指定运算符的优先级值。</returns>
		public static byte GetPrecedence(this Operator @operator)
		{
			return @operator switch
			{
				Operator.Not => 22,
				Operator.Plus => 22,
				Operator.Negate => 22,

				Operator.And => 14,
				Operator.Xor => 13,
				Operator.Or => 12,
				Operator.AndAlso => 11,
				Operator.OrElse => 10,

				Operator.Multiply => 21,
				Operator.Divide => 21,
				Operator.Modulo => 21,
				Operator.Add => 20,
				Operator.Subtract => 20,
				_ => 0,
			};
		}

		/// <summary>
		/// 获取指定<see cref="OperandType"/>操作元类型对应的<see cref="Operator"/>操作符。
		/// </summary>
		/// <param name="operandType">指定的操作元类型。</param>
		/// <param name="type">操作元的类型。</param>
		/// <returns>返回映射后的操作符。</returns>
		public static Operator GetOperator(this OperandType operandType, Type type)
		{
			return operandType switch
			{
				OperandType.Negate => Operator.Negate,
				OperandType.Not => Operator.Not,
				OperandType.Add => Operator.Add,
				OperandType.Subtract => Operator.Subtract,
				OperandType.Multiply => Operator.Multiply,
				OperandType.Divide => Operator.Divide,
				OperandType.Modulo => Operator.Modulo,
				OperandType.And => type == typeof(bool) ? Operator.AndAlso : Operator.And,
				OperandType.Or => type == typeof(bool) ? Operator.OrElse : Operator.Or,
				OperandType.Xor => Operator.Xor,
				_ => throw new InvalidOperationException($"Cannot convert the {operandType} operand to operator."),
			};
		}
	}
}
