using System;
using System.Text;
using System.Threading.Tasks;

using RabbitMQ.Client;

using Xunit;

namespace Zongsoft.Messaging.RabbitMQ.Tests;

public class RabbitQueuePublishingTests
{
	private const string RABBIT_UNAVAILABLE = "RabbitMQ is unavailable at localhost:5672 for program/xxxxxx. Start the RabbitMQ test Pod.";

	[Fact]
	public async Task PublishAndConsumeMessage()
	{
		if(!Global.IsTestingEnabled)
			return;

		Assert.SkipUnless(await RabbitTestUtility.IsAvailableAsync(), RABBIT_UNAVAILABLE);

		var baseline = await RabbitTestUtility.GetBrokerSnapshotAsync();
		var identity = Guid.NewGuid().ToString("N");
		var exchange = $"tests.exchange.{identity}";
		var queueName = $"tests.queue.{identity}";
		var topic = $"tests/basic/{identity}";
		var publisher = RabbitTestUtility.CreateQueue($"publisher-{identity}", exchange, queueName);
		var subscriber = RabbitTestUtility.CreateQueue($"subscriber-{identity}", exchange, queueName);
		using var messages = new RabbitMessageBuffer();
		RabbitSubscriber consumer = null;
		IConnection publisherConnection = null;
		IChannel publisherChannel = null;
		IChannel subscriberChannel = null;

		try
		{
			consumer = await subscriber.SubscribeAsync(topic, messages);
			Assert.NotNull(consumer);
			subscriberChannel = consumer.Channel;

			var identifier = await publisher.ProduceAsync(topic, Encoding.UTF8.GetBytes("Hello RabbitMQ 7"));
			var message = await messages.ReceiveAsync(TimeSpan.FromSeconds(10));

			Assert.False(string.IsNullOrEmpty(identifier));
			Assert.Equal(12, identifier.Length);
			Assert.Equal(topic.Replace('/', '.'), message.Topic);
			Assert.Equal("Hello RabbitMQ 7", Encoding.UTF8.GetString(message.Data));
			Assert.Equal(1, messages.AcknowledgementCount);

			publisherConnection = RabbitTestUtility.GetConnection(publisher);
			publisherChannel = RabbitTestUtility.GetPublishingChannel(publisher);
		}
		finally
		{
			if(consumer != null)
				await consumer.DisposeAsync();
			subscriber.Dispose();
			publisher.Dispose();
			await RabbitTestUtility.DeleteTestQueueAsync(queueName);
			await RabbitTestUtility.DeleteTestExchangeAsync(exchange);
		}

		Assert.True(consumer.IsClosed);
		Assert.True(consumer.IsDisposed);
		Assert.Null(consumer.Channel);
		Assert.Null(consumer.Handler);
		Assert.True(subscriberChannel.IsClosed);
		Assert.Empty(subscriber.Subscribers);
		Assert.Empty(publisher.Subscribers);
		Assert.True(subscriber.IsDisposed);
		Assert.True(publisher.IsDisposed);
		Assert.True(publisherChannel.IsClosed);
		Assert.False(publisherConnection.IsOpen);
		await AssertBrokerRestoredAsync(baseline);
	}

