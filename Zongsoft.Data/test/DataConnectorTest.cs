using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Xunit;

using Zongsoft.Data.Common;

namespace Zongsoft.Data.Tests;

[Collection(nameof(DataConnectorCollection))]
public partial class DataConnectorTest
{
	public enum SessionCompletion
	{
		Commit,
		Rollback,
		Dispose,
	}

	public enum IndependentLeaseMode
	{
		UnsupportedTransaction,
		AmbientSuppressed,
	}

	public enum ReaderFailureStage
	{
		Open,
		Execute,
	}

	[Flags]
	public enum TerminationFailure
	{
		None = 0,
		Commit = 1,
		Rollback = 2,
		TransactionDispose = 4,
		ConnectionDispose = 8,
	}

	[Fact]
	public async Task TestFailureOpensAndFastRejectionsDoNotRepeatEvents()
	{
		var timeProvider = new ManualTimeProvider();
		var source = new DataSourceMocker();
		var connector = CreateConnector(source, timeProvider);
		var attempts = 0;
		var events = new List<DataConnector.CircuitBreakerStateChangedEventArgs>();
		var failures = new List<DataConnectionFailureEventArgs>();

		connector.Breaker.StateChanged += (_, args) => events.Add(args);
		connector.Failed += (_, _, _) => throw new InvalidOperationException("The subscriber failure must be ignored.");
		connector.Failed += (_, args, _) =>
		{
			failures.Add(args);
			return ValueTask.CompletedTask;
		};

		await Assert.ThrowsAsync<InvalidOperationException>(() => connector.ConnectAsync<object>(_ =>
		{
			attempts++;
			throw new InvalidOperationException("Database unavailable.");
		}));

		Assert.Equal(DataConnector.CircuitBreakerState.Opened, connector.Breaker.State);
		Assert.Equal(1, attempts);

		for(int index = 0; index < 10; index++)
		{
			var exception = await Assert.ThrowsAsync<DataConnectionException>(() =>
				connector.ConnectAsync(_ => Task.FromResult(attempts++)));

			Assert.Equal(source.Name, exception.SourceName);
			Assert.Equal(source.Driver.Name, exception.DriverName);
			Assert.Equal(TimeSpan.FromSeconds(1), exception.RetryAfter);
		}

		Assert.Equal(1, attempts);

		var stateChanged = Assert.Single(events);

		Assert.Equal(DataConnector.CircuitBreakerState.Closed, stateChanged.OriginalState);
		Assert.Equal(DataConnector.CircuitBreakerState.Opened, stateChanged.CurrentState);

		var failure = Assert.Single(failures);

		Assert.Same(source, failure.Source);
		Assert.Equal(1, failure.FailureCount);
		Assert.True(failure.IsSuspended);
		Assert.Equal(timeProvider.GetUtcNow().AddSeconds(1), failure.RetryAt);
		Assert.Equal(TimeSpan.FromSeconds(1), failure.RetryAfter);
		Assert.IsType<InvalidOperationException>(failure.Exception);
	}

	[Fact]
	public async Task TestHighFrequencyConnectsAreRejectedWithoutReconnect()
	{
		var timeProvider = new ManualTimeProvider();
		var attempts = 0;
		var source = new DataSourceMocker(new DataDriverMocker(_ =>
		{
			Interlocked.Increment(ref attempts);
			throw new InvalidOperationException("Database unavailable.");
		}));

		var connector = CreateConnector(source, timeProvider);
		var transitions = 0;
		var failures = 0;

		connector.Breaker.StateChanged += (_, _) => Interlocked.Increment(ref transitions);
		connector.Failed += (_, _, _) =>
		{
			Interlocked.Increment(ref failures);
			return ValueTask.CompletedTask;
		};

		var requests = new Task[256];

		for(int index = 0; index < requests.Length; index++)
		{
			requests[index] = Task.Run(() =>
				Assert.ThrowsAnyAsync<Exception>(async () =>
				{
					await using var connection = await connector.ConnectAsync();
				}));
		}

		await Task.WhenAll(requests);

		Assert.Equal(1, attempts);
		Assert.Equal(1, transitions);
		Assert.Equal(1, failures);
		Assert.Equal(DataConnector.CircuitBreakerState.Opened, connector.Breaker.State);
	}

