using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;

namespace Zongsoft.Messaging.Mqtt.Tests;

public class MqttQueuePublishingTests
{
	[Fact]
	public async Task PublishAndConsumeMessage()
	{
		if(!Global.IsTestingEnabled)
			return;

		using var server = await MqttServerScope.StartAsync();
		using var publisher = MqttTestUtility.CreateQueue(server.Port, "publisher");
		using var subscriber = MqttTestUtility.CreateQueue(server.Port, "subscriber");
		using var messages = new MqttMessageBuffer();
		var topic = $"tests/basic/{Guid.NewGuid():N}";

		var consumer = await subscriber.SubscribeAsync(topic, messages);
		Assert.NotNull(consumer);

		var identifier = await publisher.ProduceAsync(topic, Encoding.UTF8.GetBytes("Hello MQTTnet 5"));
		var message = await messages.ReceiveAsync(TimeSpan.FromSeconds(5));

		Assert.Null(identifier);
		Assert.Equal(topic, message.Topic);
		Assert.Equal("Hello MQTTnet 5", Encoding.UTF8.GetString(message.Data));
		Assert.Null(message.Identifier);
	}

	[Fact]
	public async Task ExplicitExactlyOnceUsesBrokerPacketIdentifier()
	{
		if(!Global.IsTestingEnabled)
			return;

		using var server = await MqttServerScope.StartAsync();
		using var publisher = MqttTestUtility.CreateQueue(server.Port, "exact-publisher");
		using var subscriber = MqttTestUtility.CreateQueue(server.Port, "exact-subscriber");
		using var messages = new MqttMessageBuffer();
		var topic = $"tests/exact/{Guid.NewGuid():N}";
		await subscriber.SubscribeAsync(topic, messages, new MessageSubscribeOptions(MessageReliability.ExactlyOnce));

		var identifier = await publisher.ProduceAsync(topic, Encoding.UTF8.GetBytes("exact"), new MessageEnqueueOptions(MessageReliability.ExactlyOnce));
		var message = await messages.ReceiveAsync(TimeSpan.FromSeconds(5));

		Assert.False(string.IsNullOrEmpty(identifier));
		Assert.False(string.IsNullOrEmpty(message.Identifier));
		Assert.Equal("exact", Encoding.UTF8.GetString(message.Data));
	}

	[Fact]
	public async Task WildcardSubscriptionAndUnsubscribe()
	{
		if(!Global.IsTestingEnabled)
			return;

		using var server = await MqttServerScope.StartAsync();
		using var publisher = MqttTestUtility.CreateQueue(server.Port, "publisher");
		using var subscriber = MqttTestUtility.CreateQueue(server.Port, "subscriber");
		using var messages = new MqttMessageBuffer();
		var prefix = $"tests/wildcard/{Guid.NewGuid():N}";
		var filter = $"{prefix}/+";
		var topic = $"{prefix}/temperature";

		var consumer = await subscriber.SubscribeAsync(filter, messages);
		await publisher.ProduceAsync(topic, Encoding.UTF8.GetBytes("21.5"));

		var message = await messages.ReceiveAsync(TimeSpan.FromSeconds(5));
		Assert.Equal(topic, message.Topic);

		await consumer.DisposeAsync();
		Assert.True(consumer.IsClosed);
		Assert.True(consumer.IsDisposed);
		Assert.Null(consumer.Handler);
		Assert.Empty(subscriber.Subscribers);

		await publisher.ProduceAsync(topic, Encoding.UTF8.GetBytes("22.0"));

		Assert.Null(await messages.TryReceiveAsync(TimeSpan.FromMilliseconds(500)));

		subscriber.Dispose();
		Assert.True(subscriber.IsDisposed);
		Assert.Empty(subscriber.Subscribers);
	}

