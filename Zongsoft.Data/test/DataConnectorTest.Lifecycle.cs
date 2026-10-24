using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Xunit;
using Zongsoft.Data.Common;
using Zongsoft.Data.Transactions;

using Zongsoft.Data.Features;

namespace Zongsoft.Data.Tests;

public partial class DataConnectorTest
{
	[Theory]
	[InlineData(TerminationFailure.Commit, false)]
	[InlineData(TerminationFailure.Commit, true)]
	[InlineData(TerminationFailure.TransactionDispose, false)]
	[InlineData(TerminationFailure.TransactionDispose, true)]
	[InlineData(TerminationFailure.ConnectionDispose, false)]
	[InlineData(TerminationFailure.ConnectionDispose, true)]
	public async Task SessionCompletion_PendingCommitOrCleanup_SharedByAllCallers(TerminationFailure stage, bool fail)
	{
		var entered = NewGate();
		var release = NewGate();
		var failure = new InvalidOperationException();
		var driver = new DataDriverMocker();

		if(stage == TerminationFailure.Commit)
			driver.Commit = cancellation =>
			{
				Assert.False(cancellation.CanBeCanceled);
				return WaitAsync();
			};
		else if(stage == TerminationFailure.TransactionDispose)
			driver.TransactionDispose = WaitAsync;
		else
			driver.ConnectionDispose = WaitAsync;

		var session = new DataSession(new DataSourceMocker(driver));
		await using var lease = await session.AcquireLeaseAsync();
		var connection = Assert.IsType<DbConnectionMocker>(lease.Connection);
		var transaction = Assert.IsType<DbTransactionMocker>(lease.Transaction);

		await lease.DisposeAsync();

		try
		{
			var first = session.CommitAsync().AsTask();
			await entered.Task.WaitAsync(_timeout);

			using var cancellation = new CancellationTokenSource();
			cancellation.Cancel();

			var disposal = session.DisposeAsync().AsTask();
			var others = new[] { session.CommitAsync(cancellation.Token).AsTask(), session.RollbackAsync(cancellation.Token).AsTask(), disposal };

			Assert.Same(disposal, session.DisposeAsync().AsTask());
			Assert.False(first.IsCompleted);
			Assert.All(others, task => Assert.False(task.IsCompleted));

			if(fail)
				release.SetException(failure);
			else
				release.SetResult();

			foreach(var pending in new[] { first, others[0], others[1], others[2] })
			{
				if(fail)
					Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => pending.WaitAsync(_timeout)));
				else
					await pending.WaitAsync(_timeout);
			}

