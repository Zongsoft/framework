using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

using StackExchange.Redis;

using Xunit;

using Zongsoft.Caching;
using Zongsoft.Components;

namespace Zongsoft.Externals.Redis.Tests;

public class RedisCacheNotificationTests
{
	private const string REDIS_UNAVAILABLE = "Redis is unavailable at 127.0.0.1:6379.";
	private const string NOTIFICATIONS_UNAVAILABLE = "Redis keyspace notifications must include K and A (notify-keyspace-events KA).";
	private const string TESTS_DISABLED = "Redis integration tests require ZONGSOFT_REDIS_TESTS=1.";

	public static TheoryData<KeyNotificationType, DistributedCacheNotificationKind> SupportedNotificationTypes => new()
	{
		{ KeyNotificationType.Expired, DistributedCacheNotificationKind.Expired },
		{ KeyNotificationType.Evicted, DistributedCacheNotificationKind.Evicted },
		{ KeyNotificationType.Del, DistributedCacheNotificationKind.Removed },
		{ KeyNotificationType.RenameFrom, DistributedCacheNotificationKind.Removed },
		{ KeyNotificationType.MoveFrom, DistributedCacheNotificationKind.Removed },
		{ KeyNotificationType.Append, DistributedCacheNotificationKind.Updated },
		{ KeyNotificationType.Copy, DistributedCacheNotificationKind.Updated },
		{ KeyNotificationType.HDel, DistributedCacheNotificationKind.Updated },
		{ KeyNotificationType.HExpired, DistributedCacheNotificationKind.Updated },
		{ KeyNotificationType.HIncrByFloat, DistributedCacheNotificationKind.Updated },
		{ KeyNotificationType.HIncrBy, DistributedCacheNotificationKind.Updated },
		{ KeyNotificationType.HSet, DistributedCacheNotificationKind.Updated },
		{ KeyNotificationType.IncrByFloat, DistributedCacheNotificationKind.Updated },
		{ KeyNotificationType.IncrBy, DistributedCacheNotificationKind.Updated },
		{ KeyNotificationType.LInsert, DistributedCacheNotificationKind.Updated },
		{ KeyNotificationType.LPop, DistributedCacheNotificationKind.Updated },
		{ KeyNotificationType.LPush, DistributedCacheNotificationKind.Updated },
		{ KeyNotificationType.LRem, DistributedCacheNotificationKind.Updated },
		{ KeyNotificationType.LSet, DistributedCacheNotificationKind.Updated },
		{ KeyNotificationType.LTrim, DistributedCacheNotificationKind.Updated },
		{ KeyNotificationType.MoveTo, DistributedCacheNotificationKind.Updated },
		{ KeyNotificationType.RenameTo, DistributedCacheNotificationKind.Updated },
		{ KeyNotificationType.Restore, DistributedCacheNotificationKind.Updated },
		{ KeyNotificationType.RPop, DistributedCacheNotificationKind.Updated },
		{ KeyNotificationType.RPush, DistributedCacheNotificationKind.Updated },
		{ KeyNotificationType.SAdd, DistributedCacheNotificationKind.Updated },
		{ KeyNotificationType.Set, DistributedCacheNotificationKind.Updated },
		{ KeyNotificationType.SetRange, DistributedCacheNotificationKind.Updated },
		{ KeyNotificationType.SortStore, DistributedCacheNotificationKind.Updated },
		{ KeyNotificationType.SRem, DistributedCacheNotificationKind.Updated },
		{ KeyNotificationType.SPop, DistributedCacheNotificationKind.Updated },
		{ KeyNotificationType.XAdd, DistributedCacheNotificationKind.Updated },
		{ KeyNotificationType.XDel, DistributedCacheNotificationKind.Updated },
		{ KeyNotificationType.XTrim, DistributedCacheNotificationKind.Updated },
		{ KeyNotificationType.ZAdd, DistributedCacheNotificationKind.Updated },
		{ KeyNotificationType.ZDiffStore, DistributedCacheNotificationKind.Updated },
		{ KeyNotificationType.ZInterStore, DistributedCacheNotificationKind.Updated },
		{ KeyNotificationType.ZUnionStore, DistributedCacheNotificationKind.Updated },
		{ KeyNotificationType.ZIncr, DistributedCacheNotificationKind.Updated },
		{ KeyNotificationType.ZRemByRank, DistributedCacheNotificationKind.Updated },
		{ KeyNotificationType.ZRemByScore, DistributedCacheNotificationKind.Updated },
		{ KeyNotificationType.ZRem, DistributedCacheNotificationKind.Updated },
		{ KeyNotificationType.ArDel, DistributedCacheNotificationKind.Updated },
		{ KeyNotificationType.ArDelRange, DistributedCacheNotificationKind.Updated },
		{ KeyNotificationType.ArSet, DistributedCacheNotificationKind.Updated },
	};

