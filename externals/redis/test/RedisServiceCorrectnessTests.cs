using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

using StackExchange.Redis;

using Xunit;

using Zongsoft.Caching;
using Zongsoft.Common;

namespace Zongsoft.Externals.Redis.Tests;

public class RedisServiceCorrectnessTests
{
	private const string REDIS_UNAVAILABLE = "Redis is unavailable at 127.0.0.1:6379.";
	private const string TESTS_DISABLED = "Redis integration tests require ZONGSOFT_REDIS_TESTS=1.";

	[Fact]
	public async Task Namespace_IsolatesEntryFactoriesAndNullDeletion()
	{
		EnsureRedis();

		await using var cache = CreateCache(out var cacheNamespace);
		using var connection = CreateConnection();
		var database = connection.GetDatabase();
		var token = Guid.NewGuid().ToString("N");
		var valueKey = $"value-{token}";
		var dictionaryKey = $"dictionary-{token}";
		var setKey = $"set-{token}";
		var scopedValueKey = $"{cacheNamespace}:{valueKey}";
		var scopedDictionaryKey = $"{cacheNamespace}:{dictionaryKey}";
		var scopedSetKey = $"{cacheNamespace}:{setKey}";

		try
		{
			await database.StringSetAsync(valueKey, "unscoped");
			await database.HashSetAsync(dictionaryKey, "field", "unscoped");
			await database.SetAddAsync(setKey, "unscoped");

			Assert.True(cache.SetEntry(valueKey, "scoped", TimeSpan.Zero));
			var dictionary = cache.CreateDictionary(dictionaryKey);
			dictionary["field"] = "scoped";
			var set = cache.CreateHashset(setKey);
			set.Add("scoped");

			Assert.Equal("unscoped", (string)await database.StringGetAsync(valueKey));
			Assert.Equal("scoped", (string)await database.StringGetAsync(scopedValueKey));
			Assert.Equal("unscoped", (string)await database.HashGetAsync(dictionaryKey, "field"));
			Assert.Equal("scoped", (string)await database.HashGetAsync(scopedDictionaryKey, "field"));
			Assert.True(await database.SetContainsAsync(setKey, "unscoped"));
			Assert.True(await database.SetContainsAsync(scopedSetKey, "scoped"));

			Assert.True(cache.SetEntry(valueKey, null, TimeSpan.Zero));
			Assert.True(await database.KeyExistsAsync(valueKey));
			Assert.False(await database.KeyExistsAsync(scopedValueKey));
		}
		finally
		{
			await database.KeyDeleteAsync([valueKey, dictionaryKey, setKey, scopedValueKey, scopedDictionaryKey, scopedSetKey]);
		}
	}

	[Fact]
	public async Task SetExpiry_Zero_PersistsSynchronouslyAndAsynchronously()
	{
		EnsureRedis();

		await using var cache = CreateCache(out _);
		Assert.True(cache.SetValue("sync", "value", TimeSpan.FromMinutes(1)));
		Assert.NotNull(cache.GetExpiry("sync"));

		Assert.True(cache.SetExpiry("sync", TimeSpan.Zero));
		Assert.True(cache.Exists("sync"));
		Assert.Null(cache.GetExpiry("sync"));

		Assert.True(await cache.SetValueAsync("async", "value", TimeSpan.FromMinutes(1)));
		Assert.NotNull(await cache.GetExpiryAsync("async"));

		Assert.True(await cache.SetExpiryAsync("async", TimeSpan.Zero));
		Assert.True(await cache.ExistsAsync("async"));
		Assert.Null(await cache.GetExpiryAsync("async"));
	}

	[Fact]
	public async Task Sequence_SubsecondExpiryAndResetWithoutExpiry_PreserveExpectedLifetime()
	{
		EnsureRedis();

		await using var cache = CreateCache(out _);
		var sequence = (ISequence)cache;
		var expiry = TimeSpan.FromMilliseconds(800);

		Assert.Equal(6, sequence.Increase("sequence", interval: 1, seed: 5, expiry: expiry));
		var remaining = cache.GetExpiry("sequence");
		Assert.NotNull(remaining);
		Assert.InRange(remaining.Value, TimeSpan.Zero, expiry);

		sequence.Reset("sequence", 9, TimeSpan.Zero);
		Assert.Equal(9, await sequence.IncreaseAsync("sequence", interval: 0));
		Assert.Null(await cache.GetExpiryAsync("sequence"));

		Assert.Equal(1, await sequence.IncreaseAsync("sequence-async", interval: 1, expiry: expiry));
		Assert.NotNull(await cache.GetExpiryAsync("sequence-async"));
		await sequence.ResetAsync("sequence-async", 13, expiry: null);
		Assert.Equal(13, await sequence.IncreaseAsync("sequence-async", interval: 0));
		Assert.Null(cache.GetExpiry("sequence-async"));
	}

