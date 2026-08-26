using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using NetMQ;
using NetMQ.Sockets;

using Xunit;

namespace Zongsoft.Messaging.ZeroMQ.Tests;

public class ZeroQueuePublishingTests
{
	[Fact]
	public async Task PublishWithoutSubscriberReturnsNullImmediately()
	{
		if(!Global.IsTestingEnabled)
			return;

		using var server = await ZeroServerScope.StartAsync();
		using var publisher = ZeroTestUtility.CreateQueue(server.Port, "missing-publisher");
		using var subscriber = ZeroTestUtility.CreateQueue(server.Port, "missing-subscriber");
		using var handler = new MessageBuffer();
		var topic = "topic/missing";

		Assert.Null(await publisher.ProduceAsync(topic, Encoding.UTF8.GetBytes("missing")));
		var started = DateTime.UtcNow;
		Assert.Null(await publisher.ProduceAsync(topic, Encoding.UTF8.GetBytes("missing-again")));
		Assert.True(DateTime.UtcNow - started < TimeSpan.FromSeconds(1));

		await subscriber.SubscribeAsync(topic, handler);
		Assert.Null(await handler.TryReceiveAsync(TimeSpan.FromMilliseconds(300)));
	}

	[Fact]
	public async Task SuccessfulBroadcastReturnsDeliveredIdentifier()
	{
		if(!Global.IsTestingEnabled)
			return;

		using var server = await ZeroServerScope.StartAsync();
		using var publisher = ZeroTestUtility.CreateQueue(server.Port, "identifier-publisher");
		using var subscriber = ZeroTestUtility.CreateQueue(server.Port, "identifier-subscriber");
		using var handler = new MessageBuffer();
		var topic = "topic/identifier";

		await subscriber.SubscribeAsync(topic, handler);
		string identifier;
		do
		{
			identifier = await publisher.ProduceAsync(topic, "kind:sample,format:text", Encoding.UTF8.GetBytes("identified"));
			if(identifier == null)
				await Task.Delay(25);
		}
		while(identifier == null);
		var message = await handler.ReceiveAsync(TimeSpan.FromSeconds(5));

		Assert.False(string.IsNullOrWhiteSpace(identifier));
		Assert.Equal(identifier, message.Identifier);
		Assert.Equal(publisher.Instance, message.Identity);
		Assert.Equal("kind:sample,format:text", message.Tags);
	}

	[Fact]
	public async Task MostOnceCompressionUsesTypedThreshold()
	{
		if(!Global.IsTestingEnabled)
			return;

		using var server = await ZeroServerScope.StartAsync();
		using var publisher = ZeroTestUtility.CreateQueue(server.Port, "compression-publisher");
		using var subscriber = new SubscriberSocket();
		var topic = "topic/compression";
		var payload = Enumerable.Repeat((byte)'A', 16 * 1024).ToArray();
		var options = new MessageEnqueueOptions() { Compression = new MessageCompression("Brotli", 1) };
		var ports = ZeroTestUtility.GetServerPorts(server.Port);

		subscriber.Connect($"tcp://127.0.0.1:{ports.Outgoing}");
		subscriber.Subscribe(topic);
		var identifier = await ZeroTestUtility.PublishUntilAcceptedAsync(publisher, topic, payload, options);
		var message = new NetMQMessage();

		Assert.True(subscriber.TryReceiveMultipartMessage(TimeSpan.FromSeconds(5), ref message));
		Assert.Equal(2, message.FrameCount);
		Assert.Contains($"Identifier:{identifier}", message[0].ConvertToString());
		Assert.Contains("Compression:Brotli", message[0].ConvertToString());
		Assert.True(message[1].BufferSize < payload.Length);
		Assert.Equal(payload, IO.Compression.Compressor.Decompress("Brotli", message[1].ToByteArray()));
	}

	[Fact]
	public async Task ConcurrentBroadcastPublishesReturnUniqueIdentifiers()
	{
		if(!Global.IsTestingEnabled)
			return;

		using var server = await ZeroServerScope.StartAsync();
		using var publisher = ZeroTestUtility.CreateQueue(server.Port, "concurrent-publisher");
		using var subscriber = ZeroTestUtility.CreateQueue(server.Port, "concurrent-subscriber");
		using var handler = new MessageBuffer();
		var topic = "topic/concurrent";

		await subscriber.SubscribeAsync(topic, handler);
		await ZeroTestUtility.PublishUntilAcceptedAsync(publisher, topic, Encoding.UTF8.GetBytes("probe"));
		await handler.ReceiveAsync(TimeSpan.FromSeconds(5));
		var publications = Enumerable.Range(0, 64)
			.Select(index => publisher.ProduceAsync(topic, Encoding.UTF8.GetBytes($"message-{index}")).AsTask())
			.ToArray();
		var identifiers = await Task.WhenAll(publications);
		var messages = await handler.ReceiveManyAsync(64, TimeSpan.FromSeconds(5));

		Assert.All(identifiers, identifier => Assert.False(string.IsNullOrWhiteSpace(identifier)));
		Assert.Equal(64, identifiers.Distinct(StringComparer.Ordinal).Count());
		Assert.Equal(identifiers.OrderBy(identifier => identifier), messages.Select(message => message.Identifier).OrderBy(identifier => identifier));
	}