			Assert.Equal(1, transaction.CommitCount);
			Assert.Equal(0, transaction.RollbackCount);
			Assert.Equal(1, transaction.DisposeCount);
			Assert.Equal(1, connection.DisposeCount);
		}
		finally
		{
			release.TrySetResult();
			await Record.ExceptionAsync(() => session.DisposeAsync().AsTask().WaitAsync(_timeout));
		}

		async Task WaitAsync()
		{
			entered.TrySetResult();
			await release.Task;
		}
	}

	[Theory]
	[InlineData(false, false)]
	[InlineData(false, true)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public async Task ConnectionLease_DisposeAsync_PendingOwnedOrLastSharedRelease_SharesOutcome(bool owned, bool fail)
	{
		var entered = NewGate();
		var release = NewGate();
		var failure = new InvalidOperationException();
		var driver = new DataDriverMocker
		{
			ConnectionDispose = async () =>
			{
				entered.TrySetResult();
				await release.Task;
			},
		};

		var source = new DataSourceMocker(driver);

		if(owned)
			source.Features.Set<ITransactionFeature>(new TransactionFeature(suppressed: true));

		var session = new DataSession(source);
		var lease = await session.AcquireLeaseAsync();
		var connection = Assert.IsType<DbConnectionMocker>(lease.Connection);
		var completion = session.DisposeAsync().AsTask();

		try
		{
			if(owned)
			{
				await completion.WaitAsync(_timeout);
				Assert.Equal(0, connection.DisposeCount);
			}
			else
				Assert.False(completion.IsCompleted);

			var first = lease.DisposeAsync().AsTask();

			await entered.Task.WaitAsync(_timeout);
			Assert.Same(first, lease.DisposeAsync().AsTask());
			Assert.False(first.IsCompleted);

			if(fail)
				release.SetException(failure);
			else
				release.SetResult();

			if(fail)
			{
				Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => first.WaitAsync(_timeout)));
				Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => lease.DisposeAsync().AsTask()));

				if(!owned)
					Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => completion.WaitAsync(_timeout)));
			}
			else
				await Task.WhenAll(first, completion).WaitAsync(_timeout);

			Assert.Equal(1, connection.DisposeCount);
		}
		finally
		{
			release.TrySetResult();
			await Record.ExceptionAsync(() => lease.DisposeAsync().AsTask().WaitAsync(_timeout));
			await Record.ExceptionAsync(() => session.DisposeAsync().AsTask().WaitAsync(_timeout));
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task AcquireLeaseAsync_PendingBeginTransaction_UsesAsyncProviderAndRestoresActivity(bool cancel)
	{
		var entered = NewGate();
		var release = NewGate();
		var failure = new InvalidOperationException();
		using var cancellation = new CancellationTokenSource();

		var driver = new DataDriverMocker
		{
			Begin = async token =>
			{
				Assert.Equal(cancellation.Token, token);
				entered.TrySetResult();
				await release.Task.WaitAsync(token);
			},
		};

		await using var session = new DataSession(new DataSourceMocker(driver));
		var acquisition = session.AcquireLeaseAsync(cancellation: cancellation.Token).AsTask();

		try
		{
			await entered.Task.WaitAsync(_timeout);

			var connection = Assert.Single(driver.Connections);
			Assert.Equal(ConnectionState.Open, connection.State);
			Assert.Equal(1, connection.OpenCount);
			Assert.Equal(1, connection.BeginCount);
			Assert.Null(session.Transaction);

			var pending = session.DisposeAsync().AsTask();
			Assert.False(pending.IsCompleted);

			if(cancel)
			{
				cancellation.Cancel();
				var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => acquisition.WaitAsync(_timeout));
				Assert.Equal(cancellation.Token, exception.CancellationToken);
			}
			else
			{
				release.SetException(failure);
				Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => acquisition.WaitAsync(_timeout)));
			}

			await pending.WaitAsync(_timeout);
			Assert.Null(session.Connection);
			Assert.Null(session.Transaction);
			Assert.Equal(1, connection.DisposeCount);
			Assert.Equal(ConnectionState.Closed, connection.State);
		}
		finally
		{
			release.TrySetResult();
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task SessionReader_DisposeAsync_AwaitsReaderThenLease(bool close)
	{
		var entered = NewGate();
		var release = NewGate();
		var trace = new List<string>();
		var driver = new DataDriverMocker
		{
			ReaderDispose = async () =>
			{
				trace.Add("reader-start");
				entered.TrySetResult();
				await release.Task;
				trace.Add("reader-end");
			},
			ConnectionDispose = () =>
			{
				trace.Add("connection");
				return Task.CompletedTask;
			},
		};

		await using var session = new DataSession(new DataSourceMocker(driver));
		await using var command = session.Build(null, null);
		await using var reader = await command.ExecuteReaderAsync(CommandBehavior.CloseConnection);

		var provider = Assert.Single(driver.Commands);
		Assert.Equal(CommandBehavior.Default, provider.ReaderBehavior);

		var completion = session.CommitAsync().AsTask();

		try
		{
			var first = close ? reader.CloseAsync() : reader.DisposeAsync().AsTask();

			await entered.Task.WaitAsync(_timeout);
			Assert.Same(first, reader.CloseAsync());
			Assert.Same(first, reader.DisposeAsync().AsTask());
			Assert.False(first.IsCompleted);
			Assert.False(completion.IsCompleted);
			Assert.Equal(new[] { "reader-start" }, trace);
			Assert.Throws<NotSupportedException>(reader.Close);
			Assert.Throws<NotSupportedException>(reader.Dispose);
		}
		finally
		{
			release.TrySetResult();
		}

		await reader.DisposeAsync().AsTask().WaitAsync(_timeout);
		await completion.WaitAsync(_timeout);
		reader.Close();
		reader.Dispose();
		Assert.Equal(new[] { "reader-start", "reader-end", "connection" }, trace);
		Assert.Equal(1, provider.Reader.DisposeCount);
		Assert.Equal(1, Assert.Single(driver.Connections).DisposeCount);
		Assert.True(reader.IsClosed);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task SessionCommand_DisposeAsync_ForwardsOwnedCommand(bool fail)
	{
		var entered = NewGate();
		var release = NewGate();
		var failure = new InvalidOperationException();
		var driver = new DataDriverMocker
		{
			CommandDispose = async () =>
			{
				entered.TrySetResult();
				await release.Task;
			},
		};

		await using var session = new DataSession(new DataSourceMocker(driver));
		var command = session.Build(null, null);

		try
		{
			Assert.Throws<NotSupportedException>(command.Dispose);

			var first = command.DisposeAsync().AsTask();

			await entered.Task.WaitAsync(_timeout);
			Assert.Same(first, command.DisposeAsync().AsTask());
			Assert.False(first.IsCompleted);
			Assert.Throws<NotSupportedException>(command.Dispose);

			if(fail)
			{
				release.SetException(failure);
				Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => first.WaitAsync(_timeout)));
				Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => command.DisposeAsync().AsTask()));
				Assert.Throws<NotSupportedException>(command.Dispose);
			}
			else
			{
				release.SetResult();
				await first.WaitAsync(_timeout);
				command.Dispose();
			}

			Assert.Equal(1, Assert.Single(driver.Commands).DisposeCount);
		}
		finally
		{
			release.TrySetResult();
			await Record.ExceptionAsync(() => command.DisposeAsync().AsTask().WaitAsync(_timeout));
		}
	}

	[Fact]
	public async Task SessionReader_AsyncReadsForwardTokensAndRejectSynchronousOperations()
	{
		var entered = NewGate();
		var release = NewGate();
		using var cancellation = new CancellationTokenSource();

		var driver = new DataDriverMocker
		{
			Read = async token =>
			{
				Assert.Equal(cancellation.Token, token);
				entered.TrySetResult();
				await release.Task;
				return true;
			},
			NextResult = token =>
			{
				Assert.Equal(cancellation.Token, token);
				return Task.FromResult(false);
			},
		};

		await using var session = new DataSession(new DataSourceMocker(driver));
		await using var command = session.Build(null, null);

		Assert.Throws<NotSupportedException>(() => command.ExecuteNonQuery());
		Assert.Throws<NotSupportedException>(() => command.ExecuteScalar());
		Assert.Throws<NotSupportedException>(() => command.ExecuteReader());
		Assert.Throws<NotSupportedException>(command.Prepare);
		Assert.Empty(driver.Connections);
		await command.PrepareAsync(cancellation.Token);

		await using var reader = await command.ExecuteReaderAsync();

		try
		{
			Assert.Throws<NotSupportedException>(() => reader.Read());
			Assert.Throws<NotSupportedException>(() => reader.NextResult());
			Assert.Throws<NotSupportedException>(() => reader.GetEnumerator());
			Assert.Throws<NotSupportedException>(reader.Close);

			var pending = reader.ReadAsync(cancellation.Token);

			await entered.Task.WaitAsync(_timeout);
			Assert.False(pending.IsCompleted);
			release.SetResult();
			Assert.True(await pending.WaitAsync(_timeout));
			Assert.Equal(1, reader.GetInt32(0));
			Assert.False(await reader.NextResultAsync(cancellation.Token));

			var provider = Assert.Single(driver.Commands);

			Assert.Equal(1, provider.PrepareCount);
			Assert.Equal(1, provider.Reader.ReadCount);
			Assert.Equal(1, provider.Reader.NextResultCount);
		}
		finally
		{
			release.TrySetResult();
		}
	}

	[Fact]
	public async Task SessionReader_CloseAndLeaseFailure_PreservesBoth()
	{
		var readFailure = new InvalidOperationException();
		var driver = new DataDriverMocker(terminationFailures: TerminationFailure.TransactionDispose | TerminationFailure.ConnectionDispose)
		{
			ReaderDispose = () => Task.FromException(readFailure),
		};

		var session = new DataSession(new DataSourceMocker(driver));
		await using var command = session.Build(null, null);
		var reader = await command.ExecuteReaderAsync();
		var pending = session.CommitAsync().AsTask();
		var exception = await Assert.ThrowsAsync<AggregateException>(() => reader.CloseAsync().WaitAsync(_timeout));

		Assert.Collection(exception.InnerExceptions,
			error => Assert.Same(readFailure, error),
			error => Assert.Same(driver.Errors[TerminationFailure.TransactionDispose], error),
			error => Assert.Same(driver.Errors[TerminationFailure.ConnectionDispose], error));
		Assert.Same(exception, await Assert.ThrowsAsync<AggregateException>(() => reader.DisposeAsync().AsTask()));
		Assert.Throws<NotSupportedException>(reader.Close);

		var completionFailure = await Assert.ThrowsAsync<AggregateException>(() => pending.WaitAsync(_timeout));

		Assert.Same(completionFailure, await Assert.ThrowsAsync<AggregateException>(() => session.DisposeAsync().AsTask()));
		Assert.Equal(1, Assert.Single(driver.Commands).Reader.DisposeCount);

		var connection = Assert.Single(driver.Connections);

		Assert.Equal(1, connection.Transaction.CommitCount);
		Assert.Equal(0, connection.Transaction.RollbackCount);
		Assert.Equal(1, connection.Transaction.DisposeCount);
		Assert.Equal(1, connection.DisposeCount);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task SessionCommand_ExecuteAndLeaseFailure_PropagatesLeaseFailure(bool reader)
	{
		var entered = NewGate();
		var release = NewGate();
		var executeFailure = new InvalidOperationException();
		var driver = new DataDriverMocker(terminationFailures: TerminationFailure.ConnectionDispose)
		{
			Execute = async _ =>
			{
				entered.TrySetResult();
				await release.Task;
			},
		};

		var session = new DataSession(new DataSourceMocker(driver));
		await using var command = session.Build(null, null);
		var operation = reader ? (Task)command.ExecuteReaderAsync() : command.ExecuteNonQueryAsync();

		try
		{
			await entered.Task.WaitAsync(_timeout);

			var completion = session.RollbackAsync().AsTask();

			Assert.False(completion.IsCompleted);
			release.SetException(executeFailure);

			Assert.Same(driver.Errors[TerminationFailure.ConnectionDispose], await Assert.ThrowsAsync<InvalidOperationException>(() => operation.WaitAsync(_timeout)));
			Assert.Same(driver.Errors[TerminationFailure.ConnectionDispose], await Assert.ThrowsAsync<InvalidOperationException>(() => completion.WaitAsync(_timeout)));
			Assert.Equal(1, Assert.Single(driver.Connections).DisposeCount);
		}
		finally
		{
			release.TrySetException(executeFailure);
			await Record.ExceptionAsync(() => operation.WaitAsync(_timeout));
			await Record.ExceptionAsync(() => session.DisposeAsync().AsTask().WaitAsync(_timeout));
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task AmbientCommitAsync_NotificationOrCommitFailure_PreservesDatabaseOutcome(bool commitFailure)
	{
		var notificationFailure = new InvalidOperationException();
		var driver = new DataDriverMocker(terminationFailures: commitFailure ? TerminationFailure.Commit : TerminationFailure.None);
		var ambient = new Transaction();
		var session = new DataSession(new DataSourceMocker(driver), ambient.Context);
		await using var lease = await session.AcquireLeaseAsync();
		var transaction = Assert.IsType<DbTransactionMocker>(lease.Transaction);

		await lease.DisposeAsync();

		if(!commitFailure)
			ambient.Context.Completed += (_, _, _) => ValueTask.FromException(notificationFailure);

		try
		{
			var expected = commitFailure ? driver.Errors[TerminationFailure.Commit] : notificationFailure;

			Assert.Same(expected, await Assert.ThrowsAsync<InvalidOperationException>(() => ambient.CommitAsync()));
			Assert.Same(expected, await Assert.ThrowsAsync<InvalidOperationException>(() => ambient.DisposeAsync().AsTask()));
			Assert.Equal(commitFailure ? TransactionStatus.Undetermined : TransactionStatus.Committed, ambient.Context.Status);
			Assert.Equal(1, transaction.CommitCount);
			Assert.Equal(0, transaction.RollbackCount);
			Assert.Equal(1, transaction.DisposeCount);
			Assert.Equal(1, Assert.Single(driver.Connections).DisposeCount);
		}
		finally
		{
			await Record.ExceptionAsync(() => ambient.DisposeAsync().AsTask().WaitAsync(_timeout));
			await Record.ExceptionAsync(() => session.DisposeAsync().AsTask().WaitAsync(_timeout));
		}
	}

	[Fact]
	public async Task ConnectAsync_OpenAndDisposeFailure_PropagatesDisposeFailure()
	{
		var openFailure = new InvalidOperationException();
		var disposeFailure = new ArgumentException();
		var entered = NewGate();
		var release = NewGate();
		var driver = new DataDriverMocker(_ => Task.FromException(openFailure))
		{
			ConnectionDispose = async () =>
			{
				entered.TrySetResult();
				await release.Task;
				throw disposeFailure;
			},
		};

		var connector = CreateConnector(new DataSourceMocker(driver), new ManualTimeProvider());
		var operation = connector.ConnectAsync();

		try
		{
			await entered.Task.WaitAsync(_timeout);
			Assert.False(operation.IsCompleted);
		}
		finally
		{
			release.TrySetResult();
		}

		Assert.Same(disposeFailure, await Assert.ThrowsAsync<ArgumentException>(() => operation.WaitAsync(_timeout)));
		Assert.Equal(1, Assert.Single(driver.Connections).DisposeCount);
		Assert.Equal(DataConnector.CircuitBreakerState.Opened, connector.Breaker.State);
	}

	[Fact]
	public async Task ConnectAsync_PreCancelled_DoesNotCreateConnection()
	{
		var driver = new DataDriverMocker();
		var connector = CreateConnector(new DataSourceMocker(driver), new ManualTimeProvider());
		using var cancellation = new CancellationTokenSource();

		cancellation.Cancel();

		var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => connector.ConnectAsync(cancellation.Token));

		Assert.Equal(cancellation.Token, exception.CancellationToken);
		Assert.Empty(driver.Connections);
		Assert.Equal(DataConnector.CircuitBreakerState.Closed, connector.Breaker.State);
	}

	[Fact]
	public async Task OpenAsync_CancelledWhileWaiting_DoesNotOpenOrDisposeBorrowedConnection()
	{
		var entered = NewGate();
		var release = NewGate();
		var driver = new DataDriverMocker(async _ =>
		{
			entered.TrySetResult();
			await release.Task;
		});

		var connector = CreateConnector(new DataSourceMocker(driver), new ManualTimeProvider());
		await using var first = (DbConnectionMocker)driver.CreateConnection();
		await using var second = (DbConnectionMocker)driver.CreateConnection();
		using var cancellation = new CancellationTokenSource();
		var opening = connector.OpenAsync(first);

		try
		{
			await entered.Task.WaitAsync(_timeout);

			var waiting = connector.OpenAsync(second, cancellation.Token);

			Assert.False(waiting.IsCompleted);
			cancellation.Cancel();
			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiting.WaitAsync(_timeout));
			Assert.Equal(0, second.OpenCount);
			Assert.Equal(0, second.DisposeCount);
			Assert.Equal(DataConnector.CircuitBreakerState.Closed, connector.Breaker.State);
		}
		finally
		{
			release.TrySetResult();
		}

		await opening.WaitAsync(_timeout);
		Assert.Equal(1, first.OpenCount);
		Assert.Equal(ConnectionState.Open, first.State);
	}
}
