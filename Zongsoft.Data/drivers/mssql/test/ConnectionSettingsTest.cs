using Microsoft.Data.SqlClient;

using Xunit;

namespace Zongsoft.Data.MsSql.Tests;

public class ConnectionSettingsTest
{
	[Fact]
	public void MaximumPoolSizeMatchesProviderDefault()
	{
		var settings = Configuration.MsSqlConnectionSettingsDriver.Instance.GetSettings("Server=localhost");
		var builder = new SqlConnectionStringBuilder();

		Assert.Equal((uint)builder.MaxPoolSize, settings.MaximumPoolSize);
	}
}
