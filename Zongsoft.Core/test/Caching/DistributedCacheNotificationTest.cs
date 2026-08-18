using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Xunit;

using Zongsoft.Components;
using Zongsoft.Communication;

namespace Zongsoft.Caching.Tests;

public class DistributedCacheNotificationTest
{
	[Theory]
	[InlineData(DistributedCacheNotificationKind.None, 0)]
	[InlineData(DistributedCacheNotificationKind.Updated, 1)]
	[InlineData(DistributedCacheNotificationKind.Removed, 2)]
	[InlineData(DistributedCacheNotificationKind.Expired, 4)]
	[InlineData(DistributedCacheNotificationKind.Evicted, 8)]
	[InlineData(DistributedCacheNotificationKind.All, 15)]
	public void Kind_HasExpectedFlags(DistributedCacheNotificationKind kind, int value)
	{
		Assert.Equal(value, (int)kind);
	}

	[Theory]
	[InlineData(DistributedCacheNotificationKind.Updated, "[Updated] cache-key")]
	[InlineData(DistributedCacheNotificationKind.Removed, "[Removed] cache-key")]
	[InlineData(DistributedCacheNotificationKind.Expired, "[Expired] cache-key")]
	[InlineData(DistributedCacheNotificationKind.Evicted, "[Evicted] cache-key")]
	public void Constructor_SingleKind_InitializesNotification(DistributedCacheNotificationKind kind, string text)
	{
		var notification = new DistributedCacheNotification(kind, "cache-key");

		Assert.Equal(kind, notification.Kind);
		Assert.Equal("cache-key", notification.Key);
		Assert.False(notification.IsEmpty);
		Assert.Equal(text, notification.ToString());
	}

