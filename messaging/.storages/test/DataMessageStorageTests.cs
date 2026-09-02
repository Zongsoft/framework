using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Data;
using Zongsoft.Data.Metadata;

using Xunit;

namespace Zongsoft.Messaging.Storages.Data.Tests;

[Collection(DatabaseMessageStorageCollection.Name)]
public sealed class DataMessageStorageTests(SQLiteDatabaseFixture fixture)
{
	private readonly SQLiteDatabaseFixture _fixture = fixture;

	[Fact]
	public void Constructor_ValidDependencies_ExposesImmutableContract()
	{
		var storage = _fixture.CreateStorage("constructor", "constructor-node");

		Assert.Equal("constructor", storage.ConnectionSettings.Name);
		Assert.Equal("Zongsoft.Messaging.Storage:constructor:constructor-node", storage.Partition);
		Assert.IsNotAssignableFrom<IDisposable>(storage);
		Assert.Null(typeof(IMessageStorage).GetProperty("Settings"));
		Assert.Null(typeof(IMessageStorage).GetProperty("Disposable"));
		Assert.Throws<ArgumentNullException>(() => new DataMessageStorage("test", null, _fixture.ConnectionSettings, "partition"));
		Assert.Throws<ArgumentNullException>(() => new DataMessageStorage("test", _fixture.Accessor, null, "partition"));
		Assert.Throws<ArgumentNullException>(() => new DataMessageStorage("test", _fixture.Accessor, _fixture.ConnectionSettings, null));
	}

	[Fact]
	public async Task SetAsync_CompleteMutableMessage_PersistsIndependentSnapshotWithoutAcknowledger()
	{
		var storage = _fixture.CreateStorage(identifier: NewIdentifier());
		var acknowledged = 0;
		var timestamp = new DateTime(2026, 9, 1, 2, 3, 4, DateTimeKind.Unspecified);
		var data = new byte[] { 1, 2, 3, 4 };
		var message = new Message("snapshot", "Orders/Created", data, "blue,priority", () => acknowledged++)
		{
			Identity = "producer-7",
			Timestamp = timestamp,
		};

		await storage.SetAsync(message, TimeSpan.FromMinutes(1));
		data[0] = 99;
		message.Data = [8];
		message.Tags = "changed";

		var restored = Assert.Single(await ReadAsync(storage));

		Assert.Equal("snapshot", restored.Identifier);
		Assert.Equal("Orders/Created", restored.Topic);
		Assert.Equal("producer-7", restored.Identity);
		Assert.Equal("blue,priority", restored.Tags);
		Assert.Equal(DateTime.SpecifyKind(timestamp, DateTimeKind.Utc), restored.Timestamp);
		Assert.Equal(DateTimeKind.Utc, restored.Timestamp.Kind);
		Assert.Equal([1, 2, 3, 4], restored.Data);

		restored.Data[1] = 88;
		restored.Acknowledge();
		var reread = Assert.Single(await ReadAsync(storage));
		Assert.Equal([1, 2, 3, 4], reread.Data);
		Assert.Equal(0, acknowledged);

		message.Acknowledge();
		Assert.Equal(1, acknowledged);
	}

	[Fact]
	public async Task SetAsync_NullAndEmptyPayloads_PreserveDistinction()
	{
		var storage = _fixture.CreateStorage(identifier: NewIdentifier());
		var empty = CreateMessage("empty", string.Empty, []);
		empty.Identity = string.Empty;
		empty.Tags = string.Empty;

		await storage.SetAsync(CreateMessage("null", string.Empty, null));
		await storage.SetAsync(empty);

		var messages = (await ReadAsync(storage, string.Empty)).ToDictionary(message => message.Identifier);

		Assert.Equal(2, messages.Count);
		Assert.Null(messages["null"].Data);
		Assert.Null(messages["null"].Identity);
		Assert.Null(messages["null"].Tags);
		Assert.Empty(messages["empty"].Data);
		Assert.Equal(string.Empty, messages["empty"].Identity);
		Assert.Equal(string.Empty, messages["empty"].Tags);
		Assert.All(messages.Values, message => Assert.Equal(string.Empty, message.Topic));
	}

