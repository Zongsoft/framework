using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using RabbitMQ.Client;

using Xunit;

using Zongsoft.Collections;
using Zongsoft.Components;

namespace Zongsoft.Messaging.RabbitMQ.Tests;

public class RabbitSubscriberTests
{
	[Fact]
	public async Task SubscribePassesQueueTagsAndManualAcknowledgement()
	{
		using var queue = CreateQueue("Subscriber-Unit", "orders.queue");
		var channel = DispatchProxy.Create<IChannel, ChannelProxy>();
		var proxy = (ChannelProxy)(object)channel;
		var handler = new RecordingHandler();
		var subscriber = new RabbitSubscriber(queue, channel, "orders.*", "priority;region", handler);

		var method = typeof(RabbitSubscriber).GetMethod("SubscribeAsync", BindingFlags.Instance | BindingFlags.NonPublic);
		var identifier = await Assert.IsType<Task<string>>(method.Invoke(subscriber, [CancellationToken.None]));

		Assert.Equal("consumer-unit", identifier);
		Assert.Equal("orders.queue", proxy.Queue);
		Assert.False(proxy.AutoAck);
		Assert.Equal("priority,region", proxy.ConsumerTag);
		Assert.Same(subscriber, proxy.Consumer);
	}

	[Fact]
	public async Task DeliveryMapsMessageAndAcknowledgement()
	{
		using var queue = CreateQueue("Delivery-Unit");
		var channel = DispatchProxy.Create<IChannel, ChannelProxy>();
		var proxy = (ChannelProxy)(object)channel;
		var handler = new RecordingHandler();
		var subscriber = new RabbitSubscriber(queue, channel, "orders.created", null, handler);
		var payload = new byte[] { 2, 4, 6, 8 };
		var properties = new BasicProperties { MessageId = "message-001" };

		await ((IAsyncBasicConsumer)subscriber).HandleBasicDeliverAsync(
			"consumer-unit", 42, false, "unit.exchange", "orders.created", properties, payload, CancellationToken.None);

		Assert.NotNull(handler.Message);
		Assert.Equal("message-001", handler.Message.Value.Identifier);
		Assert.Equal("orders.created", handler.Message.Value.Topic);
		Assert.Equal(payload, handler.Message.Value.Data);
		Assert.Null(proxy.AcknowledgedDelivery);

		await handler.Message.Value.AcknowledgeAsync();
		Assert.Equal((ulong)42, proxy.AcknowledgedDelivery);
		Assert.False(proxy.Multiple);
	}

	[Fact]
	public async Task DeliveryWithoutMessageIdLeavesIdentifierEmpty()
	{
		using var queue = CreateQueue("Delivery-NoId");
		var channel = DispatchProxy.Create<IChannel, ChannelProxy>();
		var proxy = (ChannelProxy)(object)channel;
		var handler = new RecordingHandler();
		var subscriber = new RabbitSubscriber(queue, channel, "orders.created", null, handler);

		await ((IAsyncBasicConsumer)subscriber).HandleBasicDeliverAsync(
			"consumer-unit", 7, false, "unit.exchange", "orders.created", new BasicProperties(), new byte[] { 9 }, CancellationToken.None);

		Assert.NotNull(handler.Message);
		Assert.Null(handler.Message.Value.Identifier);
		Assert.Equal(new byte[] { 9 }, handler.Message.Value.Data);

		await handler.Message.Value.AcknowledgeAsync();
		Assert.Equal((ulong)7, proxy.AcknowledgedDelivery);
	}

	[Fact]
	public void NullChannelThrowsArgumentNullException()
	{
		using var queue = CreateQueue("Subscriber-Null");

		Assert.Throws<ArgumentNullException>(() => new RabbitSubscriber(queue, null, "orders.*", null, new RecordingHandler()));
	}