	[Theory]
	[InlineData(DistributedCacheNotificationKind.None)]
	[InlineData(DistributedCacheNotificationKind.Updated | DistributedCacheNotificationKind.Removed)]
	[InlineData((DistributedCacheNotificationKind)16)]
	[InlineData((DistributedCacheNotificationKind)(-1))]
	public void Constructor_InvalidKind_ThrowsArgumentOutOfRangeException(DistributedCacheNotificationKind kind)
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => new DistributedCacheNotification(kind, "cache-key"));
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	public void Constructor_EmptyKey_ThrowsArgumentNullException(string key)
	{
		Assert.Throws<ArgumentNullException>(() => new DistributedCacheNotification(DistributedCacheNotificationKind.Updated, key));
	}

	[Fact]
	public void DefaultNotification_IsEmpty()
	{
		var notification = default(DistributedCacheNotification);

		Assert.Equal(DistributedCacheNotificationKind.None, notification.Kind);
		Assert.Null(notification.Key);
		Assert.True(notification.IsEmpty);
		Assert.Equal(string.Empty, notification.ToString());
	}

	[Fact]
	public void SubscriptionOptions_DefaultsAndConstructor_PreserveValues()
	{
		var defaults = new DistributedCacheSubscriptionOptions();
		var options = new DistributedCacheSubscriptionOptions("Case:Sensitive", DistributedCacheNotificationKind.Updated | DistributedCacheNotificationKind.Expired);

		Assert.Null(defaults.Prefix);
		Assert.Equal(DistributedCacheNotificationKind.All, defaults.Kind);
		Assert.Equal(1024, defaults.Capacity);
		Assert.Equal(DistributedCacheNotificationOverflowPolicy.DropOldest, defaults.OverflowPolicy);
		Assert.Null(DistributedCacheSubscriptionOptions.Default.Prefix);
		Assert.Equal(DistributedCacheNotificationKind.All, DistributedCacheSubscriptionOptions.Default.Kind);
		Assert.Equal(1024, DistributedCacheSubscriptionOptions.Default.Capacity);
		Assert.Equal(DistributedCacheNotificationOverflowPolicy.DropOldest, DistributedCacheSubscriptionOptions.Default.OverflowPolicy);
		Assert.Equal("Case:Sensitive", options.Prefix);
		Assert.Equal(DistributedCacheNotificationKind.Updated | DistributedCacheNotificationKind.Expired, options.Kind);
	}

	[Fact]
	public void SubscriptionOptions_SnapshotIsImmutableAndIndependentFromSource()
	{
		var source = new DistributedCacheSubscriptionOptions("before", DistributedCacheNotificationKind.Updated)
		{
			Capacity = 3,
			OverflowPolicy = DistributedCacheNotificationOverflowPolicy.DropNewest,
		};
		var snapshot = source.Snapshot();

		source.Prefix = "after";
		source.Kind = DistributedCacheNotificationKind.Removed;
		source.Capacity = 7;
		source.OverflowPolicy = DistributedCacheNotificationOverflowPolicy.DropOldest;

		Assert.Equal("before", snapshot.Prefix);
		Assert.Equal(DistributedCacheNotificationKind.Updated, snapshot.Kind);
		Assert.Equal(3, snapshot.Capacity);
		Assert.Equal(DistributedCacheNotificationOverflowPolicy.DropNewest, snapshot.OverflowPolicy);
		Assert.Throws<NotSupportedException>(() => snapshot.Prefix = "mutated");
		Assert.Throws<NotSupportedException>(() => snapshot.Kind = DistributedCacheNotificationKind.Expired);
		Assert.Throws<NotSupportedException>(() => snapshot.Capacity = 4);
		Assert.Throws<NotSupportedException>(() => snapshot.OverflowPolicy = DistributedCacheNotificationOverflowPolicy.DropOldest);
	}

	[Fact]
	public void SubscriptionOptions_InvalidCapacityAndOverflowThrow()
	{
		var options = new DistributedCacheSubscriptionOptions();

		Assert.Throws<ArgumentOutOfRangeException>(() => options.Capacity = 0);
		Assert.Throws<ArgumentOutOfRangeException>(() => options.Capacity = -1);
		Assert.Throws<ArgumentOutOfRangeException>(() => options.OverflowPolicy = (DistributedCacheNotificationOverflowPolicy)2);
		Assert.Equal(1024, options.Capacity);
		Assert.Equal(DistributedCacheNotificationOverflowPolicy.DropOldest, options.OverflowPolicy);
	}

	[Fact]
	public void SubscriptionInterface_DefaultCountersAreZero()
	{
		IDistributedCacheSubscription subscription = new DefaultCounterSubscription(new UnsupportedCache("cache"));

		Assert.Equal(0, subscription.PendingCount);
		Assert.Equal(0, subscription.DroppedCount);
	}

	[Fact]
	public async Task SubscribeAsync_UnsupportedCache_ThrowsNotSupportedException()
	{
		IDistributedCache cache = new UnsupportedCache("legacy");
		var handler = Handler.Handle<DistributedCacheNotification>(_ => { });

		var exception = await Assert.ThrowsAsync<NotSupportedException>(async () => await cache.SubscribeAsync(handler));

		Assert.Contains("legacy", exception.Message, StringComparison.Ordinal);
		Assert.Contains("does not support notifications", exception.Message, StringComparison.Ordinal);
	}

	private sealed class UnsupportedCache(string name) : IDistributedCache
	{
		public string Name { get; } = name;

		public long GetCount() => throw new NotImplementedException();
		public ValueTask<long> GetCountAsync(CancellationToken cancellation = default) => throw new NotImplementedException();
		public bool Exists(string key) => throw new NotImplementedException();
		public ValueTask<bool> ExistsAsync(string key, CancellationToken cancellation = default) => throw new NotImplementedException();
		public IEnumerable<string> Find(string pattern) => throw new NotImplementedException();
		public IAsyncEnumerable<string> FindAsync(string pattern, CancellationToken cancellation = default) => throw new NotImplementedException();
		public TimeSpan? GetExpiry(string key) => throw new NotImplementedException();
		public ValueTask<TimeSpan?> GetExpiryAsync(string key, CancellationToken cancellation = default) => throw new NotImplementedException();
		public bool SetExpiry(string key, TimeSpan expiry) => throw new NotImplementedException();
		public ValueTask<bool> SetExpiryAsync(string key, TimeSpan expiry, CancellationToken cancellation = default) => throw new NotImplementedException();
		public void Clear() => throw new NotImplementedException();
		public ValueTask ClearAsync(CancellationToken cancellation = default) => throw new NotImplementedException();
		public bool Remove(string key) => throw new NotImplementedException();
		public bool Remove(string key, out object value) => throw new NotImplementedException();
		public int Remove(IEnumerable<string> keys) => throw new NotImplementedException();
		public ValueTask<bool> RemoveAsync(string key, CancellationToken cancellation = default) => throw new NotImplementedException();
		public ValueTask<int> RemoveAsync(IEnumerable<string> keys, CancellationToken cancellation = default) => throw new NotImplementedException();
		public bool Rename(string oldKey, string newKey) => throw new NotImplementedException();
		public ValueTask<bool> RenameAsync(string oldKey, string newKey, CancellationToken cancellation = default) => throw new NotImplementedException();
		public object GetValue(string key) => throw new NotImplementedException();
		public T GetValue<T>(string key) => throw new NotImplementedException();
		public object GetValue(string key, out TimeSpan? expiry) => throw new NotImplementedException();
		public T GetValue<T>(string key, out TimeSpan? expiry) => throw new NotImplementedException();
		public ValueTask<object> GetValueAsync(string key, CancellationToken cancellation = default) => throw new NotImplementedException();
		public ValueTask<T> GetValueAsync<T>(string key, CancellationToken cancellation = default) => throw new NotImplementedException();
		public ValueTask<(object Value, TimeSpan? Expiry)> GetValueExpiryAsync(string key, CancellationToken cancellation = default) => throw new NotImplementedException();
		public ValueTask<(T Value, TimeSpan? Expiry)> GetValueExpiryAsync<T>(string key, CancellationToken cancellation = default) => throw new NotImplementedException();
		public bool TryGetValue<T>(string key, out T value) => throw new NotImplementedException();
		public bool TryGetValue<T>(string key, out T value, out TimeSpan? expiry) => throw new NotImplementedException();
		public ValueTask<(bool result, object value)> TryGetValueAsync(string key, CancellationToken cancellation = default) => throw new NotImplementedException();
		public ValueTask<(bool result, T value)> TryGetValueAsync<T>(string key, CancellationToken cancellation = default) => throw new NotImplementedException();
		public bool SetValue(string key, object value, CacheRequisite requisite = CacheRequisite.Always) => throw new NotImplementedException();
		public ValueTask<bool> SetValueAsync(string key, object value, CacheRequisite requisite = CacheRequisite.Always, CancellationToken cancellation = default) => throw new NotImplementedException();
		public bool SetValue(string key, object value, TimeSpan expiry, CacheRequisite requisite = CacheRequisite.Always) => throw new NotImplementedException();
		public ValueTask<bool> SetValueAsync(string key, object value, TimeSpan expiry, CacheRequisite requisite = CacheRequisite.Always, CancellationToken cancellation = default) => throw new NotImplementedException();
	}

	private sealed class DefaultCounterSubscription(IDistributedCache cache) : ChannelBase, IDistributedCacheSubscription
	{
		public IDistributedCache Cache { get; } = cache;
		public DistributedCacheSubscriptionOptions Options { get; } = DistributedCacheSubscriptionOptions.Default;
		public IHandler<DistributedCacheNotification> Handler { get; } = Zongsoft.Components.Handler.Handle<DistributedCacheNotification>(_ => { });

		public ValueTask UnsubscribeAsync(CancellationToken cancellation = default) => this.CloseAsync(cancellation);
		protected override ValueTask OnCloseAsync(CancellationToken cancellation) => ValueTask.CompletedTask;
	}
}
