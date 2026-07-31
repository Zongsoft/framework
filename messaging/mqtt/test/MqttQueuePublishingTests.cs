using System;
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
		using var server = await MqttServerScope.StartAsync();
		using var publisher = MqttTestUtility.CreateQueue(server.Port, "publisher");
		using var subscriber = MqttTestUtility.CreateQueue(server.Port, "subscriber");
		using var messages = new MqttMessageBuffer();
		var topic = $"tests/basic/{Guid.NewGuid():N}";

		var consumer = await subscriber.SubscribeAsync(topic, messages);
		Assert.NotNull(consumer);

		var identifier = await publisher.ProduceAsync(topic, Encoding.UTF8.GetBytes("Hello MQTTnet 5"));
		var message = await messages.ReceiveAsync(TimeSpan.FromSeconds(5));

		Assert.False(string.IsNullOrEmpty(identifier));
		Assert.Equal(topic, message.Topic);
		Assert.Equal("Hello MQTTnet 5", Encoding.UTF8.GetString(message.Data));
		Assert.False(string.IsNullOrEmpty(message.Identifier));
	}

	[Fact]
	public async Task WildcardSubscriptionAndUnsubscribe()
	{
		using var server = await MqttServerScope.StartAsync();
		using var publisher = MqttTestUtility.CreateQueue(server.Port, "publisher");
		using var subscriber = MqttTestUtility.CreateQueue(server.Port, "subscriber");
		using var messages = new MqttMessageBuffer();
		var prefix = $"tests/wildcard/{Guid.NewGuid():N}";
		var filter = $"{prefix}/+";
		var topic = $"{prefix}/temperature";

		var consumer = await subscriber.SubscribeAsync(filter, messages);
		var client = MqttTestUtility.GetClient(subscriber);
		Assert.NotNull(client);
		Assert.True(client.IsConnected);
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
		Assert.False(client.IsConnected);
		Assert.True(MqttTestUtility.IsQueueTransportReleased(subscriber));
	}

	[Fact]
	public async Task ConcurrentPublishingDeliversAllMessages()
	{
		const int count = 500;

		using var server = await MqttServerScope.StartAsync();
		using var publisher = MqttTestUtility.CreateQueue(server.Port, "publisher");
		using var subscriber = MqttTestUtility.CreateQueue(server.Port, "subscriber");
		using var messages = new MqttMessageBuffer();
		var topic = $"tests/concurrent/{Guid.NewGuid():N}";

		await subscriber.SubscribeAsync(topic, messages);

		var publishing = Enumerable.Range(0, count)
			.Select(index => publisher.ProduceAsync(topic, BitConverter.GetBytes(index)).AsTask())
			.ToArray();

		await Task.WhenAll(publishing);
		var received = await messages.ReceiveManyAsync(count, TimeSpan.FromSeconds(30));

		Assert.Equal(count, received.Length);
		Assert.Equal(count, received.Select(message => BitConverter.ToInt32(message.Data)).Distinct().Count());
		Assert.All(publishing, task => Assert.False(string.IsNullOrEmpty(task.Result)));
	}

	[Fact]
	public async Task SlowHandlersAreConsumedConcurrently()
	{
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
