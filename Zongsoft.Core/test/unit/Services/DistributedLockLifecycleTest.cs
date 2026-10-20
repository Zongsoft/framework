using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.Services.Distributing;

namespace Zongsoft.Tests.Services;

public class DistributedLockLifecycleTest
{
	[Theory]
	[InlineData(false, false)]
	[InlineData(false, true)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public async Task DisposeAsync_PendingRelease_SharesCompletionAndFailure(bool exitFirst, bool fail)
	{
		var manager = new TestManager();
		var distributedLock = new TestLock(manager);
		var failure = new InvalidOperationException();
		Task<bool> exit = null;
		Task[] disposals = [];
		var returned = new TaskCompletionSource<Task>(TaskCreationOptions.RunContinuationsAsynchronously);
		var invocation = Task.Run(() =>
		{
			try
			{
				if(exitFirst)
					exit = distributedLock.ExitAsync().AsTask();

				returned.SetResult(distributedLock.DisposeAsync().AsTask());
			}
			catch(Exception exception) { returned.SetResult(Task.FromException(exception)); }
		});

		try
		{
			var first = await returned.Task.WaitAsync(TimeSpan.FromSeconds(5));
			await manager.Started.WaitAsync(TimeSpan.FromSeconds(5));
			disposals = Enumerable.Range(0, 16).Select(_ => distributedLock.DisposeAsync().AsTask()).ToArray();
			Assert.All(disposals, disposal => Assert.Same(first, disposal));
			Assert.False(first.IsCompleted);
			Assert.Equal(1, manager.ReleaseCount);
			Assert.Equal(1, distributedLock.DisposalCount);
			Assert.False(distributedLock.IsHeld);
			Assert.Equal(distributedLock.Key, manager.Key);
			Assert.Same(distributedLock.Token, manager.Token);

			if(exitFirst)
				Assert.False(exit.IsCompleted);

			if(fail)
				manager.Completion.SetException(failure);
			else
				manager.Completion.SetResult(true);

			foreach(var disposal in disposals)
			{
				if(fail)
					Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => disposal.WaitAsync(TimeSpan.FromSeconds(5))));
				else
					await disposal.WaitAsync(TimeSpan.FromSeconds(5));
			}

			if(exitFirst)
			{
				if(fail)
					Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => exit.WaitAsync(TimeSpan.FromSeconds(5))));
				else
					Assert.True(await exit.WaitAsync(TimeSpan.FromSeconds(5)));
			}

			Assert.Same(first, distributedLock.DisposeAsync().AsTask());
			Assert.Equal(1, manager.ReleaseCount);
		}
		finally
		{
			manager.Completion.TrySetResult(true);
			await invocation.WaitAsync(TimeSpan.FromSeconds(5));
			await Record.ExceptionAsync(() => distributedLock.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5)));

			if(exit != null)
				await Record.ExceptionAsync(() => exit.WaitAsync(TimeSpan.FromSeconds(5)));
		}
	}

	[Fact]
	public async Task ExitAsync_CanceledRelease_DisposeObservesSameCancellationWithoutReleasingAgain()
	{
		using var cancellation = new CancellationTokenSource();
		var manager = new TestManager();
		var distributedLock = new TestLock(manager);
		var exit = distributedLock.ExitAsync(cancellation.Token).AsTask();
		await manager.Started.WaitAsync(TimeSpan.FromSeconds(5));

		try
		{
			Assert.Equal(cancellation.Token, manager.Cancellation);
			Assert.False(exit.IsCompleted);
			cancellation.Cancel();
			manager.Completion.SetCanceled(cancellation.Token);

			var exitError = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => exit.WaitAsync(TimeSpan.FromSeconds(5)));
			var disposeError = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => distributedLock.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5)));

			Assert.Equal(cancellation.Token, exitError.CancellationToken);
			Assert.Equal(cancellation.Token, disposeError.CancellationToken);
			Assert.Equal(1, manager.ReleaseCount);
			Assert.False(distributedLock.IsHeld);
		}
		finally
		{
			manager.Completion.TrySetCanceled(cancellation.Token);
			await Record.ExceptionAsync(() => distributedLock.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5)));
		}
	}

	private sealed class TestLock(TestManager manager) : DistributedLockBase<TestManager>(manager, "test-lock", [1, 2, 3], TimeSpan.FromMinutes(1), true)
	{
		public int DisposalCount { get; private set; }
		protected override ValueTask<bool> OnEnterAsync(CancellationToken cancellation) => ValueTask.FromResult(true);
		protected override ValueTask DisposeAsync(bool disposing)
		{
			this.DisposalCount++;
			return base.DisposeAsync(disposing);
		}
	}

	private sealed class TestManager : IDistributedLockManager
	{
		private readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource<bool> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public Task Started => _started.Task;
		public int ReleaseCount { get; private set; }
		public string Key { get; private set; }
		public byte[] Token { get; private set; }
		public CancellationToken Cancellation { get; private set; }
		public IDistributedLockTokenizer Tokenizer { get; set; }
		public ValueTask<TimeSpan?> GetExpiryAsync(string key, CancellationToken cancellation = default) => ValueTask.FromResult<TimeSpan?>(null);
		public ValueTask<IDistributedLock> AcquireAsync(string key, DistributedLockOptions options, CancellationToken cancellation = default) => throw new NotSupportedException();
		public ValueTask<bool> ReleaseAsync(string key, byte[] token, CancellationToken cancellation = default)
		{
			this.ReleaseCount++;
			this.Key = key;
			this.Token = token;
			this.Cancellation = cancellation;
			_started.TrySetResult();
			return new(this.Completion.Task);
		}
	}
}
