using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

using StackExchange.Redis;

using Zongsoft.Messaging;
using Zongsoft.Components;
using Zongsoft.Collections;

namespace Zongsoft.Externals.Redis.Messaging.Tests;

public class RedisQueueTest
{
	private const string REDIS_UNAVAILABLE = "Redis is unavailable at 127.0.0.1:6379.";

	[Fact]
	public async Task PublishAndConsumeMessage()
	{
		if(!global::Zongsoft.Externals.Redis.Tests.Global.IsTestingEnabled)
			return;

		Assert.SkipUnless(global::Zongsoft.Externals.Redis.Tests.Global.IsAvailable(), REDIS_UNAVAILABLE);

		var identity = Guid.NewGuid().ToString("N");
		var name = $"tests-{identity}";
		var topic = $"basic-{identity}";
		var group = $"group-{identity}";
		var key = global::Zongsoft.Externals.Redis.Tests.RedisTestUtility.GetQueueKey(name, topic);

		using var administration = ConnectionMultiplexer.Connect($"{(global::Zongsoft.Externals.Redis.Tests.Global.Server)},password={(global::Zongsoft.Externals.Redis.Tests.Global.Password)}");
		var database = administration.GetDatabase();
		await using var queue = global::Zongsoft.Externals.Redis.Tests.RedisTestUtility.CreateQueue(name, group, $"client-{identity}");
		using var messages = new RedisMessageBuffer();
		RedisSubscriber subscriber = null;

		try
		{
			subscriber = await queue.SubscribeAsync(topic, "alpha;beta", messages);
			Assert.NotNull(subscriber);
			Assert.Equal(["alpha", "beta"], subscriber.Tags);

			var before = DateTime.UtcNow;
			var identifier = await queue.ProduceAsync(topic, "alpha;beta", Encoding.UTF8.GetBytes("Hello Redis Streams"));
			var received = await messages.ReceiveAsync(TimeSpan.FromSeconds(10));

			Assert.False(string.IsNullOrEmpty(identifier));
			Assert.True(received.HasValue);
			Assert.Equal(identifier, received.Value.Identifier);
			Assert.Equal(topic, received.Value.Topic);
			Assert.Equal("alpha;beta", received.Value.Tags);
			Assert.Equal("Hello Redis Streams", Encoding.UTF8.GetString(received.Value.Data));
			Assert.InRange(received.Value.Timestamp, before.AddSeconds(-1), DateTime.UtcNow.AddSeconds(1));
			Assert.Equal(1, messages.AcknowledgementCount);
			Assert.Equal(0, (await database.StreamPendingAsync(key, group)).PendingMessageCount);

			await subscriber.UnsubscribeAsync();
			Assert.Empty(queue.Subscribers);
		}
		finally
		{
			if(subscriber != null)
				await subscriber.DisposeAsync();

			await database.KeyDeleteAsync(key);
		}
	}

	[Fact]
	public async Task ExistingConsumerGroupCanSubscribeAgain()
	{
		if(!global::Zongsoft.Externals.Redis.Tests.Global.IsTestingEnabled)
			return;

		Assert.SkipUnless(global::Zongsoft.Externals.Redis.Tests.Global.IsAvailable(), REDIS_UNAVAILABLE);

		var identity = Guid.NewGuid().ToString("N");
		var name = $"tests-{identity}";
		var topic = $"existing-{identity}";
		var group = $"group-{identity}";
		var key = global::Zongsoft.Externals.Redis.Tests.RedisTestUtility.GetQueueKey(name, topic);

		using var administration = ConnectionMultiplexer.Connect($"{(global::Zongsoft.Externals.Redis.Tests.Global.Server)},password={(global::Zongsoft.Externals.Redis.Tests.Global.Password)}");
		var database = administration.GetDatabase();

		try
		{
			await using(var firstQueue = global::Zongsoft.Externals.Redis.Tests.RedisTestUtility.CreateQueue(name, group, $"first-{identity}"))
			using(var firstMessages = new RedisMessageBuffer())
			{
				var firstSubscriber = await firstQueue.SubscribeAsync(topic, firstMessages);
				await firstSubscriber.UnsubscribeAsync();
				await firstSubscriber.DisposeAsync();
			}

			await using var secondQueue = global::Zongsoft.Externals.Redis.Tests.RedisTestUtility.CreateQueue(name, group, $"second-{identity}");
			using var secondMessages = new RedisMessageBuffer();
			var secondSubscriber = await secondQueue.SubscribeAsync(topic, secondMessages);
			var identifier = await secondQueue.ProduceAsync(topic, Encoding.UTF8.GetBytes("existing group"));
			var received = await secondMessages.ReceiveAsync(TimeSpan.FromSeconds(10));

			Assert.True(received.HasValue);
			Assert.Equal(identifier, received.Value.Identifier);
			Assert.Equal("existing group", Encoding.UTF8.GetString(received.Value.Data));

			await secondSubscriber.DisposeAsync();
			Assert.Empty(secondQueue.Subscribers);
		}
		finally
		{
			await database.KeyDeleteAsync(key);
		}
	}

