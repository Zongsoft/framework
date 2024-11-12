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
using System.IO;
using System.Data;
using System.Data.Common;
using System.Collections;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Data.Common
{
	/// <summary>
	/// 表示数据操作的会话类。
	/// </summary>
	public class DataSession : IDisposable, IAsyncDisposable
	{
		#region 常量定义
		private const int COMPLETION_NONE = 0;
		private const int COMPLETION_COMMIT = 1;
		private const int COMPLETION_ROLLBACK = 2;
		#endregion

		#region 私有变量
		private readonly bool ShareConnectionSupported;
		private readonly bool TransactionSupported;
		#endregion

		#region 私有变量
		private volatile int _pending;    //表示当前会话等待打开的数据读取器的数量
		private volatile int _reading;    //表示当前会话已经打开的数据读取器的数量
		private volatile int _completion; //表示当前会话是否已经结束(提交或回滚)的标记
		private readonly AutoResetEvent _semaphore; //表示当前会话结束与连接操作的同步信号量
		private readonly ConcurrentBag<IDbCommand> _commands; //表示待关联事务的命令对象集
		#endregion

		#region 成员字段
		private readonly IDataSource _source;
		private volatile DbConnection _connection;
		private volatile DbTransaction _transaction;
		private readonly Transactions.Transaction _ambient;
		#endregion

		#region 构造函数
		public DataSession(IDataSource source, Zongsoft.Transactions.Transaction ambient = null)
		{
			_source = source ?? throw new ArgumentNullException(nameof(source));
			_ambient = ambient;

			_semaphore = new AutoResetEvent(true);
			_commands = new ConcurrentBag<IDbCommand>();

			if(_ambient != null)
				_ambient.Enlist(new Enlistment(this));

			this.TransactionSupported = !source.Features.Support(Feature.TransactionSuppressed);
			this.ShareConnectionSupported = source.Features.Support(Feature.MultipleActiveResultSets);
		}
		#endregion

		#region 公共属性
		/// <summary>获取一个值，指示当前数据会话是否位于环境事务中。</summary>
		public bool InAmbient => _ambient != null;

		/// <summary>获取当前数据会话的数据源对象。</summary>
		public IDataSource Source => _source;

		/// <summary>获取当前数据会话的主连接对象。</summary>
		public IDbConnection Connection => _connection;

		/// <summary>获取当前数据会话关联的数据事务。</summary>
		public IDbTransaction Transaction => _transaction;

		/// <summary>获取一个值，指示当前会话是否已经完成(提交或回滚)。</summary>
		public bool IsCompleted => _completion != COMPLETION_NONE;

		/// <summary>获取一个值，指示当前会话是否还有“待执行”的数据读取命令或“执行中”的数据读取操作。</summary>
		public bool HasPending => _pending > 0 || _reading > 0;
		#endregion

		#region 公共方法
		/// <summary>创建语句对应的 <see cref="DbCommand"/> 数据命令。</summary>
		/// <param name="context">指定的数据访问上下文。</param>
		/// <param name="statement">指定要创建命令的语句。</param>
		/// <returns>返回创建的数据命令对象。</returns>
		public DbCommand Build(IDataAccessContextBase context, Expressions.IStatementBase statement)
		{
			if(statement is Expressions.SelectStatementBase)
				Interlocked.Increment(ref _pending);

			return new SessionCommand(this, _source.Driver.CreateCommand(context, statement));
		}

		/// <summary>提交当前会话事务。</summary>
		public void Commit()
		{
			/*
			 * 注意：如果当前会话位于环境事务内，则提交操作必须由环境事务的 Enlistment 回调函数处理，即本方法不做任何处理。
			 */
			if(_ambient != null)
				return;

			this.Complete(true);
		}

		/// <summary>提交当前会话事务。</summary>
		public async ValueTask CommitAsync(CancellationToken cancellation)
		{
			/*
			 * 注意：如果当前会话位于环境事务内，则提交操作必须由环境事务的 Enlistment 回调函数处理，即本方法不做任何处理。
			 */
			if(_ambient != null)
				return;

			await this.CompleteAsync(true, cancellation);
		}

		/// <summary>回滚当前会话事务。</summary>
		public void Rollback()
		{
			/*
			 * 注意：如果当前会话位于环境事务内，则回滚操作必须由环境事务的 Enlistment 回调函数处理，即本方法不做任何处理。
			 */
			if(_ambient != null)
				return;

			this.Complete(false);
		}

		/// <summary>回滚当前会话事务。</summary>
		public async ValueTask RollbackAsync(CancellationToken cancellation)
		{
			/*
			 * 注意：如果当前会话位于环境事务内，则回滚操作必须由环境事务的 Enlistment 回调函数处理，即本方法不做任何处理。
			 */
			if(_ambient != null)
				return;

			await this.CompleteAsync(false, cancellation);
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if(disposing)
			{
				if(this.Complete(false))
				{
					//释放信号量资源
					_semaphore.Dispose();
				}
			}
		}

		public async ValueTask DisposeAsync()
		{
			await this.DisposeAsync(true);
			GC.SuppressFinalize(this);
		}

		protected virtual async ValueTask DisposeAsync(bool disposing)
		{
			if(disposing)
			{
				if(await this.CompleteAsync(false, CancellationToken.None))
				{
					//释放信号量资源
					_semaphore.Dispose();
				}
			}
		}

		/// <summary>完成当前数据会话。</summary>
		/// <param name="committing">指定是否提交当前数据事务。</param>
		/// <returns>如果当前会话已经完成了则返回假(False)，否则返回真(True)。</returns>
		private bool Complete(bool committing)
		{
			//设置完成标记
			var completed = Interlocked.Exchange(ref _completion, committing ? COMPLETION_COMMIT : COMPLETION_ROLLBACK);

			//如果已经完成过则返回
			if(completed != COMPLETION_NONE)
				return false;

			//等待信号量
			_semaphore.WaitOne();

			try
			{
				//如果还有“待执行”或“执行中”的数据读取器则不能提交事务及释放资源
				if(this.HasPending)
					return true;

				//执行事务提交和释放资源
				this.Destroy();
			}
			finally
			{
				//释放当前持有的信号
				_semaphore.Set();
			}

			//返回完成成功
			return true;
		}

		/// <summary>完成当前数据会话。</summary>
		/// <param name="committing">指定是否提交当前数据事务。</param>
		/// <param name="cancellation">指定的异步操作的取消标记。</param>
		/// <returns>如果当前会话已经完成了则返回假(False)，否则返回真(True)。</returns>
		private async ValueTask<bool> CompleteAsync(bool committing, CancellationToken cancellation)
		{
			//设置完成标记
			var completed = Interlocked.Exchange(ref _completion, committing ? COMPLETION_COMMIT : COMPLETION_ROLLBACK);

			//如果已经完成过则返回
			if(completed != COMPLETION_NONE)
				return false;

			//等待信号量
			_semaphore.WaitOne();

			try
			{
				//如果还有“待执行”或“执行中”的数据读取器则不能提交事务及释放资源
				if(this.HasPending)
					return true;

				//执行事务提交和释放资源
				await this.DestroyAsync(cancellation);
			}
			finally
			{
				//释放当前持有的信号
				_semaphore.Set();
			}

			//返回完成成功
			return true;
		}
		#endregion

		#region 内部方法
		/// <summary>绑定指定命令的数据连接，并关联命令的数据事务。</summary>
		/// <param name="command">指定要绑定的命令对象。</param>
		internal void Bind(IDbCommand command)
		{
			//等待信号量
			_semaphore.WaitOne();

			try
			{
				if(_connection == null)
				{
					lock(this)
					{
						if(_connection == null)
						{
							_connection = _source.Driver.CreateConnection(_source.ConnectionString);
							_connection.StateChange += Connection_StateChange;

							command.Connection = _connection;

							//将命令加入到待绑定事务的命令集中
							_commands.Add(command);

							return;
						}
					}
				}

				//设置当前命令的连接为当前会话的主连接
				command.Connection = _connection;

				//如果驱动支持事务则进行相关事务处理
				if(TransactionSupported)
				{
					//如果当前事务已启动则更新命令否则将命令加入到待绑定集合中
					if(_transaction == null)
					{
						if(_connection.State == ConnectionState.Open)
							_transaction = _connection.BeginTransaction();
						else
							_commands.Add(command); //将命令加入到绑定事务的命令集，等待事务绑定
					}

					command.Transaction = _transaction;
				}
			}
			finally
			{
				//释放当前持有的信号
				_semaphore.Set();
			}
		}

		internal bool PrepareReader(IDbCommand command)
		{
			//如果当前会话已经完成，则数据读取器应构建独属的连接
			if(this.IsCompleted)
			{
				command.Connection = _source.Driver.CreateConnection(_source.ConnectionString);
				return false;
			}

			//递减“待执行”的数据读取器数量
			Interlocked.Decrement(ref _pending);
			//递增“执行中”的数据读取器数量
			var reading = Interlocked.Increment(ref _reading);

			if(ShareConnectionSupported)
			{
				this.Bind(command);
				return true;
			}

			//如果当前会话的主连接已经被其他读取器占用则只能创建新的连接
			if(reading > 1)
			{
				command.Connection = _source.Driver.CreateConnection(_source.ConnectionString);
				return false;
			}
			else
			{
				this.Bind(command);
				return true;
			}
		}

		internal void ReleaseReader()
		{
			//递减“执行中”的数据读取器数量
			Interlocked.Decrement(ref _reading);

			//只有当“待执行”和“执行中”的数据读取器都没有了，并且当前会话已经结束才能提交事务及释放所有资源
			if(!this.HasPending && this.IsCompleted)
				this.Destroy();
		}

		internal ValueTask ReleaseReaderAsync(CancellationToken cancellation = default)
		{
			//递减“执行中”的数据读取器数量
			Interlocked.Decrement(ref _reading);

			//只有当“待执行”和“执行中”的数据读取器都没有了，并且当前会话已经结束才能提交事务及释放所有资源
			if(!this.HasPending && this.IsCompleted)
				return this.DestroyAsync(cancellation);
			else
				return ValueTask.CompletedTask;
		}

		private void Destroy()
		{
			//获取并将事务对象置空
			var transaction = Interlocked.Exchange(ref _transaction, null);

			if(transaction != null)
			{
				//尝试提交或回滚事务
				switch(_completion)
				{
					case COMPLETION_COMMIT:
						transaction.Commit();
						break;
					case COMPLETION_ROLLBACK:
						transaction.Rollback();
						break;
				}
			}

			//获取并将主连接对象置空
			var connection = Interlocked.Exchange(ref _connection, null);

			if(connection != null)
			{
				//取消连接事件处理
				connection.StateChange -= Connection_StateChange;

				//释放主数据连接
				connection.Dispose();
			}
		}

		private async ValueTask DestroyAsync(CancellationToken cancellation)
		{
			//获取并将事务对象置空
			var transaction = Interlocked.Exchange(ref _transaction, null);

			if(transaction != null)
			{
				//尝试提交或回滚事务
				switch(_completion)
				{
					case COMPLETION_COMMIT:
						await transaction.CommitAsync(cancellation);
						break;
					case COMPLETION_ROLLBACK:
						await transaction.RollbackAsync(cancellation);
						break;
				}
			}

			//获取并将主连接对象置空
			var connection = Interlocked.Exchange(ref _connection, null);

			if(connection != null)
			{
				//取消连接事件处理
				connection.StateChange -= Connection_StateChange;

				//释放主数据连接
				await connection.DisposeAsync();
			}
		}
		#endregion

		#region 连接事件
		private void Connection_StateChange(object sender, StateChangeEventArgs e)
		{
			var connection = (DbConnection)sender;

			switch(e.CurrentState)
			{
				case ConnectionState.Open:
					//只有驱动支持事务才能发起事务操作
					if(TransactionSupported)
					{
						_transaction = connection.BeginTransaction(GetIsolationLevel());

						//依次设置待绑定命令的事务
						while(_commands.TryTake(out var command))
						{
							command.Transaction = _transaction;
						}
					}

					break;
				case ConnectionState.Closed:
					_transaction = null;
					break;
			}
		}
		#endregion

		#region 私有方法
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private IsolationLevel GetIsolationLevel()
		{
			if(_ambient == null)
				return IsolationLevel.Unspecified;

			return _ambient.IsolationLevel switch
			{
				Transactions.IsolationLevel.ReadCommitted => IsolationLevel.ReadCommitted,
				Transactions.IsolationLevel.ReadUncommitted => IsolationLevel.ReadUncommitted,
				Transactions.IsolationLevel.RepeatableRead => IsolationLevel.RepeatableRead,
				Transactions.IsolationLevel.Serializable => IsolationLevel.Serializable,
				_ => IsolationLevel.Unspecified,
			};
		}
		#endregion

		#region 嵌套子类
		private class Enlistment(DataSession session) : Zongsoft.Transactions.IEnlistment
		{
			private readonly DataSession _session = session;

			public void OnEnlist(Zongsoft.Transactions.EnlistmentContext context)
			{
				if(context.Phase == Zongsoft.Transactions.EnlistmentPhase.Prepare)
					return;

				bool? commit = null;

				switch(context.Phase)
				{
					case Transactions.EnlistmentPhase.Commit:
						commit = true;
						break;
					case Transactions.EnlistmentPhase.Abort:
					case Transactions.EnlistmentPhase.Rollback:
						commit = false;
						break;
				}

				if(commit.HasValue)
					_session.Complete(commit.Value);
			}
		}

		private class SessionCommand : DbCommand
		{
			#region 成员字段
			private readonly DataSession _session;
			private readonly DbCommand _command;
			#endregion

			#region 构造函数
			internal SessionCommand(DataSession session, DbCommand command)
			{
				_session = session ?? throw new ArgumentNullException(nameof(session));
				_command = command ?? throw new ArgumentNullException(nameof(command));
			}
			#endregion

			#region 重写属性
			public override string CommandText
			{
				get => _command.CommandText;
				set => _command.CommandText = value;
			}

			public override CommandType CommandType
			{
				get => _command.CommandType;
				set => _command.CommandType = value;
			}

			public override int CommandTimeout
			{
				get => _command.CommandTimeout;
				set => _command.CommandTimeout = value;
			}

			protected override DbConnection DbConnection
			{
				get => _command.Connection;
				set => _command.Connection = value;
			}

			protected override DbTransaction DbTransaction
			{
				get => _command.Transaction;
				set => _command.Transaction = value;
			}

			protected override DbParameterCollection DbParameterCollection
			{
				get => _command.Parameters;
			}

			public override bool DesignTimeVisible
			{
				get => _command.DesignTimeVisible;
				set => _command.DesignTimeVisible = value;
			}

			public override UpdateRowSource UpdatedRowSource
			{
				get => _command.UpdatedRowSource;
				set => _command.UpdatedRowSource = value;
			}
			#endregion

			#region 重写方法
			public override void Cancel()
			{
				if(_command.Connection != null)
					_command.Cancel();
			}

			public override void Prepare() => _command.Prepare();
			protected override DbParameter CreateDbParameter() => _command.CreateParameter();

			public override object ExecuteScalar()
			{
				//确认数据连接已打开
				this.EnsureConnect();

				//返回数据命令执行结果
				return _command.ExecuteScalar();
			}

			public override async Task<object> ExecuteScalarAsync(CancellationToken cancellation)
			{
				//确认数据连接已打开
				await this.EnsureConnectAsync(cancellation);

				//返回数据命令执行结果
				return await _command.ExecuteScalarAsync(cancellation);
			}

			public override int ExecuteNonQuery()
			{
				//确认数据连接已打开
				this.EnsureConnect();

				//返回数据命令执行结果
				return _command.ExecuteNonQuery();
			}

			public override async Task<int> ExecuteNonQueryAsync(CancellationToken cancellation)
			{
				//确认数据连接已打开
				await this.EnsureConnectAsync(cancellation);

				//返回数据命令执行结果
				return await _command.ExecuteNonQueryAsync(cancellation);
			}

			protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
			{
				//准备读取器，返回真则表示该会话支持多活动结果集(MARS)，或者当前命令关联的是会话主连接(即会话事务)
				if(_session.PrepareReader(_command))
				{
					//确认数据连接已打开
					this.EnsureConnect();

					//构建会话数据读取器，注意：该读取器关联的连接为非独享连接
					return new SessionReader(_session, _command.ExecuteReader(behavior & ~CommandBehavior.CloseConnection));
				}
				else
				{
					//确认数据连接已打开
					this.EnsureConnect();

					//构建会话数据读取器，并设置读取器关联的连接为该命令对象的独享连接
					return new SessionReader(_session, _command.ExecuteReader(behavior | CommandBehavior.CloseConnection), _command.Connection);
				}
			}

			protected override async Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellation)
			{
				//准备读取器，返回真则表示该会话支持多活动结果集(MARS)，或者当前命令关联的是会话主连接(即会话事务)
				if(_session.PrepareReader(_command))
				{
					//确认数据连接已打开
					await this.EnsureConnectAsync(cancellation);

					//构建会话数据读取器，注意：该读取器关联的连接为非独享连接
					return new SessionReader(_session, await _command.ExecuteReaderAsync(behavior & ~CommandBehavior.CloseConnection, cancellation));
				}
				else
				{
					//确认数据连接已打开
					await this.EnsureConnectAsync(cancellation);

					//构建会话数据读取器，并设置读取器关联的连接为该命令对象的独享连接
					return new SessionReader(_session, await _command.ExecuteReaderAsync(behavior | CommandBehavior.CloseConnection, cancellation), _command.Connection);
				}
			}
			#endregion

			#region 私有方法
			[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
			private void EnsureConnect()
			{
				if(_command.Connection == null)
					_session.Bind(_command);

				if(_command.Connection.State == ConnectionState.Closed || _command.Connection.State == ConnectionState.Broken)
					_command.Connection.Open();
			}

			[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
			private Task EnsureConnectAsync(CancellationToken cancellation)
			{
				if(_command.Connection == null)
					_session.Bind(_command);

				if(_command.Connection.State == ConnectionState.Closed || _command.Connection.State == ConnectionState.Broken)
					return _command.Connection.OpenAsync(cancellation);

				return Task.CompletedTask;
			}
			#endregion
		}

		private class SessionReader : DbDataReader
		{
			#region 成员字段
			private readonly DataSession _session;
			private readonly DbDataReader _reader;
			private DbConnection _connection; //表示当前数据读取器的独享连接，如果为空则表示采用当前会话的主连接
			#endregion

			#region 构造函数
			internal SessionReader(DataSession session, DbDataReader reader, DbConnection connection = null)
			{
				_session = session ?? throw new ArgumentNullException(nameof(session));
				_reader = reader ?? throw new ArgumentNullException(nameof(reader));
				_connection = connection;
			}
			#endregion

			#region 重写属性
			public override object this[int ordinal] => _reader[ordinal];
			public override object this[string name] => _reader[name];
			public override int Depth => _reader.Depth;
			public override int FieldCount => _reader.FieldCount;
			public override bool HasRows => _reader.HasRows;
			public override bool IsClosed => _reader.IsClosed;
			public override int RecordsAffected => _reader.RecordsAffected;
			public override int VisibleFieldCount => _reader.VisibleFieldCount;
			#endregion

			#region 重写方法
			public override bool GetBoolean(int ordinal) => _reader.GetBoolean(ordinal);
			public override byte GetByte(int ordinal) => _reader.GetByte(ordinal);
			public override long GetBytes(int ordinal, long offset, byte[] buffer, int bufferOffset, int length) => _reader.GetBytes(ordinal, offset, buffer, bufferOffset, length);
			public override char GetChar(int ordinal) => _reader.GetChar(ordinal);
			public override long GetChars(int ordinal, long offset, char[] buffer, int bufferOffset, int length) => _reader.GetChars(ordinal, offset, buffer, bufferOffset, length);
			public override DateTime GetDateTime(int ordinal) => _reader.GetDateTime(ordinal);
			public override decimal GetDecimal(int ordinal) => _reader.GetDecimal(ordinal);
			public override double GetDouble(int ordinal) => _reader.GetDouble(ordinal);
			public override float GetFloat(int ordinal) => _reader.GetFloat(ordinal);
			public override Guid GetGuid(int ordinal) => _reader.GetGuid(ordinal);
			public override short GetInt16(int ordinal) => _reader.GetInt16(ordinal);
			public override int GetInt32(int ordinal) => _reader.GetInt32(ordinal);
			public override long GetInt64(int ordinal) => _reader.GetInt64(ordinal);
			public override string GetString(int ordinal) => _reader.GetString(ordinal);
			public override Stream GetStream(int ordinal) => _reader.GetStream(ordinal);
			public override object GetValue(int ordinal) => _reader.GetValue(ordinal);
			public override int GetValues(object[] values) => _reader.GetValues(values);
			public override string GetName(int ordinal) => _reader.GetName(ordinal);
			public override int GetOrdinal(string name) => _reader.GetOrdinal(name);
			public override string GetDataTypeName(int ordinal) => _reader.GetDataTypeName(ordinal);
			public override Type GetFieldType(int ordinal) => _reader.GetFieldType(ordinal);
			public override T GetFieldValue<T>(int ordinal) => _reader.GetFieldValue<T>(ordinal);
			public override Task<T> GetFieldValueAsync<T>(int ordinal, CancellationToken cancellationToken) => _reader.GetFieldValueAsync<T>(ordinal, cancellationToken);
			public override TextReader GetTextReader(int ordinal) => _reader.GetTextReader(ordinal);
			public override IEnumerator GetEnumerator() => _reader.GetEnumerator();
			public override bool IsDBNull(int ordinal) => _reader.IsDBNull(ordinal);
			public override bool NextResult() => _reader.NextResult();
			public override bool Read() => _reader.Read();
			#endregion

			#region 关闭方法
			public override void Close()
			{
				if(_reader.IsClosed)
					return;

				//关闭数据读取器
				_reader.Close();

				//如果当前读取器有独享连接，则必须将该连接释放
				_connection?.Dispose();

				//设置当前读取器的独享连接置空
				_connection = null;

				//通知当前会话，关闭一个读取器
				_session.ReleaseReader();
			}

			public override async Task CloseAsync()
			{
				if(_reader.IsClosed)
					return;

				//关闭数据读取器
				await _reader.CloseAsync();

				//如果当前读取器有独享连接，则必须将该连接释放
				var connection = _connection;
				if(connection != null)
					await connection.DisposeAsync();

				//设置当前读取器的独享连接置空
				_connection = null;

				//通知当前会话，关闭一个读取器
				await _session.ReleaseReaderAsync();
			}
			#endregion
		}
		#endregion
	}
}