	[Fact]
	public async Task TestHighFrequencySessionQueriesAndMutationsDoNotReconnect()
	{
		var attempts = 0;
		var source = new DataSourceMocker(new DataDriverMocker(_ =>
		{
			Interlocked.Increment(ref attempts);
			throw new InvalidOperationException();
		}));
		DataConnectorManager.GetConnector(source).Failed += (_, args, _) =>
		{
			args.ExceptionHandled = true;
			return ValueTask.CompletedTask;
		};

		await using(var session = new DataSession(source))
		await using(var command = session.Build(null, null))
			await Assert.ThrowsAsync<InvalidOperationException>(() => command.ExecuteNonQueryAsync());

		var requests = new Task[256];

		for(int index = 0; index < requests.Length; index++)
		{
			var query = index % 2 == 0;
			requests[index] = Task.Run(async () =>
			{
				await using var session = new DataSession(source);
				await using var command = session.Build(null, null);

				if(query)
					await Assert.ThrowsAsync<DataConnectionException>(() => command.ExecuteScalarAsync());
				else
					await Assert.ThrowsAsync<DataConnectionException>(() => command.ExecuteNonQueryAsync());
			});
		}

		await Task.WhenAll(requests);
		Assert.Equal(1, attempts);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task SessionReaderLease_RoutesConcurrentReadersByMarsCapability(bool mars)
	{
		var driver = new DataDriverMocker();
		var source = new DataSourceMocker(driver);
		source.Features.Add(Feature.TransactionSuppressed);

		if(mars)
			source.Features.Add(Feature.MultipleActiveResultSets);

		await using var session = new DataSession(source);
		await using var firstCommand = session.Build(null, null);
		await using var secondCommand = session.Build(null, null);
		await using var firstReader = await firstCommand.ExecuteReaderAsync();

		Assert.True(await firstReader.ReadAsync());

		var shared = Assert.IsType<DbConnectionMocker>(session.Connection);
		await using var secondReader = await secondCommand.ExecuteReaderAsync();

		Assert.True(await secondReader.ReadAsync());
		Assert.Equal(mars ? 1 : 2, driver.Connections.Length);

		var independent = mars ? null : driver.Connections[1];

		if(independent != null)
			Assert.NotSame(shared, independent);

		await secondReader.DisposeAsync();
		Assert.True(secondReader.IsClosed);
		Assert.False(firstReader.IsClosed);
		Assert.Equal(0, shared.DisposeCount);

		if(independent != null)
		{
			Assert.Equal(1, independent.DisposeCount);
			Assert.Equal(ConnectionState.Closed, independent.State);
		}

		await firstReader.DisposeAsync();
		Assert.True(firstReader.IsClosed);
		Assert.Equal(0, shared.DisposeCount);
		await session.DisposeAsync();
		Assert.Equal(1, shared.DisposeCount);
		Assert.Equal(ConnectionState.Closed, shared.State);
	}

	[Fact]
	public async Task SessionReaderLease_ReleasedSharedReaderCanBeReusedWhileIndependentReaderRemains()
	{
		var driver = new DataDriverMocker();
		var source = new DataSourceMocker(driver);
		source.Features.Add(Feature.TransactionSuppressed);

		await using var session = new DataSession(source);
		await using var firstCommand = session.Build(null, null);
		await using var secondCommand = session.Build(null, null);
		await using var thirdCommand = session.Build(null, null);
		await using var first = await firstCommand.ExecuteReaderAsync();
		var shared = Assert.IsType<DbConnectionMocker>(session.Connection);
		await using var second = await secondCommand.ExecuteReaderAsync();
		var independent = Assert.IsType<DbConnectionMocker>(secondCommand.Connection);

		Assert.NotSame(shared, independent);
		await first.DisposeAsync();

		await using var third = await thirdCommand.ExecuteReaderAsync();

		Assert.Same(shared, thirdCommand.Connection);
		Assert.Equal(2, driver.Connections.Length);
		Assert.False(second.IsClosed);
		Assert.Equal(ConnectionState.Open, independent.State);
		await third.DisposeAsync();
		await second.DisposeAsync();
		await session.DisposeAsync();
		Assert.Equal(1, independent.DisposeCount);
		Assert.Equal(1, shared.DisposeCount);
	}

	[Theory]
	[InlineData(SessionCompletion.Commit)]
	[InlineData(SessionCompletion.Rollback)]
	[InlineData(SessionCompletion.Dispose)]
	public async Task AcquireLease_SharedLeasesDeferSessionDestructionUntilLastRelease(SessionCompletion completion)
	{
		await using var session = new DataSession(new DataSourceMocker());
		await using var first = await session.AcquireLeaseAsync();
		await using var last = await session.AcquireLeaseAsync();
		var connection = Assert.IsType<DbConnectionMocker>(first.Connection);
		var transaction = Assert.IsType<DbTransactionMocker>(first.Transaction);

		Assert.Same(connection, last.Connection);
		Assert.Same(transaction, last.Transaction);

		var pending = CompleteSessionAsync(session, completion);
		var repeated = new[] { session.CommitAsync().AsTask(), session.RollbackAsync().AsTask(), session.DisposeAsync().AsTask() };

		await AssertSessionCompletedAsync(session);
		Assert.False(pending.IsCompleted);
		Assert.All(repeated, task => Assert.False(task.IsCompleted));
		Assert.Equal(0, transaction.CommitCount + transaction.RollbackCount);
		Assert.Equal(0, connection.DisposeCount);
		await first.DisposeAsync();
		Assert.False(pending.IsCompleted);
		Assert.Same(connection, session.Connection);
		await last.DisposeAsync();
		await pending.WaitAsync(_timeout);
		await Task.WhenAll(repeated).WaitAsync(_timeout);
		await last.DisposeAsync();
		await first.DisposeAsync();
		Assert.Null(session.Connection);
		Assert.Null(session.Transaction);
		Assert.Equal(ConnectionState.Closed, connection.State);
		Assert.Equal(1, connection.DisposeCount);
		Assert.Equal(completion == SessionCompletion.Commit ? 1 : 0, transaction.CommitCount);
		Assert.Equal(completion == SessionCompletion.Commit ? 0 : 1, transaction.RollbackCount);
		Assert.Equal(1, transaction.DisposeCount);
	}

	[Theory]
	[InlineData(IndependentLeaseMode.UnsupportedTransaction)]
	[InlineData(IndependentLeaseMode.AmbientSuppressed)]
	public async Task AcquireLease_IndependentLeaseSurvivesSessionCompletion(IndependentLeaseMode mode)
	{
		var driver = new DataDriverMocker();
		var source = new DataSourceMocker(driver);
		await using var ambient = mode == IndependentLeaseMode.AmbientSuppressed ? new Transaction() : null;

		if(mode == IndependentLeaseMode.UnsupportedTransaction)
			source.Features.Add(Feature.TransactionSuppressed);
		await using var session = new DataSession(source, ambient?.Context);
		DbConnectionMocker shared = null;
		DbTransactionMocker transaction = null;

		if(ambient != null)
		{
			await using var sharedLease = await session.AcquireLeaseAsync();
			shared = Assert.IsType<DbConnectionMocker>(sharedLease.Connection);
			transaction = Assert.IsType<DbTransactionMocker>(sharedLease.Transaction);
		}

		await using var lease = await session.AcquireLeaseAsync(transactionSuppressed: ambient != null);
		var independent = Assert.IsType<DbConnectionMocker>(lease.Connection);

		Assert.Null(lease.Transaction);
		Assert.NotSame(shared, independent);

		if(ambient != null)
			await ambient.RollbackAsync().WaitAsync(_timeout);
		else
			await session.DisposeAsync();
		await AssertSessionCompletedAsync(session);
		Assert.Equal(ConnectionState.Open, independent.State);
		Assert.Equal(0, independent.DisposeCount);
		Assert.Null(session.Connection);
		Assert.Null(session.Transaction);

		if(shared != null)
		{
			Assert.Equal(1, shared.DisposeCount);
			Assert.Equal(1, transaction.RollbackCount);
			Assert.Equal(1, transaction.DisposeCount);
		}

		await lease.DisposeAsync();
		await lease.DisposeAsync();
		Assert.Equal(1, independent.DisposeCount);
		Assert.Equal(ConnectionState.Closed, independent.State);
	}

	[Fact]
	public async Task AcquireLease_AmbientSuppressionWithoutAmbientTransactionRetainsLocalTransaction()
	{
		await using var session = new DataSession(new DataSourceMocker());
		await using var lease = await session.AcquireLeaseAsync(transactionSuppressed: true);
		var connection = Assert.IsType<DbConnectionMocker>(lease.Connection);
		var transaction = Assert.IsType<DbTransactionMocker>(lease.Transaction);

		Assert.False(session.InTransaction);
		Assert.Same(connection, session.Connection);
		Assert.Same(transaction, session.Transaction);

		var pending = session.RollbackAsync().AsTask();

		await AssertSessionCompletedAsync(session);
		Assert.False(pending.IsCompleted);
		Assert.Equal(0, transaction.RollbackCount);
		await lease.DisposeAsync();
		await pending.WaitAsync(_timeout);
		Assert.Equal(1, transaction.RollbackCount);
		Assert.Equal(1, transaction.DisposeCount);
		Assert.Equal(1, connection.DisposeCount);
	}

	[Fact]
	public async Task AcquireLease_OpenFailureRestoresActivity()
	{
		var failure = new InvalidOperationException();
		var driver = new DataDriverMocker(_ => Task.FromException(failure));
		var source = new DataSourceMocker(driver);
		DataConnectorManager.GetConnector(source).Failed += (_, args, _) =>
		{
			args.ExceptionHandled = true;
			return ValueTask.CompletedTask;
		};

		await using var session = new DataSession(source);

		Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => session.AcquireLeaseAsync().AsTask()));

