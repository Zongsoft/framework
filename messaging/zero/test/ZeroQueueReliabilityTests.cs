using System;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

using NetMQ;
using NetMQ.Sockets;

using Xunit;

using Zongsoft.Collections;
using Zongsoft.Components;
using Zongsoft.Configuration;

namespace Zongsoft.Messaging.ZeroMQ.Tests;

public class ZeroQueueReliabilityTests
{
	[Fact]
	public async Task LeastOnceCompletesAfterBrokerAcceptanceBeforeAcknowledge()
	{
		if(!Global.IsTestingEnabled)
			return;

		await using var scope = await ReliableServerScope.StartAsync();
		using var publisher = CreateQueue(scope.Port, "publisher", "publisher");
		using var subscriber = CreateQueue(scope.Port, "subscriber", "subscriber");
		var handler = new AcknowledgingHandler(1, false);
		await subscriber.SubscribeAsync("topic/reliable", handler, ReliableSubscribeOptions());

		var identifier = await publisher.ProduceAsync("topic/reliable", Encoding.UTF8.GetBytes("reliable"), ReliableEnqueueOptions()).AsTask().WaitAsync(TimeSpan.FromSeconds(5));
		var message = await handler.ReceiveAsync(TimeSpan.FromSeconds(5));

		Assert.False(string.IsNullOrWhiteSpace(identifier));
		Assert.Equal(identifier, message.Identifier);
		Assert.Single(await GetPendingAsync(scope));

		await message.AcknowledgeAsync();
		Assert.True(await WaitForPendingCountAsync(scope, 0, TimeSpan.FromSeconds(5)));
	}

	[Fact]
	public async Task LeastOncePreservesMessageMetadataInBrokerStorageAndDelivery()
	{
		if(!Global.IsTestingEnabled)
			return;

		await using var scope = await ReliableServerScope.StartAsync();
		using var publisher = CreateQueue(scope.Port, "metadata-publisher", "publisher");
		using var subscriber = CreateQueue(scope.Port, "metadata-subscriber", "subscriber");
		var handler = new AcknowledgingHandler(1, false);
		await subscriber.SubscribeAsync("topic/metadata", handler, ReliableSubscribeOptions());

		var identifier = await publisher.ProduceAsync("topic/metadata", "kind:sample", Encoding.UTF8.GetBytes("metadata"), ReliableEnqueueOptions());
		var delivered = await handler.ReceiveAsync(TimeSpan.FromSeconds(5));
		var stored = Assert.Single(await GetPendingAsync(scope));

		Assert.Equal(identifier, delivered.Identifier);
		Assert.Equal(identifier, stored.Identifier);
		Assert.Equal("topic/metadata", delivered.Topic);
		Assert.Equal("topic/metadata", stored.Topic);
		Assert.Equal("kind:sample", delivered.Tags);
		Assert.Equal("kind:sample", stored.Tags);
		Assert.Equal(publisher.Instance, delivered.Identity);
		Assert.Equal(publisher.Instance, stored.Identity);
		Assert.Equal(stored.Timestamp, delivered.Timestamp);

		await delivered.AcknowledgeAsync();
		Assert.True(await WaitForPendingCountAsync(scope, 0, TimeSpan.FromSeconds(5)));
	}

	[Fact]
	public async Task LeastOnceWithoutSubscriptionReturnsNullWithoutPersistence()
	{
		if(!Global.IsTestingEnabled)
			return;

		await using var scope = await ReliableServerScope.StartAsync();
		using var publisher = CreateQueue(scope.Port, "missing-publisher", null);
		var started = DateTime.UtcNow;

		var identifier = await publisher.ProduceAsync("topic/missing-reliable", ReadOnlyMemory<byte>.Empty, ReliableEnqueueOptions());

		Assert.Null(identifier);
		Assert.True(DateTime.UtcNow - started < TimeSpan.FromSeconds(1));
		Assert.Empty(await GetPendingAsync(scope));
		Assert.Equal(0, scope.Storage.SetCount);
	}

