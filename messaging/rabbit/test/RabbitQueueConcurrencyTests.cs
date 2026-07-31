using System;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using RabbitMQ.Client;

using Xunit;

namespace Zongsoft.Messaging.RabbitMQ.Tests;

public class RabbitQueueConcurrencyTests
{
	private const string RABBIT_UNAVAILABLE = "RabbitMQ is unavailable at localhost:5672 for program/xxxxxx. Start the RabbitMQ test Pod.";
	private static readonly TimeSpan TEST_TIMEOUT = TimeSpan.FromSeconds(90);
	private static readonly TimeSpan RESTORE_TIMEOUT = TimeSpan.FromSeconds(60);

	[Fact]
	public async Task MultipleClientsReceiveEveryConcurrentPublication()
	{
		if(!Global.IsTestingEnabled)
			return;

		const int CLIENT_COUNT = 6;
		const int MESSAGES_PER_CLIENT = 24;
		const int EXPECTED_MESSAGES_PER_SUBSCRIBER = CLIENT_COUNT * MESSAGES_PER_CLIENT;
		const int EXPECTED_DELIVERIES = CLIENT_COUNT * EXPECTED_MESSAGES_PER_SUBSCRIBER;
		const double MINIMUM_DELIVERIES_PER_SECOND = 5;

		Assert.SkipUnless(await RabbitTestUtility.IsAvailableAsync(), RABBIT_UNAVAILABLE);

		var baseline = await RabbitTestUtility.GetBrokerSnapshotAsync();
		var identity = Guid.NewGuid().ToString("N");
		var exchange = $"tests.exchange.concurrent.{identity}";
		var topic = $"tests/concurrent/{identity}";
		var queues = Enumerable.Range(0, CLIENT_COUNT)
			.Select(index => RabbitTestUtility.CreateQueue($"concurrent-client-{index}-{identity}", exchange, null))
			.ToArray();
		var audits = Enumerable.Range(0, CLIENT_COUNT).Select(_ => new RabbitMessageAudit()).ToArray();
		var subscribers = Array.Empty<RabbitSubscriber>();
		var subscriberChannels = Array.Empty<IChannel>();
		var publishingChannels = Array.Empty<IChannel>();
		var connections = Array.Empty<IConnection>();
		var expectedPayloads = CreatePayloads("fanout", CLIENT_COUNT, MESSAGES_PER_CLIENT);
		var stopwatch = new Stopwatch();

		try
		{
			subscribers = await Task.WhenAll(queues.Select((queue, index) => queue.SubscribeAsync(topic, audits[index]).AsTask()));
			subscriberChannels = subscribers.Select(subscriber => subscriber.Channel).ToArray();
			Assert.All(queues, queue => Assert.Single(queue.Subscribers));
			Assert.All(subscribers, subscriber => Assert.False(subscriber.IsClosed));

			stopwatch.Start();
			await Task.WhenAll(queues.Select((queue, publisher) => PublishBatchAsync(queue, topic, "fanout", publisher, MESSAGES_PER_CLIENT)));
			var completed = await Task.WhenAll(audits.Select(audit => audit.WaitForCountAsync(EXPECTED_MESSAGES_PER_SUBSCRIBER, TEST_TIMEOUT)));
			Assert.All(completed, received => Assert.True(received,
				$"A subscriber did not receive {EXPECTED_MESSAGES_PER_SUBSCRIBER} messages within {TEST_TIMEOUT}."));
			stopwatch.Stop();

			foreach(var audit in audits)
			{
				Assert.Equal(EXPECTED_MESSAGES_PER_SUBSCRIBER, audit.Count);
				Assert.Equal(EXPECTED_MESSAGES_PER_SUBSCRIBER, audit.AcknowledgementCount);
				Assert.Equal(0, audit.DuplicateCount);
				Assert.Equal(0, audit.InvalidIdentifierCount);
				Assert.Equal(expectedPayloads, audit.Payloads);
			}

			var throughput = EXPECTED_DELIVERIES / stopwatch.Elapsed.TotalSeconds;
			Assert.InRange(stopwatch.Elapsed, TimeSpan.Zero, TEST_TIMEOUT);
			Assert.True(throughput >= MINIMUM_DELIVERIES_PER_SECOND,
				$"Fan-out throughput {throughput:F2} deliveries/s was below {MINIMUM_DELIVERIES_PER_SECOND:F2}; {EXPECTED_DELIVERIES} deliveries took {stopwatch.Elapsed}.");

			publishingChannels = queues.Select(RabbitTestUtility.GetPublishingChannel).ToArray();
			connections = queues.Select(RabbitTestUtility.GetConnection).ToArray();
			Assert.All(publishingChannels, channel => Assert.True(channel?.IsOpen));
			Assert.All(connections, connection => Assert.True(connection?.IsOpen));
		}
		finally
		{
			await CleanupClientsAsync(queues, subscribers, audits);
			await RabbitTestUtility.DeleteTestExchangeAsync(exchange);
		}

		Assert.All(queues, queue =>
		{
			Assert.True(queue.IsDisposed);
			Assert.Empty(queue.Subscribers);
		});
		Assert.All(subscribers, subscriber =>
		{
			Assert.True(subscriber.IsClosed);
			Assert.True(subscriber.IsDisposed);
			Assert.Null(subscriber.Channel);
			Assert.Null(subscriber.Handler);
		});
		Assert.All(subscriberChannels, channel => Assert.True(channel.IsClosed));
		Assert.All(publishingChannels, channel => Assert.True(channel.IsClosed));
		Assert.All(connections, connection => Assert.False(connection.IsOpen));
		await AssertBrokerRestoredAsync(baseline);
	}

