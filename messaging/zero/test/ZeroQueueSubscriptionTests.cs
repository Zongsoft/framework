using System;
using System.Text;
using System.Threading.Tasks;

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
		await Task.Delay(300);

		await publisher.ProduceAsync("topic/repeat", Encoding.UTF8.GetBytes("ready"));
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
		await Task.Delay(300);

		await publisher.ProduceAsync("topic/live", Encoding.UTF8.GetBytes("live"));
		var message = await handler.ReceiveAsync(TimeSpan.FromSeconds(5));

		Assert.False(message.IsEmpty);
		Assert.Equal("live", Encoding.UTF8.GetString(message.Data));
	}
}
