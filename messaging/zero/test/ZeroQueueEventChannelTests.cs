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
}
