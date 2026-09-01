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
	[Fact]
	public void ConstructorsAndSettings_ExposeRedisContractAndRemainMutableBeforeActivation()
	{
		var settings = CreateSettings("typed", "typed-namespace");
		using var storage = new RedisMessageStorage(settings);

		Assert.Equal("Redis", storage.Name);
		Assert.True(storage.Disposable);
		Assert.Same(settings, storage.Settings);
		Assert.Same(settings, ((IMessageStorage)storage).Settings);

		var replacement = CreateSettings("replacement", "replacement-namespace");
		storage.Settings = replacement;
		Assert.Same(replacement, storage.Settings);

		using var convenient = new RedisMessageStorage("convenient", GetConnectionString());
		Assert.Equal("convenient", convenient.Settings.Name);
		Assert.Throws<ArgumentNullException>(() => new RedisMessageStorage(null));
	}

	[Fact]
	public async Task Operations_PreCanceledTokenDoNotActivateStorage()
	{
		using var storage = new RedisMessageStorage(CreateSettings("cancelled", "cancelled-namespace"));
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

		var replacement = CreateSettings("replacement", "replacement-namespace");
		storage.Settings = replacement;
		Assert.Same(replacement, storage.Settings);
	}

	[Fact]
	public async Task Dispose_BeforeActivationMakesStorageUnavailableAndIsIdempotent()
	{
		var storage = new RedisMessageStorage(CreateSettings("disposed", "disposed-namespace"));

		await storage.DisposeAsync();
		await storage.DisposeAsync();

		Assert.Throws<ObjectDisposedException>(() => storage.Settings = CreateSettings("other", "other-namespace"));
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
	public void ServiceProvider_LocalAtRedisRegistrationUsesExactNamedSettingsAndIndependentInstances()
	{
		var configuration = new ConfigurationBuilder()
			.AddOptionFile(Path.Combine(AppContext.BaseDirectory, "Messaging", "RedisMessageStorage.option"))
			.Build();

		var services = new ServiceCollection();
		services.AddSingleton<IConfigurationRoot>(configuration);
		services.Register(typeof(RedisServiceProvider).Assembly, configuration);

		using var scope = new ServiceScope(new ServiceProviderFactory().CreateServiceProvider(services));
		using var application = new ApplicationScope(scope.Provider);
		var provider = Assert.IsAssignableFrom<IServiceProvider<IMessageStorage>>(scope.Provider.Resolve("redis"));
		var first = Assert.IsType<RedisMessageStorage>(provider.GetService("local"));
		var second = Assert.IsType<RedisMessageStorage>(provider.GetService("local"));
		var nullable = Assert.IsType<RedisMessageStorage>(provider.GetService(null));
		var empty = Assert.IsType<RedisMessageStorage>(provider.GetService(string.Empty));

		Assert.NotSame(first, second);
		Assert.Equal("local", first.Settings.Name);
		Assert.Equal("local", nullable.Settings.Name);
		Assert.Equal("local", empty.Settings.Name);
		Assert.Null(provider.GetService("missing"));
		Assert.Null(provider.GetService(" local "));

		first.Dispose();
		second.Dispose();
		nullable.Dispose();
		empty.Dispose();
	}

	[Fact]
	public void ServiceLocator_LocalAtRedisResolvesRegisteredStorageProvider()
	{
		var configuration = new ConfigurationBuilder()
			.AddOptionFile(Path.Combine(AppContext.BaseDirectory, "Messaging", "RedisMessageStorage.option"))
			.Build();

		var services = new ServiceCollection();
		services.AddSingleton<IConfigurationRoot>(configuration);
		services.Register(typeof(RedisServiceProvider).Assembly, configuration);

		using var scope = new ServiceScope(new ServiceProviderFactory().CreateServiceProvider(services));
		using var application = new ApplicationScope(scope.Provider);
		var provider = Assert.IsType<RedisServiceProvider>(scope.Provider.Resolve("redis"));
		var contracts = Assert.Single(typeof(RedisServiceProvider).GetCustomAttributes<ServiceAttribute>()).Contracts;

		Assert.Same(provider, scope.Provider.Resolve("Redis"));
		foreach(var contract in contracts)
			Assert.Same(provider, scope.Provider.GetRequiredService(contract));

		using var storage = Assert.IsType<RedisMessageStorage>(scope.Provider.Locate<IMessageStorage>("local@redis"));

		Assert.Equal("local", storage.Settings.Name);
		Assert.Equal("locator-local", storage.Settings.Namespace);
		Assert.Null(scope.Provider.Locate<IMessageStorage>("missing@redis"));
	}

	public static IEnumerable<object[]> InvalidSnapshots =>
	[
		[Array.Empty<byte>()],
		[Encoding.UTF8.GetBytes("not-json")],
		[Encoding.UTF8.GetBytes("null")],
		[Encoding.UTF8.GetBytes("{\"version\":2,\"identifier\":\"message\",\"topic\":\"topic\"}")],
		[Encoding.UTF8.GetBytes("{\"version\":1,\"identifier\":\" \",\"topic\":\"topic\"}")],
	];

	private static RedisConnectionSettings CreateSettings(string name, string @namespace) =>
		RedisConnectionSettingsDriver.Instance.GetSettings(name, $"{GetConnectionString()};namespace={@namespace}");

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

}