	[Fact]
	public async Task ConcurrentFirstProduceInitializesSingleQueue()
	{
		if(!Global.IsTestingEnabled)
			return;

		const int PUBLICATION_COUNT = 64;
		const double MINIMUM_PUBLICATIONS_PER_SECOND = 1;

		Assert.SkipUnless(await RabbitTestUtility.IsAvailableAsync(), RABBIT_UNAVAILABLE);

		var baseline = await RabbitTestUtility.GetBrokerSnapshotAsync();
		var identity = Guid.NewGuid().ToString("N");
		var topic = $"tests/initialize/{identity}";
		var exchange = $"tests.exchange.initialize.{identity}";
		var queue = RabbitTestUtility.CreateQueue($"initialize-client-{identity}", exchange, null);
		var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		IChannel publishingChannel = null;
		IConnection connection = null;
		var stopwatch = new Stopwatch();

		try
		{
			var tasks = Enumerable.Range(0, PUBLICATION_COUNT).Select(async index =>
			{
				await gate.Task;
				return await queue.ProduceAsync(topic, Encoding.UTF8.GetBytes($"initialize:{index:D4}"));
			}).ToArray();

			stopwatch.Start();
			gate.SetResult();
			var identifiers = await Task.WhenAll(tasks).WaitAsync(TEST_TIMEOUT);
			stopwatch.Stop();

			Assert.Equal(PUBLICATION_COUNT, identifiers.Length);
			Assert.Equal(PUBLICATION_COUNT, identifiers.Distinct(StringComparer.Ordinal).Count());
			Assert.All(identifiers, identifier => Assert.Equal(12, identifier.Length));
			Assert.Empty(queue.Subscribers);

			var throughput = PUBLICATION_COUNT / stopwatch.Elapsed.TotalSeconds;
			Assert.InRange(stopwatch.Elapsed, TimeSpan.Zero, TEST_TIMEOUT);
			Assert.True(throughput >= MINIMUM_PUBLICATIONS_PER_SECOND,
				$"Concurrent first-use throughput {throughput:F2} publications/s was below {MINIMUM_PUBLICATIONS_PER_SECOND:F2}; {PUBLICATION_COUNT} publications took {stopwatch.Elapsed}.");

			publishingChannel = RabbitTestUtility.GetPublishingChannel(queue);
			connection = RabbitTestUtility.GetConnection(queue);
			Assert.True(publishingChannel?.IsOpen);
			Assert.True(connection?.IsOpen);
		}
		finally
		{
			queue.Dispose();
			await RabbitTestUtility.DeleteTestExchangeAsync(exchange);
		}

		Assert.True(queue.IsDisposed);
		Assert.Empty(queue.Subscribers);
		Assert.True(publishingChannel.IsClosed);
		Assert.False(connection.IsOpen);
		await AssertBrokerRestoredAsync(baseline);
	}

