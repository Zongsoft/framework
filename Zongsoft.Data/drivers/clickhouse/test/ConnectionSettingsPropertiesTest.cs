using System;
using System.Data;

using Xunit;
using ClickHouse.Driver.ADO;

namespace Zongsoft.Data.ClickHouse.Tests;

public class ConnectionSettingsPropertiesTest
{
	[Fact]
	public void UnknownPropertiesArePreserved()
	{
		var settings = Configuration.ClickHouseConnectionSettingsDriver.Instance.GetSettings(
			"CircuitBreaker.Duration=00:01:00;CircuitBreaker.MaximumDuration=00:02:00");

		Assert.Equal("00:01:00", settings.Properties["CircuitBreaker.Duration"]);
		Assert.Equal("00:02:00", settings.Properties["CircuitBreaker.MaximumDuration"]);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void ProviderSettingsPreserveValues(bool enabled)
	{
		var settings = Configuration.ClickHouseConnectionSettingsDriver.Instance.GetSettings(
			$"Server=localhost;Port=8443;Protocol=https;Path=/clickhouse;Database=sample;UserName=reader;Timeout=45s;Compression={enabled};UseSession={enabled};SessionId=sample-session;UseCustomDecimals={enabled}");
		var options = settings.GetOptions();

		Assert.Equal(TimeSpan.FromSeconds(45), options.Timeout);
		using var connection = Assert.IsType<ClickHouseConnection>(ClickHouseDriver.Instance.CreateConnection(settings.Value));
		Assert.Equal(ConnectionState.Closed, connection.State);
		Assert.Equal("localhost", connection.Settings.Host);
		Assert.Equal((ushort)8443, connection.Settings.Port);
		Assert.Equal("https", connection.Settings.Protocol);
		Assert.Equal("/clickhouse", connection.Settings.Path);
		Assert.Equal("sample", connection.Database);
		Assert.Equal("reader", connection.Settings.Username);
		Assert.Equal(TimeSpan.FromSeconds(45), connection.Settings.Timeout);
		Assert.Equal(enabled, connection.Settings.UseCompression);
		Assert.Equal(enabled, connection.Settings.UseSession);
		Assert.Equal("sample-session", connection.Settings.SessionId);
		Assert.Equal(enabled, connection.Settings.UseCustomDecimals);
	}

	[Fact]
	public void DefaultTimeoutIsPreserved()
	{
		using var connection = Assert.IsType<ClickHouseConnection>(ClickHouseDriver.Instance.CreateConnection("Server=localhost;Database=sample"));
		Assert.Equal(TimeSpan.FromSeconds(30), connection.Settings.Timeout);
	}

	[Fact]
	public void CreatesProviderCommands()
	{
		using var empty = ClickHouseDriver.Instance.CreateCommand();
		Assert.IsType<ClickHouseCommand>(empty);

		using var command = ClickHouseDriver.Instance.CreateCommand("SELECT @value", CommandType.Text);
		Assert.IsType<ClickHouseCommand>(command);
		Assert.Equal("SELECT @value", command.CommandText);
		Assert.Equal(CommandType.Text, command.CommandType);

		var parameter = command.CreateParameter();
		parameter.ParameterName = "value";
		parameter.DbType = DbType.Int32;
		parameter.Value = 42;
		command.Parameters.Add(parameter);
		Assert.Same(parameter, command.Parameters["value"]);
	}
}
