using System;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.Data.Transactions;

namespace Zongsoft.Data.Tests;

public class TransactionTest
{
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

	private sealed class Recorder : IEnlistment
	{
		private readonly System.Collections.Concurrent.ConcurrentQueue<EnlistmentContext> _contexts = new();
		public System.Collections.Generic.IReadOnlyCollection<EnlistmentContext> Contexts => _contexts.ToArray();

		public void OnEnlist(EnlistmentContext context) => _contexts.Enqueue(context);
	}
}
