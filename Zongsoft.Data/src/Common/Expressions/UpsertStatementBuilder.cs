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
using System.Collections;
using System.Collections.Generic;

using Zongsoft.Data.Metadata;

namespace Zongsoft.Data.Common.Expressions;

public class UpsertStatementBuilder : IStatementBuilder<DataUpsertContext>
{
	#region 构建方法
	public IEnumerable<IStatementBase> Build(DataUpsertContext context)
	{
		return BuildUpserts(context, context.Entity, (context.IsMultiple ? null : context.Data), null, context.Schema.Members);
	}
	#endregion

	#region 私有方法
	internal static IEnumerable<UpsertStatement> BuildUpserts(IDataMutateContextBase context, IDataEntity entity, object data, SchemaMember owner, IEnumerable<SchemaMember> schemas)
	{
		var inherits = entity.GetInherits();
		var sequenceRetrieverSuppressed = IsSequenceRetrieverSuppressed(context);
		object container = null;

		if(data != null && owner != null)
		{
			/*
			 * 如果从当前容器数据中获取指定成员值失败，则：
			 * 1). 容器数据是集合类型，无法确定从集合中的哪个元素来获取指定成员的值，因此设置上下文数据为空；
			 * 2). 容器数据不是集合类型，则说明指定的成员可能有误或发生了内部错误，因此抛出异常。
			 */

			if(owner.Token.TryGetValue(data, null, out var value))
			{
				container = data;
				data = value;
			}
			else if(Zongsoft.Common.TypeExtension.IsEnumerable(data.GetType()))
				data = null;
			else
				throw new DataException($"Cannot get the specified '{owner.Name}' member from the '{data.GetType().FullName}' type.");
		}

		foreach(var inherit in inherits)
		{
			var statement = new UpsertStatement(inherit, owner);
			var sequences = new List<IDataEntitySimplexProperty>();

			if(context is DataUpsertContextBase ctx)
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

						/* 开始处理修改子句部分 */

						//注：可能因为非主键的唯一约束导致新增部分失败，
						//因此必须对含有外部序列字段的UPSERT语句，增加一个重新获取这些外部序列字段的附属查询语句。
						if(simplex.Sequence != null && !context.IsMultiple())
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

						statement.Updation.Add(new FieldValue(field, parameter));
					}
				}
				else
				{
					//不可变复合属性不支持任何写操作，即在增改操作中不能包含不可变复合属性
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
					foreach(var parameter in statement.Parameters)
						selection.Parameters.Add(parameter);

					selection.Where = conditions;
					statement.Slaves.Add(selection);
				}
			}

			if(statement.Fields.Count > 0)
			{
				var count = GetRecordCount(data ?? context.Data, out var list);

				if(list != null)
				{
					if(container != null)
						owner.Token.SetValue(ref container, list);
					else
						context.Data = list;
				}

				for(int i = 1; i < count; i++)
				{
					for(int j = 0; j < statement.Fields.Count; j++)
					{
						if(statement.Values[j] is ParameterExpression pe && pe.Schema != null)
						{
							var parameter = pe.Clone(ParameterExpression.Anonymous);
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

	private static int GetRecordCount(object data, out ICollection result)
	{
		if(data == null)
		{
			result = null;
			return 0;
		}

		if(data is ICollection collection)
		{
			result = null;
			return collection.Count;
		}

		if(data is IEnumerable enumerable)
		{
			var list = Utility.CreateList(data);

			var enumerator = enumerable.GetEnumerator();
			while(enumerator.MoveNext())
				list.Add(enumerator.Current);

			result = list;
			return list.Count;
		}

		result = null;
		return 0;
	}

	private static bool IsSequenceRetrieverSuppressed(IDataMutateContextBase context) => context is DataUpsertContextBase ctx && ctx.Options.SequenceRetrieverSuppressed;
	#endregion

	#region 虚拟方法
	protected virtual UpsertStatement CreateStatement(IDataEntity entity, SchemaMember schema) => new(entity, schema);
	#endregion
}
