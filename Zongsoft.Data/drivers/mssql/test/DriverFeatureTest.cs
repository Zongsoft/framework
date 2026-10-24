using Xunit;

using Microsoft.Data.SqlClient;

using Zongsoft.Data.Common;
using Zongsoft.Data.Features;

namespace Zongsoft.Data.MsSql.Tests;

public class DriverFeatureTest() : Zongsoft.Data.Tests.DriverFeatureTestBase(MsSqlDriver.Instance, true, false, connectionSharing: true)
{
	[Theory]
	[InlineData("Server=localhost", true)]
	[InlineData("Server=localhost;MultipleActiveResultSets=True", true)]
	[InlineData("Server=localhost;MultipleActiveResultSets=False", false)]
	[InlineData("Server=localhost;multipleactiveresultsets=false", false)]
	public void ConnectionSharingMatchesNativeConnectionSettings(string settings, bool enabled)
	{
		var source = new DataSource("ConnectionSharing", settings);
		var feature = MsSqlDriver.Instance.Features.Get<IConnectionSharingFeature>();
		using var connection = MsSqlDriver.Instance.CreateConnection(settings);
		var options = new SqlConnectionStringBuilder(connection.ConnectionString);

		Assert.NotNull(feature);
		Assert.Equal(enabled, options.MultipleActiveResultSets);
		Assert.Equal(options.MultipleActiveResultSets, feature.CanShare(source));
	}
}
