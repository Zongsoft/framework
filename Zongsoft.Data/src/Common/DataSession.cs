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

using Zongsoft.Data.Features;

namespace Zongsoft.Data.Common;

/// <summary>表示数据操作的会话类。</summary>
public partial class DataSession : IAsyncDisposable
{
	#region 私有变量
	private readonly bool _TransactionSupported_;
	private readonly bool _ShareConnectionSupported_;

	private readonly SemaphoreSlim _semaphore; //表示当前会话连接及事务初始化的同步信号量
	#endregion

	#region 成员字段
	private readonly IDataSource _source;
	private readonly DataConnector _connector;
	private volatile DbConnection _connection;
	private volatile DbTransaction _transaction;
	private readonly TransactionContext _ambient;
	private TaskCompletionSource _disposal;
	#endregion

	#region 构造函数
	public DataSession(IDataSource source, TransactionContext ambient = null)
	{
		_source = source ?? throw new ArgumentNullException(nameof(source));
		_connector = DataConnectorManager.GetConnector(source);
		_ambient = ambient;
		_TransactionSupported_ = source.Features.Get<ITransactionFeature>()?.Suppressed != true;
		_ShareConnectionSupported_ = source.Features.Get<IConnectionSharingFeature>()?.CanShare(source) == true;

		_synchrolock = new object();
		_semaphore = new SemaphoreSlim(1, 1);
		_completionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

		if(_ambient != null && !_ambient.Enlist(new Enlistment(this)))
		{
			_semaphore.Dispose();
			throw new DataException(Properties.Resources.DataSession_AmbientTransactionCompleted_Message);
		}
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

	/// <summary>获取一个值，指示当前会话是否已接受提交或回滚决议。</summary>
	/// <remarks>决议接受后仍可能等待活动租约归还及资源释放；等待终结方法完成后才能确认终结结果。</remarks>
	public bool IsCompleted => _completion != CompletionKind.None;
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

	/// <summary>异步获取当前数据会话的数据连接租约。</summary>
	/// <param name="transactionSuppressed">指示是否禁止关联当前环境事务。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回获取的数据连接租约。</returns>
	public async ValueTask<ConnectionLease> AcquireLeaseAsync(bool transactionSuppressed = false, CancellationToken cancellation = default)
	{
		if(!_TransactionSupported_ || (_ambient != null && transactionSuppressed))
		{
			this.EnsureActive();
			return await this.CreateIndependentLeaseAsync(cancellation).ConfigureAwait(false);
		}

		return await this.CreateSessionLeaseAsync(this.ReserveSession(), cancellation).ConfigureAwait(false);
	}

	/// <summary>提交当前会话事务。</summary>
	/// <param name="cancellation">仅在提交决议被接受前响应的取消令牌；提交开始后不可取消。</param>
	/// <returns>表示会话提交操作的异步任务；位于环境事务中时不执行提交。</returns>
	public async ValueTask CommitAsync(CancellationToken cancellation = default)
	{
		/*
		 * 注意：如果当前会话位于环境事务内，则提交操作必须由环境事务的 Enlistment 回调函数处理，即本方法不做任何处理。
		 */
		if(_ambient != null)
			return;

		/*
		 * 取消语义：仅在提交决议被接受前响应取消（取消已请求则拒绝接受决议并抛 OperationCanceledException，
		 * 会话保持活动，由调用方决定回滚或重试）；决议一经接受，底层提交不可取消（见 FinishAsync）。
		 */
		await this.RequestCompletionAsync(CompletionKind.Commit, cancellation).ConfigureAwait(false);
	}

	/// <summary>回滚当前会话事务。</summary>
	/// <param name="cancellation">保留用于兼容现有调用；回滚属于资源清理操作，不响应此取消标记。</param>
	/// <returns>表示会话回滚操作的异步任务；位于环境事务中时不执行回滚。</returns>
	/// <remarks>回滚一旦发起即不可取消，与 DisposeAsync 保持一致，确保会话资源必然被释放。</remarks>
	public async ValueTask RollbackAsync(CancellationToken cancellation = default)
	{
		/*
		 * 注意：如果当前会话位于环境事务内，则回滚操作必须由环境事务的 Enlistment 回调函数处理，即本方法不做任何处理。
		 */
		if(_ambient != null)
			return;

		await this.RequestCompletionAsync(CompletionKind.Rollback, CancellationToken.None).ConfigureAwait(false);
	}

	public ValueTask DisposeAsync()
	{
		TaskCompletionSource disposal;

		lock(_synchrolock)
		{
			if(_disposal != null)
				return new ValueTask(_disposal.Task);

			_disposal = disposal = new(TaskCreationOptions.RunContinuationsAsynchronously);
		}

		_ = this.DisposeAsyncCore(disposal);
		return new ValueTask(disposal.Task);
	}

	private async Task DisposeAsyncCore(TaskCompletionSource disposal)
	{
		try
		{
			await this.DisposeAsync(true).ConfigureAwait(false);
			GC.SuppressFinalize(this);
			disposal.TrySetResult();
		}
		catch(Exception exception)
		{
			disposal.TrySetException(exception);
		}
	}

	protected virtual async ValueTask DisposeAsync(bool disposing)
	{
		if(disposing)
			await this.RequestCompletionAsync(CompletionKind.Rollback).ConfigureAwait(false);
	}
	#endregion

	#region 连接准备
	/// <summary>异步准备指定命令的数据连接及其关联事务。</summary>
	/// <param name="command">指定的数据命令。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回指定命令的数据连接租约。</returns>
	private async ValueTask<ConnectionLease> PrepareCommandAsync(DbCommand command, CancellationToken cancellation = default)
	{
		if(command == null)
			throw new ArgumentNullException(nameof(command));

		//如果当前命令已关联外部连接，则保持其连接和事务不变
		if(command.Connection != null && !object.ReferenceEquals(command.Connection, _connection))
		{
			this.EnsureActive();
			await this.OpenConnectionAsync(command.Connection, cancellation).ConfigureAwait(false);
			return new ConnectionLease(command.Connection, command.Transaction);
		}

		var lease = await this.CreateSessionLeaseAsync(this.ReserveSession(), cancellation).ConfigureAwait(false);

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

	/// <summary>异步准备指定读取命令的数据连接及其关联事务。</summary>
	/// <param name="command">指定的数据读取命令。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回指定读取命令的数据连接租约。</returns>
	private async ValueTask<ConnectionLease> PrepareReaderAsync(DbCommand command, CancellationToken cancellation = default)
	{
		if(command == null)
			throw new ArgumentNullException(nameof(command));

		var kind = this.ReserveReader();
		var lease = kind == LeaseKind.SessionReader ?
			await this.CreateSessionLeaseAsync(kind, cancellation).ConfigureAwait(false) :
			await this.CreateIndependentLeaseAsync(cancellation).ConfigureAwait(false);

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

	private async ValueTask<ConnectionLease> CreateSessionLeaseAsync(LeaseKind kind, CancellationToken cancellation)
	{
		try
		{
			await _semaphore.WaitAsync(cancellation).ConfigureAwait(false);

			try
			{
				var connection = this.EnsureConnection();
				await this.OpenConnectionAsync(connection, cancellation).ConfigureAwait(false);
				return new ConnectionLease(this, connection, await this.EnsureTransactionAsync(connection, cancellation).ConfigureAwait(false), kind == LeaseKind.SessionReader);
			}
			finally
			{
				_semaphore.Release();
			}
		}
		catch
		{
			await this.ReleaseAsync(kind).ConfigureAwait(false);
			throw;
		}
	}

	private async ValueTask<ConnectionLease> CreateIndependentLeaseAsync(CancellationToken cancellation)
	{
		var connection = await _connector.ConnectAsync(cancellation).ConfigureAwait(false);
		return new ConnectionLease(connection);
	}
	#endregion

	#region 私有方法
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
		}

		return _connection;
	}

	private async ValueTask<DbTransaction> EnsureTransactionAsync(DbConnection connection, CancellationToken cancellation)
	{
		if(!_TransactionSupported_)
			return null;

		return _transaction ??= await connection.BeginTransactionAsync(_ambient?.IsolationLevel ?? IsolationLevel.Unspecified, cancellation).ConfigureAwait(false);
	}
	#endregion
}
