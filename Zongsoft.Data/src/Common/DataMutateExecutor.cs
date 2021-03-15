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
using System.Collections;
using System.Collections.Generic;

using Zongsoft.Data.Metadata;
using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.Common
{
	public abstract class DataMutateExecutor<TStatement> : IDataExecutor<TStatement> where TStatement : IMutateStatement
	{
		#region 执行方法
		public bool Execute(IDataAccessContext context, TStatement statement)
		{
			if(context is IDataMutateContext ctx)
				return this.OnExecute(ctx, statement);

			throw new DataException($"Data Engine Error: The '{this.GetType().Name}' executor does not support execution of '{context.GetType().Name}' context.");
		}

		protected virtual bool OnExecute(IDataMutateContext context, TStatement statement)
		{
			if(context.Method != DataAccessMethod.Insert && context.Entity.Immutable)
				throw new DataException($"The '{context.Entity.Name}' is an immutable entity and does not support {context.Method} operation.");

			//根据生成的脚本创建对应的数据命令
			var command = context.Session.Build(statement);

			//获取当前操作是否为多数据
			var isMultiple = context.IsMultiple;

			//保存当前上下文的数据
			var data = context.Data;

			if(statement.Schema != null)
			{
				isMultiple = statement.Schema.Token.IsMultiple;
				context.Data = statement.Schema.Token.GetValue(context.Data);

				if(context.Data == null)
				{
					context.Data = data;
					return false;
				}
			}

			try
			{
				if(isMultiple)
				{
					foreach(var item in (IEnumerable)context.Data)
					{
						//更新当前操作数据
						context.Data = item;

						var continued = this.Mutate(context, statement, command);

						if(continued && statement.HasSlaves)
						{
							foreach(var slave in statement.Slaves)
								context.Provider.Executor.Execute(context, slave);
						}
					}

					return false;
				}
				else
				{
					return this.Mutate(context, statement, command);
				}
			}
			finally
			{
				//还原当前上下文的数据
				context.Data = data;
			}
		}
		#endregion

		#region 虚拟方法
		protected virtual bool OnMutated(IDataMutateContext context, TStatement statement, System.Data.IDataReader reader)
		{
			if(context is DataIncrementContextBase increment)
			{
				if(reader.Read())
					increment.Result = reader.IsDBNull(0) ? 0 : (long)Convert.ChangeType(reader.GetValue(0), TypeCode.Int64);
				else
					return false;
			}

			return true;
		}

		protected virtual bool OnMutated(IDataMutateContext context, TStatement statement, int count)
		{
			return count > 0;
		}

		protected virtual void OnMutating(IDataMutateContext context, TStatement statement)
		{
		}
		#endregion

		#region 私有方法
		private bool Mutate(IDataMutateContext context, TStatement statement, System.Data.Common.DbCommand command)
		{
			bool continued;

			//调用写入操作开始方法
			this.OnMutating(context, statement);

			//绑定命令参数
			statement.Bind(context, command, context.Data);

			if(statement.Returning != null && statement.Returning.Table == null)
			{
				using(var reader = command.ExecuteReader())
				{
					//调用写入操作完成方法
					continued = this.OnMutated(context, statement, reader);
				}
			}
			else
			{
				//执行数据命令操作
				var count = command.ExecuteNonQuery();

				//累加总受影响的记录数
				context.Count += count;

				//调用写入操作完成方法
				continued = this.OnMutated(context, statement, count);
			}

			//如果需要继续并且有从属语句则尝试绑定从属写操作数据
			if(continued && statement.HasSlaves)
				this.Bind(context, statement.Slaves);

			return continued;
		}

		private void Bind(IDataMutateContext context, IEnumerable<IStatementBase> statements)
		{
			if(context.Data == null)
				return;

			foreach(var statement in statements)
			{
				if(statement is IMutateStatement mutation && mutation.Schema != null)
				{
					//设置子新增语句中的关联参数值
					this.SetLinkedParameters(mutation, context.Data);
				}
			}
		}

		private void SetLinkedParameters(IMutateStatement statement, object data)
		{
			if(statement.Schema == null || statement.Schema.Token.Property.IsSimplex)
				return;

			var complex = (IDataEntityComplexProperty)statement.Schema.Token.Property;
			UpdateStatement updation = null;

			foreach(var link in complex.Links)
			{
				if(!statement.HasParameters || !statement.Parameters.TryGet(link.Foreign.Name, out var parameter))
					continue;

				if(link.Foreign.Sequence == null)
				{
					if(Utility.TryGetMemberValue(ref data, link.Principal.Name, out var value))
						parameter.Value = value;
				}
				else if(statement.Schema.HasChildren && statement.Schema.Children.TryGet(link.Foreign.Name, out var member))
				{
					/*
					 * 如果复合属性的外链字段含序号器(自增)，链接参数值不能直接绑定必须通过执行器动态绑定
					 * 如果当前语句为新增或增改并且含有主键，则在该语句执行之后由其从属语句再更新对应的外链字段的序号器(自增)值
					 */

					parameter.Schema = member;

					if(link.Principal.Entity.Key.Length > 0 && (statement is InsertStatement || statement is UpsertStatement))
					{
						if(updation == null)
						{
							updation = new UpdateStatement(link.Principal.Entity);
							statement.Slaves.Add(updation);

							foreach(var key in link.Principal.Entity.Key)
							{
								var equals = Expression.Equal(
									updation.Table.CreateField(key),
									Expression.Constant(Utility.GetMemberValue(ref data, key.Name) ?? Utility.GetDefaultValue(key.Type, key.Nullable)));

								if(updation.Where == null)
									updation.Where = equals;
								else
									updation.Where = Expression.AndAlso(updation.Where, equals);
							}
						}

						var field = updation.Table.CreateField(link.Principal);
						var fieldValue = Expression.Parameter(field, new SchemaMember(member.Token));
						updation.Fields.Add(new FieldValue(field, fieldValue));
						updation.Parameters.Add(fieldValue);
					}
				}
			}
		}
		#endregion
	}
}
