using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

using Xunit;

using Zongsoft.Components;
using Zongsoft.Collections;
using Zongsoft.Common;

namespace Zongsoft.Messaging.Tests;

public class MessageQueueBaseTest
{
	[Fact]
	public void MessageEnqueueOptionsSupportsCompressionSetting()
	{
		var options = new MessageEnqueueOptions();

		Assert.True(options.Compression.IsEmpty);

		options.Compression = new MessageCompression("Brotli", 4096);

		Assert.Equal(new MessageCompression("brotli", 4096), options.Compression);
		Assert.Equal(4096, options.Compression.Value);
	}

	[Fact]
	public void MessageQueueFeaturesAreNamedValuesAndCaseInsensitiveKeys()
	{
		Assert.Throws<ArgumentNullException>(() => new MessageQueueFeature(null));
		Assert.Throws<ArgumentNullException>(() => new MessageQueueFeature("  "));

		var delay = new MessageQueueFeature(" Delay ");
		var feature = new MessageQueueFeature("DELAY");
		var features = new MessageQueueFeatureCollection() { delay };

		Assert.Equal("delay", delay.Name);
		Assert.Equal(delay, feature);
		Assert.Equal(delay.GetHashCode(), feature.GetHashCode());
		Assert.True(features.Contains("delay"));
		Assert.True(features.Contains("DELAY"));
		Assert.Same(delay, features["Delay"]);
	}

	[Fact]
	public void MessageQueueInterfaceExposesBaseFeatureCollection()
	{
		using var queue = new TestQueue();
		IMessageQueue contract = queue;

		Assert.Same(queue.Features, contract.Features);
		Assert.Empty(contract.Features);
	}

	[Fact]
	public async Task UnsupportedDelayFailsBeforeDriverOperation()
	{
		using var queue = new TestQueue();
		var options = new MessageEnqueueOptions(TimeSpan.FromSeconds(1));

		var exception = await Assert.ThrowsAsync<OperationException>(() => queue.ProduceAsync("tests/delay", ReadOnlyMemory<byte>.Empty, options).AsTask());

		Assert.Equal(nameof(OperationException.Unsupported), exception.Reason);
		Assert.Contains(MessageQueueFeature.Delay.Name, exception.Message);
		Assert.Equal(0, queue.ProduceCount);
	}

	[Fact]
	public async Task SupportedDelayReachesDriverOperation()
	{
		using var queue = new TestQueue();
		queue.Features.Add(MessageQueueFeature.Delay);

		var options = new MessageEnqueueOptions(TimeSpan.FromSeconds(1));
		await queue.ProduceAsync("tests/delay", ReadOnlyMemory<byte>.Empty, options);

		Assert.Equal(1, queue.ProduceCount);
		Assert.Same(options, queue.ProducedOptions);
	}

	[Fact]
	public async Task UnsupportedCompressionFailsBeforeDriverOperation()
	{
		using var queue = new TestQueue();
		var options = new MessageEnqueueOptions() { Compression = new("Brotli", 4096) };

		var exception = await Assert.ThrowsAsync<OperationException>(() => queue.ProduceAsync("tests/compression", new byte[4096], options).AsTask());

		Assert.Equal(nameof(OperationException.Unsupported), exception.Reason);
		Assert.Contains(MessageQueueFeature.Compression.Name, exception.Message);
		Assert.Equal(0, queue.ProduceCount);
	}

	[Fact]
	public async Task SupportedCompressionReachesDriverOperation()
	{
		using var queue = new TestQueue();
		queue.Features.Add(MessageQueueFeature.Compression);

		var options = new MessageEnqueueOptions() { Compression = new("GZip", 1024) };
		await queue.ProduceAsync("tests/compression", new byte[1024], options);

		Assert.Equal(1, queue.ProduceCount);
		Assert.Same(options, queue.ProducedOptions);
	}

