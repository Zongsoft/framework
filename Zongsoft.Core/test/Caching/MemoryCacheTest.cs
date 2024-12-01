using System;
using System.Threading;
using System.Threading.Tasks;

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
		Assert.False(cache.Remove("KEY"));
		Assert.False(cache.TryGetValue("KEY", out _));

		value = cache.GetOrCreate("K1", () => "V1");
		Assert.NotNull(value);
		Assert.True(cache.Contains("K1"));
		Assert.Equal("V1", value);
		Assert.True(cache.Remove("K1"));
		Assert.Equal(0, cache.Count);

		const int COUNT = 10000;
		Parallel.For(0, COUNT, index => cache.SetValue($"KEY#{index}", $"Value#{index}@{Environment.CurrentManagedThreadId}"));
		Assert.Equal(COUNT, cache.Count);
	}
}
