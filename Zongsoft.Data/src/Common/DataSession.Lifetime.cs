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
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Zongsoft.Data.Common;

partial class DataSession
{
	#region 枚举定义
	private enum CompletionKind
	{
		None,
		Commit,
		Rollback,
	}

	private enum LeaseKind
	{
		External,
		Owned,
		Session,
		SessionReader,
	}
	#endregion

	#region 成员字段
	private int _reading;
	private int _activities;
	private volatile CompletionKind _completion;
	private readonly object _synchrolock;
	private readonly TaskCompletionSource _completionSource;
	#endregion

	#region 活动管理
	private void EnsureActive()
	{
		lock(_synchrolock)
		{
			if(_completion != CompletionKind.None)
				throw new DataException(Properties.Resources.DataSession_Completed_Message);
		}
	}

	private LeaseKind ReserveSession()
	{
		lock(_synchrolock)
		{
			if(_completion != CompletionKind.None)
				throw new DataException(Properties.Resources.DataSession_Completed_Message);

			_activities++;
			return LeaseKind.Session;
		}
	}

	private LeaseKind ReserveReader()
	{
		lock(_synchrolock)
		{
			if(_completion != CompletionKind.None)
				return LeaseKind.Owned;

			if(!this.ShareConnectionSupported && _reading > 0)
				return LeaseKind.Owned;

			_reading++;
			_activities++;
			return LeaseKind.SessionReader;
		}
	}

	private bool Exit(LeaseKind kind)
	{
		lock(_synchrolock)
		{
			if(kind == LeaseKind.SessionReader)
				_reading--;

			return --_activities == 0 && _completion != CompletionKind.None;
		}
	}

	private void Release(LeaseKind kind)
	{
		if(!this.Exit(kind))
			return;

		Throw(this.Finish());
	}

	private ValueTask ReleaseAsync(LeaseKind kind)
	{
		return this.Exit(kind) ? this.ReleaseAsyncCore() : ValueTask.CompletedTask;
	}

	private async ValueTask ReleaseAsyncCore()
	{
		Throw(await this.FinishAsync().ConfigureAwait(false));
	}

	private void ReleaseFailed(LeaseKind kind, Exception exception)
	{
		try
		{
			this.Release(kind);
		}
		catch(Exception ex)
		{
			throw Combine(exception, ex);
		}
	}

	private async ValueTask ReleaseFailedAsync(LeaseKind kind, Exception exception)
	{
		try
		{
			await this.ReleaseAsync(kind).ConfigureAwait(false);
		}
		catch(Exception ex)
		{
			throw Combine(exception, ex);
		}
	}
	#endregion

	#region 完成管理
	private void RequestCompletion(CompletionKind completion)
	{
		if(this.TryComplete(completion))
			Throw(this.Finish());
	}

	private ValueTask RequestCompletionAsync(CompletionKind completion, CancellationToken cancellation = default)
	{
		return this.TryComplete(completion, cancellation) ? this.RequestCompletionAsyncCore() : ValueTask.CompletedTask;
	}

	private async ValueTask RequestCompletionAsyncCore()
	{
		Throw(await this.FinishAsync().ConfigureAwait(false));
	}

	private void CompleteAndWait(CompletionKind completion)
	{
		if(this.TryComplete(completion))
			this.Finish();

		_completionSource.Task.GetAwaiter().GetResult();
	}

	private async ValueTask CompleteAndWaitAsync(CompletionKind completion)
	{
		if(this.TryComplete(completion))
			await this.FinishAsync().ConfigureAwait(false);

		await _completionSource.Task.ConfigureAwait(false);
	}

	private bool TryComplete(CompletionKind completion, CancellationToken cancellation = default)
	{
		lock(_synchrolock)
		{
			if(_completion != CompletionKind.None)
				return false;

			cancellation.ThrowIfCancellationRequested();
			_completion = completion;
			return _activities == 0;
		}
	}
	#endregion

	#region 资源终结
	private Exception Finish()
	{
		var transaction = Interlocked.Exchange(ref _transaction, null);
		var connection = Interlocked.Exchange(ref _connection, null);
		Exception exception = null;
		List<Exception> exceptions = null;

		if(transaction != null)
		{
			try
			{
				if(_completion == CompletionKind.Commit)
					transaction.Commit();
				else if(_completion == CompletionKind.Rollback)
					transaction.Rollback();
			}
			catch(Exception ex)
			{
				Add(ref exception, ref exceptions, ex);
			}

			try
			{
				transaction.Dispose();
			}
			catch(Exception ex)
			{
				Add(ref exception, ref exceptions, ex);
			}
		}

		if(connection != null)
		{
			try
			{
				connection.StateChange -= this.Connection_StateChange;
			}
			catch(Exception ex)
			{
				Add(ref exception, ref exceptions, ex);
			}

			try
			{
				connection.Dispose();
			}
			catch(Exception ex)
			{
				Add(ref exception, ref exceptions, ex);
			}
		}

		try
		{
			_semaphore.Dispose();
		}
		catch(Exception ex)
		{
			Add(ref exception, ref exceptions, ex);
		}

		return this.NotifyCompletion(ToException(exception, exceptions));
	}

