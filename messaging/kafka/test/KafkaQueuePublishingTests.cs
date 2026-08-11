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
		if(!Global.IsTestingEnabled)
			return;

		Assert.SkipUnless(KafkaTestUtility.IsAvailable(), KAFKA_UNAVAILABLE);

		var identity = Guid.NewGuid().ToString("N");
		var topic = $"tests-basic-{identity}";
		var group = $"tests-subscriber-{identity}";
		var before = await KafkaTestUtility.GetResourcesAsync(topic, [group]);
		Assert.False(before.TopicExists);
		Assert.Empty(before.Groups);

		var publisher = KafkaTestUtility.CreateQueue($"publisher-{identity}", $"tests-unused-publisher-{identity}");
		var subscriber = KafkaTestUtility.CreateQueue($"subscriber-{identity}", group);
		using var messages = new KafkaMessageBuffer();
		KafkaSubscriber consumer = null;
		KafkaBrokerResources restored = default;

		try
		{
			await KafkaTestUtility.CreateTopicAsync(topic);
			consumer = await subscriber.SubscribeAsync(topic, messages);
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
		finally
		{
			if(consumer != null)
				await consumer.DisposeAsync();
			subscriber.Dispose();
			publisher.Dispose();
			restored = await KafkaTestUtility.DeleteResourcesAsync(topic, [group], TimeSpan.FromSeconds(45));
		}

		AssertReleased(publisher, subscriber, consumer);
		Assert.False(restored.TopicExists);
		Assert.Empty(restored.Groups);
	}

	[Fact]
	public async Task UnsubscribeStopsMessageDelivery()
	{
		if(!Global.IsTestingEnabled)
			return;

		Assert.SkipUnless(KafkaTestUtility.IsAvailable(), KAFKA_UNAVAILABLE);

		var identity = Guid.NewGuid().ToString("N");
		var topic = $"tests-unsubscribe-{identity}";
		var group = $"tests-subscriber-{identity}";
		var before = await KafkaTestUtility.GetResourcesAsync(topic, [group]);
		Assert.False(before.TopicExists);
		Assert.Empty(before.Groups);

		var publisher = KafkaTestUtility.CreateQueue($"publisher-{identity}", $"tests-unused-publisher-{identity}");
		var subscriber = KafkaTestUtility.CreateQueue($"subscriber-{identity}", group);
		using var messages = new KafkaMessageBuffer();
		KafkaSubscriber consumer = null;
		KafkaBrokerResources restored = default;

		try
		{
			await KafkaTestUtility.CreateTopicAsync(topic);
			consumer = await subscriber.SubscribeAsync(topic, messages);
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
		finally
		{
			if(consumer != null)
				await consumer.DisposeAsync();
			subscriber.Dispose();
			publisher.Dispose();
			restored = await KafkaTestUtility.DeleteResourcesAsync(topic, [group], TimeSpan.FromSeconds(45));
		}

		AssertReleased(publisher, subscriber, consumer);
		Assert.False(restored.TopicExists);
		Assert.Empty(restored.Groups);
	}

	private static void AssertReleased(KafkaQueue publisher, KafkaQueue subscriber, KafkaSubscriber consumer)
	{
		Assert.True(publisher.IsDisposed);
		Assert.True(subscriber.IsDisposed);
		Assert.Empty(publisher.Subscribers);
		Assert.Empty(subscriber.Subscribers);
		if(consumer != null)
		{
			Assert.True(consumer.IsClosed);
			Assert.True(consumer.IsDisposed);
			Assert.Null(consumer.Handler);
		}
	}
}
