using System;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;

using Confluent.Kafka;

using Xunit;

namespace Zongsoft.Messaging.Kafka.Tests;

public class KafkaQueueConcurrencyTests
{
	private const string KAFKA_UNAVAILABLE = "Kafka is unavailable at localhost:9092. Start test/Zongsoft.Messaging.Kafka-pod.yaml with Podman.";
	private static readonly TimeSpan TEST_TIMEOUT = TimeSpan.FromSeconds(120);
	private static readonly TimeSpan CLEANUP_TIMEOUT = TimeSpan.FromSeconds(45);

	[Fact]
	public async Task IndependentConsumerGroupsReceiveEveryConcurrentPublication()
	{
		const int PUBLISHER_COUNT = 6;
		const int GROUP_COUNT = 3;
		const int MESSAGES_PER_PUBLISHER = 24;
		const int MESSAGE_COUNT = PUBLISHER_COUNT * MESSAGES_PER_PUBLISHER;
		const int DELIVERY_COUNT = GROUP_COUNT * MESSAGE_COUNT;
		const double MINIMUM_DELIVERIES_PER_SECOND = 3;

		Assert.SkipUnless(KafkaTestUtility.IsAvailable(), KAFKA_UNAVAILABLE);

		var identity = Guid.NewGuid().ToString("N");
		var topic = $"tests-concurrent-{identity}";
		var groups = Enumerable.Range(0, GROUP_COUNT).Select(index => $"tests-concurrent-group-{index}-{identity}").ToArray();
		var before = await KafkaTestUtility.GetResourcesAsync(topic, groups);
		Assert.False(before.TopicExists);
		Assert.Empty(before.Groups);

		var publishers = Enumerable.Range(0, PUBLISHER_COUNT)
			.Select(index => KafkaTestUtility.CreateQueue($"concurrent-publisher-{index}-{identity}", $"tests-unused-publisher-{index}-{identity}"))
			.ToArray();
		var subscribers = groups.Select((group, index) => KafkaTestUtility.CreateQueue($"concurrent-subscriber-{index}-{identity}", group)).ToArray();
		var audits = Enumerable.Range(0, GROUP_COUNT).Select(_ => new KafkaMessageAudit("fanout:")).ToArray();
		var consumers = Array.Empty<KafkaSubscriber>();
		var expectedPayloads = CreatePayloads("fanout", PUBLISHER_COUNT, MESSAGES_PER_PUBLISHER);
		var stopwatch = new Stopwatch();
		KafkaBrokerResources restored = default;

		try
		{
			await KafkaTestUtility.CreateTopicAsync(topic, PUBLISHER_COUNT);
			consumers = await Task.WhenAll(subscribers.Select((queue, index) => queue.SubscribeAsync(topic, audits[index]).AsTask()));
			await KafkaTestUtility.WarmUpGroupsAsync(publishers[0], topic, audits, TEST_TIMEOUT);

			stopwatch.Start();
			var identifiers = (await Task.WhenAll(publishers.Select((queue, publisher) =>
				PublishBatchAsync(queue, topic, "fanout", publisher, MESSAGES_PER_PUBLISHER)))).SelectMany(values => values).ToArray();
			var completed = await Task.WhenAll(audits.Select(audit => audit.WaitForCountAsync(MESSAGE_COUNT, TEST_TIMEOUT)));
			stopwatch.Stop();

			Assert.All(completed, received => Assert.True(received, $"A consumer group did not receive all {MESSAGE_COUNT} messages."));
			Assert.Equal(MESSAGE_COUNT, identifiers.Length);
			Assert.All(identifiers, identifier => Assert.Contains(identifier, ValidPartitions(topic, PUBLISHER_COUNT)));

			foreach(var audit in audits)
			{
				Assert.Equal(MESSAGE_COUNT, audit.Count);
				Assert.Equal(MESSAGE_COUNT, audit.AcknowledgementCount);
				Assert.Equal(0, audit.DuplicateCount);
				Assert.Equal(expectedPayloads, audit.Payloads);
			}

			var throughput = DELIVERY_COUNT / stopwatch.Elapsed.TotalSeconds;
			Assert.InRange(stopwatch.Elapsed, TimeSpan.Zero, TEST_TIMEOUT);
			Assert.True(throughput >= MINIMUM_DELIVERIES_PER_SECOND,
				$"Kafka group throughput {throughput:F2} deliveries/s was below {MINIMUM_DELIVERIES_PER_SECOND:F2}; {DELIVERY_COUNT} committed deliveries took {stopwatch.Elapsed}.");
		}
		finally
		{
			await CleanupAsync(publishers.Concat(subscribers).ToArray(), consumers, audits);
			restored = await KafkaTestUtility.DeleteResourcesAsync(topic, groups, CLEANUP_TIMEOUT);
		}

		AssertReleased(publishers.Concat(subscribers).ToArray(), consumers);
		Assert.False(restored.TopicExists);
		Assert.Empty(restored.Groups);
	}

