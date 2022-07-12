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
using System.Data;
using System.Data.Common;
using System.Collections;
using System.Collections.Generic;

using Zongsoft.Data.Metadata;

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
					if(parameter.Schema == null || parameter.IsChanged)
					{
						if(parameter.Value is IDataValueBinder binder)
							dbParameter.Value = binder.Bind(context, data, TryGetParameterValue(data, parameter.Schema, null, out var value) ? value : null);
						else
							dbParameter.Value = parameter.Value;

						/*
						 * 对于Schema不为空（即表示该参数对应有数据成员），同时还设置了参数值的情况，
						 * 说明该参数值是数据提供程序或导航连接所得，因此必须将其值写回对应的数据项中。
						 */
						if(parameter.Schema != null)
						{
							if(parameter.Schema.Parent == null)
								parameter.Schema.Token.SetValue(data, parameter.IsChanged && !(parameter.Value is IDataValueBinder) ? parameter.Value : dbParameter.Value);
							else
								parameter.Schema.Token.SetValue(parameter.Schema.Parent.Token.GetValue(data), parameter.IsChanged && !(parameter.Value is IDataValueBinder) ? parameter.Value : dbParameter.Value);
						}
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

		private static bool TryGetParameterValue(object data, SchemaMember member, DbType? dbType, out object value)
		{
			value = null;

			//尝试递归解析当前成员对应的所属数据
			data = Recursive(data, member);

			if(data is IModel model)
			{
				if(model.HasChanges(member.Name))
				{
					value = member.Token.GetValue(data, dbType.HasValue ? dbType.Value.AsType() : null);
					return true;
				}

				return false;
			}

			if(data is IDataDictionary dictionary)
			{
				if(dictionary.HasChanges(member.Name))
				{
					value = member.Token.GetValue(data, dbType.HasValue ? dbType.Value.AsType() : null);
					return true;
				}

				return false;
			}

			value = member.Token.GetValue(data, dbType.HasValue ? dbType.Value.AsType() : null);
			return true;

			static object Recursive(object data, SchemaMember member)
			{
				if(data == null || member == null || member.Parent == null || member.Parent.Token.IsMultiple)
					return data;

				var stack = new Stack<SchemaMember>();

				while(member.Parent != null)
				{
					stack.Push(member.Parent);
					member = member.Parent;
				}

				while(stack.Count > 0)
				{
					member = stack.Pop();
					data = member.Token.GetValue(data);

					if(data == null)
						return null;
				}

				return data;
			}
		}

		private static object GetParameterValue(object data, SchemaMember member, DbType? dbType)
		{
			return TryGetParameterValue(data, member, dbType, out var value) ? value : ((IDataEntitySimplexProperty)member.Token.Property).DefaultValue;
		}

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
						throw new DataException($"The specified '{ctx.FullPath}' member is a one-to-many composite(navigation) property that cannot appear in the sorting and specific condition clauses.");
					}

					source = statement.Join(aliaser, source, complex, ctx.FullPath);
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

		public static IExpression Where(this IStatement statement, ICondition criteria, Aliaser aliaser, bool fieldExpending = true)
		{
			if(criteria == null)
				return null;

			if(criteria is Condition c)
				return GetConditionExpression(statement, aliaser, c, fieldExpending);

			if(criteria is ConditionCollection cc)
				return GetConditionExpression(statement, aliaser, cc, fieldExpending);

			throw new NotSupportedException($"The '{criteria.GetType().FullName}' type is an unsupported condition type.");
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

				throw new DataException($"Unable to build a subquery corresponding to the specified '{condition.Name}' parameter({condition.Operator}).");
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
					throw new NotSupportedException($"Unsupported '{condition.Operator}' condition operation.");
			}
		}

		private static IExpression GetConditionValue(IStatement statement, Aliaser aliaser, ConditionOperator @operator, object value, DbType dbType, bool fieldExpending)
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
					collection.Add(statement.Parameters.AddParameter(item, dbType));

				return collection;
			}

			return statement.Parameters.AddParameter(value, dbType);
		}

		private static IExpression GetOperandExpression(this IStatement statement, Aliaser aliaser, Operand operand, bool fieldExpending, out DbType dbType)
		{
			dbType = DbType.Object;

			if(operand == null)
				return null;

			switch(operand.Type)
			{
				case OperandType.Field:
					return fieldExpending ?
						GetField(statement, ((Operand.FieldOperand)operand).Name, aliaser, out dbType) :
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
					return Expression.Not(GetOperandExpression(statement, aliaser, ((Operand.UnaryOperand)operand).Operand, fieldExpending, out dbType));
				case OperandType.Negate:
					return Expression.Negate(GetOperandExpression(statement, aliaser, ((Operand.UnaryOperand)operand).Operand, fieldExpending, out dbType));
				case OperandType.Add:
					return GetBinaryExpression(statement, aliaser, operand, Expression.Add, fieldExpending, out dbType);
				case OperandType.Subtract:
					return GetBinaryExpression(statement, aliaser, operand, Expression.Subtract, fieldExpending, out dbType);
				case OperandType.Multiply:
					return GetBinaryExpression(statement, aliaser, operand, Expression.Multiply, fieldExpending, out dbType);
				case OperandType.Modulo:
					return GetBinaryExpression(statement, aliaser, operand, Expression.Modulo, fieldExpending, out dbType);
				case OperandType.Divide:
					return GetBinaryExpression(statement, aliaser, operand, Expression.Divide, fieldExpending, out dbType);
				case OperandType.And:
					return GetBinaryExpression(statement, aliaser, operand, Expression.And, fieldExpending, out dbType);
				case OperandType.Or:
					return GetBinaryExpression(statement, aliaser, operand, Expression.Or, fieldExpending, out dbType);
				case OperandType.Xor:
					return GetBinaryExpression(statement, aliaser, operand, Expression.Xor, fieldExpending, out dbType);
				default:
					throw new DataException($"Unsupported '{operand.Type}' operand type.");
			}

			static IExpression GetBinaryExpression(IStatement host, Aliaser aliaser, Operand opd, Func<IExpression, IExpression, IExpression> generator, bool fieldExpending, out DbType dbType)
			{
				var binary = (Operand.BinaryOperand)opd;

				return generator(
					host.GetOperandExpression(aliaser, binary.Left, fieldExpending, out dbType),
					host.GetOperandExpression(aliaser, binary.Right, fieldExpending, out dbType));
			}
		}

		private static FieldIdentifier GetField(IStatement host, string name, Aliaser aliaser, out DbType dbType)
		{
			var source = From(host, name, aliaser, (src, complex) => CreateSubquery(host, aliaser, src, complex, null), out var property);

			if(property.IsSimplex)
			{
				dbType = ((IDataEntitySimplexProperty)property).Type;
				return source.CreateField(property);
			}

			throw new DataException($"The specified '{name}' field is associated with a composite(navigation) property and cannot perform arithmetic or logical operations on it.");
		}

		private static ISource GetSubquery(this IStatement statement, string name, Aliaser aliaser, ICondition filter)
		{
			var subquery = From(statement, name, aliaser, (src, complex) => CreateSubquery(statement, aliaser, src, complex, filter), out var property);

			if(property.IsComplex && ((IDataEntityComplexProperty)property).Multiplicity == DataAssociationMultiplicity.Many)
				return subquery;

			throw new DataException($"The specified '{name}' field is associated with a one-to-many composite(navigation) property and a subquery cannot be generated.");
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

		private static ParameterExpression AddParameter(this ParameterExpressionCollection parameters, object value, DbType? dbType = null)
		{
			var parameter = dbType.HasValue ? Expression.Parameter(dbType.Value, value) : Expression.Parameter(value);
			parameters.Add(parameter);
			return parameter;
		}

		private static int GetCollectionCount(object value)
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
}
