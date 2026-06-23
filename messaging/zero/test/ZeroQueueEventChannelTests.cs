using System;
using System.Threading.Tasks;

using Xunit;

namespace Zongsoft.Messaging.ZeroMQ.Tests;

public class ZeroQueueEventChannelTests
{
	[Fact]
	public async Task EventChannelCloseBeforeOpenIsSafe()
	{
		using var queue = ZeroTestUtility.CreateQueue(ZeroTestUtility.GetFreePort(), "events");
		var channel = queue.Channel;

		await channel.CloseAsync();
		await channel.DisposeAsync();
	}

	[Fact]
	public void EventChannelWrapperRequiresQueue()
	{
		var channel = new ZeroQueueEventChannel();

		Assert.Throws<InvalidOperationException>(() => _ = channel.IsClosed);
		Assert.Throws<InvalidOperationException>(() => channel.CloseAsync().AsTask().GetAwaiter().GetResult());
	}

	[Fact]
	public async Task EventChannelWrapperUnsubscribesPreviousQueueClosedEvent()
	{
		using var first = ZeroTestUtility.CreateQueue(ZeroTestUtility.GetFreePort(), "events-first");
		using var second = ZeroTestUtility.CreateQueue(ZeroTestUtility.GetFreePort(), "events-second");
		var wrapper = new ZeroQueueEventChannel();
		var closed = 0;
		wrapper.Closed += (_, _) => closed++;

		wrapper.Queue = first;
		wrapper.Queue = second;

		await first.Channel.CloseAsync();
		Assert.Equal(0, closed);

		await second.Channel.CloseAsync();
		Assert.Equal(1, closed);
	}
}