	[Fact]
	public async Task ConcurrentPublishingDeliversAllMessages()
	{
		if(!Global.IsTestingEnabled)
			return;

		const int count = 500;

		using var server = await MqttServerScope.StartAsync();
		using var publisher = MqttTestUtility.CreateQueue(server.Port, "publisher");
		using var subscriber = MqttTestUtility.CreateQueue(server.Port, "subscriber");
		using var messages = new MqttMessageBuffer();
		var topic = $"tests/concurrent/{Guid.NewGuid():N}";

		await subscriber.SubscribeAsync(topic, messages);

		var stopwatch = Stopwatch.StartNew();
		var publishing = Enumerable.Range(0, count)
			.Select(index => publisher.ProduceAsync(topic, BitConverter.GetBytes(index)).AsTask())
			.ToArray();

		await Task.WhenAll(publishing);
		var received = await messages.ReceiveManyAsync(count, TimeSpan.FromSeconds(30));
		stopwatch.Stop();

		Assert.Equal(count, received.Length);
		Assert.Equal(count, received.Select(message => BitConverter.ToInt32(message.Data)).Distinct().Count());
		Assert.All(publishing, task => Assert.Null(task.Result));
		Assert.InRange(stopwatch.Elapsed, TimeSpan.Zero, TimeSpan.FromSeconds(30));
		Assert.True(count / stopwatch.Elapsed.TotalSeconds >= 1,
			$"MQTT concurrent producer throughput was below 1 publication/s; {count} published-and-received payloads took {stopwatch.Elapsed}.");
	}

	[Fact]
	public async Task SlowHandlersAreConsumedConcurrently()
	{
		if(!Global.IsTestingEnabled)
			return;

		const int count = 32;

		using var server = await MqttServerScope.StartAsync();
		using var publisher = MqttTestUtility.CreateQueue(server.Port, "publisher");
		using var subscriber = MqttTestUtility.CreateQueue(server.Port, "subscriber");
		var handler = new ConcurrentMessageHandler(count);
		var topic = $"tests/consumers/{Guid.NewGuid():N}";

		await subscriber.SubscribeAsync(topic, handler);

		var publishing = Enumerable.Range(0, count)
			.Select(index => publisher.ProduceAsync(topic, BitConverter.GetBytes(index)).AsTask());

		await Task.WhenAll(publishing);
		await handler.WaitAsync(TimeSpan.FromSeconds(10));

		Assert.Equal(count, handler.Count);
		Assert.True(handler.MaximumConcurrency > 1);
	}

	[Fact]
	public async Task ClientsReconnectAndRestoreSubscriptionsAfterServerRestart()
	{
		if(!Global.IsTestingEnabled)
			return;

		var port = MqttTestUtility.GetFreePort();
		using var server = new MqttQueueServer { Port = port };
		using var publisher = MqttTestUtility.CreateQueue(port, "publisher");
		using var subscriber = MqttTestUtility.CreateQueue(port, "subscriber");
		using var messages = new MqttMessageBuffer();
		var topic = $"tests/reconnect/{Guid.NewGuid():N}";

		await server.StartAsync([]);
		await subscriber.SubscribeAsync(topic, messages);
		await publisher.ProduceAsync(topic, Encoding.UTF8.GetBytes("before"));
		Assert.Equal("before", Encoding.UTF8.GetString((await messages.ReceiveAsync(TimeSpan.FromSeconds(5))).Data));

		await server.StopAsync([]);
		await server.StartAsync([]);

		Assert.True(await MqttTestUtility.WaitUntilAsync(
			() => server.Channels.Count >= 2, TimeSpan.FromSeconds(15)));

		Message message = default;
		for(int i = 0; i < 10 && message.IsEmpty; i++)
		{
			await publisher.ProduceAsync(topic, Encoding.UTF8.GetBytes("after"));
			message = await messages.TryReceiveAsync(TimeSpan.FromMilliseconds(500)) ?? default;
		}

		Assert.False(message.IsEmpty);
		Assert.Equal("after", Encoding.UTF8.GetString(message.Data));
	}
}
