using MySqlConnector;

using Xunit;

namespace Zongsoft.Data.MySql.Tests;

public class ConnectionSettingsTest
{
	[Fact]
	public void MaximumPoolSizeMatchesProviderDefault()
	{
		var settings = Configuration.MySqlConnectionSettingsDriver.Instance.GetSettings("Server=localhost");
		var builder = new MySqlConnectionStringBuilder();

		Assert.Equal(builder.MaximumPoolSize, settings.MaximumPoolSize);
	}
}
