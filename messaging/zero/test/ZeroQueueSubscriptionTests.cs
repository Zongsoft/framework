using System;
using System.Text;
using System.Threading.Tasks;

using NetMQ;
using NetMQ.Sockets;

using Xunit;

namespace Zongsoft.Messaging.ZeroMQ.Tests;

public class ZeroQueueSubscriptionTests
{
	[Fact]
	public async Task SubscribeAndUnsubscribeCanRepeatAndReceiveAfterResubscribe()
	{
		using var server = await ZeroServerScope.StartAsync();
		using var publisher = ZeroTestUtility.CreateQueue(server.Port, "publisher");
		using var subscriber = ZeroTestUtility.CreateQueue(server.Port, "subscriber");
		var handler = new MessageCollector();

		for(int i = 0; i < 10; i++)
		{
			var consumer = await subscriber.SubscribeAsync("topic/repeat", handler);
			await consumer.UnsubscribeAsync();
		}

		await subscriber.SubscribeAsync("topic/repeat", handler);
		await Task.Delay(750);

		await PublishRepeatedlyAsync(publisher, "topic/repeat", "ready");
		var message = await handler.ReceiveAsync(TimeSpan.FromSeconds(5));

		Assert.False(message.IsEmpty);
		Assert.Equal("ready", Encoding.UTF8.GetString(message.Data));
	}

	[Fact]
	public async Task DisposingOneQueueDoesNotBreakOtherQueues()
	{
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
	public async Task SubscribersReconnectAfterQueueServerRestartsWithNewExchangePorts()
	{
		var port = ZeroTestUtility.GetFreePort();
		var topic = "topic/restart";
		var server = new ZeroQueueServer { Port = port };

		try
		{
			await server.StartAsync([]);
			var original = ZeroTestUtility.GetServerPorts(port);

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

			using var publisherBlocker = new ResponseSocket();
			using var subscriberBlocker = new ResponseSocket();
			publisherBlocker.Bind($"tcp://*:{original.Publisher}");
			subscriberBlocker.Bind($"tcp://*:{original.Subscriber}");

			await server.StartAsync([]);
			var restarted = ZeroTestUtility.GetServerPorts(port);

			Assert.NotEqual(original, restarted);

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
}
