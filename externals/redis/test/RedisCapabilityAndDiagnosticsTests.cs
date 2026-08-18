using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using StackExchange.Redis;

using Xunit;

namespace Zongsoft.Externals.Redis.Tests;

public class RedisCapabilityAndDiagnosticsTests
{
	private const string REDIS_UNAVAILABLE = "Redis is unavailable at 127.0.0.1:6379.";
	private const string TESTS_DISABLED = "Redis integration tests require ZONGSOFT_REDIS_TESTS=1.";

	[Theory]
	[InlineData(null, RedisCapabilities.None)]
	[InlineData("6.0", RedisCapabilities.None)]
	[InlineData("6.2", RedisCapabilities.StreamAutoClaim)]
	[InlineData("8.1", RedisCapabilities.StreamAutoClaim)]
	[InlineData("8.2", RedisCapabilities.StreamAutoClaim | RedisCapabilities.StreamAcknowledgeAndDelete | RedisCapabilities.StreamGroupTrimming)]
	[InlineData("8.5", RedisCapabilities.StreamAutoClaim | RedisCapabilities.StreamAcknowledgeAndDelete | RedisCapabilities.StreamGroupTrimming)]
	[InlineData("8.6", RedisCapabilities.StreamAutoClaim | RedisCapabilities.StreamAcknowledgeAndDelete | RedisCapabilities.StreamGroupTrimming | RedisCapabilities.StreamIdempotentProducer)]
	[InlineData("9.0", RedisCapabilities.StreamAutoClaim | RedisCapabilities.StreamAcknowledgeAndDelete | RedisCapabilities.StreamGroupTrimming | RedisCapabilities.StreamIdempotentProducer)]
	public void CapabilityMatrix_UsesConservativeVersionBoundaries(string value, RedisCapabilities expected)
	{
		Assert.Equal(expected, RedisCapabilityMatrix.GetCapabilities(value == null ? null : Version.Parse(value)));
	}

	[Fact]
	public void Diagnostics_ExposeStableSourceAndInstrumentNames()
	{
		Assert.Equal("Zongsoft.Externals.Redis", RedisDiagnostics.Name);
		Assert.Equal(RedisDiagnostics.Name, RedisDiagnostics.ActivitySource.Name);
		Assert.Equal(RedisDiagnostics.Name, RedisDiagnostics.Meter.Name);

		Assert.Equal("redis.connections.active", RedisDiagnostics.ActiveConnections.Name);
		Assert.Equal("redis.connections.failures", RedisDiagnostics.ConnectionFailures.Name);
		Assert.Equal("redis.connections.restorations", RedisDiagnostics.ConnectionRestorations.Name);
		Assert.Equal("redis.connections.errors", RedisDiagnostics.ConnectionErrors.Name);
		Assert.Equal("redis.cache.notifications.pending", RedisDiagnostics.PendingNotifications.Name);
		Assert.Equal("redis.cache.notifications.dropped", RedisDiagnostics.DroppedNotifications.Name);
		Assert.Equal("redis.cache.notification.duration", RedisDiagnostics.NotificationDuration.Name);
		Assert.Equal("redis.queue.deadletters", RedisDiagnostics.DeadLetters.Name);
		Assert.Equal("redis.lock.renewal.failures", RedisDiagnostics.LockRenewalFailures.Name);
	}

	[Fact]
	public async Task ServiceInfo_ReportsConservativeCapabilitiesForAllPrimaryServers()
	{
		EnsureRedis();

		await using var cache = new RedisService($"capabilities-{Guid.NewGuid():N}",
			$"server={RedisTestUtility.Server};password={RedisTestUtility.Password};timeout=5s;");
		var info = await cache.GetInfoAsync();
		var primaries = info.Servers.Where(server => !server.IsSlave).ToArray();

		Assert.NotEmpty(primaries);
		var expected = primaries
			.Select(server => RedisCapabilityMatrix.GetCapabilities(server.Version))
			.Aggregate((left, right) => left & right);
		Assert.Equal(expected, info.Capabilities);
	}