	[Fact]
	public async Task ConcurrentProduceOnSingleQueueIsThreadSafe()
	{
		const int PUBLICATION_COUNT = 64;
		const int PARTITION_COUNT = 6;
		const double MINIMUM_PUBLICATIONS_PER_SECOND = 1;

		Assert.SkipUnless(KafkaTestUtility.IsAvailable(), KAFKA_UNAVAILABLE);

		var identity = Guid.NewGuid().ToString("N");
		var topic = $"tests-initialize-{identity}";
		var group = $"tests-initialize-group-{identity}";
		var before = await KafkaTestUtility.GetResourcesAsync(topic, [group]);
		Assert.False(before.TopicExists);
		Assert.Empty(before.Groups);

		var publisher = KafkaTestUtility.CreateQueue($"initialize-publisher-{identity}", $"tests-unused-publisher-{identity}");
		var subscriber = KafkaTestUtility.CreateQueue($"initialize-subscriber-{identity}", group);
		var audit = new KafkaMessageAudit("initialize:");
		KafkaSubscriber consumer = null;
		var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var stopwatch = new Stopwatch();
		KafkaBrokerResources restored = default;

		try
		{
			await KafkaTestUtility.CreateTopicAsync(topic, PARTITION_COUNT);
			consumer = await subscriber.SubscribeAsync(topic, audit);
			await KafkaTestUtility.WarmUpGroupsAsync(publisher, topic, [audit], TEST_TIMEOUT);

			var tasks = Enumerable.Range(0, PUBLICATION_COUNT).Select(async index =>
			{
				await gate.Task;
				return await publisher.ProduceAsync(topic, Encoding.UTF8.GetBytes($"initialize:{index:D4}"));
			}).ToArray();

			stopwatch.Start();
			gate.SetResult();
			var identifiers = await Task.WhenAll(tasks).WaitAsync(TEST_TIMEOUT);
			Assert.True(await audit.WaitForCountAsync(PUBLICATION_COUNT, TEST_TIMEOUT),
				$"The independent consumer group did not receive all {PUBLICATION_COUNT} concurrent publications.");
			stopwatch.Stop();

			Assert.Equal(PUBLICATION_COUNT, identifiers.Length);
			Assert.All(identifiers, identifier => Assert.Contains(identifier, ValidPartitions(topic, PARTITION_COUNT)));
			Assert.Equal(PUBLICATION_COUNT, audit.Count);
			Assert.Equal(PUBLICATION_COUNT, audit.AcknowledgementCount);
			Assert.Equal(0, audit.DuplicateCount);
			Assert.Equal(CreatePayloads("initialize", 1, PUBLICATION_COUNT, false), audit.Payloads);

			var throughput = PUBLICATION_COUNT / stopwatch.Elapsed.TotalSeconds;
			Assert.InRange(stopwatch.Elapsed, TimeSpan.Zero, TEST_TIMEOUT);
			Assert.True(throughput >= MINIMUM_PUBLICATIONS_PER_SECOND,
				$"Kafka concurrent producer throughput {throughput:F2} publications/s was below {MINIMUM_PUBLICATIONS_PER_SECOND:F2}; {PUBLICATION_COUNT} published-and-committed payloads took {stopwatch.Elapsed}.");
		}
		finally
		{
			await CleanupAsync([publisher, subscriber], consumer == null ? [] : [consumer], [audit]);
			restored = await KafkaTestUtility.DeleteResourcesAsync(topic, [group], CLEANUP_TIMEOUT);
		}

		AssertReleased([publisher, subscriber], consumer == null ? [] : [consumer]);
		Assert.False(restored.TopicExists);
		Assert.Empty(restored.Groups);
	}

