using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;

namespace Zongsoft.Messaging.ZeroMQ.Tests;

public class ZeroQueuePublishingTests
{
	[Fact]
	public async Task ProduceSnapshotsPayloadBeforeLocalSendCompletes()
	{
		if(!Global.IsTestingEnabled)
			return;

		using var server = await ZeroServerScope.StartAsync();
		using var publisher = ZeroTestUtility.CreateQueue(server.Port, "snapshot-publisher");
		using var subscriber = ZeroTestUtility.CreateQueue(server.Port, "snapshot-subscriber");
		using var handler = new MessageBuffer();
		var topic = "topic/snapshot";

		await subscriber.SubscribeAsync(topic, handler);
		await PublishUntilReceivedAsync(publisher, handler, topic, "warmup");

		var payload = Encoding.UTF8.GetBytes("original");
		await publisher.ProduceAsync(topic, payload);
		Array.Fill(payload, (byte)'x');

		var message = await handler.ReceiveAsync(TimeSpan.FromSeconds(5));
		Assert.Equal("original", Encoding.UTF8.GetString(message.Data));
	}

	[Fact]
	public async Task FailedInitializationCanRetryAfterServerStarts()
	{
		if(!Global.IsTestingEnabled)
			return;

		var port = ZeroTestUtility.GetFreePort();
		using var publisher = ZeroTestUtility.CreateQueue(port, "retry-publisher", settings => settings.Timeout = TimeSpan.FromMilliseconds(100));
		var server = new ZeroQueueServer { Port = port };

		await Assert.ThrowsAsync<InvalidOperationException>(() =>
			publisher.ProduceAsync("topic/retry", ReadOnlyMemory<byte>.Empty).AsTask());

		try
		{
			await server.StartAsync([]);
			using var subscriber = ZeroTestUtility.CreateQueue(port, "retry-subscriber");
			using var handler = new MessageBuffer();
			await subscriber.SubscribeAsync("topic/retry", handler);

			var message = await PublishUntilReceivedAsync(publisher, handler, "topic/retry", "retried");
			Assert.Equal("retried", Encoding.UTF8.GetString(message.Data));
		}
		finally
		{
			await server.StopAsync([]);
			((IDisposable)server).Dispose();
		}
	}

	[Fact]
	public async Task QueueSnapshotsDefaultTopicAndGroup()
	{
		if(!Global.IsTestingEnabled)
			return;

		using var server = await ZeroServerScope.StartAsync();
		var settings = Configuration.ZeroConnectionSettingsDriver.Instance.GetSettings("ZeroMQ",
			$"server=127.0.0.1;port={server.Port};client=snapshot-settings;topic=original;group=tenant;Timeout=5s;");
		using var publisher = new ZeroQueue("ZeroMQ", settings);
		settings.Topic = "changed";
		settings.Group = "changed";

		using var subscriber = ZeroTestUtility.CreateQueue(server.Port, "snapshot-settings-subscriber", options => options.Group = "tenant");
		using var handler = new MessageBuffer();
		await subscriber.SubscribeAsync("original", handler);

		var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);
		do
		{
			await publisher.ProduceAsync(Encoding.UTF8.GetBytes("snapshot"));
			var message = await handler.TryReceiveAsync(TimeSpan.FromMilliseconds(250));
			if(message.HasValue)
			{
				Assert.Equal("original", message.Value.Topic);
				Assert.Equal("snapshot", Encoding.UTF8.GetString(message.Value.Data));
				return;
			}
		}
		while(DateTime.UtcNow < deadline);

		throw new TimeoutException("Timed out waiting for the snapshotted default topic and group.");
	}

	[Fact]
	public async Task QueueWithShortHeartbeatStillPublishesMessages()
	{
		if(!Global.IsTestingEnabled)
			return;

		using var server = await ZeroServerScope.StartAsync();
		//短心跳会更频繁触发 poller 回调，用来覆盖发布者初始化和发送路径的竞态保护。
		using var publisher = ZeroTestUtility.CreateQueue(server.Port, "publisher", settings => settings.Heartbeat = TimeSpan.FromMilliseconds(10));
		using var subscriber = ZeroTestUtility.CreateQueue(server.Port, "subscriber");
		using var handler = new MessageBuffer();

		await subscriber.SubscribeAsync("topic/heartbeat", handler);
		var message = await PublishUntilReceivedAsync(publisher, handler, "topic/heartbeat", "heartbeat");

		Assert.Equal("heartbeat", Encoding.UTF8.GetString(message.Data));
	}

	[Fact]
	public async Task PublishBurstMessagesAreDelivered()
	{
		if(!Global.IsTestingEnabled)
			return;

		using var server = await ZeroServerScope.StartAsync();
		using var publisher = ZeroTestUtility.CreateQueue(server.Port, "publisher");
		using var subscriber = ZeroTestUtility.CreateQueue(server.Port, "subscriber");
		using var handler = new MessageBuffer();
		var topic = "topic/burst";
		var count = 100;

		await subscriber.SubscribeAsync(topic, handler);
		await PublishUntilReceivedAsync(publisher, handler, topic, "warmup");

		//突发发送覆盖 ZeroQueue.OnQueueReady() 的批量 drain 逻辑。
		for(int i = 0; i < count; i++)
			await publisher.ProduceAsync(topic, Encoding.UTF8.GetBytes($"burst-{i}"));

		var messages = await handler.ReceiveManyAsync(count, TimeSpan.FromSeconds(10));
		var payloads = messages.Select(message => Encoding.UTF8.GetString(message.Data)).ToHashSet();

		Assert.Equal(count, payloads.Count);

		for(int i = 0; i < count; i++)
			Assert.Contains($"burst-{i}", payloads);
	}

	private static async Task<Message> PublishUntilReceivedAsync(ZeroQueue publisher, MessageBuffer handler, string topic, string payload)
	{
		var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);

		do
		{
			await publisher.ProduceAsync(topic, Encoding.UTF8.GetBytes(payload));
			var message = await handler.TryReceiveAsync(TimeSpan.FromMilliseconds(250));
			if(message.HasValue && Encoding.UTF8.GetString(message.Value.Data) == payload)
				return message.Value;
		}
		while(DateTime.UtcNow < deadline);

		throw new TimeoutException($"Timed out warming topic '{topic}'.");
	}
}