	[Fact]
	public async Task LeastOnceSubscribersCompeteForMessages()
	{
		if(!Global.IsTestingEnabled)
			return;

		await using var scope = await ReliableServerScope.StartAsync();
		using var publisher = CreateQueue(scope.Port, "compete-publisher", null);
		using var first = CreateQueue(scope.Port, "compete-first", null);
		using var second = CreateQueue(scope.Port, "compete-second", null);
		var firstHandler = new AcknowledgingHandler(1, true);
		var secondHandler = new AcknowledgingHandler(1, true);
		await first.SubscribeAsync("topic/compete", firstHandler, ReliableSubscribeOptions());
		await second.SubscribeAsync("topic/compete", secondHandler, ReliableSubscribeOptions());

		var identifiers = new HashSet<string>(StringComparer.Ordinal);
		for(var index = 0; index < 8; index++)
			identifiers.Add(await publisher.ProduceAsync("topic/compete", Encoding.UTF8.GetBytes($"message-{index}"), ReliableEnqueueOptions()));

		Assert.True(await ZeroTestUtility.WaitUntilAsync(() => firstHandler.Count + secondHandler.Count >= 8, TimeSpan.FromSeconds(5)));
		Assert.Equal(8, identifiers.Count);
		Assert.Equal(8, firstHandler.Count + secondHandler.Count);
		Assert.True(firstHandler.Count > 0);
		Assert.True(secondHandler.Count > 0);
		Assert.True(await WaitForPendingCountAsync(scope, 0, TimeSpan.FromSeconds(5)));
	}

	[Fact]
	public async Task LeastOnceRetriesSameIdentifierAndAnyAcknowledgeRemovesPending()
	{
		if(!Global.IsTestingEnabled)
			return;

		await using var scope = await ReliableServerScope.StartAsync();
		using var publisher = CreateQueue(scope.Port, "retry-publisher", null);
		using var first = CreateQueue(scope.Port, "retry-first", null);
		var firstHandler = new AcknowledgingHandler(1, false);
		await first.SubscribeAsync("topic/retry", firstHandler, ReliableSubscribeOptions());

		var identifier = await publisher.ProduceAsync("topic/retry", Encoding.UTF8.GetBytes("retry"), ReliableEnqueueOptions());
		var firstDelivery = await firstHandler.ReceiveAsync(TimeSpan.FromSeconds(5));
		using var second = CreateQueue(scope.Port, "retry-second", null);
		var secondHandler = new AcknowledgingHandler(1, true);
		await second.SubscribeAsync("topic/retry", secondHandler, ReliableSubscribeOptions());
		var secondDelivery = await secondHandler.ReceiveAsync(TimeSpan.FromSeconds(5));

		Assert.Equal(identifier, firstDelivery.Identifier);
		Assert.Equal(identifier, secondDelivery.Identifier);
		Assert.True(await WaitForPendingCountAsync(scope, 0, TimeSpan.FromSeconds(5)));
	}

	[Fact]
	public async Task BrokerRestartRestoresAcceptedPendingMessage()
	{
		if(!Global.IsTestingEnabled)
			return;

		await using var scope = await ReliableServerScope.StartAsync();
		using var publisher = CreateQueue(scope.Port, "restart-publisher", null);
		using var subscriber = CreateQueue(scope.Port, "restart-subscriber", null);
		var handler = new AcknowledgingHandler(2, true);
		await subscriber.SubscribeAsync("topic/restart", handler, ReliableSubscribeOptions());

		var identifier = await publisher.ProduceAsync("topic/restart", Encoding.UTF8.GetBytes("restart"), ReliableEnqueueOptions(TimeSpan.FromSeconds(20)));
		Assert.Equal(identifier, (await handler.ReceiveAsync(TimeSpan.FromSeconds(5))).Identifier);
		Assert.Single(await GetPendingAsync(scope));

		await scope.RestartAsync();
		Assert.Equal(identifier, (await handler.ReceiveAsync(TimeSpan.FromSeconds(10))).Identifier);
		Assert.True(await WaitForPendingCountAsync(scope, 0, TimeSpan.FromSeconds(5)));
	}

	[Fact]
	public async Task LeastOnceExpirationRemovesPending()
	{
		if(!Global.IsTestingEnabled)
			return;

		await using var scope = await ReliableServerScope.StartAsync();
		using var publisher = CreateQueue(scope.Port, "expired-publisher", null);
		using var subscriber = CreateQueue(scope.Port, "expired-subscriber", null);
		await subscriber.SubscribeAsync("topic/expired", new AcknowledgingHandler(int.MaxValue, false), ReliableSubscribeOptions());

		var identifier = await publisher.ProduceAsync("topic/expired", Encoding.UTF8.GetBytes("expired"), ReliableEnqueueOptions(TimeSpan.FromMilliseconds(500)));

		Assert.False(string.IsNullOrWhiteSpace(identifier));
		Assert.True(await WaitForPendingCountAsync(scope, 0, TimeSpan.FromSeconds(5)));
	}