	[Fact]
	public async Task BroadcastSubscriptionDoesNotRequireConsumerGroup()
	{
		if(!global::Zongsoft.Externals.Redis.Tests.Global.IsTestingEnabled)
			return;

		Assert.SkipUnless(global::Zongsoft.Externals.Redis.Tests.Global.IsAvailable(), REDIS_UNAVAILABLE);

		var identity = Guid.NewGuid().ToString("N");
		var name = $"tests-{identity}";
		var topic = $"broadcast-{identity}";
		var key = global::Zongsoft.Externals.Redis.Tests.RedisTestUtility.GetQueueKey(name, topic);

		using var administration = ConnectionMultiplexer.Connect($"{(global::Zongsoft.Externals.Redis.Tests.Global.Server)},password={(global::Zongsoft.Externals.Redis.Tests.Global.Password)}");
		var database = administration.GetDatabase();
		await using var queue = global::Zongsoft.Externals.Redis.Tests.RedisTestUtility.CreateQueue(name, client: $"client-{identity}");
		using var messages = new RedisMessageBuffer();
		RedisSubscriber subscriber = null;

		try
		{
			subscriber = await queue.SubscribeAsync(topic, messages);
			var identifier = await queue.ProduceAsync(topic, "broadcast", Encoding.UTF8.GetBytes("without group"));
			var received = await messages.ReceiveAsync(TimeSpan.FromSeconds(10));

			Assert.True(received.HasValue);
			Assert.Equal(identifier, received.Value.Identifier);
			Assert.Equal("broadcast", received.Value.Tags);
			Assert.Equal("without group", Encoding.UTF8.GetString(received.Value.Data));
			Assert.Equal(1, messages.AcknowledgementCount);
			Assert.Empty(await database.StreamGroupInfoAsync(key));
		}
		finally
		{
			if(subscriber != null)
				await subscriber.DisposeAsync();

			await database.KeyDeleteAsync(key);
		}
	}

	[Fact]
	public async Task InjectedDatabaseUsesExactStreamAndRemainsOwnedByCaller()
	{
		if(!global::Zongsoft.Externals.Redis.Tests.Global.IsTestingEnabled)
			return;

		Assert.SkipUnless(global::Zongsoft.Externals.Redis.Tests.Global.IsAvailable(), REDIS_UNAVAILABLE);

		var key = $"tests:exact:{Guid.NewGuid():N}";
		using var connection = ConnectionMultiplexer.Connect($"{(global::Zongsoft.Externals.Redis.Tests.Global.Server)},password={(global::Zongsoft.Externals.Redis.Tests.Global.Password)}");
		var database = connection.GetDatabase();
		using var messages = new RedisMessageBuffer();
		var identifier = await database.StreamAddAsync(key, "Data", "exact stream");
		var queue = new RedisQueue(key, database);

		try
		{
			var subscriber = await queue.SubscribeAsync(messages);
			var received = await messages.ReceiveAsync(TimeSpan.FromSeconds(10));

			Assert.True(received.HasValue);
			Assert.Equal((string)identifier, received.Value.Identifier);
			Assert.Equal("exact stream", Encoding.UTF8.GetString(received.Value.Data));

			await subscriber.DisposeAsync();
			await queue.DisposeAsync();
			Assert.True(await database.PingAsync() >= TimeSpan.Zero);
		}
		finally
		{
			await queue.DisposeAsync();
			await database.KeyDeleteAsync(key);
		}
	}

