using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using StackExchange.Redis.Profiling;

using Xunit;

namespace Zongsoft.Externals.Redis.Tests;

public class RedisRoundTripTests
{
	private const string REDIS_UNAVAILABLE = "Redis is unavailable at 127.0.0.1:6379.";
	private const string TESTS_DISABLED = "Redis integration tests require ZONGSOFT_REDIS_TESTS=1.";

	[Theory]
	[InlineData(true, "PTTL", "TYPE", "GET")]
	[InlineData(false, "PTTL", "TYPE")]
	public async Task GetEntryAsync_DoesNotIssueRedundantExistsCommand(bool exists, params string[] expected)
	{
		EnsureRedis();

		await using var cache = new RedisService($"roundtrip-{Guid.NewGuid():N}",
			$"server={RedisTestUtility.Server};password={RedisTestUtility.Password};timeout=5s;")
		{
			Namespace = $"Zongsoft.Tests.RoundTrip.{Guid.NewGuid():N}",
		};
		var key = exists ? "existing" : "missing";
		if(exists)
			Assert.True(await cache.SetValueAsync(key, "value"));
		else
			Assert.False(await cache.ExistsAsync(key));

		var ambient = new AsyncLocal<ProfilingSession>();
		cache.Database.Multiplexer.RegisterProfiler(() => ambient.Value);
		var session = new ProfilingSession();
		ambient.Value = session;

		try
		{
			var result = await cache.GetEntryAsync(key);
			Assert.Equal(exists, result.entryType != RedisEntryType.None);
		}
		finally
		{
			ambient.Value = null;
		}

		var commands = session.FinishProfiling().Select(command => command.Command.ToString()).OrderBy(command => command).ToArray();
		Assert.DoesNotContain("EXISTS", commands);
		Assert.Equal(expected.OrderBy(command => command), commands);

		await cache.ClearAsync();
	}

	private static void EnsureRedis()
	{
		Assert.SkipUnless(RedisTestUtility.IsTestingEnabled, TESTS_DISABLED);
		Assert.SkipUnless(RedisTestUtility.IsAvailable(), REDIS_UNAVAILABLE);
	}
}
