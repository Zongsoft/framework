using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.Extensions.DependencyInjection;

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
		await using var queue = ZeroTestUtility.CreateQueue(server.Port, "event-lifecycle");
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
		await using var publisher = ZeroTestUtility.CreateQueue(server.Port, "event-publisher");
		await using var subscriber = ZeroTestUtility.CreateQueue(server.Port, "event-subscriber");

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
	[InlineData(false)]
	[InlineData(true)]
	public async Task ChannelRoundTripPreservesBothDeclarationForms(bool generic)
	{
		if(!Global.IsTestingEnabled)
			return;

		var provider = new Services.ServiceProviderFactory().CreateServiceProvider(new ServiceCollection());
		await using var providerDisposal = (IAsyncDisposable)provider;
		await using var application = new TestApplication(provider);

		var module = new TestModule();
		application.Modules.Add(module);
		var single = new CaptureHandler<string>();
		var batch = new CaptureHandler<IEnumerable<string>>();

		var singleName = generic ? "SingleChanged" : "Changed";
		var batchName = generic ? "BatchChanged" : "Changed";
		module.Events[singleName].Handlers.Add(single);
		module.Events[batchName].Handlers.Add(batch);

		await using var server = await ZeroServerScope.StartAsync();
		await using var publisher = ZeroTestUtility.CreateQueue(server.Port, "signature-publisher");
		await using var subscriber = ZeroTestUtility.CreateQueue(server.Port, "signature-subscriber");
		await using var outgoing = new ZeroQueueEventChannel(publisher);
		await using var incoming = new ZeroQueueEventChannel(subscriber);
		await incoming.OpenAsync(EventExchanger.Instance);

		//等待实际订阅可见；空载荷探针被通用事件接收器忽略。
		await ZeroTestUtility.PublishUntilAcceptedAsync(publisher, Events.GetTopic("Wire:" + singleName), ReadOnlyMemory<byte>.Empty);
		await outgoing.SendAsync(new EventContext<string>(module.Events, singleName, "one"));
		Assert.Equal("one", await single.Received.Task.WaitAsync(TimeSpan.FromSeconds(5)));
		Assert.False(batch.Received.Task.IsCompleted);

		await outgoing.SendAsync(new EventContext<IEnumerable<string>>(module.Events, batchName, new[] { "two", "three" }));
		Assert.Equal(["two", "three"], (await batch.Received.Task.WaitAsync(TimeSpan.FromSeconds(5))).ToArray());
		Assert.Equal(1, single.Count);
		Assert.Equal(1, batch.Count);
	}

	private sealed class TestApplication(IServiceProvider services) : Services.ApplicationContext(services);
	private sealed class TestModule() : Services.ApplicationModule<WireRegistry>("Wire");
	public sealed class WireRegistry : EventRegistryBase
	{
		public WireRegistry() : base("Wire")
		{
			this.Event(new EventDescriptor("Changed", [typeof(string), typeof(IEnumerable<string>)]));
			this.Event(new EventDescriptor<string>("SingleChanged"));
			this.Event(new EventDescriptor<IEnumerable<string>>("BatchChanged"));
		}
	}
	private sealed class CaptureHandler<T> : HandlerBase<T>
	{
		public int Count { get; private set; }
		public TaskCompletionSource<T> Received { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		protected override ValueTask OnHandleAsync(T argument, Collections.Parameters parameters, CancellationToken cancellation)
		{
			this.Count++;
			this.Received.TrySetResult(argument);
			return ValueTask.CompletedTask;
		}
	}

	private sealed class TestEventRegistry : EventRegistryBase
	{
		public TestEventRegistry(string name) : base(name) => this.Event<object>("Changed");
	}
	private sealed class Receiver : IEventReceiver
	{
		public ValueTask ReceiveAsync(string name, Type argumentType, object argument, Collections.Parameters parameters, CancellationToken cancellation = default) => ValueTask.CompletedTask;
	}
}
