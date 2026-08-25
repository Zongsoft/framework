using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

using Xunit;

using Zongsoft.Collections;
using Zongsoft.Components;

using Enumerable = System.Linq.Enumerable;

namespace Zongsoft.Messaging.ZeroMQ.Tests;

public class ZeroQueueConcurrencyTests
{
	#region 常量定义
	private static readonly TimeSpan TEST_TIMEOUT = TimeSpan.FromSeconds(20);
	#endregion

	[Fact]
	public async Task MultipleClientsReceiveEveryConcurrentPublication()
	{
		if(!Global.IsTestingEnabled)
			return;

		const int PUBLISHER_COUNT = 4;
		const int SUBSCRIBER_COUNT = 3;
		const int MESSAGES_PER_PUBLISHER = 24;
		const int MESSAGE_COUNT = PUBLISHER_COUNT * MESSAGES_PER_PUBLISHER;
		const int DELIVERY_COUNT = SUBSCRIBER_COUNT * MESSAGE_COUNT;
		const double MINIMUM_DELIVERIES_PER_SECOND = 5;

		using var server = await ZeroServerScope.StartAsync();
		var identity = Guid.NewGuid().ToString("N");
		var topic = $"tests/concurrent/fanout/{identity}";
		var publishers = Enumerable.Range(0, PUBLISHER_COUNT)
			.Select(index => ZeroTestUtility.CreateQueue(server.Port, $"fanout-publisher-{index}-{identity}"))
			.ToArray();
		var subscribers = Enumerable.Range(0, SUBSCRIBER_COUNT)
			.Select(index => ZeroTestUtility.CreateQueue(server.Port, $"fanout-subscriber-{index}-{identity}"))
			.ToArray();
		var audits = Enumerable.Range(0, SUBSCRIBER_COUNT).Select(_ => new ZeroMessageAudit(topic)).ToArray();
		var consumers = Array.Empty<ZeroSubscriber>();
		var expectedPayloads = CreatePayloads("fanout", PUBLISHER_COUNT, MESSAGES_PER_PUBLISHER);
		var stopwatch = new Stopwatch();

		try
		{
			consumers = await Task.WhenAll(subscribers.Select((queue, index) => queue.SubscribeAsync(topic, audits[index]).AsTask()));
			await WarmupAsync(publishers, topic, audits);
			foreach(var audit in audits)
				audit.Reset();

			stopwatch.Start();
			await Task.WhenAll(publishers.Select((queue, publisher) =>
				PublishBatchAsync(queue, topic, "fanout", publisher, MESSAGES_PER_PUBLISHER)));
			var completed = await Task.WhenAll(audits.Select(audit => audit.WaitForCountAsync(MESSAGE_COUNT, TEST_TIMEOUT)));
			await Task.Delay(TimeSpan.FromMilliseconds(250));
			stopwatch.Stop();

			Assert.All(completed, received => Assert.True(received, $"A ZeroMQ subscriber did not receive all {MESSAGE_COUNT} messages."));

			foreach(var audit in audits)
			{
				Assert.Equal(MESSAGE_COUNT, audit.Count);
				Assert.Equal(0, audit.DuplicateCount);
				Assert.Equal(0, audit.InvalidTopicCount);
				Assert.Equal(expectedPayloads, audit.Payloads);
			}

			var throughput = DELIVERY_COUNT / stopwatch.Elapsed.TotalSeconds;
			Assert.InRange(stopwatch.Elapsed, TimeSpan.Zero, TEST_TIMEOUT);
			Assert.True(throughput >= MINIMUM_DELIVERIES_PER_SECOND,
				$"ZeroMQ fan-out throughput {throughput:F2} deliveries/s was below {MINIMUM_DELIVERIES_PER_SECOND:F2}; {DELIVERY_COUNT} deliveries took {stopwatch.Elapsed}.");
		}
		finally
		{
			await CleanupAsync(publishers, subscribers, consumers);
		}

		AssertReleased(publishers.Concat(subscribers).ToArray(), consumers);
	}

