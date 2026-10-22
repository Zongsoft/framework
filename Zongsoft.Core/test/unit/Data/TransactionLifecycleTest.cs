using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.Data.Transactions;

namespace Zongsoft.Data.Tests;

public class TransactionLifecycleTest
{
	private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(5);

	[Theory]
	[InlineData(true, false)]
	[InlineData(false, false)]
	[InlineData(true, true)]
	[InlineData(false, true)]
	public async Task CompleteAsync_ConcurrentCalls_ShareAcceptedCompletion(bool commit, bool fail)
	{
		var transaction = new Transaction();
		var entered = NewGate();
		var release = NewGate();
		var failure = new InvalidOperationException();
		var calls = 0;
		var notifications = 0;
		var acceptedPhase = default(EnlistmentPhase);
		Assert.True(transaction.Context.Enlist(new CallbackEnlistment(async (context, cancellation) =>
		{
			Interlocked.Increment(ref calls);
			acceptedPhase = context.Phase;
			Assert.False(cancellation.CanBeCanceled);
			entered.TrySetResult();
			await release.Task;
		})));
		transaction.Context.Completed += (_, _, cancellation) =>
		{
			Assert.False(cancellation.CanBeCanceled);
			Interlocked.Increment(ref notifications);
			return ValueTask.CompletedTask;
		};

		try
		{
			var first = commit ? transaction.CommitAsync() : transaction.RollbackAsync();
			await entered.Task.WaitAsync(_timeout);
			using var cancellation = new CancellationTokenSource();
			cancellation.Cancel();
			var concurrent = await Task.WhenAll(Enumerable.Range(0, 12).Select(index => Task.Run(() =>
				Tuple.Create((index % 3) switch
				{
					0 => transaction.CommitAsync(cancellation.Token),
					1 => transaction.RollbackAsync(cancellation.Token),
					_ => transaction.DisposeAsync().AsTask(),
				}))));

			Assert.All(concurrent, result => Assert.Same(first, result.Item1));
			Assert.False(first.IsCompleted);
			Assert.Equal(1, calls);
			Assert.Equal(0, notifications);
			Assert.Equal(commit ? EnlistmentPhase.Commit : EnlistmentPhase.Rollback, acceptedPhase);

			if(fail)
				release.SetException(failure);
			else
				release.SetResult();

			if(fail)
			{
				Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => first.WaitAsync(_timeout)));
				Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => transaction.DisposeAsync().AsTask()));
				Assert.Equal(TransactionStatus.Undetermined, transaction.Context.Status);
			}
			else
			{
				await first.WaitAsync(_timeout);
				Assert.Equal(commit ? TransactionStatus.Committed : TransactionStatus.Aborted, transaction.Context.Status);
			}

			Assert.Same(first, transaction.CommitAsync());
			Assert.Same(first, transaction.RollbackAsync());
			Assert.Same(first, transaction.DisposeAsync().AsTask());
			Assert.Equal(1, notifications);
			Assert.Equal(1, calls);
		}
		finally
		{
			release.TrySetResult();
			await Record.ExceptionAsync(() => transaction.DisposeAsync().AsTask().WaitAsync(_timeout));
		}
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task CompleteAsync_PendingObserver_WaitsForOrderedNotifications(bool commit)
	{
		await using var transaction = new Transaction();
		var entered = NewGate();
		var release = NewGate();
		var trace = new List<string>();
		var status = default(TransactionStatus);
		Assert.True(transaction.Context.Enlist(new CallbackEnlistment((_, _) =>
		{
			trace.Add("enlistment");
			return ValueTask.CompletedTask;
		})));
		transaction.Context.Completed += async (sender, args, cancellation) =>
		{
			Assert.Same(transaction.Context, sender);
			Assert.Same(EventArgs.Empty, args);
			Assert.False(cancellation.CanBeCanceled);
			status = transaction.Context.Status;
			trace.Add("first-started");
			entered.TrySetResult();
			await release.Task;
			trace.Add("first-ended");
		};
		transaction.Context.Completed += (_, _, _) =>
		{
			trace.Add("second");
			return ValueTask.CompletedTask;
		};
		var completion = commit ? transaction.CommitAsync() : transaction.RollbackAsync();

		try
		{
			await entered.Task.WaitAsync(_timeout);
			Assert.False(completion.IsCompleted);
			Assert.Equal(commit ? TransactionStatus.Committed : TransactionStatus.Aborted, status);
			Assert.Equal(["enlistment", "first-started"], trace);
		}
		finally { release.TrySetResult(); }

		await completion.WaitAsync(_timeout);
		Assert.Equal(["enlistment", "first-started", "first-ended", "second"], trace);
	}

	[Fact]
	public async Task CompleteAsync_EnlistmentAndNotificationFailures_AttemptsAllAndPreservesErrors()
	{
		var transaction = new Transaction();
		var first = new InvalidOperationException();
		var second = new ArgumentException();
		var third = new NotSupportedException();
		var fourth = new ApplicationException();
		var trace = new List<string>();
		Assert.True(transaction.Context.Enlist(new CallbackEnlistment((_, _) =>
		{
			trace.Add("enlistment-sync-failure");
			throw first;
		})));
		Assert.True(transaction.Context.Enlist(new CallbackEnlistment(async (_, _) =>
		{
			await Task.Yield();
			trace.Add("enlistment-async-failure");
			throw second;
		})));
		transaction.Context.Completed += (_, _, _) =>
		{
			trace.Add("notification-sync-failure");
			throw third;
		};
		transaction.Context.Completed += async (_, _, _) =>
		{
			await Task.Yield();
			trace.Add("notification-async-failure");
			throw fourth;
		};
		transaction.Context.Completed += (_, _, _) =>
		{
			trace.Add("last-notification");
			return ValueTask.CompletedTask;
		};

		try
		{
			var failure = await Assert.ThrowsAsync<AggregateException>(() => transaction.CommitAsync().WaitAsync(_timeout));
			Assert.Collection(failure.InnerExceptions,
				error => Assert.Same(first, error),
				error => Assert.Same(second, error),
				error => Assert.Same(third, error),
				error => Assert.Same(fourth, error));
			Assert.Equal(["enlistment-sync-failure", "enlistment-async-failure", "notification-sync-failure", "notification-async-failure", "last-notification"], trace);
			Assert.Equal(TransactionStatus.Undetermined, transaction.Context.Status);
			Assert.Same(failure, await Assert.ThrowsAsync<AggregateException>(() => transaction.DisposeAsync().AsTask()));
		}
		finally
		{
			await Record.ExceptionAsync(() => transaction.DisposeAsync().AsTask().WaitAsync(_timeout));
		}
	}

	[Theory]
	[InlineData(false, false)]
	[InlineData(false, true)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public async Task DisposeAsync_PendingRollback_ExitsCallingContextBeforeCompletion(bool nested, bool fail)
	{
		var previous = Transaction.Current;
		var parent = nested ? new Transaction() : null;
		var expected = parent ?? previous;
		var transaction = new Transaction();
		var entered = NewGate();
		var release = NewGate();
		var failure = new InvalidOperationException();

		if(nested)
			transaction.Context.Completed += (_, _, _) => new(WaitAsync());
		else
		{
			Assert.True(transaction.Context.Enlist(new CallbackEnlistment(async (context, _) =>
			{
				Assert.Equal(EnlistmentPhase.Rollback, context.Phase);
				await WaitAsync();
			})));
		}

		try
		{
			var completion = transaction.DisposeAsync().AsTask();
			Assert.Same(expected, Transaction.Current);
			Assert.Same(expected?.Context, TransactionContext.Current);
			await entered.Task.WaitAsync(_timeout);
			Assert.False(completion.IsCompleted);
			Assert.Same(completion, transaction.DisposeAsync().AsTask());

			if(fail)
				release.SetException(failure);
			else
				release.SetResult();

			if(fail)
				Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => completion.WaitAsync(_timeout)));
			else
				await completion.WaitAsync(_timeout);

			Assert.Same(expected, Transaction.Current);
			Assert.Same(expected?.Context, TransactionContext.Current);
		}
		finally
		{
			release.TrySetResult();
			await Record.ExceptionAsync(() => transaction.DisposeAsync().AsTask().WaitAsync(_timeout));

			if(parent != null)
				await parent.DisposeAsync();
		}

		Assert.Same(previous, Transaction.Current);

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
	public async Task RollbackAsync_CauseAndSuccessfulRollback_LeavesPropagationToCaller(bool canceled, bool rethrow)
	{
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		Exception cause = canceled ? new OperationCanceledException(cancellation.Token) : new InvalidOperationException();
		Transaction transaction = null;
		var rollbacks = 0;
		var failure = await Record.ExceptionAsync(ExecuteAsync);

		Assert.Same(rethrow ? cause : null, failure);
		Assert.Equal(TransactionStatus.Aborted, transaction.Context.Status);
		Assert.Equal(1, rollbacks);

		async Task ExecuteAsync()
		{
			await using var scope = transaction = new Transaction();
			Assert.True(scope.Context.Enlist(new CallbackEnlistment((context, token) =>
			{
				Assert.Equal(EnlistmentPhase.Rollback, context.Phase);
				Assert.False(token.CanBeCanceled);
				rollbacks++;
				return ValueTask.CompletedTask;
			})));

			try
			{
				await Task.Yield();
				throw cause;
			}
			catch(Exception exception)
			{
				await scope.RollbackAsync(exception);

				if(rethrow)
					throw;
			}
		}
	}

	[Theory]
	[InlineData(false, 0)]
	[InlineData(true, 0)]
	[InlineData(false, 1)]
	[InlineData(true, 1)]
	[InlineData(false, 2)]
	[InlineData(true, 2)]
	public async Task RollbackAsync_CauseAndFailures_PreservesErrorsThroughDisposal(bool canceled, int failureKind)
	{
		Exception cause = canceled ? new OperationCanceledException(new CancellationToken(true)) : new InvalidOperationException();
		var rollbackFailure = new ApplicationException();
		var notificationFailure = new NotSupportedException();
		Transaction transaction = null;
		AggregateException observed = null;
		var rollbacks = 0;
		var notifications = 0;
		var expected = new List<Exception> { cause };

		if(failureKind != 1)
			expected.Add(rollbackFailure);
		if(failureKind != 0)
			expected.Add(notificationFailure);

		var failure = await Assert.ThrowsAsync<AggregateException>(ExecuteAsync);
		Assert.Same(observed, failure);
		Assert.Equal(expected, failure.InnerExceptions);
		Assert.Equal(failureKind == 1 ? TransactionStatus.Aborted : TransactionStatus.Undetermined, transaction.Context.Status);
		Assert.Same(failure, await Assert.ThrowsAsync<AggregateException>(() => transaction.DisposeAsync().AsTask()));
		Assert.Equal(2, rollbacks);
		Assert.Equal(2, notifications);

		async Task ExecuteAsync()
		{
			await using var scope = transaction = new Transaction();
			Assert.True(scope.Context.Enlist(new CallbackEnlistment(async (context, token) =>
			{
				Assert.Equal(EnlistmentPhase.Rollback, context.Phase);
				Assert.False(token.CanBeCanceled);
				rollbacks++;
				await Task.Yield();

				if(failureKind != 1)
					throw rollbackFailure;
			})));
			Assert.True(scope.Context.Enlist(new CallbackEnlistment((_, _) =>
			{
				rollbacks++;
				return ValueTask.CompletedTask;
			})));
			scope.Context.Completed += (_, _, _) =>
			{
				notifications++;
				if(failureKind != 0)
					throw notificationFailure;

				return ValueTask.CompletedTask;
			};
			scope.Context.Completed += (_, _, _) =>
			{
				notifications++;
				return ValueTask.CompletedTask;
			};

			try
			{
				throw cause;
			}
			catch(Exception exception)
			{
				observed = await Assert.ThrowsAsync<AggregateException>(() => scope.RollbackAsync(exception));
				throw observed;
			}
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task RollbackAsync_CauseAndPendingCompletion_ReusesFirstResult(bool fail)
	{
		var transaction = new Transaction();
		var cause = new InvalidOperationException();
		var failure = new ApplicationException();
		var entered = NewGate();
		var release = NewGate();
		var calls = 0;
		Assert.True(transaction.Context.Enlist(new CallbackEnlistment(async (_, _) =>
		{
			calls++;
			entered.TrySetResult();
			await release.Task;

			if(fail)
				throw failure;
		})));

		try
		{
			var completion = transaction.RollbackAsync(cause);
			await entered.Task.WaitAsync(_timeout);
			Assert.False(completion.IsCompleted);
			Assert.Same(completion, transaction.RollbackAsync(new NotSupportedException()));
			Assert.Same(completion, transaction.RollbackAsync());
			Assert.Same(completion, transaction.CommitAsync());
			Assert.Same(completion, transaction.DisposeAsync().AsTask());
			release.SetResult();

			if(fail)
			{
				var aggregate = await Assert.ThrowsAsync<AggregateException>(() => completion.WaitAsync(_timeout));
				Assert.Collection(aggregate.InnerExceptions, error => Assert.Same(cause, error), error => Assert.Same(failure, error));
				Assert.Same(aggregate, await Assert.ThrowsAsync<AggregateException>(() => transaction.RollbackAsync(new Exception())));
			}
			else
				await completion.WaitAsync(_timeout);

			Assert.Equal(1, calls);
		}
		finally
		{
			release.TrySetResult();
			await Record.ExceptionAsync(() => transaction.DisposeAsync().AsTask().WaitAsync(_timeout));
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task RollbackAsync_AfterCommit_ReusesResultWithoutRollbackOrDuplicateErrors(bool fail)
	{
		var transaction = new Transaction();
		var failure = new InvalidOperationException();
		var phases = new List<EnlistmentPhase>();
		Assert.True(transaction.Context.Enlist(new CallbackEnlistment((context, _) =>
		{
			phases.Add(context.Phase);
			if(fail)
				throw failure;

			return ValueTask.CompletedTask;
		})));

		try
		{
			var completion = transaction.CommitAsync();
			Assert.Same(fail ? failure : null, await Record.ExceptionAsync(() => completion));
			Assert.Same(completion, transaction.RollbackAsync(failure));
			Assert.Same(fail ? failure : null, await Record.ExceptionAsync(() => transaction.RollbackAsync(failure)));
			Assert.Same(fail ? failure : null, await Record.ExceptionAsync(() => transaction.DisposeAsync().AsTask()));
			Assert.Equal([EnlistmentPhase.Commit], phases);
			Assert.Equal(fail ? TransactionStatus.Undetermined : TransactionStatus.Committed, transaction.Context.Status);
		}
		finally
		{
			await Record.ExceptionAsync(() => transaction.DisposeAsync().AsTask());
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task RollbackAsync_NullCause_PreservesOrdinaryRollbackResult(bool fail)
	{
		var transaction = new Transaction();
		var failure = new InvalidOperationException();
		Assert.True(transaction.Context.Enlist(new CallbackEnlistment((_, _) =>
		{
			if(fail)
				throw failure;

			return ValueTask.CompletedTask;
		})));

		try
		{
			Assert.Same(fail ? failure : null, await Record.ExceptionAsync(() => transaction.RollbackAsync(null)));
			Assert.Equal(fail ? TransactionStatus.Undetermined : TransactionStatus.Aborted, transaction.Context.Status);
		}
		finally
		{
			await Record.ExceptionAsync(() => transaction.DisposeAsync().AsTask());
		}
	}

	[Fact]
	public async Task RollbackAsync_CauseAlsoThrownByRollback_DoesNotDuplicateException()
	{
		var transaction = new Transaction();
		var cause = new InvalidOperationException();
		Assert.True(transaction.Context.Enlist(new CallbackEnlistment((_, _) => throw cause)));

		try
		{
			var failure = await Assert.ThrowsAsync<AggregateException>(() => transaction.RollbackAsync(cause));
			Assert.Same(cause, Assert.Single(failure.InnerExceptions));
			Assert.Equal(TransactionStatus.Undetermined, transaction.Context.Status);
		}
		finally
		{
			await Record.ExceptionAsync(() => transaction.DisposeAsync().AsTask());
		}
	}

	private static TaskCompletionSource NewGate() => new(TaskCreationOptions.RunContinuationsAsynchronously);

	private sealed class CallbackEnlistment(Func<EnlistmentContext, CancellationToken, ValueTask> callback) : IEnlistment
	{
		public ValueTask OnEnlistAsync(EnlistmentContext context, CancellationToken cancellation = default) => callback(context, cancellation);
	}
}
