using System;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.Components;

namespace Zongsoft.Messaging.ZeroMQ.Tests;

public class ZeroQueueEventChannelTests
{
	[Fact]
	public async Task DefaultEventChannelUsesBroadcastWithoutStorage()
	{
		if(!Global.IsTestingEnabled)
			return;

		using var server = await ZeroServerScope.StartAsync();
		using var publisher = ZeroTestUtility.CreateQueue(server.Port, "event-publisher");
		using var subscriber = ZeroTestUtility.CreateQueue(server.Port, "event-subscriber");
		using var messages = new MessageBuffer();
		await subscriber.SubscribeAsync("Events", messages);
		await using var channel = new ZeroQueueEventChannel(publisher);

		Assert.Equal(MessageReliability.MostOnce, channel.Options.Reliability);
		for(var index = 0; index < 100; index++)
		{
			await channel.SendAsync(new EventContext(new TestEventRegistry("Tests"), "Changed"));
			if(messages.Count > 0)
				break;
			await Task.Delay(25);
		}

		var message = await messages.ReceiveAsync(TimeSpan.FromSeconds(5));
		Assert.Equal("Events/Tests:Changed", message.Topic);
		Assert.NotEmpty(message.Data);
	}

	[Theory]
	[InlineData("Events/Orders.Created", "Orders.Created")]
	[InlineData("Events/Domain/Changed", "Domain/Changed")]
	public void EventNameUsesLogicalTopicBoundary(string topic, string expected) => Assert.Equal(expected, ZeroQueueEventChannel.GetEventName(topic));

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("Events")]
	[InlineData("EventsX/Orders.Created")]
	[InlineData("tenant:Events/Orders.Created")]
	public void EventNameRejectsNonLogicalEventTopics(string topic) => Assert.Null(ZeroQueueEventChannel.GetEventName(topic));

	private sealed class TestEventRegistry(string name) : EventRegistryBase(name);
}
