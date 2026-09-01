using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using StackExchange.Redis;

using Zongsoft.Messaging;
using Zongsoft.Externals.Redis.Messaging;
using Zongsoft.Externals.Redis.Configuration;

using Xunit;

namespace Zongsoft.Externals.Redis.Tests.MessageStorages;

[Collection(RedisMessageStorageCollection.Name)]
public sealed class RedisMessageStorageIntegrationTests
{
	private const string REDIS_UNAVAILABLE = "Redis is unavailable at 127.0.0.1:6379.";
	private const string TESTS_DISABLED = "Redis integration tests require ZONGSOFT_REDIS_TESTS=1.";

	[Fact]
	public async Task SetAndGet_RoundTripsIndependentMetadataNullEmptyPayloadsAndOverwrite()
	{
		EnsureRedis();
		await using var storage = CreateStorage(out _);

		try
		{
			var acknowledged = 0;
			var timestamp = new DateTime(2026, 9, 1, 8, 9, 10, DateTimeKind.Unspecified);
			var data = new byte[] { 1, 2, 3 };
			var original = new Message("same", "orders", data, "old", () => acknowledged++)
			{
				Identity = "producer-a",
				Timestamp = timestamp,
			};

			await storage.SetAsync(original, TimeSpan.FromMinutes(1));
			data[0] = 99;
			await storage.SetAsync(new Message("same", "Orders", [9, 8])
			{
				Identity = "producer-b",
				Tags = "new",
				Timestamp = timestamp.AddMinutes(1),
			});
			await storage.SetAsync(CreateMessage("null", string.Empty, null));
			await storage.SetAsync(CreateMessage("empty", string.Empty, []));

			var messages = await ReadAsync(storage.GetAsync());
			Assert.Equal(3, messages.Length);
			var replaced = Find(messages, "same");
			Assert.Equal("Orders", replaced.Topic);
			Assert.Equal("producer-b", replaced.Identity);
			Assert.Equal("new", replaced.Tags);
			Assert.Equal(DateTime.SpecifyKind(timestamp.AddMinutes(1), DateTimeKind.Utc), replaced.Timestamp);
			Assert.Equal([9, 8], replaced.Data);
			Assert.Null(Find(messages, "null").Data);
			Assert.Empty(Find(messages, "empty").Data);

			replaced.Acknowledge();
			Assert.Equal(0, acknowledged);
		}
		finally
		{
			await storage.ClearAsync();
		}
	}

	[Fact]
	public async Task TopicQueriesAndClear_UseOrdinalExactMatchingAndActualCounts()
	{
		EnsureRedis();
		await using var storage = CreateStorage(out _);

		try
		{
			await storage.SetAsync(CreateMessage("exact-1", "orders", [1]));
			await storage.SetAsync(CreateMessage("exact-2", "orders", [2]));
			await storage.SetAsync(CreateMessage("case", "Orders", [3]));
			await storage.SetAsync(CreateMessage("child", "orders/created", [4]));
			await storage.SetAsync(CreateMessage("default", string.Empty, [5]));

			Assert.Equal(2, (await ReadAsync(storage.GetAsync("orders"))).Length);
			Assert.Equal("case", Assert.Single(await ReadAsync(storage.GetAsync("Orders"))).Identifier);
			Assert.Equal("default", Assert.Single(await ReadAsync(storage.GetAsync(string.Empty))).Identifier);
			Assert.Empty(await ReadAsync(storage.GetAsync("ORDERs")));

			Assert.True(await storage.RemoveAsync("child"));
			Assert.False(await storage.RemoveAsync("child"));
			Assert.False(await storage.RemoveAsync("missing"));
			Assert.Equal(2, await storage.ClearAsync("orders"));
			Assert.Equal(0, await storage.ClearAsync("orders"));
			Assert.Equal(2, await storage.ClearAsync());
			Assert.Empty(await ReadAsync(storage.GetAsync()));
		}
		finally
		{
			await storage.ClearAsync();
		}
	}

	[Fact]
	public async Task ClearAsync_MoreThanOneBatchReturnsActualDeletionCount()
	{
		EnsureRedis();
		await using var storage = CreateStorage(out _);

		try
		{
			for(int i = 0; i < 101; i++)
				await storage.SetAsync(CreateMessage($"batch-{i}", "batch", [(byte)i]));

			Assert.Equal(101, await storage.ClearAsync("batch"));
			Assert.Equal(0, await storage.ClearAsync("batch"));
		}
		finally
		{
			await storage.ClearAsync();
		}
	}

	[Fact]
	public async Task Expiry_PositiveExpiresWhileZeroAndNegativeRemainPermanent()
	{
		EnsureRedis();
		await using var storage = CreateStorage(out _);

		try
		{
			await storage.SetAsync(CreateMessage("expiring", "ttl", [1]), TimeSpan.FromMilliseconds(100));
			await storage.SetAsync(CreateMessage("zero", "ttl", [2]), TimeSpan.Zero);
			await storage.SetAsync(CreateMessage("negative", "ttl", [3]), TimeSpan.FromSeconds(-1));
			await storage.SetAsync(CreateMessage("renewed", "ttl", [4]), TimeSpan.FromMilliseconds(100));
			await storage.SetAsync(CreateMessage("renewed", "ttl", [5]), TimeSpan.Zero);
			await Task.Delay(TimeSpan.FromMilliseconds(500));

			var messages = await ReadAsync(storage.GetAsync("ttl"));
			Assert.Equal(3, messages.Length);
			Assert.DoesNotContain(messages, message => message.Identifier == "expiring");
			Assert.Contains(messages, message => message.Identifier == "zero");
			Assert.Contains(messages, message => message.Identifier == "negative");
			Assert.Equal([5], Find(messages, "renewed").Data);
		}
		finally
		{
			await storage.ClearAsync();
		}
	}

