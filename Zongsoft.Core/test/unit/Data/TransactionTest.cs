using System;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.Data.Transactions;

namespace Zongsoft.Data.Tests;

public class TransactionTest
{
	[Fact]
	public async Task AmbientContext_NestedTransactions_RestoresParentContext()
	{
		Assert.Null(TransactionContext.Current);
		Assert.Null(Transaction.Current);

		await using(var root = new Transaction())
		{
			Assert.Same(root.Context, TransactionContext.Current);
			Assert.Same(root, Transaction.Current);
			Assert.Same(root.Context, root.Context.Root);
			Assert.Null(root.Context.Parent);

			await using(var child = new Transaction())
			{
				Assert.Same(child.Context, TransactionContext.Current);
				Assert.Same(child, Transaction.Current);
				Assert.Same(root.Context, child.Context.Parent);
				Assert.Same(root.Context, child.Context.Root);
				await child.CommitAsync();
			}

			Assert.Same(root.Context, TransactionContext.Current);
			Assert.Same(root, Transaction.Current);
			await root.CommitAsync();
		}

		Assert.Null(TransactionContext.Current);
		Assert.Null(Transaction.Current);
	}

	[Fact]
	public async Task NestedTransactionEnlistment_IsCompletedByRootTransaction()
	{
		await using var root = new Transaction();
		var recorder = new Recorder();

		await using(var child = new Transaction())
		{
			Assert.True(child.Context.Enlist(recorder));
			await child.CommitAsync();
			Assert.Empty(recorder.Contexts);
		}

		await root.CommitAsync();

		var context = Assert.Single(recorder.Contexts);
		Assert.Same(root, context.Transaction);
		Assert.Equal(EnlistmentPhase.Commit, context.Phase);
	}

	[Fact]
	public async Task Completed_Commit_NotifiesFinalStateExactlyOnce()
	{
		await using var transaction = new Transaction();
		var count = 0;
		object sender = null;
		TransactionStatus? status = null;

		transaction.Context.Completed += (source, _, _) =>
		{
			Interlocked.Increment(ref count);
			sender = source;
			status = transaction.Context.Status;
			return ValueTask.CompletedTask;
		};

		await transaction.CommitAsync();
		await transaction.CommitAsync();
		await transaction.RollbackAsync();

		Assert.Equal(1, count);
		Assert.Same(transaction.Context, sender);
		Assert.Equal(TransactionStatus.Committed, status);
		Assert.True(transaction.Context.IsCompleted);
	}

	[Fact]
	public async Task Completed_NestedRollback_MakesRootCompletionAborted()
	{
		await using var root = new Transaction();
		var recorder = new Recorder();
		Assert.True(root.Context.Enlist(recorder));
		var rootNotifications = 0;
		var childNotifications = 0;
		root.Context.Completed += (_, _, _) =>
		{
			Interlocked.Increment(ref rootNotifications);
			return ValueTask.CompletedTask;
		};

		await using(var child = new Transaction())
		{
			child.Context.Completed += (_, _, _) =>
			{
				Interlocked.Increment(ref childNotifications);
				return ValueTask.CompletedTask;
			};
			await child.RollbackAsync();

			Assert.Equal(1, childNotifications);
			Assert.Equal(TransactionStatus.Aborted, child.Context.Status);
			Assert.Equal(TransactionStatus.Active, root.Context.Status);
		}

		await root.CommitAsync();

		Assert.Equal(1, rootNotifications);
		Assert.Equal(TransactionStatus.Aborted, root.Context.Status);
		Assert.Equal(EnlistmentPhase.Rollback, Assert.Single(recorder.Contexts).Phase);
	}

