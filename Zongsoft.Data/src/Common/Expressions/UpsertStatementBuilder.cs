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
using System.Collections.Generic;

using Zongsoft.Data.Metadata;

namespace Zongsoft.Data.Common.Expressions
{
	public class UpsertStatementBuilder : IStatementBuilder<DataUpsertContext>
	{
		#region 构建方法
		public IEnumerable<IStatementBase> Build(DataUpsertContext context)
		{
			return BuildUpserts(context, context.Entity, context.Data, null, context.Schema.Members);
		}
		#endregion

		#region 私有方法
		internal static IEnumerable<UpsertStatement> BuildUpserts(IDataMutateContextBase context, IDataEntity entity, object data, SchemaMember owner, IEnumerable<SchemaMember> schemas)
		{
			var inherits = entity.GetInherits();

			//更新当前操作的容器数据
			if(data != null && owner != null)
				data = owner.Token.GetValue(data);

			foreach(var inherit in inherits)
			{
				var statement = new UpsertStatement(inherit, owner);
				var sequences = new List<IDataEntitySimplexProperty>();

				foreach(var schema in schemas)
				{
					if(!inherit.Properties.Contains(schema.Name))
						continue;

					if(schema.Token.Property.IsSimplex)
					{
						var simplex = (IDataEntitySimplexProperty)schema.Token.Property;

						if(simplex.Sequence != null && simplex.Sequence.IsBuiltin)
						{
							statement.Sequence = new SelectStatement(owner?.FullPath);
							statement.Sequence.Select.Members.Add(SequenceExpression.Current(simplex.Sequence.Name, simplex.Name));
						}
						else
						{
							//确认当前成员是否有提供的写入值
							var provided = context.Validate(DataAccessMethod.Insert, simplex, out var value);

							var field = statement.Table.CreateField(schema.Token);
							statement.Fields.Add(field);

							var parameter = Utility.IsLinked(owner, simplex) ?
											(
												provided ?
												Expression.Parameter(schema.Token.Property.Name, simplex.Type, value) :
												Expression.Parameter(schema.Token.Property.Name, simplex.Type)
											) :
											(
												provided ?
												Expression.Parameter(field, schema, value) :
												Expression.Parameter(field, schema)
											);

							statement.Values.Add(parameter);
							statement.Parameters.Add(parameter);

							/* 开始处理修改子句部分 */

							//注：可能因为非主键的唯一约束导致新增部分失败，
							//因此必须对含有外部序列字段的UPSERT语句，增加一个重新获取这些外部序列字段的附属查询语句。
							if(simplex.Sequence != null)
								sequences.Add(simplex);

							//忽略不可变字段和序列字段
							if(simplex.Immutable || simplex.Sequence != null)
								continue;

							if(owner == null)
							{
								if(!Utility.IsGenerateRequired(ref data, schema.Name))
									continue;
							}
							else
							{
								//只有一对一(零)的导航属性才需要验证对应的字段值是否变更过，如果没有变更则忽略当前字段
								if(owner.Token.Property is IDataEntityComplexProperty complex &&
								   complex.Multiplicity != DataAssociationMultiplicity.Many &&
								   !Utility.IsGenerateRequired(ref data, schema.Name))
									continue;
							}

							//确认当前成员是否有提供的写入值
							if(context.Validate(DataAccessMethod.Update, simplex, out value))
							{
								parameter = Expression.Parameter(field, schema, value);
								statement.Parameters.Add(parameter);
							}

							//如果当前的数据成员类型为递增步长类型则生成递增表达式
							if(schema.Token.MemberType == typeof(Interval))
							{
								/*
								 * 注：默认参数类型为对应字段的类型，而该字段类型可能为无符号整数，
								 * 因此当参数类型为无符号整数并且步长为负数(递减)，则可能导致参数类型转换溢出，
								 * 所以必须将该参数类型重置为有符号整数（32位整数）。
								 */
								parameter.DbType = System.Data.DbType.Int32;

								//字段设置项的值为字段加参数的加法表达式
								statement.Updation.Add(new FieldValue(field, field.Add(parameter)));
							}
							else
							{
								statement.Updation.Add(new FieldValue(field, parameter));
							}
						}
					}
					else
					{
						//不可变复合属性不支持任何写操作，即在修改操作中不能包含不可变复合属性
						if(schema.Token.Property.Immutable)
							throw new DataException($"The '{schema.FullPath}' is an immutable complex(navigation) property and does not support the upsert operation.");

						if(!schema.HasChildren)
							throw new DataException($"Missing members that does not specify '{schema.FullPath}' complex property.");

						var complex = (IDataEntityComplexProperty)schema.Token.Property;
						var slaves = BuildUpserts(
							context,
							complex.Foreign,
							data,
							schema,
							schema.Children);

						foreach(var slave in slaves)
						{
							slave.Schema = schema;
							statement.Slaves.Add(slave);
						}
					}
				}

				//构建重新获取外部序列字段的附属查询语句
				if(sequences != null && sequences.Count > 0)
				{
					var selection = new SelectStatement(inherit, owner?.FullPath);

					for(int i = 0; i < sequences.Count; i++)
						selection.Select.Members.Add(selection.CreateField(sequences[i]));

					var conditions = ConditionExpression.And();

					foreach(var fieldSetter in statement.Updation)
					{
						conditions.Add(Expression.Equal(fieldSetter.Field, fieldSetter.Value));
					}

					if(conditions.Count > 0)
					{
						if(statement.HasParameters)
						{
							foreach(var parameter in statement.Parameters)
								selection.Parameters.Add(parameter);
						}

						selection.Where = conditions;
						statement.Slaves.Add(selection);
					}
				}

				if(statement.Fields.Count > 0)
					yield return statement;
			}
		}
		#endregion
	}
}
