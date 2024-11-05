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
using System.Collections.Generic;

using Zongsoft.Data.Metadata;

namespace Zongsoft.Data.Common.Expressions
{
	public class SelectStatementBuilder : IStatementBuilder<DataSelectContext>
	{
		#region 构建方法
		public IEnumerable<IStatementBase> Build(DataSelectContext context)
		{
			var statement = this.CreateStatement(context);

			if(statement.Select != null)
				statement.Select.IsDistinct = context.Options.IsDistinct;

			//生成分组子句
			if(context.Grouping != null)
				GenerateGrouping(context.Aliaser, statement, context.Grouping);

			//生成查询成员
			if(context.Schema != null && !context.Schema.IsEmpty)
			{
				if(context.Grouping == null)
				{
					foreach(var member in context.Schema.Members)
					{
						//生成数据模式对应的子句
						GenerateSchema(context.Aliaser, statement, statement.Table, member);
					}
				}
				else
				{
					foreach(var member in context.Schema.Members)
					{
						if(member.Token.Property.IsComplex)
							GenerateSchema(context.Aliaser, statement, statement.Table, member);
					}
				}
			}

			//生成条件子句
			statement.Where = statement.Where(context.Validate(), context.Aliaser);

			//生成排序子句
			GenerateSortings(context.Aliaser, statement, statement.Table, context.Sortings);

			yield return statement;
		}
		#endregion

		#region 虚拟方法
		protected virtual SelectStatement CreateStatement(DataSelectContext context) => new(context.Entity) { Paging = context.Paging };
		#endregion

		#region 私有方法
		private static void GenerateGrouping(Aliaser aliaser, SelectStatement statement, Grouping grouping)
		{
			if(grouping == null)
				return;

			if(grouping.Keys != null && grouping.Keys.Length > 0)
			{
				//创建分组子句
				statement.GroupBy = new GroupByClause();

				foreach(var key in grouping.Keys)
				{
					var source = statement.From(key.Name, aliaser, null, out var property);

					if(property.IsComplex)
						throw new DataException($"The grouping key '{property.Name}' can not be a complex property.");

					statement.GroupBy.Keys.Add(source.CreateField(property));
					statement.Select.Members.Add(source.CreateField(property.GetFieldName(out var alias), key.Alias ?? alias));
				}

				if(grouping.Filter != null)
				{
					statement.GroupBy.Having = statement.Where(grouping.Filter, aliaser, false);
				}
			}

			foreach(var aggregate in grouping.Aggregates)
			{
				if(string.IsNullOrEmpty(aggregate.Name) || aggregate.Name == "*")
				{
					statement.Select.Members.Add(
						new AggregateExpression(aggregate, Expression.Constant(0)));
				}
				else
				{
					var source = statement.From(aggregate.Name, aliaser, null, out var property);

					if(property.IsComplex)
						throw new DataException($"The field '{property.Name}' of aggregate function can not be a complex property.");

					statement.Select.Members.Add(
						new AggregateExpression(aggregate, source.CreateField(property)));
				}
			}
		}

		private static void GenerateSortings(Aliaser aliaser, SelectStatement statement, TableIdentifier origin, Sorting[] sortings)
		{
			if(sortings == null || sortings.Length == 0)
				return;

			statement.OrderBy = new OrderByClause();

			foreach(var sorting in sortings)
			{
				if(string.IsNullOrEmpty(sorting.Name))
					continue;

				var source = statement.From(origin, sorting.Name, aliaser, null, out var property);

				var simplex = property.IsSimplex ?
					(IDataEntitySimplexProperty)property :
					throw new DataException($"The specified '{property.Entity.Name}.{property.Name}' is a composite(navigation) property that is not sortable.");

				if(simplex.IsPrimaryKey || simplex.Sortable)
					statement.OrderBy.Add(source.CreateField(property), sorting.Mode);
				else
					throw new DataException($"The specified '{property.Entity.Name}.{property.Name}' property is not sortable and must be enabled for sorting before it can be sorted.");
			}
		}

