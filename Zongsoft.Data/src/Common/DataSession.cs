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
using System.IO;
using System.Data;
using System.Data.Common;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Data.Common;

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
	private readonly bool TransactionSupported;
	private readonly bool ShareConnectionSupported;

	private int _reading;             //表示当前会话尚未释放的数据读取活动数量
	private int _activities;          //表示当前会话尚未结束的数据操作数量
	private int _completion;          //表示当前会话是否已经结束(提交或回滚)的标记
	private readonly object _synchrolock;      //表示当前会话状态转换的同步对象
	private readonly SemaphoreSlim _semaphore; //表示当前会话连接及事务初始化的同步信号量
	private readonly TaskCompletionSource _completionSource; //表示延迟提交/回滚完成信号（仅作等待信号，不替代上述状态机）
	#endregion

	#region 成员字段
	private readonly IDataSource _source;
	private readonly DataConnector _connector;
	private volatile DbConnection _connection;
	private volatile DbTransaction _transaction;
	private readonly TransactionContext _ambient;
	#endregion

	#region 构造函数
	public DataSession(IDataSource source, TransactionContext ambient = null)
	{
		_source = source ?? throw new ArgumentNullException(nameof(source));
		_connector = DataConnectorManager.GetConnector(source);
		_ambient = ambient;

		_synchrolock = new object();
		_semaphore = new SemaphoreSlim(1, 1);
		_completionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

		if(_ambient != null && !_ambient.Enlist(new Enlistment(this)))
		{
			_semaphore.Dispose();
			throw new DataException(Properties.Resources.DataSession_AmbientTransactionCompleted_Message);
		}

		this.TransactionSupported = !source.Features.Support(Feature.TransactionSuppressed);
		this.ShareConnectionSupported = source.Features.Support(Feature.MultipleActiveResultSets);
	}
	#endregion

	#region 公共属性
	/// <summary>获取当前数据会话的数据源对象。</summary>
	public IDataSource Source => _source;

	/// <summary>获取当前数据会话所属数据源的共享连接器。</summary>
	/// <remarks>注意：通过该连接器建立的连接为独立连接，不会自动加入当前数据会话的事务，也不属于当前数据会话的生命周期。</remarks>
	public DataConnector Connector => _connector;

	/// <summary>获取当前数据会话的主连接对象。</summary>
	public IDbConnection Connection => _connection;

	/// <summary>获取当前数据会话关联的数据事务。</summary>
	public IDbTransaction Transaction => _transaction;

	/// <summary>获取一个值，指示当前数据会话是否位于环境事务中。</summary>
	public bool InTransaction => _ambient != null;

	/// <summary>获取一个值，指示当前会话是否已经完成(提交或回滚)。</summary>
	public bool IsCompleted => Volatile.Read(ref _completion) != COMPLETION_NONE;
	#endregion

	#region 内部属性
	/// <summary>获取一个表示当前会话真实完成（提交/回滚并销毁）的任务。</summary>
	/// <remarks>供环境事务的登记回调在延迟销毁场景下等待真实完成。</remarks>
	internal Task Completion => _completionSource.Task;
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

	/// <summary>获取当前数据会话的数据连接租约。</summary>
	/// <param name="transactionSuppressed">指示是否禁止关联当前环境事务。</param>
	/// <returns>返回获取的数据连接租约。</returns>
	public ConnectionLease AcquireLease(bool transactionSuppressed = false)
	{
		var activity = this.ReserveActivity();

		return !this.TransactionSupported || (_ambient != null && transactionSuppressed) ?
			this.CreateIndependentLease(activity) : this.CreateSessionLease(activity);
	}

	/// <summary>异步获取当前数据会话的数据连接租约。</summary>
	/// <param name="transactionSuppressed">指示是否禁止关联当前环境事务。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回获取的数据连接租约。</returns>
	public async ValueTask<ConnectionLease> AcquireLeaseAsync(bool transactionSuppressed = false, CancellationToken cancellation = default)
	{
		var activity = this.ReserveActivity();

		return !this.TransactionSupported || (_ambient != null && transactionSuppressed) ?
			await this.CreateIndependentLeaseAsync(activity, cancellation).ConfigureAwait(false) :
			await this.CreateSessionLeaseAsync(activity, cancellation).ConfigureAwait(false);
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
			this.Complete(false);
	}

	public async ValueTask DisposeAsync()
	{
		await this.DisposeAsync(true);
		GC.SuppressFinalize(this);
	}

	protected virtual async ValueTask DisposeAsync(bool disposing)
	{
		if(disposing)
			await this.CompleteAsync(false, CancellationToken.None);
	}
	#endregion

	#region 连接准备
	/// <summary>准备指定命令的数据连接及其关联事务。</summary>
	/// <param name="command">指定的数据命令。</param>
	/// <returns>返回指定命令的数据连接租约。</returns>
	private ConnectionLease PrepareCommand(DbCommand command)
	{
		if(command == null)
			throw new ArgumentNullException(nameof(command));

		var activity = this.ReserveActivity();

		//如果当前命令已关联外部连接，则保持其连接和事务不变
		if(command.Connection != null && !object.ReferenceEquals(command.Connection, _connection))
		{
			try
			{
				this.OpenConnection(command.Connection);
				return new ConnectionLease(command.Connection, command.Transaction, activity);
			}
			catch
			{
				activity.Dispose();
				throw;
			}
		}

		var lease = this.CreateSessionLease(activity);

		try
		{
			command.Connection = lease.Connection;
			command.Transaction = lease.Transaction;
			return lease;
		}
		catch
		{
			lease.Dispose();
			throw;
		}
	}

	/// <summary>异步准备指定命令的数据连接及其关联事务。</summary>
	/// <param name="command">指定的数据命令。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回指定命令的数据连接租约。</returns>
	private async ValueTask<ConnectionLease> PrepareCommandAsync(DbCommand command, CancellationToken cancellation = default)
	{
		if(command == null)
			throw new ArgumentNullException(nameof(command));

		var activity = this.ReserveActivity();

		//如果当前命令已关联外部连接，则保持其连接和事务不变
		if(command.Connection != null && !object.ReferenceEquals(command.Connection, _connection))
		{
			try
			{
				await this.OpenConnectionAsync(command.Connection, cancellation).ConfigureAwait(false);
				return new ConnectionLease(command.Connection, command.Transaction, activity);
			}
			catch
			{
				await activity.DisposeAsync().ConfigureAwait(false);
				throw;
			}
		}

		var lease = await this.CreateSessionLeaseAsync(activity, cancellation).ConfigureAwait(false);

		try
		{
			command.Connection = lease.Connection;
			command.Transaction = lease.Transaction;
			return lease;
		}
		catch
		{
			await lease.DisposeAsync().ConfigureAwait(false);
			throw;
		}
	}

	/// <summary>准备指定读取命令的数据连接及其关联事务。</summary>
	/// <param name="command">指定的数据读取命令。</param>
	/// <returns>返回指定读取命令的数据连接租约。</returns>
	private ConnectionLease PrepareReader(DbCommand command)
	{
		if(command == null)
			throw new ArgumentNullException(nameof(command));

		var activity = this.ReserveReader();
		var lease = activity?.UseSessionConnection == true ?
			this.CreateSessionLease(activity) : this.CreateIndependentLease(activity);

		try
		{
			command.Connection = lease.Connection;
			command.Transaction = lease.Transaction;
			return lease;
		}
		catch
		{
			lease.Dispose();
			throw;
		}
	}

	/// <summary>异步准备指定读取命令的数据连接及其关联事务。</summary>
	/// <param name="command">指定的数据读取命令。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回指定读取命令的数据连接租约。</returns>
	private async ValueTask<ConnectionLease> PrepareReaderAsync(DbCommand command, CancellationToken cancellation = default)
	{
		if(command == null)
			throw new ArgumentNullException(nameof(command));

		var activity = this.ReserveReader();
		var lease = activity?.UseSessionConnection == true ?
			await this.CreateSessionLeaseAsync(activity, cancellation).ConfigureAwait(false) :
			await this.CreateIndependentLeaseAsync(activity, cancellation).ConfigureAwait(false);

		try
		{
			command.Connection = lease.Connection;
			command.Transaction = lease.Transaction;
			return lease;
		}
		catch
		{
			await lease.DisposeAsync().ConfigureAwait(false);
			throw;
		}
	}

	private ConnectionLease CreateSessionLease(SessionActivity activity)
	{
		try
		{
			_semaphore.Wait();

			try
			{
				var connection = this.EnsureConnection();
				this.OpenConnection(connection);
				return new ConnectionLease(connection, this.EnsureTransaction(connection), activity);
			}
			finally
			{
				_semaphore.Release();
			}
		}
		catch
		{
			activity?.Dispose();
			throw;
		}
	}

	private async ValueTask<ConnectionLease> CreateSessionLeaseAsync(SessionActivity activity, CancellationToken cancellation)
	{
		try
		{
			await _semaphore.WaitAsync(cancellation).ConfigureAwait(false);

			try
			{
				var connection = this.EnsureConnection();
				await this.OpenConnectionAsync(connection, cancellation).ConfigureAwait(false);
				return new ConnectionLease(connection, this.EnsureTransaction(connection), activity);
			}
			finally
			{
				_semaphore.Release();
			}
		}
		catch
		{
			if(activity != null)
				await activity.DisposeAsync().ConfigureAwait(false);

			throw;
		}
	}

	private ConnectionLease CreateIndependentLease(SessionActivity activity)
	{
		try
		{
			var connection = _connector.Connect();
			return new ConnectionLease(connection, null, activity, connection);
		}
		catch
		{
			activity?.Dispose();
			throw;
		}
	}

	private async ValueTask<ConnectionLease> CreateIndependentLeaseAsync(SessionActivity activity, CancellationToken cancellation)
	{
		try
		{
			var connection = await _connector.ConnectAsync(cancellation).ConfigureAwait(false);
			return new ConnectionLease(connection, null, activity, connection);
		}
		catch
		{
			if(activity != null)
				await activity.DisposeAsync().ConfigureAwait(false);

			throw;
		}
	}

	private SessionActivity ReserveActivity()
	{
		lock(_synchrolock)
		{
			if(_completion != COMPLETION_NONE)
				throw new DataException(Properties.Resources.DataSession_Completed_Message);

			_activities++;
			return new SessionActivity(this);
		}
	}

	private ReaderActivity ReserveReader()
	{
		lock(_synchrolock)
		{
			//已完成会话的读取器使用独立连接，不再参与会话生命周期
			if(_completion != COMPLETION_NONE)
				return null;

			_activities++;
			_reading++;
			return new ReaderActivity(this, ShareConnectionSupported || _reading == 1);
		}
	}

	private void ReleaseActivity(SessionActivity activity)
	{
		if(this.ReleaseActivityCore(activity))
			this.Destroy();
	}

	private ValueTask ReleaseActivityAsync(SessionActivity activity)
	{
		return this.ReleaseActivityCore(activity) ?
			this.DestroyAsync(CancellationToken.None) : ValueTask.CompletedTask;
	}

	private bool ReleaseActivityCore(SessionActivity activity)
	{
		lock(_synchrolock)
		{
			if(activity is ReaderActivity)
				_reading--;

			return --_activities == 0 && _completion != COMPLETION_NONE;
		}
	}
	#endregion

	#region 连接事件
	private void Connection_StateChange(object sender, StateChangeEventArgs args)
	{
		if(args.CurrentState == ConnectionState.Closed)
			_transaction = null;
	}
	#endregion

	#region 私有方法
	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
	private void OpenConnection(DbConnection connection)
	{
		if(connection.State == ConnectionState.Closed || connection.State == ConnectionState.Broken)
			_connector.Open(connection);
	}

	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
	private Task OpenConnectionAsync(DbConnection connection, CancellationToken cancellation)
	{
		return connection.State == ConnectionState.Closed || connection.State == ConnectionState.Broken ?
			_connector.OpenAsync(connection, cancellation) : Task.CompletedTask;
	}

	private DbConnection EnsureConnection()
	{
		if(_connection == null)
		{
			_connection = _connector.CreateConnection();

			if(this.TransactionSupported)
				_connection.StateChange += this.Connection_StateChange;
		}

		return _connection;
	}

	private DbTransaction EnsureTransaction(DbConnection connection)
	{
		if(!this.TransactionSupported)
			return null;

		return _transaction ??= connection.BeginTransaction(_ambient?.IsolationLevel ?? IsolationLevel.Unspecified);
	}

	/// <summary>完成当前数据会话。</summary>
	/// <param name="committing">指定是否提交当前数据事务。</param>
	private void Complete(bool committing)
	{
		//标记完成；若已无未结束的数据操作则立即销毁（提交/回滚），
		//否则真实提交/回滚由最后一个活动释放时触发（延迟销毁）。
		if(this.CompleteCore(committing))
			this.Destroy();
	}

	/// <summary>完成当前数据会话。</summary>
	/// <param name="committing">指定是否提交当前数据事务。</param>
	/// <param name="cancellation">指定的异步操作的取消标记。</param>
	private ValueTask CompleteAsync(bool committing, CancellationToken cancellation)
	{
		//标记完成；若已无未结束的数据操作则立即销毁（提交/回滚），
		//否则真实提交/回滚由最后一个活动释放时触发（延迟销毁）。
		return this.CompleteCore(committing) ?
			this.DestroyAsync(cancellation) : ValueTask.CompletedTask;
	}

	private bool CompleteCore(bool committing)
	{
		lock(_synchrolock)
		{
			if(_completion != COMPLETION_NONE)
				return false;

			_completion = committing ? COMPLETION_COMMIT : COMPLETION_ROLLBACK;
			return _activities == 0;
		}
	}

	private void Destroy()
	{
		Exception exception = null;

		try
		{
			//获取并将事务对象置空
			var transaction = Interlocked.Exchange(ref _transaction, null);

			try
			{
				if(transaction != null)
				{
					try
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
					finally
					{
						transaction.Dispose();
					}
				}
			}
			finally
			{
				try
				{
					//获取并将主连接对象置空
					var connection = Interlocked.Exchange(ref _connection, null);

					if(connection != null)
					{
						//取消连接事件处理
						connection.StateChange -= this.Connection_StateChange;

						//释放主数据连接
						connection.Dispose();
					}
				}
				finally
				{
					_semaphore.Dispose();
				}
			}
		}
		catch(Exception ex)
		{
			exception = ex;
		}
		finally
		{
			//发出完成信号，通知等待延迟提交/回滚完成的线程
			if(exception != null)
				_completionSource.TrySetException(exception);
			else
				_completionSource.TrySetResult();
		}

		if(exception != null)
			System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exception).Throw();
	}

	private async ValueTask DestroyAsync(CancellationToken cancellation)
	{
		Exception exception = null;

		try
		{
			//获取并将事务对象置空
			var transaction = Interlocked.Exchange(ref _transaction, null);

			try
			{
				if(transaction != null)
				{
					try
					{
						//尝试提交或回滚事务
						switch(_completion)
						{
							case COMPLETION_COMMIT:
								await transaction.CommitAsync(cancellation).ConfigureAwait(false);
								break;
							case COMPLETION_ROLLBACK:
								await transaction.RollbackAsync(cancellation).ConfigureAwait(false);
								break;
						}
					}
					finally
					{
						await transaction.DisposeAsync().ConfigureAwait(false);
					}
				}
			}
			finally
			{
				try
				{
					//获取并将主连接对象置空
					var connection = Interlocked.Exchange(ref _connection, null);

					if(connection != null)
					{
						//取消连接事件处理
						connection.StateChange -= this.Connection_StateChange;

						//释放主数据连接
						await connection.DisposeAsync().ConfigureAwait(false);
					}
				}
				finally
				{
					_semaphore.Dispose();
				}
			}
		}
		catch(Exception ex)
		{
			exception = ex;
		}
		finally
		{
			//发出完成信号，通知等待延迟提交/回滚完成的线程
			if(exception != null)
				_completionSource.TrySetException(exception);
			else
				_completionSource.TrySetResult();
		}

		if(exception != null)
			System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exception).Throw();
	}
	#endregion

	#region 嵌套子类
	private class SessionActivity(DataSession session) : IDisposable, IAsyncDisposable
	{
		private int _disposed;
		private readonly DataSession _session = session ?? throw new ArgumentNullException(nameof(session));

		public void Dispose()
		{
			if(Interlocked.Exchange(ref _disposed, 1) == 0)
				_session.ReleaseActivity(this);
		}

		public ValueTask DisposeAsync()
		{
			return Interlocked.Exchange(ref _disposed, 1) == 0 ?
				_session.ReleaseActivityAsync(this) : ValueTask.CompletedTask;
		}
	}

	private sealed class ReaderActivity(DataSession session, bool useSessionConnection) : SessionActivity(session)
	{
		public readonly bool UseSessionConnection = useSessionConnection;
	}

	/// <summary>表示一次数据操作实际使用的数据连接及其关联事务的租约。</summary>
	/// <remarks>释放租约会归还其独占连接以及关联的会话活动令牌。</remarks>
	public sealed class ConnectionLease : IDisposable, IAsyncDisposable
	{
		#region 私有变量
		private int _disposed;
		private readonly IDisposable _activity;
		private readonly DbConnection _ownedConnection;
		#endregion

		#region 构造函数
		internal ConnectionLease(DbConnection connection, DbTransaction transaction, IDisposable activity, DbConnection ownedConnection = null)
		{
			this.Connection = connection ?? throw new ArgumentNullException(nameof(connection));
			this.Transaction = transaction;
			_activity = activity;
			_ownedConnection = ownedConnection;
		}
		#endregion

		#region 公共属性
		/// <summary>获取本次数据操作实际使用的数据连接。</summary>
		public DbConnection Connection { get; }

		/// <summary>获取本次数据操作关联的数据事务。</summary>
		public DbTransaction Transaction { get; }
		#endregion

		#region 处置方法
		public void Dispose()
		{
			if(Interlocked.Exchange(ref _disposed, 1) != 0)
				return;

			try
			{
				_ownedConnection?.Dispose();
			}
			finally
			{
				_activity?.Dispose();
			}
		}

		public async ValueTask DisposeAsync()
		{
			if(Interlocked.Exchange(ref _disposed, 1) != 0)
				return;

			try
			{
				if(_ownedConnection != null)
					await _ownedConnection.DisposeAsync().ConfigureAwait(false);
			}
			finally
			{
				if(_activity is IAsyncDisposable activity)
					await activity.DisposeAsync().ConfigureAwait(false);
				else
					_activity?.Dispose();
			}
		}
		#endregion
	}

	private class Enlistment(DataSession session) : Transactions.IEnlistment
	{
		private readonly DataSession _session = session;

		public void OnEnlist(Transactions.EnlistmentContext context)
		{
			if(GetCommit(context, out var commit))
			{
				//标记完成，并等待真实提交/回滚（延迟销毁时阻塞至最后一个活动释放）
				_session.Complete(commit.Value);
				_session.Completion.GetAwaiter().GetResult();
			}
		}

		public async ValueTask OnEnlistAsync(Transactions.EnlistmentContext context, CancellationToken cancellation)
		{
			if(GetCommit(context, out var commit))
			{
				//标记完成，并等待真实提交/回滚（延迟销毁时挂起至最后一个活动释放），
				//以确保"事务提交/回滚完成"的契约在事件投递前成立。
				await _session.CompleteAsync(commit.Value, cancellation).ConfigureAwait(false);
				await _session.Completion.WaitAsync(cancellation).ConfigureAwait(false);
			}
		}

		private static bool GetCommit(Transactions.EnlistmentContext context, out bool? commit)
		{
			commit = null;

			if(context.Phase == Transactions.EnlistmentPhase.Prepare)
				return false;

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

			return commit.HasValue;
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
			//获取当前命令的数据连接租约
			using var lease = _session.PrepareCommand(_command);

			//返回数据命令执行结果
			return _command.ExecuteScalar();
		}

		public override async Task<object> ExecuteScalarAsync(CancellationToken cancellation)
		{
			//获取当前命令的数据连接租约
			await using var lease = await _session.PrepareCommandAsync(_command, cancellation);

			//返回数据命令执行结果
			return await _command.ExecuteScalarAsync(cancellation);
		}

		public override int ExecuteNonQuery()
		{
			//获取当前命令的数据连接租约
			using var lease = _session.PrepareCommand(_command);

			//返回数据命令执行结果
			return _command.ExecuteNonQuery();
		}

		public override async Task<int> ExecuteNonQueryAsync(CancellationToken cancellation)
		{
			//获取当前命令的数据连接租约
			await using var lease = await _session.PrepareCommandAsync(_command, cancellation);

			//返回数据命令执行结果
			return await _command.ExecuteNonQueryAsync(cancellation);
		}

		protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
		{
			//获取当前读取命令的数据连接租约
			var lease = _session.PrepareReader(_command);

			try
			{
				//构建会话数据读取器，由读取器接管连接租约
				var reader = _command.ExecuteReader(behavior & ~CommandBehavior.CloseConnection);
				return new SessionReader(reader, lease);
			}
			catch
			{
				lease.Dispose();
				throw;
			}
		}

		protected override async Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellation)
		{
			//获取当前读取命令的数据连接租约
			var lease = await _session.PrepareReaderAsync(_command, cancellation);

			try
			{
				//构建会话数据读取器，由读取器接管连接租约
				var reader = await _command.ExecuteReaderAsync(behavior & ~CommandBehavior.CloseConnection, cancellation);
				return new SessionReader(reader, lease);
			}
			catch
			{
				await lease.DisposeAsync().ConfigureAwait(false);
				throw;
			}
		}
		#endregion
	}

	private class SessionReader : DbDataReader
	{
		#region 成员字段
		private readonly DbDataReader _reader;
		private int _closed;
		private ConnectionLease _lease;
		#endregion

		#region 构造函数
		internal SessionReader(DbDataReader reader, ConnectionLease lease)
		{
			_reader = reader ?? throw new ArgumentNullException(nameof(reader));
			_lease = lease ?? throw new ArgumentNullException(nameof(lease));
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
			if(Interlocked.Exchange(ref _closed, 1) != 0)
				return;

			try
			{
				//关闭数据读取器
				if(!_reader.IsClosed)
					_reader.Close();
			}
			finally
			{
				//获取并清空当前读取器持有的数据连接租约
				var lease = Interlocked.Exchange(ref _lease, null);

				//释放当前读取器持有的数据连接租约
				lease?.Dispose();
			}
		}

		public override async Task CloseAsync()
		{
			if(Interlocked.Exchange(ref _closed, 1) != 0)
				return;

			try
			{
				//关闭数据读取器
				if(!_reader.IsClosed)
					await _reader.CloseAsync();
			}
			finally
			{
				//获取并清空当前读取器持有的数据连接租约
				var lease = Interlocked.Exchange(ref _lease, null);

				//释放当前读取器持有的数据连接租约
				if(lease != null)
					await lease.DisposeAsync().ConfigureAwait(false);
			}
		}
		#endregion
	}
	#endregion
}
