using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Zongsoft.Services;
using Zongsoft.Messaging;
using Zongsoft.Configuration;
using Zongsoft.Externals.Redis.Messaging;
using Zongsoft.Externals.Redis.Configuration;

using Xunit;

namespace Zongsoft.Externals.Redis.Tests.MessageStorages;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class RedisMessageStorageCollection
{
	public const string Name = nameof(RedisMessageStorageCollection);
}

[Collection(RedisMessageStorageCollection.Name)]
public sealed class RedisMessageStorageTests
{
	private const string IDENTIFIER_ENVIRONMENT_VARIABLE = "ZONGSOFT_MESSAGING_STORAGE_IDENTIFIER";

	[Fact]
	public void Constructor_ExposesRedisContractWithFrozenPartition()
	{
		var settings = CreateSettings("typed");
		using var storage = new RedisMessageStorage("test", settings, "Zongsoft.Messaging.Storage:typed:identifier-one");

		Assert.Equal("test", storage.Name);
		Assert.Same(settings, storage.ConnectionSettings);
		Assert.Equal("Zongsoft.Messaging.Storage:typed:identifier-one", storage.Partition);
		Assert.Null(typeof(IMessageStorage).GetProperty("Disposable"));
		Assert.Null(typeof(IMessageStorage).GetProperty("Settings"));
		Assert.Throws<ArgumentNullException>(() => new RedisMessageStorage("test", null, "partition"));
		Assert.Throws<ArgumentNullException>(() => new RedisMessageStorage("test", settings, null));
	}

