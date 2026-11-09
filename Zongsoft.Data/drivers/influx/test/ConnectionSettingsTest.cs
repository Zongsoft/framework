using System;

namespace Zongsoft.Data.Influx.Tests;

public class ConnectionSettingsTest
{
	[Fact]
	public void TestGetSettings()
	{
		var connectionSettings = Configuration.InfluxConnectionSettingsDriver.Instance.GetSettings("Server=http://127.0.0.1:8086;Database=unit-test;Token=unit-test-token");

		Assert.NotNull(connectionSettings);
		Assert.NotEmpty(connectionSettings);
		Assert.Equal("http://127.0.0.1:8086", connectionSettings.Server);
		Assert.Equal("unit-test", connectionSettings.Database);
		Assert.Equal("unit-test-token", connectionSettings.Token);
		Assert.True(connectionSettings.Timeout > TimeSpan.Zero);
	}
}
