using System;
using System.Collections.Generic;

using Zongsoft.Tests;

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
		Assert.False(cache.Exists("KEY"));
		Assert.False(cache.Remove("KEY"));
		Assert.False(cache.TryGetValue("KEY", out _));

		value = cache.GetOrCreate("K1", () => "V1");
		Assert.NotNull(value);
		Assert.Equal("V1", value);
		Assert.True(cache.Remove("K1"));
		Assert.Equal(0, cache.Count);
	}
}
