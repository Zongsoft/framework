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
using System.Data;
using System.Data.Common;
using System.Collections;

using Zongsoft.Data.Metadata;
using System.Runtime.Serialization.Formatters;

namespace Zongsoft.Data.Common.Expressions
{
	public static class StatementExtension
	{
		public static void Bind(this IStatementBase statement, IDataMutateContextBase context, DbCommand command, object data)
		{
			if(data == null || !statement.HasParameters)
				return;

			foreach(var parameter in statement.Parameters)
			{
				var dbParameter = command.Parameters[parameter.Name];

				if(dbParameter.Direction == ParameterDirection.Input || dbParameter.Direction == ParameterDirection.InputOutput)
				{
					if(parameter.Schema == null || parameter.HasValue)
					{
						if(parameter.Value is IDataValueBinder binder)
							dbParameter.Value = binder.Bind(context, data, GetParameterValue(data, parameter.Schema, null));
						else
							dbParameter.Value = parameter.Value;

						/*
						 * 对于Schema不为空（即表示该参数对应有数据成员），同时还设置了参数值的情况，
						 * 说明该参数值是数据提供程序或导航连接所得，因此必须将其值写回对应的数据项中。
						 */
						if(parameter.Schema != null)
							parameter.Schema.Token.SetValue(data, parameter.HasValue && !(parameter.Value is IDataValueBinder) ? parameter.Value : dbParameter.Value);
					}
					else if(data != null)
					{
						dbParameter.Value = GetParameterValue(data, parameter.Schema, dbParameter.DbType);
					}
				}

				if(dbParameter.Value == null)
					dbParameter.Value = DBNull.Value;
			}
		}

		private static object GetParameterValue(object data, SchemaMember member, DbType? dbType)
		{
			if(data is IModel model)
			{
				if(model.HasChanges(member.Name))
					return member.Token.GetValue(data, dbType.HasValue ? Utility.FromDbType(dbType.Value) : null);
				else
					return ((IDataEntitySimplexProperty)member.Token.Property).DefaultValue;
			}

			if(data is IDataDictionary dictionary)
			{
				if(dictionary.HasChanges(member.Name))
					return member.Token.GetValue(data, dbType.HasValue ? Utility.FromDbType(dbType.Value) : null);
				else
					return ((IDataEntitySimplexProperty)member.Token.Property).DefaultValue;
			}

			return member.Token.GetValue(data, dbType.HasValue ? Utility.FromDbType(dbType.Value) : null);
		}

		public static ISource From(this IStatement statement, string memberPath, Func<ISource, IDataEntityComplexProperty, ISource> subqueryFactory, out IDataEntityProperty property)
		{
			return From(statement, statement.Table, memberPath, subqueryFactory, out property);
		}

		public static ISource From(this IStatement statement, TableIdentifier origin, string memberPath, Func<ISource, IDataEntityComplexProperty, ISource> subqueryFactory, out IDataEntityProperty property)
		{
			var found = origin.Reduce(memberPath, ctx =>
			{
				var source = ctx.Source;

				if(ctx.Ancestors != null)
				{
					foreach(var ancestor in ctx.Ancestors)
					{
						source = statement.Join(source, ancestor, ctx.Path);
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
						throw new DataException($"The specified '{ctx.FullPath}' member is a one-to-many composite(navigation) property that cannot appear in the sorting and specific condition clauses.");
					}

					source = statement.Join(source, complex, ctx.FullPath);
				}

				return source;
			});

			if(found.IsFailed)
				throw new DataException($"The specified '{memberPath}' member does not exist in the '{origin.Entity?.Name}' entity and it's inherits.");

			//输出找到的属性元素
			property = found.Property;

			//返回找到的源
			return found.Source;
		}

