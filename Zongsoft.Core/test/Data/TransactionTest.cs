using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.Data.Transactions;

namespace Zongsoft.Data.Tests;

public class TransactionTest
{
	[Fact]
	public void TransactionContext_CompletionMethods_AreInternal()
	{
		AssertInternal(nameof(Transaction.Commit));
		AssertInternal(nameof(Transaction.Rollback));

		static void AssertInternal(string name)
		{
			var publicMethod = typeof(TransactionContext).GetMethod(name, BindingFlags.Instance | BindingFlags.Public);
			var internalMethod = typeof(TransactionContext).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);

			Assert.Null(publicMethod);
			Assert.NotNull(internalMethod);
			Assert.True(internalMethod.IsAssembly);
			Assert.False(internalMethod.IsPublic);
		}
	}

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

	private sealed class Recorder : IEnlistment
	{
		private readonly System.Collections.Concurrent.ConcurrentQueue<EnlistmentContext> _contexts = new();
		public System.Collections.Generic.IReadOnlyCollection<EnlistmentContext> Contexts => _contexts.ToArray();

		public void OnEnlist(EnlistmentContext context) => _contexts.Enqueue(context);
	}

	private sealed class ThrowingEnlistment : IEnlistment
	{
		public void OnEnlist(EnlistmentContext context) => throw new InvalidOperationException("Expected enlistment failure.");
	}
}