	[Fact]
	public async Task UnsubscribeClosesChannel()
	{
		using var queue = CreateQueue("Subscriber-Close");
		var channel = DispatchProxy.Create<IChannel, ChannelProxy>();
		var proxy = (ChannelProxy)(object)channel;
		proxy.IsOpen = true;
		var subscriber = new RabbitSubscriber(queue, channel, "orders.*", null, new RecordingHandler());

		await SubscribeAsync(subscriber);
		await subscriber.UnsubscribeAsync();

		Assert.Equal(1, proxy.CloseCount);
		Assert.Equal(1, proxy.CancelCount);
		Assert.Equal("consumer-unit", proxy.CancelledConsumerTag);
		Assert.True(subscriber.IsClosed);
		Assert.False(proxy.Disposed);
	}

	[Fact]
	public async Task DisposeClosesAndDisposesChannel()
	{
		using var queue = CreateQueue("Subscriber-Dispose");
		var channel = DispatchProxy.Create<IChannel, ChannelProxy>();
		var proxy = (ChannelProxy)(object)channel;
		proxy.IsOpen = true;
		var subscriber = new RabbitSubscriber(queue, channel, "orders.*", null, new RecordingHandler());

		await ((IAsyncDisposable)subscriber).DisposeAsync();

		Assert.True(subscriber.IsClosed);
		Assert.True(subscriber.IsDisposed);
		Assert.Equal(1, proxy.CloseCount);
		Assert.True(proxy.Disposed);
	}

	private static async Task<string> SubscribeAsync(RabbitSubscriber subscriber)
	{
		var method = typeof(RabbitSubscriber).GetMethod("SubscribeAsync", BindingFlags.Instance | BindingFlags.NonPublic);
		return await Assert.IsType<Task<string>>(method.Invoke(subscriber, [CancellationToken.None]));
	}

	private static RabbitQueue CreateQueue(string client, string queue = null)
	{
		var settings = Configuration.RabbitConnectionSettingsDriver.Instance.GetSettings("RabbitMQ", $"server=127.0.0.1;client={client};queue={queue};");
		return new RabbitQueue("RabbitMQ", settings);
	}

	private sealed class RecordingHandler : HandlerBase<Message>
	{
		public Message? Message { get; private set; }

		protected override ValueTask OnHandleAsync(Message message, Parameters parameters, CancellationToken cancellation)
		{
			this.Message = message;
			return ValueTask.CompletedTask;
		}
	}

	private class ChannelProxy : DispatchProxy
	{
		public string Queue { get; private set; }
		public bool AutoAck { get; private set; }
		public string ConsumerTag { get; private set; }
		public IAsyncBasicConsumer Consumer { get; private set; }
		public ulong? AcknowledgedDelivery { get; private set; }
		public bool Multiple { get; private set; }
		public bool IsOpen { get; set; }
		public int CloseCount { get; private set; }
		public int CancelCount { get; private set; }
		public string CancelledConsumerTag { get; private set; }
		public bool Disposed { get; private set; }

		protected override object Invoke(MethodInfo targetMethod, object[] args)
		{
			switch(targetMethod.Name)
			{
				case "BasicConsumeAsync":
					this.Queue = Assert.IsType<string>(args[0]);
					this.AutoAck = Assert.IsType<bool>(args[1]);
					this.ConsumerTag = Assert.IsType<string>(args[2]);
					this.Consumer = Assert.IsAssignableFrom<IAsyncBasicConsumer>(args.Length == 8 ? args[6] : args[3]);
					return Task.FromResult("consumer-unit");
				case "BasicAckAsync":
					this.AcknowledgedDelivery = Assert.IsType<ulong>(args[0]);
					this.Multiple = Assert.IsType<bool>(args[1]);
					return ValueTask.CompletedTask;
				case "CloseAsync":
					this.CloseCount++;
					return Task.CompletedTask;
				case "BasicCancelAsync":
					this.CancelCount++;
					this.CancelledConsumerTag = Assert.IsType<string>(args[0]);
					return Task.CompletedTask;
				case "get_IsOpen":
					return this.IsOpen;
				case "get_IsClosed":
					return !this.IsOpen;
				case nameof(IDisposable.Dispose):
					this.Disposed = true;
					return null;
				default:
					return RabbitQueueBehaviorTests.Default(targetMethod.ReturnType);
			}
		}
	}
}
