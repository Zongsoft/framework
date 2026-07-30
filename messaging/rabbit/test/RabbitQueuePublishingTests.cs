using System;
using System.Text;
using System.Threading.Tasks;

using Xunit;

namespace Zongsoft.Messaging.RabbitMQ.Tests;

public class RabbitQueuePublishingTests
{
	private const string RABBIT_UNAVAILABLE = "RabbitMQ is unavailable at localhost:5672 for program/xxxxxx. Start the RabbitMQ test Pod.";

	[Fact]
	public async Task PublishAndConsumeMessage()
	{
		Assert.SkipUnless(await RabbitTestUtility.IsAvailableAsync(), RABBIT_UNAVAILABLE);

		var identity = Guid.NewGuid().ToString("N");
		var exchange = $"tests.exchange.{identity}";
		var queueName = $"tests.queue.{identity}";
		var topic = $"tests/basic/{identity}";
		using var publisher = RabbitTestUtility.CreateQueue($"publisher-{identity}", exchange, queueName);
		using var subscriber = RabbitTestUtility.CreateQueue($"subscriber-{identity}", exchange, queueName);
		using var messages = new RabbitMessageBuffer();

		var consumer = await subscriber.SubscribeAsync(topic, messages);
		Assert.NotNull(consumer);

		var identifier = await publisher.ProduceAsync(topic, Encoding.UTF8.GetBytes("Hello RabbitMQ 7"));
		var message = await messages.ReceiveAsync(TimeSpan.FromSeconds(10));

		Assert.False(string.IsNullOrEmpty(identifier));
		Assert.Equal(12, identifier.Length);
		Assert.Equal(topic.Replace('/', '.'), message.Topic);
		Assert.Equal("Hello RabbitMQ 7", Encoding.UTF8.GetString(message.Data));
		Assert.Equal(1, messages.AcknowledgementCount);

		await consumer.UnsubscribeAsync();
	}

	[Fact]
	public async Task PublishAndConsumeWithGeneratedQueue()
	{
		Assert.SkipUnless(await RabbitTestUtility.IsAvailableAsync(), RABBIT_UNAVAILABLE);

		var identity = Guid.NewGuid().ToString("N");
		var topic = $"tests/generated/{identity}";
		using var queue = RabbitTestUtility.CreateQueue($"client-{identity}", null, null);
		using var messages = new RabbitMessageBuffer();

		var consumer = await queue.SubscribeAsync(topic, messages);
		Assert.NotNull(consumer);
		Assert.Single(queue.Subscribers);

		var identifier = await queue.ProduceAsync(topic, Encoding.UTF8.GetBytes("server-generated queue"));
		var message = await messages.ReceiveAsync(TimeSpan.FromSeconds(10));

		Assert.False(string.IsNullOrEmpty(identifier));
		Assert.Equal(topic.Replace('/', '.'), message.Topic);
		Assert.Equal("server-generated queue", Encoding.UTF8.GetString(message.Data));
		Assert.Equal(1, messages.AcknowledgementCount);

		await consumer.UnsubscribeAsync();
		Assert.Empty(queue.Subscribers);
	}

	[Fact]
	public async Task UnsubscribeStopsMessageDelivery()
	{
		Assert.SkipUnless(await RabbitTestUtility.IsAvailableAsync(), RABBIT_UNAVAILABLE);

		var identity = Guid.NewGuid().ToString("N");
		var exchange = $"tests.exchange.{identity}";
		var queueName = $"tests.queue.{identity}";
		var topic = $"tests/unsubscribe/{identity}";
		using var publisher = RabbitTestUtility.CreateQueue($"publisher-{identity}", exchange, queueName);
		using var subscriber = RabbitTestUtility.CreateQueue($"subscriber-{identity}", exchange, queueName);
		using var messages = new RabbitMessageBuffer();

		var consumer = await subscriber.SubscribeAsync(topic, messages);
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
}