	[Fact]
	public async Task ConcurrentProduceOnSingleQueueIsThreadSafe()
	{
		if(!Global.IsTestingEnabled)
			return;

		const int PUBLICATION_COUNT = 64;
		const double MINIMUM_PUBLICATIONS_PER_SECOND = 1;

		using var server = await ZeroServerScope.StartAsync();
		var identity = Guid.NewGuid().ToString("N");
		var topic = $"tests/concurrent/initialize/{identity}";
		var publisher = ZeroTestUtility.CreateQueue(server.Port, $"initialize-publisher-{identity}");
		var subscriber = ZeroTestUtility.CreateQueue(server.Port, $"initialize-subscriber-{identity}");
		var audit = new ZeroMessageAudit(topic);
		ZeroSubscriber consumer = null;
		var stopwatch = new Stopwatch();

		try
		{
			consumer = await subscriber.SubscribeAsync(topic, audit);
			await WarmupAsync([publisher], topic, [audit]);
			audit.Reset();

			var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			var publications = Enumerable.Range(0, PUBLICATION_COUNT).Select(async index =>
			{
				await gate.Task;
				return await publisher.ProduceAsync(topic, Encoding.UTF8.GetBytes($"initialize:{index:D4}"));
			}).ToArray();

			stopwatch.Start();
			gate.SetResult();
			var identifiers = await Task.WhenAll(publications).WaitAsync(TEST_TIMEOUT);
			Assert.True(await audit.WaitForCountAsync(PUBLICATION_COUNT, TEST_TIMEOUT),
				$"The ZeroMQ subscriber did not receive all {PUBLICATION_COUNT} concurrent first-use publications.");
			await Task.Delay(TimeSpan.FromMilliseconds(250));
			stopwatch.Stop();

			Assert.Equal(PUBLICATION_COUNT, identifiers.Length);
			Assert.All(identifiers, identifier => Assert.False(string.IsNullOrWhiteSpace(identifier)));
			Assert.Equal(PUBLICATION_COUNT, identifiers.Distinct(StringComparer.Ordinal).Count());
			Assert.Equal(PUBLICATION_COUNT, audit.Count);
			Assert.Equal(0, audit.DuplicateCount);
			Assert.Equal(0, audit.InvalidTopicCount);
			Assert.Equal(CreatePayloads("initialize", 1, PUBLICATION_COUNT, false), audit.Payloads);

			var throughput = PUBLICATION_COUNT / stopwatch.Elapsed.TotalSeconds;
			Assert.InRange(stopwatch.Elapsed, TimeSpan.Zero, TEST_TIMEOUT);
			Assert.True(throughput >= MINIMUM_PUBLICATIONS_PER_SECOND,
				$"ZeroMQ concurrent producer throughput {throughput:F2} publications/s was below {MINIMUM_PUBLICATIONS_PER_SECOND:F2}; {PUBLICATION_COUNT} published-and-received payloads took {stopwatch.Elapsed}.");
		}
		finally
		{
			await CleanupAsync([publisher], [subscriber], consumer == null ? [] : [consumer]);
		}

		AssertReleased([publisher, subscriber], consumer == null ? [] : [consumer]);
	}

