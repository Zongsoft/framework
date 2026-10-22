using System;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Xunit;

using TDengine.Driver;

namespace Zongsoft.Data.TDengine.Tests;

[Trait("Category", "Unit")]
public sealed class DriverAsyncTest
{
	private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(5);

	[Fact]
	public async Task OpenAsync_PreCancelledTokenDoesNotOpenConnection()
	{
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		await using var connection = TDengineDriver.Instance.CreateConnection("Protocol=WebSocket;Server=localhost;Database=unit");

		var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => connection.OpenAsync(cancellation.Token));

		Assert.Equal(cancellation.Token, error.CancellationToken);
		Assert.Equal(ConnectionState.Closed, connection.State);
		Assert.Null(connection.GetType().GetField("_client", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(connection));
		Assert.Throws<NotSupportedException>(() => connection.Open());
	}

	[Fact]
	public void ConnectionOptions_DefaultToWebSocketAndRejectNative()
	{
		var settings = Configuration.TDengineConnectionSettingsDriver.Instance.GetSettings("Server=localhost;Database=unit");
		Assert.Equal(Configuration.TDengineConnectionProtocol.WebSocket, settings.Protocol);
		Assert.Equal(6041, settings.GetOptions().Port);
		Assert.Throws<NotSupportedException>(() => TDengineDriver.Instance.CreateConnection("Protocol=Native;Server=localhost;Database=unit"));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task ExecuteAsync_AwaitsQueryReadAndRowsDisposal(bool scalar)
	{
		using var cancellation = new CancellationTokenSource();
		var stages = new Stages(scalar ? ["query", "read", "rows-dispose"] : ["query", "rows-dispose"]);
		var rows = SdkProxy.Create<IRowsAsync>(out var rowCalls);
		rowCalls.Callbacks["get_AffectRows"] = _ => 3;
		rowCalls.Callbacks[nameof(IRowsAsync.GetValue)] = args => { Assert.Equal(0, args[0]); return 42; };
		rowCalls.Callbacks[nameof(IRowsAsync.ReadAsync)] = args => ReadAsync(args);
		rowCalls.Callbacks[nameof(IAsyncDisposable.DisposeAsync)] = _ => Box(stages.EnterAsync("rows-dispose", CancellationToken.None));
		var client = SdkProxy.Create<ITDengineClientAsync>(out var clientCalls);
		clientCalls.Callbacks[nameof(ITDengineClientAsync.QueryAsync)] = args => QueryAsync(args);
		await using var connection = CreateOpenConnection(client);
		await using var command = connection.CreateCommand();
		command.CommandText = "SELECT 42";
		var operation = ExecuteAsync();

		try
		{
			await stages.AdvanceAsync(operation);
			Assert.Equal(scalar ? 42 : 3, await operation.WaitAsync(_timeout));
			Assert.Equal(1, clientCalls.Count(nameof(ITDengineClientAsync.QueryAsync)));
			Assert.Equal(scalar ? 1 : 0, rowCalls.Count(nameof(IRowsAsync.ReadAsync)));
			Assert.Equal(1, rowCalls.Count(nameof(IAsyncDisposable.DisposeAsync)));
			Assert.Throws<NotSupportedException>(() => command.ExecuteNonQuery());
			Assert.Throws<NotSupportedException>(() => command.ExecuteScalar());
			Assert.Throws<NotSupportedException>(() => command.ExecuteReader());
		}
		finally
		{
			stages.ReleaseAll();
			await Record.ExceptionAsync(() => operation.WaitAsync(_timeout));
		}

		async Task<IRowsAsync> QueryAsync(object[] args)
		{
			Assert.Equal("SELECT 42", args[0]);
			Assert.Equal(cancellation.Token, args[1]);
			await stages.EnterAsync("query", (CancellationToken)args[1]);
			return rows;
		}

		async Task<bool> ReadAsync(object[] args)
		{
			Assert.Equal(cancellation.Token, Assert.Single(args));
			await stages.EnterAsync("read", (CancellationToken)args[0]);
			return true;
		}

		async Task<int> ExecuteAsync() => scalar ? Assert.IsType<int>(await command.ExecuteScalarAsync(cancellation.Token)) : await command.ExecuteNonQueryAsync(cancellation.Token);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task ExecuteAsync_PendingQueryPreservesFailureOrCancellation(bool cancel)
	{
		using var cancellation = new CancellationTokenSource();
		var release = new TaskCompletionSource<IRowsAsync>(TaskCreationOptions.RunContinuationsAsynchronously);
		var client = SdkProxy.Create<ITDengineClientAsync>(out var calls);
		calls.Callbacks[nameof(ITDengineClientAsync.QueryAsync)] = args =>
		{
			Assert.Equal(cancellation.Token, args[1]);
			return release.Task.WaitAsync((CancellationToken)args[1]);
		};
		await using var connection = CreateOpenConnection(client);
		await using var command = connection.CreateCommand();
		command.CommandText = "SELECT 42";
		var pending = command.ExecuteReaderAsync(cancellation.Token);

		try
		{
			Assert.False(pending.IsCompleted);

			if(cancel)
			{
				cancellation.Cancel();
				var failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending.WaitAsync(_timeout));
				Assert.Equal(cancellation.Token, failure.CancellationToken);
			}
			else
			{
				var failure = new InvalidOperationException();
				release.SetException(failure);
				Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => pending.WaitAsync(_timeout)));
			}

			Assert.Equal(1, calls.Count(nameof(ITDengineClientAsync.QueryAsync)));
			Assert.Equal(0, calls.Count(nameof(ITDengineClientAsync.StmtInitAsync)));
		}
		finally
		{
			release.TrySetException(new InvalidOperationException());
			await Record.ExceptionAsync(() => pending.WaitAsync(_timeout));
		}
	}

	[Fact]
	public async Task ExecuteAsync_ParameterizedCommandAwaitsEverySdkStage()
	{
		using var cancellation = new CancellationTokenSource();
		var stages = new Stages(["init", "prepare", "table", "tags", "bind", "batch", "execute", "result", "rows-dispose", "statement-dispose"]);
		var rows = SdkProxy.Create<IRowsAsync>(out var rowCalls);
		rowCalls.Callbacks["get_AffectRows"] = _ => 3;
		rowCalls.Callbacks[nameof(IAsyncDisposable.DisposeAsync)] = _ => Box(stages.EnterAsync("rows-dispose", CancellationToken.None));
		var statement = SdkProxy.Create<IStmtAsync>(out var statementCalls);
		statementCalls.Callbacks[nameof(IStmtAsync.PrepareAsync)] = args => StepAsync("prepare", args, "INSERT INTO ? VALUES (?)");
		statementCalls.Callbacks[nameof(IStmtAsync.IsInsert)] = _ => true;
		statementCalls.Callbacks[nameof(IStmtAsync.SetTableNameAsync)] = args => StepAsync("table", args, "meter_17");
		statementCalls.Callbacks[nameof(IStmtAsync.SetTagsAsync)] = args => StepAsync("tags", args, new object[] { "west" });
		statementCalls.Callbacks[nameof(IStmtAsync.BindRowAsync)] = args => StepAsync("bind", args, new object[] { 17 });
		statementCalls.Callbacks[nameof(IStmtAsync.AddBatchAsync)] = args => StepAsync("batch", args);
		statementCalls.Callbacks[nameof(IStmtAsync.ExecAsync)] = args => StepAsync("execute", args);
		statementCalls.Callbacks[nameof(IStmtAsync.ResultAsync)] = args => ResultAsync(args);
		statementCalls.Callbacks[nameof(IAsyncDisposable.DisposeAsync)] = _ => Box(stages.EnterAsync("statement-dispose", CancellationToken.None));
		var client = SdkProxy.Create<ITDengineClientAsync>(out var clientCalls);
		clientCalls.Callbacks[nameof(ITDengineClientAsync.StmtInitAsync)] = args => InitAsync(args);
		await using var connection = CreateOpenConnection(client);
		var command = connection.CreateCommand();
		command.CommandText = "INSERT INTO ? VALUES (?)";
		AddParameter(command, "#table", "meter_17");
		AddParameter(command, "$location", "west");
		AddParameter(command, "@value", 17);
		var operation = ExecuteAsync();

		try
		{
			await stages.AdvanceAsync(operation);
			Assert.Equal(3, await operation.WaitAsync(_timeout));
			Assert.Equal(1, statementCalls.Count(nameof(IStmtAsync.PrepareAsync)));
			Assert.Equal(1, statementCalls.Count(nameof(IAsyncDisposable.DisposeAsync)));
			Assert.Equal(1, rowCalls.Count(nameof(IAsyncDisposable.DisposeAsync)));
			Assert.Equal(0, clientCalls.Count(nameof(ITDengineClientAsync.QueryAsync)));
		}
		finally
		{
			stages.ReleaseAll();
			await Record.ExceptionAsync(() => operation.WaitAsync(_timeout));
			await command.DisposeAsync();
		}

		async Task<IStmtAsync> InitAsync(object[] args)
		{
			await StepAsync("init", args);
			return statement;
		}

		async Task<IRowsAsync> ResultAsync(object[] args)
		{
			await StepAsync("result", args);
			return rows;
		}

		Task StepAsync(string name, object[] args, object value = null)
		{
			Assert.Equal(cancellation.Token, args[^1]);

			if(value != null)
				Assert.Equal(value, args[0]);

			return stages.EnterAsync(name, (CancellationToken)args[^1]).AsTask();
		}

		async Task<int> ExecuteAsync()
		{
			var count = await command.ExecuteNonQueryAsync(cancellation.Token);
			var first = command.DisposeAsync().AsTask();
			var second = command.DisposeAsync().AsTask();
			Assert.Same(first, second);
			await first;
			await second;
			return count;
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task ExecuteAsync_CommandTextChangeAwaitsPreviousStatementDisposal(bool fail)
	{
		using var cancellation = new CancellationTokenSource();
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var firstRows = SdkProxy.Create<IRowsAsync>(out var firstRowCalls);
		firstRowCalls.Callbacks["get_AffectRows"] = _ => 1;
		var nextRows = SdkProxy.Create<IRowsAsync>(out var nextRowCalls);
		nextRowCalls.Callbacks["get_AffectRows"] = _ => 2;
		var statement = SdkProxy.Create<IStmtAsync>(out var statementCalls);
		statementCalls.Callbacks[nameof(IStmtAsync.PrepareAsync)] = _ => Task.CompletedTask;
		statementCalls.Callbacks[nameof(IStmtAsync.IsInsert)] = _ => false;
		statementCalls.Callbacks[nameof(IStmtAsync.BindRowAsync)] = _ => Task.CompletedTask;
		statementCalls.Callbacks[nameof(IStmtAsync.AddBatchAsync)] = _ => Task.CompletedTask;
		statementCalls.Callbacks[nameof(IStmtAsync.ExecAsync)] = _ => Task.CompletedTask;
		statementCalls.Callbacks[nameof(IStmtAsync.ResultAsync)] = _ => Task.FromResult(firstRows);
		statementCalls.Callbacks[nameof(IAsyncDisposable.DisposeAsync)] = _ => new ValueTask(release.Task);
		var client = SdkProxy.Create<ITDengineClientAsync>(out var clientCalls);
		clientCalls.Callbacks[nameof(ITDengineClientAsync.StmtInitAsync)] = _ => Task.FromResult(statement);
		clientCalls.Callbacks[nameof(ITDengineClientAsync.QueryAsync)] = args =>
		{
			Assert.Equal("DELETE FROM meter_17", args[0]);
			Assert.Equal(cancellation.Token, args[1]);
			return Task.FromResult(nextRows);
		};
		await using var connection = CreateOpenConnection(client);
		var command = connection.CreateCommand();
		command.CommandText = "DELETE FROM meter_17 WHERE value=?";
		AddParameter(command, "@value", 17);
		Assert.Equal(1, await command.ExecuteNonQueryAsync(cancellation.Token));
		Assert.Equal(1, firstRowCalls.Count(nameof(IAsyncDisposable.DisposeAsync)));
		command.CommandText = "DELETE FROM meter_17";
		command.Parameters.Clear();
		Assert.Equal(0, statementCalls.Count(nameof(IAsyncDisposable.DisposeAsync)));
		var pending = command.ExecuteNonQueryAsync(cancellation.Token);

		try
		{
			Assert.False(pending.IsCompleted);
			Assert.Equal(1, statementCalls.Count(nameof(IAsyncDisposable.DisposeAsync)));
			Assert.Equal(0, clientCalls.Count(nameof(ITDengineClientAsync.QueryAsync)));

			if(fail)
			{
				var failure = new InvalidOperationException();
				release.SetException(failure);
				Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => pending.WaitAsync(_timeout)));
				Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => command.ExecuteNonQueryAsync(cancellation.Token)));
				var disposing = command.DisposeAsync().AsTask();
				Assert.Same(disposing, command.DisposeAsync().AsTask());
				Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => disposing));
			}
			else
			{
				release.SetResult();
				Assert.Equal(2, await pending.WaitAsync(_timeout));
			}

			Assert.Equal(fail ? 0 : 1, clientCalls.Count(nameof(ITDengineClientAsync.QueryAsync)));
			Assert.Equal(fail ? 0 : 1, nextRowCalls.Count(nameof(IAsyncDisposable.DisposeAsync)));
			Assert.Equal(1, clientCalls.Count(nameof(ITDengineClientAsync.StmtInitAsync)));
			Assert.Equal(1, statementCalls.Count(nameof(IAsyncDisposable.DisposeAsync)));
		}
		finally
		{
			release.TrySetResult();
			await Record.ExceptionAsync(() => pending.WaitAsync(_timeout));
			await Record.ExceptionAsync(() => command.DisposeAsync().AsTask());
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task ReaderAsync_AwaitsReadAndClosesOwnedConnectionAfterRows(bool cancel)
	{
		using var cancellation = new CancellationTokenSource();
		var read = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var stages = new Stages(["rows-dispose", "client-dispose"]);
		var rows = SdkProxy.Create<IRowsAsync>(out var rowCalls);
		rowCalls.Callbacks[nameof(IRowsAsync.ReadAsync)] = args =>
		{
			Assert.Equal(cancellation.Token, Assert.Single(args));
			return read.Task.WaitAsync((CancellationToken)args[0]);
		};
		rowCalls.Callbacks[nameof(IRowsAsync.GetInt32)] = args => { Assert.Equal(0, Assert.Single(args)); return 42; };
		rowCalls.Callbacks[nameof(IAsyncDisposable.DisposeAsync)] = _ => Box(stages.EnterAsync("rows-dispose", CancellationToken.None));
		var client = SdkProxy.Create<ITDengineClientAsync>(out var clientCalls);
		clientCalls.Callbacks[nameof(ITDengineClientAsync.QueryAsync)] = _ => Task.FromResult(rows);
		clientCalls.Callbacks[nameof(IAsyncDisposable.DisposeAsync)] = _ => Box(stages.EnterAsync("client-dispose", CancellationToken.None));
		var connection = CreateOpenConnection(client);
		await using var command = connection.CreateCommand();
		command.CommandText = "SELECT 42";
		var reader = await command.ExecuteReaderAsync(CommandBehavior.CloseConnection, cancellation.Token);
		var moving = reader.ReadAsync(cancellation.Token);

		try
		{
			Assert.False(moving.IsCompleted);
			Assert.Throws<NotSupportedException>(() => reader.Read());
			Assert.Throws<NotSupportedException>(() => reader.NextResult());

			if(cancel)
			{
				cancellation.Cancel();
				var failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => moving.WaitAsync(_timeout));
				Assert.Equal(cancellation.Token, failure.CancellationToken);
			}
			else
			{
				read.SetResult(true);
				Assert.True(await moving.WaitAsync(_timeout));
				Assert.Equal(42, reader.GetInt32(0));
				Assert.False(await reader.NextResultAsync(cancellation.Token));
			}

			var closing = reader.CloseAsync();
			Assert.Same(closing, reader.DisposeAsync().AsTask());
			await stages.AdvanceAsync(closing);
			await closing.WaitAsync(_timeout);
			Assert.True(reader.IsClosed);
			Assert.Equal(ConnectionState.Closed, connection.State);
			Assert.Equal(1, rowCalls.Count(nameof(IAsyncDisposable.DisposeAsync)));
			Assert.Equal(1, clientCalls.Count(nameof(IAsyncDisposable.DisposeAsync)));
		}
		finally
		{
			read.TrySetResult(false);
			stages.ReleaseAll();
			await Record.ExceptionAsync(() => moving.WaitAsync(_timeout));
			await reader.DisposeAsync();
			await connection.DisposeAsync();
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task DisposeAsync_RepeatedCallsSharePendingFailure(bool readerOwner)
	{
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var failure = new InvalidOperationException();
		var client = SdkProxy.Create<ITDengineClientAsync>(out var clientCalls);
		var rows = SdkProxy.Create<IRowsAsync>(out var rowCalls);
		clientCalls.Callbacks[nameof(ITDengineClientAsync.QueryAsync)] = _ => Task.FromResult(rows);
		var ownerCalls = readerOwner ? rowCalls : clientCalls;
		ownerCalls.Callbacks[nameof(IAsyncDisposable.DisposeAsync)] = _ => new ValueTask(release.Task);
		var connection = CreateOpenConnection(client);
		await using var command = connection.CreateCommand();
		command.CommandText = "SELECT 42";
		var reader = readerOwner ? await command.ExecuteReaderAsync(CommandBehavior.CloseConnection) : null;
		IAsyncDisposable owner = readerOwner ? reader : connection;
		var first = owner.DisposeAsync().AsTask();
		var second = owner.DisposeAsync().AsTask();

		try
		{
			Assert.False(first.IsCompleted);
			Assert.False(second.IsCompleted);
			release.SetException(failure);
			Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => first.WaitAsync(_timeout)));
			Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => second.WaitAsync(_timeout)));
			Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => owner.DisposeAsync().AsTask()));
			Assert.Equal(1, ownerCalls.Count(nameof(IAsyncDisposable.DisposeAsync)));
			Assert.Equal(ConnectionState.Closed, connection.State);
		}
		finally
		{
			release.TrySetException(failure);
			await Record.ExceptionAsync(() => first.WaitAsync(_timeout));
			await Record.ExceptionAsync(() => second.WaitAsync(_timeout));
			await Record.ExceptionAsync(() => connection.DisposeAsync().AsTask());
		}
	}

	private static object Box(ValueTask value) => value;

	private static DbConnection CreateOpenConnection(ITDengineClientAsync client)
	{
		var connection = TDengineDriver.Instance.CreateConnection("Protocol=WebSocket;Server=localhost;Database=unit");
		var type = connection.GetType();
		type.GetField("_client", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(connection, client);
		type.GetField("_state", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(connection, ConnectionState.Open);
		return connection;
	}
	private static void AddParameter(DbCommand command, string name, object value)
	{
		var parameter = command.CreateParameter();
		parameter.ParameterName = name;
		parameter.Value = value;
		command.Parameters.Add(parameter);
	}

	public class SdkProxy : DispatchProxy
	{
		private readonly Dictionary<string, int> _counts = [];
		public Dictionary<string, Func<object[], object>> Callbacks { get; } = [];
		public int Count(string name) => _counts.GetValueOrDefault(name);
		public static T Create<T>(out SdkProxy proxy) where T : class
		{
			var value = DispatchProxy.Create<T, SdkProxy>();
			proxy = (SdkProxy)(object)value;
			return value;
		}
		protected override object Invoke(MethodInfo method, object[] args)
		{
			_counts[method.Name] = this.Count(method.Name) + 1;

			if(this.Callbacks.TryGetValue(method.Name, out var callback))
				return callback(args);

			return method.Name switch
			{
				nameof(ITDengineClientAsync.ConnectionAvailable) => true,
				nameof(IAsyncDisposable.DisposeAsync) => ValueTask.CompletedTask,
				_ => throw new InvalidOperationException($"Unexpected SDK call: {method.Name}"),
			};
		}
	}
	private sealed class Stages(string[] names)
	{
		private readonly TaskCompletionSource[] _entered = Array.ConvertAll(names, _ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));
		private readonly TaskCompletionSource[] _released = Array.ConvertAll(names, _ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));
		private readonly List<string> _calls = [];
		public async ValueTask EnterAsync(string name, CancellationToken cancellation)
		{
			var index = Array.IndexOf(names, name);
			_calls.Add(name);
			_entered[index].TrySetResult();
			await _released[index].Task.WaitAsync(cancellation);
		}
		public async Task AdvanceAsync(Task operation)
		{
			for(int i = 0; i < names.Length; i++)
			{
				await _entered[i].Task.WaitAsync(_timeout);
				Assert.False(operation.IsCompleted);
				Assert.Equal(names[..(i + 1)], _calls);
				_released[i].TrySetResult();
			}
		}
		public void ReleaseAll()
		{
			foreach(var completion in _released)
				completion.TrySetResult();
		}
	}
}