		private static void GenerateSchema(Aliaser aliaser, SelectStatement statement, ISource origin, SchemaMember member)
		{
			if(member.Ancestors != null)
			{
				foreach(var ancestor in member.Ancestors)
				{
					origin = statement.Join(aliaser, origin, ancestor, member.Path);
				}
			}

			if(member.Token.Property.IsComplex)
			{
				var complex = (IDataEntityComplexProperty)member.Token.Property;

				//一对多的导航属性对应一个新语句（新语句别名即为该导航属性的全称）
				if(complex.Multiplicity == DataAssociationMultiplicity.Many)
				{
					var slave = new SelectStatement(complex.Foreign, member.FullPath) { Paging = member.Paging };
					var table = slave.Table;

					if(complex.ForeignProperty != null)
					{
						if(complex.ForeignProperty.IsSimplex)
							slave.Select.Members.Add(slave.Table.CreateField(complex.ForeignProperty));
						else
							table = (TableIdentifier)slave.Join(aliaser, slave.Table, (IDataEntityComplexProperty)complex.ForeignProperty).Target;
					}

					statement.Slaves.Add(slave);

					//为一对多的导航属性增加必须的链接字段及对应的条件参数
					foreach(var link in complex.Links)
					{
						foreach(var anchor in link.GetAnchors())
						{
							if(anchor.IsComplex)
							{
								origin = statement.Join(aliaser, origin, (IDataEntityComplexProperty)anchor);
							}
							else
							{
								var principalField = origin.CreateField(anchor);
								principalField.Alias = "$" + member.FullPath + ":" + anchor.Name;
								statement.Select.Members.Add(principalField);

								var foreignField = slave.Table.CreateField(link.ForeignKey);
								foreignField.Alias = null;
								if(slave.Where == null)
									slave.Where = Expression.Equal(foreignField, slave.Parameters.Add(anchor.Name, link.ForeignKey.Type));
								else
									slave.Where = Expression.AndAlso(slave.Where,
										Expression.Equal(foreignField, slave.Parameters.Add(anchor.Name, link.ForeignKey.Type)));
							}
						}
					}

					//为导航属性增加约束过滤条件
					if(complex.HasConstraints())
					{
						foreach(var constraint in complex.Constraints)
						{
							slave.Where = Expression.AndAlso(slave.Where,
								Expression.Equal(
									table.CreateField(constraint.Name),
									complex.GetConstraintValue(constraint)));
						}
					}

					if(member.Sortings != null)
						GenerateSortings(aliaser, slave, table, member.Sortings);

					if(member.HasChildren)
					{
						foreach(var child in member.Children)
						{
							GenerateSchema(aliaser, slave, table, child);
						}
					}

					return;
				}

				//对于一对一的导航属性，创建其关联子句即可
				origin = statement.Join(aliaser, origin, complex, member.FullPath);

				//确保导航属性的外链表的主键都在
				if(member.HasChildren)
				{
					foreach(var key in complex.Foreign.Key)
					{
						if(!member.Children.Contains(key.Name))
							member.Append(new SchemaMember(key));
					}
				}
			}
			else
			{
				var field = origin.CreateField(member.Token.Property);

				//只有数据模式元素是导航子元素以及与当前语句的别名不同（相同则表示为同级），才需要指定字段引用的别名
				if(member.Parent != null && !string.Equals(member.Path, statement.Alias, StringComparison.OrdinalIgnoreCase))
				{
					if(string.IsNullOrEmpty(statement.Alias))
						field.Alias = member.FullPath;
					else
						field.Alias = Zongsoft.Common.StringExtension.TrimStart(member.FullPath, statement.Alias + ".", StringComparison.OrdinalIgnoreCase);
				}

				statement.Select.Members.Add(field);
			}

			if(member.HasChildren)
			{
				foreach(var child in member.Children)
				{
					GenerateSchema(aliaser, statement, origin, child);
				}
			}
		}
		#endregion
	}
}
