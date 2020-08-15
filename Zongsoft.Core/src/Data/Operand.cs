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

namespace Zongsoft.Data
{
	public abstract class Operand
	{
		#region 构造函数
		protected Operand(OperandType type)
		{
			this.Type = type;
		}
		#endregion

		#region 公共属性
		public OperandType Type { get; }
		#endregion

		#region 静态方法
		public static FieldOperand Field(string name) => new FieldOperand(name);
		public static ConstantOperand<T> Constant<T>(T value) => new ConstantOperand<T>(value);

		public static string GetSymbol(OperandType type)
		{
			return type switch
			{
				OperandType.Add => "+",
				OperandType.Subtract => "-",
				OperandType.Multiply => "*",
				OperandType.Divide => "/",
				OperandType.Modulo => "%",
				OperandType.And => "&",
				OperandType.Or => "|",
				OperandType.Xor => "^",
				OperandType.Not => "!",
				OperandType.Negate => "-",
				_ => string.Empty,
			};
		}
		#endregion

		#region 符号重写
		public static Operand operator !(Operand operand) => new UnaryOperand(OperandType.Not, operand);
		public static Operand operator ~(Operand operand) => new UnaryOperand(OperandType.Not, operand);
		public static Operand operator -(Operand operand) => new UnaryOperand(OperandType.Negate, operand);

		public static Operand operator +(Operand a, Operand b) => new BinaryOperand(OperandType.Add, a, b);
		public static Operand operator -(Operand a, Operand b) => new BinaryOperand(OperandType.Subtract, a, b);
		public static Operand operator *(Operand a, Operand b) => new BinaryOperand(OperandType.Multiply, a, b);
		public static Operand operator /(Operand a, Operand b) => new BinaryOperand(OperandType.Divide, a, b);
		public static Operand operator %(Operand a, Operand b) => new BinaryOperand(OperandType.Modulo, a, b);
		public static Operand operator &(Operand a, Operand b) => new BinaryOperand(OperandType.And, a, b);
		public static Operand operator |(Operand a, Operand b) => new BinaryOperand(OperandType.Or, a, b);
		public static Operand operator ^(Operand a, Operand b) => new BinaryOperand(OperandType.Xor, a, b);
		#endregion

		#region 类型转化
		public static implicit operator Operand(byte value) => Constant(value);
		public static implicit operator Operand(sbyte value) => Constant(value);
		public static implicit operator Operand(short value) => Constant(value);
		public static implicit operator Operand(ushort value) => Constant(value);
		public static implicit operator Operand(int value) => Constant(value);
		public static implicit operator Operand(uint value) => Constant(value);
		public static implicit operator Operand(long value) => Constant(value);
		public static implicit operator Operand(ulong value) => Constant(value);
		public static implicit operator Operand(float value) => Constant(value);
		public static implicit operator Operand(double value) => Constant(value);
		public static implicit operator Operand(decimal value) => Constant(value);
		public static implicit operator Operand(bool value) => Constant(value);
		public static implicit operator Operand(char value) => Constant(value);
		public static implicit operator Operand(string value) => Constant(value);
		public static implicit operator Operand(DateTime value) => Constant(value);
		public static implicit operator Operand(DateTimeOffset value) => Constant(value);
		public static implicit operator Operand(Guid value) => Constant(value);
		#endregion

		#region 嵌套子类
		public class BinaryOperand : Operand
		{
			#region 构造函数
			public BinaryOperand(OperandType type, Operand left, Operand right) : base(type)
			{
				this.Left = left;
				this.Right = right;
			}
			#endregion

			#region 公共属性
			public Operand Left { get; }
			public Operand Right { get; }
			#endregion

			#region 重写方法
			public override string ToString()
			{
				return $"{this.Left} {GetSymbol(this.Type)} {this.Right}";
			}
			#endregion
		}

		public class UnaryOperand : Operand
		{
			#region 构造函数
			public UnaryOperand(OperandType type, Operand operand) : base(type)
			{
				this.Operand = operand;
			}
			#endregion

			#region 公共属性
			public Operand Operand { get; }
			#endregion

			#region 重写方法
			public override string ToString()
			{
				return $"{GetSymbol(this.Type)}{this.Operand}";
			}
			#endregion
		}

		public class ConstantOperand<T> : Operand
		{
			#region 构造函数
			public ConstantOperand(T value) : base(OperandType.Constant)
			{
				this.Value = value;
			}
			#endregion

			#region 公共属性
			public T Value { get; }
			#endregion

			#region 重写方法
			public override string ToString()
			{
				return this.Value == null ? "<NULL>" : this.Value.ToString();
			}
			#endregion
		}

		public class FieldOperand : Operand
		{
			#region 构造函数
			public FieldOperand(string name) : base(OperandType.Field)
			{
				if(string.IsNullOrWhiteSpace(name))
					throw new ArgumentNullException(nameof(name));

				this.Name = name.Trim();
			}
			#endregion

			#region 公共属性
			public string Name { get; }
			#endregion

			#region 重写方法
			public override string ToString()
			{
				return this.Name;
			}
			#endregion
		}
		#endregion
	}
}
