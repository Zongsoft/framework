using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using StackExchange.Redis;

using Xunit;

namespace Zongsoft.Externals.Redis.Tests;

public class RedisHashsetTests
{
	private const string REDIS_UNAVAILABLE = "Redis is unavailable at 127.0.0.1:6379.";
	private const string TESTS_DISABLED = "Redis integration tests require ZONGSOFT_REDIS_TESTS=1.";

	public static TheoryData<string, string[], string[]> SetOperations => new()
	{
		{ nameof(ISet<string>.ExceptWith), ["beta", "gamma", "gamma"], ["alpha"] },
		{ nameof(ISet<string>.IntersectWith), ["beta", "gamma", "gamma"], ["beta"] },
		{ nameof(ISet<string>.SymmetricExceptWith), ["beta", "gamma", "gamma"], ["alpha", "gamma"] },
		{ nameof(ISet<string>.UnionWith), ["beta", "gamma", "gamma"], ["alpha", "beta", "gamma"] },
	};

	[Theory]
	[MemberData(nameof(SetOperations))]
	public void SetAlgebra_DuplicatesAndNullMembers_MatchesHashSet(string operation, string[] items, string[] expected)
	{
		EnsureRedis();

		using var cache = CreateCache(out var cacheNamespace);
		Assert.True(cache.SetValue("set", new HashSet<string> { "alpha", "beta" }));
		var set = Assert.IsType<RedisHashset>(cache.GetValue<ISet<string>>("set"));
		var operand = items.Concat([null]);

		switch(operation)
		{
			case nameof(ISet<string>.ExceptWith):
				set.ExceptWith(operand);
				break;
			case nameof(ISet<string>.IntersectWith):
				set.IntersectWith(operand);
				break;
			case nameof(ISet<string>.SymmetricExceptWith):
				set.SymmetricExceptWith(operand);
				break;
			case nameof(ISet<string>.UnionWith):
				set.UnionWith(operand);
				break;
			default:
				throw new InvalidOperationException(operation);
		}

		Assert.Equal(expected.Order(), set.Order());
		AssertNoTemporaryKeys(cacheNamespace, "set");
	}

	[Fact]
	public void SetRelationships_DuplicatesEmptyAndNullMembers_MatchHashSet()
	{
		EnsureRedis();

		using var cache = CreateCache(out _);
		Assert.True(cache.SetValue("set", new HashSet<string> { "alpha", "beta" }));
		var set = Assert.IsType<RedisHashset>(cache.GetValue<ISet<string>>("set"));

		Assert.True(set.IsProperSubsetOf(["alpha", "beta", "gamma", "gamma", null]));
		Assert.False(set.IsProperSubsetOf(["alpha", "beta", "beta", null]));
		Assert.True(set.IsProperSupersetOf(["alpha", "alpha", null]));
		Assert.False(set.IsProperSupersetOf(["alpha", "beta", "beta", null]));
		Assert.True(set.IsSubsetOf(["alpha", "beta", "gamma", null]));
		Assert.False(set.IsSubsetOf(["alpha", null]));
		Assert.True(set.IsSupersetOf(["alpha", null]));
		Assert.False(set.IsSupersetOf(["gamma", null]));
		Assert.True(set.Overlaps(["gamma", "beta", null]));
		Assert.False(set.Overlaps(["gamma", null]));
		Assert.True(set.SetEquals(["beta", "alpha", "alpha", null]));
		Assert.False(set.SetEquals(["alpha", null]));
		Assert.False(set.SetEquals(Array.Empty<string>()));
	}

	[Fact]
	public void SetAlgebra_EmptyAndSelfOperands_HaveStandardSemantics()
	{
		EnsureRedis();

		using var cache = CreateCache(out var cacheNamespace);
		Assert.True(cache.SetValue("set", new HashSet<string> { "alpha", "beta" }));
		var set = Assert.IsType<RedisHashset>(cache.GetValue<ISet<string>>("set"));

		set.ExceptWith(Array.Empty<string>());
		Assert.Equal(["alpha", "beta"], set.Order().ToArray());
		set.IntersectWith(set);
		Assert.Equal(["alpha", "beta"], set.Order().ToArray());
		set.UnionWith(set);
		Assert.Equal(["alpha", "beta"], set.Order().ToArray());
		set.SymmetricExceptWith(Array.Empty<string>());
		Assert.Equal(["alpha", "beta"], set.Order().ToArray());

		set.ExceptWith(set);
		Assert.Empty(set);
		AssertNoTemporaryKeys(cacheNamespace, "set");

		Assert.True(cache.SetValue("set", new HashSet<string> { "alpha", "beta" }));
		set = Assert.IsType<RedisHashset>(cache.GetValue<ISet<string>>("set"));
		set.SymmetricExceptWith(set);
		Assert.Empty(set);
		AssertNoTemporaryKeys(cacheNamespace, "set");
	}