	[Fact]
	public async Task Operations_PreCanceledTokenDoNotActivateStorage()
	{
		using var storage = CreateStorage("cancelled", "identifier-cancelled");
		using var source = new CancellationTokenSource();
		source.Cancel();

		var clear = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await storage.ClearAsync(source.Token));
		var set = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await storage.SetAsync(CreateMessage("cancelled", "topic", [1]), cancellation: source.Token));
		var remove = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await storage.RemoveAsync("cancelled", source.Token));
		var get = Assert.ThrowsAny<OperationCanceledException>(() => storage.GetAsync(source.Token));

		Assert.Equal(source.Token, clear.CancellationToken);
		Assert.Equal(source.Token, set.CancellationToken);
		Assert.Equal(source.Token, remove.CancellationToken);
		Assert.Equal(source.Token, get.CancellationToken);
	}

	[Fact]
	public async Task Dispose_BeforeActivationMakesStorageUnavailableAndIsIdempotent()
	{
		var storage = CreateStorage("disposed", "identifier-disposed");

		await storage.DisposeAsync();
		await storage.DisposeAsync();

		await Assert.ThrowsAsync<ObjectDisposedException>(async () => await storage.SetAsync(CreateMessage("disposed", "topic", [1])));
	}

	[Fact]
	public void Snapshot_RoundTripsCompleteIndependentMetadataWithoutAcknowledger()
	{
		var acknowledged = 0;
		var timestamp = new DateTime(2026, 9, 1, 2, 3, 4, DateTimeKind.Unspecified);
		var data = new byte[] { 1, 2, 3, 4 };
		var message = new Message("message-1", "Orders/Created", data, "blue,priority", () => acknowledged++)
		{
			Identity = "producer-7",
			Timestamp = timestamp,
		};

		var payload = RedisMessageStorage.MessageModel.Serialize(message);
		data[0] = 99;
		message.Data = [8];
		message.Tags = "changed";
		var snapshot = RedisMessageStorage.MessageModel.Deserialize(payload);
		var restored = snapshot.ToMessage();

		Assert.Equal("message-1", restored.Identifier);
		Assert.Equal("Orders/Created", restored.Topic);
		Assert.Equal("producer-7", restored.Identity);
		Assert.Equal("blue,priority", restored.Tags);
		Assert.Equal(DateTime.SpecifyKind(timestamp, DateTimeKind.Utc), restored.Timestamp);
		Assert.Equal(DateTimeKind.Utc, restored.Timestamp.Kind);
		Assert.Equal([1, 2, 3, 4], restored.Data);

		restored.Data[1] = 88;
		restored.Acknowledge();
		var reread = snapshot.ToMessage();
		Assert.Equal([1, 2, 3, 4], reread.Data);
		Assert.Equal(0, acknowledged);
		message.Acknowledge();
		Assert.Equal(1, acknowledged);
	}

	[Fact]
	public void Snapshot_PreservesNullAndEmptyPayloads()
	{
		var nullSnapshot = RedisMessageStorage.MessageModel.Deserialize(
			RedisMessageStorage.MessageModel.Serialize(CreateMessage("null", string.Empty, null)));
		var emptySnapshot = RedisMessageStorage.MessageModel.Deserialize(
			RedisMessageStorage.MessageModel.Serialize(CreateMessage("empty", string.Empty, [])));

		Assert.Null(nullSnapshot.ToMessage().Data);
		Assert.Empty(emptySnapshot.ToMessage().Data);
		Assert.Equal(string.Empty, nullSnapshot.Topic);
		Assert.Equal(string.Empty, emptySnapshot.Topic);
	}

	[Theory]
	[MemberData(nameof(InvalidSnapshots))]
	public void Snapshot_InvalidRecordThrowsInvalidDataException(byte[] payload)
	{
		var exception = Assert.Throws<InvalidDataException>(() => RedisMessageStorage.MessageModel.Deserialize(payload));
		Assert.False(string.IsNullOrWhiteSpace(exception.Message));
	}

	[Fact]
	public void Factory_UsesExactNamedSettingsAndCreatesIndependentInstances()
	{
		using var environment = new EnvironmentVariableScope("constructor-identifier");
		var configuration = new ConfigurationBuilder()
			.AddOptionFile(Path.Combine(AppContext.BaseDirectory, "Messaging", "RedisMessageStorage.option"))
			.Build();

		var services = new ServiceCollection().AddSingleton<IConfigurationRoot>(configuration);
		using var scope = new ServiceScope(new ServiceProviderFactory().CreateServiceProvider(services));
		using var application = new ApplicationScope(scope.Provider);
		var factory = RedisMessageStorageFactory.Instance;
		Environment.SetEnvironmentVariable(IDENTIFIER_ENVIRONMENT_VARIABLE, "  redis-test-identifier  ");
		RedisMessageStorage first = factory.Create("QueueServer");
		Environment.SetEnvironmentVariable(IDENTIFIER_ENVIRONMENT_VARIABLE, "changed-identifier");
		RedisMessageStorage second = factory.Create("QueueServer");

		Assert.NotSame(first, second);
		Assert.Equal("QueueServer", first.ConnectionSettings.Name);
		Assert.Equal("Zongsoft.Messaging.Storage:QueueServer:redis-test-identifier", first.Partition);
		Assert.Equal(first.Partition, second.Partition);
		Assert.Throws<ConfigurationException>(() => factory.Create("missing"));
		Assert.Throws<ArgumentNullException>(() => factory.Create(null));

		first.Dispose();
		second.Dispose();
	}

	[Fact]
	public void RedisServiceProviderNoLongerProvidesMessageStorage()
	{
		Assert.DoesNotContain(typeof(RedisServiceProvider).GetInterfaces(), contract =>
			contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(Zongsoft.Services.IServiceProvider<>) && contract.GenericTypeArguments[0] == typeof(IMessageStorage));

		var contracts = Assert.Single(typeof(RedisServiceProvider).GetCustomAttributes<ServiceAttribute>()).Contracts;
		Assert.DoesNotContain(typeof(Zongsoft.Services.IServiceProvider<IMessageStorage>), contracts);
	}

	public static IEnumerable<object[]> InvalidSnapshots =>
	[
		[Array.Empty<byte>()],
		[Encoding.UTF8.GetBytes("not-json")],
		[Encoding.UTF8.GetBytes("null")],
		[Encoding.UTF8.GetBytes("{\"version\":2,\"identifier\":\"message\",\"topic\":\"topic\"}")],
		[Encoding.UTF8.GetBytes("{\"version\":1,\"identifier\":\" \",\"topic\":\"topic\"}")],
	];

	private static RedisMessageStorage CreateStorage(string name, string identifier) =>
		new("test", CreateSettings(name), $"Zongsoft.Messaging.Storage:{name}:{identifier}");

	private static RedisConnectionSettings CreateSettings(string name) =>
		RedisConnectionSettingsDriver.Instance.GetSettings(name, GetConnectionString());

	private static string GetConnectionString() => $"server={Global.Server};password={Global.Password};timeout=5s";
	private static Message CreateMessage(string identifier, string topic, byte[] data) => new(identifier, topic, data) { Timestamp = DateTime.UtcNow };

	private sealed class ServiceScope(IServiceProvider provider) : IDisposable
	{
		public IServiceProvider Provider { get; } = provider;
		public void Dispose() => (this.Provider as IDisposable)?.Dispose();
	}

	private sealed class ApplicationScope : IDisposable
	{
		private static readonly FieldInfo CurrentField = typeof(ApplicationContext).GetField("_current", BindingFlags.Static | BindingFlags.NonPublic);
		private readonly IApplicationContext _previous;
		private readonly TestApplicationContext _current;

		public ApplicationScope(IServiceProvider services)
		{
			_previous = ApplicationContext.Current;
			_current = new TestApplicationContext(services);
		}

		public void Dispose()
		{
			_current.Dispose();
			CurrentField.SetValue(null, _previous);
		}
	}

	private sealed class TestApplicationContext(IServiceProvider services) : ApplicationContext(services) { }

	private sealed class EnvironmentVariableScope : IDisposable
	{
		private readonly string _original;

		public EnvironmentVariableScope(string value)
		{
			_original = Environment.GetEnvironmentVariable(IDENTIFIER_ENVIRONMENT_VARIABLE);
			Environment.SetEnvironmentVariable(IDENTIFIER_ENVIRONMENT_VARIABLE, value);
		}

		public void Dispose() => Environment.SetEnvironmentVariable(IDENTIFIER_ENVIRONMENT_VARIABLE, _original);
	}

}
