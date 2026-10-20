using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace Zongsoft.Messaging.Tests;

public class MessagePollerLifecycleTest
{
	[Fact]
	public async Task StopAsync_PendingReceive_WaitsForCleanupAndDoesNotDispatchLateMessage()
	{
		var release = Signal();
		var canceled = Signal();
		await using var poller = new Poller(async (_, cancellation) =>
		{
			using var registration = cancellation.Register(() => canceled.TrySetResult());
			await release.Task;
			return new Message("topic", [1]);
		});
		var options = new MessageDequeueOptions();
		await poller.StartAsync(options);
		await poller.ReceiveEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
		var stopping = poller.StopAsync().AsTask();

		try
		{
			await canceled.Task.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.False(stopping.IsCompleted);
			Assert.True(poller.IsPolling);
			Assert.Same(options, poller.ReceivedOptions);
			Assert.Equal(0, poller.HandleCount);
		}
		finally
		{
			release.TrySetResult();
		}

		await stopping.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.False(poller.IsPolling);
		Assert.Equal(1, poller.ReceiveCount);
		Assert.Equal(0, poller.HandleCount);
	}

	[Fact]
	public async Task DisposeAsync_PendingHandler_WaitsAndSharesOneCleanup()
	{
		var release = Signal();
		var entered = Signal();
		var canceled = Signal();
		var expected = new Message("identifier", "topic", [1, 2, 3]);
		var actual = Message.Empty;
		var poller = new Poller((_, _) => ValueTask.FromResult(expected), async (message, cancellation) =>
		{
			actual = message;
			using var registration = cancellation.Register(() => canceled.TrySetResult());
			entered.TrySetResult();
			await release.Task;
		});
		await poller.StartAsync();
		await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
		var disposals = Enumerable.Range(0, 16).Select(_ => poller.DisposeAsync().AsTask()).ToArray();

		try
		{
			await canceled.Task.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.All(disposals, disposal => Assert.False(disposal.IsCompleted));
			Assert.False(poller.IsDisposed);
			Assert.Equal(1, poller.ReceiveCount);
			Assert.Equal(1, poller.HandleCount);
			Assert.Equal(0, poller.CleanupCount);
			Assert.Equal(expected.Identifier, actual.Identifier);
			Assert.Equal(expected.Data, actual.Data);
		}
		finally
		{
			release.TrySetResult();
		}

		await Task.WhenAll(disposals).WaitAsync(TimeSpan.FromSeconds(5));
		await poller.DisposeAsync();

		Assert.True(poller.IsDisposed);
		Assert.False(poller.IsPolling);
		Assert.Equal(1, poller.CleanupCount);
		Assert.Equal(1, poller.ReceiveCount);
		await Assert.ThrowsAsync<ObjectDisposedException>(() => poller.StartAsync().AsTask());
	}

	[Fact]
	public async Task StopAsync_CallerCancellationAfterAccepted_DoesNotAbandonHandler()
	{
		using var cancellation = new CancellationTokenSource();
		var release = Signal();
		var entered = Signal();
		var canceled = Signal();
		await using var poller = new Poller((_, _) => ValueTask.FromResult(new Message("topic", [1])), async (_, token) =>
		{
			using var registration = token.Register(() => canceled.TrySetResult());
			entered.TrySetResult();
			await release.Task;
		});
		await poller.StartAsync();
		await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
		var stopping = poller.StopAsync(cancellation.Token).AsTask();

		try
		{
			await canceled.Task.WaitAsync(TimeSpan.FromSeconds(5));
			cancellation.Cancel();
			Assert.False(stopping.IsCompleted);
			Assert.Equal(1, poller.HandleCount);
		}
		finally
		{
			release.TrySetResult();
		}

		await stopping.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.False(poller.IsPolling);
		Assert.Equal(1, poller.ReceiveCount);
	}