	[Fact]
	public async Task QueueRetentionProperties_DefaultAndConfiguredValuesAreHonored()
	{
		if(!global::Zongsoft.Externals.Redis.Tests.Global.IsTestingEnabled)
			return;

		Assert.SkipUnless(global::Zongsoft.Externals.Redis.Tests.Global.IsAvailable(), REDIS_UNAVAILABLE);

		var key = $"tests:retention:settings:{Guid.NewGuid():N}";
		var settings = Configuration.RedisConnectionSettingsDriver.Instance.GetSettings("retention",
			$"server={(global::Zongsoft.Externals.Redis.Tests.Global.Server)};password={(global::Zongsoft.Externals.Redis.Tests.Global.Password)};maximumLength=7;useApproximateMaximumLength=false;");

		using var connection = ConnectionMultiplexer.Connect($"{(global::Zongsoft.Externals.Redis.Tests.Global.Server)},password={(global::Zongsoft.Externals.Redis.Tests.Global.Password)}");
		var database = connection.GetDatabase();
		await using var defaults = new RedisQueue(key, database);
		await using var configured = new RedisQueue(key, database, settings);

		Assert.Equal(100000, defaults.MaximumLength);
		Assert.True(defaults.UseApproximateMaximumLength);
		Assert.Equal(7, settings.MaximumLength);
		Assert.False(settings.UseApproximateMaximumLength);
		Assert.Equal(7, configured.MaximumLength);
		Assert.False(configured.UseApproximateMaximumLength);

		settings.MaximumLength = 0;
		await using var fallback = new RedisQueue(key, database, settings);
		Assert.Equal(100000, fallback.MaximumLength);
	}

