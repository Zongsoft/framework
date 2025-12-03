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

namespace Zongsoft.Data.Common.Expressions;

public class InsertStatementBuilder : IStatementBuilder<DataInsertContext>
{
	#region 构建方法
	public IEnumerable<IStatementBase> Build(DataInsertContext context)
	{
		return this.BuildInserts(context, context.Entity, null, context.Schema.Members);
	}
	#endregion

	#region 私有方法
	private IEnumerable<InsertStatement> BuildInserts(IDataMutateContext context, IDataEntity entity, SchemaMember owner, IEnumerable<SchemaMember> schemas)
	{
		var inherits = entity.GetInherits();
		var sequenceRetrieverSuppressed = IsSequenceRetrieverSuppressed(context);

		foreach(var inherit in inherits)
		{
			var statement = this.CreateStatement(inherit, owner);

			if(context is DataInsertContextBase ctx)
				statement.Options.Apply(ctx.Options);

			foreach(var schema in schemas)
			{
				if(!inherit.Properties.Contains(schema.Name))
					continue;

				if(schema.Token.Property.IsSimplex)
				{
					var simplex = (IDataEntitySimplexProperty)schema.Token.Property;

					if(simplex.Sequence != null && simplex.Sequence.IsBuiltin && !sequenceRetrieverSuppressed)
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
					}
				}
				else
				{
					if(!schema.HasChildren)
						throw new DataException($"Missing members that does not specify '{schema.FullPath}' complex property.");

					//不可变复合属性不支持任何写操作，即在新增操作中不能包含不可变复合属性
					if(schema.Token.Property.Immutable)
						throw new DataException($"The '{schema.FullPath}' is an immutable complex(navigation) property and does not support the insert operation.");

					var complex = (IDataEntityComplexProperty)schema.Token.Property;

					//注：在构建一对多的导航属性的UPSERT语句时不能指定容器数据(即data参数值为空)，因为批量操作语句不支持在构建阶段绑定到具体数据
					var upserts = UpsertStatementBuilder.BuildUpserts(context, complex.Foreign, (context.IsMultiple() ? null : context.Data), schema, schema.Children);

					//将新建的语句加入到主语句的从属集中
					foreach(var upsert in upserts)
					{
						statement.Slaves.Add(upsert);
					}

					//var slaves = BuildInserts(context, complex.Foreign, schema, schema.Children);
					//foreach(var slave in slaves)
					//{
					//	slave.Schema = schema;
					//	statement.Slaves.Add(slave);
					//}
				}
			}

			if(statement.Fields.Count > 0)
				yield return statement;
		}
	}

	private static bool IsSequenceRetrieverSuppressed(IDataMutateContextBase context) => context is DataInsertContextBase ctx && ctx.Options.SequenceRetrieverSuppressed;
	#endregion

	#region 虚拟方法
	protected virtual InsertStatement CreateStatement(IDataEntity entity, SchemaMember schema) => new(entity, schema);
	#endregion
}
