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
				return ConditionExtension.ToExpression(c,
					field => GetField(statement, field),
					parameter => statement.Parameters.Add(parameter));

			if(criteria is ConditionCollection cc)
				return ConditionExtension.ToExpression(cc,
					field => GetField(statement, field),
					parameter => statement.Parameters.Add(parameter));

			throw new NotSupportedException($"The '{criteria.GetType().FullName}' type is an unsupported condition type.");

			FieldIdentifier GetField(IStatement host, Condition condition)
			{
				var source = From(statement, condition.Name, (src, complex) => CreateSubquery(host, src, complex, condition.Value as ICondition), out var property);

				if(property.IsSimplex)
					return source.CreateField(property);

				if(condition.Operator == ConditionOperator.Exists || condition.Operator == ConditionOperator.NotExists)
				{
					condition.Value = source;
					return null;
				}

				throw new DataException($"The specified '{condition.Name}' condition is associated with a one-to-many composite(navigation) property and does not support the {condition.Operator} operation.");
			}

			ISource CreateSubquery(IStatement host, ISource source, IDataEntityComplexProperty complex, ICondition condition)
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
		}
	}
}