		public static IExpression Where(this IStatement statement, ICondition criteria)
		{
			if(criteria == null)
				return null;

			if(criteria is Condition c)
				return GetConditionExpression(statement, c);

			if(criteria is ConditionCollection cc)
				return GetConditionExpression(statement, cc);

			throw new NotSupportedException($"The '{criteria.GetType().FullName}' type is an unsupported condition type.");
		}

		private static ConditionExpression GetConditionExpression(IStatement statement, ConditionCollection conditions)
		{
			if(conditions == null)
				throw new ArgumentNullException(nameof(conditions));

			ConditionExpression expressions = new ConditionExpression(conditions.Combination);

			foreach(var condition in conditions)
			{
				switch(condition)
				{
					case Condition c:
						var item = GetConditionExpression(statement, c);

						if(item != null)
							expressions.Add(item);

						break;
					case ConditionCollection cc:
						var items = GetConditionExpression(statement, cc);

						if(items != null && items.Count > 0)
							expressions.Add(items);

						break;
				}
			}

			return expressions.Count > 0 ? expressions : null;
		}

		private static IExpression GetConditionExpression(IStatement statement, Condition condition)
		{
			if(condition == null)
				throw new ArgumentNullException(nameof(condition));

			if(condition.Operator == ConditionOperator.Exists || condition.Operator == ConditionOperator.NotExists)
			{
				if(condition.Field.Type == OperandType.Field && condition.Value is ICondition filter)
					return condition.Operator == ConditionOperator.Exists ?
						Expression.Exists((IExpression)statement.GetSubquery(condition.Name, filter)) :
						Expression.NotExists((IExpression)statement.GetSubquery(condition.Name, filter));

				throw new DataException($"Unable to build a subquery corresponding to the specified '{condition.Name}' parameter({condition.Operator}).");
			}

			var field = statement.GetOperandExpression(condition.Field, out _);

			if(condition.Value == null)
			{
				return condition.Operator switch
				{
					ConditionOperator.Like => Expression.Equal(field, null),
					ConditionOperator.Equal => Expression.Equal(field, null),
					ConditionOperator.NotEqual => Expression.NotEqual(field, null),
					_ => throw new DataException($"The specified '{condition.Name}' parameter value of the type {condition.Operator} condition is null."),
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

					throw new DataException($"Illegal range condition value.");
				case ConditionOperator.Like:
					return Expression.Like(field, GetConditionValue(statement, condition.Value));
				case ConditionOperator.In:
					return Expression.In(field, GetConditionValue(statement, condition.Value));
				case ConditionOperator.NotIn:
					return Expression.NotIn(field, GetConditionValue(statement, condition.Value));
				case ConditionOperator.Equal:
					return Expression.Equal(field, GetConditionValue(statement, condition.Value));
				case ConditionOperator.NotEqual:
					return Expression.NotEqual(field, GetConditionValue(statement, condition.Value));
				case ConditionOperator.GreaterThan:
					return Expression.GreaterThan(field, GetConditionValue(statement, condition.Value));
				case ConditionOperator.GreaterThanEqual:
					return Expression.GreaterThanOrEqual(field, GetConditionValue(statement, condition.Value));
				case ConditionOperator.LessThan:
					return Expression.LessThan(field, GetConditionValue(statement, condition.Value));
				case ConditionOperator.LessThanEqual:
					return Expression.LessThanOrEqual(field, GetConditionValue(statement, condition.Value));
				default:
					throw new NotSupportedException($"Unsupported '{condition.Operator}' condition operation.");
			}
		}

		private static IExpression GetConditionValue(IStatement statement, object value)
		{
			if(value == null)
				return null;

			if(value is IExpression expression)
				return expression;

			if(value is Operand operand)
				return statement.GetOperandExpression(operand, out _);

			if(value.GetType().IsArray || (value.GetType() != typeof(string) && value is IEnumerable))
			{
				var collection = new ExpressionCollection();

				foreach(var item in (IEnumerable)value)
					collection.Add(statement.Parameters.AddParameter(item));

				return collection;
			}

			return statement.Parameters.AddParameter(value);
		}

		private static IExpression GetOperandExpression(this IStatement statement, Operand operand, out DbType dbType)
		{
			dbType = DbType.Object;

			if(operand == null)
				return null;

			switch(operand.Type)
			{
				case OperandType.Field:
					return GetField(statement, ((Operand.FieldOperand)operand).Name, out dbType);
				case OperandType.Constant:
					var value = Reflection.Reflector.GetValue(operand, nameof(Operand.ConstantOperand<object>.Value));

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
					return Expression.Not(GetOperandExpression(statement, ((Operand.UnaryOperand)operand).Operand, out dbType));
				case OperandType.Negate:
					return Expression.Negate(GetOperandExpression(statement, ((Operand.UnaryOperand)operand).Operand, out dbType));
				case OperandType.Add:
					return GetBinaryExpression(statement, operand, Expression.Add, out dbType);
				case OperandType.Subtract:
					return GetBinaryExpression(statement, operand, Expression.Subtract, out dbType);
				case OperandType.Multiply:
					return GetBinaryExpression(statement, operand, Expression.Multiply, out dbType);
				case OperandType.Modulo:
					return GetBinaryExpression(statement, operand, Expression.Modulo, out dbType);
				case OperandType.Divide:
					return GetBinaryExpression(statement, operand, Expression.Divide, out dbType);
				case OperandType.And:
					return GetBinaryExpression(statement, operand, Expression.And, out dbType);
				case OperandType.Or:
					return GetBinaryExpression(statement, operand, Expression.Or, out dbType);
				case OperandType.Xor:
					return GetBinaryExpression(statement, operand, Expression.Xor, out dbType);
				default:
					throw new DataException($"Unsupported '{operand.Type}' operand type.");
			}

			static IExpression GetBinaryExpression(IStatement host, Operand opd, Func<IExpression, IExpression, IExpression> generator, out DbType dbType)
			{
				var binary = (Operand.BinaryOperand)opd;
				return generator(host.GetOperandExpression(binary.Left, out dbType), host.GetOperandExpression(binary.Right, out dbType));
			}
		}

		private static FieldIdentifier GetField(IStatement host, string name, out DbType dbType)
		{
			var source = From(host, name, (src, complex) => CreateSubquery(host, src, complex, null), out var property);

			if(property.IsSimplex)
			{
				dbType = ((IDataEntitySimplexProperty)property).Type;
				return source.CreateField(property);
			}

			dbType = DbType.Object;
			throw new DataException($"The specified '{name}' field is associated with a composite(navigation) property and cannot perform arithmetic or logical operations on it.");
		}

		private static ISource GetSubquery(this IStatement statement, string name, ICondition filter)
		{
			var subquery = From(statement, name, (src, complex) => CreateSubquery(statement, src, complex, filter), out var property);

			if(property.IsComplex && ((IDataEntityComplexProperty)property).Multiplicity == DataAssociationMultiplicity.Many)
				return subquery;

			throw new DataException($"The specified '{name}' field is associated with a one-to-many composite(navigation) property and a subquery cannot be generated.");
		}

		private static ISource CreateSubquery(IStatement host, ISource source, IDataEntityComplexProperty complex, ICondition condition)
		{
			var subquery = host.Subquery(complex.Foreign);
			var where = ConditionExpression.And();

			foreach(var link in complex.Links)
			{
				subquery.Select.Members.Add(subquery.Table.CreateField(link.Foreign));

				where.Add(Expression.Equal(
					subquery.Table.CreateField(link.Foreign),
					source.CreateField(link.Principal)
				));
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

			if(condition != null)
				where.Add(Where(subquery, condition));

			subquery.Where = where;
			return subquery;
		}

		private static ParameterExpression AddParameter(this ParameterExpressionCollection parameters, object value)
		{
			var parameter = Expression.Parameter(value);
			parameters.Add(parameter);
			return parameter;
		}
	}
}