	[Fact]
	public async Task CountFindAndClear_AreLimitedToNamespaceAndReturnLogicalKeys()
	{
		EnsureRedis();

		await using var cache = CreateCache(out var cacheNamespace);
		using var connection = CreateConnection();
		var database = connection.GetDatabase();
		var unrelated = $"unrelated-{Guid.NewGuid():N}";

		try
		{
			await database.StringSetAsync(unrelated, "keep");
			Assert.True(await cache.SetValueAsync("alpha:one", "1"));
			Assert.True(await cache.SetValueAsync("alpha:two", "2"));
			Assert.True(await cache.SetValueAsync("beta", "3"));

			Assert.Equal(3, cache.GetCount());
			Assert.Equal(3, await cache.GetCountAsync());
			Assert.Equal(["alpha:one", "alpha:two"], cache.Find("alpha:*").Order().ToArray());

			var found = new List<string>();
			await foreach(var key in cache.FindAsync("alpha:*"))
				found.Add(key);

			Assert.Equal(["alpha:one", "alpha:two"], found.Order().ToArray());

			await cache.ClearAsync();
			Assert.Equal(0, await cache.GetCountAsync());
			Assert.Empty(cache.Find("*"));
			Assert.True(await database.KeyExistsAsync(unrelated));
			Assert.Empty(connection.GetServer(database.IdentifyEndpoint()).Keys(database.Database, $"{cacheNamespace}:*"));
		}
		finally
		{
			await database.KeyDeleteAsync(unrelated);
		}
	}

	[Fact]
	public async Task TryGetValue_MissingPermanentAndExpiringValues_DistinguishesExistence()
	{
		EnsureRedis();

		await using var cache = CreateCache(out _);

		Assert.False(cache.TryGetValue<string>("missing", out var missing, out var missingExpiry));
		Assert.Null(missing);
		Assert.Null(missingExpiry);

		Assert.True(cache.SetValue("permanent", "42"));
		Assert.True(cache.TryGetValue<int>("permanent", out var permanent, out var permanentExpiry));
		Assert.Equal(42, permanent);
		Assert.Null(permanentExpiry);

		Assert.True(cache.SetValue("expiring", "84", TimeSpan.FromMinutes(1)));
		Assert.True(cache.TryGetValue<int>("expiring", out var expiring, out var expiringExpiry));
		Assert.Equal(84, expiring);
		Assert.NotNull(expiringExpiry);
		Assert.InRange(expiringExpiry.Value, TimeSpan.Zero, TimeSpan.FromMinutes(1));

		Assert.False(cache.TryGetValue<ISet<string>>("missing-set", out var missingSet));
		Assert.Null(missingSet);
		Assert.False(cache.TryGetValue<IDictionary<string, string>>("missing-dictionary", out var missingDictionary));
		Assert.Null(missingDictionary);

		Assert.True(cache.SetValue("set", new HashSet<string> { "alpha" }));
		Assert.True(cache.TryGetValue<ISet<string>>("set", out var set));
		Assert.Equal(["alpha"], set.Order().ToArray());
		Assert.True(cache.SetValue("dictionary", new Dictionary<string, string> { ["field"] = "value" }));
		Assert.True(cache.TryGetValue<IDictionary<string, string>>("dictionary", out var dictionary));
		Assert.Equal("value", dictionary["field"]);
		var missingSetAsync = await cache.TryGetValueAsync<ISet<string>>("missing-set-async");
		Assert.False(missingSetAsync.result);
		Assert.Null(missingSetAsync.value);
		var setAsync = await cache.TryGetValueAsync<ISet<string>>("set");
		Assert.True(setAsync.result);
		Assert.Equal(["alpha"], setAsync.value.Order().ToArray());
		var dictionaryAsync = await cache.TryGetValueAsync<IDictionary<string, string>>("dictionary");
		Assert.True(dictionaryAsync.result);
		Assert.Equal("value", dictionaryAsync.value["field"]);

		var missingAsync = await cache.TryGetValueAsync<int>("missing-async");
		Assert.False(missingAsync.result);
		Assert.Equal(0, missingAsync.value);
		var permanentAsync = await cache.TryGetValueAsync<int>("permanent");
		Assert.True(permanentAsync.result);
		Assert.Equal(42, permanentAsync.value);
	}