		var connection = Assert.Single(driver.Connections);

		Assert.Same(connection, session.Connection);
		Assert.Null(session.Transaction);
		Assert.Equal(0, connection.DisposeCount);
		await session.RollbackAsync().AsTask().WaitAsync(_timeout);
		Assert.Null(session.Connection);
		Assert.Equal(1, connection.DisposeCount);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task AcquireLease_CompletionWhileOpeningDefersDestruction(bool disposing)
	{
		var opening = NewGate();
		var release = NewGate();
		var driver = new DataDriverMocker(async cancellation =>
		{
			opening.TrySetResult();
			await release.Task.WaitAsync(cancellation);
		});

		await using var session = new DataSession(new DataSourceMocker(driver));
		var acquisition = session.AcquireLeaseAsync().AsTask();
		Task pending;

		try
		{
			await opening.Task.WaitAsync(_timeout);
			pending = disposing ? session.DisposeAsync().AsTask() : session.CommitAsync().AsTask();
			Assert.False(pending.IsCompleted);
			Assert.False(acquisition.IsCompleted);
			await AssertSessionCompletedAsync(session);
			Assert.Equal(0, Assert.Single(driver.Connections).DisposeCount);
		}
		finally
		{
			release.TrySetResult();
		}

		await using var lease = await acquisition.WaitAsync(_timeout);
		var connection = Assert.IsType<DbConnectionMocker>(lease.Connection);
		var transaction = Assert.IsType<DbTransactionMocker>(lease.Transaction);

		Assert.False(pending.IsCompleted);
		Assert.Equal(0, transaction.CommitCount + transaction.RollbackCount);
		await lease.DisposeAsync();
		await pending.WaitAsync(_timeout);
		Assert.Equal(disposing ? 0 : 1, transaction.CommitCount);
		Assert.Equal(disposing ? 1 : 0, transaction.RollbackCount);
		Assert.Equal(1, transaction.DisposeCount);
		Assert.Equal(1, connection.DisposeCount);
	}