	[Fact]
	public async Task ProduceAsync_ExactMaximumLengthBoundsStream()
	{
		if(!global::Zongsoft.Externals.Redis.Tests.Global.IsTestingEnabled)
			return;

		Assert.SkipUnless(global::Zongsoft.Externals.Redis.Tests.Global.IsAvailable(), REDIS_UNAVAILABLE);

		var key = $"tests:retention:exact:{Guid.NewGuid():N}";
		using var connection = ConnectionMultiplexer.Connect($"{(global::Zongsoft.Externals.Redis.Tests.Global.Server)},password={(global::Zongsoft.Externals.Redis.Tests.Global.Password)}");
		var database = connection.GetDatabase();
		await using var queue = new RedisQueue(key, database)
		{
			MaximumLength = 3,
			UseApproximateMaximumLength = false,
		};

		try
		{
			for(var index = 0; index < 5; index++)
				await queue.ProduceAsync(Encoding.UTF8.GetBytes($"message-{index}"));

			var entries = await database.StreamRangeAsync(key);
			Assert.Equal(3, entries.Length);
			Assert.Equal(["message-2", "message-3", "message-4"],
				entries.Select(entry => Encoding.UTF8.GetString((byte[])entry.Values[0].Value)).ToArray());
		}
		finally
		{
			await database.KeyDeleteAsync(key);
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task UnacknowledgedMessageMovesToDeadLetterStream(bool hasHashTag)
	{
		if(!global::Zongsoft.Externals.Redis.Tests.Global.IsTestingEnabled)
			return;

		Assert.SkipUnless(global::Zongsoft.Externals.Redis.Tests.Global.IsAvailable(), REDIS_UNAVAILABLE);

		var identity = Guid.NewGuid().ToString("N");
		var name = hasHashTag ? $"{{tests-{identity}}}" : $"tests-{identity}";
		var topic = $"dead-{identity}";
		var group = $"group-{identity}";
		var key = global::Zongsoft.Externals.Redis.Tests.RedisTestUtility.GetQueueKey(name, topic);
		var deadKey = hasHashTag ? $"{key}:DEAD!" : $"{{{key}}}:DEAD!";

		using var administration = ConnectionMultiplexer.Connect($"{(global::Zongsoft.Externals.Redis.Tests.Global.Server)},password={(global::Zongsoft.Externals.Redis.Tests.Global.Password)}");
		var database = administration.GetDatabase();
		await using var queue = global::Zongsoft.Externals.Redis.Tests.RedisTestUtility.CreateQueue(name, group, $"client-{identity}", 2, "1s");
		using var messages = new RedisMessageBuffer(false);
		RedisSubscriber subscriber = null;

		try
		{
			subscriber = await queue.SubscribeAsync(topic, messages);
			await queue.ProduceAsync(topic, "dead-letter", Encoding.UTF8.GetBytes("unacknowledged"));
			Assert.True((await messages.ReceiveAsync(TimeSpan.FromSeconds(10))).HasValue);

			var timeout = DateTime.UtcNow.AddSeconds(20);
			while(!await database.KeyExistsAsync(deadKey) && DateTime.UtcNow < timeout)
				await Task.Delay(250);

			var entries = await database.StreamRangeAsync(deadKey);
			Assert.Single(entries);
			Assert.Equal("dead-letter", (string)entries[0].Values[1].Value);
			Assert.Equal("unacknowledged", Encoding.UTF8.GetString((byte[])entries[0].Values[0].Value));
			Assert.Equal(0, (await database.StreamPendingAsync(key, group)).PendingMessageCount);

			var sourceEntries = await database.StreamRangeAsync(key);
			if((queue.Capabilities & RedisCapabilities.StreamAcknowledgeAndDelete) != 0)
				Assert.Empty(sourceEntries);
			else
				Assert.Single(sourceEntries);
		}
		finally
		{
			if(subscriber != null)
				await subscriber.DisposeAsync();

			await database.KeyDeleteAsync([key, deadKey]);
		}
	}

	[Fact]
	public async Task FactoryCreatesRedisQueue()
	{
		if(!global::Zongsoft.Externals.Redis.Tests.Global.IsTestingEnabled)
			return;

		Assert.SkipUnless(global::Zongsoft.Externals.Redis.Tests.Global.IsAvailable(), REDIS_UNAVAILABLE);

		var factory = new RedisQueueFactory();
		await using var queue = factory.Create("factory", $"server={(global::Zongsoft.Externals.Redis.Tests.Global.Server)};password={(global::Zongsoft.Externals.Redis.Tests.Global.Password)};group=factory;client=factory;");

		Assert.Equal("Redis", factory.Name);
		Assert.IsType<RedisQueue>(queue);
		Assert.Equal("factory", queue.Name);
	}

	internal sealed class RedisMessageBuffer : HandlerBase<Zongsoft.Messaging.Message>, IDisposable
	{
		private readonly ConcurrentQueue<Zongsoft.Messaging.Message> _messages = new();
		private readonly SemaphoreSlim _signal = new(0);
		private readonly bool _acknowledge;
		private int _acknowledgementCount;

		public RedisMessageBuffer(bool acknowledge = true) => _acknowledge = acknowledge;

		public int AcknowledgementCount => _acknowledgementCount;

		public async Task<Zongsoft.Messaging.Message?> ReceiveAsync(TimeSpan timeout)
		{
			using var cancellation = new CancellationTokenSource(timeout);

			try
			{
				await _signal.WaitAsync(cancellation.Token);
			}
			catch(OperationCanceledException)
			{
				return null;
			}

			return _messages.TryDequeue(out var message) ? message : null;
		}

		public void Dispose() => _signal.Dispose();

		protected override async ValueTask OnHandleAsync(Zongsoft.Messaging.Message message, Collections.Parameters parameters, CancellationToken cancellation)
		{
			if(_acknowledge)
			{
				await message.AcknowledgeAsync(cancellation);
				Interlocked.Increment(ref _acknowledgementCount);
			}

			_messages.Enqueue(message);
			_signal.Release();
		}
	}
}

public class RedisQueueLifecycleTest
{
	private const string REDIS_UNAVAILABLE = "Redis is unavailable at 127.0.0.1:6379.";

	[Fact]
	public async Task CompressedPayloadRoundTripsThroughRedisStream()
	{
		if(!global::Zongsoft.Externals.Redis.Tests.Global.IsTestingEnabled)
			return;

		Assert.SkipUnless(global::Zongsoft.Externals.Redis.Tests.Global.IsAvailable(), REDIS_UNAVAILABLE);
		var identity = Guid.NewGuid().ToString("N");
		var name = $"tests-{identity}";
		var topic = $"compression-{identity}";
		var key = global::Zongsoft.Externals.Redis.Tests.RedisTestUtility.GetQueueKey(name, topic);
		var payload = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Repeat((byte)'A', 16 * 1024));
		using var administration = ConnectionMultiplexer.Connect($"{(global::Zongsoft.Externals.Redis.Tests.Global.Server)},password={(global::Zongsoft.Externals.Redis.Tests.Global.Password)}");
		var database = administration.GetDatabase();
		await using var queue = global::Zongsoft.Externals.Redis.Tests.RedisTestUtility.CreateQueue(name, $"group-{identity}", $"client-{identity}");
		var handler = new CaptureHandler();
		var subscriber = await queue.SubscribeAsync(topic, handler);

		try
		{
			var options = new MessageEnqueueOptions { Compression = new MessageCompression("GZip", 1) };
			await queue.ProduceAsync(topic, "kind:compressed", payload, options);
			var message = await handler.Completion.Task.WaitAsync(TimeSpan.FromSeconds(10));
			var entry = Assert.Single(await database.StreamRangeAsync(key));

			Assert.Equal(payload, message.Data);
			Assert.Equal("kind:compressed", message.Tags);
			Assert.Equal("GZip", (string)entry.GetMessageCompression());
			Assert.True(((byte[])entry.GetMessageData()).Length < payload.Length);
			await message.AcknowledgeAsync();
		}
		finally
		{
			await subscriber.DisposeAsync();
			await database.KeyDeleteAsync(key);
		}
	}

	[Fact]
	public async Task EquivalentQueues_ShareConnectionAndReleaseItAfterLastAsyncDispose()
	{
		if(!global::Zongsoft.Externals.Redis.Tests.Global.IsTestingEnabled)
			return;

		Assert.SkipUnless(global::Zongsoft.Externals.Redis.Tests.Global.IsAvailable(), REDIS_UNAVAILABLE);

		var identity = Guid.NewGuid().ToString("N");
		var name = $"tests-{identity}";
		var topic = $"shared-{identity}";
		var key = global::Zongsoft.Externals.Redis.Tests.RedisTestUtility.GetQueueKey(name, topic);
		var group = $"group-{identity}";
		var client = $"client-{identity}";

		using var administration = ConnectionMultiplexer.Connect($"{(global::Zongsoft.Externals.Redis.Tests.Global.Server)},password={(global::Zongsoft.Externals.Redis.Tests.Global.Password)}");
		var database = administration.GetDatabase();
		var first = global::Zongsoft.Externals.Redis.Tests.RedisTestUtility.CreateQueue(name, group, client);
		var second = global::Zongsoft.Externals.Redis.Tests.RedisTestUtility.CreateQueue(name, group, client);
		await first.ProduceAsync(topic, Encoding.UTF8.GetBytes("first"));
		await second.ProduceAsync(topic, Encoding.UTF8.GetBytes("second"));
		var connection = first.Database.Multiplexer;

		try
		{
			Assert.Same(connection, second.Database.Multiplexer);

			await first.DisposeAsync();
			Assert.True(connection.IsConnected);
			Assert.True(await second.Database.PingAsync() >= TimeSpan.Zero);

			var identifier = await second.ProduceAsync(topic, Encoding.UTF8.GetBytes("still-owned"));
			Assert.False(string.IsNullOrEmpty(identifier));

			await second.DisposeAsync();
			Assert.False(connection.IsConnected);
		}
		finally
		{
			await first.DisposeAsync();
			await second.DisposeAsync();
			await database.KeyDeleteAsync(key);
		}
	}

	[Fact]
	public async Task BlockedReceive_DisposeAsyncCancelsPromptlyAndStopsDelivery()
	{
		if(!global::Zongsoft.Externals.Redis.Tests.Global.IsTestingEnabled)
			return;

		Assert.SkipUnless(global::Zongsoft.Externals.Redis.Tests.Global.IsAvailable(), REDIS_UNAVAILABLE);

		var identity = Guid.NewGuid().ToString("N");
		var name = $"tests-{identity}";
		var topic = $"cancel-{identity}";
		var key = global::Zongsoft.Externals.Redis.Tests.RedisTestUtility.GetQueueKey(name, topic);

		using var administration = ConnectionMultiplexer.Connect($"{(global::Zongsoft.Externals.Redis.Tests.Global.Server)},password={(global::Zongsoft.Externals.Redis.Tests.Global.Password)}");
		var database = administration.GetDatabase();
		await using var queue = global::Zongsoft.Externals.Redis.Tests.RedisTestUtility.CreateQueue(name, $"group-{identity}", $"client-{identity}");
		var handler = new CountingHandler();
		var subscriber = await queue.SubscribeAsync(topic, handler);

		try
		{
			await Task.Delay(100);
			await subscriber.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(2));
			Assert.Empty(queue.Subscribers);

			await queue.ProduceAsync(topic, Encoding.UTF8.GetBytes("after-close"));
			await Task.Delay(250);
			Assert.Equal(0, handler.Count);
		}
		finally
		{
			await subscriber.DisposeAsync();
			await database.KeyDeleteAsync(key);
		}
	}

