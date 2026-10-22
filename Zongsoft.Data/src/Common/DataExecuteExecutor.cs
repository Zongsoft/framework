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
using System.Collections.Generic;

using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.Common;

public class DataExecuteExecutor : IDataExecutor<ExecutionStatement>
{
	#region 异步执行
	public ValueTask<bool> ExecuteAsync(IDataAccessContext context, ExecutionStatement statement, CancellationToken cancellation)
	{
		if(context is DataExecuteContext ctx)
			return this.OnExecuteAsync(ctx, statement, cancellation);

		throw new DataException(string.Format(Properties.Resources.DataExecutor_UnsupportedContext_Message, this.GetType().Name, context.GetType().Name));
	}

	protected virtual async ValueTask<bool> OnExecuteAsync(DataExecuteContext context, ExecutionStatement statement, CancellationToken cancellation)
	{
		if(!context.IsScalar && context.ResultType != null && context.ResultType != typeof(object))
		{
			context.Result = Activator.CreateInstance(typeof(ResultCollection<>).MakeGenericType(context.ResultType), [context, statement]);
			return false;
		}

		await using var command = context.Session.Build(context, statement);

		if(statement.IsProcedure)
			command.CommandType = CommandType.StoredProcedure;

		if(context.IsScalar)
			context.Result = await command.ExecuteScalarAsync(cancellation);
		else
			context.Result = await command.ExecuteNonQueryAsync(cancellation);

		SetOutputs(context, command);
		return false;
	}
	#endregion

	#region 私有方法
	private static void SetOutputs(DataExecuteContext context, DbCommand command)
	{
		if(command == null || context.Parameters == null)
			return;

		foreach(var parameter in context.Parameters)
		{
			if(IsOutputParameter(parameter) && TryGetParameter(context, command, parameter.Name, out var dbParameter))
				parameter.Value = Convert.IsDBNull(dbParameter.Value) ? null : dbParameter.Value;
		}

		static bool IsOutputParameter(Parameter parameter) => parameter != null &&
		(
			parameter.Direction == ParameterDirection.Output ||
			parameter.Direction == ParameterDirection.InputOutput ||
			parameter.Direction == ParameterDirection.ReturnValue
		);

		static bool TryGetParameter(DataExecuteContext context, DbCommand command, string name, out DbParameter parameter)
		{
			name = GetParameterName(name, context.Command.Parameters);

			if(command.Parameters.Contains(name))
			{
				parameter = command.Parameters[name];
				return true;
			}

			parameter = null;
			return false;

			static string GetParameterName(string name, Metadata.DataCommandParameterCollection parameters) =>
				parameters != null && parameters.TryGetValue(name, out var parameter) ? (string.IsNullOrEmpty(parameter.Alias) ? parameter.Name : parameter.Alias) : name;
		}
	}
	#endregion

	#region 嵌套子类
	private class ResultCollection<T> : IAsyncEnumerable<T>
	{
		#region 成员字段
		private readonly DataExecuteContext _context;
		private readonly ExecutionStatement _statement;
		#endregion

		#region 构造函数
		public ResultCollection(DataExecuteContext context, ExecutionStatement statement)
		{
			_context = context;
			_statement = statement;
		}
		#endregion

		#region 遍历迭代
		public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellation)
		{
			await using var command = _context.Session.Build(_context, _statement);
			if(_statement.IsProcedure)
				command.CommandType = CommandType.StoredProcedure;

			var reader = await command.ExecuteReaderAsync(cancellation);
			ResultIterator iterator;

			try
			{
				iterator = new ResultIterator(_context.Source.Driver, reader, cancellation);
			}
			catch
			{
				await reader.DisposeAsync();
				throw;
			}

			try
			{
				await using(iterator)
				{
					while(await iterator.MoveNextAsync())
						yield return iterator.Current;
				}
			}
			finally
			{
				SetOutputs(_context, command);
			}
		}
		#endregion

		#region 嵌套子类
		private class ResultIterator : IAsyncEnumerator<T>
		{
			private readonly DbDataReader _reader;
			private readonly IDataPopulator _populator;
			private readonly CancellationToken _cancellation;

			public ResultIterator(IDataDriver driver, DbDataReader reader, CancellationToken cancellation = default)
			{
				_reader = reader ?? throw new ArgumentNullException(nameof(reader));
				_cancellation = cancellation;

				_populator = DataEnvironment.Populators.GetPopulator(driver, typeof(T), _reader);
			}

			public T Current { get => _populator.Populate<T>(_reader); }

			public async ValueTask<bool> MoveNextAsync()
			{
				_cancellation.ThrowIfCancellationRequested();

				if(await _reader.ReadAsync(_cancellation))
					return true;

				await this.DisposeAsync();
				return false;
			}
			public ValueTask DisposeAsync() => _reader.DisposeAsync();

		}
		#endregion
	}
	#endregion
}
