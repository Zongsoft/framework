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
using System.Collections.Concurrent;
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

	private volatile int _reading;    //表示当前会话已经打开的数据读取器的数量
	private volatile int _leasing;    //表示当前会话尚未释放的主连接租约数量
	private volatile int _completion; //表示当前会话是否已经结束(提交或回滚)的标记
	private TaskCompletionSource _destruction; //表示当前会话的事务及连接资源释放操作及其结果（为空则尚未开始）
	private readonly AutoResetEvent _semaphore; //表示当前会话结束与连接操作的同步信号量
	private readonly ConcurrentBag<IDbCommand> _commands; //表示待关联事务的命令对象集
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

		_semaphore = new AutoResetEvent(true);
		_commands = new ConcurrentBag<IDbCommand>();

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
	public bool IsCompleted => _completion != COMPLETION_NONE;

	/// <summary>获取一个值，指示当前会话是否还有“读取中或待读取”的读取器。</summary>
	public bool IsReading => _reading > 0;

	/// <summary>获取一个值，指示当前会话是否还有尚未释放的主连接租约。</summary>
	public bool IsLeasing => _leasing > 0;
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
		if(!this.TransactionSupported || (this.InTransaction && transactionSuppressed))
			return new ConnectionLease(this, _connector.Connect(), null, LeaseBehavior.OwnsConnection);

		var connection = this.RetainConnection();

		try
		{
			this.OpenConnection(connection);
			return new ConnectionLease(this, connection, _transaction, LeaseBehavior.RetainsSession);
		}
		catch
		{
			this.ReleaseLease();
			throw;
		}
	}

	/// <summary>异步获取当前数据会话的数据连接租约。</summary>
	/// <param name="transactionSuppressed">指示是否禁止关联当前环境事务。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回获取的数据连接租约。</returns>
	public async ValueTask<ConnectionLease> AcquireLeaseAsync(bool transactionSuppressed = false, CancellationToken cancellation = default)
	{
		if(!this.TransactionSupported || (this.InTransaction && transactionSuppressed))
			return new ConnectionLease(this, await _connector.ConnectAsync(cancellation).ConfigureAwait(false), null, LeaseBehavior.OwnsConnection);

		var connection = this.RetainConnection();

		try
		{
			await this.OpenConnectionAsync(connection, cancellation).ConfigureAwait(false);
			return new ConnectionLease(this, connection, _transaction, LeaseBehavior.RetainsSession);
		}
		catch
		{
			await this.ReleaseLeaseAsync().ConfigureAwait(false);
			throw;
		}
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
	#endregion

	#region 连接准备
	/// <summary>绑定指定命令的数据连接，并关联命令的数据事务。</summary>
	/// <param name="command">指定要绑定的命令对象。</param>
	/// <param name="retain">指示是否保留当前会话的主连接。</param>
	/// <param name="force">指示是否强制将指定命令绑定到当前会话的主连接。</param>
	/// <returns>如果指定命令绑定到当前会话的主连接则返回真，否则返回假。</returns>
	private bool Bind(IDbCommand command, bool retain, bool force)
	{
		//等待信号量
		_semaphore.WaitOne();

		try
		{
			if(this.IsCompleted)
				throw new DataException(Properties.Resources.DataSession_Completed_Message);

			//如果不强制绑定并且当前命令已关联外部连接，则保持原有连接不变
			if(!force && command.Connection != null && !object.ReferenceEquals(command.Connection, _connection))
				return false;

			//设置当前命令的连接为当前会话的主连接
			command.Connection = this.EnsureConnection();

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

			if(retain)
				Interlocked.Increment(ref _leasing);

			return true;
		}
		finally
		{
			//释放当前持有的信号
			_semaphore.Set();
		}
	}

	/// <summary>准备指定命令的数据连接及其关联事务。</summary>
	/// <param name="command">指定的数据命令。</param>
	/// <returns>返回指定命令的数据连接租约。</returns>
	private ConnectionLease PrepareCommand(DbCommand command)
	{
		if(command == null)
			throw new ArgumentNullException(nameof(command));

		var retained = this.Bind(command, retain: true, force: false);

		try
		{
			this.OpenConnection(command.Connection);
			return new ConnectionLease(this, command.Connection, command.Transaction, retained ? LeaseBehavior.RetainsSession : LeaseBehavior.None);
		}
		catch
		{
			if(retained)
				this.ReleaseLease();

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

		var retained = this.Bind(command, retain: true, force: false);

		try
		{
			await this.OpenConnectionAsync(command.Connection, cancellation).ConfigureAwait(false);
			return new ConnectionLease(this, command.Connection, command.Transaction, retained ? LeaseBehavior.RetainsSession : LeaseBehavior.None);
		}
		catch
		{
			if(retained)
				await this.ReleaseLeaseAsync().ConfigureAwait(false);

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

		ConnectionLease lease = null;
		var tracked = this.TryRetainReader(out var reading);

		try
		{
			//如果当前会话已经完成，则数据读取器应构建独属的连接
			if(!tracked)
				return AcquireIndependent(command, LeaseBehavior.OwnsConnection);

			//如果当前会话不支持多活动结果集且主连接已被其他读取器占用，则只能创建新的连接
			if(!ShareConnectionSupported && reading > 1)
				return AcquireIndependent(command, LeaseBehavior.OwnsConnection | LeaseBehavior.TracksReader);

			this.Bind(command, retain: false, force: true);
			this.OpenConnection(command.Connection);
			return new ConnectionLease(this, command.Connection, command.Transaction, LeaseBehavior.TracksReader);
		}
		catch
		{
			if(lease != null)
				lease.Dispose();
			else if(tracked)
				this.ReleaseReader();

			throw;
		}

		ConnectionLease AcquireIndependent(DbCommand command, LeaseBehavior behavior)
		{
			var connection = _connector.CreateConnection();
			command.Connection = connection;
			command.Transaction = null;

			try
			{
				this.OpenConnection(connection);
				lease = new ConnectionLease(this, connection, null, behavior);
				return lease;
			}
			catch
			{
				command.Connection = null;
				connection.Dispose();
				throw;
			}
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

		ConnectionLease lease = null;
		var tracked = this.TryRetainReader(out var reading);

		try
		{
			//如果当前会话已经完成，则数据读取器应构建独属的连接
			if(!tracked)
				return await AcquireIndependentAsync(command, LeaseBehavior.OwnsConnection, cancellation).ConfigureAwait(false);

			//如果当前会话不支持多活动结果集且主连接已被其他读取器占用，则只能创建新的连接
			if(!ShareConnectionSupported && reading > 1)
				return await AcquireIndependentAsync(command, LeaseBehavior.OwnsConnection | LeaseBehavior.TracksReader, cancellation).ConfigureAwait(false);

			this.Bind(command, retain: false, force: true);
			await this.OpenConnectionAsync(command.Connection, cancellation).ConfigureAwait(false);
			return new ConnectionLease(this, command.Connection, command.Transaction, LeaseBehavior.TracksReader);
		}
		catch
		{
			if(lease != null)
				await lease.DisposeAsync().ConfigureAwait(false);
			else if(tracked)
				await this.ReleaseReaderAsync().ConfigureAwait(false);

			throw;
		}

		async ValueTask<ConnectionLease> AcquireIndependentAsync(DbCommand command, LeaseBehavior behavior, CancellationToken cancellation)
		{
			var connection = _connector.CreateConnection();
			command.Connection = connection;
			command.Transaction = null;

			try
			{
				await this.OpenConnectionAsync(connection, cancellation).ConfigureAwait(false);
				lease = new ConnectionLease(this, connection, null, behavior);
				return lease;
			}
			catch
			{
				command.Connection = null;
				await connection.DisposeAsync().ConfigureAwait(false);
				throw;
			}
		}
	}

	private bool TryRetainReader(out int reading)
	{
		//已完成会话的读取器使用独立连接，不再参与会话跟踪
		if(this.IsCompleted)
		{
			reading = 0;
			return false;
		}

		_semaphore.WaitOne();

		try
		{
			if(this.IsCompleted)
			{
				reading = 0;
				return false;
			}

			reading = Interlocked.Increment(ref _reading);
			return true;
		}
		finally
		{
			_semaphore.Set();
		}
	}

	private void ReleaseReader()
	{
		//递减“执行中”的数据读取器数量
		var reading = Interlocked.Decrement(ref _reading);

		//只有当“执行中”的数据读取器都没有了，并且当前会话已经结束才能提交事务及释放所有资源
		if(reading <= 0 && !this.IsLeasing && this.IsCompleted)
			this.Destroy();
	}

	private ValueTask ReleaseReaderAsync(CancellationToken cancellation = default)
	{
		//递减“执行中”的数据读取器数量
		var reading = Interlocked.Decrement(ref _reading);

		//只有当“执行中”的数据读取器都没有了，并且当前会话已经结束才能提交事务及释放所有资源
		if(reading <= 0 && !this.IsLeasing && this.IsCompleted)
			return this.DestroyAsync(cancellation);
		else
			return ValueTask.CompletedTask;
	}

	private void ReleaseLease()
	{
		var leasing = Interlocked.Decrement(ref _leasing);

		if(leasing <= 0 && !this.IsReading && this.IsCompleted)
			this.Destroy();
	}

	private ValueTask ReleaseLeaseAsync(CancellationToken cancellation = default)
	{
		var leasing = Interlocked.Decrement(ref _leasing);

		return leasing <= 0 && !this.IsReading && this.IsCompleted ?
			this.DestroyAsync(cancellation) : ValueTask.CompletedTask;
	}
	#endregion

	#region 连接事件
	private void Connection_StateChange(object sender, StateChangeEventArgs e)
	{
		var connection = (DbConnection)sender;

		switch(e.CurrentState)
		{
			case ConnectionState.Open:
				//连接完成则开启一个事务
				_transaction = connection.BeginTransaction(_ambient?.IsolationLevel ?? IsolationLevel.Unspecified);

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

	private DbConnection RetainConnection()
	{
		//等待信号量
		_semaphore.WaitOne();

		try
		{
			if(this.IsCompleted)
				throw new DataException(Properties.Resources.DataSession_Completed_Message);

			var connection = this.EnsureConnection();
			Interlocked.Increment(ref _leasing);
			return connection;
		}
		finally
		{
			//释放当前持有的信号
			_semaphore.Set();
		}
	}

	private DbConnection EnsureConnection()
	{
		if(_connection != null)
			return _connection;

		lock(this)
		{
			if(_connection == null)
			{
				_connection = _connector.CreateConnection();

				if(this.TransactionSupported)
					_connection.StateChange += this.Connection_StateChange;
			}
		}

		return _connection;
	}

	/// <summary>完成当前数据会话。</summary>
	/// <param name="committing">指定是否提交当前数据事务。</param>
	/// <returns>如果当前会话已经完成了则返回假(False)，否则返回真(True)。</returns>
	private bool Complete(bool committing)
	{
		//设置完成标记
		var completed = Interlocked.CompareExchange(ref _completion, committing ? COMPLETION_COMMIT : COMPLETION_ROLLBACK, COMPLETION_NONE);

		//如果已经完成过则返回
		if(completed != COMPLETION_NONE)
			return false;

		//等待信号量
		_semaphore.WaitOne();

		try
		{
			//如果还有活动的读取器或主连接租约则不能提交事务及释放资源
			if(this.IsReading || this.IsLeasing)
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
		var completed = Interlocked.CompareExchange(ref _completion, committing ? COMPLETION_COMMIT : COMPLETION_ROLLBACK, COMPLETION_NONE);

		//如果已经完成过则返回
		if(completed != COMPLETION_NONE)
			return false;

		//等待信号量
		_semaphore.WaitOne();

		try
		{
			//如果还有活动的读取器或主连接租约则不能提交事务及释放资源
			if(this.IsReading || this.IsLeasing)
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

	private void Destroy()
	{
		var destruction = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var current = Interlocked.CompareExchange(ref _destruction, destruction, null);

		if(current != null)
		{
			current.Task.GetAwaiter().GetResult();
			return;
		}

		try
		{
			this.DestroyCore();
			destruction.TrySetResult();
		}
		catch(Exception exception)
		{
			destruction.TrySetException(exception);
			_ = destruction.Task.Exception;
			throw;
		}
	}

	private async ValueTask DestroyAsync(CancellationToken cancellation)
	{
		var destruction = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var current = Interlocked.CompareExchange(ref _destruction, destruction, null);

		if(current != null)
		{
			await current.Task.ConfigureAwait(false);
			return;
		}

		try
		{
			await this.DestroyCoreAsync(cancellation).ConfigureAwait(false);
			destruction.TrySetResult();
		}
		catch(Exception exception)
		{
			destruction.TrySetException(exception);
			_ = destruction.Task.Exception;
			throw;
		}
	}

	private void DestroyCore()
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
	}

	private async ValueTask DestroyCoreAsync(CancellationToken cancellation)
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
	}
	#endregion

	#region 嵌套子类
	[Flags]
	internal enum LeaseBehavior : byte
	{
		None = 0,
		OwnsConnection = 1,
		RetainsSession = 2,
		TracksReader = 4,
	}

	/// <summary>表示一次数据操作实际使用的数据连接及其关联事务的租约。</summary>
	/// <remarks>释放租约会归还其独占连接以及关联的会话或读取器生命周期令牌。</remarks>
	public sealed class ConnectionLease : IDisposable, IAsyncDisposable
	{
		#region 私有变量
		private int _disposed;
		private readonly DataSession _session;
		private readonly LeaseBehavior _behavior;
		#endregion

		#region 构造函数
		internal ConnectionLease(DataSession session, DbConnection connection, DbTransaction transaction, LeaseBehavior behavior)
		{
			_session = session ?? throw new ArgumentNullException(nameof(session));
			this.Connection = connection ?? throw new ArgumentNullException(nameof(connection));
			this.Transaction = transaction;
			_behavior = behavior;
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
				if((_behavior & LeaseBehavior.OwnsConnection) != 0)
					this.Connection.Dispose();
			}
			finally
			{
				if((_behavior & LeaseBehavior.TracksReader) != 0)
					_session.ReleaseReader();

				if((_behavior & LeaseBehavior.RetainsSession) != 0)
					_session.ReleaseLease();
			}
		}

		public async ValueTask DisposeAsync()
		{
			if(Interlocked.Exchange(ref _disposed, 1) != 0)
				return;

			try
			{
				if((_behavior & LeaseBehavior.OwnsConnection) != 0)
					await this.Connection.DisposeAsync().ConfigureAwait(false);
			}
			finally
			{
				if((_behavior & LeaseBehavior.TracksReader) != 0)
					await _session.ReleaseReaderAsync().ConfigureAwait(false);

				if((_behavior & LeaseBehavior.RetainsSession) != 0)
					await _session.ReleaseLeaseAsync().ConfigureAwait(false);
			}
		}
		#endregion
	}

	private class Enlistment(DataSession session) : Transactions.IEnlistment
	{
		private readonly DataSession _session = session;

		public void OnEnlist(Transactions.EnlistmentContext context)
		{
			if(context.Phase == Transactions.EnlistmentPhase.Prepare)
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