	[Fact]
	public async Task LeastOnceDoesNotRequireStableClientOrInstance()
	{
		if(!Global.IsTestingEnabled)
			return;

		await using var scope = await ReliableServerScope.StartAsync();
		using var publisher = ZeroTestUtility.CreateQueue(scope.Port, "unstable-publisher", settings => settings.Instance = null);
		using var subscriber = ZeroTestUtility.CreateQueue(scope.Port, "unstable-subscriber", settings => settings.Instance = null);
		var handler = new AcknowledgingHandler(1, true);
		await subscriber.SubscribeAsync("topic/unstable", handler, ReliableSubscribeOptions());

		var identifier = await publisher.ProduceAsync("topic/unstable", Encoding.UTF8.GetBytes("unstable"), ReliableEnqueueOptions());
		var message = await handler.ReceiveAsync(TimeSpan.FromSeconds(5));

		Assert.False(string.IsNullOrWhiteSpace(identifier));
		Assert.Equal(identifier, message.Identifier);
	}

	[Fact]
	public async Task LeastOnceRequiresServerControlChannel()
	{
		if(!Global.IsTestingEnabled)
			return;

		using var server = await ZeroServerScope.StartAsync();
		using var subscriber = ZeroTestUtility.CreateQueue(server.Port, "control-subscriber");
		using var publisher = ZeroTestUtility.CreateQueue(server.Port, "control-publisher");

		await Assert.ThrowsAsync<InvalidOperationException>(() =>
			subscriber.SubscribeAsync("topic/control", new AcknowledgingHandler(1, true), ReliableSubscribeOptions()).AsTask());
		await Assert.ThrowsAsync<InvalidOperationException>(() =>
			publisher.ProduceAsync("topic/control", ReadOnlyMemory<byte>.Empty, ReliableEnqueueOptions()).AsTask());
		Assert.Empty(subscriber.Subscribers);
	}

	[Fact]
	public async Task BrokerStorageFailureRejectsLeastOncePublish()
	{
		if(!Global.IsTestingEnabled)
			return;

		await using var scope = await ReliableServerScope.StartAsync(new FailingMessageStorage());
		using var publisher = CreateQueue(scope.Port, "failure-publisher", null);
		using var subscriber = CreateQueue(scope.Port, "failure-subscriber", null);
		await subscriber.SubscribeAsync("topic/failure", new AcknowledgingHandler(1, true), ReliableSubscribeOptions());

		await Assert.ThrowsAsync<InvalidOperationException>(() =>
			publisher.ProduceAsync("topic/failure", Encoding.UTF8.GetBytes("failure"), ReliableEnqueueOptions()).AsTask());
	}

	[Fact]
	public async Task MalformedControlCommandsDoNotStopBroker()
	{
		if(!Global.IsTestingEnabled)
			return;

		await using var scope = await ReliableServerScope.StartAsync();
		var control = ZeroTestUtility.GetServerPorts(scope.Port).Control;
		using(var socket = new DealerSocket())
		{
			socket.Connect($"tcp://127.0.0.1:{control}");
			socket.SendMoreFrame(Protocol.Commands.Publish).SendFrame("too-few-frames");
			socket.SendMoreFrame(Protocol.Commands.Publish)
				.SendMoreFrame(Guid.NewGuid().ToString("N"))
				.SendMoreFrame("topic/malformed-control")
				.SendMoreFrame("producer")
				.SendMoreFrameEmpty()
				.SendMoreFrame("not-a-timestamp")
				.SendMoreFrame("not-an-expiration")
				.SendMoreFrameEmpty()
				.SendFrameEmpty();
			Assert.True(socket.TryReceiveFrameString(TimeSpan.FromSeconds(5), out var response));
			Assert.Equal(Protocol.Commands.Error, response);
		}

		using var publisher = CreateQueue(scope.Port, "valid-publisher", null);
		using var subscriber = CreateQueue(scope.Port, "valid-subscriber", null);
		var handler = new AcknowledgingHandler(1, true);
		await subscriber.SubscribeAsync("topic/valid-control", handler, ReliableSubscribeOptions());

		var identifier = await publisher.ProduceAsync("topic/valid-control", Encoding.UTF8.GetBytes("valid"), ReliableEnqueueOptions());
		Assert.Equal(identifier, (await handler.ReceiveAsync(TimeSpan.FromSeconds(5))).Identifier);
		Assert.True(await WaitForPendingCountAsync(scope, 0, TimeSpan.FromSeconds(5)));
	}

	[Fact]
	public async Task LeastOnceTimeoutCanLeaveBrokerAcceptanceUncertain()
	{
		if(!Global.IsTestingEnabled)
			return;

		var storage = new BlockingMessageStorage();
		await using var scope = await ReliableServerScope.StartAsync(storage);
		using var publisher = ZeroTestUtility.CreateQueue(scope.Port, "timeout-publisher", settings => settings.Timeout = TimeSpan.FromMilliseconds(250));
		using var subscriber = CreateQueue(scope.Port, "timeout-subscriber", null);
		var handler = new AcknowledgingHandler(int.MaxValue, false);
		await subscriber.SubscribeAsync("topic/timeout", handler, ReliableSubscribeOptions());

		var publication = publisher.ProduceAsync("topic/timeout", Encoding.UTF8.GetBytes("uncertain"), ReliableEnqueueOptions()).AsTask();
		await storage.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
		try { await Assert.ThrowsAsync<TimeoutException>(() => publication); }
		finally { storage.Release(); }

		var delivered = await handler.ReceiveAsync(TimeSpan.FromSeconds(5));
		Assert.True(await ZeroTestUtility.WaitUntilAsync(() => storage.Inner.SetCount == 1, TimeSpan.FromSeconds(5)));
		var stored = Assert.Single(await ZeroTestUtility.CollectAsync(storage.Inner.GetAsync()));
		Assert.Equal(stored.Identifier, delivered.Identifier);
	}

