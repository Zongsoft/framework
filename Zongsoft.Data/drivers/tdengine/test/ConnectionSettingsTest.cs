using System;

using Xunit;

namespace Zongsoft.Data.TDengine.Tests;

[Collection("Database")]
public class ConnectionSettingsTest(DatabaseFixture database)
{
	private readonly DatabaseFixture _database = database;

	[Fact]
	public void TestGetSettings()
	{
		var connectionSettings = _database.ConnectionSettings;

		Assert.NotNull(connectionSettings);
		Assert.NotEmpty(connectionSettings);
		Assert.NotEmpty(connectionSettings.Server);
		Assert.NotEmpty(connectionSettings.Database);
		Assert.NotEmpty(connectionSettings.UserName);
		Assert.NotEmpty(connectionSettings.Password);
		Assert.True(connectionSettings.Timeout > TimeSpan.Zero);
	}
}