	[Fact]
	public async Task UnsubscribingDuringConcurrentPublishingDoesNotInterruptOtherClients()
	{
		if(!Global.IsTestingEnabled)
			return;

		const int PUBLISHER_COUNT = 3;
		const int SUBSCRIBER_COUNT = 3;
		const int FIRST_WAVE_PER_PUBLISHER = 8;
		const int SECOND_WAVE_PER_PUBLISHER = 24;
		const int FIRST_WAVE_TOTAL = PUBLISHER_COUNT * FIRST_WAVE_PER_PUBLISHER;
		const int ALL_WAVES_TOTAL = PUBLISHER_COUNT * (FIRST_WAVE_PER_PUBLISHER + SECOND_WAVE_PER_PUBLISHER);
		const int FINAL_TOTAL = ALL_WAVES_TOTAL + 1;
		const int TIMED_DELIVERY_COUNT = (SUBSCRIBER_COUNT - 1) * (PUBLISHER_COUNT * SECOND_WAVE_PER_PUBLISHER + 1);
		const double MINIMUM_DELIVERIES_PER_SECOND = 3;

		using var server = await ZeroServerScope.StartAsync();
		var identity = Guid.NewGuid().ToString("N");
		var topic = $"tests/concurrent/unsubscribe/{identity}";
		var publishers = Enumerable.Range(0, PUBLISHER_COUNT)
			.Select(index => ZeroTestUtility.CreateQueue(server.Port, $"unsubscribe-publisher-{index}-{identity}"))
			.ToArray();
		var subscribers = Enumerable.Range(0, SUBSCRIBER_COUNT)
			.Select(index => ZeroTestUtility.CreateQueue(server.Port, $"unsubscribe-subscriber-{index}-{identity}"))
			.ToArray();
		var audits = Enumerable.Range(0, SUBSCRIBER_COUNT).Select(_ => new ZeroMessageAudit(topic)).ToArray();
		var consumers = Array.Empty<ZeroSubscriber>();
		var stopwatch = new Stopwatch();

		try
		{
			consumers = await Task.WhenAll(subscribers.Select((queue, index) => queue.SubscribeAsync(topic, audits[index]).AsTask()));
			await WarmupAsync(publishers, topic, audits);
			foreach(var audit in audits)
				audit.Reset();

			await Task.WhenAll(publishers.Select((queue, publisher) =>
				PublishBatchAsync(queue, topic, "before", publisher, FIRST_WAVE_PER_PUBLISHER)));
			var firstWave = await Task.WhenAll(audits.Select(audit => audit.WaitForCountAsync(FIRST_WAVE_TOTAL, TEST_TIMEOUT)));
			Assert.All(firstWave, received => Assert.True(received, $"A ZeroMQ subscriber did not receive the complete {FIRST_WAVE_TOTAL}-message first wave."));

			var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			var secondWave = CreateGatedPublications(publishers, topic, "during", SECOND_WAVE_PER_PUBLISHER, gate.Task);
			var unsubscribe = UnsubscribeAsync(consumers[0], gate.Task);
			stopwatch.Start();
			gate.SetResult();
			await Task.WhenAll(Task.WhenAll(secondWave), unsubscribe).WaitAsync(TEST_TIMEOUT);

			for(int index = 1; index < SUBSCRIBER_COUNT; index++)
				Assert.True(await audits[index].WaitForCountAsync(ALL_WAVES_TOTAL, TEST_TIMEOUT),
					$"ZeroMQ subscriber {index} did not receive all {ALL_WAVES_TOTAL} messages while subscriber 0 unsubscribed.");

			await Task.Delay(TimeSpan.FromMilliseconds(250));
			var stoppedCount = audits[0].Count;
			Assert.InRange(stoppedCount, FIRST_WAVE_TOTAL, ALL_WAVES_TOTAL);
			Assert.Empty(subscribers[0].Subscribers);

			await publishers[1].ProduceAsync(topic, Encoding.UTF8.GetBytes("after-unsubscribe:probe"));

			for(int index = 1; index < SUBSCRIBER_COUNT; index++)
				Assert.True(await audits[index].WaitForCountAsync(FINAL_TOTAL, TEST_TIMEOUT),
					$"ZeroMQ subscriber {index} did not receive the post-unsubscribe probe.");

			await Task.Delay(TimeSpan.FromMilliseconds(500));
			stopwatch.Stop();

			Assert.Equal(stoppedCount, audits[0].Count);
			Assert.Equal(0, audits[0].DuplicateCount);
			Assert.Equal(0, audits[0].InvalidTopicCount);
			Assert.DoesNotContain("after-unsubscribe:probe", audits[0].Payloads);

			var expectedPayloads = CreatePayloads("before", PUBLISHER_COUNT, FIRST_WAVE_PER_PUBLISHER)
				.Concat(CreatePayloads("during", PUBLISHER_COUNT, SECOND_WAVE_PER_PUBLISHER))
				.Append("after-unsubscribe:probe")
				.OrderBy(payload => payload, StringComparer.Ordinal)
				.ToArray();

			for(int index = 1; index < SUBSCRIBER_COUNT; index++)
			{
				Assert.Equal(FINAL_TOTAL, audits[index].Count);
				Assert.Equal(0, audits[index].DuplicateCount);
				Assert.Equal(0, audits[index].InvalidTopicCount);
				Assert.Equal(expectedPayloads, audits[index].Payloads);
			}

			var throughput = TIMED_DELIVERY_COUNT / stopwatch.Elapsed.TotalSeconds;
			Assert.InRange(stopwatch.Elapsed, TimeSpan.Zero, TEST_TIMEOUT);
			Assert.True(throughput >= MINIMUM_DELIVERIES_PER_SECOND,
				$"ZeroMQ unsubscribe throughput {throughput:F2} active deliveries/s was below {MINIMUM_DELIVERIES_PER_SECOND:F2}; {TIMED_DELIVERY_COUNT} deliveries took {stopwatch.Elapsed}.");
		}
		finally
		{
			await CleanupAsync(publishers, subscribers, consumers);
		}

		AssertReleased(publishers.Concat(subscribers).ToArray(), consumers);
	}

	#region 私有方法
	private static async Task PublishBatchAsync(ZeroQueue queue, string topic, string prefix, int publisher, int count)
	{
		for(int index = 0; index < count; index++)
			await queue.ProduceAsync(topic, Encoding.UTF8.GetBytes($"{prefix}:{publisher:D2}:{index:D4}"));
	}

