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

namespace Zongsoft.Data.Common;

public class DataSelectExecutor : IDataExecutor<SelectStatement>
{
	#region 同步执行
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
		}

		throw new DataException($"Data Engine Error: The '{this.GetType().Name}' executor does not support execution of '{context.GetType().Name}' context.");
	}

	protected virtual bool OnExecute(DataSelectContext context, SelectStatement statement)
	{
		//根据生成的脚本创建对应的数据命令
		var command = context.Session.Build(context, statement);

		//处理语句的插槽替换运算
		if(context.Source.Driver is DataDriverBase driver)
			driver.Slotter?.Evaluate(context, statement, command);

		context.Result = CreateResults(context.ModelType, context, statement, command, 0, context.Paging);
		return false;
	}

	protected virtual bool OnExecute(DataInsertContext context, SelectStatement statement)
	{
		//根据生成的脚本创建对应的数据命令
		var command = context.Session.Build(context, statement);

		//处理语句的插槽替换运算
		if(context.Source.Driver is DataDriverBase driver)
			driver.Slotter?.Evaluate(context, statement, command);

		//绑定命令参数
		statement.Bind(context, command, context.IsMultiple());

		using(var reader = command.ExecuteReader())
		{
			if(reader.Read())
			{
				for(int i = 0; i < reader.FieldCount; i++)
				{
					var schema = string.IsNullOrEmpty(statement.Alias) ? context.Schema.Find(reader.GetName(i)) : context.Schema.Find(statement.Alias);

					if(schema != null)
					{
						if(schema.Token.Property.IsComplex && schema.Children.TryGetValue(reader.GetName(i), out var child))
							schema = child;

						var data = context.Data;
						schema.Token.SetValue(ref data, reader.GetValue(i));
						context.Data = data;
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

		//处理语句的插槽替换运算
		if(context.Source.Driver is DataDriverBase driver)
			driver.Slotter?.Evaluate(context, statement, command);

		//绑定命令参数
		statement.Bind(context, command, context.IsMultiple());

		using(var reader = command.ExecuteReader())
		{
			if(reader.Read())
			{
				for(int i = 0; i < reader.FieldCount; i++)
				{
					var schema = string.IsNullOrEmpty(statement.Alias) ? context.Schema.Find(reader.GetName(i)) : context.Schema.Find(statement.Alias);

					if(schema != null)
					{
						if(schema.Token.Property.IsComplex && schema.Children.TryGetValue(reader.GetName(i), out var child))
							schema = child;

						var data = context.Data;
						schema.Token.SetValue(ref data, reader.GetValue(i));
						context.Data = data;
					}
				}
			}
		}

		return true;
	}
	#endregion

	#region 异步执行
	public ValueTask<bool> ExecuteAsync(IDataAccessContext context, SelectStatement statement, CancellationToken cancellation)
	{
		switch(context)
		{
			case DataSelectContext selection:
				return this.OnExecuteAsync(selection, statement, cancellation);
			case DataInsertContext insertion:
				return this.OnExecuteAsync(insertion, statement, cancellation);
			case DataUpsertContext upsertion:
				return this.OnExecuteAsync(upsertion, statement, cancellation);
		}

		throw new DataException($"Data Engine Error: The '{this.GetType().Name}' executor does not support execution of '{context.GetType().Name}' context.");
	}

	protected virtual ValueTask<bool> OnExecuteAsync(DataSelectContext context, SelectStatement statement, CancellationToken cancellation)
	{
		//根据生成的脚本创建对应的数据命令
		var command = context.Session.Build(context, statement);

		//处理语句的插槽替换运算
		if(context.Source.Driver is DataDriverBase driver)
			driver.Slotter?.Evaluate(context, statement, command);

		context.Result = CreateResults(context.ModelType, context, statement, command, 0, context.Paging);
		return ValueTask.FromResult(false);
	}

	protected virtual async ValueTask<bool> OnExecuteAsync(DataInsertContext context, SelectStatement statement, CancellationToken cancellation)
	{
		//根据生成的脚本创建对应的数据命令
		var command = context.Session.Build(context, statement);

		//处理语句的插槽替换运算
		if(context.Source.Driver is DataDriverBase driver)
			driver.Slotter?.Evaluate(context, statement, command);

		//绑定命令参数
		await statement.BindAsync(context, command, context.IsMultiple(), cancellation);

		using(var reader = await command.ExecuteReaderAsync(cancellation))
		{
			if(await reader.ReadAsync(cancellation))
			{
				for(int i = 0; i < reader.FieldCount; i++)
				{
					var schema = string.IsNullOrEmpty(statement.Alias) ? context.Schema.Find(reader.GetName(i)) : context.Schema.Find(statement.Alias);

					if(schema != null)
					{
						if(schema.Token.Property.IsComplex && schema.Children.TryGetValue(reader.GetName(i), out var child))
							schema = child;

						var data = context.Data;
						schema.Token.SetValue(ref data, reader.GetValue(i));
						context.Data = data;
					}
				}
			}
		}

		return true;
	}

	protected virtual async ValueTask<bool> OnExecuteAsync(DataUpsertContext context, SelectStatement statement, CancellationToken cancellation)
	{
		//根据生成的脚本创建对应的数据命令
		var command = context.Session.Build(context, statement);

		//处理语句的插槽替换运算
		if(context.Source.Driver is DataDriverBase driver)
			driver.Slotter?.Evaluate(context, statement, command);

		//绑定命令参数
		await statement.BindAsync(context, command, context.IsMultiple(), cancellation);

		using(var reader = await command.ExecuteReaderAsync(cancellation))
		{
			if(await reader.ReadAsync(cancellation))
			{
				for(int i = 0; i < reader.FieldCount; i++)
				{
					var schema = string.IsNullOrEmpty(statement.Alias) ? context.Schema.Find(reader.GetName(i)) : context.Schema.Find(statement.Alias);

					if(schema != null)
					{
						if(schema.Token.Property.IsComplex && schema.Children.TryGetValue(reader.GetName(i), out var child))
							schema = child;

						var data = context.Data;
						schema.Token.SetValue(ref data, reader.GetValue(i));
						context.Data = data;
					}
				}
			}
		}

		return true;
	}
	#endregion

	#region 私有方法
	private static IEnumerable CreateResults(Type elementType, DataSelectContext context, SelectStatement statement, DbCommand command, int skip, Paging paging = null)
	{
		return (IEnumerable)System.Activator.CreateInstance(
			typeof(LazyCollection<>).MakeGenericType(elementType),
			[ context, statement, command, skip, paging ]);
	}
	#endregion

	#region 嵌套子类
	private class LazyCollection<T> : IPageable, IAsyncEnumerable<T>, IEnumerable<T>, IEnumerable
	{
		#region 事件定义
		public event EventHandler<PagingEventArgs> Paginated;
		#endregion

		#region 成员变量
		private readonly int _skip;
		private readonly Paging _paging;
		private readonly DbCommand _command;
		private readonly DataSelectContext _context;
		private readonly SelectStatement _statement;
		#endregion

		#region 构造函数
		public LazyCollection(DataSelectContext context, SelectStatement statement, DbCommand command, int skip, Paging paging)
		{
			_skip = skip;
			_context = context ?? throw new ArgumentNullException(nameof(context));
			_statement = statement ?? throw new ArgumentNullException(nameof(statement));
			_command = command ?? throw new ArgumentNullException(nameof(command));
			_paging = paging;
		}
		#endregion

		#region 公共属性
		public bool Suppressed => Paging.IsDisabled(_context.Paging);
		#endregion

		#region 遍历迭代
		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		public IEnumerator<T> GetEnumerator()
		{
			var reader = _command.ExecuteReader();

			//如果启用了分页，则先获取分页信息
			if(_paging != null && _paging.Enabled)
			{
				//首先执行分页查询
				if(reader.Read())
					_paging.TotalCount = (long)Convert.ChangeType(reader.GetValue(0), typeof(long));

				//将读取器移到数据查询
				reader.NextResult();

				//激发分页完成事件
				this.Paginated?.Invoke(this, new PagingEventArgs(_context.Name, _paging));
			}

			return new LazyIterator(_context, _statement, reader, _skip);
		}

		public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellation)
		{
			var reader = await _command.ExecuteReaderAsync(cancellation);

			//如果启用了分页，则先获取分页信息
			if(_paging != null && _paging.Enabled)
			{
				//首先执行分页查询
				if(await reader.ReadAsync(cancellation))
					_paging.TotalCount = (long)Convert.ChangeType(reader.GetValue(0), typeof(long));

				//将读取器移到数据查询
				await reader.NextResultAsync(cancellation);

				//激发分页完成事件
				this.Paginated?.Invoke(this, new PagingEventArgs(_context.Name, _paging));
			}

			await using var iterator = new LazyIterator(_context, _statement, reader, _skip);

			while(await iterator.MoveNextAsync())
				yield return iterator.Current;
		}
		#endregion

		#region 数据迭代
		private class LazyIterator : IEnumerator<T>, IAsyncEnumerator<T>
		{
			#region 成员变量
			private DbDataReader _reader;
			private readonly int _skip;
			private readonly IDataPopulator _populator;
			private readonly DataSelectContext _context;
			private readonly SelectStatement _statement;
			private readonly IDictionary<string, SlaveToken> _slaves;
			#endregion

			#region 构造函数
			public LazyIterator(DataSelectContext context, SelectStatement statement, DbDataReader reader, int skip)
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
				_skip = skip;
				_slaves = GetSlaves(_context, _statement, _reader);

				if(Zongsoft.Common.TypeExtension.IsNullable(typeof(T), out var underlyingType))
					_populator = DataEnvironment.Populators.GetPopulator(context.Source.Driver, underlyingType, _reader, entity);
				else
					_populator = DataEnvironment.Populators.GetPopulator(context.Source.Driver, typeof(T), _reader, entity);
			}
			#endregion

			#region 公共成员
			public T Current
			{
				get
				{
					var model = _populator.Populate<T>(_reader);

					if(model == null)
						return default;

					if(_statement.HasSlaves)
						this.PopulateSlaves(model);

					return model;
				}
			}

			public bool MoveNext()
			{
				if(_reader.Read())
					return true;

				this.Dispose();
				return false;
			}

			public async ValueTask<bool> MoveNextAsync()
			{
				if(await _reader.ReadAsync())
					return true;

				await this.DisposeAsync();
				return false;
			}
			#endregion

			#region 私有方法
			private static Dictionary<string, SlaveToken> GetSlaves(DataSelectContext context, IStatementBase statement, IDataReader reader)
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

			private void PopulateSlaves(T model)
			{
				foreach(var slave in _statement.Slaves)
				{
					if(slave is SelectStatement selection && _slaves.TryGetValue(selection.Alias, out var token))
					{
						if(token.Schema.Token.MemberType == null)
							continue;

						object container = GetCurrentContainer(model, token, _skip);

						if(container == null)
							continue;

						//生成子查询语句对应的命令
						var command = _context.Session.Build(_context, slave);

						foreach(var parameter in token.Parameters)
						{
							command.Parameters[parameter.Name].Value = _reader.GetValue(parameter.Ordinal);
						}

						//创建一个新的查询结果集
						var results = CreateResults(Zongsoft.Common.TypeExtension.GetElementType(token.Schema.Token.MemberType), _context, selection, command, _skip + 1);

						//如果要设置的目标成员类型是一个数组或者集合，则需要将动态的查询结果集转换为固定的列表
						if(Zongsoft.Common.TypeExtension.IsCollection(token.Schema.Token.MemberType))
						{
							var list = Activator.CreateInstance(
								typeof(List<>).MakeGenericType(Zongsoft.Common.TypeExtension.GetElementType(token.Schema.Token.MemberType)),
								[results]);

							if(token.Schema.Token.MemberType.IsArray)
								results = (IEnumerable)list.GetType().GetMethod("ToArray").Invoke(list, Array.Empty<object>());
							else
								results = (IEnumerable)list;
						}

						token.Schema.Token.SetValue(ref container, results);
					}
				}
			}

			private static object GetCurrentContainer(object model, SlaveToken token, int skipCount)
			{
				if(token.Schema.Parent == null || token.Schema.Parent.Token.IsMultiple)
					return model;

				var stack = new Stack<SchemaMember>();
				var member = token.Schema.Parent;
				var container = model;

				while(member != null)
				{
					stack.Push(member);
					member = member.Parent;
				}

				int skipped = 0;

				while(stack.TryPop(out member))
				{
					if(skipped++ < skipCount)
						continue;

					container = member.Token.GetValue(container);

					if(container == null)
						return null;
				}

				return container;
			}
			#endregion

			#region 显式实现
			object IEnumerator.Current => this.Current;
			void IEnumerator.Reset() => throw new NotSupportedException();
			#endregion

			#region 处置方法
			public void Dispose()
			{
				var reader = Interlocked.Exchange(ref _reader, null);

				if(reader != null)
					reader.Dispose();
			}

			public async ValueTask DisposeAsync()
			{
				var reader = Interlocked.Exchange(ref _reader, null);

				if(reader != null)
					await reader.DisposeAsync();
			}
			#endregion
		}
		#endregion
	}

	private readonly struct SlaveToken(SchemaMember schema, IEnumerable<DataSelectExecutor.ParameterToken> parameters)
	{
		public readonly SchemaMember Schema = schema;
		public readonly IEnumerable<ParameterToken> Parameters = parameters;
	}

	private readonly struct ParameterToken(string name, int ordinal)
	{
		public readonly string Name = name;
		public readonly int Ordinal = ordinal;
	}
	#endregion
}
