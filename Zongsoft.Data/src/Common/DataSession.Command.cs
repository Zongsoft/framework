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

namespace Zongsoft.Data.Common;

partial class DataSession
{
	private sealed class SessionCommand : DbCommand
	{
		#region 成员字段
		private readonly object _sync = new();
		private TaskCompletionSource _disposal;
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

		public override void Prepare() => throw new NotSupportedException();
		public override Task PrepareAsync(CancellationToken cancellation = default) => _command.PrepareAsync(cancellation);
		protected override DbParameter CreateDbParameter() => _command.CreateParameter();

		public override object ExecuteScalar() => throw new NotSupportedException();

		public override Task<object> ExecuteScalarAsync(CancellationToken cancellation) =>
			this.ExecuteAsync(_command.ExecuteScalarAsync, cancellation);

		public override int ExecuteNonQuery() => throw new NotSupportedException();

		public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellation) =>
			this.ExecuteAsync(_command.ExecuteNonQueryAsync, cancellation);

		protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => throw new NotSupportedException();

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

		private async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> execute, CancellationToken cancellation)
		{
			await using var lease = await _session.PrepareCommandAsync(_command, cancellation).ConfigureAwait(false);
			return await execute(cancellation).ConfigureAwait(false);
		}
		#endregion

		#region 处置方法
		protected override void Dispose(bool disposing)
		{
			if(disposing && _disposal?.Task.IsCompletedSuccessfully != true)
				throw new NotSupportedException();

			base.Dispose(false);
		}

		public override ValueTask DisposeAsync()
		{
			TaskCompletionSource disposal;

			lock(_sync)
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
				await _command.DisposeAsync().ConfigureAwait(false);
				GC.SuppressFinalize(this);
				disposal.TrySetResult();
			}
			catch(Exception exception)
			{
				disposal.TrySetException(exception);
			}
		}
		#endregion
	}
}
