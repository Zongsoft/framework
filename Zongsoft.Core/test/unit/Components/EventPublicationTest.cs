using System;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.Collections;

namespace Zongsoft.Components.Tests;

[Collection(nameof(EventRegistryExtensionCollection))]
public class EventPublicationTest
{
	[Theory]
	[InlineData(false, false)]
	[InlineData(false, true)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public async Task RemoteScopeIsCheckedBeforeDispatchWithoutSuppressingLocalHandlers(bool channelConfigured, bool allowed)
	{
		var exchanger = EventExchanger.Instance;
		Assert.Empty(exchanger.Channels);
		Assert.Empty(exchanger.Filtering);
		var original = exchanger.Dispatcher;
		var dispatcher = new Dispatcher();
		var channel = new Channel();
		var registry = new Registry();
		var handler = new Handler();
		registry.Descriptor.Handlers.Add(handler);
		exchanger.Dispatcher = dispatcher;

		if(channelConfigured)
			exchanger.Channels.Add(channel);

		if(allowed)
			exchanger.Filtering.Add(new(EventFiltering.EntryKind.Inclusive, registry.Name, registry.Descriptor.Name));

		exchanger.Filtering.Add(new(EventFiltering.EntryKind.Exclusive, null, null));

		try
		{
			await exchanger.StartAsync([]);
			Assert.True(await registry.PublishAsync(TestContext.Current.CancellationToken));
			Assert.Equal(1, handler.Calls);
			Assert.Equal(channelConfigured && allowed ? 1 : 0, dispatcher.Calls);
		}
		finally
		{
			await exchanger.StopAsync([]);
			exchanger.Filtering.Clear();
			exchanger.Channels.Remove(channel);
			exchanger.Dispatcher = original;
		}
	}

	[Fact]
	public async Task DefaultRemoteScopePreservesPublicationAndReceptionRemainsLocal()
	{
		var exchanger = EventExchanger.Instance;
		Assert.Empty(exchanger.Channels);
		Assert.Empty(exchanger.Filtering);
		var original = exchanger.Dispatcher;
		var locator = exchanger.Locator;
		var dispatcher = new Dispatcher();
		var channel = new Channel();
		var registry = new Registry();
		var handler = new Handler();
		registry.Descriptor.Handlers.Add(handler);
		exchanger.Dispatcher = dispatcher;
		exchanger.Channels.Add(channel);
		exchanger.Locator = _ => (registry, registry.Descriptor);

		try
		{
			await exchanger.StartAsync([]);
			await registry.PublishAsync(TestContext.Current.CancellationToken);
			Assert.Equal(1, dispatcher.Calls);
			exchanger.Filtering.Add(new(EventFiltering.EntryKind.Exclusive, null, null));
			await ((IEventReceiver)exchanger).ReceiveAsync(registry.Descriptor.QualifiedName, typeof(string), "received", null, TestContext.Current.CancellationToken);
			Assert.Equal(2, handler.Calls);
			Assert.Equal(1, dispatcher.Calls);
		}
		finally
		{
			await exchanger.StopAsync([]);
			exchanger.Filtering.Clear();
			exchanger.Channels.Remove(channel);
			exchanger.Dispatcher = original;
			exchanger.Locator = locator;
		}
	}

	private sealed class Registry : EventRegistryBase
	{
		public Registry() : base("Publication") => this.Event(this.Descriptor);
		public EventDescriptor<string> Descriptor { get; } = new("Created");
		public ValueTask<bool> PublishAsync(CancellationToken cancellation) => this.RaiseAsync(this.Descriptor, "payload", null, cancellation);
	}
	private sealed class Handler : HandlerBase<string>
	{
		public int Calls { get; private set; }
		protected override ValueTask OnHandleAsync(string argument, Parameters parameters, CancellationToken cancellation)
		{
			this.Calls++;
			return ValueTask.CompletedTask;
		}
	}
	private sealed class Channel : IEventChannel
	{
		public ValueTask OpenAsync(IEventReceiver receiver, CancellationToken cancellation = default) => ValueTask.CompletedTask;
		public ValueTask CloseAsync(CancellationToken cancellation = default) => ValueTask.CompletedTask;
		public ValueTask SendAsync(EventContext data, CancellationToken cancellation = default) => throw new InvalidOperationException();
		public ValueTask DisposeAsync() => ValueTask.CompletedTask;
	}
	private sealed class Dispatcher : IEventDispatcher
	{
		public int Calls { get; private set; }
		public ValueTask DispatchAsync(EventContext argument, IExecutor<EventContext> executor, CancellationToken cancellation = default)
		{
			this.Calls++;
			return ValueTask.CompletedTask;
		}
	}
}