	[Fact]
	public async Task UntypedReads_RoundTripDictionarySetAndListWithoutWrongTypeErrors()
	{
		EnsureRedis();

		await using var cache = CreateCache(out var cacheNamespace);
		using var connection = CreateConnection();
		var database = connection.GetDatabase();
		Assert.True(cache.SetValue("dictionary", new Dictionary<string, string> { ["field"] = "value" }));
		Assert.True(cache.SetValue("set", new HashSet<string> { "alpha", "beta" }));
		Assert.True(cache.SetValue("list", new List<string> { "first", "second" }));
		await database.SortedSetAddAsync($"{cacheNamespace}:sorted", [new("beta", 2), new("alpha", 1)]);

		var dictionary = Assert.IsAssignableFrom<IDictionary<string, string>>(cache.GetValue("dictionary"));
		Assert.Equal("value", dictionary["field"]);
		var set = Assert.IsAssignableFrom<ISet<string>>(cache.GetValue("set"));
		Assert.Equal(["alpha", "beta"], set.Order().ToArray());
		Assert.Equal(["first", "second"], Assert.IsType<string[]>(cache.GetValue("list")));

		Assert.True(cache.TryGetValue<object>("dictionary", out var dictionaryValue));
		Assert.Equal("value", Assert.IsAssignableFrom<IDictionary<string, string>>(dictionaryValue)["field"]);
		Assert.True(cache.TryGetValue<object>("set", out var setValue));
		Assert.Equal(["alpha", "beta"], Assert.IsAssignableFrom<ISet<string>>(setValue).Order().ToArray());
		Assert.True(cache.TryGetValue<object>("list", out var listValue));
		Assert.Equal(["first", "second"], Assert.IsType<string[]>(listValue));

		var dictionaryEntry = await cache.GetEntryAsync("dictionary");
		Assert.Equal(RedisEntryType.Dictionary, dictionaryEntry.entryType);
		Assert.Equal("value", Assert.IsAssignableFrom<IDictionary<string, string>>(dictionaryEntry.value)["field"]);
		var setEntry = await cache.GetEntryAsync("set");
		Assert.Equal(RedisEntryType.Set, setEntry.entryType);
		Assert.Equal(["alpha", "beta"], Assert.IsAssignableFrom<ISet<string>>(setEntry.value).Order().ToArray());
		var listEntry = await cache.GetEntryAsync("list");
		Assert.Equal(RedisEntryType.List, listEntry.entryType);
		Assert.Equal(["first", "second"], Assert.IsType<string[]>(listEntry.value));
		var sortedEntry = await cache.GetEntryAsync("sorted");
		Assert.Equal(RedisEntryType.SortedSet, sortedEntry.entryType);
		Assert.Equal(["alpha", "beta"], Assert.IsType<string[]>(sortedEntry.value));

		var listAsync = await cache.TryGetValueAsync("list");
		Assert.True(listAsync.result);
		Assert.Equal(["first", "second"], Assert.IsType<string[]>(listAsync.value));
	}

