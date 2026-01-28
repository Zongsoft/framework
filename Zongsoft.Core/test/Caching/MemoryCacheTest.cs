using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Primitives;

using Xunit;

namespace Zongsoft.Caching.Tests;

public class MemoryCacheTest
{
	[Fact]
	public void Test()
	{
		object value;

		var cache = new MemoryCache();
		Assert.Equal(0, cache.Count);
		Assert.False(cache.Contains("KEY"));
		Assert.False(cache.Remove("KEY", out _));
		Assert.False(cache.TryGetValue("KEY", out _));

		value = cache.GetOrCreate("K1", () => "V1");
		Assert.NotNull(value);
		Assert.True(cache.Contains("K1"));
		Assert.Equal("V1", value);
		Assert.True(cache.Remove("K1", out value));
		Assert.Equal("V1", value);
		Assert.Equal(0, cache.Count);

		const int COUNT = 10000;
		Parallel.For(0, COUNT, index => cache.SetValue($"KEY#{index}", $"Value#{index}@{Environment.CurrentManagedThreadId}"));
		Assert.Equal(COUNT, cache.Count);
	}

	[Fact]
	public void TestDependency()
	{
		var cache = new MemoryCache();
		cache.Evicted += this.Cache_Evicted;

		var cancellation = new CancellationTokenSource();
		var value = cache.GetOrCreate("KEY", key =>
		{
			return ("Value1", new CancellationChangeToken(cancellation.Token));
		});

		Assert.NotNull(value);
		Assert.Equal("Value1", value);
		Assert.Equal(1, cache.Count);

		//通知缓存项过期
		cancellation.Cancel();
		Assert.False(cache.Contains("KEY"));
		Assert.Equal(0, cache.Count);

		//清理缓存
		cache.Compact();

		//等待缓存项过期事件的触发
		SpinWait.SpinUntil(() => _reason.HasValue);

		//确认缓存过期的原因
		Assert.Equal(CacheEvictedReason.Depended, _reason);

		//创建一个已经过期的缓存项
		value = cache.GetOrCreate("KEY", key =>
		{
			return ("Value2", new CancellationChangeToken(cancellation.Token));
		});

		Assert.NotNull(value);
		Assert.Equal("Value2", value);
		Assert.False(cache.Contains("KEY"));
		Assert.Equal(0, cache.Count);

		//创建一个依赖失效的缓存项
		value = cache.GetOrCreate("KEY", key =>
		{
			return ("Value3", Common.Notification.GetToken());
		});

		Assert.NotNull(value);
		Assert.Equal("Value3", value);
		Assert.False(cache.Contains("KEY"));
		Assert.Equal(0, cache.Count);
	}

	[Fact]
	public void TestOptions()
	{
		Assert.NotNull(MemoryCache.Shared.Options);
		Assert.Throws<InvalidOperationException>(() => MemoryCache.Shared.Options.CountLimit = 100);
		Assert.Throws<InvalidOperationException>(() => MemoryCache.Shared.Options.ScanFrequency = TimeSpan.Zero);

		var cache = new MemoryCache();
		Assert.NotNull(cache.Options);
		cache.Options.CountLimit = 100;
		Assert.Equal(100, cache.Options.CountLimit);
		cache.Options.ScanFrequency = TimeSpan.FromSeconds(10);
		Assert.Equal(TimeSpan.FromSeconds(10), cache.Options.ScanFrequency);
	}

	private CacheEvictedReason? _reason;
	private void Cache_Evicted(object sender, CacheEvictedEventArgs args) => _reason = args.Reason;
}