	[Fact]
	public async Task SetAsync_ExistingIdentifier_OverwritesEveryPersistedFieldAndClearsOldExpiry()
	{
		var storage = _fixture.CreateStorage(identifier: NewIdentifier());
		await storage.SetAsync(new Message("same", "old", [1])
		{
			Identity = "old-identity",
			Tags = "old-tags",
			Timestamp = new DateTime(2025, 1, 2, 3, 4, 5, DateTimeKind.Utc),
		}, TimeSpan.FromMilliseconds(150));

		var replacementTimestamp = new DateTime(2026, 6, 7, 8, 9, 10, DateTimeKind.Utc);
		await storage.SetAsync(new Message("same", "new", [9, 8])
		{
			Identity = "new-identity",
			Tags = "new-tags",
			Timestamp = replacementTimestamp,
		}, TimeSpan.Zero);

		await Task.Delay(300);
		var restored = Assert.Single(await ReadAsync(storage));

		Assert.Equal("new", restored.Topic);
		Assert.Equal("new-identity", restored.Identity);
		Assert.Equal("new-tags", restored.Tags);
		Assert.Equal(replacementTimestamp, restored.Timestamp);
		Assert.Equal([9, 8], restored.Data);
	}

	[Fact]
	public async Task GetAsync_Topic_UsesOrdinalExactMatchingIncludingEmptyTopic()
	{
		var storage = _fixture.CreateStorage(identifier: NewIdentifier());
		await storage.SetAsync(CreateMessage("lower", "orders", [1]));
		await storage.SetAsync(CreateMessage("upper", "Orders", [2]));
		await storage.SetAsync(CreateMessage("child", "orders/created", [3]));
		await storage.SetAsync(CreateMessage("empty", string.Empty, [4]));

		var lower = Assert.Single(await ReadAsync(storage, "orders"));
		var empty = Assert.Single(await ReadAsync(storage, string.Empty));

		Assert.Equal("lower", lower.Identifier);
		Assert.Equal("orders", lower.Topic);
		Assert.Equal("empty", empty.Identifier);
		Assert.Equal(string.Empty, empty.Topic);
		Assert.Equal(4, (await ReadAsync(storage)).Count);
	}

	[Fact]
	public async Task RemoveAsync_ExistingAndMissing_ReturnsActualResult()
	{
		var storage = _fixture.CreateStorage(identifier: NewIdentifier());
		await storage.SetAsync(CreateMessage("remove", "topic", [1]));

		Assert.True(await storage.RemoveAsync("remove"));
		Assert.False(await storage.RemoveAsync("remove"));
		Assert.Empty(await ReadAsync(storage));
	}

	[Fact]
	public async Task ClearAsync_TopicAndAll_ReturnActualCounts()
	{
		var storage = _fixture.CreateStorage(identifier: NewIdentifier());
		await storage.SetAsync(CreateMessage("lower-1", "orders", [1]));
		await storage.SetAsync(CreateMessage("lower-2", "orders", [2]));
		await storage.SetAsync(CreateMessage("upper", "Orders", [3]));
		await storage.SetAsync(CreateMessage("empty", string.Empty, [4]));

		Assert.Equal(2, await storage.ClearAsync("orders"));
		Assert.Equal(0, await storage.ClearAsync("orders"));
		Assert.Equal(2, await storage.ClearAsync());
		Assert.Equal(0, await storage.ClearAsync());
	}

	[Fact]
	public async Task GetAsync_ExpiredEntry_IsFilteredWhilePermanentEntriesRemain()
	{
		var storage = _fixture.CreateStorage(identifier: NewIdentifier());
		await storage.SetAsync(CreateMessage("expiring", "ttl", [1]), TimeSpan.FromMilliseconds(120));
		await storage.SetAsync(CreateMessage("zero", "ttl", [2]), TimeSpan.Zero);
		await storage.SetAsync(CreateMessage("negative", "ttl", [3]), TimeSpan.FromSeconds(-1));

		await Task.Delay(350);
		var identifiers = (await ReadAsync(storage)).Select(message => message.Identifier).OrderBy(value => value).ToArray();

		Assert.Equal(["negative", "zero"], identifiers);
		Assert.Equal(3, await storage.ClearAsync());
	}

	[Fact]
	public async Task Partitions_ConnectionNameAndIdentifierIsolateData()
	{
		var name = $"connection-{Guid.NewGuid():N}";
		var sharedIdentifier = $"identifier-{Guid.NewGuid():N}";
		var writer = _fixture.CreateStorage(name, sharedIdentifier);
		var reader = _fixture.CreateStorage(name, sharedIdentifier);
		var isolated = _fixture.CreateStorage(name, $"isolated-{Guid.NewGuid():N}");

		await writer.SetAsync(CreateMessage("shared", "topic", [1]));
		await isolated.SetAsync(CreateMessage("shared", "topic", [2]));

		Assert.Equal([1], Assert.Single(await ReadAsync(reader)).Data);
		Assert.Equal([2], Assert.Single(await ReadAsync(isolated)).Data);
		Assert.Equal(1, await reader.ClearAsync());
		Assert.Empty(await ReadAsync(writer));
		Assert.Single(await ReadAsync(isolated));
	}