	[Fact]
	public async Task ConcurrentSubscribersShareOneInitializationAndExposeOnlyActiveConsumer()
	{
		using var queue = new TestQueue();
		var handler = new TestHandler();
		queue.BlockInitialization();

		var subscriptions = new Task<TestConsumer>[64];
		for(var index = 0; index < subscriptions.Length; index++)
			subscriptions[index] = queue.SubscribeAsync("tests/shared", handler).AsTask();

		await queue.InitializationStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.Equal(1, queue.CreateCount);
		Assert.Equal(1, queue.SubscribeCount);
		Assert.Empty(queue.Subscribers);

		queue.ReleaseInitialization();
		var consumers = await Task.WhenAll(subscriptions);
		var repeated = await queue.SubscribeAsync("tests/shared", handler);

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
		var handler = new TestHandler();
		queue.BlockInitialization();

		var cancelled = queue.SubscribeAsync("tests/cancel", handler, cancellation.Token).AsTask();
		var survivor = queue.SubscribeAsync("tests/cancel", handler).AsTask();
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

	[Fact]
	public async Task ConflictingSubscriptionDoesNotReplaceExistingConsumer()
	{
		using var queue = new TestQueue();
		var handler = new TestHandler();
		var first = await queue.SubscribeAsync("tests/conflict", "alpha,beta", handler, new MessageSubscribeOptions(MessageReliability.MostOnce));

		await Assert.ThrowsAsync<InvalidOperationException>(() => queue.SubscribeAsync("tests/conflict", "alpha,beta", new TestHandler(), new MessageSubscribeOptions(MessageReliability.MostOnce)).AsTask());
		await Assert.ThrowsAsync<InvalidOperationException>(() => queue.SubscribeAsync("tests/conflict", "alpha", handler, new MessageSubscribeOptions(MessageReliability.MostOnce)).AsTask());
		await Assert.ThrowsAsync<InvalidOperationException>(() => queue.SubscribeAsync("tests/conflict", "alpha,beta", handler, new MessageSubscribeOptions(MessageReliability.LeastOnce)).AsTask());

		Assert.Same(first, queue.Subscribers["tests/conflict"]);
		Assert.Equal(1, queue.CreateCount);
		Assert.Equal(1, queue.SubscribeCount);
	}

	[Fact]
	public async Task UnsupportedReliabilityFailsBeforeDriverOperation()
	{
		using var queue = new TestQueue { MaximumReliability = MessageReliability.LeastOnce };
		var options = new MessageEnqueueOptions(MessageReliability.ExactlyOnce);

		await Assert.ThrowsAsync<NotSupportedException>(() => queue.ProduceAsync("tests/reliability", new byte[] { 1, 2, 3 }, options).AsTask());
		await Assert.ThrowsAsync<NotSupportedException>(() => queue.ProduceAsync("tests/reliability", "payload".AsMemory(), options).AsTask());
		await Assert.ThrowsAsync<NotSupportedException>(() => queue.SubscribeAsync("tests/reliability", new TestHandler(), new MessageSubscribeOptions(MessageReliability.ExactlyOnce)).AsTask());

		Assert.Equal(0, queue.ProduceCount);
		Assert.Equal(0, queue.CreateCount);
		Assert.Empty(queue.Subscribers);
	}

	[Fact]
	public async Task AllProducerOverloadsUseTheSameReliabilityUpperBound()
	{
		using var queue = new TestQueue { MaximumReliability = MessageReliability.LeastOnce };
		var options = new MessageEnqueueOptions(MessageReliability.ExactlyOnce);
		var bytes = new byte[] { 1, 2, 3 }.AsMemory();
		var text = "payload".AsMemory();

		await Assert.ThrowsAsync<NotSupportedException>(() => queue.ProduceAsync(bytes, options).AsTask());
		await Assert.ThrowsAsync<NotSupportedException>(() => queue.ProduceAsync(text, options).AsTask());
		await Assert.ThrowsAsync<NotSupportedException>(() => queue.ProduceAsync(text, Encoding.Unicode, options).AsTask());
		await Assert.ThrowsAsync<NotSupportedException>(() => queue.ProduceAsync("tests/reliability", bytes, options).AsTask());
		await Assert.ThrowsAsync<NotSupportedException>(() => queue.ProduceAsync("tests/reliability", "tag", bytes, options).AsTask());
		await Assert.ThrowsAsync<NotSupportedException>(() => queue.ProduceAsync("tests/reliability", text, options).AsTask());
		await Assert.ThrowsAsync<NotSupportedException>(() => queue.ProduceAsync("tests/reliability", text, Encoding.Unicode, options).AsTask());
		await Assert.ThrowsAsync<NotSupportedException>(() => queue.ProduceAsync("tests/reliability", "tag", text, options).AsTask());
		await Assert.ThrowsAsync<NotSupportedException>(() => queue.ProduceAsync("tests/reliability", "tag", text, Encoding.Unicode, options).AsTask());

		Assert.Equal(0, queue.ProduceCount);
	}

	[Fact]
	public async Task AllSubscriberOverloadsUseTheSameReliabilityUpperBound()
	{
		using var queue = new TestQueue { MaximumReliability = MessageReliability.LeastOnce };
		IMessageQueue contract = queue;
		var options = new MessageSubscribeOptions(MessageReliability.ExactlyOnce);
		var handler = new TestHandler();
		Action<Message> action = _ => { };

		await Assert.ThrowsAsync<NotSupportedException>(() => contract.SubscribeAsync(action, options).AsTask());
		await Assert.ThrowsAsync<NotSupportedException>(() => contract.SubscribeAsync(handler, options).AsTask());
		await Assert.ThrowsAsync<NotSupportedException>(() => contract.SubscribeAsync("tests/action", action, options).AsTask());
		await Assert.ThrowsAsync<NotSupportedException>(() => contract.SubscribeAsync("tests/handler", handler, options).AsTask());
		await Assert.ThrowsAsync<NotSupportedException>(() => contract.SubscribeAsync("tests/tagged-action", "tag", action, options).AsTask());
		await Assert.ThrowsAsync<NotSupportedException>(() => contract.SubscribeAsync("tests/tagged-handler", "tag", handler, options).AsTask());

		Assert.Equal(0, queue.CreateCount);
		Assert.Empty(queue.Subscribers);
	}

	[Fact]
	public async Task ReliabilityUpperBoundAllowsLowerAndEqualValues()
	{
		using var queue = new TestQueue { MaximumReliability = MessageReliability.LeastOnce };

		await queue.ProduceAsync("tests/most", ReadOnlyMemory<byte>.Empty, new MessageEnqueueOptions(MessageReliability.MostOnce));
		await queue.ProduceAsync("tests/least", ReadOnlyMemory<byte>.Empty, new MessageEnqueueOptions(MessageReliability.LeastOnce));
		Assert.NotNull(await queue.SubscribeAsync("tests/most", new TestHandler(), new MessageSubscribeOptions(MessageReliability.MostOnce)));
		Assert.NotNull(await queue.SubscribeAsync("tests/least", new TestHandler(), new MessageSubscribeOptions(MessageReliability.LeastOnce)));

		Assert.Equal(2, queue.ProduceCount);
		Assert.Equal(2, queue.CreateCount);
	}

	private sealed class TestQueue() : MessageQueueBase<TestConsumer>("Tests")
	{
		private int _created;
		private int _subscribed;
		private int _disposed;
		private int _unsubscribed;
		private int _produced;
		private TaskCompletionSource _initialization;
		private readonly ConcurrentQueue<object> _results = new();

		public int CreateCount => _created;
		public int SubscribeCount => _subscribed;
		public int DisposedCount => _disposed;
		public int UnsubscribedCount => _unsubscribed;
		public int ProduceCount => _produced;
		public MessageEnqueueOptions ProducedOptions { get; private set; }
		public MessageReliability MaximumReliability { get; set; } = MessageReliability.ExactlyOnce;
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

		protected override ValueTask<string> OnProduceAsync(string topic, string tags, ReadOnlyMemory<byte> data, MessageEnqueueOptions options, CancellationToken cancellation)
		{
			Interlocked.Increment(ref _produced);
			this.ProducedOptions = options;
			return ValueTask.FromResult(string.Empty);
		}

		protected override MessageReliability Reliability => this.MaximumReliability;

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
			return ValueTask.FromResult(new TestConsumer(this, topic, tags, handler, options));
		}

		protected override void OnUnsubscribed(TestConsumer subscriber) => Interlocked.Increment(ref _unsubscribed);
	}

	private sealed class TestConsumer : MessageConsumerBase<TestQueue>
	{
		private readonly TestQueue _queue;

		public TestConsumer(TestQueue queue, string topic, string tags, IHandler<Message> handler, MessageSubscribeOptions options) : base(queue, topic, tags, handler, options) => _queue = queue;

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