	[Fact]
	public async Task UnsubscribingDuringConcurrentPublishingDoesNotInterruptOtherClients()
	{
		if(!Global.IsTestingEnabled)
			return;

		const int CLIENT_COUNT = 4;
		const int FIRST_WAVE_PER_CLIENT = 12;
		const int SECOND_WAVE_PER_CLIENT = 36;
		const int FIRST_WAVE_TOTAL = CLIENT_COUNT * FIRST_WAVE_PER_CLIENT;
		const int ALL_WAVES_TOTAL = CLIENT_COUNT * (FIRST_WAVE_PER_CLIENT + SECOND_WAVE_PER_CLIENT);
		const int FINAL_TOTAL = ALL_WAVES_TOTAL + 1;
		const double MINIMUM_DELIVERIES_PER_SECOND = 3;

		Assert.SkipUnless(await RabbitTestUtility.IsAvailableAsync(), RABBIT_UNAVAILABLE);

		var baseline = await RabbitTestUtility.GetBrokerSnapshotAsync();
		var identity = Guid.NewGuid().ToString("N");
		var exchange = $"tests.exchange.unsubscribe.{identity}";
		var topic = $"tests/unsubscribe/concurrent/{identity}";
		var queues = Enumerable.Range(0, CLIENT_COUNT)
			.Select(index => RabbitTestUtility.CreateQueue($"unsubscribe-client-{index}-{identity}", exchange, null))
			.ToArray();
		var audits = Enumerable.Range(0, CLIENT_COUNT).Select(_ => new RabbitMessageAudit()).ToArray();
		var subscribers = Array.Empty<RabbitSubscriber>();
		var subscriberChannels = Array.Empty<IChannel>();
		var publishingChannels = Array.Empty<IChannel>();
		var connections = Array.Empty<IConnection>();
		var stopwatch = new Stopwatch();

		try
		{
			subscribers = await Task.WhenAll(queues.Select((queue, index) => queue.SubscribeAsync(topic, audits[index]).AsTask()));
			subscriberChannels = subscribers.Select(subscriber => subscriber.Channel).ToArray();

			await Task.WhenAll(queues.Select((queue, publisher) => PublishBatchAsync(queue, topic, "before", publisher, FIRST_WAVE_PER_CLIENT)));
			var firstWaveCompleted = await Task.WhenAll(audits.Select(audit => audit.WaitForCountAsync(FIRST_WAVE_TOTAL, TEST_TIMEOUT)));
			Assert.All(firstWaveCompleted, received => Assert.True(received,
				$"A subscriber did not receive the complete {FIRST_WAVE_TOTAL}-message first wave."));

			stopwatch.Start();
			var secondWave = Task.WhenAll(queues.Select((queue, publisher) => PublishBatchAsync(queue, topic, "during", publisher, SECOND_WAVE_PER_CLIENT)));
			var unsubscribe = subscribers[0].UnsubscribeAsync().AsTask();
			await Task.WhenAll(secondWave, unsubscribe).WaitAsync(TEST_TIMEOUT);

			for(var index = 1; index < CLIENT_COUNT; index++)
				Assert.True(await audits[index].WaitForCountAsync(ALL_WAVES_TOTAL, TEST_TIMEOUT),
					$"Subscriber {index} did not receive all {ALL_WAVES_TOTAL} messages while subscriber 0 unsubscribed.");

			await Task.Delay(TimeSpan.FromMilliseconds(250));
			var unsubscribedCount = audits[0].Count;
			var unsubscribedAcknowledgements = audits[0].AcknowledgementCount;
			Assert.InRange(unsubscribedCount, FIRST_WAVE_TOTAL, ALL_WAVES_TOTAL);
			Assert.Equal(unsubscribedCount, unsubscribedAcknowledgements);
			Assert.Empty(queues[0].Subscribers);

			await queues[1].ProduceAsync(topic, Encoding.UTF8.GetBytes("after-unsubscribe:probe"));
			for(var index = 1; index < CLIENT_COUNT; index++)
				Assert.True(await audits[index].WaitForCountAsync(FINAL_TOTAL, TEST_TIMEOUT),
					$"Subscriber {index} did not receive the post-unsubscribe probe.");
			await Task.Delay(TimeSpan.FromSeconds(1));
			stopwatch.Stop();

			Assert.Equal(unsubscribedCount, audits[0].Count);
			Assert.Equal(unsubscribedAcknowledgements, audits[0].AcknowledgementCount);
			Assert.Equal(0, audits[0].DuplicateCount);
			Assert.Equal(0, audits[0].InvalidIdentifierCount);

			var expectedPayloads = CreatePayloads("before", CLIENT_COUNT, FIRST_WAVE_PER_CLIENT)
				.Concat(CreatePayloads("during", CLIENT_COUNT, SECOND_WAVE_PER_CLIENT))
				.Append("after-unsubscribe:probe")
				.OrderBy(payload => payload, StringComparer.Ordinal)
				.ToArray();
			for(var index = 1; index < CLIENT_COUNT; index++)
			{
				Assert.Equal(FINAL_TOTAL, audits[index].Count);
				Assert.Equal(FINAL_TOTAL, audits[index].AcknowledgementCount);
				Assert.Equal(0, audits[index].DuplicateCount);
				Assert.Equal(0, audits[index].InvalidIdentifierCount);
				Assert.Equal(expectedPayloads, audits[index].Payloads);
			}

			var unaffectedDeliveries = (CLIENT_COUNT - 1) * FINAL_TOTAL;
			var throughput = unaffectedDeliveries / stopwatch.Elapsed.TotalSeconds;
			Assert.InRange(stopwatch.Elapsed, TimeSpan.Zero, TEST_TIMEOUT);
			Assert.True(throughput >= MINIMUM_DELIVERIES_PER_SECOND,
				$"Unsubscribe-concurrency throughput {throughput:F2} unaffected deliveries/s was below {MINIMUM_DELIVERIES_PER_SECOND:F2}; {unaffectedDeliveries} verified deliveries took {stopwatch.Elapsed}.");

			publishingChannels = queues.Select(RabbitTestUtility.GetPublishingChannel).ToArray();
			connections = queues.Select(RabbitTestUtility.GetConnection).ToArray();
			Assert.All(publishingChannels, channel => Assert.True(channel?.IsOpen));
			Assert.All(connections, connection => Assert.True(connection?.IsOpen));
		}
		finally
		{
			await CleanupClientsAsync(queues, subscribers, audits);
			await RabbitTestUtility.DeleteTestExchangeAsync(exchange);
		}

		Assert.All(queues, queue =>
		{
			Assert.True(queue.IsDisposed);
			Assert.Empty(queue.Subscribers);
		});
		Assert.All(subscribers, subscriber =>
		{
			Assert.True(subscriber.IsClosed);
			Assert.True(subscriber.IsDisposed);
			Assert.Null(subscriber.Channel);
			Assert.Null(subscriber.Handler);
		});
		Assert.All(subscriberChannels, channel => Assert.True(channel.IsClosed));
		Assert.All(publishingChannels, channel => Assert.True(channel.IsClosed));
		Assert.All(connections, connection => Assert.False(connection.IsOpen));
		await AssertBrokerRestoredAsync(baseline);
	}

