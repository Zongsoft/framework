using Xunit;

namespace Zongsoft.Data.TDengine.Tests;

public class ConnectionSettingsPropertiesTest
{
	[Fact]
	public void UnknownPropertiesArePreserved()
	{
		var settings = Configuration.TDengineConnectionSettingsDriver.Instance.GetSettings(
			"CircuitBreaker.Duration=00:01:00;CircuitBreaker.MaximumDuration=00:02:00");

		Assert.Equal("00:01:00", settings.Properties["CircuitBreaker.Duration"]);
		Assert.Equal("00:02:00", settings.Properties["CircuitBreaker.MaximumDuration"]);
	}
}