	[Fact]
	public async Task SetEntryAsync_CollectionsReplaceContentsClearTtlAndCompleteTransactions()
	{
		EnsureRedis();

		await using var cache = CreateCache(out var cacheNamespace);
		using var connection = CreateConnection();
		var database = connection.GetDatabase();
		var dictionaryKey = $"{cacheNamespace}:dictionary";
		var setKey = $"{cacheNamespace}:set";
		var listKey = $"{cacheNamespace}:list";
		var timeout = TimeSpan.FromSeconds(5);

		Assert.True(await cache.SetEntryAsync("dictionary",
			new Dictionary<string, string> { ["old"] = "remove", ["shared"] = "old" }, TimeSpan.FromMinutes(1)).AsTask().WaitAsync(timeout));
		Assert.True(await cache.SetEntryAsync("set",
			new HashSet<string> { "old", "shared" }, TimeSpan.FromMinutes(1)).AsTask().WaitAsync(timeout));
		Assert.True(await cache.SetEntryAsync("list",
			new List<string> { "old", "shared" }, TimeSpan.FromMinutes(1)).AsTask().WaitAsync(timeout));

		Assert.True(await cache.SetEntryAsync("dictionary",
			new Dictionary<string, string> { ["shared"] = "new", ["added"] = "value" }, TimeSpan.Zero).AsTask().WaitAsync(timeout));
		Assert.True(await cache.SetEntryAsync("set",
			new HashSet<string> { "shared", "added" }, TimeSpan.Zero).AsTask().WaitAsync(timeout));
		Assert.True(await cache.SetEntryAsync("list",
			new List<string> { "shared", "added" }, TimeSpan.Zero).AsTask().WaitAsync(timeout));

		Assert.Equal(new HashEntry[] { new("added", "value"), new("shared", "new") },
			(await database.HashGetAllAsync(dictionaryKey)).OrderBy(entry => entry.Name).ToArray());
		Assert.Equal(new RedisValue[] { "added", "shared" }, (await database.SetMembersAsync(setKey)).Order().ToArray());
		Assert.Equal(new RedisValue[] { "shared", "added" }, await database.ListRangeAsync(listKey));
		Assert.Null(await database.KeyTimeToLiveAsync(dictionaryKey));
		Assert.Null(await database.KeyTimeToLiveAsync(setKey));
		Assert.Null(await database.KeyTimeToLiveAsync(listKey));

		Assert.True(await cache.SetEntryAsync("dictionary", new Dictionary<string, string>(), TimeSpan.Zero).AsTask().WaitAsync(timeout));
		Assert.True(await cache.SetEntryAsync("set", new HashSet<string>(), TimeSpan.Zero).AsTask().WaitAsync(timeout));
		Assert.True(await cache.SetEntryAsync("list", new List<string>(), TimeSpan.Zero).AsTask().WaitAsync(timeout));
		Assert.False(await database.KeyExistsAsync(dictionaryKey));
		Assert.False(await database.KeyExistsAsync(setKey));
		Assert.False(await database.KeyExistsAsync(listKey));
	}

	[Fact]
	public async Task SetEntry_CollectionsReplaceContentsSynchronously()
	{
		EnsureRedis();

		using var cache = CreateCache(out var cacheNamespace);
		using var connection = CreateConnection();
		var database = connection.GetDatabase();
		var dictionaryKey = $"{cacheNamespace}:dictionary-sync";
		var setKey = $"{cacheNamespace}:set-sync";
		var listKey = $"{cacheNamespace}:list-sync";

		Assert.True(cache.SetEntry("dictionary-sync", new Dictionary<string, string> { ["old"] = "remove" }, TimeSpan.Zero));
		Assert.True(cache.SetEntry("set-sync", new HashSet<string> { "old" }, TimeSpan.Zero));
		Assert.True(cache.SetEntry("list-sync", new List<string> { "old" }, TimeSpan.Zero));

		Assert.True(cache.SetEntry("dictionary-sync", new Dictionary<string, string> { ["new"] = "value" }, TimeSpan.Zero));
		Assert.True(cache.SetEntry("set-sync", new HashSet<string> { "new" }, TimeSpan.Zero));
		Assert.True(cache.SetEntry("list-sync", new List<string> { "new" }, TimeSpan.Zero));

		Assert.Equal(new HashEntry[] { new("new", "value") }, await database.HashGetAllAsync(dictionaryKey));
		Assert.Equal(new RedisValue[] { "new" }, await database.SetMembersAsync(setKey));
		Assert.Equal(new RedisValue[] { "new" }, await database.ListRangeAsync(listKey));
	}

