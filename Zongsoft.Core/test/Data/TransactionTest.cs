using System;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.Data.Transactions;

namespace Zongsoft.Data.Tests;

public class TransactionTest
{
	[Fact]
	public void AmbientContext_NestedTransactions_RestoresParentContext()
	{
		Assert.Null(TransactionContext.Current);
		Assert.Null(Transaction.Current);

		using(var root = new Transaction())
		{
			Assert.Same(root.Context, TransactionContext.Current);
			Assert.Same(root, Transaction.Current);
			Assert.Same(root.Context, root.Context.Root);
			Assert.Null(root.Context.Parent);

			using(var child = new Transaction())
			{
				Assert.Same(child.Context, TransactionContext.Current);
				Assert.Same(child, Transaction.Current);
				Assert.Same(root.Context, child.Context.Parent);
				Assert.Same(root.Context, child.Context.Root);
				child.Commit();
			}

			Assert.Same(root.Context, TransactionContext.Current);
			Assert.Same(root, Transaction.Current);
			root.Commit();
		}

		Assert.Null(TransactionContext.Current);
		Assert.Null(Transaction.Current);
	}

	[Fact]
	public void NestedTransactionEnlistment_IsCompletedByRootTransaction()
	{
		using var root = new Transaction();
		var recorder = new Recorder();

		using(var child = new Transaction())
		{
			Assert.True(child.Enlist(recorder));
			child.Commit();
			Assert.Empty(recorder.Contexts);
		}

		root.Commit();

		var context = Assert.Single(recorder.Contexts);
		Assert.Same(root, context.Transaction);
		Assert.Equal(EnlistmentPhase.Commit, context.Phase);
	}

	[Fact]
	public void Completed_Commit_NotifiesFinalStateExactlyOnce()
	{
		using var transaction = new Transaction();
		var count = 0;
		object sender = null;
		TransactionStatus? status = null;

		transaction.Context.Completed += (source, _) =>
		{
			Interlocked.Increment(ref count);
			sender = source;
			status = transaction.Context.Status;
		};

		transaction.Commit();
		transaction.Commit();
		transaction.Rollback();

		Assert.Equal(1, count);
		Assert.Same(transaction.Context, sender);
		Assert.Equal(TransactionStatus.Committed, status);
		Assert.True(transaction.Context.IsCompleted);
	}

	[Fact]
	public void Completed_NestedRollback_MakesRootCompletionAborted()
	{
		using var root = new Transaction();
		var rootNotifications = 0;
		var childNotifications = 0;
		root.Context.Completed += (_, _) => Interlocked.Increment(ref rootNotifications);

		using(var child = new Transaction())
		{
			child.Context.Completed += (_, _) => Interlocked.Increment(ref childNotifications);
			child.Rollback();

			Assert.Equal(1, childNotifications);
			Assert.Equal(TransactionStatus.Aborted, child.Context.Status);
			Assert.Equal(TransactionStatus.Active, root.Context.Status);
		}

		root.Commit();

		Assert.Equal(1, rootNotifications);
		Assert.Equal(TransactionStatus.Aborted, root.Context.Status);
	}