	private static Task<string>[] CreateGatedPublications(ZeroQueue[] queues, string topic, string prefix, int count, Task gate) =>
		queues.SelectMany((queue, publisher) => Enumerable.Range(0, count).Select(async index =>
		{
			await gate;
			return await queue.ProduceAsync(topic, Encoding.UTF8.GetBytes($"{prefix}:{publisher:D2}:{index:D4}"));
		})).ToArray();

	private static async Task UnsubscribeAsync(ZeroSubscriber subscriber, Task gate)
	{
		await gate;
		await subscriber.UnsubscribeAsync();
	}

	private static async Task WarmupAsync(ZeroQueue[] publishers, string topic, ZeroMessageAudit[] audits)
	{
		for(var index = 0; index < publishers.Length; index++)
		{
			var marker = $"warmup:{index}";
			var deadline = DateTime.UtcNow + TEST_TIMEOUT;

			do
			{
				await publishers[index].ProduceAsync(topic, Encoding.UTF8.GetBytes(marker));
				if(await ZeroTestUtility.WaitUntilAsync(() => audits.All(audit => audit.Contains(marker)), TimeSpan.FromMilliseconds(250)))
					break;
			}
			while(DateTime.UtcNow < deadline);

			if(!audits.All(audit => audit.Contains(marker)))
				throw new TimeoutException($"Timed out warming publisher {index} for '{topic}'.");
		}
	}

	private static string[] CreatePayloads(string prefix, int publisherCount, int messageCount, bool includePublisher = true) =>
		Enumerable.Range(0, publisherCount)
			.SelectMany(publisher => Enumerable.Range(0, messageCount)
				.Select(index => includePublisher ? $"{prefix}:{publisher:D2}:{index:D4}" : $"{prefix}:{index:D4}"))
			.OrderBy(payload => payload, StringComparer.Ordinal)
			.ToArray();

	private static async Task CleanupAsync(ZeroQueue[] publishers, ZeroQueue[] subscribers, ZeroSubscriber[] consumers)
	{
		try
		{
			foreach(var consumer in consumers)
			{
				if(consumer != null)
					await consumer.DisposeAsync();
			}
		}
		finally
		{
			foreach(var queue in publishers.Concat(subscribers))
				queue.Dispose();
		}
	}

	private static void AssertReleased(ZeroQueue[] queues, ZeroSubscriber[] consumers)
	{
		Assert.All(queues, queue =>
		{
			Assert.True(queue.IsDisposed);
			Assert.Empty(queue.Subscribers);
		});
		Assert.All(consumers, consumer =>
		{
			Assert.True(consumer.IsClosed);
			Assert.True(consumer.IsDisposed);
			Assert.Null(consumer.Handler);
			Assert.Null(consumer.Channel);
		});
	}
	#endregion

	#region 嵌套类型
	private sealed class ZeroMessageAudit(string topic) : HandlerBase<Message>
	{
		private readonly ConcurrentDictionary<string, int> _payloads = new(StringComparer.Ordinal);
		private int _count;
		private int _duplicateCount;
		private int _invalidTopicCount;

		public int Count => Volatile.Read(ref _count);
		public int DuplicateCount => Volatile.Read(ref _duplicateCount);
		public int InvalidTopicCount => Volatile.Read(ref _invalidTopicCount);
		public string[] Payloads => _payloads.Keys.OrderBy(payload => payload, StringComparer.Ordinal).ToArray();

		public Task<bool> WaitForCountAsync(int count, TimeSpan timeout) =>
			ZeroTestUtility.WaitUntilAsync(() => this.Count >= count, timeout);
		public bool Contains(string payload) => _payloads.ContainsKey(payload);

		public void Reset()
		{
			_payloads.Clear();
			Volatile.Write(ref _count, 0);
			Volatile.Write(ref _duplicateCount, 0);
			Volatile.Write(ref _invalidTopicCount, 0);
		}

		protected override ValueTask OnHandleAsync(Message message, Parameters parameters, CancellationToken cancellation)
		{
			if(!string.Equals(topic, message.Topic, StringComparison.Ordinal))
				Interlocked.Increment(ref _invalidTopicCount);

			var payload = Encoding.UTF8.GetString(message.Data);
			if(_payloads.AddOrUpdate(payload, 1, (_, current) => current + 1) > 1)
				Interlocked.Increment(ref _duplicateCount);

			Interlocked.Increment(ref _count);
			return ValueTask.CompletedTask;
		}
	}
	#endregion
}