	[Fact]
	public async Task Partition_IsFrozenAtConstruction()
	{
		var original = NewIdentifier();
		var changed = NewIdentifier();
		var storage = _fixture.CreateStorage("mutable", original);

		await storage.SetAsync(CreateMessage("frozen", "topic", [7]));

		Assert.Equal("frozen", Assert.Single(await ReadAsync(_fixture.CreateStorage("mutable", original))).Identifier);
		Assert.Empty(await ReadAsync(_fixture.CreateStorage("mutable", changed)));
		Assert.Equal(1, await _fixture.CreateStorage("mutable", original).ClearAsync());
	}

	[Fact]
	public async Task SetAsync_ConcurrentSameIdentifier_LeavesOneCompleteSnapshot()
	{
		var storage = _fixture.CreateStorage(identifier: NewIdentifier());
		var first = new Message("concurrent", "first", [1, 1]) { Identity = "first-id", Tags = "first-tags" };
		var second = new Message("concurrent", "second", [2, 2]) { Identity = "second-id", Tags = "second-tags" };
		var writes = Enumerable.Range(0, 8)
			.Select(index => storage.SetAsync(index % 2 == 0 ? first : second).AsTask())
			.ToArray();

		await Task.WhenAll(writes);
		var result = Assert.Single(await ReadAsync(storage));
		var isFirst = result.Topic == "first";

		Assert.True(isFirst || result.Topic == "second");
		Assert.Equal(isFirst ? "first-id" : "second-id", result.Identity);
		Assert.Equal(isFirst ? "first-tags" : "second-tags", result.Tags);
		Assert.Equal(isFirst ? new byte[] { 1, 1 } : [2, 2], result.Data);
	}

	[Fact]
	public async Task Operations_PreCanceledTokens_PropagateWithoutPoisoningStorage()
	{
		var storage = _fixture.CreateStorage(identifier: NewIdentifier());
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

		await storage.SetAsync(CreateMessage("valid", "topic", [9]));
		Assert.Equal("valid", Assert.Single(await ReadAsync(storage)).Identifier);
	}

	[Fact]
	public async Task Storage_DoesNotOwnSharedIDataAccess()
	{
		var storage = _fixture.CreateStorage(identifier: NewIdentifier());
		await storage.SetAsync(CreateMessage("owned", "topic", [1]));

		Assert.False(_fixture.Accessor.IsDisposed);
		Assert.Same(_fixture.Accessor, storage.Accessor);
		Assert.Equal("owned", Assert.Single(await ReadAsync(storage)).Identifier);
		Assert.False(_fixture.Accessor.IsDisposed);
	}

	[Fact]
	public async Task Schema_Reexecution_IsIdempotent()
	{
		_fixture.ExecuteSchema();
		_fixture.ExecuteSchema();

		Assert.False(_fixture.Accessor.IsDisposed);
		Assert.Equal(0, await _fixture.CreateStorage(identifier: NewIdentifier()).ClearAsync());
	}

	[Fact]
	public void Mappings_ResolveExpectedQNamesAndDateTimeParameters()
	{
		var commands = new[] { "Set", "Get", "GetByTopic", "Remove", "Clear", "ClearByTopic" };
		var drivers = new[] { "SQLite", "MySql", "PostgreSql", "MsSql" };

		foreach(var commandName in commands)
		{
			var command = Mapping.Commands[$"Messaging.Storages.{commandName}"];
			Assert.Equal("Messaging.Storages", command.Namespace);

			foreach(var driver in drivers)
				Assert.False(string.IsNullOrWhiteSpace(command.Scriptor.GetScript(driver)));
		}

		var set = Mapping.Commands["Messaging.Storages.Set"];
		Assert.Equal(System.Data.DbType.DateTime, set.Parameters["Timestamp"].Type.DbType);
		Assert.Equal(System.Data.DbType.DateTime, set.Parameters["Expiration"].Type.DbType);

		foreach(var parameter in new[] { "IdentityIsNull", "TagsIsNull", "ExpirationIsNull", "DataIsNull" })
			Assert.Equal(System.Data.DbType.Boolean, set.Parameters[parameter].Type.DbType);
	}

	private static string NewIdentifier() => $"test-{Guid.NewGuid():N}";

	private static Message CreateMessage(string identifier, string topic, byte[] data) => new(identifier, topic, data)
	{
		Timestamp = DateTime.UtcNow,
	};

	private static async Task<List<Message>> ReadAsync(DataMessageStorage storage, string topic = null)
	{
		var result = new List<Message>();
		var messages = topic == null ? storage.GetAsync() : storage.GetAsync(topic);

		await foreach(var message in messages)
			result.Add(message);

		return result;
	}
}
