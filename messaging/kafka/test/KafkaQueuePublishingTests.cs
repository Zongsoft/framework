using System;
using System.Text;
using System.Threading.Tasks;

using Xunit;

namespace Zongsoft.Messaging.Kafka.Tests;

public class KafkaQueuePublishingTests
{
	private const string KAFKA_UNAVAILABLE = "Kafka is unavailable at localhost:9092. Start test/Zongsoft.Messaging.Kafka-pod.yaml with Podman.";

	[Fact]
	public async Task PublishAndConsumeMessage()
	{
		Assert.SkipUnless(KafkaTestUtility.IsAvailable(), KAFKA_UNAVAILABLE);

		var identity = Guid.NewGuid().ToString("N");
		var topic = $"tests-basic-{identity}";
		await KafkaTestUtility.CreateTopicAsync(topic);

		using var publisher = KafkaTestUtility.CreateQueue($"publisher-{identity}", $"publisher-{identity}");
		using var subscriber = KafkaTestUtility.CreateQueue($"subscriber-{identity}", $"subscriber-{identity}");
		using var messages = new KafkaMessageBuffer();

		var consumer = await subscriber.SubscribeAsync(topic, messages);
		Assert.NotNull(consumer);

		var delivery = await KafkaTestUtility.ProduceAndReceiveAsync(
			publisher, topic, "Hello Apache Kafka", messages, TimeSpan.FromSeconds(15));

		Assert.False(string.IsNullOrEmpty(delivery.Identifier));
		Assert.False(delivery.Message.IsEmpty);
		Assert.Equal(topic, delivery.Message.Topic);
		Assert.Equal("Hello Apache Kafka", Encoding.UTF8.GetString(delivery.Message.Data));
		Assert.NotEqual(default, delivery.Message.Timestamp);
		Assert.True(messages.AcknowledgementCount > 0);
	}

	[Fact]
	public async Task UnsubscribeStopsMessageDelivery()
	{
		Assert.SkipUnless(KafkaTestUtility.IsAvailable(), KAFKA_UNAVAILABLE);

		var identity = Guid.NewGuid().ToString("N");
		var topic = $"tests-unsubscribe-{identity}";
		await KafkaTestUtility.CreateTopicAsync(topic);

		using var publisher = KafkaTestUtility.CreateQueue($"publisher-{identity}", $"publisher-{identity}");
		using var subscriber = KafkaTestUtility.CreateQueue($"subscriber-{identity}", $"subscriber-{identity}");
		using var messages = new KafkaMessageBuffer();

		var consumer = await subscriber.SubscribeAsync(topic, messages);
		var first = await KafkaTestUtility.ProduceAndReceiveAsync(
			publisher, topic, "before", messages, TimeSpan.FromSeconds(15));
		Assert.Equal("before", Encoding.UTF8.GetString(first.Message.Data));

		await consumer.UnsubscribeAsync();
		await messages.DrainAsync(TimeSpan.FromMilliseconds(500));
		var acknowledgements = messages.AcknowledgementCount;

		var identifier = await publisher.ProduceAsync(topic, Encoding.UTF8.GetBytes("after"));
		Assert.False(string.IsNullOrEmpty(identifier));
		Assert.Null(await messages.TryReceiveAsync(TimeSpan.FromSeconds(2)));
		Assert.Equal(acknowledgements, messages.AcknowledgementCount);
	}
}