	public static TheoryData<KeyNotificationType> IgnoredNotificationTypes => new()
	{
		KeyNotificationType.Unknown,
		KeyNotificationType.Expire,
		KeyNotificationType.Persist,
		KeyNotificationType.HExpire,
		KeyNotificationType.HPersist,
		KeyNotificationType.XGroupCreateConsumer,
		KeyNotificationType.XGroupCreate,
		KeyNotificationType.XGroupDelConsumer,
		KeyNotificationType.XGroupDestroy,
		KeyNotificationType.XGroupSetId,
		KeyNotificationType.XSetId,
		KeyNotificationType.New,
		KeyNotificationType.Overwritten,
		KeyNotificationType.TypeChanged,
	};

	[Theory]
	[MemberData(nameof(SupportedNotificationTypes))]
	public void TryGetNotificationKind_SupportedType_ReturnsMappedKind(KeyNotificationType type, DistributedCacheNotificationKind expected)
	{
		Assert.True(RedisCacheSubscription.TryGetNotificationKind(type, out var actual));
		Assert.Equal(expected, actual);
	}

	[Theory]
	[MemberData(nameof(IgnoredNotificationTypes))]
	public void TryGetNotificationKind_MetadataOrUnknownType_IsIgnored(KeyNotificationType type)
	{
		Assert.False(RedisCacheSubscription.TryGetNotificationKind(type, out var kind));
		Assert.Equal(DistributedCacheNotificationKind.None, kind);
	}

