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
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;

using Zongsoft.Data.Metadata;
using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.Common
{
	public abstract class DataMutateExecutor<TStatement> : IDataExecutor<TStatement> where TStatement : IMutateStatement
	{
		#region 同步执行
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
			var command = context.Session.Build(context, statement);

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
					//获取当前一对多导航属性的链接成员标记
					var tokens = GetLinkTokens(data, statement.Schema);

					foreach(var item in (IEnumerable)context.Data)
					{
						//更新当前操作数据
						context.Data = item;

						if(tokens != null && tokens.Length > 0)
						{
							var current = item;

							//依次同步当前集合元素中的导航属性值
							for(int i = 0; i < tokens.Length; i++)
								tokens[i].SetForeignValue(ref current);

							context.Data = current;
						}

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

		#region 异步执行
		public Task<bool> ExecuteAsync(IDataAccessContext context, TStatement statement, CancellationToken cancellation)
		{
			if(context is IDataMutateContext ctx)
				return this.OnExecuteAsync(ctx, statement, cancellation);

			throw new DataException($"Data Engine Error: The '{this.GetType().Name}' executor does not support execution of '{context.GetType().Name}' context.");
		}

		protected virtual async Task<bool> OnExecuteAsync(IDataMutateContext context, TStatement statement, CancellationToken cancellation)
		{
			if(context.Method != DataAccessMethod.Insert && context.Entity.Immutable)
				throw new DataException($"The '{context.Entity.Name}' is an immutable entity and does not support {context.Method} operation.");

			//根据生成的脚本创建对应的数据命令
			var command = context.Session.Build(context, statement);

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
					//获取当前一对多导航属性的链接成员标记
					var tokens = GetLinkTokens(data, statement.Schema);

					foreach(var item in (IEnumerable)context.Data)
					{
						//更新当前操作数据
						context.Data = item;

						if(tokens != null && tokens.Length > 0)
						{
							var current = item;

							//依次同步当前集合元素中的导航属性值
							for(int i = 0; i < tokens.Length; i++)
								tokens[i].SetForeignValue(ref current);

							context.Data = current;
						}

						var continued = await this.MutateAsync(context, statement, command, cancellation);

						if(continued && statement.HasSlaves)
						{
							foreach(var slave in statement.Slaves)
								await context.Provider.Executor.ExecuteAsync(context, slave, cancellation);
						}
					}

					return false;
				}
				else
				{
					return await this.MutateAsync(context, statement, command, cancellation);
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
		protected virtual bool OnMutated(IDataMutateContext context, TStatement statement, DbDataReader reader)
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

		protected virtual async Task<bool> OnMutatedAsync(IDataMutateContext context, TStatement statement, DbDataReader reader, CancellationToken cancellation)
		{
			if(context is DataIncrementContextBase increment)
			{
				if(await reader.ReadAsync(cancellation))
					increment.Result = await reader.IsDBNullAsync(0, cancellation) ? 0 : (long)Convert.ChangeType(reader.GetValue(0), TypeCode.Int64);
				else
					return false;
			}

			return true;
		}

		protected virtual bool OnMutated(IDataMutateContext context, TStatement statement, int count) => count > 0;
		protected virtual Task<bool> OnMutatedAsync(IDataMutateContext context, TStatement statement, int count, CancellationToken cancellation) => Task.FromResult(count > 0);

		protected virtual void OnMutating(IDataMutateContext context, TStatement statement) { }
		protected virtual Task OnMutatingAsync(IDataMutateContext context, TStatement statement, CancellationToken cancellation) => Task.CompletedTask;
		#endregion

		#region 私有方法
		private bool Mutate(IDataMutateContext context, TStatement statement, DbCommand command)
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
				this.Bind(context.Data, statement.Slaves);

			return continued;
		}

		private async Task<bool> MutateAsync(IDataMutateContext context, TStatement statement, DbCommand command, CancellationToken cancellation)
		{
			bool continued;

			//调用写入操作开始方法
			this.OnMutating(context, statement);

			//绑定命令参数
			statement.Bind(context, command, context.Data);

			if(statement.Returning != null && statement.Returning.Table == null)
			{
				using(var reader = await command.ExecuteReaderAsync(cancellation))
				{
					//调用写入操作完成方法
					continued = await this.OnMutatedAsync(context, statement, reader, cancellation);
				}
			}
			else
			{
				//执行数据命令操作
				var count = await command.ExecuteNonQueryAsync(cancellation);

				//累加总受影响的记录数
				context.Count += count;

				//调用写入操作完成方法
				continued = this.OnMutated(context, statement, count);
			}

			//如果需要继续并且有从属语句则尝试绑定从属写操作数据
			if(continued && statement.HasSlaves)
				this.Bind(context.Data, statement.Slaves);

			return continued;
		}

		private void Bind(object data, IEnumerable<IStatementBase> statements)
		{
			if(data == null)
				return;

			foreach(var statement in statements)
			{
				if(statement is IMutateStatement mutation && mutation.Schema != null)
				{
					//设置子新增语句中的关联参数值
					this.SetLinkedParameters(mutation, data);
				}
			}
		}

		private void SetLinkedParameters(IMutateStatement statement, object data)
		{
			if(statement.Schema == null || statement.Schema.Token.Property.IsSimplex)
				return;

			var complex = (IDataEntityComplexProperty)statement.Schema.Token.Property;
			UpdateStatement updation = null;

			for(int i = 0; i < complex.Links.Length; i++)
			{
				var link = complex.Links[i];

				if(!statement.HasParameters || !statement.Parameters.TryGetValue(link.ForeignKey.Name, out var parameter))
					continue;

				if(link.ForeignKey.Sequence == null)
				{
					object refer = data;

					foreach(var anchor in link.GetAnchors())
					{
						if(Utility.TryGetMemberValue(ref refer, anchor.Name, out var value))
							refer = value;
					}

					parameter.Value = refer;
				}
				else if(statement.Schema.HasChildren && statement.Schema.Children.TryGetValue(link.ForeignKey.Name, out var member))
				{
					/*
					 * 如果复合属性的外链字段含序号器(自增)，链接参数值不能直接绑定必须通过执行器动态绑定
					 * 如果当前语句为新增或增改并且含有主键，则在该语句执行之后由其从属语句再更新对应的外链字段的序号器(自增)值
					 */

					parameter.Schema = member;

					if(complex.Entity.Key.Length > 0 && (statement is InsertStatement || statement is UpsertStatement))
					{
						if(updation == null)
						{
							updation = new UpdateStatement(complex.Entity);
							statement.Slaves.Add(updation);

							foreach(var key in complex.Entity.Key)
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

						var field = updation.Table.CreateField(link.GetAnchors()[0]);
						var fieldValue = Expression.Parameter(field, new SchemaMember(member.Token));
						updation.Fields.Add(new FieldValue(field, fieldValue));
						updation.Parameters.Add(fieldValue);
					}
				}
			}
		}

		private LinkToken[] GetLinkTokens(object data, SchemaMember member)
		{
			if(member == null || member.Token.Property.IsSimplex)
				return Array.Empty<LinkToken>();

			var complex = (IDataEntityComplexProperty)member.Token.Property;
			var tokens = new LinkToken[complex.Links.Length];

			for(int i = 0; i < complex.Links.Length; i++)
			{
				var link = complex.Links[i];
				var anchors = link.GetAnchors();

				if(anchors.Length > 1)
					throw new DataException($"The '{member.FullPath}' multi-level link anchors are not supported in mutate operation.");

				if(Utility.TryGetMemberValue(ref data, anchors[0].Name, out var value))
					tokens[i] = new LinkToken(link.ForeignKey, value);
				else
					tokens[i] = new LinkToken(link.ForeignKey, null);
			}

			return tokens;
		}
		#endregion

		#region 私有结构
		private readonly struct LinkToken
		{
			public LinkToken(IDataEntitySimplexProperty foreign, object value)
			{
				this.ForeignProperty = foreign;
				this.PrincipalValue = value;
			}

			public readonly object PrincipalValue;
			public readonly IDataEntitySimplexProperty ForeignProperty;

			public void SetForeignValue(ref object data)
			{
				if(data == null)
					return;

				if(this.PrincipalValue == null && !this.ForeignProperty.Nullable)
					return;

				Utility.TrySetMemberValue(ref data, this.ForeignProperty.Name, this.PrincipalValue);
			}
		}
		#endregion
	}
}
