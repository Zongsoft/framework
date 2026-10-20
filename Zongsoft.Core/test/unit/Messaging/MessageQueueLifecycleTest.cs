using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Xunit;

using Zongsoft.Components;

namespace Zongsoft.Messaging.Tests;

public class MessageQueueLifecycleTest
{
	[Fact]
	public async Task DisposeAsync_PendingProduce_DrainsBeforeCleanupAndSharesCompletion()
	{
		var release = Signal();
		var queue = new Queue { ProduceRelease = release };
		var producing = queue.ProduceAsync("topic", new byte[] { 1, 2 }).AsTask();
		var disposals = Enumerable.Range(0, 16).Select(_ => queue.DisposeAsync().AsTask()).ToArray();

		try
		{
			await queue.ProduceCanceled.Task.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.True(queue.ProduceCancellation.IsCancellationRequested);
			Assert.All(disposals, disposal => Assert.False(disposal.IsCompleted));
			Assert.False(queue.IsDisposed);
			Assert.Equal(0, queue.CleanupCount);
			await Assert.ThrowsAsync<ObjectDisposedException>(() => queue.ProduceAsync("later", new byte[] { 3 }).AsTask());
			await Assert.ThrowsAsync<ObjectDisposedException>(() => queue.SubscribeAsync("later", _ => { }).AsTask());
			Assert.Empty(queue.Created);
		}
		finally
		{
			release.TrySetResult();
		}

		Assert.Equal("produced", await producing.WaitAsync(TimeSpan.FromSeconds(5)));
		await Task.WhenAll(disposals).WaitAsync(TimeSpan.FromSeconds(5));
		await queue.DisposeAsync();

		Assert.True(queue.IsDisposed);
		Assert.Equal(1, queue.CleanupCount);
		Assert.True(queue.ProduceCompletedBeforeCleanup);
	}

	[Fact]
	public async Task DisposeAsync_CanceledInitialization_AwaitsFinallyAndConsumerRollback()
	{
		var initialization = Signal();
		var finishing = Signal();
		var closing = Signal();
		var queue = new Queue
		{
			InitializeRelease = initialization,
			InitializeFinallyRelease = finishing,
			ConsumerRelease = closing,
		};
		var subscription = queue.SubscribeAsync("topic", _ => { }).AsTask();
		await queue.InitializationEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
		var disposal = queue.DisposeAsync().AsTask();

		try
		{
			await queue.InitializationFinallyEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.False(disposal.IsCompleted);
			Assert.False(subscription.IsCompleted);
			Assert.Equal(0, queue.CleanupCount);
			Assert.Equal(0, Assert.Single(queue.Created).DisposeCount);
			finishing.TrySetResult();

			await queue.Created[0].DisposalEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.False(disposal.IsCompleted);
			Assert.False(subscription.IsCompleted);
			Assert.Equal(0, queue.CleanupCount);
		}
		finally
		{
			initialization.TrySetResult();
			finishing.TrySetResult();
			closing.TrySetResult();
		}

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => subscription);
		await disposal.WaitAsync(TimeSpan.FromSeconds(5));

