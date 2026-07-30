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
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;

using Zongsoft.Data.Metadata;

namespace Zongsoft.Data.Common.Expressions;

public static class StatementExtension
{
	public static void Bind(this IStatementBase statement, IDataMutateContextBase context, DbCommand command) => GetBinder(context).Bind(context, statement, command);
	public static ValueTask BindAsync(this IStatementBase statement, IDataMutateContextBase context, DbCommand command, CancellationToken cancellation) => GetBinder(context).BindAsync(context, statement, command, cancellation);
	private static IStatementBinder GetBinder(IDataMutateContextBase context) => context is IDataAccessContext accessor ? accessor.Source.Driver?.Binder ?? StatementBinder.Default : StatementBinder.Default;

	public static ISource From(this IStatement statement, string memberPath, Aliaser aliaser, Func<ISource, IDataEntityComplexProperty, ISource> subqueryFactory, out IDataEntityProperty property)
	{
		return From(statement, statement.Table, memberPath, aliaser, subqueryFactory, out property);
	}

	public static ISource From(this IStatement statement, TableIdentifier origin, string memberPath, Aliaser aliaser, Func<ISource, IDataEntityComplexProperty, ISource> subqueryFactory, out IDataEntityProperty property)
	{
		var found = origin.Reduce(memberPath, ctx =>
		{
			var source = ctx.Source;

			if(ctx.Ancestors != null)
			{
				foreach(var ancestor in ctx.Ancestors)
				{
					source = statement.Join(aliaser, source, ancestor, ctx.Path);
				}
			}

			if(ctx.Property.IsComplex)
			{
				var complex = (IDataEntityComplexProperty)ctx.Property;

				if(complex.Multiplicity == DataAssociationMultiplicity.Many)
				{
					if(subqueryFactory != null)
						return subqueryFactory(source, complex);

					//如果不允许一对多的子查询则抛出异常
					throw new DataException(string.Format(Properties.Resources.Sorting_OneToManyMemberUnsupported_Message, ctx.FullPath));
				}

				source = statement.Join(aliaser, source, complex, ctx.FullPath);
			}

			return source;
		});

		if(found.IsFailed)
			throw new DataException(string.Format(Properties.Resources.DataEntity_InheritedMemberNotFound_Message, memberPath, origin.Entity?.Name));

		//输出找到的属性元素
		property = found.Property;

		//返回找到的源
		return found.Source;
	}

	public static IExpression Where(this IStatement statement, ICondition criteria, Aliaser aliaser, bool fieldExpending = true)
	{
		if(criteria == null)
			return null;

		if(criteria is Condition c)
			return GetConditionExpression(statement, aliaser, c, fieldExpending);

		if(criteria is ConditionCollection cc)
			return GetConditionExpression(statement, aliaser, cc, fieldExpending);

		throw new NotSupportedException(string.Format(Properties.Resources.Condition_TypeUnsupported_Message, criteria.GetType().FullName));
	}

	private static ConditionExpression GetConditionExpression(IStatement statement, Aliaser aliaser, ConditionCollection conditions, bool fieldExpending)
	{
		if(conditions == null)
			throw new ArgumentNullException(nameof(conditions));

		ConditionExpression expressions = new ConditionExpression(conditions.Combination);

		foreach(var condition in conditions)
		{
			switch(condition)
			{
				case Condition c:
					var item = GetConditionExpression(statement, aliaser, c, fieldExpending);

					if(item != null)
						expressions.Add(item);

					break;
				case ConditionCollection cc:
					var items = GetConditionExpression(statement, aliaser, cc, fieldExpending);

					if(items != null && items.Count > 0)
						expressions.Add(items);

					break;
			}
		}

		return expressions.Count > 0 ? expressions : null;
	}

