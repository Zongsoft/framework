using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Zongsoft.Data.Common;
using Zongsoft.Data.Features;
using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.Tests;

public partial class DataConnectorTest
{
	internal class DataDriverMocker : DataDriverBase
	{
		private readonly ConcurrentQueue<DbConnectionMocker> _connections = new();
		private readonly ConcurrentQueue<DbCommandMocker> _commands = new();

		public DataDriverMocker(Func<CancellationToken, Task> open = null, bool readerFailure = false, TerminationFailure terminationFailures = TerminationFailure.None)
		{
			this.Open = open ?? (_ => Task.CompletedTask);
			this.ReaderFailure = readerFailure;

			foreach(var failure in new[] { TerminationFailure.Commit, TerminationFailure.Rollback, TerminationFailure.TransactionDispose, TerminationFailure.ConnectionDispose })
			{
				if(terminationFailures.HasFlag(failure))
					this.Errors.Add(failure, new InvalidOperationException());
			}
		}

		public bool ReaderFailure { get; }
		public Func<CancellationToken, Task> Open { get; }
		public Func<CancellationToken, Task> Begin { get; set; }
		public Func<CancellationToken, Task> Commit { get; set; }
		public Func<CancellationToken, Task> Rollback { get; set; }
		public Func<CancellationToken, Task> Execute { get; set; }
		public Func<CancellationToken, Task<bool>> Read { get; set; }
		public Func<CancellationToken, Task<bool>> NextResult { get; set; }
		public Func<Task> ConnectionDispose { get; set; }
		public Func<Task> TransactionDispose { get; set; }
		public Func<Task> CommandDispose { get; set; }
		public Func<Task> ReaderDispose { get; set; }
		public Dictionary<TerminationFailure, Exception> Errors { get; } = new();
		public override string Name => "Mock";
		public override IStatementBuilder Builder => null;
		public DbConnectionMocker[] Connections => _connections.ToArray();
		public DbCommandMocker[] Commands => _commands.ToArray();
		public override DbCommand CreateCommand(string text, CommandType commandType = CommandType.Text) => throw new InvalidOperationException("A synchronous provider entry point was used.");
		public override DbCommand CreateCommand(IDataAccessContextBase context, IStatementBase statement)
		{
			var command = new DbCommandMocker(this);
			_commands.Enqueue(command);
			return command;
		}
		public override DbConnection CreateConnection(string connectionString = null)
		{
			var connection = new DbConnectionMocker(this);
			_connections.Enqueue(connection);
			return connection;
		}
		public override DbConnectionStringBuilder CreateConnectionBuilder(string connectionString = null) => new() { ConnectionString = connectionString };
		protected override IDataImporter CreateImporter() => null;
		protected override ExpressionVisitorBase CreateVisitor() => null;
		public void ThrowIfRequested(TerminationFailure failure)
		{
			if(this.Errors.TryGetValue(failure, out var exception))
				throw exception;
		}
	}

