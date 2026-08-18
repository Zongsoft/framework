using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using StackExchange.Redis;

using Xunit;

namespace Zongsoft.Externals.Redis.Tests;

public class RedisLifecycleAndConnectionTests
{
	private const string REDIS_UNAVAILABLE = "Redis is unavailable at 127.0.0.1:6379.";
	private const string TESTS_DISABLED = "Redis integration tests require ZONGSOFT_REDIS_TESTS=1.";

	[Fact]
	public void RedisService_LifecycleUsesSingleGateAndDisposalCompletionField()
	{
		var fields = typeof(RedisService).GetFields(BindingFlags.Instance | BindingFlags.NonPublic);

		var gate = Assert.Single(fields, field => field.FieldType == typeof(SemaphoreSlim));
		Assert.Equal("_gate", gate.Name);
		var disposal = Assert.Single(fields, field => field.FieldType == typeof(TaskCompletionSource<bool>));
		Assert.Equal("_disposal", disposal.Name);
		Assert.DoesNotContain(fields, field => field.Name is "_connectionLock" or "_subscriptionLock" or "_disposeLock" or "_disposeTask" or "_disposed");
	}

	[Fact]
	public void ApprovedHotPathImplementationTypes_AreSealed()
	{
		Assert.True(typeof(RedisConnectionLease).IsSealed);
		Assert.True(typeof(RedisCacheNotificationHub).IsSealed);
		Assert.True(typeof(RedisCacheSubscription).IsSealed);

		var poller = typeof(Messaging.RedisSubscriber).GetNestedType("Poller", BindingFlags.NonPublic);
		Assert.NotNull(poller);
		Assert.True(poller.IsSealed);
	}

	[Fact]
	public async Task DisposeAsync_ConcurrentCallersShareOneCompletionAndCannotResurrectService()
	{
		EnsureRedis();

		var cache = CreateCache();
		Assert.False(await cache.ExistsAsync("activate"));

		var tasks = Enumerable.Range(0, 32).Select(_ => cache.DisposeAsync().AsTask()).ToArray();
		Assert.All(tasks, task => Assert.Same(tasks[0], task));
		await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromSeconds(10));

		Assert.Throws<ObjectDisposedException>(() => cache.Exists("after-dispose"));
		await Assert.ThrowsAsync<ObjectDisposedException>(async () => await cache.ExistsAsync("after-dispose"));
	}

	[Fact]
	public async Task EquivalentOptions_ConcurrentAcquisitionCreatesOneSharedMultiplexer()
	{
		EnsureRedis();

		var options = CreateOptions();
		var operations = Enumerable.Range(0, 16).Select(async _ => await RedisConnectionPool.AcquireAsync(options.Clone())).ToArray();
		var leases = await Task.WhenAll(operations).WaitAsync(TimeSpan.FromSeconds(10));
		var connection = leases[0].Connection;

		try
		{
			Assert.All(leases, lease => Assert.Same(connection, lease.Connection));
			Assert.True(connection.IsConnected);

			for(var index = 0; index < leases.Length - 1; index++)
				await leases[index].DisposeAsync();

			Assert.True(await leases[^1].Connection.GetDatabase().PingAsync() >= TimeSpan.Zero);
		}
		finally
		{
			foreach(var lease in leases)
				await lease.DisposeAsync();
		}

		Assert.False(connection.IsConnected);
	}

	[Fact]
	public void ScopeViews_AreImmutableBeforeActivation()
	{
		using var root = CreateCache();
		root.Namespace = "root";
		root.Use(1);
		using var namespaceView = root.WithNamespace("view");
		using var databaseView = root.WithDatabase(2);

		Assert.Equal("view", namespaceView.Namespace);
		Assert.Equal(1, namespaceView.DatabaseId);
		Assert.Equal("root", databaseView.Namespace);
		Assert.Equal(2, databaseView.DatabaseId);
		Assert.Throws<InvalidOperationException>(() => namespaceView.Namespace = "changed");
		Assert.Throws<InvalidOperationException>(() => namespaceView.Use(3));
		Assert.Throws<InvalidOperationException>(() => databaseView.Namespace = "changed");
		Assert.Throws<InvalidOperationException>(() => databaseView.Use(3));
	}

	[Fact]
	public async Task LegacyScopeMutation_BeforeActivationSucceedsAndAfterActivationFreezes()
	{
		EnsureRedis();

		await using var cache = CreateCache();
		cache.Namespace = "before";
		cache.Use(1);
		Assert.Equal("before", cache.Namespace);
		Assert.Equal(1, cache.DatabaseId);

		Assert.False(await cache.ExistsAsync("activate"));
		cache.Namespace = "before";
		cache.Use(1);
		await cache.UseAsync(1);

		Assert.Throws<InvalidOperationException>(() => cache.Namespace = "after");
		Assert.Throws<InvalidOperationException>(() => cache.Use(2));
		await Assert.ThrowsAsync<InvalidOperationException>(async () => await cache.UseAsync(2));
	}

	[Fact]
	public async Task ScopeViews_ShareConnectionAndDisposeIndependently()
	{
		EnsureRedis();

		await using var root = CreateCache();
		root.Namespace = $"Zongsoft.Tests.Scope.Root.{Guid.NewGuid():N}";
		await using var namespaceView = root.WithNamespace($"Zongsoft.Tests.Scope.View.{Guid.NewGuid():N}");
		await using var databaseView = root.WithDatabase(1);

		Assert.True(await root.SetValueAsync("root", "one"));
		Assert.True(await namespaceView.SetValueAsync("view", "two"));
		Assert.True(await databaseView.SetValueAsync("database", "three"));
		Assert.Same(root.Database.Multiplexer, namespaceView.Database.Multiplexer);
		Assert.Same(root.Database.Multiplexer, databaseView.Database.Multiplexer);

		await namespaceView.DisposeAsync();
		Assert.Equal("one", await root.GetValueAsync<string>("root"));
		await databaseView.DisposeAsync();
		Assert.True(await root.ExistsAsync("root"));

		await root.ClearAsync();
	}

	private static RedisService CreateCache() => new($"lifecycle-{Guid.NewGuid():N}",
		$"server={RedisTestUtility.Server};password={RedisTestUtility.Password};timeout=5s;");

	private static ConfigurationOptions CreateOptions()
	{
		var options = ConfigurationOptions.Parse($"{RedisTestUtility.Server},password={RedisTestUtility.Password},connectTimeout=2000");
		options.ClientName = $"pool-tests-{Guid.NewGuid():N}";
		return options;
	}

	private static void EnsureRedis()
	{
		Assert.SkipUnless(RedisTestUtility.IsTestingEnabled, TESTS_DISABLED);
		Assert.SkipUnless(RedisTestUtility.IsAvailable(), REDIS_UNAVAILABLE);
	}
}