	[Fact]
	public async Task ConnectionPool_EmitsActiveConnectionMeasurements()
	{
		EnsureRedis();

		var options = ConfigurationOptions.Parse($"{RedisTestUtility.Server},password={RedisTestUtility.Password},connectTimeout=2000");
		options.ClientName = $"diagnostics-{Guid.NewGuid():N}";
		var measurements = new ConcurrentQueue<long>();
		using var listener = new MeterListener();
		listener.InstrumentPublished = (instrument, current) =>
		{
			if(instrument.Meter.Name == RedisDiagnostics.Name && instrument.Name == "redis.connections.active")
				current.EnableMeasurementEvents(instrument);
		};
		listener.SetMeasurementEventCallback<long>((_, value, tags, _) =>
		{
			foreach(var tag in tags)
			{
				if(tag.Key == "redis.client.name" && string.Equals(tag.Value as string, options.ClientName, StringComparison.Ordinal))
				{
					measurements.Enqueue(value);
					break;
				}
			}
		});
		listener.Start();

		var lease = await RedisConnectionPool.AcquireAsync(options);
		await lease.DisposeAsync();

		Assert.Equal([1L, -1L], measurements.ToArray());
	}

	[Fact]
	public async Task ConnectionPool_WiresMultiplexerConnectionEventsToDiagnosticsHandlers()
	{
		EnsureRedis();

		var options = ConfigurationOptions.Parse($"{RedisTestUtility.Server},password={RedisTestUtility.Password},connectTimeout=2000");
		options.ClientName = $"event-wiring-{Guid.NewGuid():N}";
		await using var lease = await RedisConnectionPool.AcquireAsync(options);
		var handlers = lease.Connection.GetType()
			.GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
			.Where(field => typeof(Delegate).IsAssignableFrom(field.FieldType))
			.Select(field => field.GetValue(lease.Connection) as Delegate)
			.Where(value => value != null)
			.SelectMany(value => value.GetInvocationList())
			.Where(handler => handler.Method.DeclaringType?.Assembly == typeof(RedisConnectionPool).Assembly)
			.Where(handler => handler.Method.DeclaringType?.FullName?.Contains(nameof(RedisConnectionPool), StringComparison.Ordinal) == true)
			.ToArray();

		Assert.True(handlers.Length >= 3, "ConnectionFailed, ConnectionRestored, and ErrorMessage must be wired to Redis diagnostics counters.");
	}

	[Fact]
	public async Task RedisOperations_EmitConnectLockAndQueueActivities()
	{
		EnsureRedis();

		var activities = new ConcurrentQueue<(string Name, ActivityKind Kind)>();
		using var listener = new ActivityListener
		{
			ShouldListenTo = source => source.Name == RedisDiagnostics.Name,
			Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
			ActivityStarted = activity => activities.Enqueue((activity.OperationName, activity.Kind)),
		};
		ActivitySource.AddActivityListener(listener);

		var identity = Guid.NewGuid().ToString("N");
		var connectionString = $"server={RedisTestUtility.Server};password={RedisTestUtility.Password};timeout=5s;";
		await using var cache = new RedisService($"diagnostics-{identity}", connectionString) { Namespace = $"Zongsoft.Tests.Diagnostics.{identity}" };
		await using var queue = RedisTestUtility.CreateQueue($"diagnostics-{identity}");
		var queueKey = RedisTestUtility.GetQueueKey($"diagnostics-{identity}", "activity");

		try
		{
			Assert.False(await cache.ExistsAsync("connect"));
			await using var distributedLock = await cache.AcquireAsync("lock", TimeSpan.FromSeconds(2));
			await queue.ProduceAsync("activity", Encoding.UTF8.GetBytes("payload"));

			Assert.Contains(("redis.connect", ActivityKind.Client), activities);
			Assert.Contains(("redis.lock.acquire", ActivityKind.Client), activities);
			Assert.Contains(("redis.queue.produce", ActivityKind.Producer), activities);
		}
		finally
		{
			await cache.Database.KeyDeleteAsync([$"{cache.Namespace}:lock", $"{cache.Namespace}:lock:FENCE", queueKey]);
		}
	}

	private static void EnsureRedis()
	{
		Assert.SkipUnless(RedisTestUtility.IsTestingEnabled, TESTS_DISABLED);
		Assert.SkipUnless(RedisTestUtility.IsAvailable(), REDIS_UNAVAILABLE);
	}
}