	[Fact]
	public async Task UnsubscribingOneGroupDuringConcurrentPublishingDoesNotInterruptOtherGroups()
	{
		const int PUBLISHER_COUNT = 4;
		const int GROUP_COUNT = 3;
		const int FIRST_WAVE_PER_PUBLISHER = 12;
		const int SECOND_WAVE_PER_PUBLISHER = 36;
		const int FIRST_WAVE_TOTAL = PUBLISHER_COUNT * FIRST_WAVE_PER_PUBLISHER;
		const int ALL_WAVES_TOTAL = PUBLISHER_COUNT * (FIRST_WAVE_PER_PUBLISHER + SECOND_WAVE_PER_PUBLISHER);
		const int FINAL_TOTAL = ALL_WAVES_TOTAL + 1;
		const double MINIMUM_DELIVERIES_PER_SECOND = 3;

		Assert.SkipUnless(KafkaTestUtility.IsAvailable(), KAFKA_UNAVAILABLE);

		var identity = Guid.NewGuid().ToString("N");
		var topic = $"tests-unsubscribe-concurrent-{identity}";
		var groups = Enumerable.Range(0, GROUP_COUNT).Select(index => $"tests-unsubscribe-group-{index}-{identity}").ToArray();
		var before = await KafkaTestUtility.GetResourcesAsync(topic, groups);
		Assert.False(before.TopicExists);
		Assert.Empty(before.Groups);

		var publishers = Enumerable.Range(0, PUBLISHER_COUNT)
			.Select(index => KafkaTestUtility.CreateQueue($"unsubscribe-publisher-{index}-{identity}", $"tests-unused-publisher-{index}-{identity}"))
			.ToArray();
		var subscribers = groups.Select((group, index) => KafkaTestUtility.CreateQueue($"unsubscribe-subscriber-{index}-{identity}", group)).ToArray();
		var audits = Enumerable.Range(0, GROUP_COUNT).Select(_ => new KafkaMessageAudit("flow:")).ToArray();
		var consumers = Array.Empty<KafkaSubscriber>();
		var stopwatch = new Stopwatch();
		KafkaBrokerResources restored = default;

		try
		{
			await KafkaTestUtility.CreateTopicAsync(topic, PUBLISHER_COUNT);
			consumers = await Task.WhenAll(subscribers.Select((queue, index) => queue.SubscribeAsync(topic, audits[index]).AsTask()));
			await KafkaTestUtility.WarmUpGroupsAsync(publishers[0], topic, audits, TEST_TIMEOUT);

			await Task.WhenAll(publishers.Select((queue, publisher) =>
				PublishBatchAsync(queue, topic, "flow:before", publisher, FIRST_WAVE_PER_PUBLISHER)));
			var firstWave = await Task.WhenAll(audits.Select(audit => audit.WaitForCountAsync(FIRST_WAVE_TOTAL, TEST_TIMEOUT)));
			Assert.All(firstWave, received => Assert.True(received, $"A group did not receive the complete {FIRST_WAVE_TOTAL}-message first wave."));

			stopwatch.Start();
			var secondWave = Task.WhenAll(publishers.Select((queue, publisher) =>
				PublishBatchAsync(queue, topic, "flow:during", publisher, SECOND_WAVE_PER_PUBLISHER)));
			var unsubscribe = consumers[0].UnsubscribeAsync().AsTask();
			await Task.WhenAll(secondWave, unsubscribe).WaitAsync(TEST_TIMEOUT);

			for(var index = 1; index < GROUP_COUNT; index++)
				Assert.True(await audits[index].WaitForCountAsync(ALL_WAVES_TOTAL, TEST_TIMEOUT),
					$"Consumer group {index} did not receive all {ALL_WAVES_TOTAL} messages.");

			await Task.Delay(TimeSpan.FromMilliseconds(500));
			var stoppedCount = audits[0].Count;
			var stoppedAcknowledgements = audits[0].AcknowledgementCount;
			Assert.InRange(stoppedCount, FIRST_WAVE_TOTAL, ALL_WAVES_TOTAL);
			Assert.Equal(stoppedCount, stoppedAcknowledgements);
			Assert.Empty(subscribers[0].Subscribers);

			await publishers[1].ProduceAsync(topic, Encoding.UTF8.GetBytes("flow:after:probe"));
			for(var index = 1; index < GROUP_COUNT; index++)
				Assert.True(await audits[index].WaitForCountAsync(FINAL_TOTAL, TEST_TIMEOUT),
					$"Consumer group {index} did not receive the post-unsubscribe probe.");
			await Task.Delay(TimeSpan.FromSeconds(1));
			stopwatch.Stop();

			Assert.Equal(stoppedCount, audits[0].Count);
			Assert.Equal(stoppedAcknowledgements, audits[0].AcknowledgementCount);
			Assert.Equal(0, audits[0].DuplicateCount);

			var expectedPayloads = CreatePayloads("flow:before", PUBLISHER_COUNT, FIRST_WAVE_PER_PUBLISHER)
				.Concat(CreatePayloads("flow:during", PUBLISHER_COUNT, SECOND_WAVE_PER_PUBLISHER))
				.Append("flow:after:probe")
				.OrderBy(payload => payload, StringComparer.Ordinal)
				.ToArray();
			for(var index = 1; index < GROUP_COUNT; index++)
			{
				Assert.Equal(FINAL_TOTAL, audits[index].Count);
				Assert.Equal(FINAL_TOTAL, audits[index].AcknowledgementCount);
				Assert.Equal(0, audits[index].DuplicateCount);
				Assert.Equal(expectedPayloads, audits[index].Payloads);
			}

			var unaffectedDeliveries = (GROUP_COUNT - 1) * FINAL_TOTAL;
			var throughput = unaffectedDeliveries / stopwatch.Elapsed.TotalSeconds;
			Assert.InRange(stopwatch.Elapsed, TimeSpan.Zero, TEST_TIMEOUT);
			Assert.True(throughput >= MINIMUM_DELIVERIES_PER_SECOND,
				$"Kafka unsubscribe throughput {throughput:F2} unaffected deliveries/s was below {MINIMUM_DELIVERIES_PER_SECOND:F2}; {unaffectedDeliveries} committed deliveries took {stopwatch.Elapsed}.");
		}
		finally
		{
			await CleanupAsync(publishers.Concat(subscribers).ToArray(), consumers, audits);
			restored = await KafkaTestUtility.DeleteResourcesAsync(topic, groups, CLEANUP_TIMEOUT);
		}

		AssertReleased(publishers.Concat(subscribers).ToArray(), consumers);
		Assert.False(restored.TopicExists);
		Assert.Empty(restored.Groups);
	}

