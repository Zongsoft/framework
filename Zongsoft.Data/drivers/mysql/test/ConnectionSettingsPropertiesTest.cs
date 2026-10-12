using System;
using System.Linq;
using System.Collections.Generic;

using MySqlConnector;

using Xunit;

using Zongsoft.Configuration;

namespace Zongsoft.Data.MySql.Tests;

public class ConnectionSettingsPropertiesTest
{
	[Fact]
	public void UnknownPropertiesArePreserved()
	{
		var settings = Configuration.MySqlConnectionSettingsDriver.Instance.GetSettings(
			"CircuitBreaker.Duration=00:01:00;CircuitBreaker.MaximumDuration=00:02:00");

		Assert.Equal("00:01:00", settings.Properties["CircuitBreaker.Duration"]);
		Assert.Equal("00:02:00", settings.Properties["CircuitBreaker.MaximumDuration"]);
	}

	[Fact]
	public void NativeConnectionStringIncludesMappedSettingsAndExcludesExtensionProperties()
	{
		var settings = Configuration.MySqlConnectionSettingsDriver.Instance.GetSettings(
			"Server=mysql.invalid;Port=3308;Database=archive;Timeout=15s;CircuitBreaker.Duration=60s;mode=Read;ABC=original");

		Assert.Equal(4, ((IReadOnlyDictionary<string, string>)settings).Count);
		Assert.Equal("mysql.invalid", settings.Server);
		Assert.Equal((ushort)3308, settings.Port);
		Assert.Equal("archive", settings.Database);
		Assert.Equal(TimeSpan.FromSeconds(15), settings.Timeout);
		Assert.Equal(3, settings.Properties.Count);
		Assert.Equal("60s", settings.Properties["CircuitBreaker.Duration"]);
		Assert.Equal("Read", settings.Properties["mode"]);
		Assert.Equal("original", settings.Properties["ABC"]);

		var options = settings.GetOptions();
		Assert.Equal("mysql.invalid", options.Server);
		Assert.Equal(3308U, options.Port);
		Assert.Equal("archive", options.Database);
		Assert.Equal(15U, options.ConnectionTimeout);
		Assert.Equal(15U, options.DefaultCommandTimeout);
		var native = new MySqlConnectionStringBuilder(options.ConnectionString);
		Assert.Equal("mysql.invalid", native.Server);
		Assert.Equal(3308U, native.Port);
		Assert.Equal("archive", native.Database);
		Assert.Equal(15U, native.ConnectionTimeout);
		Assert.Equal(15U, native.DefaultCommandTimeout);
		Assert.DoesNotContain(native.Keys.Cast<string>(), settings.Properties.ContainsKey);
	}

	[Fact]
	public void PropertiesAndDatabaseUpdatesSurviveValueReconstructionAndStayOutsideNativeOptions()
	{
		var driver = Configuration.MySqlConnectionSettingsDriver.Instance;
		var settings = driver.GetSettings(
			"Server=mysql.invalid;Port=3308;Database=archive;Timeout=15s;CircuitBreaker.Duration=60s;mode=Read;ABC=original");
		settings.Properties["XYZ"] = "added";

		settings.Database = "reports";

		Assert.Equal("reports", settings.Database);
		Assert.Equal(4, settings.Properties.Count);
		Assert.Equal("60s", settings.Properties["CircuitBreaker.Duration"]);
		Assert.Equal("Read", settings.Properties["mode"]);
		Assert.Equal("original", settings.Properties["ABC"]);
		Assert.Equal("added", settings.Properties["XYZ"]);
		var value = Settings.Parse(settings.Value);
		Assert.Equal(8, ((IReadOnlyDictionary<string, string>)value).Count);
		Assert.Equal("mysql.invalid", value["Server"]);
		Assert.Equal("3308", value["Port"]);
		Assert.Equal("reports", value["Database"]);
		Assert.Equal(TimeSpan.FromSeconds(15), value.GetValue<TimeSpan>("Timeout"));
		Assert.Equal("60s", value["CircuitBreaker.Duration"]);
		Assert.Equal("Read", value["mode"]);
		Assert.Equal("original", value["ABC"]);
		Assert.Equal("added", value["XYZ"]);

		var restored = driver.GetSettings(settings.Value);
		Assert.Equal(4, ((IReadOnlyDictionary<string, string>)restored).Count);
		Assert.Equal("mysql.invalid", restored.Server);
		Assert.Equal((ushort)3308, restored.Port);
		Assert.Equal("reports", restored.Database);
		Assert.Equal(TimeSpan.FromSeconds(15), restored.Timeout);
		Assert.Equal(4, restored.Properties.Count);
		Assert.Equal("60s", restored.Properties["CircuitBreaker.Duration"]);
		Assert.Equal("Read", restored.Properties["mode"]);
		Assert.Equal("original", restored.Properties["ABC"]);
		Assert.Equal("added", restored.Properties["XYZ"]);
		var native = new MySqlConnectionStringBuilder(restored.GetOptions().ConnectionString);
		Assert.Equal("mysql.invalid", native.Server);
		Assert.Equal(3308U, native.Port);
		Assert.Equal("reports", native.Database);
		Assert.Equal(15U, native.ConnectionTimeout);
		Assert.Equal(15U, native.DefaultCommandTimeout);
		Assert.DoesNotContain(native.Keys.Cast<string>(), restored.Properties.ContainsKey);
	}

	[Theory]
	[InlineData("'trainer;model=alpha'", "trainer;model=alpha")]
	[InlineData("\"trainer;model='alpha'\"", "trainer;model='alpha'")]
	public void QuotedClientTextSurvivesApplicationNameMappingAndNativeReparse(string encoded, string expected)
	{
		var settings = Configuration.MySqlConnectionSettingsDriver.Instance.GetSettings(
			$"Server=mysql.invalid;Port=3308;Database=archive;Timeout=15s;Client={encoded}");

		Assert.Equal(expected, settings.Client);
		Assert.Empty(settings.Properties);
		var options = settings.GetOptions();
		Assert.Equal(expected, options.ApplicationName);
		var native = new MySqlConnectionStringBuilder(options.ConnectionString);
		Assert.Equal(expected, native.ApplicationName);
		Assert.Equal("mysql.invalid", native.Server);
		Assert.Equal(3308U, native.Port);
		Assert.Equal("archive", native.Database);
		Assert.Equal(15U, native.ConnectionTimeout);
		Assert.Equal(15U, native.DefaultCommandTimeout);
	}
}