	[Theory]
	[InlineData(IndependentLeaseMode.UnsupportedTransaction)]
	[InlineData(IndependentLeaseMode.AmbientSuppressed)]
	public async Task AcquireLease_AfterSessionCompletionIsRejected(IndependentLeaseMode mode)
	{
		var driver = new DataDriverMocker();
		var source = new DataSourceMocker(driver);
		await using var ambient = mode == IndependentLeaseMode.AmbientSuppressed ? new Transaction() : null;

		if(mode == IndependentLeaseMode.UnsupportedTransaction)
			source.Features.Add(Feature.TransactionSuppressed);
		await using var session = new DataSession(source, ambient?.Context);

		if(ambient != null)
			await ambient.RollbackAsync();
		else
			await session.DisposeAsync();
		await AssertSessionCompletedAsync(session);
		await Assert.ThrowsAsync<DataException>(() => session.AcquireLeaseAsync(mode == IndependentLeaseMode.AmbientSuppressed).AsTask());
		Assert.Empty(driver.Connections);
		Assert.Null(session.Connection);
		Assert.Null(session.Transaction);
	}

	[Theory]
	[InlineData(ReaderFailureStage.Open)]
	[InlineData(ReaderFailureStage.Execute)]
	public async Task SessionReader_OpenOrExecutionFailureRestoresActivityAndReading(ReaderFailureStage stage)
	{
		var driver = stage == ReaderFailureStage.Open ?
			new DataDriverMocker(_ => Task.FromException(new InvalidOperationException())) :
			new DataDriverMocker(readerFailure: true);
		var source = new DataSourceMocker(driver);
		DataConnectorManager.GetConnector(source).Failed += (_, args, _) =>
		{
			args.ExceptionHandled = true;
			return ValueTask.CompletedTask;
		};

		await using var session = new DataSession(source);
		await using var command = session.Build(null, null);

		await Assert.ThrowsAsync<InvalidOperationException>(() => command.ExecuteReaderAsync());

		var connection = Assert.Single(driver.Connections);

		Assert.Same(connection, session.Connection);
		Assert.Equal(0, connection.DisposeCount);

		if(stage == ReaderFailureStage.Open)
			Assert.Null(connection.Transaction);
		else
			Assert.NotNull(connection.Transaction);
		await session.RollbackAsync().AsTask().WaitAsync(_timeout);
		Assert.Equal(1, connection.DisposeCount);

		if(connection.Transaction != null)
		{
			Assert.Equal(1, connection.Transaction.RollbackCount);
			Assert.Equal(1, connection.Transaction.DisposeCount);
		}
	}