	private static async Task<string[]> PublishBatchAsync(KafkaQueue queue, string topic, string prefix, int publisher, int count)
	{
		var identifiers = new string[count];

		for(var index = 0; index < count; index++)
			identifiers[index] = await queue.ProduceAsync(topic, Encoding.UTF8.GetBytes($"{prefix}:{publisher:D2}:{index:D4}"));

		return identifiers;
	}

	private static string[] CreatePayloads(string prefix, int publisherCount, int messageCount, bool includePublisher = true) =>
		Enumerable.Range(0, publisherCount)
			.SelectMany(publisher => Enumerable.Range(0, messageCount)
				.Select(index => includePublisher ? $"{prefix}:{publisher:D2}:{index:D4}" : $"{prefix}:{index:D4}"))
			.OrderBy(payload => payload, StringComparer.Ordinal)
			.ToArray();

	private static string[] ValidPartitions(string topic, int count) =>
		Enumerable.Range(0, count).Select(index => new TopicPartition(topic, new Partition(index)).ToString()).ToArray();

	private static async Task CleanupAsync(KafkaQueue[] queues, KafkaSubscriber[] consumers, KafkaMessageAudit[] audits)
	{
		try
		{
			await Task.WhenAll(consumers.Select(consumer => consumer.DisposeAsync().AsTask()));
		}
		finally
		{
			foreach(var queue in queues)
				queue.Dispose();
			foreach(var audit in audits)
				audit.Dispose();
		}
	}

	private static void AssertReleased(KafkaQueue[] queues, KafkaSubscriber[] consumers)
	{
		Assert.All(queues, queue =>
		{
			Assert.True(queue.IsDisposed);
			Assert.Empty(queue.Subscribers);
			Assert.True(KafkaTestUtility.IsQueueTransportReleased(queue));
		});
		Assert.All(consumers, consumer =>
		{
			Assert.True(consumer.IsClosed);
			Assert.True(consumer.IsDisposed);
			Assert.Null(consumer.Handler);
			Assert.True(KafkaTestUtility.IsSubscriberTransportReleased(consumer));
		});
	}
}