	internal sealed class DbConnectionMocker(DataDriverMocker driver) : DbConnection
	{
		private ConnectionState _state;
		private int _disposeCount;
		private int _openCount;
		private int _beginCount;
		public override string ConnectionString { get; set; }
		public override string Database => "Test";
		public override string DataSource => "Test";
		public override string ServerVersion => "1.0";
		public override ConnectionState State => _state;
		public int DisposeCount => _disposeCount;
		public int OpenCount => _openCount;
		public int BeginCount => _beginCount;
		public bool IsDisposed => _disposeCount != 0;
		public DbTransactionMocker Transaction { get; private set; }
		public override void ChangeDatabase(string databaseName) => throw new InvalidOperationException("A synchronous provider entry point was used.");
		public override void Close() => throw new InvalidOperationException("A synchronous provider entry point was used.");
		public override Task CloseAsync()
		{
			this.SetState(ConnectionState.Closed);
			return Task.CompletedTask;
		}
		public override void Open() => throw new InvalidOperationException("A synchronous provider entry point was used.");
		public override async Task OpenAsync(CancellationToken cancellationToken)
		{
			Interlocked.Increment(ref _openCount);
			await driver.Open(cancellationToken);
			this.SetState(ConnectionState.Open);
		}
		protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new InvalidOperationException("A synchronous provider entry point was used.");
		protected override async ValueTask<DbTransaction> BeginDbTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken)
		{
			Interlocked.Increment(ref _beginCount);

			if(driver.Begin != null)
				await driver.Begin(cancellationToken);
			return this.Transaction = new DbTransactionMocker(this, isolationLevel, driver);
		}
		protected override DbCommand CreateDbCommand() => throw new InvalidOperationException("A synchronous provider entry point was used.");
		protected override void Dispose(bool disposing)
		{
			if(disposing)
				throw new InvalidOperationException("A synchronous provider entry point was used.");
		}
		public override async ValueTask DisposeAsync()
		{
			Interlocked.Increment(ref _disposeCount);

			if(driver.ConnectionDispose != null)
				await driver.ConnectionDispose();
			this.SetState(ConnectionState.Closed);
			driver.ThrowIfRequested(TerminationFailure.ConnectionDispose);
		}
		private void SetState(ConnectionState state)
		{
			var original = _state;
			_state = state;

			if(original != state)
				this.OnStateChange(new StateChangeEventArgs(original, state));
		}
	}

	internal sealed class DbTransactionMocker(DbConnection connection, IsolationLevel isolationLevel, DataDriverMocker driver) : DbTransaction
	{
		private int _commitCount;
		private int _rollbackCount;
		private int _disposeCount;
		public override IsolationLevel IsolationLevel { get; } = isolationLevel;
		protected override DbConnection DbConnection { get; } = connection;
		public int CommitCount => _commitCount;
		public int RollbackCount => _rollbackCount;
		public int DisposeCount => _disposeCount;
		public override void Commit() => throw new InvalidOperationException("A synchronous provider entry point was used.");
		public override void Rollback() => throw new InvalidOperationException("A synchronous provider entry point was used.");
		public override async Task CommitAsync(CancellationToken cancellationToken = default)
		{
			Interlocked.Increment(ref _commitCount);

			if(driver.Commit != null)
				await driver.Commit(cancellationToken);
			driver.ThrowIfRequested(TerminationFailure.Commit);
		}
		public override async Task RollbackAsync(CancellationToken cancellationToken = default)
		{
			Interlocked.Increment(ref _rollbackCount);

			if(driver.Rollback != null)
				await driver.Rollback(cancellationToken);
			driver.ThrowIfRequested(TerminationFailure.Rollback);
		}
		protected override void Dispose(bool disposing)
		{
			if(disposing)
				throw new InvalidOperationException("A synchronous provider entry point was used.");
		}
		public override async ValueTask DisposeAsync()
		{
			Interlocked.Increment(ref _disposeCount);

			if(driver.TransactionDispose != null)
				await driver.TransactionDispose();
			driver.ThrowIfRequested(TerminationFailure.TransactionDispose);
		}
	}

	internal sealed class DbCommandMocker(DataDriverMocker driver) : DbCommand
	{
		private int _disposeCount;
		public override string CommandText { get; set; }
		public override int CommandTimeout { get; set; }
		public override CommandType CommandType { get; set; }
		public override bool DesignTimeVisible { get; set; }
		public override UpdateRowSource UpdatedRowSource { get; set; }
		protected override DbConnection DbConnection { get; set; }
		protected override DbTransaction DbTransaction { get; set; }
		protected override DbParameterCollection DbParameterCollection => throw new InvalidOperationException("A synchronous provider entry point was used.");
		public DbReaderMocker Reader { get; private set; }
		public int DisposeCount => _disposeCount;
		public int PrepareCount { get; private set; }
		public CommandBehavior ReaderBehavior { get; private set; }
		public override void Cancel() { }
		public override int ExecuteNonQuery() => throw new InvalidOperationException("A synchronous provider entry point was used.");
		public override object ExecuteScalar() => throw new InvalidOperationException("A synchronous provider entry point was used.");
		public override void Prepare() => throw new InvalidOperationException("A synchronous provider entry point was used.");
		public override Task PrepareAsync(CancellationToken cancellationToken = default)
		{
			cancellationToken.ThrowIfCancellationRequested();
			this.PrepareCount++;
			return Task.CompletedTask;
		}
		public override async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
		{
			if(driver.Execute != null)
				await driver.Execute(cancellationToken);
			return 1;
		}
		public override async Task<object> ExecuteScalarAsync(CancellationToken cancellationToken)
		{
			if(driver.Execute != null)
				await driver.Execute(cancellationToken);
			return 1;
		}
		protected override DbParameter CreateDbParameter() => throw new InvalidOperationException("A synchronous provider entry point was used.");
		protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => throw new InvalidOperationException("A synchronous provider entry point was used.");
		protected override async Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
		{
			if(driver.Execute != null)
				await driver.Execute(cancellationToken);
			if(driver.ReaderFailure)
				throw new InvalidOperationException();
			this.ReaderBehavior = behavior;
			return this.Reader = new DbReaderMocker(driver);
		}
		protected override void Dispose(bool disposing)
		{
			if(disposing)
				throw new InvalidOperationException("A synchronous provider entry point was used.");
		}
		public override async ValueTask DisposeAsync()
		{
			Interlocked.Increment(ref _disposeCount);

			if(driver.CommandDispose != null)
				await driver.CommandDispose();
		}
	}

	internal sealed class DbReaderMocker(DataDriverMocker driver) : DbDataReader
	{
		private int _disposeCount;
		private int _readCount;
		private int _nextResultCount;
		private bool _closed;
		public int DisposeCount => _disposeCount;
		public int ReadCount => _readCount;
		public int NextResultCount => _nextResultCount;
		public override object this[int ordinal] => this.GetValue(ordinal);
		public override object this[string name] => this.GetValue(this.GetOrdinal(name));
		public override int Depth => 0;
		public override int FieldCount => 1;
		public override bool HasRows => true;
		public override bool IsClosed => _closed;
		public override int RecordsAffected => -1;
		public override void Close() => throw new InvalidOperationException("A synchronous provider entry point was used.");
		public override Task CloseAsync() => throw new InvalidOperationException("A synchronous provider entry point was used.");
		protected override void Dispose(bool disposing)
		{
			if(disposing)
				throw new InvalidOperationException("A synchronous provider entry point was used.");
		}
		public override async ValueTask DisposeAsync()
		{
			Interlocked.Increment(ref _disposeCount);

			if(driver.ReaderDispose != null)
				await driver.ReaderDispose();
			_closed = true;
		}
		public override bool Read() => throw new InvalidOperationException("A synchronous provider entry point was used.");
		public override bool NextResult() => throw new InvalidOperationException("A synchronous provider entry point was used.");
		public override IEnumerator GetEnumerator() => throw new InvalidOperationException("A synchronous provider entry point was used.");
		public override async Task<bool> ReadAsync(CancellationToken cancellationToken)
		{
			var count = Interlocked.Increment(ref _readCount);

			return driver.Read == null ? count == 1 : await driver.Read(cancellationToken);
		}
		public override async Task<bool> NextResultAsync(CancellationToken cancellationToken)
		{
			Interlocked.Increment(ref _nextResultCount);
			return driver.NextResult != null && await driver.NextResult(cancellationToken);
		}
		public override string GetName(int ordinal) => ordinal == 0 ? "Value" : throw new IndexOutOfRangeException();
		public override int GetOrdinal(string name) => name == "Value" ? 0 : throw new IndexOutOfRangeException();
		public override Type GetFieldType(int ordinal) => typeof(int);
		public override string GetDataTypeName(int ordinal) => "Int32";
		public override object GetValue(int ordinal) => this.GetInt32(ordinal);
		public override int GetValues(object[] values)
		{
			if(values.Length == 0)
				return 0;
			values[0] = 1;
			return 1;
		}
		public override int GetInt32(int ordinal) => ordinal == 0 ? 1 : throw new IndexOutOfRangeException();
		public override bool IsDBNull(int ordinal) => false;
		public override bool GetBoolean(int ordinal) => throw new InvalidOperationException("A synchronous provider entry point was used.");
		public override byte GetByte(int ordinal) => throw new InvalidOperationException("A synchronous provider entry point was used.");
		public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length) => throw new InvalidOperationException("A synchronous provider entry point was used.");
		public override char GetChar(int ordinal) => throw new InvalidOperationException("A synchronous provider entry point was used.");
		public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length) => throw new InvalidOperationException("A synchronous provider entry point was used.");
		public override DateTime GetDateTime(int ordinal) => throw new InvalidOperationException("A synchronous provider entry point was used.");
		public override decimal GetDecimal(int ordinal) => throw new InvalidOperationException("A synchronous provider entry point was used.");
		public override double GetDouble(int ordinal) => throw new InvalidOperationException("A synchronous provider entry point was used.");
		public override float GetFloat(int ordinal) => throw new InvalidOperationException("A synchronous provider entry point was used.");
		public override Guid GetGuid(int ordinal) => throw new InvalidOperationException("A synchronous provider entry point was used.");
		public override short GetInt16(int ordinal) => throw new InvalidOperationException("A synchronous provider entry point was used.");
		public override long GetInt64(int ordinal) => throw new InvalidOperationException("A synchronous provider entry point was used.");
		public override string GetString(int ordinal) => throw new InvalidOperationException("A synchronous provider entry point was used.");
	}

	private sealed class SharedConnectionFeature : ConnectionSharingFeature
	{
		public override bool CanShare(IDataSource source) => true;
	}
}