	[Fact]
	public async Task LeastOnceCancellationCanLeaveBrokerAcceptanceUncertain()
	{
		if(!Global.IsTestingEnabled)
			return;

		var storage = new BlockingMessageStorage();
		await using var scope = await ReliableServerScope.StartAsync(storage);
		using var publisher = CreateQueue(scope.Port, "cancel-publisher", null);
		using var subscriber = CreateQueue(scope.Port, "cancel-subscriber", null);
		var handler = new AcknowledgingHandler(int.MaxValue, false);
		await subscriber.SubscribeAsync("topic/cancel", handler, ReliableSubscribeOptions());
		using var cancellation = new CancellationTokenSource();

		var publication = publisher.ProduceAsync("topic/cancel", Encoding.UTF8.GetBytes("uncertain"), ReliableEnqueueOptions(), cancellation.Token).AsTask();
		await storage.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
		await cancellation.CancelAsync();
		try { await Assert.ThrowsAnyAsync<OperationCanceledException>(() => publication); }
		finally { storage.Release(); }

		var delivered = await handler.ReceiveAsync(TimeSpan.FromSeconds(5));
		Assert.True(await ZeroTestUtility.WaitUntilAsync(() => storage.Inner.SetCount == 1, TimeSpan.FromSeconds(5)));
		var stored = Assert.Single(await ZeroTestUtility.CollectAsync(storage.Inner.GetAsync()));
		Assert.Equal(stored.Identifier, delivered.Identifier);
	}