	[Fact]
	public void Completed_ThrowingObserver_DoesNotEscapeOrSuppressFollowingObserver()
	{
		using var transaction = new Transaction();
		var followingNotifications = 0;
		transaction.Context.Completed += (_, _) => throw new InvalidOperationException("Expected observer failure.");
		transaction.Context.Completed += (_, _) => Interlocked.Increment(ref followingNotifications);

		var exception = Record.Exception(transaction.Commit);

		Assert.Null(exception);
		Assert.Equal(1, followingNotifications);
		Assert.Equal(TransactionStatus.Committed, transaction.Context.Status);
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
				using var transaction = new Transaction();
				var enlistment = new Recorder();

				Assert.True(transaction.Enlist(enlistment));

				if(Interlocked.Decrement(ref remaining) == 0)
					ready.SetResult();

				await ready.Task.WaitAsync(TimeSpan.FromSeconds(10));

				if(phase == EnlistmentPhase.Commit)
					transaction.Commit();
				else
					transaction.Rollback();

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
			using var transaction = new Transaction();
			var enlistment = new Recorder();
			var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

			var enrolling = Task.Run(async () =>
			{
				await start.Task;
				return transaction.Enlist(enlistment);
			});

			var completing = Task.Run(async () =>
			{
				await start.Task;
				transaction.Commit();
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
	public void CompletionContinuesAfterEnlistmentThrows()
	{
		using var transaction = new Transaction();
		var following = new Recorder();
		var notifications = 0;
		TransactionStatus? completedStatus = null;
		transaction.Context.Completed += (_, _) =>
		{
			Interlocked.Increment(ref notifications);
			completedStatus = transaction.Context.Status;
		};

		Assert.True(transaction.Enlist(new ThrowingEnlistment()));
		Assert.True(transaction.Enlist(following));

		Assert.Throws<InvalidOperationException>(transaction.Commit);

		var context = Assert.Single(following.Contexts);
		Assert.Same(transaction, context.Transaction);
		Assert.Equal(EnlistmentPhase.Commit, context.Phase);
		Assert.Equal(1, notifications);
		Assert.Equal(TransactionStatus.Undetermined, completedStatus);
	}

	[Theory]
	[InlineData(EnlistmentPhase.Commit)]
	[InlineData(EnlistmentPhase.Rollback)]
	public void Complete_UsesSynchronousEnlistmentOnly(EnlistmentPhase phase)
	{
		using var transaction = new Transaction();
		var enlistment = new PathRecorder();
		Assert.True(transaction.Enlist(enlistment));

		Complete(transaction, phase);

		var context = Assert.Single(enlistment.Contexts);
		Assert.Equal(phase, context.Phase);
		Assert.Equal(0, enlistment.AsyncCalls);
		Assert.Equal(phase == EnlistmentPhase.Commit ? TransactionStatus.Committed : TransactionStatus.Aborted, transaction.Context.Status);
	}

	[Fact]
	public async Task CommitAsync_CompletedFiresAfterAsyncEnlistmentsFinishAsync()
	{
		using var transaction = new Transaction();
		var enlistment = new AsyncRecorder();
		Assert.True(transaction.Enlist(enlistment));

		TransactionStatus? statusAtCompletion = null;
		transaction.Context.Completed += (_, _) =>
		{
			//断言异步登记回调已经全部完成
			Assert.True(enlistment.IsCompleted);
			statusAtCompletion = transaction.Context.Status;
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
		using var transaction = new Transaction();
		var enlistment = new AsyncRecorder();
		Assert.True(transaction.Enlist(enlistment));

		TransactionStatus? statusAtCompletion = null;
		transaction.Context.Completed += (_, _) => statusAtCompletion = transaction.Context.Status;

		await transaction.RollbackAsync();

		Assert.True(enlistment.IsCompleted);
		Assert.Equal(TransactionStatus.Aborted, statusAtCompletion);
		Assert.Equal(TransactionStatus.Aborted, transaction.Context.Status);
	}

	[Theory]
	[InlineData(EnlistmentPhase.Commit)]
	[InlineData(EnlistmentPhase.Rollback)]
	public async Task CompleteAsync_PreCancelled_CompletesWithNonCancellableEnlistmentAsync(EnlistmentPhase phase)
	{
		using var transaction = new Transaction();
		var enlistment = new PathRecorder();
		Assert.True(transaction.Enlist(enlistment));
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();

		Task completion = CompleteAsync(transaction, phase, cancellation.Token);
		await completion;

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
		using var transaction = new Transaction();
		var enlistment = new GatedEnlistment();
		Assert.True(transaction.Enlist(enlistment));
		using var cancellation = new CancellationTokenSource();

		var completion = CompleteAsync(transaction, phase, cancellation.Token);
		await enlistment.Entered.WaitAsync(TimeSpan.FromSeconds(5));
		cancellation.Cancel();

		Assert.False(completion.IsCompleted);
		enlistment.Release();
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

		public void OnEnlist(EnlistmentContext context) => _contexts.Enqueue(context);
		public ValueTask OnEnlistAsync(EnlistmentContext context, CancellationToken cancellation) { _contexts.Enqueue(context); return ValueTask.CompletedTask; }
	}

	private sealed class AsyncRecorder : IEnlistment
	{
		private readonly System.Collections.Concurrent.ConcurrentQueue<EnlistmentContext> _contexts = new();
		public System.Collections.Generic.IReadOnlyCollection<EnlistmentContext> Contexts => _contexts.ToArray();
		public bool IsCompleted { get; private set; }

		public void OnEnlist(EnlistmentContext context) => _contexts.Enqueue(context);
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

		public void OnEnlist(EnlistmentContext context) => _contexts.Enqueue(context);
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
		public void OnEnlist(EnlistmentContext context) => throw new InvalidOperationException("Expected enlistment failure.");
		public ValueTask OnEnlistAsync(EnlistmentContext context, CancellationToken cancellation) => throw new InvalidOperationException("Expected enlistment failure.");
	}

	private sealed class GatedEnlistment : IEnlistment
	{
		private readonly System.Collections.Concurrent.ConcurrentQueue<EnlistmentContext> _contexts = new();
		private readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
		private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public Task Entered => _entered.Task;
		public System.Collections.Generic.IReadOnlyCollection<EnlistmentContext> Contexts => _contexts.ToArray();

		public void OnEnlist(EnlistmentContext context) => throw new NotSupportedException();
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

	private static void Complete(Transaction transaction, EnlistmentPhase phase)
	{
		if(phase == EnlistmentPhase.Commit)
			transaction.Commit();
		else
			transaction.Rollback();
	}
}
