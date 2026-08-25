using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

using NetMQ;
using NetMQ.Sockets;

using Xunit;

using Zongsoft.Collections;
using Zongsoft.Components;

namespace Zongsoft.Messaging.ZeroMQ.Tests;

public class ZeroQueueSubscriptionTests
{
	[Fact]
	public async Task SubscriberPreservesOrderThroughBoundedBackpressure()
	{
		if(!Global.IsTestingEnabled)
			return;

		using var server = await ZeroServerScope.StartAsync();
		using var queue = ZeroTestUtility.CreateQueue(server.Port, "backpressure-subscriber");
		var handler = new BlockingHandler();
		var subscriber = await queue.SubscribeAsync("topic/backpressure", handler);

		Assert.True(subscriber.Dispatch(CreateMessage(0)));
		await handler.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));

		for(var index = 1; index <= 1000; index++)
			Assert.True(subscriber.Dispatch(CreateMessage(index)));

		Assert.False(subscriber.Dispatch(CreateMessage(1001)));
		handler.Release();
		await handler.Completed.Task.WaitAsync(TimeSpan.FromSeconds(10));

		Assert.Equal(System.Linq.Enumerable.Range(0, 1002), handler.Identifiers);
		await subscriber.DisposeAsync();

		static Message CreateMessage(int identifier) => new("topic/backpressure", BitConverter.GetBytes(identifier));
	}

	[Fact]
	public async Task DisposingQueueClosesActiveSubscriber()
	{
		if(!Global.IsTestingEnabled)
			return;

		using var server = await ZeroServerScope.StartAsync();
		var queue = ZeroTestUtility.CreateQueue(server.Port, "dispose-active");
		var consumer = await queue.SubscribeAsync("topic/dispose", new MessageCollector());
		var channel = consumer.Channel;

		queue.Dispose();

		Assert.True(queue.IsDisposed);
		Assert.Empty(queue.Subscribers);
		Assert.True(consumer.IsClosed);
		Assert.True(consumer.IsDisposed);
		Assert.Null(consumer.Handler);
		Assert.Null(consumer.Channel);
		Assert.True(channel.IsDisposed);
	}

	[Fact]
	public async Task EmptyBusinessPayloadIsDelivered()
	{
		if(!Global.IsTestingEnabled)
			return;

		using var server = await ZeroServerScope.StartAsync();
		using var publisher = ZeroTestUtility.CreateQueue(server.Port, "empty-publisher");
		using var subscriber = ZeroTestUtility.CreateQueue(server.Port, "empty-subscriber");
		using var handler = new MessageBuffer();
		var topic = "topic/empty";
		await subscriber.SubscribeAsync(topic, handler);

		Message? message = null;
		var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);
		do
		{
			await publisher.ProduceAsync(topic, ReadOnlyMemory<byte>.Empty);
			message = await handler.TryReceiveAsync(TimeSpan.FromMilliseconds(250));
		}
		while(!message.HasValue && DateTime.UtcNow < deadline);

		Assert.True(message.HasValue);
		Assert.Equal(topic, message.Value.Topic);
		Assert.Empty(message.Value.Data);
	}

	[Fact]
	public async Task GroupedMessagesExposeLogicalTopic()
	{
		if(!Global.IsTestingEnabled)
			return;

		using var server = await ZeroServerScope.StartAsync();
		using var publisher = ZeroTestUtility.CreateQueue(server.Port, "group-publisher", settings => settings.Group = "tenant");
		using var subscriber = ZeroTestUtility.CreateQueue(server.Port, "group-subscriber", settings => settings.Group = "tenant");
		using var handler = new MessageBuffer();
		var topic = "topic/grouped";
		await subscriber.SubscribeAsync(topic, handler);

		var message = await PublishUntilReceivedAsync(publisher, handler, topic, "grouped", TimeSpan.FromSeconds(10));
		Assert.Equal(topic, message.Topic);
	}

	[Fact]
	public async Task SubscribeAndDisposeCanRepeatAndReceiveAfterResubscribe()
	{
		if(!Global.IsTestingEnabled)
			return;

		using var server = await ZeroServerScope.StartAsync();
		using var publisher = ZeroTestUtility.CreateQueue(server.Port, "publisher");
		using var subscriber = ZeroTestUtility.CreateQueue(server.Port, "subscriber");
		var handler = new MessageCollector();

		for(int i = 0; i < 10; i++)
		{
			var consumer = await subscriber.SubscribeAsync("topic/repeat", handler);
			var channel = consumer.Channel;
			Assert.NotNull(channel);
			Assert.False(channel.IsDisposed);

			await consumer.DisposeAsync();

			Assert.True(consumer.IsClosed);
			Assert.True(consumer.IsDisposed);
			Assert.Null(consumer.Handler);
			Assert.Null(consumer.Channel);
			Assert.True(await ZeroTestUtility.WaitUntilAsync(() => channel.IsDisposed, TimeSpan.FromSeconds(5)));
			Assert.Empty(subscriber.Subscribers);
		}

		var finalConsumer = await subscriber.SubscribeAsync("topic/repeat", handler);
		var finalChannel = finalConsumer.Channel;
		Assert.NotNull(finalChannel);
		await Task.Delay(750);

		await PublishRepeatedlyAsync(publisher, "topic/repeat", "ready");
		var message = await handler.ReceiveAsync(TimeSpan.FromSeconds(5));

		Assert.False(message.IsEmpty);
		Assert.Equal("ready", Encoding.UTF8.GetString(message.Data));

		await finalConsumer.DisposeAsync();
		Assert.True(finalConsumer.IsClosed);
		Assert.True(finalConsumer.IsDisposed);
		Assert.Null(finalConsumer.Handler);
		Assert.Null(finalConsumer.Channel);
		Assert.True(await ZeroTestUtility.WaitUntilAsync(() => finalChannel.IsDisposed, TimeSpan.FromSeconds(5)));
		Assert.Empty(subscriber.Subscribers);

		subscriber.Dispose();
		Assert.True(subscriber.IsDisposed);
		Assert.Empty(subscriber.Subscribers);
	}

	[Fact]
	public async Task DisposingOneQueueDoesNotBreakOtherQueues()
	{
		if(!Global.IsTestingEnabled)
			return;

		using var server = await ZeroServerScope.StartAsync();
		using var publisher = ZeroTestUtility.CreateQueue(server.Port, "publisher");
		using var subscriber = ZeroTestUtility.CreateQueue(server.Port, "subscriber");
		using(var disposable = ZeroTestUtility.CreateQueue(server.Port, "disposable"))
		{
			await disposable.ProduceAsync("topic/warmup", Encoding.UTF8.GetBytes("warmup"));
		}

		var handler = new MessageCollector();
		await subscriber.SubscribeAsync("topic/live", handler);
		await Task.Delay(750);

		await PublishRepeatedlyAsync(publisher, "topic/live", "live");
		var message = await handler.ReceiveAsync(TimeSpan.FromSeconds(5));

		Assert.False(message.IsEmpty);
		Assert.Equal("live", Encoding.UTF8.GetString(message.Data));
	}

	[Fact]
	public async Task SubscribersReconnectAfterQueueServerRestartsWithFixedExchangePorts()
	{
		if(!Global.IsTestingEnabled)
			return;

		var port = ZeroTestUtility.GetFreePort();
		var incoming = ZeroTestUtility.GetFreePort();
		var outgoing = ZeroTestUtility.GetFreePort();
		var args = new[] { $"--incoming:{incoming}", $"--outgoing:{outgoing}" };
		var topic = "topic/restart";
		var server = new ZeroQueueServer { Port = port };

		try
		{
			await server.StartAsync(args);
			var original = ZeroTestUtility.GetServerPorts(port);

			Assert.Equal(outgoing, original.Publisher);
			Assert.Equal(incoming, original.Subscriber);

			using var publisher = CreateRestartQueue(port, "publisher");
			using var subscriber = CreateRestartQueue(port, "subscriber");
			using var handler = new MessageBuffer();

			await subscriber.SubscribeAsync(topic, handler);
			await Task.Delay(750);

			await PublishRepeatedlyAsync(publisher, topic, "before");
			var before = await handler.ReceiveAsync(TimeSpan.FromSeconds(5));

			Assert.Equal("before", Encoding.UTF8.GetString(before.Data));

			await server.StopAsync([]);
			Assert.True(await ZeroTestUtility.WaitUntilAsync(() => !ZeroTestUtility.CanQueryServer(port), TimeSpan.FromSeconds(5)));

			await server.StartAsync(args);
			var restarted = ZeroTestUtility.GetServerPorts(port);

			Assert.Equal(original, restarted);

			var after = await PublishUntilReceivedAsync(publisher, handler, topic, "after-restart", TimeSpan.FromSeconds(10));
			Assert.Equal("after-restart", Encoding.UTF8.GetString(after.Data));
		}
		finally
		{
			await server.StopAsync([]);
			((IDisposable)server).Dispose();
		}
	}

	[Fact]
	public async Task SubscriberDrainsMalformedMultipartBeforeNextMessage()
	{
		if(!Global.IsTestingEnabled)
			return;

		using var server = await ZeroServerScope.StartAsync();
		using var subscriber = ZeroTestUtility.CreateQueue(server.Port, "subscriber");
		using var handler = new MessageBuffer();
		var topic = "topic/malformed";

		await subscriber.SubscribeAsync(topic, handler);
		await Task.Delay(750);

		var ports = ZeroTestUtility.GetServerPorts(server.Port);
		using var publisher = new PublisherSocket();
		publisher.Connect($"tcp://127.0.0.1:{ports.Subscriber}");
		await Task.Delay(1000);

		//先发送一个合法头帧但带额外尾帧的畸形 multipart，验证 subscriber 不会把尾帧当作新消息。
		publisher
			.SendMoreFrame($"{topic}@{subscriber.Instance}")
			.SendMoreFrame("ignored")
			.SendMoreFrame("fake-topic")
			.SendFrame(Encoding.UTF8.GetBytes("fake-data"));

		var unexpected = await handler.TryReceiveAsync(TimeSpan.FromMilliseconds(500));
		Assert.Null(unexpected);

		publisher.SendMoreFrame($"{topic}@external\nBroken").SendFrame("invalid-option");
		publisher.SendMoreFrame($"{topic}@external\nCompressor:Unknown").SendFrame("unknown-compressor");
		publisher.SendMoreFrame($"{topic}@external\nCompressor:Brotli").SendFrame([0xFF, 0xFF, 0xFF, 0xFF]);
		Assert.Null(await handler.TryReceiveAsync(TimeSpan.FromMilliseconds(500)));

		//再发送合法外部消息，验证前一条畸形消息不会破坏后续消息边界。
		for(int i = 0; i < 3; i++)
		{
			publisher
				.SendMoreFrame($"{topic}@external")
				.SendFrame(Encoding.UTF8.GetBytes("valid"));

			await Task.Delay(100);
		}

		var message = await handler.ReceiveAsync(TimeSpan.FromSeconds(5));
		Assert.Equal(topic, message.Topic);
		Assert.Equal("valid", Encoding.UTF8.GetString(message.Data));
	}

	private static async Task PublishRepeatedlyAsync(ZeroQueue publisher, string topic, string text)
	{
		// NetMQ PUB/SUB 在订阅刚建立时仍可能处于 slow-joiner 窗口，测试通过短重试避免把时序抖动误判为功能失败。
		for(int i = 0; i < 3; i++)
		{
			await publisher.ProduceAsync(topic, Encoding.UTF8.GetBytes(text));
			await Task.Delay(100);
		}
	}

	private static ZeroQueue CreateRestartQueue(ushort serverPort, string client) =>
		ZeroTestUtility.CreateQueue(serverPort, client, settings =>
		{
			settings.Timeout = TimeSpan.FromMilliseconds(500);
			settings.Heartbeat = TimeSpan.FromMilliseconds(200);
		});

	private static async Task<Message> PublishUntilReceivedAsync(ZeroQueue publisher, MessageBuffer handler, string topic, string text, TimeSpan timeout)
	{
		var deadline = DateTime.UtcNow + timeout;

		do
		{
			await publisher.ProduceAsync(topic, Encoding.UTF8.GetBytes(text));

			var message = await handler.TryReceiveAsync(TimeSpan.FromMilliseconds(250));
			if(message.HasValue && !message.Value.IsEmpty && Encoding.UTF8.GetString(message.Value.Data) == text)
				return message.Value;
		}
		while(DateTime.UtcNow < deadline);

		throw new TimeoutException($"Timed out waiting for message '{text}' on topic '{topic}'.");
	}

	private sealed class BlockingHandler : HandlerBase<Message>
	{
		private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
		private readonly ConcurrentQueue<int> _identifiers = new();
		private int _count;

		public int[] Identifiers => _identifiers.ToArray();
		public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource Completed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public void Release() => _release.TrySetResult();

		protected override async ValueTask OnHandleAsync(Message message, Parameters parameters, CancellationToken cancellation)
		{
			_identifiers.Enqueue(BitConverter.ToInt32(message.Data));
			if(Interlocked.Increment(ref _count) == 1)
			{
				Started.TrySetResult();
				await _release.Task.WaitAsync(cancellation);
			}

			if(Volatile.Read(ref _count) == 1002)
				Completed.TrySetResult();
		}
	}
}
