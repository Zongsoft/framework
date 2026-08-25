using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

using Xunit;

using Zongsoft.Components;
using Zongsoft.Collections;

namespace Zongsoft.Messaging.Tests;

public class MessageQueueBaseTest
{
	[Fact]
	public async Task ConcurrentSubscribersShareOneInitializationAndExposeOnlyActiveConsumer()
	{
		using var queue = new TestQueue();
		queue.BlockInitialization();

		var subscriptions = new Task<TestConsumer>[64];
		for(var index = 0; index < subscriptions.Length; index++)
			subscriptions[index] = queue.SubscribeAsync("tests/shared", new TestHandler()).AsTask();

		await queue.InitializationStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.Equal(1, queue.CreateCount);
		Assert.Equal(1, queue.SubscribeCount);
		Assert.Empty(queue.Subscribers);

		queue.ReleaseInitialization();
		var consumers = await Task.WhenAll(subscriptions);
		var repeated = await queue.SubscribeAsync("tests/shared", new TestHandler());

		Assert.All(consumers, consumer => Assert.Same(consumers[0], consumer));
		Assert.Same(consumers[0], repeated);
		Assert.Single(queue.Subscribers);
		Assert.Equal(1, queue.CreateCount);
		Assert.Equal(1, queue.SubscribeCount);
	}

	[Fact]
	public async Task FailedInitializationRollsBackAndCanRetry()
	{
		using var queue = new TestQueue();
		queue.EnqueueResult(false);
		queue.EnqueueResult(true);

		var failed = await queue.SubscribeAsync("tests/retry", new TestHandler());

		Assert.Null(failed);
		Assert.Empty(queue.Subscribers);
		Assert.Equal(1, queue.DisposedCount);

		var subscriber = await queue.SubscribeAsync("tests/retry", new TestHandler());

		Assert.NotNull(subscriber);
		Assert.Single(queue.Subscribers);
		Assert.Equal(2, queue.CreateCount);
		Assert.Equal(2, queue.SubscribeCount);
	}

	[Fact]
	public async Task ExceptionalInitializationRollsBackAndCanRetry()
	{
		using var queue = new TestQueue();
		queue.EnqueueResult(new InvalidOperationException("Invalid subscription."));
		queue.EnqueueResult(true);

		var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => queue.SubscribeAsync("tests/exception", new TestHandler()).AsTask());
		Assert.Equal("Invalid subscription.", exception.Message);
		Assert.Empty(queue.Subscribers);
		Assert.Equal(1, queue.DisposedCount);