	[Fact]
	public async Task AsyncHandler_IsAwaitedAndMessagesAreDeliveredSerially()
	{
		if(!global::Zongsoft.Externals.Redis.Tests.Global.IsTestingEnabled)
			return;

		Assert.SkipUnless(global::Zongsoft.Externals.Redis.Tests.Global.IsAvailable(), REDIS_UNAVAILABLE);

		var identity = Guid.NewGuid().ToString("N");
		var name = $"tests-{identity}";
		var topic = $"serial-{identity}";
		var key = global::Zongsoft.Externals.Redis.Tests.RedisTestUtility.GetQueueKey(name, topic);

		using var administration = ConnectionMultiplexer.Connect($"{(global::Zongsoft.Externals.Redis.Tests.Global.Server)},password={(global::Zongsoft.Externals.Redis.Tests.Global.Password)}");
		var database = administration.GetDatabase();
		await using var queue = global::Zongsoft.Externals.Redis.Tests.RedisTestUtility.CreateQueue(name, $"group-{identity}", $"client-{identity}");
		var handler = new SerialHandler();
		var subscriber = await queue.SubscribeAsync(topic, handler);

		try
		{
			await queue.ProduceAsync(topic, Encoding.UTF8.GetBytes("first"));
			await queue.ProduceAsync(topic, Encoding.UTF8.GetBytes("second"));
			await handler.FirstStarted.WaitAsync(TimeSpan.FromSeconds(10));

			await Task.Delay(200);
			Assert.Equal(1, handler.Count);
			Assert.Equal(1, handler.MaximumConcurrency);

			handler.ReleaseFirst();
			await handler.BothCompleted.WaitAsync(TimeSpan.FromSeconds(10));
			Assert.Equal(2, handler.Count);
			Assert.Equal(1, handler.MaximumConcurrency);
		}
		finally
		{
			handler.ReleaseFirst();
			await subscriber.DisposeAsync();
			await database.KeyDeleteAsync(key);
		}
	}