	[Fact]
	public async Task SetEntry_CollectionsHonorCacheRequisiteDuringReplacement()
	{
		EnsureRedis();

		await using var cache = CreateCache(out var cacheNamespace);
		using var connection = CreateConnection();
		var database = connection.GetDatabase();
		var setKey = $"{cacheNamespace}:set";
		var dictionaryKey = $"{cacheNamespace}:dictionary";
		var listKey = $"{cacheNamespace}:list";

		Assert.True(cache.SetEntry("set", new HashSet<string> { "original" }, TimeSpan.Zero));
		Assert.False(cache.SetEntry("set", new HashSet<string> { "rejected" }, TimeSpan.Zero, CacheRequisite.NotExists));
		Assert.Equal(new RedisValue[] { "original" }, await database.SetMembersAsync(setKey));

		Assert.False(await cache.SetEntryAsync("dictionary", new Dictionary<string, string> { ["field"] = "rejected" },
			TimeSpan.Zero, CacheRequisite.Exists));
		Assert.False(await database.KeyExistsAsync(dictionaryKey));

		Assert.True(cache.SetEntry("dictionary", new Dictionary<string, string> { ["field"] = "original" }, TimeSpan.Zero));
		Assert.True(cache.SetEntry("dictionary", new Dictionary<string, string> { ["field"] = "replacement" },
			TimeSpan.Zero, CacheRequisite.Exists));
		Assert.Equal("replacement", (string)await database.HashGetAsync(dictionaryKey, "field"));

		Assert.True(await cache.SetEntryAsync("list", new List<string> { "created" },
			TimeSpan.Zero, CacheRequisite.NotExists));
		Assert.Equal(new RedisValue[] { "created" }, await database.ListRangeAsync(listKey));

		Assert.False(cache.SetEntry("set", new HashSet<string>(), TimeSpan.Zero, CacheRequisite.NotExists));
		Assert.Equal(new RedisValue[] { "original" }, await database.SetMembersAsync(setKey));
		Assert.False(await cache.SetEntryAsync("missing-dictionary", new Dictionary<string, string>(),
			TimeSpan.Zero, CacheRequisite.Exists));
		Assert.False(await database.KeyExistsAsync($"{cacheNamespace}:missing-dictionary"));
		Assert.True(await cache.SetEntryAsync("list", new List<string>(), TimeSpan.Zero, CacheRequisite.Exists));
		Assert.False(await database.KeyExistsAsync(listKey));
	}

	[Fact]
	public async Task Dispose_RepeatedAndPostDisposeOperations_AreSafeAndRejected()
	{
		EnsureRedis();

		var cache = CreateCache(out _);
		Assert.False(cache.Exists("connect"));

		cache.Dispose();
		cache.Dispose();
		await cache.DisposeAsync();

		Assert.Throws<ObjectDisposedException>(() => cache.Exists("after-dispose"));
		await Assert.ThrowsAsync<ObjectDisposedException>(async () => await cache.ExistsAsync("after-dispose"));
		Assert.Throws<ObjectDisposedException>(() => cache.Namespace = "after-dispose");
	}

	[Fact]
	public async Task ConnectAndDispose_ConcurrentCalls_DoNotResurrectService()
	{
		EnsureRedis();

		var cache = CreateCache(out _);
		using var start = new ManualResetEventSlim(false);
		var exceptions = new ConcurrentQueue<Exception>();
		var operations = Enumerable.Range(0, 32).Select(_ => Task.Run(async () =>
		{
			start.Wait();
			try
			{
				await cache.ExistsAsync("race");
			}
			catch(ObjectDisposedException)
			{
			}
			catch(Exception exception)
			{
				exceptions.Enqueue(exception);
			}
		})).ToArray();
		var disposal = Task.Run(async () =>
		{
			start.Wait();
			await cache.DisposeAsync();
		});

		start.Set();
		await Task.WhenAll(operations.Append(disposal)).WaitAsync(TimeSpan.FromSeconds(15));

		Assert.Empty(exceptions);
		Assert.Throws<ObjectDisposedException>(() => cache.Exists("after-race"));
		await Assert.ThrowsAsync<ObjectDisposedException>(async () => await cache.ExistsAsync("after-race"));
	}

	private static RedisService CreateCache(out string cacheNamespace)
	{
		cacheNamespace = $"Zongsoft.Tests.Correctness.{Guid.NewGuid():N}";
		return new RedisService($"correctness-{Guid.NewGuid():N}",
			$"server={Global.Server};password={Global.Password};timeout=5s;")
		{
			Namespace = cacheNamespace,
		};
	}

	private static ConnectionMultiplexer CreateConnection() =>
		ConnectionMultiplexer.Connect($"{Global.Server},password={Global.Password},connectTimeout=2000");

	private static void EnsureRedis()
	{
		Assert.SkipUnless(Global.IsTestingEnabled, TESTS_DISABLED);
		Assert.SkipUnless(Global.IsAvailable(), REDIS_UNAVAILABLE);
	}
}