	[Fact]
	public async Task Namespace_ExplicitAndFallbackScopesIsolateIdentifiersAndEscapePatterns()
	{
		EnsureRedis();
		var sharedName = $"shared-{Guid.NewGuid():N}";
		var wildcardNamespace = $"tenant[*]?-{Guid.NewGuid():N}";
		await using var first = CreateStorage(sharedName, wildcardNamespace);
		await using var second = CreateStorage(sharedName, $"tenant-b-{Guid.NewGuid():N}");
		await using var fallbackWriter = CreateStorage(sharedName, null);
		await using var fallbackReader = CreateStorage(sharedName, null);

		try
		{
			await first.SetAsync(CreateMessage("same", "orders", [1]));
			await second.SetAsync(CreateMessage("same", "orders", [2]));
			await fallbackWriter.SetAsync(CreateMessage("fallback", "orders", [3]));

			Assert.Equal([1], Assert.Single(await ReadAsync(first.GetAsync())).Data);
			Assert.Equal([2], Assert.Single(await ReadAsync(second.GetAsync())).Data);
			Assert.Equal([3], Assert.Single(await ReadAsync(fallbackReader.GetAsync())).Data);
			Assert.Equal(1, await first.ClearAsync());
			Assert.Empty(await ReadAsync(first.GetAsync()));
			Assert.Single(await ReadAsync(second.GetAsync()));
		}
		finally
		{
			await first.ClearAsync();
			await second.ClearAsync();
			await fallbackWriter.ClearAsync();
		}
	}

	[Theory]
	[InlineData("not-json")]
	[InlineData("{\"version\":2,\"identifier\":\"corrupt\",\"topic\":\"topic\"}")]
	public async Task GetAsync_MalformedOrUnsupportedRecordThrowsInvalidDataException(string payload)
	{
		EnsureRedis();
		await using var storage = CreateStorage(out var settings);
		await using var lease = await RedisConnectionPool.AcquireAsync(settings.GetOptions());
		var database = lease.Connection.GetDatabase(settings.Database);
		var key = $"Zongsoft.Messaging.Storage:{settings.Namespace}:corrupt";
		await database.StringSetAsync(key, payload);

		try
		{
			await Assert.ThrowsAsync<InvalidDataException>(async () => await ReadAsync(storage.GetAsync()));
		}
		finally
		{
			await database.KeyDeleteAsync(key);
		}
	}

	[Fact]
	public async Task Activation_FreezesSettingsAndDisposeReleasesOwnedPoolLease()
	{
		EnsureRedis();
		var client = $"storage-client-{Guid.NewGuid():N}";
		var settings = CreateSettings($"owned-{Guid.NewGuid():N}", $"owned-{Guid.NewGuid():N}", client);
		var storage = new RedisMessageStorage(settings);
		await storage.SetAsync(CreateMessage("owned", "lifecycle", [1]));

		var field = typeof(RedisMessageStorage).GetField("_lease", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
		var lease = Assert.IsType<RedisConnectionLease>(field.GetValue(storage));
		var connection = lease.Connection;

		Assert.Throws<InvalidOperationException>(() => storage.Settings = CreateSettings("replacement", "replacement"));
		settings.Namespace = "changed-after-activation";
		Assert.Equal("owned", Assert.Single(await ReadAsync(storage.GetAsync())).Identifier);
		Assert.True(connection.IsConnected);
		await storage.ClearAsync();
		await storage.DisposeAsync();
		Assert.False(connection.IsConnected);
		await Assert.ThrowsAsync<ObjectDisposedException>(async () => await storage.RemoveAsync("owned"));
	}

	private static RedisMessageStorage CreateStorage(out RedisConnectionSettings settings)
	{
		settings = CreateSettings($"storage-{Guid.NewGuid():N}", $"namespace-{Guid.NewGuid():N}");
		return new RedisMessageStorage(settings);
	}

	private static RedisMessageStorage CreateStorage(string name, string @namespace) => new(CreateSettings(name, @namespace));
	private static RedisConnectionSettings CreateSettings(string name, string @namespace, string client = null)
	{
		var text = $"server={Global.Server};password={Global.Password};timeout=5s";
		if(!string.IsNullOrEmpty(@namespace))
			text += $";namespace={@namespace}";
		if(!string.IsNullOrEmpty(client))
			text += $";client={client}";

		return RedisConnectionSettingsDriver.Instance.GetSettings(name, text);
	}

	private static Message CreateMessage(string identifier, string topic, byte[] data) => new(identifier, topic, data) { Timestamp = DateTime.UtcNow };

	private static async Task<Message[]> ReadAsync(IAsyncEnumerable<Message> source, CancellationToken cancellation = default)
	{
		var messages = new List<Message>();
		await foreach(var message in source.WithCancellation(cancellation))
			messages.Add(message);

		return [.. messages];
	}

	private static Message Find(IEnumerable<Message> messages, string identifier)
	{
		foreach(var message in messages)
		{
			if(string.Equals(message.Identifier, identifier, StringComparison.Ordinal))
				return message;
		}

		throw new Xunit.Sdk.XunitException($"The message '{identifier}' was not found.");
	}

	private static void EnsureRedis()
	{
		Assert.SkipUnless(Global.IsTestingEnabled, TESTS_DISABLED);
		Assert.SkipUnless(Global.IsAvailable(), REDIS_UNAVAILABLE);
	}
}