	[Fact]
	public async Task SessionReader_AfterSessionCompletionUsesIndependentConnection()
	{
		var driver = new DataDriverMocker();
		await using var session = new DataSession(new DataSourceMocker(driver));
		await using var command = session.Build(null, null);

		await session.DisposeAsync();

		await using var reader = await command.ExecuteReaderAsync();

		Assert.True(await reader.ReadAsync());

		var connection = Assert.Single(driver.Connections);

		await AssertSessionCompletedAsync(session);
		Assert.False(reader.IsClosed);
		Assert.Null(session.Connection);
		Assert.Null(session.Transaction);
		Assert.Equal(0, connection.DisposeCount);
		await reader.DisposeAsync();
		Assert.True(reader.IsClosed);
		Assert.Equal(ConnectionState.Closed, connection.State);
		Assert.Equal(1, connection.DisposeCount);
	}

	[Fact]
	public async Task AmbientCommitAsync_CancelledAfterDecisionWaitsForSessionReaderReleaseAsync()
	{
		await using var ambient = new Transaction();
		await using var session = new DataSession(new DataSourceMocker(), ambient.Context);
		await using var command = session.Build(null, null);
		await using var reader = await command.ExecuteReaderAsync();
		var connection = Assert.IsType<DbConnectionMocker>(session.Connection);
		var transaction = Assert.IsType<DbTransactionMocker>(session.Transaction);
		using var cancellation = new CancellationTokenSource();
		var completion = ambient.CommitAsync(cancellation.Token);

		Assert.True(session.IsCompleted);
		cancellation.Cancel();
		Assert.False(completion.IsCompleted);
		Assert.Equal(0, transaction.CommitCount);
		Assert.Equal(0, connection.DisposeCount);
		await reader.DisposeAsync();
		await completion.WaitAsync(_timeout);
		Assert.Equal(Zongsoft.Data.Transactions.TransactionStatus.Committed, ambient.Context.Status);
		Assert.Equal(1, transaction.CommitCount);
		Assert.Equal(1, transaction.DisposeCount);
		Assert.Equal(1, connection.DisposeCount);
		Assert.Null(session.Transaction);
		Assert.Null(session.Connection);
	}

	[Fact]
	public async Task SessionCompletion_PreCancelledCommitRejectsDecisionWhileRollbackStillExecutesAsync()
	{
		await using var session = new DataSession(new DataSourceMocker());
		await using var lease = await session.AcquireLeaseAsync();
		var transaction = Assert.IsType<DbTransactionMocker>(lease.Transaction);

		await lease.DisposeAsync();

		using var cancellation = new CancellationTokenSource();

		cancellation.Cancel();

		var failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => session.CommitAsync(cancellation.Token).AsTask());

