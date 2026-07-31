using System;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.Components;
using Zongsoft.Collections;

namespace Zongsoft.Messaging.Tests;

public class MessageConsumerBaseTest
{
	[Fact]
	public async Task DisposeAsyncClosesAndMarksConsumerDisposed()
	{
		using var queue = new TestQueue();
		var consumer = new TestConsumer(queue, new TestHandler());
		var closed = 0;
		consumer.Closed += (_, _) => Interlocked.Increment(ref closed);

		Assert.False(consumer.IsClosed);
		Assert.False(consumer.IsDisposed);
		Assert.NotNull(consumer.Handler);

		await consumer.DisposeAsync();

		Assert.True(consumer.IsClosed);
		Assert.True(consumer.IsDisposed);
		Assert.Null(consumer.Handler);
		Assert.Equal(1, consumer.CloseCount);
		Assert.Equal(1, closed);

		await consumer.DisposeAsync();

		Assert.True(consumer.IsClosed);
		Assert.True(consumer.IsDisposed);
		Assert.Equal(1, consumer.CloseCount);
		Assert.Equal(1, closed);
	}

	[Fact]
	public async Task ConcurrentDisposeAsyncClosesAndDisposesOnlyOnce()
	{
		const int CONCURRENCY = 64;

		using var queue = new TestQueue();
		var consumer = new TestConsumer(queue, new TestHandler());
		var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var closed = 0;
		consumer.Closed += (_, _) => Interlocked.Increment(ref closed);

		var tasks = new Task[CONCURRENCY];
		for(var index = 0; index < tasks.Length; index++)
		{
			tasks[index] = Task.Run(async () =>
			{
				await gate.Task;
				await consumer.DisposeAsync();
			});
		}

		gate.SetResult();
		await Task.WhenAll(tasks);

		Assert.True(consumer.IsClosed);
		Assert.True(consumer.IsDisposed);
		Assert.Null(consumer.Handler);
		Assert.Equal(1, consumer.CloseCount);
		Assert.Equal(1, closed);
	}

	private sealed class TestConsumer(TestQueue queue, IHandler<Message> handler) : MessageConsumerBase<TestQueue>(queue, "tests/dispose", handler)
	{
		private int _closeCount;
		public int CloseCount => _closeCount;

		protected override ValueTask OnCloseAsync(CancellationToken cancellation)
		{
			Interlocked.Increment(ref _closeCount);
			return ValueTask.CompletedTask;
		}
	}

	private sealed class TestQueue() : MessageQueueBase<TestConsumer>("Tests")
	{
		protected override ValueTask<string> OnProduceAsync(string topic, string tags, ReadOnlyMemory<byte> data, MessageEnqueueOptions options, CancellationToken cancellation) => ValueTask.FromResult(string.Empty);
		protected override ValueTask<bool> OnSubscribeAsync(TestConsumer subscriber, CancellationToken cancellation) => ValueTask.FromResult(true);
		protected override ValueTask<TestConsumer> CreateSubscriberAsync(string topic, string tags, IHandler<Message> handler, MessageSubscribeOptions options, CancellationToken cancellation) => ValueTask.FromResult(new TestConsumer(this, handler));
	}

	private sealed class TestHandler : HandlerBase<Message>
	{
		protected override ValueTask OnHandleAsync(Message argument, Parameters parameters, CancellationToken cancellation) => ValueTask.CompletedTask;
	}
}
