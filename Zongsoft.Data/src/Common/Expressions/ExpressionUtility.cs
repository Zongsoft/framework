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
using System.Reflection;

namespace Zongsoft.Data.Common.Expressions
{
	public static class ExpressionUtility
	{
		private static readonly MethodInfo GetValueMethod = typeof(Operand.ConstantOperand<>).GetMethod(nameof(Operand.ConstantOperand<object>.ToObject));

		public static IExpression Convert(this Operand operand, Func<string, Type> typeThunk, Func<string, IExpression> fieldThunk, Func<object, IExpression> valueThunk)
		{
			if(operand == null)
				return null;

			switch(operand)
			{
				case Operand.FieldOperand field:
					return fieldThunk(field.Name);
				case Operand.UnaryOperand unary:
					return new UnaryExpression(
						unary.Type.GetOperator(unary.Operand.GetOperandValueType(typeThunk)),
						unary.Operand.Convert(typeThunk, fieldThunk, valueThunk)
					);
				case Operand.BinaryOperand binary:
					return new BinaryExpression(
						binary.Type.GetOperator(binary.GetOperandValueType(typeThunk)),
						binary.Left.Convert(typeThunk, fieldThunk, valueThunk),
						binary.Right.Convert(typeThunk, fieldThunk, valueThunk)
					);
			}

			var type = operand.GetType();

			if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Operand.ConstantOperand<>))
				return valueThunk(GetValueMethod.MakeGenericMethod(type.GenericTypeArguments[0]).Invoke(operand, null));

			throw new DataException($"Unsupported {operand.GetType().FullName} operand type.");
		}

		public static Type GetOperandValueType(this Operand operand, Func<string, Type> typeThunk)
		{
			if(operand == null)
				return null;

			switch(operand)
			{
				case Operand.FieldOperand field:
					return typeThunk(field.Name);
				case Operand.UnaryOperand unary:
					return GetOperandValueType(unary.Operand, typeThunk);
				case Operand.BinaryOperand binary:
					return GetOperandValueType(binary, typeThunk);
			}

			var type = operand.GetType();

			if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Operand.ConstantOperand<>))
				return type.GenericTypeArguments[0];

			return null;
		}

		public static Type GetOperandValueType(this Operand.BinaryOperand binary, Func<string, Type> typeThunk)
		{
			if(binary == null)
				return null;

			var type1 = GetOperandValueType(binary.Left, typeThunk);
			var type2 = GetOperandValueType(binary.Right, typeThunk);

			if(type1 == typeof(string) || type2 == typeof(string))
				return typeof(string);

			return type1 ?? type2;
		}
	}
}