	[Fact]
	public void SetMethods_NullEnumerable_ThrowArgumentNullException()
	{
		EnsureRedis();

		using var cache = CreateCache(out _);
		Assert.True(cache.SetValue("set", new HashSet<string> { "alpha" }));
		var set = Assert.IsType<RedisHashset>(cache.GetValue<ISet<string>>("set"));

		Assert.Throws<ArgumentNullException>(() => set.ExceptWith(null));
		Assert.Throws<ArgumentNullException>(() => set.IntersectWith(null));
		Assert.Throws<ArgumentNullException>(() => set.SymmetricExceptWith(null));
		Assert.Throws<ArgumentNullException>(() => set.UnionWith(null));
		Assert.Throws<ArgumentNullException>(() => set.IsProperSubsetOf(null));
		Assert.Throws<ArgumentNullException>(() => set.IsProperSupersetOf(null));
		Assert.Throws<ArgumentNullException>(() => set.IsSubsetOf(null));
		Assert.Throws<ArgumentNullException>(() => set.IsSupersetOf(null));
		Assert.Throws<ArgumentNullException>(() => set.Overlaps(null));
		Assert.Throws<ArgumentNullException>(() => set.SetEquals(null));
	}

	[Fact]
	public async Task Move_PreservesCacheNamespaceForDestination()
	{
		EnsureRedis();

		using var cache = CreateCache(out var cacheNamespace);
		using var connection = ConnectionMultiplexer.Connect($"{Global.Server},password={Global.Password},connectTimeout=2000");
		var database = connection.GetDatabase();
		var destination = $"destination-{Guid.NewGuid():N}";

		try
		{
			Assert.True(cache.SetValue("source", new HashSet<string> { "alpha", "beta" }));
			var source = Assert.IsType<RedisHashset>(cache.GetValue<ISet<string>>("source"));

			Assert.True(source.Move(destination, "alpha"));
			Assert.DoesNotContain("alpha", source);
			Assert.True(cache.GetValue<ISet<string>>(destination).Contains("alpha"));
			Assert.False(await database.KeyExistsAsync(destination));
			Assert.True(await database.SetContainsAsync($"{cacheNamespace}:{destination}", "alpha"));
		}
		finally
		{
			cache.Clear();
			await database.KeyDeleteAsync(destination);
		}
	}

	[Fact]
	public void CopyTo_ValidatesCapacityAndCopiesAtRequestedOffset()
	{
		EnsureRedis();

		using var cache = CreateCache(out _);
		Assert.True(cache.SetValue("set", new HashSet<string> { "alpha", "beta" }));
		var set = (ICollection<string>)cache.GetValue<ISet<string>>("set");

		Assert.Throws<ArgumentNullException>(() => set.CopyTo(null, 0));
		Assert.Throws<ArgumentOutOfRangeException>(() => set.CopyTo(new string[2], -1));
		Assert.Throws<ArgumentOutOfRangeException>(() => set.CopyTo(new string[2], 3));
		Assert.Throws<ArgumentException>(() => set.CopyTo(new string[2], 1));

		var destination = new string[3];
		set.CopyTo(destination, 1);
		Assert.Null(destination[0]);
		Assert.Equal(["alpha", "beta"], destination.Skip(1).Order().ToArray());
	}

	private static RedisService CreateCache(out string cacheNamespace)
	{
		cacheNamespace = $"Zongsoft.Tests.Hashset.{Guid.NewGuid():N}";
		return new RedisService($"hashset-{Guid.NewGuid():N}",
			$"server={Global.Server};password={Global.Password};timeout=5s;")
		{
			Namespace = cacheNamespace,
		};
	}

	private static void AssertNoTemporaryKeys(string cacheNamespace, string key)
	{
		using var connection = ConnectionMultiplexer.Connect($"{Global.Server},password={Global.Password},connectTimeout=2000");
		var database = connection.GetDatabase();
		var server = connection.GetServer(database.IdentifyEndpoint());
		Assert.Empty(server.Keys(database.Database, $"{cacheNamespace}:{key}:*"));
	}

	private static void EnsureRedis()
	{
		Assert.SkipUnless(Global.IsTestingEnabled, TESTS_DISABLED);
		Assert.SkipUnless(Global.IsAvailable(), REDIS_UNAVAILABLE);
	}
}
