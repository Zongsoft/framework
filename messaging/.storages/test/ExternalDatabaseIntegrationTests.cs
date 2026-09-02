using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Data;
using Zongsoft.Data.Common;
using Zongsoft.Data.Metadata;
using Zongsoft.Configuration;

using Xunit;

namespace Zongsoft.Messaging.Storages.Data.Tests;

[Collection(DatabaseMessageStorageCollection.Name)]
public sealed class ExternalDatabaseIntegrationTests
{
	[Theory]
	[InlineData("MySql")]
	[InlineData("PostgreSql")]
	[InlineData("MsSql")]
	public async Task Storage_ExternalDriver_RoundTripsSnapshotAndActualClearCount(string driverName)
	{
		Assert.SkipUnless(Global.ExternalTestsEnabled, Global.ExternalTestsDisabled);
		var target = GetTarget(driverName);
		var added = !DataEnvironment.Drivers.Contains(target.Driver.Name);

		if(added)
			DataEnvironment.Drivers.Add(target.Driver);

		IDataAccess accessor = null;

		try
		{
			var settings = target.Settings.GetSettings($"integration-{Guid.NewGuid():N}", target.ConnectionString);
			accessor = DataAccessProvider.Instance.GetAccessor($"Messaging.Storages.Data.Integration.{Guid.NewGuid():N}", new DataAccessOptions([settings]));
			var script = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "database", target.Schema, "schema.sql"));
			var schema = Mapping.Commands.Script(target.Driver.Name, true, script);
			await accessor.ExecuteAsync(schema.QualifiedName);
			await accessor.ExecuteAsync(schema.QualifiedName);

			var storage = new DataMessageStorage("test", accessor, settings, $"Zongsoft.Messaging.Storage:{settings.Name}:integration-identifier");
			var timestamp = new DateTime(2026, 9, 1, 4, 5, 6, DateTimeKind.Utc);
			await storage.SetAsync(new Message("integration", "Orders", [1, 2, 3])
			{
				Identity = "producer",
				Tags = "external",
				Timestamp = timestamp,
			});

			var messages = new List<Message>();
			await foreach(var message in storage.GetAsync("Orders"))
				messages.Add(message);

			var restored = Assert.Single(messages);
			Assert.Equal("integration", restored.Identifier);
			Assert.Equal("producer", restored.Identity);
			Assert.Equal("external", restored.Tags);
			Assert.Equal(timestamp, restored.Timestamp);
			Assert.Equal([1, 2, 3], restored.Data);
			Assert.Equal(1, await storage.ClearAsync());
			Assert.Equal(0, await storage.ClearAsync());
		}
		finally
		{
			accessor?.Dispose();

			if(added)
				DataEnvironment.Drivers.Remove(target.Driver);
		}
	}

	private static Target GetTarget(string driver) => driver switch
	{
		"MySql" => new(
			Zongsoft.Data.MySql.MySqlDriver.Instance,
			Zongsoft.Data.MySql.Configuration.MySqlConnectionSettingsDriver.Instance,
			"mysql",
			Global.GetConnectionString("MYSQL", "Server=127.0.0.1;Database=zongsoft;UserName=program;Password=xxxxxx;Charset=utf8mb4;AllowPublicKeyRetrieval=true;")),
		"PostgreSql" => new(
			Zongsoft.Data.PostgreSql.PostgreSqlDriver.Instance,
			Zongsoft.Data.PostgreSql.Configuration.PostgreSqlConnectionSettingsDriver.Instance,
			"postgresql",
			Global.GetConnectionString("POSTGRESQL", "Server=127.0.0.1;Database=zongsoft;UserName=program;Password=xxxxxx;")),
		"MsSql" => new(
			Zongsoft.Data.MsSql.MsSqlDriver.Instance,
			Zongsoft.Data.MsSql.Configuration.MsSqlConnectionSettingsDriver.Instance,
			"mssql",
			Global.GetConnectionString("MSSQL", "Server=127.0.0.1;Database=zongsoft;UserName=program;Password=xxxxxx;TrustServerCertificate=true;")),
		_ => throw new ArgumentOutOfRangeException(nameof(driver)),
	};

	private sealed record Target(IDataDriver Driver, IConnectionSettingsDriver Settings, string Schema, string ConnectionString);
}