	private static IExpression GetConditionExpression(IStatement statement, Aliaser aliaser, Condition condition, bool fieldExpending)
	{
		if(condition == null)
			throw new ArgumentNullException(nameof(condition));

		if(condition.Operator == ConditionOperator.Exists || condition.Operator == ConditionOperator.NotExists)
		{
			if(condition.Field.Type == OperandType.Field)
			{
				var subquery = statement.GetSubquery(condition.Name, aliaser, condition.Value as ICondition);

				//设置子查询的返回记录数限定为1，以提升查询性能
				if(subquery is SelectStatement select)
					select.Paging = Paging.Limit(1);

				return condition.Operator == ConditionOperator.Exists ?
					Expression.Exists((IExpression)subquery) :
					Expression.NotExists((IExpression)subquery);
			}

			throw new DataException(string.Format(Properties.Resources.Subquery_BuildFailed_Message, condition.Name, condition.Operator));
		}

		var field = statement.GetOperandExpression(aliaser, condition.Field, fieldExpending, out var dbType);

		if(field == null)
			return null;

		if(condition.Value == null)
		{
			return condition.Operator switch
			{
				ConditionOperator.Like => Expression.Equal(field, null),
				ConditionOperator.Equal => Expression.Equal(field, null),
				ConditionOperator.NotEqual => Expression.NotEqual(field, null),
				_ => throw new DataException(string.Format(Properties.Resources.Condition_NullParameterValue_Message, condition.Name, condition.Operator)),
			};
		}

		if(condition.Operator == ConditionOperator.Equal && Range.IsRange(condition.Value))
			condition.Operator = ConditionOperator.Between;

		switch(condition.Operator)
		{
			case ConditionOperator.Between:
				if(Range.IsRange(condition.Value, out var minimum, out var maximum))
				{
					if(object.Equals(minimum, maximum))
						return Expression.Equal(field, statement.Parameters.AddParameter(minimum));

					if(minimum == null)
						return maximum == null ? null : Expression.LessThanOrEqual(field, statement.Parameters.AddParameter(maximum));

					return maximum == null ?
						   Expression.GreaterThanOrEqual(field, statement.Parameters.AddParameter(minimum)) :
						   Expression.Between(field, statement.Parameters.AddParameter(minimum), statement.Parameters.AddParameter(maximum));
				}

				throw new DataException(Properties.Resources.Condition_InvalidRangeValue_Message);
			case ConditionOperator.Like:
				return Expression.Like(field, GetConditionValue(statement, aliaser, condition.Operator, condition.Value, dbType, fieldExpending));
			case ConditionOperator.In:
				var value = GetConditionValue(statement, aliaser, condition.Operator, condition.Value, dbType, fieldExpending);
				var count = GetCollectionCount(value);

				if(count == 0)
					return null;

				if(count == 1 && value is IEnumerable<IExpression> es1)
					return Expression.Equal(field, es1.FirstOrDefault());

				return Expression.In(field, value);
			case ConditionOperator.NotIn:
				value = GetConditionValue(statement, aliaser, condition.Operator, condition.Value, dbType, fieldExpending);
				count = GetCollectionCount(value);

				if(count == 0)
					return null;

				if(count == 1 && value is IEnumerable<IExpression> es2)
					return Expression.NotEqual(field, es2.FirstOrDefault());

				return Expression.NotIn(field, value);
			case ConditionOperator.Equal:
				return Expression.Equal(field, GetConditionValue(statement, aliaser, condition.Operator, condition.Value, dbType, fieldExpending));
			case ConditionOperator.NotEqual:
				return Expression.NotEqual(field, GetConditionValue(statement, aliaser, condition.Operator, condition.Value, dbType, fieldExpending));
			case ConditionOperator.GreaterThan:
				return Expression.GreaterThan(field, GetConditionValue(statement, aliaser, condition.Operator, condition.Value, dbType, fieldExpending));
			case ConditionOperator.GreaterThanEqual:
				return Expression.GreaterThanOrEqual(field, GetConditionValue(statement, aliaser, condition.Operator, condition.Value, dbType, fieldExpending));
			case ConditionOperator.LessThan:
				return Expression.LessThan(field, GetConditionValue(statement, aliaser, condition.Operator, condition.Value, dbType, fieldExpending));
			case ConditionOperator.LessThanEqual:
				return Expression.LessThanOrEqual(field, GetConditionValue(statement, aliaser, condition.Operator, condition.Value, dbType, fieldExpending));
			default:
				throw new NotSupportedException(string.Format(Properties.Resources.Condition_OperatorUnsupported_Message, condition.Operator));
		}

		static int GetCollectionCount(object value)
		{
			if(value == null)
				return 0;

			if(value is ICollection<IExpression> genericCollection)
				return genericCollection.Count;

			if(value is ICollection classicCollection)
				return classicCollection.Count;

			return 1;
		}
	}

	private static IExpression GetConditionValue(IStatement statement, Aliaser aliaser, ConditionOperator @operator, object value, DataType type, bool fieldExpending)
	{
		if(value == null)
			return null;

		if(value is IExpression expression)
			return expression;

		if(value is Operand operand)
			return statement.GetOperandExpression(aliaser, operand, fieldExpending, out _);

		if((@operator == ConditionOperator.In || @operator == ConditionOperator.NotIn) && (value.GetType().IsArray || (value.GetType() != typeof(string) && value is IEnumerable)))
		{
			var collection = new ExpressionCollection();

			foreach(var item in (IEnumerable)value)
				collection.Add(statement.Parameters.AddParameter(item, type));

			return collection;
		}

		return statement.Parameters.AddParameter(value, type);
	}