	private static async Task PublishBatchAsync(RabbitQueue queue, string topic, string prefix, int publisher, int count)
	{
		for(var index = 0; index < count; index++)
			await queue.ProduceAsync(topic, Encoding.UTF8.GetBytes($"{prefix}:{publisher:D2}:{index:D4}"));
	}

	private static string[] CreatePayloads(string prefix, int publisherCount, int messageCount) =>
		Enumerable.Range(0, publisherCount)
			.SelectMany(publisher => Enumerable.Range(0, messageCount)
				.Select(index => $"{prefix}:{publisher:D2}:{index:D4}"))
			.OrderBy(payload => payload, StringComparer.Ordinal)
			.ToArray();

	private static async Task CleanupClientsAsync(RabbitQueue[] queues, RabbitSubscriber[] subscribers, RabbitMessageAudit[] audits)
	{
		try
		{
			await Task.WhenAll(subscribers.Select(subscriber => subscriber.DisposeAsync().AsTask()));
		}
		finally
		{
			foreach(var queue in queues)
				queue.Dispose();
			foreach(var audit in audits)
				audit.Dispose();
		}
	}

	private static async Task AssertBrokerRestoredAsync(RabbitBrokerSnapshot baseline)
	{
		var restored = await RabbitTestUtility.WaitForBrokerRestoreAsync(baseline, RESTORE_TIMEOUT);
		Assert.True(restored.IsAtMost(baseline),
			$"RabbitMQ resources did not return to baseline within {RESTORE_TIMEOUT}. Baseline={baseline}; Current={restored}.");
	}
}