	[Fact]
	public async Task Completed_ThrowingObserver_PreservesCommittedStateAndReportsFailure()
	{
		var transaction = new Transaction();
		var failure = new InvalidOperationException();
		var followingNotifications = 0;
		transaction.Context.Completed += (_, _, _) => throw failure;
		transaction.Context.Completed += (_, _, _) =>
		{
			Interlocked.Increment(ref followingNotifications);
			return ValueTask.CompletedTask;
		};

		try
		{
			Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => transaction.CommitAsync()));
			Assert.Equal(1, followingNotifications);
			Assert.Equal(TransactionStatus.Committed, transaction.Context.Status);
		}
		finally
		{
			Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => transaction.DisposeAsync().AsTask()));
		}
	}

	[Fact]
	public async Task ConcurrentTransactionsCompleteTheirOwnEnlistmentsAsync()
	{
		var remaining = 2;
		var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

		var commit = RunAsync(EnlistmentPhase.Commit);
		var rollback = RunAsync(EnlistmentPhase.Rollback);
		var results = await Task.WhenAll(commit, rollback);

		Assert.Equal(EnlistmentPhase.Commit, results[0].Phase);
		Assert.Equal(EnlistmentPhase.Rollback, results[1].Phase);

		async Task<EnlistmentContext> RunAsync(EnlistmentPhase phase)
		{
			return await Task.Run(async () =>
			{
				await using var transaction = new Transaction();
				var enlistment = new Recorder();

				Assert.True(transaction.Context.Enlist(enlistment));

				if(Interlocked.Decrement(ref remaining) == 0)
					ready.SetResult();

				await ready.Task.WaitAsync(TimeSpan.FromSeconds(10));

				if(phase == EnlistmentPhase.Commit)
					await transaction.CommitAsync();
				else
					await transaction.RollbackAsync();

				var context = Assert.Single(enlistment.Contexts);
				Assert.Same(transaction, context.Transaction);
				return context;
			});
		}
	}

	[Fact]
	public async Task EnlistRacingCompletionIsInvokedOnlyWhenAcceptedAsync()
	{
		const int COUNT = 256;

		for(int index = 0; index < COUNT; index++)
		{
			await using var transaction = new Transaction();
			var enlistment = new Recorder();
			var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

			var enrolling = Task.Run(async () =>
			{
				await start.Task;
				return transaction.Context.Enlist(enlistment);
			});

			var completing = Task.Run(async () =>
			{
				await start.Task;
				await transaction.CommitAsync();
			});

			start.SetResult();
			var accepted = await enrolling;
			await completing;

			Assert.Equal(accepted ? 1 : 0, enlistment.Contexts.Count);

			if(accepted)
			{
				var context = Assert.Single(enlistment.Contexts);
				Assert.Same(transaction, context.Transaction);
				Assert.Equal(EnlistmentPhase.Commit, context.Phase);
			}
		}
	}

	[Fact]
	public async Task CompletionContinuesAfterEnlistmentThrows()
	{
		var transaction = new Transaction();
		var following = new Recorder();
		var notifications = 0;
		TransactionStatus? completedStatus = null;
		transaction.Context.Completed += (_, _, _) =>
		{
			Interlocked.Increment(ref notifications);
			completedStatus = transaction.Context.Status;
			return ValueTask.CompletedTask;
		};

		Assert.True(transaction.Context.Enlist(new ThrowingEnlistment()));
		Assert.True(transaction.Context.Enlist(following));

		try
		{
			await Assert.ThrowsAsync<InvalidOperationException>(() => transaction.CommitAsync());

			var context = Assert.Single(following.Contexts);
			Assert.Same(transaction, context.Transaction);
			Assert.Equal(EnlistmentPhase.Commit, context.Phase);
			Assert.Equal(1, notifications);
			Assert.Equal(TransactionStatus.Undetermined, completedStatus);
		}
		finally
		{
			await Assert.ThrowsAsync<InvalidOperationException>(() => transaction.DisposeAsync().AsTask());
		}
	}

	[Theory]
	[InlineData(EnlistmentPhase.Commit)]
	[InlineData(EnlistmentPhase.Rollback)]
	public async Task CompleteAsync_UsesAsynchronousEnlistmentOnly(EnlistmentPhase phase)
	{
		await using var transaction = new Transaction();
		var enlistment = new PathRecorder();
		Assert.True(transaction.Context.Enlist(enlistment));

		await CompleteAsync(transaction, phase, CancellationToken.None);

		var context = Assert.Single(enlistment.Contexts);
		Assert.Equal(phase, context.Phase);
		Assert.Equal(1, enlistment.AsyncCalls);
		Assert.Equal(phase == EnlistmentPhase.Commit ? TransactionStatus.Committed : TransactionStatus.Aborted, transaction.Context.Status);
	}

	[Fact]
	public async Task CommitAsync_CompletedFiresAfterAsyncEnlistmentsFinishAsync()
	{
		await using var transaction = new Transaction();
		var enlistment = new AsyncRecorder();
		Assert.True(transaction.Context.Enlist(enlistment));

		TransactionStatus? statusAtCompletion = null;
		transaction.Context.Completed += (_, _, _) =>
		{
			//断言异步登记回调已经全部完成
			Assert.True(enlistment.IsCompleted);
			statusAtCompletion = transaction.Context.Status;
			return ValueTask.CompletedTask;
		};

		await transaction.CommitAsync();

		var context = Assert.Single(enlistment.Contexts);
		Assert.Same(transaction, context.Transaction);
		Assert.Equal(EnlistmentPhase.Commit, context.Phase);
		Assert.Equal(TransactionStatus.Committed, statusAtCompletion);
	}

	[Fact]
	public async Task RollbackAsync_CompletedFiresAfterAsyncEnlistmentsFinishAsync()
	{
		await using var transaction = new Transaction();
		var enlistment = new AsyncRecorder();
		Assert.True(transaction.Context.Enlist(enlistment));

		TransactionStatus? statusAtCompletion = null;
		transaction.Context.Completed += (_, _, _) =>
		{
			statusAtCompletion = transaction.Context.Status;
			return ValueTask.CompletedTask;
		};

		await transaction.RollbackAsync();

		Assert.True(enlistment.IsCompleted);
		Assert.Equal(TransactionStatus.Aborted, statusAtCompletion);
		Assert.Equal(TransactionStatus.Aborted, transaction.Context.Status);
	}

	[Theory]
	[InlineData(EnlistmentPhase.Commit)]
	[InlineData(EnlistmentPhase.Rollback)]
	public async Task CompleteAsync_PreCancelled_RejectsCommitButStillRollsBack(EnlistmentPhase phase)
	{
		await using var transaction = new Transaction();
		var enlistment = new PathRecorder();
		Assert.True(transaction.Context.Enlist(enlistment));
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();

		if(phase == EnlistmentPhase.Commit)
		{
			var failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => transaction.CommitAsync(cancellation.Token));
			Assert.Equal(cancellation.Token, failure.CancellationToken);
			Assert.Empty(enlistment.Contexts);
			Assert.Equal(TransactionStatus.Active, transaction.Context.Status);
			await transaction.CommitAsync();
		}
		else
			await transaction.RollbackAsync(cancellation.Token);

		var context = Assert.Single(enlistment.Contexts);
		Assert.Equal(phase, context.Phase);
		Assert.Equal(1, enlistment.AsyncCalls);
		Assert.False(enlistment.CancellationRequested);
		Assert.Equal(phase == EnlistmentPhase.Commit ? TransactionStatus.Committed : TransactionStatus.Aborted, transaction.Context.Status);
	}

	[Theory]
	[InlineData(EnlistmentPhase.Commit)]
	[InlineData(EnlistmentPhase.Rollback)]
	public async Task CompleteAsync_CancelledAfterDecision_WaitsForAcceptedCompletionAsync(EnlistmentPhase phase)
	{
		await using var transaction = new Transaction();
		var enlistment = new GatedEnlistment();
		Assert.True(transaction.Context.Enlist(enlistment));
		using var cancellation = new CancellationTokenSource();

		var completion = CompleteAsync(transaction, phase, cancellation.Token);

		try
		{
			await enlistment.Entered.WaitAsync(TimeSpan.FromSeconds(5));
			cancellation.Cancel();

			Assert.False(completion.IsCompleted);
		}
		finally { enlistment.Release(); }

		await completion.WaitAsync(TimeSpan.FromSeconds(5));

		var context = Assert.Single(enlistment.Contexts);
		Assert.Equal(phase, context.Phase);
		Assert.Equal(phase == EnlistmentPhase.Commit ? TransactionStatus.Committed : TransactionStatus.Aborted, transaction.Context.Status);
	}

	[Fact]
	public async Task DisposeAsync_RollsBackAndExitsAmbientContextAsync()
	{
		Assert.Null(TransactionContext.Current);

		var transaction = new Transaction();
		Assert.Same(transaction, Transaction.Current);

		await transaction.DisposeAsync();

		Assert.Null(Transaction.Current);
		Assert.Null(TransactionContext.Current);
		Assert.Equal(TransactionStatus.Aborted, transaction.Context.Status);
	}

	private sealed class Recorder : IEnlistment
	{
		private readonly System.Collections.Concurrent.ConcurrentQueue<EnlistmentContext> _contexts = new();
		public System.Collections.Generic.IReadOnlyCollection<EnlistmentContext> Contexts => _contexts.ToArray();

		public ValueTask OnEnlistAsync(EnlistmentContext context, CancellationToken cancellation) { _contexts.Enqueue(context); return ValueTask.CompletedTask; }
	}

	private sealed class AsyncRecorder : IEnlistment
	{
		private readonly System.Collections.Concurrent.ConcurrentQueue<EnlistmentContext> _contexts = new();
		public System.Collections.Generic.IReadOnlyCollection<EnlistmentContext> Contexts => _contexts.ToArray();
		public bool IsCompleted { get; private set; }

		public async ValueTask OnEnlistAsync(EnlistmentContext context, CancellationToken cancellation)
		{
			await Task.Yield();
			_contexts.Enqueue(context);
			this.IsCompleted = true;
		}
	}

	private sealed class PathRecorder : IEnlistment
	{
		private int _asyncCalls;
		private readonly System.Collections.Concurrent.ConcurrentQueue<EnlistmentContext> _contexts = new();

		public int AsyncCalls => _asyncCalls;
		public bool CancellationRequested { get; private set; }
		public System.Collections.Generic.IReadOnlyCollection<EnlistmentContext> Contexts => _contexts.ToArray();

		public ValueTask OnEnlistAsync(EnlistmentContext context, CancellationToken cancellation)
		{
			Interlocked.Increment(ref _asyncCalls);
			this.CancellationRequested = cancellation.IsCancellationRequested;
			_contexts.Enqueue(context);
			return ValueTask.CompletedTask;
		}
	}

	private sealed class ThrowingEnlistment : IEnlistment
	{
		public ValueTask OnEnlistAsync(EnlistmentContext context, CancellationToken cancellation) => throw new InvalidOperationException("Expected enlistment failure.");
	}

	private sealed class GatedEnlistment : IEnlistment
	{
		private readonly System.Collections.Concurrent.ConcurrentQueue<EnlistmentContext> _contexts = new();
		private readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
		private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public Task Entered => _entered.Task;
		public System.Collections.Generic.IReadOnlyCollection<EnlistmentContext> Contexts => _contexts.ToArray();

		public async ValueTask OnEnlistAsync(EnlistmentContext context, CancellationToken cancellation)
		{
			_contexts.Enqueue(context);
			_entered.TrySetResult();
			await _release.Task.WaitAsync(cancellation);
		}

		public void Release() => _release.TrySetResult();
	}

	private static Task CompleteAsync(Transaction transaction, EnlistmentPhase phase, CancellationToken cancellation) =>
		phase == EnlistmentPhase.Commit ? transaction.CommitAsync(cancellation) : transaction.RollbackAsync(cancellation);
}