	private async ValueTask<Exception> FinishAsync()
	{
		var transaction = Interlocked.Exchange(ref _transaction, null);
		var connection = Interlocked.Exchange(ref _connection, null);
		Exception exception = null;
		List<Exception> exceptions = null;

		if(transaction != null)
		{
			try
			{
				if(_completion == CompletionKind.Commit)
					await transaction.CommitAsync(CancellationToken.None).ConfigureAwait(false);
				else if(_completion == CompletionKind.Rollback)
					await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
			}
			catch(Exception ex)
			{
				Add(ref exception, ref exceptions, ex);
			}

			try
			{
				await transaction.DisposeAsync().ConfigureAwait(false);
			}
			catch(Exception ex)
			{
				Add(ref exception, ref exceptions, ex);
			}
		}

		if(connection != null)
		{
			try
			{
				connection.StateChange -= this.Connection_StateChange;
			}
			catch(Exception ex)
			{
				Add(ref exception, ref exceptions, ex);
			}

			try
			{
				await connection.DisposeAsync().ConfigureAwait(false);
			}
			catch(Exception ex)
			{
				Add(ref exception, ref exceptions, ex);
			}
		}

		try
		{
			_semaphore.Dispose();
		}
		catch(Exception ex)
		{
			Add(ref exception, ref exceptions, ex);
		}

		return this.NotifyCompletion(ToException(exception, exceptions));
	}

	private Exception NotifyCompletion(Exception exception)
	{
		if(exception == null)
			_completionSource?.TrySetResult();
		else
			_completionSource?.TrySetException(exception);

		return exception;
	}
	#endregion

	#region 异常处理
	private static void Add(ref Exception exception, ref List<Exception> exceptions, Exception error)
	{
		if(exception == null)
		{
			exception = error;
			return;
		}

		if(exceptions == null)
			exceptions = [exception];

		exceptions.Add(error);
	}

	private static Exception ToException(Exception exception, List<Exception> exceptions) =>
		exceptions == null ? exception : new AggregateException(exceptions);

	private static Exception Combine(Exception exception, Exception cleanup)
	{
		if(cleanup is AggregateException aggregate)
		{
			var exceptions = new List<Exception>(aggregate.InnerExceptions.Count + 1) { exception };
			exceptions.AddRange(aggregate.InnerExceptions);
			return new AggregateException(exceptions);
		}

		return new AggregateException(exception, cleanup);
	}

	private static void Throw(Exception exception)
	{
		if(exception != null)
			System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exception).Throw();
	}
	#endregion

	#region 嵌套子类
	/// <summary>表示一次数据操作实际使用的数据连接及其关联事务的租约。</summary>
	/// <remarks>释放租约会归还其独占连接或关联的会话活动。</remarks>
	public sealed class ConnectionLease : IDisposable, IAsyncDisposable
	{
		#region 成员字段
		private int _disposed;
		private readonly LeaseKind _kind;
		private readonly DataSession _session;
		#endregion

		#region 构造函数
		internal ConnectionLease(DbConnection connection)
		{
			_kind = LeaseKind.Owned;
			this.Connection = connection ?? throw new ArgumentNullException(nameof(connection));
		}

		internal ConnectionLease(DbConnection connection, DbTransaction transaction)
		{
			_kind = LeaseKind.External;
			this.Connection = connection ?? throw new ArgumentNullException(nameof(connection));
			this.Transaction = transaction;
		}

		internal ConnectionLease(DataSession session, DbConnection connection, DbTransaction transaction, bool reading)
		{
			_session = session ?? throw new ArgumentNullException(nameof(session));
			_kind = reading ? LeaseKind.SessionReader : LeaseKind.Session;
			this.Connection = connection ?? throw new ArgumentNullException(nameof(connection));
			this.Transaction = transaction;
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

			if(_kind == LeaseKind.Owned)
				this.Connection.Dispose();
			else if(_kind is LeaseKind.Session or LeaseKind.SessionReader)
				_session.Release(_kind);
		}

		public ValueTask DisposeAsync()
		{
			if(Interlocked.Exchange(ref _disposed, 1) != 0)
				return ValueTask.CompletedTask;

			return _kind switch
			{
				LeaseKind.Owned => this.Connection.DisposeAsync(),
				LeaseKind.Session or LeaseKind.SessionReader => _session.ReleaseAsync(_kind),
				_ => ValueTask.CompletedTask,
			};
		}
		#endregion
	}
	#endregion
}
