using System;
using System.Linq;
using System.Collections.Generic;

using Xunit;

namespace Zongsoft.Externals.Redis.Tests;

public class RedisDictionaryTests
{
	private const string REDIS_UNAVAILABLE = "Redis is unavailable at 127.0.0.1:6379.";
	private const string TESTS_DISABLED = "Redis integration tests require ZONGSOFT_REDIS_TESTS=1.";

	[Fact]
	public void KeysAndValues_ReturnConvertedStringCollections()
	{
		EnsureRedis();

		using var cache = CreateCache();
		var dictionary = cache.CreateDictionary("dictionary");
		dictionary.Add("alpha", "one");
		dictionary.Add("beta", "two");

		Assert.Equal(["alpha", "beta"], dictionary.Keys.Order().ToArray());
		Assert.Equal(["one", "two"], dictionary.Values.Order().ToArray());
		Assert.All(dictionary.Keys, key => Assert.IsType<string>(key));
		Assert.All(dictionary.Values, value => Assert.IsType<string>(value));
	}

	[Fact]
	public void PairContainsAndRemove_RequireBothKeyAndValueToMatch()
	{
		EnsureRedis();

		using var cache = CreateCache();
		var dictionary = cache.CreateDictionary("dictionary");
		var pairs = (ICollection<KeyValuePair<string, string>>)dictionary;
		var exact = new KeyValuePair<string, string>("alpha", "one");
		var mismatched = new KeyValuePair<string, string>("alpha", "different");

		dictionary.Add(exact.Key, exact.Value);
		Assert.True(pairs.Contains(exact));
		Assert.False(pairs.Contains(mismatched));
		Assert.False(pairs.Remove(mismatched));
		Assert.True(dictionary.ContainsKey("alpha"));
		Assert.True(pairs.Remove(exact));
		Assert.False(dictionary.ContainsKey("alpha"));
		Assert.False(pairs.Remove(exact));
	}

	[Fact]
	public void CopyTo_ValidatesBoundsAndCopiesEveryEntryAtOffset()
	{
		EnsureRedis();

		using var cache = CreateCache();
		var dictionary = cache.CreateDictionary("dictionary");
		dictionary.Add("alpha", "one");
		dictionary.Add("beta", "two");

		Assert.Throws<ArgumentNullException>(() => dictionary.CopyTo(null, 0));
		Assert.Throws<ArgumentOutOfRangeException>(() => dictionary.CopyTo(new KeyValuePair<string, string>[2], -1));
		Assert.Throws<ArgumentOutOfRangeException>(() => dictionary.CopyTo(new KeyValuePair<string, string>[2], 3));
		Assert.Throws<ArgumentException>(() => dictionary.CopyTo(new KeyValuePair<string, string>[2], 1));

		var destination = new KeyValuePair<string, string>[3];
		dictionary.CopyTo(destination, 1);

		Assert.Equal(default, destination[0]);
		Assert.Equal(new Dictionary<string, string> { ["alpha"] = "one", ["beta"] = "two" },
			destination.Skip(1).ToDictionary(entry => entry.Key, entry => entry.Value));
	}

	private static RedisService CreateCache() => new($"dictionary-{Guid.NewGuid():N}",
		$"server={Global.Server};password={Global.Password};timeout=5s;")
	{
		Namespace = $"Zongsoft.Tests.Dictionary.{Guid.NewGuid():N}",
	};

	private static void EnsureRedis()
	{
		Assert.SkipUnless(Global.IsTestingEnabled, TESTS_DISABLED);
		Assert.SkipUnless(Global.IsAvailable(), REDIS_UNAVAILABLE);
	}
}
