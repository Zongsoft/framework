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

using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.Common;

public class DataExecuteExecutor : IDataExecutor<ExecutionStatement>
{
	#region 同步执行
	public bool Execute(IDataAccessContext context, ExecutionStatement statement)
	{
		if(context is DataExecuteContext ctx)
			return this.OnExecute(ctx, statement);

		throw new DataException($"Data Engine Error: The '{this.GetType().Name}' executor does not support execution of '{context.GetType().Name}' context.");
	}

	protected virtual bool OnExecute(DataExecuteContext context, ExecutionStatement statement)
	{
		//根据生成的脚本创建对应的数据命令
		var command = context.Session.Build(context, statement);

		if(statement.IsProcedure)
			command.CommandType = CommandType.StoredProcedure;

		if(context.IsScalar)
		{
			context.Result = command.ExecuteScalar();
			return false;
		}

		context.Result = System.Activator.CreateInstance(
			typeof(ResultCollection<>).MakeGenericType(context.ResultType),
			new object[] { context, command });

		return false;
	}
	#endregion

	#region 异步执行
	public ValueTask<bool> ExecuteAsync(IDataAccessContext context, ExecutionStatement statement, CancellationToken cancellation)
	{
		if(context is DataExecuteContext ctx)
			return this.OnExecuteAsync(ctx, statement, cancellation);

		throw new DataException($"Data Engine Error: The '{this.GetType().Name}' executor does not support execution of '{context.GetType().Name}' context.");
	}

	protected virtual async ValueTask<bool> OnExecuteAsync(DataExecuteContext context, ExecutionStatement statement, CancellationToken cancellation)
	{
		//根据生成的脚本创建对应的数据命令
		var command = context.Session.Build(context, statement);

		if(statement.IsProcedure)
			command.CommandType = CommandType.StoredProcedure;

		if(context.IsScalar)
		{
			context.Result = await command.ExecuteScalarAsync(cancellation);
			return false;
		}

		context.Result = System.Activator.CreateInstance(
			typeof(ResultCollection<>).MakeGenericType(context.ResultType),
			new object[] { context, command });

		return false;
	}
	#endregion

	#region 嵌套子类
	private class ResultCollection<T> : IAsyncEnumerable<T>, IEnumerable<T>, IEnumerable
	{
		#region 成员字段
		private readonly IDataDriver _driver;
		private readonly DbCommand _command;
		#endregion

		#region 构造函数
		public ResultCollection(DataExecuteContext context, DbCommand command)
		{
			_command = command;
			_driver = context.Source.Driver;
		}
		#endregion

		#region 遍历迭代
		public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellation)
		{
			var iterator = new ResultIterator(_driver, await _command.ExecuteReaderAsync(cancellation));

			while(await iterator.MoveNextAsync())
				yield return iterator.Current;
		}

		public IEnumerator<T> GetEnumerator() => new ResultIterator(_driver, _command.ExecuteReader());
		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		#endregion

		#region 嵌套子类
		private class ResultIterator : IEnumerator<T>, IAsyncEnumerator<T>
		{
			private readonly DbDataReader _reader;
			private readonly IDataPopulator _populator;

			public ResultIterator(IDataDriver driver, DbDataReader reader)
			{
				_reader = reader;
				_populator = DataEnvironment.Populators.GetPopulator(driver, typeof(T), _reader);
			}

			public T Current { get => _populator.Populate<T>(_reader); }

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

			public void Dispose() => _reader.Dispose();
			public ValueTask DisposeAsync() => _reader.DisposeAsync();

			object IEnumerator.Current { get => this.Current; }
			void IEnumerator.Reset() { }
		}
		#endregion
	}
	#endregion
}