	[Fact]
	public async Task ProduceSnapshotsPayloadBeforeLocalSendCompletes()
	{
		if(!Global.IsTestingEnabled)
			return;

		using var server = await ZeroServerScope.StartAsync();
		using var publisher = ZeroTestUtility.CreateQueue(server.Port, "snapshot-publisher");
		using var subscriber = ZeroTestUtility.CreateQueue(server.Port, "snapshot-subscriber");
		using var handler = new MessageBuffer();
		var topic = "topic/snapshot";

		await subscriber.SubscribeAsync(topic, handler);
		var payload = Encoding.UTF8.GetBytes("original");
		await ZeroTestUtility.PublishUntilAcceptedAsync(publisher, topic, payload);
		Array.Fill(payload, (byte)'x');

		var message = await handler.ReceiveAsync(TimeSpan.FromSeconds(5));
		Assert.Equal("original", Encoding.UTF8.GetString(message.Data));
	}

	[Fact]
	public async Task FailedInitializationCanRetryAfterServerStarts()
	{
		if(!Global.IsTestingEnabled)
			return;

		var port = ZeroTestUtility.GetFreePort();
		using var publisher = ZeroTestUtility.CreateQueue(port, "retry-publisher", settings => settings.Timeout = TimeSpan.FromMilliseconds(100));
		var server = new ZeroQueueServer { Port = port };

		await Assert.ThrowsAsync<TimeoutException>(() =>
			publisher.ProduceAsync("topic/retry", ReadOnlyMemory<byte>.Empty).AsTask());

		try
		{
			await server.StartAsync([]);
			using var subscriber = ZeroTestUtility.CreateQueue(port, "retry-subscriber");
			using var handler = new MessageBuffer();
			await subscriber.SubscribeAsync("topic/retry", handler);

			await ZeroTestUtility.PublishUntilAcceptedAsync(publisher, "topic/retry", Encoding.UTF8.GetBytes("retried"));
			var message = await handler.ReceiveAsync(TimeSpan.FromSeconds(5));
			Assert.Equal("retried", Encoding.UTF8.GetString(message.Data));
		}
		finally
		{
			await server.StopAsync([]);
			((IDisposable)server).Dispose();
		}
	}

	[Fact]
	public async Task QueueSnapshotsDefaultTopicAndGroup()
	{
		if(!Global.IsTestingEnabled)
			return;

		using var server = await ZeroServerScope.StartAsync();
		var settings = Configuration.ZeroConnectionSettingsDriver.Instance.GetSettings("ZeroMQ",
			$"server=127.0.0.1;port={server.Port};client=snapshot-settings;topic=original;group=tenant;Timeout=5s;");
		using var publisher = new ZeroQueue("ZeroMQ", settings);
		settings.Topic = "changed";
		settings.Group = "changed";

		using var subscriber = ZeroTestUtility.CreateQueue(server.Port, "snapshot-settings-subscriber", options => options.Group = "tenant");
		using var handler = new MessageBuffer();
		await subscriber.SubscribeAsync("original", handler);

		string identifier;
		do
		{
			identifier = await publisher.ProduceAsync(Encoding.UTF8.GetBytes("snapshot"));
			if(identifier == null)
				await Task.Delay(25);
		}
		while(identifier == null);
		var message = await handler.ReceiveAsync(TimeSpan.FromSeconds(5));
		Assert.Equal("original", message.Topic);
		Assert.Equal("snapshot", Encoding.UTF8.GetString(message.Data));
	}

	[Fact]
	public async Task QueueWithShortHeartbeatStillPublishesMessages()
	{
		if(!Global.IsTestingEnabled)
			return;

		using var server = await ZeroServerScope.StartAsync();
		//短心跳会更频繁触发 poller 回调，用来覆盖发布者初始化和发送路径的竞态保护。
		using var publisher = ZeroTestUtility.CreateQueue(server.Port, "publisher", settings => settings.Heartbeat = TimeSpan.FromMilliseconds(10));
		using var subscriber = ZeroTestUtility.CreateQueue(server.Port, "subscriber");
		using var handler = new MessageBuffer();

		await subscriber.SubscribeAsync("topic/heartbeat", handler);
		await ZeroTestUtility.PublishUntilAcceptedAsync(publisher, "topic/heartbeat", Encoding.UTF8.GetBytes("heartbeat"));
		var message = await handler.ReceiveAsync(TimeSpan.FromSeconds(5));

		Assert.Equal("heartbeat", Encoding.UTF8.GetString(message.Data));
	}

	[Fact]
	public async Task PublishBurstMessagesAreDelivered()
	{
		if(!Global.IsTestingEnabled)
			return;

		using var server = await ZeroServerScope.StartAsync();
		using var publisher = ZeroTestUtility.CreateQueue(server.Port, "publisher");
		using var subscriber = ZeroTestUtility.CreateQueue(server.Port, "subscriber");
		using var handler = new MessageBuffer();
		var topic = "topic/burst";
		var count = 100;

		await subscriber.SubscribeAsync(topic, handler);
		await ZeroTestUtility.PublishUntilAcceptedAsync(publisher, topic, Encoding.UTF8.GetBytes("probe"));
		await handler.ReceiveAsync(TimeSpan.FromSeconds(5));
		//突发发送覆盖传输 Actor 的批量命令及即时发布路径。
		for(int i = 0; i < count; i++)
			await publisher.ProduceAsync(topic, Encoding.UTF8.GetBytes($"burst-{i}"));

		var messages = await handler.ReceiveManyAsync(count, TimeSpan.FromSeconds(10));
		var payloads = messages.Select(message => Encoding.UTF8.GetString(message.Data)).ToHashSet();

		Assert.Equal(count, payloads.Count);

		for(int i = 0; i < count; i++)
			Assert.Contains($"burst-{i}", payloads);
	}
}
