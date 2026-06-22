using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;

namespace Zongsoft.Messaging.ZeroMQ.Tests;

public class ZeroQueuePublishingTests
{
	[Fact]
	public async Task QueueWithShortHeartbeatStillPublishesMessages()
	{
		using var server = await ZeroServerScope.StartAsync();
		//短心跳会更频繁触发 poller 回调，用来覆盖发布者初始化和发送路径的竞态保护。
		using var publisher = ZeroTestUtility.CreateQueue(server.Port, "publisher", settings => settings.Heartbeat = TimeSpan.FromMilliseconds(10));
		using var subscriber = ZeroTestUtility.CreateQueue(server.Port, "subscriber");
		var handler = new MessageCollector();

		await subscriber.SubscribeAsync("topic/heartbeat", handler);
		await Task.Delay(750);

		await publisher.ProduceAsync("topic/heartbeat", Encoding.UTF8.GetBytes("heartbeat"));
		var message = await handler.ReceiveAsync(TimeSpan.FromSeconds(10));

		Assert.Equal("heartbeat", Encoding.UTF8.GetString(message.Data));
	}

	[Fact]
	public async Task PublishBurstMessagesAreDelivered()
	{
		using var server = await ZeroServerScope.StartAsync();
		using var publisher = ZeroTestUtility.CreateQueue(server.Port, "publisher");
		using var subscriber = ZeroTestUtility.CreateQueue(server.Port, "subscriber");
		using var handler = new MessageBuffer();
		var topic = "topic/burst";
		var count = 100;

		await subscriber.SubscribeAsync(topic, handler);
		await Task.Delay(750);

		//突发发送覆盖 ZeroQueue.OnQueueReady() 的批量 drain 逻辑。
		for(int i = 0; i < count; i++)
			await publisher.ProduceAsync(topic, Encoding.UTF8.GetBytes($"burst-{i}"));

		var messages = await handler.ReceiveManyAsync(count, TimeSpan.FromSeconds(10));
		var payloads = messages.Select(message => Encoding.UTF8.GetString(message.Data)).ToHashSet();

		Assert.Equal(count, payloads.Count);

		for(int i = 0; i < count; i++)
			Assert.Contains($"burst-{i}", payloads);
	}
}
