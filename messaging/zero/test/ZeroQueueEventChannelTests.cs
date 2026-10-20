using System;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.Components;

namespace Zongsoft.Messaging.ZeroMQ.Tests;

public class ZeroQueueEventChannelTests
{
	[Fact]
	public async Task ChannelCanReopenAndDoesNotOwnQueue()
	{
		if(!Global.IsTestingEnabled)
			return;

		await using var server = await ZeroServerScope.StartAsync();
		using var queue = ZeroTestUtility.CreateQueue(server.Port, "event-lifecycle");
		await using var channel = new ZeroQueueEventChannel(queue);
		var receiver = new Receiver();
		await Task.WhenAll(channel.OpenAsync(receiver).AsTask(), channel.OpenAsync(receiver).AsTask());

		var first = Assert.Single(queue.Subscribers);
		await Task.WhenAll(channel.CloseAsync().AsTask(), channel.CloseAsync().AsTask());

		Assert.Empty(queue.Subscribers);
		Assert.False(queue.IsDisposed);

		using var canceled = new CancellationTokenSource();
		canceled.Cancel();
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => channel.OpenAsync(receiver, canceled.Token).AsTask());
		await channel.OpenAsync(receiver);

		Assert.NotSame(first, Assert.Single(queue.Subscribers));

		await channel.DisposeAsync();
		await Assert.ThrowsAsync<ObjectDisposedException>(() => channel.OpenAsync(receiver).AsTask());

		Assert.Empty(queue.Subscribers);
		Assert.False(queue.IsDisposed);
	}

	[Fact]
	public async Task DefaultEventChannelUsesBroadcastWithoutStorage()
	{
		if(!Global.IsTestingEnabled)
			return;

		await using var server = await ZeroServerScope.StartAsync();
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

	private sealed class TestEventRegistry(string name) : EventRegistryBase(name);
	private sealed class Receiver : IEventReceiver
	{
		public ValueTask ReceiveAsync(string name, object argument, Collections.Parameters parameters, CancellationToken cancellation = default) => ValueTask.CompletedTask;
	}
}
