using Xunit;

namespace Zongsoft.Messaging.ZeroMQ.Tests;

public class ZeroQueueEventChannelTests
{
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
}