	[Fact]
	public async Task PublishAndConsumeWithGeneratedQueue()
	{
		if(!Global.IsTestingEnabled)
			return;

		Assert.SkipUnless(await RabbitTestUtility.IsAvailableAsync(), RABBIT_UNAVAILABLE);

		var baseline = await RabbitTestUtility.GetBrokerSnapshotAsync();
		var identity = Guid.NewGuid().ToString("N");
		var topic = $"tests/generated/{identity}";
		var queue = RabbitTestUtility.CreateQueue($"client-{identity}", null, null);
		using var messages = new RabbitMessageBuffer();
		RabbitSubscriber consumer = null;
		IConnection connection = null;
		IChannel publishingChannel = null;
		IChannel subscriberChannel = null;

		try
		{
			consumer = await queue.SubscribeAsync(topic, messages);
			Assert.NotNull(consumer);
			Assert.Single(queue.Subscribers);
			subscriberChannel = consumer.Channel;

			var identifier = await queue.ProduceAsync(topic, Encoding.UTF8.GetBytes("server-generated queue"));
			var message = await messages.ReceiveAsync(TimeSpan.FromSeconds(10));

			Assert.False(string.IsNullOrEmpty(identifier));
			Assert.Equal(topic.Replace('/', '.'), message.Topic);
			Assert.Equal("server-generated queue", Encoding.UTF8.GetString(message.Data));
			Assert.Equal(1, messages.AcknowledgementCount);

			connection = RabbitTestUtility.GetConnection(queue);
			publishingChannel = RabbitTestUtility.GetPublishingChannel(queue);
		}
		finally
		{
			if(consumer != null)
				await consumer.DisposeAsync();
			queue.Dispose();
		}

		Assert.True(consumer.IsClosed);
		Assert.True(consumer.IsDisposed);
		Assert.Null(consumer.Channel);
		Assert.Null(consumer.Handler);
		Assert.True(subscriberChannel.IsClosed);
		Assert.True(queue.IsDisposed);
		Assert.Empty(queue.Subscribers);
		Assert.True(publishingChannel.IsClosed);
		Assert.False(connection.IsOpen);
		await AssertBrokerRestoredAsync(baseline);
	}

	[Fact]
	public async Task UnsubscribeStopsMessageDelivery()
	{
		if(!Global.IsTestingEnabled)
			return;

		Assert.SkipUnless(await RabbitTestUtility.IsAvailableAsync(), RABBIT_UNAVAILABLE);

		var baseline = await RabbitTestUtility.GetBrokerSnapshotAsync();
		var identity = Guid.NewGuid().ToString("N");
		var exchange = $"tests.exchange.{identity}";
		var queueName = $"tests.queue.{identity}";
		var topic = $"tests/unsubscribe/{identity}";
		var publisher = RabbitTestUtility.CreateQueue($"publisher-{identity}", exchange, queueName);
		var subscriber = RabbitTestUtility.CreateQueue($"subscriber-{identity}", exchange, queueName);
		using var messages = new RabbitMessageBuffer();
		RabbitSubscriber consumer = null;
		IChannel subscriberChannel = null;

		try
		{
			consumer = await subscriber.SubscribeAsync(topic, messages);
			subscriberChannel = consumer.Channel;
			await publisher.ProduceAsync(topic, Encoding.UTF8.GetBytes("before"));
			Assert.Equal("before", Encoding.UTF8.GetString((await messages.ReceiveAsync(TimeSpan.FromSeconds(10))).Data));

			await consumer.UnsubscribeAsync();
			var acknowledgements = messages.AcknowledgementCount;
			Assert.Empty(subscriber.Subscribers);
			var identifier = await publisher.ProduceAsync(topic, Encoding.UTF8.GetBytes("after"));

			Assert.False(string.IsNullOrEmpty(identifier));
			Assert.Null(await messages.TryReceiveAsync(TimeSpan.FromSeconds(2)));
			Assert.Equal(acknowledgements, messages.AcknowledgementCount);
		}
		finally
		{
			if(consumer != null)
				await consumer.DisposeAsync();
			subscriber.Dispose();
			publisher.Dispose();
			await RabbitTestUtility.DeleteTestQueueAsync(queueName);
			await RabbitTestUtility.DeleteTestExchangeAsync(exchange);
		}

		Assert.True(consumer.IsClosed);
		Assert.True(consumer.IsDisposed);
		Assert.Null(consumer.Channel);
		Assert.Null(consumer.Handler);
		Assert.True(subscriberChannel.IsClosed);
		Assert.Empty(subscriber.Subscribers);
		Assert.Empty(publisher.Subscribers);
		Assert.True(subscriber.IsDisposed);
		Assert.True(publisher.IsDisposed);
		await AssertBrokerRestoredAsync(baseline);
	}

	private static async Task AssertBrokerRestoredAsync(RabbitBrokerSnapshot baseline)
	{
		var restored = await RabbitTestUtility.WaitForBrokerRestoreAsync(baseline, TimeSpan.FromSeconds(60));
		Assert.True(restored.IsAtMost(baseline), $"RabbitMQ resources did not return to baseline. Baseline={baseline}; Current={restored}.");
	}
}
