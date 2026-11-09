using System;
using System.IO;
using System.Threading.Tasks;

using Zongsoft.Configuration;
using Zongsoft.Data;
using Zongsoft.Data.Metadata;
using Zongsoft.Data.SQLite;
using Zongsoft.Data.SQLite.Configuration;

namespace Zongsoft.Messaging.Storages.Data.Tests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class DatabaseMessageStorageCollection : ICollectionFixture<SQLiteDatabaseFixture>
{
	public const string Name = nameof(DatabaseMessageStorageCollection);
}

public sealed class SQLiteDatabaseFixture : IAsyncLifetime
{
	private const string IDENTIFIER_ENVIRONMENT_VARIABLE = "ZONGSOFT_MESSAGING_STORAGE_IDENTIFIER";

	private readonly bool _driverAdded;
	private readonly Mapping.Loader _loader;
	private readonly string _databaseFile;
	private readonly string _connectionString;
	private readonly string _originalIdentifier;

	public SQLiteDatabaseFixture()
	{
		_originalIdentifier = Environment.GetEnvironmentVariable(IDENTIFIER_ENVIRONMENT_VARIABLE);
		Environment.SetEnvironmentVariable(IDENTIFIER_ENVIRONMENT_VARIABLE, "database-test-identifier");
		_databaseFile = Path.Combine(Path.GetTempPath(), $"zongsoft-messaging-{Guid.NewGuid():N}.db");
		_connectionString = $"DataSource={_databaseFile};Pooling=False;PRAGMA:journal_mode=WAL;PRAGMA:synchronous=NORMAL;";

		if(!DataEnvironment.Drivers.Contains(SQLiteDriver.NAME))
		{
			DataEnvironment.Drivers.Add(SQLiteDriver.Instance);
			_driverAdded = true;
		}

		Mapping.Loaders.Add(_loader = new Zongsoft.Data.Metadata.Profiles.MetadataFileLoader(AppContext.BaseDirectory));
		this.ConnectionSettings = SQLiteConnectionSettingsDriver.Instance.GetSettings($"fixture-{Guid.NewGuid():N}", _connectionString);
		this.Accessor = DataAccessProvider.Instance.GetAccessor($"Messaging.Storages.Data.Tests.{Guid.NewGuid():N}", new DataAccessOptions([this.ConnectionSettings]));
	}

	public DataAccess Accessor { get; }
	public SQLiteConnectionSettings ConnectionSettings { get; }

	public DataMessageStorage CreateStorage(string name = null, string identifier = null)
	{
		name ??= $"storage-{Guid.NewGuid():N}";
		identifier ??= $"identifier-{Guid.NewGuid():N}";
		var settings = SQLiteConnectionSettingsDriver.Instance.GetSettings(name, _connectionString);
		return new DataMessageStorage("test", this.Accessor, settings, $"Zongsoft.Messaging.Storage:{name}:{identifier}");
	}

	public async ValueTask ExecuteSchemaAsync()
	{
		var script = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "database", "sqlite", "schema.sql"));

		await using var connection = SQLiteDriver.Instance.CreateConnection(_connectionString);
		await using var command = connection.CreateCommand();
		await connection.OpenAsync();
		command.CommandText = script;
		await command.ExecuteNonQueryAsync();
	}

	public ValueTask InitializeAsync() => this.ExecuteSchemaAsync();

	public async ValueTask DisposeAsync()
	{
		try
		{
			await this.Accessor.DisposeAsync();
			Mapping.Loaders.Remove(_loader);

			if(_driverAdded)
				DataEnvironment.Drivers.Remove(SQLiteDriver.Instance);

			Delete(_databaseFile);
			Delete($"{_databaseFile}-wal");
			Delete($"{_databaseFile}-shm");
		}
		finally
		{
			Environment.SetEnvironmentVariable(IDENTIFIER_ENVIRONMENT_VARIABLE, _originalIdentifier);
		}

		static void Delete(string path)
		{
			if(File.Exists(path))
				File.Delete(path);
		}
	}
}