		Assert.Empty(queue.Subscribers);
		Assert.Equal(1, queue.Created[0].DisposeCount);
		Assert.True(queue.Created[0].IsClosed);
		Assert.Equal(1, queue.CleanupCount);
	}

	[Fact]
	public async Task DisposeAsync_ConsumerAndDriverFailures_CleansEveryResourceAndSharesFailure()
	{
		var consumerFailure = new InvalidOperationException("consumer cleanup");
		var driverFailure = new ArgumentException("driver cleanup");
		var queue = new Queue { CleanupFailure = driverFailure };
		var first = await queue.SubscribeAsync("first", _ => { });
		var second = await queue.SubscribeAsync("second", _ => { });
		first.DisposeFailure = consumerFailure;

		var exception = await Assert.ThrowsAsync<AggregateException>(() => queue.DisposeAsync().AsTask());
		var repeated = await Assert.ThrowsAsync<AggregateException>(() => queue.DisposeAsync().AsTask());

		Assert.Same(exception, repeated);
		Assert.Equal(2, exception.InnerExceptions.Count);
		Assert.Contains(consumerFailure, exception.InnerExceptions);
		Assert.Contains(driverFailure, exception.InnerExceptions);
		Assert.Equal(1, first.DisposeCount);
		Assert.Equal(1, second.DisposeCount);
		Assert.True(second.IsClosed);
		Assert.Equal(1, queue.CleanupCount);
		Assert.True(queue.IsDisposed);
	}

	[Fact]
	public async Task DisposeAsync_PendingDriverCleanup_DoesNotCompleteAnyCallerEarly()
	{
		var release = Signal();
		var queue = new Queue { CleanupRelease = release };
		var first = queue.DisposeAsync().AsTask();
		await queue.CleanupEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
		var second = queue.DisposeAsync().AsTask();

		try
		{
			Assert.False(first.IsCompleted);
			Assert.False(second.IsCompleted);
			Assert.False(queue.IsDisposed);
			Assert.Equal(1, queue.CleanupCount);
		}
		finally
		{
			release.TrySetResult();
		}

		await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(5));
		Assert.True(queue.IsDisposed);
		Assert.Equal(1, queue.CleanupCount);
	}

	[Fact]
	public async Task SubscribeAsync_InitializationAndRollbackFail_PreservesBothFailures()
	{
		var initializationFailure = new InvalidOperationException("subscription initialization");
		var rollbackFailure = new ArgumentException("subscription rollback");
		var queue = new Queue
		{
			InitializeFailure = initializationFailure,
			ConsumerFailure = rollbackFailure,
		};

		var exception = await Assert.ThrowsAsync<AggregateException>(() => queue.SubscribeAsync("topic", _ => { }).AsTask());

		Assert.Equal(2, exception.InnerExceptions.Count);
		Assert.Contains(initializationFailure, exception.InnerExceptions);
		Assert.Contains(rollbackFailure, exception.InnerExceptions);
		Assert.Equal(1, Assert.Single(queue.Created).DisposeCount);
		Assert.Empty(queue.Subscribers);

		await queue.DisposeAsync();
		Assert.True(queue.IsDisposed);
		Assert.Equal(1, queue.CleanupCount);
	}

	private static TaskCompletionSource Signal() => new(TaskCreationOptions.RunContinuationsAsynchronously);

	private sealed class Queue() : MessageQueueBase<Consumer>("Lifecycle")
	{
		private bool _produceCompleted;

		public TaskCompletionSource ProduceRelease { get; init; }
		public TaskCompletionSource InitializeRelease { get; init; }
		public TaskCompletionSource InitializeFinallyRelease { get; init; }
		public TaskCompletionSource ConsumerRelease { get; init; }
		public TaskCompletionSource CleanupRelease { get; init; }
		public Exception CleanupFailure { get; init; }
		public Exception InitializeFailure { get; init; }
		public Exception ConsumerFailure { get; init; }
		public TaskCompletionSource InitializationEntered { get; } = Signal();
		public TaskCompletionSource InitializationFinallyEntered { get; } = Signal();
		public TaskCompletionSource CleanupEntered { get; } = Signal();
		public TaskCompletionSource ProduceCanceled { get; } = Signal();
		public List<Consumer> Created { get; } = [];
		public CancellationToken ProduceCancellation { get; private set; }
		public int CleanupCount { get; private set; }
		public bool ProduceCompletedBeforeCleanup { get; private set; }

		protected override async ValueTask<string> OnProduceAsync(string topic, string tags, ReadOnlyMemory<byte> data, MessageEnqueueOptions options, CancellationToken cancellation)
		{
			this.ProduceCancellation = cancellation;
			using var registration = cancellation.Register(() => this.ProduceCanceled.TrySetResult());

			if(this.ProduceRelease != null)
				await this.ProduceRelease.Task;

			_produceCompleted = true;
			return "produced";
		}

		protected override async ValueTask<bool> OnSubscribeAsync(Consumer subscriber, CancellationToken cancellation)
		{
			this.InitializationEntered.TrySetResult();

			try
			{
				if(this.InitializeRelease != null)
					await this.InitializeRelease.Task.WaitAsync(cancellation);

				if(this.InitializeFailure != null)
					throw this.InitializeFailure;

				return true;
			}
			finally
			{
				this.InitializationFinallyEntered.TrySetResult();
				if(this.InitializeFinallyRelease != null)
					await this.InitializeFinallyRelease.Task;
			}
		}

		protected override ValueTask<Consumer> CreateSubscriberAsync(string topic, string tags, IHandler<Message> handler, MessageSubscribeOptions options, CancellationToken cancellation)
		{
			var consumer = new Consumer(topic, handler, this.ConsumerRelease) { DisposeFailure = this.ConsumerFailure };
			this.Created.Add(consumer);
			return ValueTask.FromResult(consumer);
		}

		protected override async ValueTask DisposeAsync(bool disposing)
		{
			this.CleanupCount++;
			this.ProduceCompletedBeforeCleanup = _produceCompleted;
			this.CleanupEntered.TrySetResult();

			if(this.CleanupRelease != null)
				await this.CleanupRelease.Task;

			if(this.CleanupFailure != null)
				throw this.CleanupFailure;
		}
	}

	private sealed class Consumer(string topic, IHandler<Message> handler, TaskCompletionSource release) : IMessageConsumer
	{
		public event EventHandler Closing;
		public event EventHandler Closed;
		public string Topic => topic;
		public string[] Tags => [];
		public IHandler<Message> Handler => handler;
		public MessageSubscribeOptions Options => null;
		public bool IsClosed { get; private set; }
		public bool IsDisposed { get; private set; }
		public int DisposeCount { get; private set; }
		public Exception DisposeFailure { get; set; }
		public TaskCompletionSource DisposalEntered { get; } = Signal();

		public ValueTask UnsubscribeAsync(CancellationToken cancellation = default) => this.CloseAsync(cancellation);
		public ValueTask CloseAsync(CancellationToken cancellation = default)
		{
			this.Closing?.Invoke(this, EventArgs.Empty);
			this.IsClosed = true;
			this.Closed?.Invoke(this, EventArgs.Empty);
			return ValueTask.CompletedTask;
		}

		public async ValueTask DisposeAsync()
		{
			this.DisposeCount++;
			this.DisposalEntered.TrySetResult();
			if(release != null)
				await release.Task;
			if(this.DisposeFailure != null)
				throw this.DisposeFailure;
			await this.CloseAsync();
			this.IsDisposed = true;
		}
	}
}
