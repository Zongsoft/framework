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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Zongsoft.Data.Metadata;
using Zongsoft.Data.Features;

namespace Zongsoft.Data.Common.Expressions;

public class InsertStatementBuilder : IStatementBuilder<DataInsertContext>
{
	#region 构建方法
	public IAsyncEnumerable<IStatementBase> BuildAsync(DataInsertContext context, CancellationToken cancellation = default)
	{
		return this.BuildInsertsAsync(context, context.Entity, context.Data, null, context.Schema.Members, cancellation);
	}
	#endregion

	#region 私有方法
	private async IAsyncEnumerable<InsertStatement> BuildInsertsAsync(IDataMutateContext context, IDataEntity entity, object data, SchemaMember owner, IEnumerable<SchemaMember> schemas, [EnumeratorCancellation] CancellationToken cancellation = default)
	{
		if(data == null)
			throw new ArgumentNullException(nameof(data));

		var inherits = entity.GetInherits();
		var feature = context.Source.Features.Get<IReturningFeature>();
		var sequenceRetrievable = IsSequenceRetrievable(context);
		var recordCount = 0;

		if(owner == null)
		{
			recordCount = Utility.GetRecordCount(data, out var list);
			if(list != null)
				context.Data = list;
		}
		else
		{
			if(Utility.IsMultiple(data, out var items))
			{
				foreach(var item in items)
				{
					var value = owner.Token.GetValue(item);
					recordCount += Utility.GetRecordCount(value, out var list);

					if(list != null)
					{
						var current = item;
						owner.Token.SetValue(ref current, list);
					}
				}
			}
			else
			{
				var value = owner.Token.GetValue(data);
				recordCount = Utility.GetRecordCount(value, out var list);
				if(list != null)
					owner.Token.SetValue(ref data, list);
			}
		}

		if(recordCount < 1)
			yield break;

		foreach(var inherit in inherits)
		{
			var statement = this.CreateStatement(inherit, owner);

			if(context is DataInsertContextBase ctx)
				statement.Options.Apply(ctx.Options);

			foreach(var schema in schemas)
			{
				if(schema.Ignored)
					continue;

				if(!inherit.Properties.Contains(schema.Name))
					continue;

				if(schema.Token.Property.IsSimplex)
				{
					var simplex = (IDataEntitySimplexProperty)schema.Token.Property;

					if(simplex.Sequence != null && simplex.Sequence.IsBuiltin && sequenceRetrievable)
					{
						if(feature?.Supports(DataAccessMethod.Insert, ReturningKind.Newer) == true)
						{
							var field = statement.Table.CreateField(schema.Token);

							if(statement.Returning == null)
								statement.Returning = new ReturningClause(new ReturningClause.ReturningMember(field, ReturningKind.Newer));
							else
								statement.Returning.Append(field, ReturningKind.Newer);
						}
						else
						{
							statement.SequenceRetriever = new SelectStatement(owner?.FullPath);
							statement.SequenceRetriever.Select.Members.Add(SequenceExpression.Current(simplex.Sequence.Name, simplex.Name));
						}
					}
					else
					{
						//确认当前成员是否有提供的写入值
						var validation = await context.ValidateAsync(DataAccessMethod.Insert, simplex, cancellation).ConfigureAwait(false);
						var provided = validation.HasValue;
						var value = validation.Value;

						var field = statement.Table.CreateField(schema.Token);
						statement.Fields.Add(field);

						//var parameter = Utility.IsLinked(owner, simplex) ?
						//(
						//	provided ?
						//	Expression.Parameter(schema.Token.Property.Name, simplex.Type, value) :
						//	Expression.Parameter(schema.Token.Property.Name, simplex.Type)
						//) :
						//(
						//	provided ?
						//	Expression.Parameter(field, schema, value) :
						//	Expression.Parameter(field, schema)
						//);

						var parameter = provided ?
							Expression.Parameter(field, schema, value) :
							Expression.Parameter(field, schema);

						statement.Values.Add(parameter);
						statement.Parameters.Add(parameter);
					}
				}
				else
				{
					//不可变复合属性不支持任何写操作，即在新增操作中不能包含不可变复合属性
					if(schema.Token.Property.Immutable)
						throw new DataException(string.Format(Properties.Resources.InsertStatement_ImmutableProperty_Message, schema.FullPath));

					if(!schema.HasChildren)
						throw new DataException(string.Format(Properties.Resources.Statement_MissingComplexPropertyMembers_Message, schema.FullPath));

					var complex = (IDataEntityComplexProperty)schema.Token.Property;
					var slaves = this.BuildInsertsAsync(
						context,
						complex.Foreign,
						data,
						schema,
						schema.Children, cancellation);

					await foreach(var slave in slaves.ConfigureAwait(false))
					{
						slave.Schema = schema;
						statement.Slaves.Add(slave);
					}
				}
			}

			if(owner == null && context.Options.HasReturning(out var returning) && feature != null)
			{
				foreach(var column in returning.Columns)
				{
					if(!feature.Supports(DataAccessMethod.Insert, column.Kind))
						continue;

					statement.Returning ??= new();
					var field = statement.Table.CreateField((IDataEntitySimplexProperty)context.Entity.Properties[column.Name], column.Alias);
					statement.Returning.Append(field, column.Kind);
				}
			}

			if(statement.Fields.Count > 0)
			{
				for(int i = 1; i < recordCount; i++)
				{
					for(int j = 0; j < statement.Fields.Count; j++)
					{
						if(statement.Values[j] is ParameterExpression pe && pe.Schema != null)
						{
							var parameter = pe.Clone(ParameterExpression.ANONYMOUS);
							statement.Values.Add(parameter);
							statement.Parameters.Add(parameter);
						}
						else
							statement.Values.Add(statement.Values[j]);
					}
				}

				yield return statement;
			}
		}
	}

	private static bool IsSequenceRetrievable(IDataMutateContextBase context) => context is DataInsertContextBase ctx && ctx.Options.SequenceRetrievable;
	#endregion

	#region 虚拟方法
	protected virtual InsertStatement CreateStatement(IDataEntity entity, SchemaMember schema) => new(entity, schema);
	#endregion
}