		Assert.NotNull(await queue.SubscribeAsync("tests/exception", new TestHandler()));
		Assert.Single(queue.Subscribers);
	}

	[Fact]
	public async Task CallerCancellationDoesNotCancelSharedInitialization()
	{
		using var queue = new TestQueue();
		using var cancellation = new CancellationTokenSource();
		queue.BlockInitialization();

		var cancelled = queue.SubscribeAsync("tests/cancel", new TestHandler(), cancellation.Token).AsTask();
		var survivor = queue.SubscribeAsync("tests/cancel", new TestHandler()).AsTask();
		await queue.InitializationStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

		cancellation.Cancel();
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelled);
		Assert.False(survivor.IsCompleted);

		queue.ReleaseInitialization();
		Assert.NotNull(await survivor);
		Assert.Single(queue.Subscribers);
		Assert.Equal(1, queue.CreateCount);
		Assert.Equal(1, queue.SubscribeCount);
	}

	[Fact]
	public async Task QueueDisposalDuringInitializationRollsBackConsumer()
	{
		var queue = new TestQueue();
		queue.BlockInitialization();

		var subscription = queue.SubscribeAsync("tests/disposal", new TestHandler()).AsTask();
		await queue.InitializationStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

		queue.Dispose();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => subscription);
		Assert.Empty(queue.Subscribers);
		Assert.Equal(1, queue.DisposedCount);
		Assert.Equal(0, queue.UnsubscribedCount);
	}

	[Fact]
	public async Task FailedInitializationIsRemovedAfterAllWaitersCancel()
	{
		using var queue = new TestQueue();
		using var cancellation = new CancellationTokenSource();
		queue.BlockInitialization();
		queue.EnqueueResult(new InvalidOperationException("Delayed failure."));

		var abandoned = queue.SubscribeAsync("tests/abandoned", new TestHandler(), cancellation.Token).AsTask();
		await queue.InitializationStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
		cancellation.Cancel();
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => abandoned);

		queue.ReleaseInitialization();
		await WaitUntilAsync(() => queue.DisposedCount == 1, TimeSpan.FromSeconds(5));
		queue.EnqueueResult(true);

		Assert.NotNull(await queue.SubscribeAsync("tests/abandoned", new TestHandler()));
		Assert.Equal(2, queue.CreateCount);
		Assert.Equal(2, queue.SubscribeCount);

		static async Task WaitUntilAsync(Func<bool> predicate, TimeSpan timeout)
		{
			var deadline = DateTime.UtcNow + timeout;
			while(!predicate() && DateTime.UtcNow < deadline)
				await Task.Delay(10);

			Assert.True(predicate());
		}
	}

	[Fact]
	public async Task ConsumerClosedDuringInitializationIsNotCached()
	{
		using var queue = new TestQueue();
		queue.CloseDuringInitialization = true;

		var subscriber = await queue.SubscribeAsync("tests/closed", new TestHandler());

		Assert.Null(subscriber);
		Assert.Empty(queue.Subscribers);
		Assert.Equal(1, queue.DisposedCount);
		Assert.Equal(0, queue.UnsubscribedCount);
	}

	[Fact]
	public async Task ActiveConsumerCloseRemovesEntryAndAllowsResubscribe()
	{
		using var queue = new TestQueue();
		var first = await queue.SubscribeAsync("tests/resubscribe", new TestHandler());

		Assert.NotNull(first);
		Assert.Single(queue.Subscribers);

		await first.DisposeAsync();

		Assert.Empty(queue.Subscribers);
		Assert.Equal(1, queue.UnsubscribedCount);

		var second = await queue.SubscribeAsync("tests/resubscribe", new TestHandler());

		Assert.NotNull(second);
		Assert.NotSame(first, second);
		Assert.Single(queue.Subscribers);
		Assert.Equal(2, queue.CreateCount);
		Assert.Equal(2, queue.SubscribeCount);
	}

	private sealed class TestQueue() : MessageQueueBase<TestConsumer>("Tests")
	{
		private int _created;
		private int _subscribed;
		private int _disposed;
		private int _unsubscribed;
		private TaskCompletionSource _initialization;
		private readonly ConcurrentQueue<object> _results = new();

		public int CreateCount => _created;
		public int SubscribeCount => _subscribed;
		public int DisposedCount => _disposed;
		public int UnsubscribedCount => _unsubscribed;
		public bool CloseDuringInitialization { get; set; }
		public TaskCompletionSource InitializationStarted { get; private set; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public void BlockInitialization()
		{
			_initialization = new(TaskCreationOptions.RunContinuationsAsynchronously);
			InitializationStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
		}

		public void ReleaseInitialization() => _initialization?.TrySetResult();
		public void EnqueueResult(bool result) => _results.Enqueue(result);
		public void EnqueueResult(Exception exception) => _results.Enqueue(exception);
		public void OnDisposed() => Interlocked.Increment(ref _disposed);

		protected override ValueTask<string> OnProduceAsync(string topic, string tags, ReadOnlyMemory<byte> data, MessageEnqueueOptions options, CancellationToken cancellation) => ValueTask.FromResult(string.Empty);

		protected override async ValueTask<bool> OnSubscribeAsync(TestConsumer subscriber, CancellationToken cancellation)
		{
			Interlocked.Increment(ref _subscribed);
			InitializationStarted.TrySetResult();

			if(_initialization != null)
				await _initialization.Task.WaitAsync(cancellation);

			if(this.CloseDuringInitialization)
				await subscriber.DisposeAsync();

			if(!_results.TryDequeue(out var result))
				return true;

			if(result is Exception exception)
				throw exception;

			return (bool)result;
		}

		protected override ValueTask<TestConsumer> CreateSubscriberAsync(string topic, string tags, IHandler<Message> handler, MessageSubscribeOptions options, CancellationToken cancellation)
		{
			Interlocked.Increment(ref _created);
			return ValueTask.FromResult(new TestConsumer(this, topic, handler));
		}

		protected override void OnUnsubscribed(TestConsumer subscriber) => Interlocked.Increment(ref _unsubscribed);
	}

	private sealed class TestConsumer : MessageConsumerBase<TestQueue>
	{
		private readonly TestQueue _queue;

		public TestConsumer(TestQueue queue, string topic, IHandler<Message> handler) : base(queue, topic, handler) => _queue = queue;

		protected override ValueTask OnCloseAsync(CancellationToken cancellation)
		{
			_queue.OnDisposed();
			return ValueTask.CompletedTask;
		}
	}

	private sealed class TestHandler : HandlerBase<Message>
	{
		protected override ValueTask OnHandleAsync(Message argument, Parameters parameters, CancellationToken cancellation) => ValueTask.CompletedTask;
	}
}