		Assert.Equal(cancellation.Token, failure.CancellationToken);
		Assert.False(session.IsCompleted);
		Assert.Equal(0, transaction.CommitCount + transaction.RollbackCount + transaction.DisposeCount);
		await session.RollbackAsync(cancellation.Token);
		await AssertSessionCompletedAsync(session);
		Assert.Equal(0, transaction.CommitCount);
		Assert.Equal(1, transaction.RollbackCount);
		Assert.Equal(1, transaction.DisposeCount);
	}

	[Fact]
	public async Task SessionCommand_ExternalConnectionWithoutTransactionIsNotDisposedByLease()
	{
		var driver = new DataDriverMocker();
		await using var session = new DataSession(new DataSourceMocker(driver));
		await using var command = session.Build(null, null);
		var connection = (DbConnectionMocker)driver.CreateConnection();
		command.Connection = connection;
		Assert.Equal(1, await command.ExecuteNonQueryAsync());
		Assert.Equal(ConnectionState.Open, connection.State);
		Assert.Null(session.Connection);
		Assert.Null(session.Transaction);
		await session.DisposeAsync();
		Assert.Equal(0, connection.DisposeCount);
		await connection.DisposeAsync();
		Assert.Equal(1, connection.DisposeCount);
	}

	[Theory]
	[InlineData(SessionCompletion.Commit, TerminationFailure.Commit)]
	[InlineData(SessionCompletion.Rollback, TerminationFailure.Rollback)]
	[InlineData(SessionCompletion.Commit, TerminationFailure.TransactionDispose)]
	[InlineData(SessionCompletion.Commit, TerminationFailure.ConnectionDispose)]
	public async Task SessionCompletion_SingleTerminationFailurePreservesOriginalException(SessionCompletion completion, TerminationFailure failure)
	{
		var driver = new DataDriverMocker(terminationFailures: failure);
		var session = new DataSession(new DataSourceMocker(driver));
		await using var lease = await session.AcquireLeaseAsync();
		var connection = Assert.IsType<DbConnectionMocker>(lease.Connection);
		var transaction = Assert.IsType<DbTransactionMocker>(lease.Transaction);

		await lease.DisposeAsync();

		var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => CompleteSessionAsync(session, completion));

		Assert.Same(driver.Errors[failure], exception);
		Assert.Same(exception, await Assert.ThrowsAsync<InvalidOperationException>(() => session.DisposeAsync().AsTask()));
		Assert.Equal(1, transaction.DisposeCount);
		Assert.Equal(1, connection.DisposeCount);
		Assert.Null(session.Transaction);
		Assert.Null(session.Connection);
	}

	[Theory]
	[InlineData(SessionCompletion.Commit)]
	[InlineData(SessionCompletion.Rollback)]
	public async Task SessionCompletion_MultipleTerminationFailuresAreAggregatedInResourceOrder(SessionCompletion completion)
	{
		var action = completion == SessionCompletion.Commit ? TerminationFailure.Commit : TerminationFailure.Rollback;
		var driver = new DataDriverMocker(terminationFailures: action | TerminationFailure.TransactionDispose | TerminationFailure.ConnectionDispose);
		var session = new DataSession(new DataSourceMocker(driver));
		await using var lease = await session.AcquireLeaseAsync();
		var connection = Assert.IsType<DbConnectionMocker>(lease.Connection);
		var transaction = Assert.IsType<DbTransactionMocker>(lease.Transaction);

		await lease.DisposeAsync();

		var exception = await Assert.ThrowsAsync<AggregateException>(() => CompleteSessionAsync(session, completion));

		Assert.Collection(exception.InnerExceptions,
			error => Assert.Same(driver.Errors[action], error),
			error => Assert.Same(driver.Errors[TerminationFailure.TransactionDispose], error),
			error => Assert.Same(driver.Errors[TerminationFailure.ConnectionDispose], error));
		Assert.Same(exception, await Assert.ThrowsAsync<AggregateException>(() => session.DisposeAsync().AsTask()));
		Assert.Equal(1, transaction.DisposeCount);
		Assert.Equal(1, connection.DisposeCount);
		Assert.Null(session.Transaction);
		Assert.Null(session.Connection);
	}

	[Fact]
	public async Task TestHalfOpenAllowsSingleProbeAndRecovers()
	{
		var timeProvider = new ManualTimeProvider();
		var source = new DataSourceMocker();
		var connector = CreateConnector(source, timeProvider);
		var states = new List<DataConnector.CircuitBreakerState>();
		var failures = new List<DataConnectionFailureEventArgs>();

		connector.Breaker.StateChanged += (_, args) => states.Add(args.CurrentState);
		connector.Failed += (_, args, _) =>
		{
			failures.Add(args);
			return ValueTask.CompletedTask;
		};

		await Assert.ThrowsAsync<InvalidOperationException>(() =>
			connector.ConnectAsync<object>(_ => throw new InvalidOperationException()));

		timeProvider.Advance(TimeSpan.FromSeconds(1));
		await connector.ConnectAsync(_ => Task.FromResult(100));

		Assert.Equal(DataConnector.CircuitBreakerState.Closed, connector.Breaker.State);
		Assert.Equal(
			[
				DataConnector.CircuitBreakerState.Opened,
				DataConnector.CircuitBreakerState.HalfOpen,
				DataConnector.CircuitBreakerState.Closed,
			],
			states);
		Assert.Single(failures);
	}

	[Fact]
	public async Task TestRepeatedProbeFailuresBackOffExponentially()
	{
		var timeProvider = new ManualTimeProvider();
		var source = new DataSourceMocker();
		var connector = CreateConnector(source, timeProvider);
		var failures = new List<DataConnectionFailureEventArgs>();

		connector.Failed += (_, args, _) =>
		{
			failures.Add(args);
			return ValueTask.CompletedTask;
		};

		await Assert.ThrowsAsync<InvalidOperationException>(() =>
			connector.ConnectAsync<object>(_ => throw new InvalidOperationException()));
		Assert.Equal(timeProvider.GetUtcNow().AddSeconds(1), connector.Breaker.RetryAt);

		timeProvider.Advance(TimeSpan.FromSeconds(1));
		await Assert.ThrowsAsync<InvalidOperationException>(() =>
			connector.ConnectAsync<object>(_ => throw new InvalidOperationException()));
		Assert.Equal(timeProvider.GetUtcNow().AddSeconds(2), connector.Breaker.RetryAt);

		timeProvider.Advance(TimeSpan.FromSeconds(2));
		await connector.ConnectAsync(_ => Task.FromResult(100));

		Assert.Equal(DataConnector.CircuitBreakerState.Closed, connector.Breaker.State);
		Assert.Null(connector.Breaker.RetryAt);

		await Assert.ThrowsAsync<InvalidOperationException>(() =>
			connector.ConnectAsync<object>(_ => throw new InvalidOperationException()));
		Assert.Equal([1, 2, 1], failures.ConvertAll(failure => failure.FailureCount));
	}

	[Fact]
	public async Task TestUnhandledFailureUsesDefaultLogging()
	{
		var logger = new RecordingLogger();
		Zongsoft.Diagnostics.Logging.Loggers.Add(logger);

		try
		{
			var timeProvider = new ManualTimeProvider();
			var source = new DataSourceMocker();
			var connector = CreateConnector(source, timeProvider, false);
			var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
				connector.ConnectAsync<object>(_ => throw new InvalidOperationException("Database unavailable.")));

			var entry = Assert.Single(logger.Entries);

			Assert.Equal(Zongsoft.Diagnostics.LogLevel.Error, entry.Level);
			Assert.Equal("Zongsoft.Data", entry.Source);
			Assert.Same(exception, entry.Exception);
			Assert.DoesNotContain("ConnectionString", entry.Message, StringComparison.OrdinalIgnoreCase);
			Assert.DoesNotContain("Password", entry.Message, StringComparison.OrdinalIgnoreCase);
			Assert.DoesNotContain("secret", entry.Message, StringComparison.OrdinalIgnoreCase);

			await Assert.ThrowsAsync<DataConnectionException>(() => connector.ConnectAsync(_ => Task.FromResult(100)));

			var handled = CreateConnector(new DataSourceMocker(), timeProvider);

			await Assert.ThrowsAsync<InvalidOperationException>(() =>
				handled.ConnectAsync<object>(_ => throw new InvalidOperationException("Handled database failure.")));
			Assert.Single(logger.Entries);
		}
		finally
		{
			Zongsoft.Diagnostics.Logging.Loggers.Remove(logger);
		}
	}

	[Fact]
	public async Task TestCallerCancellationDoesNotOpenProtection()
	{
		var timeProvider = new ManualTimeProvider();
		var connector = CreateConnector(new DataSourceMocker(), timeProvider);
		using var cancellation = new CancellationTokenSource();

		cancellation.Cancel();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
			connector.ConnectAsync<int>(
				token => Task.FromCanceled<int>(token),
				cancellation.Token));

		Assert.Equal(DataConnector.CircuitBreakerState.Closed, connector.Breaker.State);
	}

	[Fact]
	public async Task TestConnectorsAreSharedPerSourceAndIsolatedBetweenSources()
	{
		var driver = new DataDriverMocker();
		var first = new DataSourceMocker(driver);
		var second = new DataSourceMocker(driver);

		Assert.Same(DataConnectorManager.GetConnector(first), DataConnectorManager.GetConnector(first));
		Assert.NotSame(DataConnectorManager.GetConnector(first), DataConnectorManager.GetConnector(second));

		await using(var firstSession = new DataSession(first))
		await using(var secondSession = new DataSession(first))
		{
			Assert.Same(DataConnectorManager.GetConnector(first), firstSession.Connector);
			Assert.Same(firstSession.Connector, secondSession.Connector);
		}

		await Assert.ThrowsAsync<InvalidOperationException>(() =>
		{
			DataConnectorManager.GetConnector(first).Failed += (_, args, _) =>
			{
				args.ExceptionHandled = true;
				return ValueTask.CompletedTask;
			};
			return DataConnectorManager.GetConnector(first).ConnectAsync<object>(_ => throw new InvalidOperationException());
		});

		Assert.Equal(100, await DataConnectorManager.GetConnector(second).ConnectAsync(_ => Task.FromResult(100)));
		Assert.Equal(DataConnector.CircuitBreakerState.Opened, DataConnectorManager.GetConnector(first).Breaker.State);
		Assert.Equal(DataConnector.CircuitBreakerState.Closed, DataConnectorManager.GetConnector(second).Breaker.State);
	}

	[Fact]
	public async Task TestConcurrentFirstAccessReturnsOneConnector()
	{
		var source = new DataSourceMocker();
		var requests = new Task<DataConnector>[256];

		for(int index = 0; index < requests.Length; index++)
			requests[index] = Task.Run(() => DataConnectorManager.GetConnector(source));

		var connectors = await Task.WhenAll(requests);

		for(int index = 1; index < connectors.Length; index++)
			Assert.Same(connectors[0], connectors[index]);
	}

	private static DataConnector CreateConnector(IDataSource source, TimeProvider timeProvider, bool handleFailures = true)
	{
		var connector = new DataConnector(
			source,
			new DataConnector.CircuitBreakerOptions
			{
				Duration = TimeSpan.FromSeconds(1),
				MaximumDuration = TimeSpan.FromSeconds(8),
				Jitter = 0,
			},
			timeProvider);

		if(handleFailures)
			connector.Failed += (_, args, _) =>
			{
				args.ExceptionHandled = true;
				return ValueTask.CompletedTask;
			};

		return connector;
	}

	private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(5);
	private static TaskCompletionSource NewGate() => new(TaskCreationOptions.RunContinuationsAsynchronously);

	private static async Task AssertSessionCompletedAsync(DataSession session)
	{
		Assert.True(session.IsCompleted);
		await Assert.ThrowsAsync<DataException>(() => session.AcquireLeaseAsync().AsTask());
	}

	private static Task CompleteSessionAsync(DataSession session, SessionCompletion completion) => completion switch
	{
		SessionCompletion.Commit => session.CommitAsync().AsTask(),
		SessionCompletion.Rollback => session.RollbackAsync().AsTask(),
		_ => session.DisposeAsync().AsTask(),
	};

	private sealed class RecordingLogger : Zongsoft.Diagnostics.ILogger
	{
		private readonly ConcurrentQueue<Zongsoft.Diagnostics.LogEntry> _entries = new();

		public string Name => nameof(RecordingLogger);
		public IReadOnlyCollection<Zongsoft.Diagnostics.LogEntry> Entries => _entries;

		public ValueTask FlushAsync(CancellationToken cancellation = default) => ValueTask.CompletedTask;
		public void Clear()
		{
			while(_entries.TryDequeue(out _))
			{ }
		}

		public ValueTask LogAsync<TLog>(TLog log, CancellationToken cancellation = default) where TLog : Zongsoft.Diagnostics.ILog
		{
			if(log is Zongsoft.Diagnostics.LogEntry entry)
				_entries.Enqueue(entry);

			return ValueTask.CompletedTask;
		}
	}

	private sealed class ManualTimeProvider : TimeProvider
	{
		private readonly object _sync = new();
		private DateTimeOffset _utcNow = new(2026, 7, 29, 0, 0, 0, TimeSpan.Zero);

		public override DateTimeOffset GetUtcNow()
		{
			lock(_sync)
				return _utcNow;
		}

		public void Advance(TimeSpan duration)
		{
			lock(_sync)
				_utcNow += duration;
		}
	}

	internal sealed class DataSourceMocker : IDataSource
	{
		public DataSourceMocker(IDataDriver driver = null)
		{
			this.Driver = driver ?? new DataDriverMocker();
			this.Features = new FeatureCollection(this.Driver.Features);
			this.Properties = new Dictionary<string, object>
			{
				[DataConnector.CircuitBreakerOptions.DURATION_PROPERTY] = TimeSpan.FromSeconds(1),
				[DataConnector.CircuitBreakerOptions.MAXIMUM_DURATION_PROPERTY] = TimeSpan.FromSeconds(8),
				[DataConnector.CircuitBreakerOptions.JITTER_PROPERTY] = 0,
			};
		}

		public string Name => "Test";
		public string ConnectionString => "Server=database.example.com;Port=3306;Database=sample;UserName=tester;Password=secret;";
		public DataAccessMode Mode { get; set; } = DataAccessMode.All;
		public IDataDriver Driver { get; }
		public FeatureCollection Features { get; }
		public IDictionary<string, object> Properties { get; }

		public ValueTask<DataTable> GetSchemaAsync(string name, bool refresh = false, CancellationToken cancellation = default) => ValueTask.FromResult<DataTable>(null);
		public bool Equals(IDataSource other) => object.ReferenceEquals(this, other);
	}

}