	[Fact]
	public async Task BlockedStorageWriteDoesNotBlockBroadcastOrDiscovery()
	{
		if(!Global.IsTestingEnabled)
			return;

		var storage = new BlockingMessageStorage();
		await using var scope = await ReliableServerScope.StartAsync(storage);
		using var publisher = CreateQueue(scope.Port, "nonblocking-publisher", null);
		using var subscriber = CreateQueue(scope.Port, "nonblocking-subscriber", null);
		var reliableHandler = new AcknowledgingHandler(1, true);
		var broadcastHandler = new AcknowledgingHandler(1, false);
		await subscriber.SubscribeAsync("topic/nonblocking/reliable", reliableHandler, ReliableSubscribeOptions());
		await subscriber.SubscribeAsync("topic/nonblocking/broadcast", broadcastHandler);

		var publication = publisher.ProduceAsync(
			"topic/nonblocking/reliable",
			Encoding.UTF8.GetBytes("reliable"),
			ReliableEnqueueOptions()).AsTask();
		await storage.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));

		try
		{
			Assert.False(publication.IsCompleted);
			Assert.True(ZeroTestUtility.CanQueryServer(scope.Port));

			var identifier = await ZeroTestUtility.PublishUntilAcceptedAsync(
				publisher,
				"topic/nonblocking/broadcast",
				Encoding.UTF8.GetBytes("broadcast"));
			var broadcast = await broadcastHandler.ReceiveAsync(TimeSpan.FromSeconds(5));

			Assert.Equal(identifier, broadcast.Identifier);
			Assert.Equal("broadcast", Encoding.UTF8.GetString(broadcast.Data));
			Assert.False(publication.IsCompleted);
		}
		finally
		{
			storage.Release();
		}

		var reliableIdentifier = await publication.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.Equal(reliableIdentifier, (await reliableHandler.ReceiveAsync(TimeSpan.FromSeconds(5))).Identifier);
		Assert.True(await ZeroTestUtility.WaitUntilAsync(() => storage.Inner.RemoveCount > 0, TimeSpan.FromSeconds(5)));
	}

	[Fact]
	public async Task AcknowledgeStopsDeliveryWhileStorageRemovalRetries()
	{
		if(!Global.IsTestingEnabled)
			return;

		var storage = new RetryingRemoveMessageStorage();
		await using var scope = await ReliableServerScope.StartAsync(storage);
		using var publisher = CreateQueue(scope.Port, "remove-retry-publisher", null);
		using var subscriber = CreateQueue(scope.Port, "remove-retry-subscriber", null);
		var handler = new AcknowledgingHandler(1, false);
		await subscriber.SubscribeAsync("topic/remove-retry", handler, ReliableSubscribeOptions());

		var identifier = await publisher.ProduceAsync("topic/remove-retry", Encoding.UTF8.GetBytes("remove-retry"), ReliableEnqueueOptions());
		var delivered = await handler.ReceiveAsync(TimeSpan.FromSeconds(5));
		Assert.Equal(identifier, delivered.Identifier);

		await delivered.AcknowledgeAsync();
		await storage.FirstRemoveFailed.Task.WaitAsync(TimeSpan.FromSeconds(5));
		await storage.RetryStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

		try
		{
			Assert.Single(await ZeroTestUtility.CollectAsync(storage.Inner.GetAsync()));
			Assert.Null(await handler.TryReceiveAsync(TimeSpan.FromMilliseconds(1500)));
			Assert.Equal(2, storage.RemoveAttempts);
		}
		finally
		{
			storage.ReleaseRetry();
		}

		Assert.True(await WaitForStoredCountAsync(storage, 0, TimeSpan.FromSeconds(5)));
		Assert.Equal(1, storage.Inner.RemoveCount);
	}

	[Fact]
	public async Task ServerStopDuringStorageWriteDrainsAndRestoresPendingOnRestart()
	{
		if(!Global.IsTestingEnabled)
			return;

		var storage = new BlockingMessageStorage();
		await using var scope = await ReliableServerScope.StartAsync(storage);
		using var publisher = CreateQueue(scope.Port, "stop-drain-publisher", null);
		using var subscriber = CreateQueue(scope.Port, "stop-drain-subscriber", null);
		var handler = new AcknowledgingHandler(1, false);
		await subscriber.SubscribeAsync("topic/stop-drain", handler, ReliableSubscribeOptions());

		var publication = publisher.ProduceAsync("topic/stop-drain", Encoding.UTF8.GetBytes("stop-drain"), ReliableEnqueueOptions(TimeSpan.FromSeconds(20))).AsTask();
		await storage.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
		var stopping = scope.StopAsync();

		try
		{
			Assert.True(await ZeroTestUtility.WaitUntilAsync(() => !ZeroTestUtility.CanQueryServer(scope.Port), TimeSpan.FromSeconds(5)));
			Assert.False(stopping.IsCompleted);
		}
		finally
		{
			storage.Release();
		}

		await stopping.WaitAsync(TimeSpan.FromSeconds(5));
		var stored = Assert.Single(await ZeroTestUtility.CollectAsync(storage.Inner.GetAsync()));
		publisher.Dispose();
		try { await publication.WaitAsync(TimeSpan.FromSeconds(1)); }
		catch(Exception) { }

		await scope.StartAsync();
		var restored = await handler.ReceiveAsync(TimeSpan.FromSeconds(10));
		Assert.Equal(stored.Identifier, restored.Identifier);
		Assert.Equal("stop-drain", Encoding.UTF8.GetString(restored.Data));

		await restored.AcknowledgeAsync();
		Assert.True(await WaitForStoredCountAsync(storage, 0, TimeSpan.FromSeconds(5)));
		Assert.True(storage.Inner.RemoveCount > 0);
	}

	[Fact]
	public async Task StorageWorkerCapacityReturnsStorageBusyWithoutPersistingRejectedMessage()
	{
		if(!Global.IsTestingEnabled)
			return;

		var storage = new BlockingMessageStorage();
		await using var scope = await ReliableServerScope.StartAsync(storage);
		using var subscriber = CreateQueue(scope.Port, "capacity-subscriber", null);
		await subscriber.SubscribeAsync("topic/capacity", new AcknowledgingHandler(int.MaxValue, false), ReliableSubscribeOptions());
		var control = ZeroTestUtility.GetServerPorts(scope.Port).Control;

		string rejected;
		try
		{
			rejected = await PublishUntilStorageBusyAsync(control, "topic/capacity", 1026).WaitAsync(TimeSpan.FromSeconds(10));
			Assert.Equal(0, storage.Inner.SetCount);
		}
		finally
		{
			storage.Release();
		}

		Assert.True(await ZeroTestUtility.WaitUntilAsync(() => storage.Inner.SetCount > 0, TimeSpan.FromSeconds(5)));
		Assert.DoesNotContain(await ZeroTestUtility.CollectAsync(storage.Inner.GetAsync()), message => message.Identifier == rejected);
	}

	[Fact]
	public async Task QueueCombinesBroadcastAndReliableSubscriptionsInOneRegistry()
	{
		if(!Global.IsTestingEnabled)
			return;

		await using var scope = await ReliableServerScope.StartAsync();
		using var publisher = CreateQueue(scope.Port, "combined-publisher", null);
		using var subscriber = CreateQueue(scope.Port, "combined-subscriber", null);
		var broadcastHandler = new AcknowledgingHandler(1, true);
		var reliableHandler = new AcknowledgingHandler(1, true);
		var broadcast = await subscriber.SubscribeAsync("topic/combined-broadcast", broadcastHandler);
		var reliable = await subscriber.SubscribeAsync("topic/combined-reliable", reliableHandler, ReliableSubscribeOptions());

		Assert.Equal(2, subscriber.Subscribers.Count);
		await ZeroTestUtility.PublishUntilAcceptedAsync(publisher, "topic/combined-broadcast", Encoding.UTF8.GetBytes("broadcast"));
		await publisher.ProduceAsync("topic/combined-reliable", Encoding.UTF8.GetBytes("reliable"), ReliableEnqueueOptions());
		Assert.Equal("broadcast", Encoding.UTF8.GetString((await broadcastHandler.ReceiveAsync(TimeSpan.FromSeconds(5))).Data));
		Assert.Equal("reliable", Encoding.UTF8.GetString((await reliableHandler.ReceiveAsync(TimeSpan.FromSeconds(5))).Data));

		await broadcast.DisposeAsync();
		await reliable.DisposeAsync();
		Assert.Empty(subscriber.Subscribers);
	}

	[Fact]
	public async Task ReliableInstanceFilterAcknowledgesWithoutInvokingHandler()
	{
		if(!Global.IsTestingEnabled)
			return;

		await using var scope = await ReliableServerScope.StartAsync();
		using var publisher = CreateQueue(scope.Port, "filter-publisher", "publisher");
		using var subscriber = ZeroTestUtility.CreateQueue(scope.Port, "filter-subscriber", settings => settings.Filter = "!publisher");
		var handler = new AcknowledgingHandler(1, true);
		await subscriber.SubscribeAsync("topic/filter", handler, ReliableSubscribeOptions());

		Assert.NotNull(await publisher.ProduceAsync("topic/filter", Encoding.UTF8.GetBytes("filtered"), ReliableEnqueueOptions()));
		Assert.Null(await handler.TryReceiveAsync(TimeSpan.FromMilliseconds(300)));
		Assert.True(await WaitForPendingCountAsync(scope, 0, TimeSpan.FromSeconds(5)));
	}

	private static ZeroQueue CreateQueue(ushort port, string client, string instance) =>
		ZeroTestUtility.CreateQueue(port, client, settings =>
		{
			settings.Client = client;
			settings.Instance = instance;
			settings.ReconnectInterval = TimeSpan.FromMilliseconds(200);
		});

	private static MessageSubscribeOptions ReliableSubscribeOptions() => new(MessageReliability.LeastOnce);
	private static MessageEnqueueOptions ReliableEnqueueOptions(TimeSpan expiration = default) => new(MessageReliability.LeastOnce)
	{
		Expiration = expiration == default ? TimeSpan.FromSeconds(10) : expiration,
	};

	private static Task<string> PublishUntilStorageBusyAsync(ushort control, string topic, int count) => Task.Run(() =>
	{
		using var socket = new DealerSocket();
		socket.Options.SendHighWatermark = count * 2;
		socket.Options.ReceiveHighWatermark = count * 2;
		socket.Connect($"tcp://127.0.0.1:{control}");
		var timestamp = DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture);
		var expiration = DateTime.UtcNow.AddMinutes(1).Ticks.ToString(CultureInfo.InvariantCulture);

		for(var index = 0; index < count; index++)
		{
			socket.SendMoreFrame(Protocol.Commands.Publish)
				.SendMoreFrame($"capacity-{index}")
				.SendMoreFrame(topic)
				.SendMoreFrame("capacity-producer")
				.SendMoreFrameEmpty()
				.SendMoreFrame(timestamp)
				.SendMoreFrame(expiration)
				.SendMoreFrameEmpty()
				.SendFrame(BitConverter.GetBytes(index));
		}

		var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
		do
		{
			var response = new NetMQMessage();
			if(socket.TryReceiveMultipartMessage(ref response) &&
			   response.FrameCount == 3 &&
			   string.Equals(response[0].ConvertToString(), Protocol.Commands.Error, StringComparison.Ordinal) &&
			   string.Equals(response[1].ConvertToString(), Protocol.Errors.StorageBusy, StringComparison.Ordinal))
				return response[2].ConvertToString();

			Thread.Sleep(10);
		}
		while(DateTime.UtcNow < deadline);

		throw new TimeoutException("The Control channel did not report StorageBusy before the test timeout.");
	});

	private static Task<Message[]> GetPendingAsync(ReliableServerScope scope) =>
		ZeroTestUtility.CollectAsync(scope.Storage.GetAsync());

	private static async Task<bool> WaitForPendingCountAsync(ReliableServerScope scope, int count, TimeSpan timeout)
	{
		var deadline = DateTime.UtcNow + timeout;
		do
		{
			if((await GetPendingAsync(scope)).Length == count)
				return true;
			await Task.Delay(50);
		}
		while(DateTime.UtcNow < deadline);

		return (await GetPendingAsync(scope)).Length == count;
	}

	private static async Task<bool> WaitForStoredCountAsync(IMessageStorage storage, int count, TimeSpan timeout)
	{
		var deadline = DateTime.UtcNow + timeout;
		do
		{
			if((await ZeroTestUtility.CollectAsync(storage.GetAsync())).Length == count)
				return true;
			await Task.Delay(50);
		}
		while(DateTime.UtcNow < deadline);

		return (await ZeroTestUtility.CollectAsync(storage.GetAsync())).Length == count;
	}

	private sealed class AcknowledgingHandler(int acknowledgeOn, bool automatic) : HandlerBase<Message>
	{
		private readonly SemaphoreSlim _signal = new(0);
		private readonly ConcurrentQueue<Message> _messages = new();
		private int _count;

		public int Count => Volatile.Read(ref _received);
		private int _received;
		public ConcurrentQueue<string> Identifiers { get; } = new();

		public async Task<Message> ReceiveAsync(TimeSpan timeout)
		{
			using var cancellation = new CancellationTokenSource(timeout);
			await _signal.WaitAsync(cancellation.Token);
			Assert.True(_messages.TryDequeue(out var message));
			return message;
		}

		public async Task<Message?> TryReceiveAsync(TimeSpan timeout)
		{
			using var cancellation = new CancellationTokenSource(timeout);
			try { await _signal.WaitAsync(cancellation.Token); }
			catch(OperationCanceledException) { return null; }
			Assert.True(_messages.TryDequeue(out var message));
			return message;
		}

		protected override async ValueTask OnHandleAsync(Message message, Parameters parameters, CancellationToken cancellation)
		{
			Identifiers.Enqueue(message.Identifier);
			_messages.Enqueue(message);
			Interlocked.Increment(ref _received);
			_signal.Release();
			if(automatic && Interlocked.Increment(ref _count) >= acknowledgeOn)
				await message.AcknowledgeAsync(cancellation);
		}
	}

	private sealed class FailingMessageStorage : IMessageStorage
	{
		public string Name => "failure";
		public bool Disposable => false;
		public IConnectionSettings Settings { get; set; } = new ConnectionSettings();

		public ValueTask<int> ClearAsync(CancellationToken cancellation = default) => ValueTask.FromResult(0);
		public ValueTask<int> ClearAsync(string topic, CancellationToken cancellation = default) => ValueTask.FromResult(0);

		public ValueTask SetAsync(Message message, TimeSpan expiry = default, CancellationToken cancellation = default) =>
			ValueTask.FromException(new InvalidOperationException("Storage failure."));

		public ValueTask<bool> RemoveAsync(string identifier, CancellationToken cancellation = default) => ValueTask.FromResult(false);

		public async IAsyncEnumerable<Message> GetAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellation = default)
		{
			await Task.CompletedTask;
			yield break;
		}

		public async IAsyncEnumerable<Message> GetAsync(string topic, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellation = default)
		{
			await Task.CompletedTask;
			yield break;
		}
	}

	[Fact]
	public async Task LeastOnceCompressedPayloadIsPersistedRecoveredAndDelivered()
	{
		if(!Global.IsTestingEnabled)
			return;

		await using var scope = await ReliableServerScope.StartAsync();
		using var publisher = CreateQueue(scope.Port, "compressed-publisher", "publisher");
		using var subscriber = CreateQueue(scope.Port, "compressed-subscriber", "subscriber");
		var handler = new AcknowledgingHandler(2, false);
		await subscriber.SubscribeAsync("topic/compressed", handler, ReliableSubscribeOptions());
		var payload = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Repeat((byte)'A', 16 * 1024));
		var options = ReliableEnqueueOptions(TimeSpan.FromSeconds(20));
		options.Compression = new MessageCompression("Brotli", 1);

		var identifier = await publisher.ProduceAsync("topic/compressed", "kind:compressed", payload, options);
		var first = await handler.ReceiveAsync(TimeSpan.FromSeconds(5));
		var stored = Assert.Single(await GetPendingAsync(scope));
		using(var document = JsonDocument.Parse(stored.Data))
		{
			Assert.Equal(Protocol.Version, document.RootElement.GetProperty("Version").GetString());
			Assert.Equal("Brotli", document.RootElement.GetProperty("Compression").GetString());
			Assert.True(document.RootElement.GetProperty("Data").GetBytesFromBase64().Length < payload.Length);
		}

		Assert.Equal(identifier, first.Identifier);
		Assert.Equal(payload, first.Data);
		Assert.Equal("kind:compressed", first.Tags);

		await scope.RestartAsync();
		var second = await handler.ReceiveAsync(TimeSpan.FromSeconds(10));
		Assert.Equal(identifier, second.Identifier);
		Assert.Equal(payload, second.Data);
		await second.AcknowledgeAsync();
		Assert.True(await WaitForPendingCountAsync(scope, 0, TimeSpan.FromSeconds(5)));
	}

	private sealed class BlockingMessageStorage : IMessageStorage
	{
		private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public MemoryMessageStorage Inner { get; } = new();
		public string Name => this.Inner.Name;
		public bool Disposable => false;
		public IConnectionSettings Settings
		{
			get => this.Inner.Settings;
			set => this.Inner.Settings = value;
		}
		public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public void Release() => _release.TrySetResult();

		public ValueTask<int> ClearAsync(CancellationToken cancellation = default) => this.Inner.ClearAsync(cancellation);
		public ValueTask<int> ClearAsync(string topic, CancellationToken cancellation = default) => this.Inner.ClearAsync(topic, cancellation);

		public async ValueTask SetAsync(Message message, TimeSpan expiry = default, CancellationToken cancellation = default)
		{
			this.Started.TrySetResult();
			await _release.Task.WaitAsync(cancellation);
			await this.Inner.SetAsync(message, expiry, cancellation);
		}

		public ValueTask<bool> RemoveAsync(string identifier, CancellationToken cancellation = default) =>
			this.Inner.RemoveAsync(identifier, cancellation);

		public IAsyncEnumerable<Message> GetAsync(CancellationToken cancellation = default) =>
			this.Inner.GetAsync(cancellation);

		public IAsyncEnumerable<Message> GetAsync(string topic, CancellationToken cancellation = default) =>
			this.Inner.GetAsync(topic, cancellation);
	}

	private sealed class RetryingRemoveMessageStorage : IMessageStorage
	{
		private int _removeAttempts;
		private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public MemoryMessageStorage Inner { get; } = new();
		public string Name => this.Inner.Name;
		public bool Disposable => false;
		public IConnectionSettings Settings
		{
			get => this.Inner.Settings;
			set => this.Inner.Settings = value;
		}
		public int RemoveAttempts => Volatile.Read(ref _removeAttempts);
		public TaskCompletionSource FirstRemoveFailed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource RetryStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public void ReleaseRetry() => _release.TrySetResult();

		public ValueTask<int> ClearAsync(CancellationToken cancellation = default) => this.Inner.ClearAsync(cancellation);
		public ValueTask<int> ClearAsync(string topic, CancellationToken cancellation = default) => this.Inner.ClearAsync(topic, cancellation);

		public ValueTask SetAsync(Message message, TimeSpan expiry = default, CancellationToken cancellation = default) =>
			this.Inner.SetAsync(message, expiry, cancellation);

		public async ValueTask<bool> RemoveAsync(string identifier, CancellationToken cancellation = default)
		{
			if(Interlocked.Increment(ref _removeAttempts) == 1)
			{
				this.FirstRemoveFailed.TrySetResult();
				throw new InvalidOperationException("First remove failure.");
			}

			this.RetryStarted.TrySetResult();
			await _release.Task.WaitAsync(cancellation);
			return await this.Inner.RemoveAsync(identifier, cancellation);
		}

		public IAsyncEnumerable<Message> GetAsync(CancellationToken cancellation = default) =>
			this.Inner.GetAsync(cancellation);

		public IAsyncEnumerable<Message> GetAsync(string topic, CancellationToken cancellation = default) =>
			this.Inner.GetAsync(topic, cancellation);
	}

	private sealed class ReliableServerScope : IAsyncDisposable
	{
		private readonly ZeroQueueServer _server;

		private ReliableServerScope(ZeroQueueServer server) => _server = server;

		public ushort Port => _server.Port;
		public MemoryMessageStorage Storage => _server.Storage as MemoryMessageStorage;

		public static async Task<ReliableServerScope> StartAsync(IMessageStorage storage = null)
		{
			var server = new ZeroQueueServer { Port = ZeroTestUtility.GetFreePort(), Storage = storage ?? new MemoryMessageStorage() };
			await server.StartAsync([]);
			return new ReliableServerScope(server);
		}

		public async Task RestartAsync()
		{
			await _server.StopAsync([]);
			await _server.StartAsync([]);
		}

		public Task StopAsync() => _server.StopAsync([]);
		public Task StartAsync() => _server.StartAsync([]);

		public async ValueTask DisposeAsync()
		{
			await _server.StopAsync([]);
			((IDisposable)_server).Dispose();
		}
	}
}
