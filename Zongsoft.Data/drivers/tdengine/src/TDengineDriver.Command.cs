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
 * This file is part of Zongsoft.Data.TDengine library.
 *
 * The Zongsoft.Data.TDengine is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data.TDengine is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data.TDengine library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using TDengine.Driver;
using TDengine.Data.Client;

namespace Zongsoft.Data.TDengine;

partial class TDengineDriver
{
	private sealed class Command : DbCommand
	{
		#region 成员字段
		private readonly TDengineParameterCollection _parameters = new();
		private Connection _connection;
		private IStmtAsync _statement;
		private string _text;
		private bool _prepared;
		private Task _disposing;
		private Task _releasing;
		#endregion

		#region 公共成员
		public override string CommandText
		{
			get => _text;
			set
			{
				_text = value;
				_prepared = false;
			}
		}

		public override int CommandTimeout { get; set; }
		public override bool DesignTimeVisible { get; set; }
		public override UpdateRowSource UpdatedRowSource { get; set; }
		public override CommandType CommandType
		{
			get => CommandType.Text;
			set
			{
				if(value != CommandType.Text)
					throw new ArgumentException(Properties.Resources.Driver_TextCommandRequired_Message, nameof(value));
			}
		}

		protected override DbConnection DbConnection
		{
			get => _connection;
			set
			{
				if(value != null && value is not TDengineDriver.Connection)
					throw new ArgumentException(Properties.Resources.Driver_ConnectionTypeRequired_Message, nameof(value));

				if(!ReferenceEquals(_connection, value))
					_prepared = false;

				_connection = (Connection)value;
			}
		}

		protected override DbTransaction DbTransaction { get; set; }
		protected override DbParameterCollection DbParameterCollection => _parameters;
		protected override DbParameter CreateDbParameter() => new TDengineParameter();

		public override void Cancel() => throw new NotSupportedException(Properties.Resources.Driver_AsyncRequired_Message);
		public override void Prepare() => throw new NotSupportedException(Properties.Resources.Driver_AsyncRequired_Message);
		public override Task PrepareAsync(CancellationToken cancellation = default)
		{
			this.Validate(cancellation);
			return Task.CompletedTask;
		}

		public override int ExecuteNonQuery() => throw new NotSupportedException(Properties.Resources.Driver_AsyncRequired_Message);
		public override object ExecuteScalar() => throw new NotSupportedException(Properties.Resources.Driver_AsyncRequired_Message);
		protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => throw new NotSupportedException(Properties.Resources.Driver_AsyncRequired_Message);

		public override async Task<int> ExecuteNonQueryAsync(CancellationToken cancellation)
		{
			await using var rows = await this.ExecuteAsync(cancellation).ConfigureAwait(false);
			return rows.AffectRows;
		}

		public override async Task<object> ExecuteScalarAsync(CancellationToken cancellation)
		{
			await using var rows = await this.ExecuteAsync(cancellation).ConfigureAwait(false);
			return await rows.ReadAsync(cancellation).ConfigureAwait(false) ? rows.GetValue(0) : null;
		}

		protected override async Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellation) =>
			new Reader(await this.ExecuteAsync(cancellation).ConfigureAwait(false), (behavior & CommandBehavior.CloseConnection) != 0 ? _connection : null);

		private async Task<IRowsAsync> ExecuteAsync(CancellationToken cancellation)
		{
			this.Validate(cancellation);

			if(!_prepared && _statement != null)
			{
				_releasing ??= _statement.DisposeAsync().AsTask();
				await _releasing.ConfigureAwait(false);
				_statement = null;
				_releasing = null;
			}

			if(_parameters.Count == 0)
				return await _connection.Client.QueryAsync(_text, cancellation).ConfigureAwait(false);

			_statement ??= await _connection.Client.StmtInitAsync(cancellation).ConfigureAwait(false);

			if(!_prepared)
			{
				await _statement.PrepareAsync(_text, cancellation).ConfigureAwait(false);
				_prepared = true;
			}

			var table = string.Empty;
			var tags = new List<object>();
			var data = new List<object>();

			foreach(DbParameter parameter in _parameters)
			{
				switch(string.IsNullOrEmpty(parameter.ParameterName) ? '\0' : parameter.ParameterName[0])
				{
					case '$':
						tags.Add(parameter.Value);
						break;
					case '@':
						data.Add(parameter.Value);
						break;
					case '#':
						if(!string.IsNullOrEmpty(table))
							throw new ArgumentException(Properties.Resources.Driver_TableNameDuplicate_Message);

						table = parameter.Value as string;
						break;
					default:
						throw new ArgumentException(Properties.Resources.Driver_ParameterNameInvalid_Message);
				}
			}

			if(_statement.IsInsert())
			{
				if(!string.IsNullOrEmpty(table))
					await _statement.SetTableNameAsync(table, cancellation).ConfigureAwait(false);

				if(tags.Count > 0)
					await _statement.SetTagsAsync(tags.ToArray(), cancellation).ConfigureAwait(false);
			}

			if(data.Count > 0)
				await _statement.BindRowAsync(data.ToArray(), cancellation).ConfigureAwait(false);

			await _statement.AddBatchAsync(cancellation).ConfigureAwait(false);
			await _statement.ExecAsync(cancellation).ConfigureAwait(false);
			return await _statement.ResultAsync(cancellation).ConfigureAwait(false);
		}

		private void Validate(CancellationToken cancellation)
		{
			ObjectDisposedException.ThrowIf(_disposing != null, this);
			cancellation.ThrowIfCancellationRequested();

			if(_connection?.State != ConnectionState.Open)
				throw new InvalidOperationException(Properties.Resources.Driver_ConnectionOpenRequired_Message);

			if(string.IsNullOrEmpty(_text))
				throw new InvalidOperationException(Properties.Resources.Driver_CommandTextRequired_Message);
		}

		public override ValueTask DisposeAsync() => new(_disposing ??= this.DisposeCoreAsync());
		private async Task DisposeCoreAsync()
		{
			var statement = _statement;
			_statement = null;

			try
			{
				if(statement != null)
					await (_releasing ??= statement.DisposeAsync().AsTask()).ConfigureAwait(false);
			}
			finally
			{
				base.Dispose(true);
				GC.SuppressFinalize(this);
			}
		}

		protected override void Dispose(bool disposing)
		{
			if(disposing && (_statement != null || _disposing is { IsCompleted: false }))
				throw new NotSupportedException(Properties.Resources.Driver_AsyncRequired_Message);

			base.Dispose(disposing);
		}
		#endregion
	}
}
