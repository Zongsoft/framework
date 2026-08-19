using System;
using System.Threading.Tasks;

using StackExchange.Redis;

using Xunit;

using Zongsoft.Services.Distributing;

namespace Zongsoft.Externals.Redis.Tests;

public class RedisDistributedLockTests
{
	private const string REDIS_UNAVAILABLE = "Redis is unavailable at 127.0.0.1:6379.";
	private const string TESTS_DISABLED = "Redis integration tests require ZONGSOFT_REDIS_TESTS=1.";

	[Fact]
	public async Task Acquire_RequiresPositiveExpiryAndValidRenewalInterval()
	{
		EnsureRedis();

		await using var cache = CreateCache(out _);
		await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await cache.AcquireAsync("zero", TimeSpan.Zero));
		await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await cache.AcquireAsync("negative", TimeSpan.FromMilliseconds(-1)));

		var options = new DistributedLockOptions(TimeSpan.FromSeconds(1)) { RenewalInterval = TimeSpan.FromSeconds(1) };
		await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await cache.AcquireAsync("invalid-renewal", options));
	}

	[Fact]
	public async Task SuccessfulAcquisitions_ReturnStrictlyIncreasingFencingTokens()
	{
		EnsureRedis();

		await using var cache = CreateCache(out var cacheNamespace);
		const string key = "fencing";

		try
		{
			await using var first = await cache.AcquireAsync(key, TimeSpan.FromSeconds(2));
			Assert.True(first.IsLocked);
			Assert.True(first.FencingToken > 0);

			await using var rejected = await cache.AcquireAsync(key, TimeSpan.FromSeconds(2));
			Assert.True(rejected.IsUnheld);
			Assert.Equal(0, rejected.FencingToken);

			var firstToken = first.FencingToken;
			await first.DisposeAsync();
			await using var second = await cache.AcquireAsync(key, TimeSpan.FromSeconds(2));
			Assert.True(second.IsLocked);
			Assert.True(second.FencingToken > firstToken);
		}
		finally
		{
			await DeleteLockKeysAsync(cache, cacheNamespace, key);
		}
	}

	[Fact]
	public async Task ManualRenewal_ExtendsLeaseAndOwnershipLossClearsHeldState()
	{
		EnsureRedis();

		await using var cache = CreateCache(out var cacheNamespace);
		const string key = "manual-renewal";

		try
		{
			await using var distributedLock = await cache.AcquireAsync(key, TimeSpan.FromMilliseconds(500));
			Assert.True(distributedLock.IsLocked);

			await Task.Delay(300);
			Assert.True(await distributedLock.RenewAsync());
			await Task.Delay(300);
			Assert.True(distributedLock.IsLocked);
			Assert.True((await cache.GetExpiryAsync(key)) > TimeSpan.Zero);

			await cache.Database.StringSetAsync($"{cacheNamespace}:{key}", "foreign-owner", TimeSpan.FromSeconds(2));
			Assert.False(await distributedLock.RenewAsync());
			Assert.True(distributedLock.IsUnheld);
			Assert.Equal("foreign-owner", (string)await cache.Database.StringGetAsync($"{cacheNamespace}:{key}"));
		}
		finally
		{
			await DeleteLockKeysAsync(cache, cacheNamespace, key);
		}
	}

	[Fact]
	public async Task AutoRenewal_IsOffByDefaultAndWhenEnabledKeepsOwnershipAlive()
	{
		EnsureRedis();

		await using var cache = CreateCache(out var cacheNamespace);

		try
		{
			await using(var manual = await cache.AcquireAsync("manual", TimeSpan.FromMilliseconds(250)))
			{
				await Task.Delay(500);
				Assert.True(manual.IsExpired);
				await using var successor = await cache.AcquireAsync("manual", TimeSpan.FromSeconds(1));
				Assert.True(successor.IsLocked);
				Assert.True(successor.FencingToken > manual.FencingToken);
				await manual.DisposeAsync();
				Assert.True(await successor.RenewAsync());
				Assert.True(await cache.ExistsAsync("manual"));
			}

			var options = new DistributedLockOptions(TimeSpan.FromMilliseconds(300))
			{
				RenewalInterval = TimeSpan.FromMilliseconds(75),
			};
			await using var renewed = await cache.AcquireAsync("automatic", options);
			await Task.Delay(800);
			Assert.True(renewed.IsLocked);

			await using var competitor = await cache.AcquireAsync("automatic", TimeSpan.FromSeconds(1));
			Assert.True(competitor.IsUnheld);
			Assert.Equal(0, competitor.FencingToken);

			var renewedToken = renewed.FencingToken;
			await renewed.DisposeAsync();
			await Task.Delay(150);
			await using var autoSuccessor = await cache.AcquireAsync("automatic", TimeSpan.FromSeconds(1));
			Assert.True(autoSuccessor.IsLocked);
			Assert.True(autoSuccessor.FencingToken > renewedToken);
		}
		finally
		{
			await DeleteLockKeysAsync(cache, cacheNamespace, "manual", "automatic");
		}
	}

	[Fact]
	public async Task EnterAsync_AfterInitialContentionStartsAutomaticRenewal()
	{
		EnsureRedis();

		await using var cache = CreateCache(out var cacheNamespace);
		const string key = "entered-renewal";

		try
		{
			await using var holder = await cache.AcquireAsync(key, TimeSpan.FromSeconds(2));
			Assert.True(holder.IsLocked);

			var options = new DistributedLockOptions(TimeSpan.FromMilliseconds(300))
			{
				RenewalInterval = TimeSpan.FromMilliseconds(75),
			};
			await using var contender = await cache.AcquireAsync(key, options);
			Assert.True(contender.IsUnheld);
			Assert.Equal(0, contender.FencingToken);

			var holderToken = holder.FencingToken;
			await holder.DisposeAsync();
			await contender.EnterAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(2));
			Assert.True(contender.IsLocked);
			Assert.True(contender.FencingToken > holderToken);

			await Task.Delay(800);
			Assert.True(contender.IsLocked);
			Assert.True((await cache.GetExpiryAsync(key)) > TimeSpan.Zero);

			await using var secondCompetitor = await cache.AcquireAsync(key, TimeSpan.FromSeconds(1));
			Assert.True(secondCompetitor.IsUnheld);
			Assert.Equal(0, secondCompetitor.FencingToken);
		}
		finally
		{
			await DeleteLockKeysAsync(cache, cacheNamespace, key);
		}
	}

	private static RedisService CreateCache(out string cacheNamespace)
	{
		cacheNamespace = $"Zongsoft.Tests.Lock.{Guid.NewGuid():N}";
		return new RedisService($"lock-{Guid.NewGuid():N}",
			$"server={Global.Server};password={Global.Password};timeout=5s;")
		{
			Namespace = cacheNamespace,
		};
	}

	private static async Task DeleteLockKeysAsync(RedisService cache, string cacheNamespace, params string[] keys)
	{
		var redisKeys = new RedisKey[keys.Length * 2];
		for(var index = 0; index < keys.Length; index++)
		{
			redisKeys[index * 2] = $"{cacheNamespace}:{keys[index]}";
			redisKeys[index * 2 + 1] = $"{cacheNamespace}:{keys[index]}:FENCE";
		}

		await cache.Database.KeyDeleteAsync(redisKeys);
	}

	private static void EnsureRedis()
	{
		Assert.SkipUnless(Global.IsTestingEnabled, TESTS_DISABLED);
		Assert.SkipUnless(Global.IsAvailable(), REDIS_UNAVAILABLE);
	}
}
