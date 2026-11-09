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

namespace Zongsoft.Data.Common.Expressions;

public static class OperatorExtension
{
	/// <summary>计算运算符的优先级。</summary>
	/// <param name="operator">指定的运算符。</param>
	/// <returns>返回指定指定运算符的优先级值。</returns>
	public static byte GetPrecedence(this Operator @operator) => @operator switch
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

	/// <summary>获取指定<see cref="OperandType"/>操作元类型对应的<see cref="Operator"/>操作符。</summary>
	/// <param name="operandType">指定的操作元类型。</param>
	/// <param name="type">操作元的类型。</param>
	/// <returns>返回映射后的操作符。</returns>
	public static Operator GetOperator(this OperandType operandType, Type type) => operandType switch
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
		_ => throw new InvalidOperationException(string.Format(Properties.Resources.Operator_InvalidOperandConversion_Message, operandType)),
	};
}
