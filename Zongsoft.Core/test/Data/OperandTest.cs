using System;
using System.Collections.Generic;

using Xunit;

namespace Zongsoft.Data.Tests
{
	public class OperandTest
	{
		[Fact]
		public void Test1()
		{
			var a = Operand.Constant(1);
			var b = (Operand)2;
			var c = (Operand)3;
			var d = (Operand)4;
			var e = (Operand)5;

			var expression = (a + b) * (c - d) / e;

			Assert.IsType<Operand.BinaryOperand>(expression);
			Assert.Equal(OperandType.Divide, expression.Type);

			var binary = (Operand.BinaryOperand)expression;
			Assert.Equal(OperandType.Constant, binary.Right.Type);
			Assert.Equal(5, ((Operand.ConstantOperand<int>)binary.Right).Value);

			Assert.IsType<Operand.BinaryOperand>(binary.Left);
			binary = (Operand.BinaryOperand)binary.Left;
			Assert.Equal(OperandType.Multiply, binary.Type);

			Assert.IsType<Operand.BinaryOperand>(binary.Left);
			var addition = (Operand.BinaryOperand)binary.Left;
			Assert.Equal(OperandType.Add, addition.Type);
			Assert.Equal(OperandType.Constant, addition.Left.Type);
			Assert.Equal(OperandType.Constant, addition.Right.Type);
			Assert.Equal(1, ((Operand.ConstantOperand<int>)addition.Left).Value);
			Assert.Equal(2, ((Operand.ConstantOperand<int>)addition.Right).Value);

			Assert.IsType<Operand.BinaryOperand>(binary.Right);
			var subtraction = (Operand.BinaryOperand)binary.Right;
			Assert.Equal(OperandType.Subtract, subtraction.Type);
			Assert.Equal(OperandType.Constant, subtraction.Left.Type);
			Assert.Equal(OperandType.Constant, subtraction.Right.Type);
			Assert.Equal(3, ((Operand.ConstantOperand<int>)subtraction.Left).Value);
			Assert.Equal(4, ((Operand.ConstantOperand<int>)subtraction.Right).Value);
		}

		[Fact]
		public void Test2()
		{
			/* 提示：算术运算符的不同优先级 */

			var a = Operand.Constant(1);
			var b = (Operand)2;
			var c = (Operand)3;
			var d = (Operand)4;
			var e = (Operand)5;

			var expression = a + b * c - d / e;

			Assert.IsType<Operand.BinaryOperand>(expression);
			Assert.Equal(OperandType.Subtract, expression.Type);
			var binary = (Operand.BinaryOperand)expression;

			Assert.IsType<Operand.BinaryOperand>(binary.Left);
			var addition = (Operand.BinaryOperand)binary.Left;
			Assert.Equal(OperandType.Constant, addition.Left.Type);
			Assert.Equal(1, ((Operand.ConstantOperand<int>)addition.Left).Value);

			Assert.IsType<Operand.BinaryOperand>(addition.Right);
			var multiply = (Operand.BinaryOperand)addition.Right;
			Assert.Equal(OperandType.Multiply, multiply.Type);
			Assert.Equal(OperandType.Constant, multiply.Left.Type);
			Assert.Equal(OperandType.Constant, multiply.Right.Type);
			Assert.Equal(2, ((Operand.ConstantOperand<int>)multiply.Left).Value);
			Assert.Equal(3, ((Operand.ConstantOperand<int>)multiply.Right).Value);

			Assert.IsType<Operand.BinaryOperand>(binary.Right);
			var divide = (Operand.BinaryOperand)binary.Right;
			Assert.Equal(OperandType.Divide, divide.Type);
			Assert.Equal(OperandType.Constant, divide.Left.Type);
			Assert.Equal(OperandType.Constant, divide.Right.Type);
			Assert.Equal(4, ((Operand.ConstantOperand<int>)divide.Left).Value);
			Assert.Equal(5, ((Operand.ConstantOperand<int>)divide.Right).Value);
		}
	}
}
