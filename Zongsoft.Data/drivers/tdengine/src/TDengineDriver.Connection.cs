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

using TDengine.Driver;
using TDengine.Driver.Client;
using TDengine.Data.Client;

namespace Zongsoft.Data.TDengine;

partial class TDengineDriver
{
	private sealed class Connection : DbConnection
	{
		#region 成员字段
		private TDengineConnectionStringBuilder _settings;
		private ITDengineClientAsync _client;
		private ConnectionState _state;
		private Task _closing;
		private string _database;
		private string _version = string.Empty;
		#endregion

		#region 公共成员
		public Connection(string connectionString) => this.ConnectionString = connectionString;

		public override string ConnectionString
		{
			get => _settings.ConnectionString;
			set
			{
				if(_state != ConnectionState.Closed)
					throw new InvalidOperationException(Properties.Resources.Driver_ConnectionClosedRequired_Message);

				_settings = Configuration.TDengineConnectionSettingsDriver.Instance.GetSettings(value).GetOptions();
				_database = _settings.Database;
			}
		}

		public override string Database => _database;
		public override string DataSource => _settings.Host;
		public override string ServerVersion => _version;
		public override ConnectionState State
		{
			get
			{
				if(_state == ConnectionState.Open && _client != null && !_client.ConnectionAvailable())
					this.SetState(ConnectionState.Broken);

				return _state;
			}
		}

		public ITDengineClientAsync Client => _client ?? throw new InvalidOperationException(Properties.Resources.Driver_ConnectionOpenRequired_Message);

		public override void Open() => throw new NotSupportedException(Properties.Resources.Driver_AsyncRequired_Message);
		public override async Task OpenAsync(CancellationToken cancellation)
		{
			cancellation.ThrowIfCancellationRequested();

			if(this.State == ConnectionState.Open)
				return;

			if(_closing != null)
				await _closing.ConfigureAwait(false);
			else if(_client != null)
				await this.CloseAsync().ConfigureAwait(false);

			_closing = null;
			_client = await DbDriver.OpenAsync(_settings, cancellation).ConfigureAwait(false);

			try
			{
				_database = await this.GetMetadataAsync("select database();", cancellation).ConfigureAwait(false);
				_version = await this.GetMetadataAsync("select server_version();", cancellation).ConfigureAwait(false);
				this.SetState(ConnectionState.Open);
			}
			catch
			{
				await this.CloseAsync().ConfigureAwait(false);
				throw;
			}
		}

		private async Task<string> GetMetadataAsync(string text, CancellationToken cancellation)
		{
			try
			{
				await using var rows = await this.Client.QueryAsync(text, cancellation).ConfigureAwait(false);
				return await rows.ReadAsync(cancellation).ConfigureAwait(false) ? rows.GetString(0) : string.Empty;
			}
			catch(OperationCanceledException) when(cancellation.IsCancellationRequested)
			{
				throw;
			}
			catch
			{
				return string.Empty;
			}
		}

		public override void ChangeDatabase(string databaseName) => throw new NotSupportedException(Properties.Resources.Driver_AsyncRequired_Message);
		public override async Task ChangeDatabaseAsync(string databaseName, CancellationToken cancellation = default)
		{
			ArgumentException.ThrowIfNullOrEmpty(databaseName);
			await this.Client.ExecAsync("use `" + databaseName.Replace("`", "``") + "`;", cancellation).ConfigureAwait(false);
			_database = databaseName;
		}

		public override void Close()
		{
			if(_client != null || _closing is { IsCompleted: false })
				throw new NotSupportedException(Properties.Resources.Driver_AsyncRequired_Message);
		}

		public override Task CloseAsync() => _closing ??= this.CloseCoreAsync();
		private async Task CloseCoreAsync()
		{
			var client = _client;
			_client = null;

			try
			{
				if(client != null)
					await client.DisposeAsync().ConfigureAwait(false);
			}
			finally
			{
				this.SetState(ConnectionState.Closed);
			}
		}

		public override async ValueTask DisposeAsync()
		{
			await this.CloseAsync().ConfigureAwait(false);
			base.Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected override void Dispose(bool disposing)
		{
			if(disposing)
				this.Close();

			base.Dispose(disposing);
		}

		protected override DbCommand CreateDbCommand() => new Command { Connection = this };
		protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotSupportedException();

		private void SetState(ConnectionState state)
		{
			var original = _state;
			if(original == state)
				return;

			_state = state;
			this.OnStateChange(new StateChangeEventArgs(original, state));
		}
		#endregion
	}
}
