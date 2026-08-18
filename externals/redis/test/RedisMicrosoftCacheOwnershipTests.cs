using System;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;

using StackExchange.Redis;

using Xunit;

using Zongsoft.Externals.Redis.Configuration;
using Zongsoft.Externals.Redis.Messaging;

namespace Zongsoft.Externals.Redis.Tests;

public class RedisMicrosoftCacheOwnershipTests
{
	private const string REDIS_UNAVAILABLE = "Redis is unavailable at 127.0.0.1:6379.";
	private const string TESTS_DISABLED = "Redis integration tests require ZONGSOFT_REDIS_TESTS=1.";

	[Fact]
	public async Task MicrosoftCacheProxy_ReleasesOnlyItsLeaseAndKeepsServiceAndQueueAlive()
	{
		EnsureRedis();

		var identity = Guid.NewGuid().ToString("N");
		var connectionString = $"server={RedisTestUtility.Server};password={RedisTestUtility.Password};timeout=5s;client={identity};";
		var service = new RedisService($"microsoft-{identity}", connectionString) { Namespace = $"Zongsoft.Tests.Microsoft.{identity}" };
		var settings = RedisConnectionSettingsDriver.Instance.GetSettings($"queue-{identity}", connectionString);
		var queue = new RedisQueue($"queue-{identity}", settings);
		var options = service.Options.Clone();
		var services = new ServiceCollection();
		services.AddStackExchangeRedisCache(configuration =>
		{
			configuration.ConfigurationOptions = options;
			configuration.InstanceName = $"Microsoft:{identity}:";
			configuration.ConnectionMultiplexerFactory = async () =>
			{
				var lease = await RedisConnectionPool.AcquireAsync(options);
				return lease.CreateProxy();
			};
		});
		var provider = services.BuildServiceProvider();
		var microsoft = provider.GetRequiredService<IDistributedCache>();
		var microsoftKey = "entry";
		var queueTopic = "ownership";
		var queueKey = RedisTestUtility.GetQueueKey(queue.Name, queueTopic);
		var serviceKey = $"{service.Namespace}:service";

		using var administration = ConnectionMultiplexer.Connect($"{RedisTestUtility.Server},password={RedisTestUtility.Password}");
		var database = administration.GetDatabase();

		try
		{
			Assert.True(await service.SetValueAsync("service", "alive"));
			await microsoft.SetStringAsync(microsoftKey, "microsoft");
			var connection = service.Database.Multiplexer;

			Assert.Same(connection, queue.Database.Multiplexer);
			await provider.DisposeAsync();

			Assert.True(connection.IsConnected);
			Assert.Equal("alive", await service.GetValueAsync<string>("service"));
			Assert.False(string.IsNullOrEmpty(await queue.ProduceAsync(queueTopic, Encoding.UTF8.GetBytes("queue-alive"))));

			await service.DisposeAsync();
			Assert.True(connection.IsConnected);
			Assert.True(await queue.Database.PingAsync() >= TimeSpan.Zero);

			await queue.DisposeAsync();
			Assert.False(connection.IsConnected);
		}
		finally
		{
			await provider.DisposeAsync();
			await service.DisposeAsync();
			await queue.DisposeAsync();
			await database.KeyDeleteAsync([serviceKey, $"Microsoft:{identity}:{microsoftKey}", queueKey]);
		}
	}

	private static void EnsureRedis()
	{
		Assert.SkipUnless(RedisTestUtility.IsTestingEnabled, TESTS_DISABLED);
		Assert.SkipUnless(RedisTestUtility.IsAvailable(), REDIS_UNAVAILABLE);
	}
}