	private static IExpression GetOperandExpression(this IStatement statement, Aliaser aliaser, Operand operand, bool fieldExpending, out DataType type)
	{
		type = DataType.Object;

		if(operand == null)
			return null;

		switch(operand.Type)
		{
			case OperandType.Field:
				return fieldExpending ?
					GetField(statement, ((Operand.FieldOperand)operand).Name, aliaser, out type) :
					new FieldIdentifier(((Operand.FieldOperand)operand).Name);
			case OperandType.Constant:
				var value = Reflection.Reflector.GetValue(ref operand, nameof(Operand.ConstantOperand<object>.Value));

				if(value == null || Convert.IsDBNull(value))
					return Expression.Constant(null);

				if(Zongsoft.Common.TypeExtension.IsCollection(value.GetType()))
				{
					var collection = new ExpressionCollection();

					foreach(var item in (ICollection)value)
						collection.Add(statement.Parameters.AddParameter(item));

					return collection;
				}

				return statement.Parameters.AddParameter(value);
			case OperandType.Not:
				return Expression.Not(GetOperandExpression(statement, aliaser, ((Operand.UnaryOperand)operand).Operand, fieldExpending, out type));
			case OperandType.Negate:
				return Expression.Negate(GetOperandExpression(statement, aliaser, ((Operand.UnaryOperand)operand).Operand, fieldExpending, out type));
			case OperandType.Add:
				return GetBinaryExpression(statement, aliaser, operand, Expression.Add, fieldExpending, out type);
			case OperandType.Subtract:
				return GetBinaryExpression(statement, aliaser, operand, Expression.Subtract, fieldExpending, out type);
			case OperandType.Multiply:
				return GetBinaryExpression(statement, aliaser, operand, Expression.Multiply, fieldExpending, out type);
			case OperandType.Modulo:
				return GetBinaryExpression(statement, aliaser, operand, Expression.Modulo, fieldExpending, out type);
			case OperandType.Divide:
				return GetBinaryExpression(statement, aliaser, operand, Expression.Divide, fieldExpending, out type);
			case OperandType.And:
				return GetBinaryExpression(statement, aliaser, operand, Expression.And, fieldExpending, out type);
			case OperandType.Or:
				return GetBinaryExpression(statement, aliaser, operand, Expression.Or, fieldExpending, out type);
			case OperandType.Xor:
				return GetBinaryExpression(statement, aliaser, operand, Expression.Xor, fieldExpending, out type);
			default:
				throw new DataException(string.Format(Properties.Resources.Operand_Unsupported_Message, operand.Type));
		}

		static IExpression GetBinaryExpression(IStatement host, Aliaser aliaser, Operand opd, Func<IExpression, IExpression, IExpression> generator, bool fieldExpending, out DataType type)
		{
			var binary = (Operand.BinaryOperand)opd;

			return generator(
				host.GetOperandExpression(aliaser, binary.Left, fieldExpending, out type),
				host.GetOperandExpression(aliaser, binary.Right, fieldExpending, out type));
		}
	}

	private static FieldIdentifier GetField(IStatement host, string name, Aliaser aliaser, out DataType type)
	{
		var source = From(host, name, aliaser, (src, complex) => CreateSubquery(host, aliaser, src, complex, null), out var property);

		if(property.IsSimplex)
		{
			type = ((IDataEntitySimplexProperty)property).Type;
			return source.CreateField(property);
		}

		throw new DataException(string.Format(Properties.Resources.Field_ComplexOperationUnsupported_Message, name));
	}

	private static ISource GetSubquery(this IStatement statement, string name, Aliaser aliaser, ICondition filter)
	{
		var subquery = From(statement, name, aliaser, (src, complex) => CreateSubquery(statement, aliaser, src, complex, filter), out var property);

		if(property.IsComplex && ((IDataEntityComplexProperty)property).Multiplicity == DataAssociationMultiplicity.Many)
			return subquery;

		throw new DataException(string.Format(Properties.Resources.Field_SubqueryUnsupported_Message, name));
	}

	private static ISource CreateSubquery(IStatement host, Aliaser aliaser, ISource source, IDataEntityComplexProperty complex, ICondition criteria)
	{
		var subquery = host.Subquery(new TableIdentifier(complex.Foreign, aliaser.Generate()));
		var where = ConditionExpression.And();

		foreach(var link in complex.Links)
		{
			subquery.Select.Members.Add(subquery.Table.CreateField(link.ForeignKey));

			foreach(var anchor in link.GetAnchors())
			{
				if(anchor.IsComplex)
				{
					source = host.Join(aliaser, source, (IDataEntityComplexProperty)anchor);
				}
				else
				{
					where.Add(Expression.Equal(
						subquery.Table.CreateField(link.ForeignKey),
						source.CreateField(anchor)
					));
				}
			}
		}

		if(complex.HasConstraints())
		{
			foreach(var constraint in complex.Constraints)
			{
				where.Add(Expression.Equal(
					subquery.Table.CreateField(constraint.Name),
					complex.GetConstraintValue(constraint)
				));
			}
		}

		if(criteria != null)
			where.Add(Where(subquery, criteria, aliaser));

		subquery.Where = where;
		return subquery;
	}

	private static ParameterExpression AddParameter(this ParameterExpressionCollection parameters, object value, DataType type = null)
	{
		var parameter = type != null ? Expression.Parameter(type, value) : Expression.Parameter(value);
		parameters.Add(parameter);
		return parameter;
	}
}
