using Npgsql;

using Xunit;

namespace Zongsoft.Data.PostgreSql.Tests;

public class ConnectionSettingsTest
{
	[Fact]
	public void MaximumPoolSizeMatchesProviderDefault()
	{
		var settings = Configuration.PostgreSqlConnectionSettingsDriver.Instance.GetSettings("Server=localhost");
		var builder = new NpgsqlConnectionStringBuilder();

		Assert.Equal((uint)builder.MaxPoolSize, settings.MaximumPoolSize);
	}
}