	[Fact]
	public async Task StartAsync_AfterStop_StartsFreshRunAndPreCanceledStartDoesNotReceive()
	{
		using var cancellation = new CancellationTokenSource();
		var first = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
		var second = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
		var calls = 0;
		await using var poller = new Poller(async (_, token) =>
		{
			(Interlocked.Increment(ref calls) == 1 ? first : second).TrySetResult(token);
			await Task.Delay(Timeout.InfiniteTimeSpan, token);
			return Message.Empty;
		});
		cancellation.Cancel();
		var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => poller.StartAsync(cancellation.Token).AsTask());
		Assert.Equal(cancellation.Token, exception.CancellationToken);
		Assert.False(poller.IsPolling);
		Assert.Equal(0, poller.ReceiveCount);

		await poller.StartAsync();
		var firstToken = await first.Task.WaitAsync(TimeSpan.FromSeconds(5));
		await poller.StartAsync();
		Assert.Equal(1, poller.ReceiveCount);
		await poller.StopAsync();
		Assert.True(firstToken.IsCancellationRequested);
		Assert.False(poller.IsPolling);

		await poller.StartAsync();
		var secondToken = await second.Task.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.NotEqual(firstToken, secondToken);
		Assert.False(secondToken.IsCancellationRequested);
		Assert.True(poller.IsPolling);
		await poller.StopAsync();
		Assert.Equal(2, poller.ReceiveCount);
		Assert.Equal(0, poller.HandleCount);
	}

	[Fact]
	public async Task DisposeAsync_PendingCleanup_SharesCompletionAndOriginalFailure()
	{
		var release = Signal();
		var entered = Signal();
		var failure = new InvalidOperationException("poller cleanup");
		var poller = new Poller((_, _) => ValueTask.FromResult(Message.Empty), cleanup: async () =>
		{
			entered.TrySetResult();
			await release.Task;
			throw failure;
		});
		var first = poller.DisposeAsync().AsTask();
		await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
		var second = poller.DisposeAsync().AsTask();

		try
		{
			Assert.False(first.IsCompleted);
			Assert.False(second.IsCompleted);
			Assert.Equal(1, poller.CleanupCount);
			await Assert.ThrowsAsync<ObjectDisposedException>(() => poller.StartAsync().AsTask());
		}
		finally
		{
			release.TrySetResult();
		}

		Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => first));
		Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => second));
		Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => poller.DisposeAsync().AsTask()));
		Assert.Equal(1, poller.CleanupCount);
		Assert.True(poller.IsDisposed);
	}

	private static TaskCompletionSource Signal() => new(TaskCreationOptions.RunContinuationsAsynchronously);

	private sealed class Poller(
		Func<MessageDequeueOptions, CancellationToken, ValueTask<Message>> receive,
		Func<Message, CancellationToken, ValueTask> handle = null,
		Func<ValueTask> cleanup = null) : MessagePollerBase
	{
		private int _receiveCount;
		private int _handleCount;
		private int _cleanupCount;

		public TaskCompletionSource ReceiveEntered { get; } = Signal();
		public MessageDequeueOptions ReceivedOptions { get; private set; }
		public int ReceiveCount => Volatile.Read(ref _receiveCount);
		public int HandleCount => Volatile.Read(ref _handleCount);
		public int CleanupCount => Volatile.Read(ref _cleanupCount);

		protected override ValueTask<Message> ReceiveAsync(MessageDequeueOptions options, CancellationToken cancellation)
		{
			Interlocked.Increment(ref _receiveCount);
			this.ReceivedOptions = options;
			this.ReceiveEntered.TrySetResult();
			return receive(options, cancellation);
		}

		protected override ValueTask OnHandleAsync(Message message, CancellationToken cancellation)
		{
			Interlocked.Increment(ref _handleCount);
			return handle?.Invoke(message, cancellation) ?? ValueTask.CompletedTask;
		}

		protected override ValueTask DisposeAsync(bool disposing)
		{
			Interlocked.Increment(ref _cleanupCount);
			return cleanup?.Invoke() ?? ValueTask.CompletedTask;
		}
	}
}
