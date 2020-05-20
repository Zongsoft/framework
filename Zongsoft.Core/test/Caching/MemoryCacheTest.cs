using System;
using System.Threading;

using Xunit;

namespace Zongsoft.Caching
{
	public class MemoryCacheTest
	{
		private MemoryCache _cache = new MemoryCache();

		[Fact]
		public void TestSetValue()
		{
			string KEY = Guid.NewGuid().ToString();

			Assert.True(_cache.SetValue(KEY, "Value#0"));
			Assert.True(_cache.SetValue(KEY, "Value#1", CacheRequisite.Always));
			Assert.True(_cache.SetValue(KEY, "Value#2", CacheRequisite.Exists));
			Assert.False(_cache.SetValue(KEY, "Value#3", CacheRequisite.NotExists));
		}

		[Fact]
		public void TestSetValueWithExpiry()
		{
			var e = Assert.RaisesAny<CacheChangedEventArgs>(a => _cache.Changed += a, b => _cache.Changed -= b, () =>
			{
				_cache.SetValue("K1", "V1", TimeSpan.FromSeconds(1));
				Thread.Sleep(TimeSpan.FromMilliseconds(1100));
			});

			Assert.Equal(CacheChangedReason.Expired, e.Arguments.Reason);
			Assert.Equal("K1", e.Arguments.Key);
			Assert.Equal("V1", e.Arguments.OldValue);
			Assert.Empty(_cache);
		}
	}
}
