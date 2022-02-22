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
using System.Collections.Generic;

using Zongsoft.Data.Metadata;
using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.Common
{
	public class DataSelectExecutor : IDataExecutor<SelectStatement>
	{
		#region 执行方法
		public bool Execute(IDataAccessContext context, SelectStatement statement)
		{
			switch(context)
			{
				case DataSelectContext selection:
					return this.OnExecute(selection, statement);
				case DataInsertContext insertion:
					return this.OnExecute(insertion, statement);
				case DataUpsertContext upsertion:
					return this.OnExecute(upsertion, statement);
				case DataIncrementContext increment:
					return this.OnExecute(increment, statement);
			}

			throw new DataException($"Data Engine Error: The '{this.GetType().Name}' executor does not support execution of '{context.GetType().Name}' context.");
		}

		protected virtual bool OnExecute(DataSelectContext context, SelectStatement statement)
		{
			//根据生成的脚本创建对应的数据命令
			var command = context.Session.Build(context, statement);
			context.Result = CreateResults(context.ModelType, context, statement, command, null);

			return false;
		}

		protected virtual bool OnExecute(DataInsertContext context, SelectStatement statement)
		{
			//根据生成的脚本创建对应的数据命令
			var command = context.Session.Build(context, statement);

			//绑定命令参数
			statement.Bind(context, command, context.Data);

			using(var reader = command.ExecuteReader())
			{
				if(reader.Read())
				{
					for(int i = 0; i < reader.FieldCount; i++)
					{
						var schema = string.IsNullOrEmpty(statement.Alias) ? context.Schema.Find(reader.GetName(i)) : context.Schema.Find(statement.Alias);

						if(schema != null)
						{
							if(schema.Token.Property.IsComplex && schema.Children.TryGet(reader.GetName(i), out var child))
								schema = child;

							schema.Token.SetValue(context.Data, reader.GetValue(i));
						}
					}
				}
			}

			return true;
		}

		protected virtual bool OnExecute(DataUpsertContext context, SelectStatement statement)
		{
			//根据生成的脚本创建对应的数据命令
			var command = context.Session.Build(context, statement);

			//绑定命令参数
			statement.Bind(context, command, context.Data);

			using(var reader = command.ExecuteReader())
			{
				if(reader.Read())
				{
					for(int i = 0; i < reader.FieldCount; i++)
					{
						var schema = string.IsNullOrEmpty(statement.Alias) ? context.Schema.Find(reader.GetName(i)) : context.Schema.Find(statement.Alias);

						if(schema != null)
						{
							if(schema.Token.Property.IsComplex && schema.Children.TryGet(reader.GetName(i), out var child))
								schema = child;

							schema.Token.SetValue(context.Data, reader.GetValue(i));
						}
					}
				}
			}

			return true;
		}

		protected virtual bool OnExecute(DataIncrementContext context, SelectStatement statement)
		{
			//根据生成的脚本创建对应的数据命令
			var command = context.Session.Build(context, statement);

			//执行命令
			context.Result = Zongsoft.Common.Convert.ConvertValue<long>(command.ExecuteScalar());

			return true;
		}
		#endregion

		#region 私有方法
		private static IEnumerable CreateResults(Type elementType, DataSelectContext context, SelectStatement statement, DbCommand command, Action<string, Paging> paginator)
		{
			return (IEnumerable)System.Activator.CreateInstance(
				typeof(LazyCollection<>).MakeGenericType(elementType),
				new object[]
				{
					context, statement, command, paginator
				});
		}
		#endregion

		#region 嵌套子类
		private class LazyCollection<T> : IEnumerable<T>, IEnumerable, IPageable
		{
			#region 公共事件
			public event EventHandler<PagingEventArgs> Paginated;
			#endregion

			#region 成员变量
			private readonly DbCommand _command;
			private readonly DataSelectContext _context;
			private readonly SelectStatement _statement;
			private readonly Action<string, Paging> _paginate;
			#endregion

			#region 构造函数
			public LazyCollection(DataSelectContext context, SelectStatement statement, DbCommand command, Action<string, Paging> paginate)
			{
				_context = context ?? throw new ArgumentNullException(nameof(context));
				_statement = statement ?? throw new ArgumentNullException(nameof(statement));
				_command = command ?? throw new ArgumentNullException(nameof(command));

				if(paginate != null)
					_paginate = paginate;
				else
					_paginate = (key, paging) => this.OnPaginated(key, paging);
			}
			#endregion

			#region 公共属性
			public bool Suppressed => Paging.IsDisabled(_context.Paging);
			#endregion

			#region 遍历迭代
			public IEnumerator<T> GetEnumerator()
			{
				return new LazyIterator(_context, _statement, _command.ExecuteReader(), _paginate);
			}

			IEnumerator IEnumerable.GetEnumerator()
			{
				return this.GetEnumerator();
			}
			#endregion

			#region 激发事件
			protected virtual void OnPaginated(string key, Paging paging)
			{
				this.Paginated?.Invoke(this, new PagingEventArgs(key, paging));
			}
			#endregion

			#region 数据迭代
			private class LazyIterator : IEnumerator<T>, IDisposable
			{
				#region 成员变量
				private IDataReader _reader;
				private Action<string, Paging> _paginate;
				private readonly IDataPopulator _populator;
				private readonly DataSelectContext _context;
				private readonly SelectStatement _statement;
				private readonly IDictionary<string, SlaveToken> _slaves;
				#endregion

				#region 构造函数
				public LazyIterator(DataSelectContext context, SelectStatement statement, IDataReader reader, Action<string, Paging> paginate)
				{
					var entity = context.Entity;

					if(!string.IsNullOrEmpty(statement.Alias))
					{
						var complex = (IDataEntityComplexProperty)context.Entity.Find(statement.Alias);

						if(complex.ForeignProperty == null || complex.ForeignProperty.IsSimplex)
							entity = complex.Foreign;
						else
							entity = ((IDataEntityComplexProperty)complex.ForeignProperty).Foreign;
					}

					_context = context;
					_statement = statement;
					_reader = reader;
					_paginate = paginate;
					_slaves = GetSlaves(_context, _statement, _reader);
					_populator = DataEnvironment.Populators.GetProvider(typeof(T)).GetPopulator(entity, typeof(T), _reader);
				}
				#endregion

				#region 公共成员
				public T Current
				{
					get
					{
						var entity = _populator.Populate(_reader);

						if(entity == null)
							return default;

						if(_statement.HasSlaves)
						{
							foreach(var slave in _statement.Slaves)
							{
								if(slave is SelectStatement selection && _slaves.TryGetValue(selection.Alias, out var token))
								{
									if(token.Schema.Token.MemberType == null)
										continue;

									object container = GetCurrentContainer(entity, token);

									if(container == null)
										continue;

									//生成子查询语句对应的命令
									var command = _context.Session.Build(_context, slave);

									foreach(var parameter in token.Parameters)
									{
										command.Parameters[parameter.Name].Value = _reader.GetValue(parameter.Ordinal);
									}

									//创建一个新的查询结果集
									var results = CreateResults(Zongsoft.Common.TypeExtension.GetElementType(token.Schema.Token.MemberType), _context, selection, command, _paginate);

									//如果要设置的目标成员类型是一个数组或者集合，则需要将动态的查询结果集转换为固定的列表
									if(Zongsoft.Common.TypeExtension.IsCollection(token.Schema.Token.MemberType))
									{
										var list = Activator.CreateInstance(
											typeof(List<>).MakeGenericType(Zongsoft.Common.TypeExtension.GetElementType(token.Schema.Token.MemberType)),
											new object[] { results });

										if(token.Schema.Token.MemberType.IsArray)
											results = (IEnumerable)list.GetType().GetMethod("ToArray").Invoke(list, Array.Empty<object>());
										else
											results = (IEnumerable)list;
									}

									token.Schema.Token.SetValue(container, results);
								}
							}
						}

						return (T)entity;
					}
				}

				public bool MoveNext()
				{
					if(_reader.Read())
						return true;

					this.Dispose();
					return false;
				}

				public void Reset()
				{
					throw new NotSupportedException();
				}
				#endregion

				#region 私有方法
				private static IDictionary<string, SlaveToken> GetSlaves(DataSelectContext context, IStatementBase statement, IDataReader reader)
				{
					static IEnumerable<ParameterToken> GetParameters(IDataReader reader, string path)
					{
						if(string.IsNullOrEmpty(path))
							yield break;

						for(int i = 0; i < reader.FieldCount; i++)
						{
							var name = reader.GetName(i);

							if(name.StartsWith("$" + path + ":"))
								yield return new ParameterToken(name.Substring(path.Length + 2), i);
						}
					}

					if(statement.HasSlaves)
					{
						var tokens = new Dictionary<string, SlaveToken>(statement.Slaves.Count);

						foreach(var slave in statement.Slaves)
						{
							if(slave is SelectStatementBase selection && !string.IsNullOrEmpty(selection.Alias))
							{
								var schema = context.Schema.Find(selection.Alias);

								if(schema != null)
									tokens.Add(selection.Alias, new SlaveToken(schema, GetParameters(reader, selection.Alias)));
							}
						}

						if(tokens.Count > 0)
							return tokens;
					}

					return null;
				}

				private static object GetCurrentContainer(object entity, SlaveToken token)
				{
					if(token.Schema.Parent == null || token.Schema.Parent.Token.IsMultiple)
						return entity;

					var stack = new Stack<SchemaMember>();
					var member = token.Schema.Parent;
					var container = entity;

					while(member != null)
					{
						stack.Push(member);
						member = member.Parent;
					}

					while(stack.TryPop(out member))
					{
						container = member.Token.GetValue(container);

						if(container == null)
							return null;
					}

					return container;
				}
				#endregion

				#region 显式实现
				object IEnumerator.Current
				{
					get => this.Current;
				}
				#endregion

				#region 处置方法
				public void Dispose()
				{
					var reader = System.Threading.Interlocked.Exchange(ref _reader, null);

					if(reader != null)
					{
						//处理分页的总记录数
						if(_statement.Paging != null && _statement.Paging.PageSize > 0)
						{
							if(reader.NextResult() && reader.Read())
							{
								_statement.Paging.TotalCount = (long)Convert.ChangeType(reader.GetValue(0), typeof(long));
								_paginate?.Invoke(_statement.Alias, _statement.Paging);
							}
						}

						reader.Dispose();
					}
				}
				#endregion
			}
			#endregion
		}

		private struct SlaveToken
		{
			public SchemaMember Schema;
			public IEnumerable<ParameterToken> Parameters;

			public SlaveToken(SchemaMember schema, IEnumerable<ParameterToken> parameters)
			{
				this.Schema = schema;
				this.Parameters = parameters;
			}
		}

		private struct ParameterToken
		{
			public readonly string Name;
			public readonly int Ordinal;

			public ParameterToken(string name, int ordinal)
			{
				this.Name = name;
				this.Ordinal = ordinal;
			}
		}
		#endregion
	}
}
