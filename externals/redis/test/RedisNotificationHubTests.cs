using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

using StackExchange.Redis;

using Xunit;

using Zongsoft.Caching;
using Zongsoft.Components;

namespace Zongsoft.Externals.Redis.Tests;

public class RedisNotificationHubTests
{
	private const string REDIS_UNAVAILABLE = "Redis is unavailable at 127.0.0.1:6379.";
	private const string TESTS_DISABLED = "Redis integration tests require ZONGSOFT_REDIS_TESTS=1.";

	[Fact]
	public async Task ScopeIdentity_SharesMatchingHubAndSeparatesDatabaseOrNamespace()
	{
		EnsureRedis();

		using var connection = CreateConnection();
		var prefix = $"Zongsoft.Tests.Hub.{Guid.NewGuid():N}:";
		var first = await RedisService.DistributedCacheNotificationHub.GetAsync(connection, 0, prefix, default);
		var same = await RedisService.DistributedCacheNotificationHub.GetAsync(connection, 0, prefix, default);
		var otherDatabase = await RedisService.DistributedCacheNotificationHub.GetAsync(connection, 1, prefix, default);
		var otherNamespace = await RedisService.DistributedCacheNotificationHub.GetAsync(connection, 0, prefix + "other:", default);

		Assert.Same(first, same);
		Assert.NotSame(first, otherDatabase);
		Assert.NotSame(first, otherNamespace);
	}

	[Theory]
	[InlineData(DistributedCacheNotificationOverflowPolicy.DropOldest, new[] { "first", "third", "fourth" })]
	[InlineData(DistributedCacheNotificationOverflowPolicy.DropNewest, new[] { "first", "second", "third" })]
	public async Task FullQueue_AppliesOverflowPolicyAndUpdatesCounters(DistributedCacheNotificationOverflowPolicy overflowPolicy, string[] expected)
	{
		EnsureRedis();

		using var connection = CreateConnection();
		await using var cache = CreateCache();
		var hub = await RedisService.DistributedCacheNotificationHub.GetAsync(connection, 0, $"Zongsoft.Tests.Hub.{Guid.NewGuid():N}:", default);
		using var handler = new BlockingNotificationHandler();
		var options = new DistributedCacheSubscriptionOptions
		{
			Capacity = 2,
			OverflowPolicy = overflowPolicy,
		};
		await using var subscription = new RedisService.DistributedCacheSubscription(cache, hub, handler, options);
		await subscription.SubscribeAsync(default);

		subscription.Enqueue(DistributedCacheNotificationKind.Updated, "first");
		await handler.Started.WaitAsync(TimeSpan.FromSeconds(5));
		subscription.Enqueue(DistributedCacheNotificationKind.Updated, "second");
		subscription.Enqueue(DistributedCacheNotificationKind.Updated, "third");
		subscription.Enqueue(DistributedCacheNotificationKind.Updated, "fourth");

		Assert.Equal(2, subscription.PendingCount);
		Assert.Equal(1, subscription.DroppedCount);
		Assert.Equal(2, subscription.Options.Capacity);
		Assert.Equal(overflowPolicy, subscription.Options.OverflowPolicy);
		Assert.Throws<NotSupportedException>(() => subscription.Options.Capacity = 4);

		handler.Release();
		await handler.WaitForCountAsync(3, TimeSpan.FromSeconds(5));

		Assert.Equal(expected, handler.Notifications.Select(notification => notification.Key).ToArray());
		Assert.Equal(0, subscription.PendingCount);
		Assert.Equal(1, subscription.DroppedCount);
	}

	[Fact]
	public async Task LastSubscriberDispose_ReleasesHubAndClearsPendingNotifications()
	{
		EnsureRedis();

		using var connection = CreateConnection();
		await using var cache = CreateCache();
		var prefix = $"Zongsoft.Tests.Hub.{Guid.NewGuid():N}:";
		var firstHub = await RedisService.DistributedCacheNotificationHub.GetAsync(connection, 0, prefix, default);
		using var handler = new BlockingNotificationHandler();
		var subscription = new RedisService.DistributedCacheSubscription(cache, firstHub, handler, new DistributedCacheSubscriptionOptions { Capacity = 2 });
		await subscription.SubscribeAsync(default);

		subscription.Enqueue(DistributedCacheNotificationKind.Updated, "first");
		await handler.Started.WaitAsync(TimeSpan.FromSeconds(5));
		subscription.Enqueue(DistributedCacheNotificationKind.Updated, "pending");
		Assert.Equal(1, subscription.PendingCount);

		await subscription.DisposeAsync();
		Assert.True(subscription.IsDisposed);
		Assert.Equal(0, subscription.PendingCount);
		var replacementHub = await RedisService.DistributedCacheNotificationHub.GetAsync(connection, 0, prefix, default);
		Assert.NotSame(firstHub, replacementHub);
	}

	private static ConnectionMultiplexer CreateConnection() =>
		ConnectionMultiplexer.Connect($"{Global.Server},password={Global.Password},connectTimeout=2000");

	private static RedisService CreateCache() => new($"hub-{Guid.NewGuid():N}",
		$"server={Global.Server};password={Global.Password};timeout=5s;");

	private static void EnsureRedis()
	{
		Assert.SkipUnless(Global.IsTestingEnabled, TESTS_DISABLED);
		Assert.SkipUnless(Global.IsAvailable(), REDIS_UNAVAILABLE);
	}

	private sealed class BlockingNotificationHandler : HandlerBase<DistributedCacheNotification>, IDisposable
	{
		private readonly ConcurrentQueue<DistributedCacheNotification> _notifications = new();
		private readonly TaskCompletionSource<bool> _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
		private readonly TaskCompletionSource<bool> _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
		private int _count;

		public Task Started => _started.Task;
		public DistributedCacheNotification[] Notifications => _notifications.ToArray();

		public void Release() => _release.TrySetResult(true);

		public async Task WaitForCountAsync(int count, TimeSpan timeout)
		{
			var deadline = DateTime.UtcNow + timeout;
			while(Volatile.Read(ref _count) < count && DateTime.UtcNow < deadline)
				await Task.Delay(10);

			Assert.Equal(count, Volatile.Read(ref _count));
		}

		public void Dispose() => this.Release();

		protected override async ValueTask OnHandleAsync(DistributedCacheNotification notification, Zongsoft.Collections.Parameters parameters, CancellationToken cancellation)
		{
			_notifications.Enqueue(notification);
			if(Interlocked.Increment(ref _count) == 1)
			{
				_started.TrySetResult(true);
				await _release.Task.WaitAsync(cancellation);
			}
		}
	}
}
