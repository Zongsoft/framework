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
	public abstract class OperandConverter<TContext> where TContext : IDataAccessContext, IAliasable
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
				case Operand.FunctionOperand.CastFunction casting:
					return casting.ConversionType.IsDecimal() ?
						new CastFunctionExpression(this.Convert(context, statement, casting.Value), casting.ConversionType, casting.Precision, casting.Scale, casting.Style) :
						new CastFunctionExpression(this.Convert(context, statement, casting.Value), casting.ConversionType, casting.Length, casting.Style);
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
			SelectStatement selection;
			IDataEntityProperty property;
			FieldIdentifier field;
			var parts = aggregate.Member.Split('.', StringSplitOptions.RemoveEmptyEntries);

			switch(parts.Length)
			{
				case 1:
					if(!entity.Properties.TryGet(parts[0], out property))
						throw new DataException($"The specified '{parts[0]}' field does not exist in the '{entity.Name}' entity.");

					selection = new SelectStatement(entity);
					field = selection.Table.CreateField(property);
					selection.Select.Members.Add(new AggregateExpression(aggregate.Function, aggregate.Distinct, field));

					break;
				case 2:
					if(!entity.Properties.TryGet(parts[0], out property))
						throw new DataException($"The specified '{parts[0]}' field does not exist in the '{entity.Name}' entity.");

					if(property.IsSimplex)
						throw new DataException($"The specified '{parts[0]}' is a simple property and cannot be navigated.");

					var complex = (IDataEntityComplexProperty)property;

					selection = new SelectStatement(complex.Foreign);
					field = selection.Table.CreateField(complex.Foreign.Properties.Get(parts[1]));
					selection.Select.Members.Add(new AggregateExpression(aggregate.Function, aggregate.Distinct, field));

					var conditions = ConditionExpression.And();

					foreach(var link in complex.Links)
					{
						ISource src = statement.Table;

						foreach(var anchor in link.GetAnchors())
						{
							if(anchor.IsComplex)
							{
								src = selection.Join(context.Aliaser, selection, (IDataEntityComplexProperty)anchor);
							}
							else
								conditions.Add(Expression.Equal(
									selection.Table.CreateField(link.ForeignKey),
									src.CreateField(anchor)));
						}
					}

					selection.Where = conditions;
					break;
				default:
					throw new DataException($"Invalid aggregate member ‘{aggregate.Member}’ because its navigation level is too deep.");
			}

			if(aggregate.Filter != null)
			{
				if(selection.Where == null)
					selection.Where = selection.Where(aggregate.Filter.Flatten(), context.Aliaser);
				else
					selection.Where = ConditionExpression.And(selection.Where, selection.Where(aggregate.Filter.Flatten(), context.Aliaser));
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
