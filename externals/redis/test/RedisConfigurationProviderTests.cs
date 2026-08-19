using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using StackExchange.Redis;

using Xunit;

using Zongsoft.Externals.Redis.Configuration;

namespace Zongsoft.Externals.Redis.Tests;

public class RedisConfigurationProviderTests
{
	private const string REDIS_UNAVAILABLE = "Redis is unavailable at 127.0.0.1:6379.";
	private const string TESTS_DISABLED = "Redis integration tests require ZONGSOFT_REDIS_TESTS=1.";
	private const string NOTIFICATIONS_UNAVAILABLE = "Redis keyspace notifications must include K and A (notify-keyspace-events KA).";

	[Fact]
	public async Task Load_MaterializesCaseInsensitiveLocalSnapshotAndNotificationReloadsIt()
	{
		EnsureRedisNotifications();

		await using var cache = CreateCache(out var cacheNamespace);
		const string configurationKey = "configuration";
		cache.SetValue(configurationKey, new Dictionary<string, string>
		{
			["Feature"] = "old",
			["Nested:Value"] = "one",
		});

		var source = new RedisConfigurationSource("test") { Namespace = configurationKey };
		await using var provider = new RedisConfigurationProvider(source, cache);
		provider.Load();

		Assert.True(provider.TryGet("feature", out var oldValue));
		Assert.Equal("old", oldValue);
		Assert.True(provider.TryGet("NESTED:VALUE", out var nested));
		Assert.Equal("one", nested);

		await provider.SubscriptionTask.WaitAsync(TimeSpan.FromSeconds(10));
		var reloaded = GetNextReloadAsync(provider);
		await cache.Database.HashSetAsync($"{cacheNamespace}:{configurationKey}", "Feature", "new");

		Assert.True(provider.TryGet("Feature", out var snapshotValue));
		Assert.Equal("old", snapshotValue);
		await reloaded.WaitAsync(TimeSpan.FromSeconds(10));
		Assert.True(provider.TryGet("Feature", out var currentValue));
		Assert.Equal("new", currentValue);

		await cache.ClearAsync();
	}

	[Fact]
	public async Task NotificationFilter_RequiresExactKeyOrColonBoundary()
	{
		EnsureRedisNotifications();

		await using var cache = CreateCache(out var cacheNamespace);
		const string configurationKey = "settings";
		cache.SetValue(configurationKey, new Dictionary<string, string> { ["Value"] = "one" });

		var source = new RedisConfigurationSource("test") { Namespace = configurationKey };
		await using var provider = new RedisConfigurationProvider(source, cache);
		provider.Load();
		await provider.SubscriptionTask.WaitAsync(TimeSpan.FromSeconds(10));

		var unrelatedReload = GetNextReloadAsync(provider);
		await cache.Database.StringSetAsync($"{cacheNamespace}:{configurationKey}Extra", "ignored");
		await Task.Delay(300);
		Assert.False(unrelatedReload.IsCompleted);

		var childReload = GetNextReloadAsync(provider);
		await cache.Database.StringSetAsync($"{cacheNamespace}:{configurationKey}:Child", "related");
		await childReload.WaitAsync(TimeSpan.FromSeconds(10));

		await cache.ClearAsync();
	}

	[Fact]
	public async Task IndividualStringKeys_LoadAndReloadWithNamespacePrefixRemoved()
	{
		EnsureRedisNotifications();

		await using var cache = CreateCache(out _);
		const string configurationKey = "individual";
		Assert.True(await cache.SetValueAsync($"{configurationKey}:First", "one"));
		Assert.True(await cache.SetValueAsync($"{configurationKey}:Second", "two"));

		var source = new RedisConfigurationSource("test") { Namespace = configurationKey };
		await using var provider = new RedisConfigurationProvider(source, cache);
		provider.Load();
		await provider.SubscriptionTask.WaitAsync(TimeSpan.FromSeconds(10));

		Assert.True(provider.TryGet("First", out var first));
		Assert.Equal("one", first);
		Assert.True(provider.TryGet("Second", out var second));
		Assert.Equal("two", second);
		Assert.False(provider.TryGet($"{configurationKey}:First", out _));

		var reloaded = GetNextReloadAsync(provider);
		Assert.True(await cache.SetValueAsync($"{configurationKey}:Third", "three"));
		await reloaded.WaitAsync(TimeSpan.FromSeconds(10));

		Assert.True(provider.TryGet("Third", out var third));
		Assert.Equal("three", third);
		Assert.False(provider.TryGet($"{configurationKey}:Third", out _));

		await cache.ClearAsync();
	}

	[Fact]
	public async Task DisposeAsync_StopsNotificationReloadsAndLeavesInjectedServiceOwnedByCaller()
	{
		EnsureRedisNotifications();

		await using var cache = CreateCache(out var cacheNamespace);
		const string configurationKey = "dispose";
		cache.SetValue(configurationKey, new Dictionary<string, string> { ["Value"] = "before" });

		var source = new RedisConfigurationSource("test") { Namespace = configurationKey };
		var provider = new RedisConfigurationProvider(source, cache);
		provider.Load();
		await provider.SubscriptionTask.WaitAsync(TimeSpan.FromSeconds(10));
		await provider.DisposeAsync();

		var reload = GetNextReloadAsync(provider);
		await cache.Database.HashSetAsync($"{cacheNamespace}:{configurationKey}", "Value", "after");
		await Task.Delay(300);

		Assert.False(reload.IsCompleted);
		Assert.True(provider.TryGet("Value", out var snapshot));
		Assert.Equal("before", snapshot);
		Assert.True(await cache.ExistsAsync(configurationKey));

		await provider.DisposeAsync();
		await cache.ClearAsync();
	}

	private static Task GetNextReloadAsync(RedisConfigurationProvider provider)
	{
		var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var registration = provider.GetReloadToken().RegisterChangeCallback(
			static state => ((TaskCompletionSource<bool>)state).TrySetResult(true), completion);
		return AwaitReloadAsync(completion.Task, registration);

		static async Task AwaitReloadAsync(Task task, IDisposable registration)
		{
			try { await task; }
			finally { registration.Dispose(); }
		}
	}

	private static RedisService CreateCache(out string cacheNamespace)
	{
		cacheNamespace = $"Zongsoft.Tests.Configuration.{Guid.NewGuid():N}";
		return new RedisService($"configuration-{Guid.NewGuid():N}",
			$"server={Global.Server};password={Global.Password};timeout=5s;")
		{
			Namespace = cacheNamespace,
		};
	}

	private static void EnsureRedisNotifications()
	{
		Assert.SkipUnless(Global.IsTestingEnabled, TESTS_DISABLED);
		Assert.SkipUnless(Global.IsAvailable(), REDIS_UNAVAILABLE);
		Assert.SkipUnless(HasRedisNotifications(), NOTIFICATIONS_UNAVAILABLE);
	}

	private static bool HasRedisNotifications()
	{
		try
		{
			using var connection = ConnectionMultiplexer.Connect($"{Global.Server},password={Global.Password},connectTimeout=2000,allowAdmin=true");
			var server = connection.GetServer(connection.GetEndPoints()[0]);
			var value = (string)server.ConfigGet("notify-keyspace-events").FirstOrDefault().Value;
			return value?.Contains('K') == true && value.Contains('A');
		}
		catch
		{
			return false;
		}
	}
}
