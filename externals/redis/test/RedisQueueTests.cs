using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using StackExchange.Redis;

using Xunit;

using Zongsoft.Externals.Redis.Messaging;

namespace Zongsoft.Externals.Redis.Tests;

public class RedisQueueTests
{
	private const string REDIS_UNAVAILABLE = "Redis is unavailable at 127.0.0.1:6379.";

	[Fact]
	public async Task PublishAndConsumeMessage()
	{
		if(!RedisTestUtility.IsTestingEnabled)
			return;

		Assert.SkipUnless(RedisTestUtility.IsAvailable(), REDIS_UNAVAILABLE);

		var identity = Guid.NewGuid().ToString("N");
		var name = $"tests-{identity}";
		var topic = $"basic-{identity}";
		var group = $"group-{identity}";
		var key = RedisTestUtility.GetQueueKey(name, topic);

		using var administration = ConnectionMultiplexer.Connect($"{RedisTestUtility.Server},password={RedisTestUtility.Password}");
		var database = administration.GetDatabase();
		using var queue = RedisTestUtility.CreateQueue(name, group, $"client-{identity}");
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
		if(!RedisTestUtility.IsTestingEnabled)
			return;

		Assert.SkipUnless(RedisTestUtility.IsAvailable(), REDIS_UNAVAILABLE);

		var identity = Guid.NewGuid().ToString("N");
		var name = $"tests-{identity}";
		var topic = $"existing-{identity}";
		var group = $"group-{identity}";
		var key = RedisTestUtility.GetQueueKey(name, topic);

		using var administration = ConnectionMultiplexer.Connect($"{RedisTestUtility.Server},password={RedisTestUtility.Password}");
		var database = administration.GetDatabase();

		try
		{
			using(var firstQueue = RedisTestUtility.CreateQueue(name, group, $"first-{identity}"))
			using(var firstMessages = new RedisMessageBuffer())
			{
				var firstSubscriber = await firstQueue.SubscribeAsync(topic, firstMessages);
				await firstSubscriber.UnsubscribeAsync();
				await firstSubscriber.DisposeAsync();
			}

			using var secondQueue = RedisTestUtility.CreateQueue(name, group, $"second-{identity}");
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
		if(!RedisTestUtility.IsTestingEnabled)
			return;

		Assert.SkipUnless(RedisTestUtility.IsAvailable(), REDIS_UNAVAILABLE);

		var identity = Guid.NewGuid().ToString("N");
		var name = $"tests-{identity}";
		var topic = $"broadcast-{identity}";
		var key = RedisTestUtility.GetQueueKey(name, topic);

		using var administration = ConnectionMultiplexer.Connect($"{RedisTestUtility.Server},password={RedisTestUtility.Password}");
		var database = administration.GetDatabase();
		using var queue = RedisTestUtility.CreateQueue(name, client: $"client-{identity}");
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
		if(!RedisTestUtility.IsTestingEnabled)
			return;

		Assert.SkipUnless(RedisTestUtility.IsAvailable(), REDIS_UNAVAILABLE);

		var key = $"tests:exact:{Guid.NewGuid():N}";
		using var connection = ConnectionMultiplexer.Connect($"{RedisTestUtility.Server},password={RedisTestUtility.Password}");
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
			queue.Dispose();
			Assert.True(await database.PingAsync() >= TimeSpan.Zero);
		}
		finally
		{
			queue.Dispose();
			await database.KeyDeleteAsync(key);
		}
	}

	[Fact]
	public void QueueRetentionProperties_DefaultAndConfiguredValuesAreHonored()
	{
		if(!RedisTestUtility.IsTestingEnabled)
			return;

		Assert.SkipUnless(RedisTestUtility.IsAvailable(), REDIS_UNAVAILABLE);

		var key = $"tests:retention:settings:{Guid.NewGuid():N}";
		var settings = Configuration.RedisConnectionSettingsDriver.Instance.GetSettings("retention",
			$"server={RedisTestUtility.Server};password={RedisTestUtility.Password};maximumLength=7;useApproximateMaximumLength=false;");

		using var connection = ConnectionMultiplexer.Connect($"{RedisTestUtility.Server},password={RedisTestUtility.Password}");
		var database = connection.GetDatabase();
		using var defaults = new RedisQueue(key, database);
		using var configured = new RedisQueue(key, database, settings);

		Assert.Equal(100000, defaults.MaximumLength);
		Assert.True(defaults.UseApproximateMaximumLength);
		Assert.Equal(7, settings.MaximumLength);
		Assert.False(settings.UseApproximateMaximumLength);
		Assert.Equal(7, configured.MaximumLength);
		Assert.False(configured.UseApproximateMaximumLength);

		settings.MaximumLength = 0;
		using var fallback = new RedisQueue(key, database, settings);
		Assert.Equal(100000, fallback.MaximumLength);
	}

	[Fact]
	public async Task ProduceAsync_ExactMaximumLengthBoundsStream()
	{
		if(!RedisTestUtility.IsTestingEnabled)
			return;

		Assert.SkipUnless(RedisTestUtility.IsAvailable(), REDIS_UNAVAILABLE);

		var key = $"tests:retention:exact:{Guid.NewGuid():N}";
		using var connection = ConnectionMultiplexer.Connect($"{RedisTestUtility.Server},password={RedisTestUtility.Password}");
		var database = connection.GetDatabase();
		using var queue = new RedisQueue(key, database)
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
		if(!RedisTestUtility.IsTestingEnabled)
			return;

		Assert.SkipUnless(RedisTestUtility.IsAvailable(), REDIS_UNAVAILABLE);

		var identity = Guid.NewGuid().ToString("N");
		var name = hasHashTag ? $"{{tests-{identity}}}" : $"tests-{identity}";
		var topic = $"dead-{identity}";
		var group = $"group-{identity}";
		var key = RedisTestUtility.GetQueueKey(name, topic);
		var deadKey = hasHashTag ? $"{key}:DEAD!" : $"{{{key}}}:DEAD!";

		using var administration = ConnectionMultiplexer.Connect($"{RedisTestUtility.Server},password={RedisTestUtility.Password}");
		var database = administration.GetDatabase();
		using var queue = RedisTestUtility.CreateQueue(name, group, $"client-{identity}", 2, "1s");
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
	public void FactoryCreatesRedisQueue()
	{
		if(!RedisTestUtility.IsTestingEnabled)
			return;

		Assert.SkipUnless(RedisTestUtility.IsAvailable(), REDIS_UNAVAILABLE);

		var factory = new RedisQueueFactory();
		using var queue = factory.Create("factory", $"server={RedisTestUtility.Server};password={RedisTestUtility.Password};group=factory;client=factory;");

		Assert.Equal("Redis", factory.Name);
		Assert.IsType<RedisQueue>(queue);
		Assert.Equal("factory", queue.Name);
	}
}