	[Fact]
	public async Task SubscribeAsync_Options_AreValidatedAndSnapshotted()
	{
		EnsureRedisNotifications();

		await using var cache = CreateCache(out _);
		using var buffer = new NotificationBuffer();
		var handler = buffer;

		await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
			await cache.SubscribeAsync(handler, new DistributedCacheSubscriptionOptions(null, DistributedCacheNotificationKind.None)));
		await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
			await cache.SubscribeAsync(handler, new DistributedCacheSubscriptionOptions(null, (DistributedCacheNotificationKind)16)));

		var options = new DistributedCacheSubscriptionOptions("   ", DistributedCacheNotificationKind.Updated);
		await using var subscription = await cache.SubscribeAsync(handler, options);

		options.Prefix = "changed";
		options.Kind = DistributedCacheNotificationKind.Removed;

		Assert.NotSame(options, subscription.Options);
		Assert.Equal(string.Empty, subscription.Options.Prefix);
		Assert.Equal(DistributedCacheNotificationKind.Updated, subscription.Options.Kind);
		Assert.Same(cache, subscription.Cache);
		Assert.Same(handler, subscription.Handler);
	}

	[Fact]
	public async Task SubscribeAsync_CanceledBeforeEstablishment_ThrowsOperationCanceledException()
	{
		Assert.SkipUnless(RedisTestUtility.IsTestingEnabled, TESTS_DISABLED);
		Assert.SkipUnless(RedisTestUtility.IsAvailable(), REDIS_UNAVAILABLE);

		await using var cache = CreateCache(out _);
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
			await cache.SubscribeAsync(Handler.Handle<DistributedCacheNotification>(_ => { }), cancellation: cancellation.Token));
	}

	[Fact]
	public async Task SubscribeAsync_NullHandler_ThrowsArgumentNullException()
	{
		await using var cache = CreateCache(out _);

		await Assert.ThrowsAsync<ArgumentNullException>(async () => await cache.SubscribeAsync(null));
	}

	[Fact]
	public async Task UpdatedNotifications_StringCollectionAndExpiry_ReportContentChangesOnly()
	{
		EnsureRedisNotifications();

		await using var cache = CreateCache(out _);
		using var buffer = new NotificationBuffer();
		await using var subscription = await cache.SubscribeAsync(buffer, new DistributedCacheSubscriptionOptions(null, DistributedCacheNotificationKind.Updated));

		Assert.True(await cache.SetValueAsync("plain", "value"));
		Assert.True(await cache.SetValueAsync("plain", "overwritten"));
		Assert.True(await cache.SetValueAsync("expiring", "value", TimeSpan.FromSeconds(30)));
		Assert.True(cache.SetValue("set", new HashSet<string> { "alpha", "beta" }));

		Assert.Equal(new DistributedCacheNotification(DistributedCacheNotificationKind.Updated, "plain"), await buffer.ReceiveRequiredAsync());
		Assert.Equal(new DistributedCacheNotification(DistributedCacheNotificationKind.Updated, "plain"), await buffer.ReceiveRequiredAsync());
		Assert.Equal(new DistributedCacheNotification(DistributedCacheNotificationKind.Updated, "expiring"), await buffer.ReceiveRequiredAsync());
		Assert.Equal(new DistributedCacheNotification(DistributedCacheNotificationKind.Updated, "set"), await buffer.ReceiveRequiredAsync());
		Assert.True(await cache.SetExpiryAsync("plain", TimeSpan.FromSeconds(30)));
		Assert.Null(await buffer.ReceiveAsync(TimeSpan.FromMilliseconds(750)));
	}

	[Fact]
	public async Task ClearNotification_ReportsEveryRemovedKey()
	{
		EnsureRedisNotifications();

		await using var cache = CreateCache(out _);
		using var buffer = new NotificationBuffer();
		await using var subscription = await cache.SubscribeAsync(buffer);

		Assert.True(await cache.SetValueAsync("clear-one", "value"));
		Assert.True(await cache.SetValueAsync("clear-two", "value"));
		Assert.Equal(DistributedCacheNotificationKind.Updated, (await buffer.ReceiveRequiredAsync()).Kind);
		Assert.Equal(DistributedCacheNotificationKind.Updated, (await buffer.ReceiveRequiredAsync()).Kind);

		await cache.ClearAsync();

		var notifications = new[] { await buffer.ReceiveRequiredAsync(), await buffer.ReceiveRequiredAsync() };
		Assert.All(notifications, notification => Assert.Equal(DistributedCacheNotificationKind.Removed, notification.Kind));
		Assert.Equal(["clear-one", "clear-two"], notifications.Select(notification => notification.Key).Order().ToArray());
	}

	[Fact]
	public async Task RemovedAndExpiredNotifications_ReportActualRemovalReason()
	{
		EnsureRedisNotifications();

		await using var cache = CreateCache(out _);
		using var buffer = new NotificationBuffer();
		await using var subscription = await cache.SubscribeAsync(buffer);

		Assert.True(await cache.SetValueAsync("removed", "value"));
		Assert.Equal(DistributedCacheNotificationKind.Updated, (await buffer.ReceiveRequiredAsync()).Kind);
		Assert.True(await cache.RemoveAsync("removed"));

		var removed = await buffer.ReceiveRequiredAsync();
		Assert.Equal(DistributedCacheNotificationKind.Removed, removed.Kind);
		Assert.Equal("removed", removed.Key);

		Assert.True(await cache.SetValueAsync("expired", "value", TimeSpan.FromMilliseconds(250)));
		Assert.Equal(DistributedCacheNotificationKind.Updated, (await buffer.ReceiveRequiredAsync()).Kind);

		var expired = await buffer.ReceiveRequiredAsync(TimeSpan.FromSeconds(15));
		Assert.Equal(DistributedCacheNotificationKind.Expired, expired.Kind);
		Assert.Equal("expired", expired.Key);
	}

	[Fact]
	public async Task RenameNotification_ReportsRemovedSourceAndUpdatedDestination()
	{
		EnsureRedisNotifications();

		await using var cache = CreateCache(out _);
		using var buffer = new NotificationBuffer();
		await using var subscription = await cache.SubscribeAsync(buffer);

		Assert.True(await cache.SetValueAsync("before", "value"));
		Assert.Equal(DistributedCacheNotificationKind.Updated, (await buffer.ReceiveRequiredAsync()).Kind);
		Assert.True(await cache.RenameAsync("before", "after"));

		var notifications = new[] { await buffer.ReceiveRequiredAsync(), await buffer.ReceiveRequiredAsync() };
		Assert.Contains(new DistributedCacheNotification(DistributedCacheNotificationKind.Removed, "before"), notifications);
		Assert.Contains(new DistributedCacheNotification(DistributedCacheNotificationKind.Updated, "after"), notifications);
	}

	[Fact]
	public async Task ExternalClientAndFilters_ApplyLogicalPrefixKindsAndNamespace()
	{
		EnsureRedisNotifications();

		await using var cache = CreateCache(out var cacheNamespace);
		using var buffer = new NotificationBuffer();
		await using var subscription = await cache.SubscribeAsync(buffer,
			new DistributedCacheSubscriptionOptions("Case:", DistributedCacheNotificationKind.Removed));
		using var connection = CreateConnection();
		var database = connection.GetDatabase(cache.DatabaseId);

		await database.StringSetAsync($"{cacheNamespace}:Case:matched", "value");
		await database.StringSetAsync($"{cacheNamespace}:case:wrong-case", "value");
		await database.StringSetAsync($"other-{cacheNamespace}:Case:wrong-namespace", "value");
		await database.KeyDeleteAsync($"{cacheNamespace}:Case:matched");
		await database.KeyDeleteAsync($"{cacheNamespace}:case:wrong-case");
		await database.KeyDeleteAsync($"other-{cacheNamespace}:Case:wrong-namespace");

		var notification = await buffer.ReceiveRequiredAsync();
		Assert.Equal(DistributedCacheNotificationKind.Removed, notification.Kind);
		Assert.Equal("Case:matched", notification.Key);
		Assert.Null(await buffer.ReceiveAsync(TimeSpan.FromMilliseconds(750)));
	}

	[Fact]
	public async Task MatchingSubscriptions_FanOutTheSameNotification()
	{
		EnsureRedisNotifications();

		await using var cache = CreateCache(out _);
		using var first = new NotificationBuffer();
		using var second = new NotificationBuffer();
		await using var firstSubscription = await cache.SubscribeAsync(first);
		await using var secondSubscription = await cache.SubscribeAsync(second);

		Assert.True(await cache.SetValueAsync("fan-out", "value"));

		var expected = new DistributedCacheNotification(DistributedCacheNotificationKind.Updated, "fan-out");
		Assert.Equal(expected, await first.ReceiveRequiredAsync());
		Assert.Equal(expected, await second.ReceiveRequiredAsync());
	}

	[Fact]
	public async Task Subscription_ProcessesSeriallyAndContinuesAfterHandlerFailure()
	{
		EnsureRedisNotifications();

		await using var cache = CreateCache(out _);
		using var handler = new FailingSerialHandler(3);
		await using var subscription = await cache.SubscribeAsync(handler,
			new DistributedCacheSubscriptionOptions(null, DistributedCacheNotificationKind.Updated));

		await cache.SetValueAsync("sequence-1", "value");
		await cache.SetValueAsync("sequence-2", "value");
		await cache.SetValueAsync("sequence-3", "value");
		await handler.WaitAsync(TimeSpan.FromSeconds(10));

		Assert.Equal(1, handler.MaximumConcurrency);
		Assert.Equal(["sequence-1", "sequence-2", "sequence-3"], handler.Keys);
		Assert.Equal(3, handler.CallCount);
	}

	[Fact]
	public async Task SubscriptionLifecycle_IsIdempotentAndStopsDelivery()
	{
		EnsureRedisNotifications();

		await using var cache = CreateCache(out _);
		using var buffer = new NotificationBuffer();
		var subscription = await cache.SubscribeAsync(buffer);

		await subscription.UnsubscribeAsync();
		await subscription.UnsubscribeAsync();
		await subscription.CloseAsync();
		await subscription.DisposeAsync();
		await subscription.DisposeAsync();

		Assert.True(subscription.IsClosed);
		Assert.True(subscription.IsDisposed);
		Assert.Throws<InvalidOperationException>(() => cache.Namespace = $"changed-{Guid.NewGuid():N}");
		Assert.Throws<InvalidOperationException>(() => cache.Use(cache.DatabaseId + 1));
		Assert.True(await cache.SetValueAsync("after-unsubscribe", "value"));
		Assert.Null(await buffer.ReceiveAsync(TimeSpan.FromMilliseconds(750)));
	}

	[Fact]
	public async Task UnsubscribeAsync_CancelsInFlightHandler()
	{
		EnsureRedisNotifications();

		await using var cache = CreateCache(out _);
		using var handler = new CancellationHandler();
		await using var subscription = await cache.SubscribeAsync(handler);

		Assert.True(await cache.SetValueAsync("cancel-handler", "value"));
		await handler.Started.WaitAsync(TimeSpan.FromSeconds(10));
		await subscription.UnsubscribeAsync();

		await handler.Canceled.WaitAsync(TimeSpan.FromSeconds(10));
		Assert.True(subscription.IsClosed);
		Assert.Equal(1, handler.CallCount);
	}

	[Fact]
	public async Task ActiveSubscription_GuardsScopeAndServiceDisposalClosesSubscription()
	{
		EnsureRedisNotifications();

		var cache = CreateCache(out var cacheNamespace);
		using var buffer = new NotificationBuffer();
		var subscription = await cache.SubscribeAsync(buffer);

		cache.Namespace = cacheNamespace;
		cache.Use(cache.DatabaseId);
		Assert.Throws<InvalidOperationException>(() => cache.Namespace = $"other-{cacheNamespace}");
		Assert.Throws<InvalidOperationException>(() => cache.Use(cache.DatabaseId + 1));

		await cache.DisposeAsync();

		Assert.True(subscription.IsClosed);
		Assert.True(subscription.IsDisposed);
		await subscription.DisposeAsync();
	}

	[Fact]
	public async Task SynchronousServiceDispose_ClosesActiveSubscription()
	{
		EnsureRedisNotifications();

		var cache = CreateCache(out _);
		using var buffer = new NotificationBuffer();
		var subscription = await cache.SubscribeAsync(buffer);

		cache.Dispose();

		Assert.True(subscription.IsClosed);
		Assert.True(subscription.IsDisposed);
		await subscription.DisposeAsync();
	}

	private static RedisService CreateCache(out string cacheNamespace)
	{
		cacheNamespace = $"Zongsoft.Tests.Cache.{Guid.NewGuid():N}";
		return new RedisService($"cache-tests-{Guid.NewGuid():N}",
			$"server={RedisTestUtility.Server};password={RedisTestUtility.Password};timeout=5s;")
		{
			Namespace = cacheNamespace,
		};
	}

	private static ConnectionMultiplexer CreateConnection() =>
		ConnectionMultiplexer.Connect($"{RedisTestUtility.Server},password={RedisTestUtility.Password},connectTimeout=2000,allowAdmin=true");

	private static void EnsureRedisNotifications()
	{
		Assert.SkipUnless(RedisTestUtility.IsTestingEnabled, TESTS_DISABLED);
		Assert.SkipUnless(RedisTestUtility.IsAvailable(), REDIS_UNAVAILABLE);
		Assert.SkipUnless(HasRedisNotifications(), NOTIFICATIONS_UNAVAILABLE);
	}

	private static bool HasRedisNotifications()
	{
		try
		{
			using var connection = CreateConnection();
			var server = connection.GetServer(connection.GetEndPoints()[0]);
			var value = (string)server.ConfigGet("notify-keyspace-events").FirstOrDefault().Value;
			return value?.Contains('K') == true && value.Contains('A');
		}
		catch
		{
			return false;
		}
	}

	private sealed class NotificationBuffer : HandlerBase<DistributedCacheNotification>, IDisposable
	{
		private readonly ConcurrentQueue<DistributedCacheNotification> _notifications = new();
		private readonly SemaphoreSlim _signal = new(0);

		public async Task<DistributedCacheNotification?> ReceiveAsync(TimeSpan timeout)
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

			return _notifications.TryDequeue(out var notification) ? notification : null;
		}

		public async Task<DistributedCacheNotification> ReceiveRequiredAsync(TimeSpan? timeout = null)
		{
			var notification = await this.ReceiveAsync(timeout ?? TimeSpan.FromSeconds(10));
			Assert.True(notification.HasValue, "Timed out waiting for a Redis cache notification.");
			return notification.Value;
		}

		public void Dispose() => _signal.Dispose();

		protected override ValueTask OnHandleAsync(DistributedCacheNotification notification, Zongsoft.Collections.Parameters parameters, CancellationToken cancellation)
		{
			_notifications.Enqueue(notification);
			_signal.Release();
			return ValueTask.CompletedTask;
		}
	}

	private sealed class FailingSerialHandler(int expectedCount) : HandlerBase<DistributedCacheNotification>, IDisposable
	{
		private readonly TaskCompletionSource _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
		private readonly List<string> _keys = new(expectedCount);
		private readonly object _sync = new();
		private int _active;
		private int _maximumConcurrency;
		private int _callCount;

		public int CallCount => Volatile.Read(ref _callCount);
		public int MaximumConcurrency => Volatile.Read(ref _maximumConcurrency);
		public string[] Keys { get { lock(_sync) return _keys.ToArray(); } }

		public async Task WaitAsync(TimeSpan timeout) => await _completion.Task.WaitAsync(timeout);
		public void Dispose() => _completion.TrySetCanceled();

		protected override async ValueTask OnHandleAsync(DistributedCacheNotification notification, Zongsoft.Collections.Parameters parameters, CancellationToken cancellation)
		{
			var active = Interlocked.Increment(ref _active);
			InterlockedExtensions.Max(ref _maximumConcurrency, active);

			try
			{
				lock(_sync)
					_keys.Add(notification.Key);

				var count = Interlocked.Increment(ref _callCount);
				await Task.Delay(75, cancellation);

				if(count == 1)
					throw new InvalidOperationException("Expected handler failure.");

				if(count == expectedCount)
					_completion.TrySetResult();
			}
			finally
			{
				Interlocked.Decrement(ref _active);
			}
		}
	}

	private sealed class CancellationHandler : HandlerBase<DistributedCacheNotification>, IDisposable
	{
		private readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
		private readonly TaskCompletionSource _canceled = new(TaskCreationOptions.RunContinuationsAsynchronously);
		private int _callCount;

		public Task Started => _started.Task;
		public Task Canceled => _canceled.Task;
		public int CallCount => Volatile.Read(ref _callCount);

		public void Dispose()
		{
			_started.TrySetCanceled();
			_canceled.TrySetCanceled();
		}

		protected override async ValueTask OnHandleAsync(DistributedCacheNotification notification, Zongsoft.Collections.Parameters parameters, CancellationToken cancellation)
		{
			Interlocked.Increment(ref _callCount);
			_started.TrySetResult();

			try
			{
				await Task.Delay(Timeout.InfiniteTimeSpan, cancellation);
			}
			catch(OperationCanceledException) when (cancellation.IsCancellationRequested)
			{
				_canceled.TrySetResult();
				throw;
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
