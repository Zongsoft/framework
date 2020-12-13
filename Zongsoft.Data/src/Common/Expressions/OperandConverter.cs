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
using System.Linq;

using Zongsoft.Data.Metadata;

namespace Zongsoft.Data.Common.Expressions
{
	public abstract class OperandConverter<TContext> where TContext : IDataAccessContext
	{
		#region 构造函数
		protected OperandConverter() { }
		#endregion

		#region 公共方法
		public IExpression Convert(TContext context, IStatementBase statement, Operand operand)
		{
			if(operand == null)
				return null;

			Type type;

			switch(operand)
			{
				case Operand.FieldOperand field:
					return this.GetField(context, statement, field.Name);
				case Operand.UnaryOperand unary:
					return new UnaryExpression(
						unary.Type.GetOperator(this.GetOperandValueType(context, unary.Operand)),
						this.Convert(context, statement, unary.Operand)
					);
				case Operand.BinaryOperand binary:
					type = this.GetOperandValueType(context, binary);

					return type == typeof(string) ?
						(IExpression)MethodExpression.Function("concat",
							this.Convert(context, statement, binary.Left),
							this.Convert(context, statement, binary.Right)) :
						new BinaryExpression(
							binary.Type.GetOperator(type),
							this.Convert(context, statement, binary.Left),
							this.Convert(context, statement, binary.Right));
				case Operand.FunctionOperand function:
					if(function.Arguments == null || function.Arguments.Length == 0)
						return MethodExpression.Function(function.Name);
					else
						return MethodExpression.Function(function.Name, function.Arguments.Select(arg => this.Convert(context, statement, arg)).ToArray());
				case Operand.AggregateOperand aggregate:
					return this.GenerateAggregate(context, statement, aggregate);
			}

			type = operand.GetType();

			if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Operand.ConstantOperand<>))
				return this.GetValue(context, statement, operand.GetType().GetMethod("ToObject").Invoke(operand, null));

			throw new DataException($"Unsupported {operand.GetType().FullName} operand type.");
		}
		#endregion

		#region 私有方法
		private Type GetOperandValueType(TContext context, Operand operand)
		{
			if(operand == null)
				return null;

			switch(operand)
			{
				case Operand.FieldOperand field:
					return this.GetOperandType(context, field.Name);
				case Operand.UnaryOperand unary:
					return GetOperandValueType(context, unary.Operand);
				case Operand.BinaryOperand binary:
					return GetOperandValueType(context, binary);
			}

			var type = operand.GetType();

			if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Operand.ConstantOperand<>))
				return type.GenericTypeArguments[0];

			return null;
		}

		private Type GetOperandValueType(TContext context, Operand.BinaryOperand binary)
		{
			if(binary == null)
				return null;

			var type1 = GetOperandValueType(context, binary.Left);

			if(type1 == typeof(string))
				return typeof(string);

			var type2 = GetOperandValueType(context, binary.Right);

			if(type2 == typeof(string))
				return typeof(string);

			return type1 ?? type2;
		}

		private IExpression GenerateAggregate(TContext context, IStatementBase statement, Operand.AggregateOperand aggregate)
		{
			var entity = this.GetEntity(context);
			SelectStatement selection = null;
			var parts = aggregate.Member.Split('.', StringSplitOptions.RemoveEmptyEntries);

			if(parts.Length == 1)
			{
				if(!entity.Properties.TryGet(parts[0], out var property))
					throw new DataException($"");

				selection = new SelectStatement(entity);
				var field = selection.Table.CreateField(property);
				selection.Select.Members.Add(new AggregateExpression(aggregate.Function, field));
			}
			else
			{
				if(!entity.Properties.TryGet(parts[0], out var property))
					throw new DataException($"");

				if(property.IsSimplex)
					throw new DataException($"");

				var complex = (IDataEntityComplexProperty)property;

				selection = new SelectStatement(complex.Foreign);
				var field = selection.Table.CreateField(complex.Foreign.Properties.Get(parts[1]));
				selection.Select.Members.Add(new AggregateExpression(aggregate.Function, field));

				var conditions = ConditionExpression.And();

				foreach(var link in complex.Links)
				{
					conditions.Add(Expression.Equal(
						selection.Table.CreateField(link.Foreign),
						statement.Table.CreateField(link.Principal)));
				}

				selection.Where = conditions;
			}

			if(aggregate.Filter != null)
			{
				if(selection.Where == null)
					selection.Where = selection.Where(aggregate.Filter.Flatten());
				else
					selection.Where = ConditionExpression.And(selection.Where, selection.Where(aggregate.Filter.Flatten()));
			}

			selection.Table.Alias = null;
			return selection;
		}
		#endregion

		#region 抽象方法
		protected abstract Type GetOperandType(TContext context, string name);
		protected abstract IExpression GetField(TContext context, IStatementBase statement, string name);
		protected abstract IExpression GetValue(TContext context, IStatementBase statement, object value);
		protected abstract IDataEntity GetEntity(TContext context);
		#endregion
	}
}
