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
	public class DataSession : IDisposable
	{
		#region 常量定义
		private const int NONE_FLAG = 0;
		private const int READING_FLAG = 1;
		private const int COMPLETED_FLAG = 1;
		#endregion

		#region 私有变量
		private readonly bool ShareConnectionSupported;
		#endregion

		#region 私有变量
		private volatile int _reads;
		private volatile int _completedFlag;
		private readonly AutoResetEvent _semaphore;
		private readonly ConcurrentBag<IDbCommand> _commands;
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

		/// <summary>获取一个值，指示当前环境事务是否已经完成(提交或回滚)。</summary>
		public bool IsCompleted => _completedFlag != NONE_FLAG;

		/// <summary>获取一个值，指示当前环境事务的数据连接是否正在进行数据读取操作。</summary>
		public bool IsReading => _reads != NONE_FLAG;
		#endregion

		#region 公共方法
		/// <summary>创建语句对应的 <see cref="DbCommand"/> 数据命令。</summary>
		/// <param name="context">指定的数据访问上下文。</param>
		/// <param name="statement">指定要创建命令的语句。</param>
		/// <returns>返回创建的数据命令对象。</returns>
		public DbCommand Build(IDataAccessContextBase context, Expressions.IStatementBase statement)
		{
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

		/// <summary>释放会话，如果当前会话没有完成则回滚事务。</summary>
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

		/// <summary>完成当前数据会话。</summary>
		/// <param name="commit">指定是否提交当前数据事务。</param>
		/// <returns>如果当前会话已经完成了则返回假(False)，否则返回真(True)。</returns>
		private bool Complete(bool? commit)
		{
			//设置完成标记
			var completed = Interlocked.Exchange(ref _completedFlag, COMPLETED_FLAG);

			//如果已经完成过则返回假(False)
			if(completed == COMPLETED_FLAG)
				return false;

			//等待信号量
			_semaphore.WaitOne();

			try
			{
				//获取并将数据事务对象置空
				var transaction = Interlocked.Exchange(ref _transaction, null);

				if(transaction != null && commit.HasValue)
				{
					if(commit.Value)
						transaction.Commit();
					else
						transaction.Rollback();
				}

				//获取并将数据连接对象置空
				var connection = Interlocked.Exchange(ref _connection, null);

				if(connection != null)
				{
					//取消连接事件处理
					connection.StateChange -= Connection_StateChange;

					//如果当前连接正在读取数据，则不要释放该数据连接（由对应的数据读取器关联的延迟加载集合负责关闭）
					if(!this.IsReading)
						connection.Dispose();
				}
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
		/// <summary>设置指定命令的数据连接和关联到必要的数据事务。</summary>
		/// <param name="command">指定要绑定的命令对象。</param>
		internal void Bind(IDbCommand command)
		{
			//如果已经终止则返回
			if(_completedFlag == COMPLETED_FLAG || (_ambient != null && _ambient.IsCompleted))
				throw new DataException("The data session or ambient transaction have been completed.");

			this.Bind(command, () => ShareConnectionSupported || !this.IsReading);
		}

		private void Bind(IDbCommand command, Func<bool> sharable)
		{
			if(command == null)
				throw new ArgumentNullException(nameof(command));

			if(sharable == null)
				throw new ArgumentNullException(nameof(sharable));

			//等待信号量
			_semaphore.WaitOne();

			try
			{
				if(_connection == null)
				{
					lock(_semaphore)
					{
						if(_connection == null)
						{
							_connection = _source.Driver.CreateConnection(_source.ConnectionString);
							_connection.StateChange += Connection_StateChange;

							command.Connection = _connection;
							_commands.Add(command);

							return;
						}
					}
				}

				if(sharable())
				{
					command.Connection = _connection;

					//如果当前事务已启动则更新命令否则将命令加入到待绑定集合中
					if(_transaction == null)
						_commands.Add(command); //将指定的命令加入到命令集，等待事务绑定
					else
						command.Transaction = _transaction;
				}
				else
				{
					command.Connection = _source.Driver.CreateConnection(_source.ConnectionString);
				}
			}
			finally
			{
				//释放当前持有的信号
				_semaphore.Set();
			}
		}

		/// <summary>尝试进入读取临界区。</summary>
		/// <returns>如果成功进入读取状态则返回真(True)，否则返回假(False)。</returns>
		/// <remarks>
		/// 	<p>注意：进入成功的操作者必须确保当读取完成时调用<see cref="ExitRead"/>方法以重置读取标记。</p>
		/// </remarks>
		internal bool EnterRead(IDbCommand command)
		{
			if(ShareConnectionSupported)
			{
				Interlocked.Increment(ref _reads);
				this.Bind(command, () => true);
				return true;
			}

			var original = Interlocked.CompareExchange(ref _reads, READING_FLAG, NONE_FLAG);

			if(original == NONE_FLAG)
			{
				this.Bind(command, () => true);
				return true;
			}
			else
			{
				this.Bind(command, () => false);
				return false;
			}
		}

		/// <summary>退出读取临界区。</summary>
		internal bool ExitRead()
		{
			if(ShareConnectionSupported && _reads > 0)
			{
				var count = Interlocked.Decrement(ref _reads);

				if(count < 0)
					Interlocked.Exchange(ref _reads, 0);

				return count == 0;
			}

			return Interlocked.Exchange(ref _reads, NONE_FLAG) == READING_FLAG;
		}
		#endregion

		#region 事件响应
		private void Connection_StateChange(object sender, StateChangeEventArgs e)
		{
			var connection = (DbConnection)sender;

			switch(e.CurrentState)
			{
				case ConnectionState.Open:
					_transaction = connection.BeginTransaction(GetIsolationLevel());

					//依次设置待绑定命令的事务
					while(_commands.TryTake(out var command))
					{
						command.Transaction = _transaction;
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

				_session.Complete(commit);
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
				//尝试进入数据会话的读取临界区。
				//如果进入成功，则表明该会话支持多活动结果集(MARS)，或者当前操作是会话中的首个读取请求
				if(_session.EnterRead(_command))
				{
					//确认数据连接已打开
					this.EnsureConnect();

					//执行数据命令的读取方法，并构建一个会话数据读取器。
					//注意：该读取器在关闭时会退出读取临界区，并根据会话完成状态确定是否关闭数据连接。
					return new SessionReader(_session, _command.ExecuteReader(behavior & ~CommandBehavior.CloseConnection));
				}
				else
				{
					//确认数据连接已打开
					this.EnsureConnect();

					//读取临界区进入失败：执行得到一个普通的数据读取器，该读取器始终绑定到一个新的数据连接。
					//注意：该读取器在关闭时会关闭对应的数据连接。
					return _command.ExecuteReader(behavior | CommandBehavior.CloseConnection);
				}
			}

			protected override async Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellation)
			{
				//尝试进入数据会话的读取临界区。
				//如果进入成功，则表明该会话支持多活动结果集(MARS)，或者当前操作是会话中的首个读取请求
				if(_session.EnterRead(_command))
				{
					//确认数据连接已打开
					await this.EnsureConnectAsync(cancellation);

					//执行数据命令的读取方法，并构建一个会话数据读取器。
					//注意：该读取器在关闭时会退出读取临界区，并根据会话完成状态确定是否关闭数据连接。
					return new SessionReader(_session, await _command.ExecuteReaderAsync(behavior & ~CommandBehavior.CloseConnection, cancellation));
				}
				else
				{
					//确认数据连接已打开
					await this.EnsureConnectAsync(cancellation);

					//读取临界区进入失败：执行得到一个普通的数据读取器，该读取器始终绑定到一个新的数据连接。
					//注意：该读取器在关闭时会关闭对应的数据连接。
					return await _command.ExecuteReaderAsync(behavior | CommandBehavior.CloseConnection, cancellation);
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
			private readonly DbConnection _connection;
			#endregion

			#region 构造函数
			internal SessionReader(DataSession session, DbDataReader reader)
			{
				_session = session ?? throw new ArgumentNullException(nameof(session));
				_reader = reader ?? throw new ArgumentNullException(nameof(reader));
				_connection = session._connection;
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
				//关闭数据读取器
				_reader.Close();

				//退出环境事务的读取临界区（即重置环境事务的读取标记）
				var disconnectable = _session.ExitRead();

				/*
				 * 如果数据会话已经完结，则需要关闭释放对应的数据连接。
				 * 因为当会话完成(提交或回滚)时，如果该会话正处于读取状态(即位于读取临界区内)，
				 * 完成操作是不会关闭数据连接的，因为读取操作还需要使用它，即该数据连接由本读取器进行关闭。
				 */
				if(disconnectable && _session.IsCompleted)
					_connection?.Dispose();
			}
			#endregion
		}
		#endregion
	}
}
