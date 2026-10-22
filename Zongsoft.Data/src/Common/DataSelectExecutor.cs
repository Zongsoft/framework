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
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
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
			case DataUpdateContext updation:
				return this.OnExecuteAsync(updation, statement, cancellation);
		}

		throw new DataException(string.Format(Properties.Resources.DataExecutor_UnsupportedContext_Message, this.GetType().Name, context.GetType().Name));
	}

	protected virtual ValueTask<bool> OnExecuteAsync(DataSelectContext context, SelectStatement statement, CancellationToken cancellation)
	{
		context.Result = CreateResults(context.ModelType, context, statement, 0, context.Paging);
		return ValueTask.FromResult(false);
	}

	protected virtual async ValueTask<bool> OnExecuteAsync(DataInsertContext context, SelectStatement statement, CancellationToken cancellation)
	{
		//根据生成的脚本创建对应的数据命令
		await using var command = context.Session.Build(context, statement);

		//处理语句的插槽替换运算
		StatementSlotter.Evaluate(context, statement, command);

		//绑定命令参数
		await statement.BindAsync(context, command, cancellation);

		await using(var reader = await command.ExecuteReaderAsync(cancellation))
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
						SetSequenceValue(data, schema, reader.GetValue(i));
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
		await using var command = context.Session.Build(context, statement);

		//处理语句的插槽替换运算
		StatementSlotter.Evaluate(context, statement, command);

		//绑定命令参数
		await statement.BindAsync(context, command, cancellation);

		await using(var reader = await command.ExecuteReaderAsync(cancellation))
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
						SetSequenceValue(data, schema, reader.GetValue(i));
						context.Data = data;
					}
				}
			}
		}

		return true;
	}

	protected virtual async ValueTask<bool> OnExecuteAsync(DataUpdateContext context, SelectStatement statement, CancellationToken cancellation)
	{
		await using var command = context.Session.Build(context, statement);
		StatementSlotter.Evaluate(context, statement, command);
		await statement.BindAsync(context, command, cancellation);

		await using(var reader = await command.ExecuteReaderAsync(cancellation))
		{
			if(context.Options.HasReturning(out var returning))
			{
				while(await reader.ReadAsync(cancellation))
					returning.Rows.Populate(reader);
			}
		}

		return true;
	}
	#endregion

	#region 私有方法
	private static void SetSequenceValue(object data, SchemaMember member, object value)
	{
		if(data == null || value == null || Convert.IsDBNull(value))
			return;

		if(Utility.IsMultiple(data, out var enumerable))
		{
			if(member.Token.Property.IsSimplex(out var simplex) && simplex.Sequence.IsBuiltin)
			{
				var id = Zongsoft.Common.Convert.ConvertValue<long>(value);

				foreach(var item in enumerable)
				{
					var current = item;
					member.Token.SetValue(ref current, id++);
				}
			}
			else
			{
				foreach(var item in enumerable)
				{
					var current = item;
					member.Token.SetValue(ref current, value);
				}
			}
		}
		else
			member.Token.SetValue(ref data, value);
	}

	private static object CreateResults(Type elementType, DataSelectContext context, SelectStatement statement, int skip, Paging paging = null, KeyValuePair<string, object>[] parameters = null)
	{
		return System.Activator.CreateInstance(
			typeof(LazyCollection<>).MakeGenericType(elementType),
			[context, statement, skip, paging, parameters]);
	}
	#endregion

	#region 嵌套子类
	private interface ILazyCollection
	{
		ValueTask<IEnumerable> MaterializeAsync(bool array, CancellationToken cancellation);
	}

	private sealed class LazyCollection<T> : ILazyCollection, IPageable, IAsyncEnumerable<T>
	{
		#region 事件定义
		public event Func<object, PagingEventArgs, CancellationToken, ValueTask> Paginated;
		#endregion

		#region 成员变量
		private readonly int _skip;
		private readonly Paging _paging;
		private readonly KeyValuePair<string, object>[] _parameters;
		private readonly DataSelectContext _context;
		private readonly SelectStatement _statement;
		#endregion

		#region 构造函数
		public LazyCollection(DataSelectContext context, SelectStatement statement, int skip, Paging paging, KeyValuePair<string, object>[] parameters)
		{
			_skip = skip;
			_context = context ?? throw new ArgumentNullException(nameof(context));
			_statement = statement ?? throw new ArgumentNullException(nameof(statement));
			_parameters = parameters;
			_paging = paging;
		}
		#endregion

		#region 公共属性
		public bool Suppressed => _context.Paging == null || !_context.Paging.IsPaged();
		#endregion

		#region 遍历迭代
		public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellation)
		{
			await using var command = _context.Session.Build(_context, _statement);

			if(_parameters == null)
				StatementSlotter.Evaluate(_context, _statement, command);
			else
			{
				foreach(var parameter in _parameters)
					command.Parameters[parameter.Key].Value = parameter.Value;
			}

			var reader = await command.ExecuteReaderAsync(cancellation).ConfigureAwait(false);
			LazyIterator iterator;

			try
			{
				//如果启用了分页，则先获取分页信息
				if(_paging != null && _paging.IsPaged())
				{
					//首先执行分页查询
					if(await reader.ReadAsync(cancellation).ConfigureAwait(false))
						_paging.Total = (long)Convert.ChangeType(reader.GetValue(0), typeof(long));

					//将读取器移到数据查询
					await reader.NextResultAsync(cancellation).ConfigureAwait(false);

					//激发分页完成事件
					var handlers = this.Paginated;
					if(handlers != null)
					{
						var args = new PagingEventArgs(_context.Name, _paging);
						foreach(Func<object, PagingEventArgs, CancellationToken, ValueTask> handler in handlers.GetInvocationList())
							await handler(this, args, cancellation).ConfigureAwait(false);
					}
				}

				iterator = new LazyIterator(_context, _statement, reader, _skip, cancellation);
			}
			catch
			{
				await reader.DisposeAsync().ConfigureAwait(false);
				throw;
			}

			await using(iterator.ConfigureAwait(false))
			{
				while(await iterator.MoveNextAsync().ConfigureAwait(false))
					yield return iterator.Current;
			}
		}

		public async ValueTask<IEnumerable> MaterializeAsync(bool array, CancellationToken cancellation)
		{
			var items = new List<T>();

			await foreach(var item in this.WithCancellation(cancellation).ConfigureAwait(false))
				items.Add(item);

			return array ? items.ToArray() : items;
		}
		#endregion

		#region 数据迭代
		private class LazyIterator : IAsyncEnumerator<T>
		{
			#region 成员变量
			private T _current;
			private DbDataReader _reader;
			private readonly int _skip;
			private readonly IDataPopulator _populator;
			private readonly DataSelectContext _context;
			private readonly SelectStatement _statement;
			private readonly CancellationToken _cancellation;
			private readonly IDictionary<string, SlaveToken> _slaves;
			#endregion

			#region 构造函数
			public LazyIterator(DataSelectContext context, SelectStatement statement, DbDataReader reader, int skip, CancellationToken cancellation = default)
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
				_cancellation = cancellation;
				_slaves = GetSlaves(_context, _statement, _reader);

				if(Zongsoft.Common.TypeExtension.IsNullable(typeof(T), out var underlyingType))
					_populator = DataEnvironment.Populators.GetPopulator(context.Source.Driver, underlyingType, _reader, entity);
				else
					_populator = DataEnvironment.Populators.GetPopulator(context.Source.Driver, typeof(T), _reader, entity);
			}
			#endregion

			#region 公共成员
			public T Current => _current;

			public async ValueTask<bool> MoveNextAsync()
			{
				var reader = _reader;

				if(reader == null)
					return false;

				try
				{
					_cancellation.ThrowIfCancellationRequested();

					if(await reader.ReadAsync(_cancellation).ConfigureAwait(false))
					{
						_current = await this.PopulateAsync().ConfigureAwait(false);
						return true;
					}
				}
				catch
				{
					await this.DisposeAsync().ConfigureAwait(false);
					throw;
				}

				await this.DisposeAsync().ConfigureAwait(false);
				return false;
			}
			#endregion

			#region 私有方法
			private async ValueTask<T> PopulateAsync()
			{
				var model = _populator.Populate<T>(_reader);

				if(model != null && _slaves != null)
					await this.PopulateSlavesAsync(model).ConfigureAwait(false);

				return model;
			}

			private async ValueTask PopulateSlavesAsync(T model)
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

						//先保存父记录的关联参数，子结果可在父读取器前进后再枚举。
						var parameters = new List<KeyValuePair<string, object>>();
						foreach(var parameter in token.Parameters)
							parameters.Add(new(parameter.Name, _reader.GetValue(parameter.Ordinal)));

						var memberType = token.Schema.Token.MemberType;
						var elementType = Zongsoft.Common.TypeExtension.GetElementType(memberType);

						if(elementType == null)
						{
							Zongsoft.Common.TypeExtension.IsAssignableFrom(typeof(IAsyncEnumerable<>), memberType, type =>
							{
								elementType = type.GetGenericArguments()[0];
								return true;
							});
						}

						var results = (ILazyCollection)CreateResults(elementType, _context, selection, _skip + 1, null, parameters.ToArray());
						object value = results;

						//集合成员必须异步物化，避免在异步查询路径中同步读取数据库
						if(typeof(IEnumerable).IsAssignableFrom(token.Schema.Token.MemberType))
							value = await results.MaterializeAsync(token.Schema.Token.MemberType.IsArray, _cancellation).ConfigureAwait(false);

						token.Schema.Token.SetValue(ref container, value);
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

			private static Dictionary<string, SlaveToken> GetSlaves(DataSelectContext context, IStatementBase statement, IDataReader reader)
			{
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
			}
			#endregion

			#region 处置方法
			public async ValueTask DisposeAsync()
			{
				var reader = Interlocked.Exchange(ref _reader, null);

				if(reader != null)
					await reader.DisposeAsync().ConfigureAwait(false);
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