	private sealed class CountingHandler : HandlerBase<Message>
	{
		private int _count;
		public int Count => _count;

		protected override ValueTask OnHandleAsync(Message message, Parameters parameters, CancellationToken cancellation)
		{
			Interlocked.Increment(ref _count);
			return ValueTask.CompletedTask;
		}
	}

	private sealed class CaptureHandler : HandlerBase<Message>
	{
		public TaskCompletionSource<Message> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		protected override ValueTask OnHandleAsync(Message message, Parameters parameters, CancellationToken cancellation)
		{
			this.Completion.TrySetResult(message);
			return ValueTask.CompletedTask;
		}
	}

	private sealed class SerialHandler : HandlerBase<Message>
	{
		private readonly TaskCompletionSource<bool> _firstStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
		private readonly TaskCompletionSource<bool> _releaseFirst = new(TaskCreationOptions.RunContinuationsAsynchronously);
		private readonly TaskCompletionSource<bool> _bothCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);
		private int _active;
		private int _count;
		private int _maximumConcurrency;

		public int Count => Volatile.Read(ref _count);
		public int MaximumConcurrency => Volatile.Read(ref _maximumConcurrency);
		public Task FirstStarted => _firstStarted.Task;
		public Task BothCompleted => _bothCompleted.Task;

		public void ReleaseFirst() => _releaseFirst.TrySetResult(true);

		protected override async ValueTask OnHandleAsync(Message message, Parameters parameters, CancellationToken cancellation)
		{
			var active = Interlocked.Increment(ref _active);
			InterlockedExtensions.Max(ref _maximumConcurrency, active);
			var count = Interlocked.Increment(ref _count);

			try
			{
				if(count == 1)
				{
					_firstStarted.TrySetResult(true);
					await _releaseFirst.Task.WaitAsync(cancellation);
				}

				await message.AcknowledgeAsync(cancellation);
				if(count == 2)
					_bothCompleted.TrySetResult(true);
			}
			finally
			{
				Interlocked.Decrement(ref _active);
			}
		}
	}

	private static class InterlockedExtensions
	{
		public static void Max(ref int location, int value)
		{
			var current = Volatile.Read(ref location);
			while(current < value)
			{
				var previous = Interlocked.CompareExchange(ref location, value, current);
				if(previous == current)
					return;

				current = previous;
			}
		}
	}
}
