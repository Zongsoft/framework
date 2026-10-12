using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

using Microsoft.Extensions.Configuration;

using Xunit;

using Zongsoft.Components;

namespace Zongsoft.Configuration.Tests;

[CollectionDefinition(NAME, DisableParallelization = true)]
public sealed class ConfigurationSettingsScenarioCollection
{
	public const string NAME = nameof(ConfigurationSettingsScenarioCollection);
}

[Collection(ConfigurationSettingsScenarioCollection.NAME)]
public class ConfigurationSettingsScenarioTest
{
	private static readonly BindingDriver _driver = new();

	[Fact]
	public void XmlBindsSingleMessageSettings()
	{
		using var configuration = CreateConfiguration("""
			<configuration>
				<option path="/Messaging">
					<settings value="Sender=notifications@example.test;Host=mail.example.test;Port=2525;AuthorityCode='test=authority;code'" />
				</option>
			</configuration>
			""");

		var settings = configuration.GetOption<Settings>("/Messaging/Settings");

		Assert.NotNull(settings);
		Assert.Equal(4, ((IReadOnlyDictionary<string, string>)settings).Count);
		Assert.Equal("notifications@example.test", settings["Sender"]);
		Assert.Equal("mail.example.test", settings["Host"]);
		Assert.Equal((ushort)2525, SettingsUtility.GetValue<ushort>(settings, "Port"));
		Assert.Equal("test=authority;code", settings["AuthorityCode"]);

		var reparsed = new Settings(settings.Value);
		Assert.Equal(settings["Sender"], reparsed["sender"]);
		Assert.Equal(settings["Host"], reparsed["HOST"]);
		Assert.Equal(settings["Port"], reparsed["Port"]);
		Assert.Equal(settings["AuthorityCode"], reparsed["AuthorityCode"]);
	}

	[Fact]
	public void XmlConnectionCollectionSelectsDefaultNameAndDriver()
	{
		WithDriver(driver =>
		{
			using var configuration = CreateConfiguration($"""
				<configuration>
					<option path="/Telemetry">
						<connectionSettings default="Primary">
							<connectionSetting connectionSetting.name="Primary" driver="{driver.Name}" value="Host=primary.example.test;Port=1883" />
							<connectionSetting connectionSetting.name="Archive" value="Server=archive.example.test;Port=1884" />
						</connectionSettings>
					</option>
				</configuration>
				""");

			var collection = configuration.GetOption<ConnectionSettingsCollection>("/Telemetry/ConnectionSettings");
			Assert.Equal(2, collection.Count);
			Assert.Equal("Primary", collection.Default);
			Assert.Same(collection["PRIMARY"], collection.GetDefault());
			Assert.Same(collection.GetDefault(), collection[string.Empty]);
			Assert.True(collection.TryGetValue("primary", driver.Name.ToUpperInvariant(), out var primary));
			Assert.IsType<BindingSettings>(primary);
			Assert.True(collection.Contains("PRIMARY", driver.Name.ToLowerInvariant()));
			Assert.False(collection.Contains("Primary", "different-driver"));
			Assert.False(collection.TryGetValue("missing", out _));
			Assert.False(collection.TryGetValue("Primary", "different-driver", out _));

			var selected = configuration.GetConnectionSettings("/Telemetry", null, driver.Name.ToUpperInvariant());
			Assert.IsType<BindingSettings>(selected);
			Assert.Equal("Primary", selected.Name);
			Assert.Equal("primary.example.test", selected["Server"]);
			Assert.Equal("Primary", configuration.GetConnectionSettings("/Telemetry/ConnectionSettings", "PRIMARY", driver.Name).Name);
			Assert.Equal("Archive", configuration.GetConnectionSettings("/Telemetry", "archive").Name);
			Assert.Null(configuration.GetConnectionSettings("/Telemetry", "Primary", "different-driver"));
			Assert.Null(configuration.GetConnectionSettings("/Telemetry", "missing", driver.Name));
			Assert.Null(configuration.GetConnectionSettings("/Missing", "Primary", driver.Name));
		});
	}

	[Fact]
	public void XmlResolvesNamedConcreteConnectionSettingsAndOptions()
	{
		WithDriver(driver =>
		{
			using var configuration = CreateConfiguration($"""
				<configuration>
					<option path="/Telemetry">
						<connectionSettings default="Primary">
							<connectionSetting connectionSetting.name="Primary" driver="{driver.Name}" value="Host=collector.example.test;Port=1883;Timeout=1500ms;Channels=2,5,8" />
						</connectionSettings>
					</option>
				</configuration>
				""");

			var settings = Assert.IsType<BindingSettings>(configuration.GetOption<IConnectionSettings>("/telemetry/connectionsettings/PRIMARY"));
			Assert.Equal("Primary", settings.Name);
			Assert.Same(driver, settings.Driver);
			Assert.Equal("collector.example.test", settings.Server);
			Assert.Equal(TimeSpan.FromMilliseconds(1500), settings.Timeout);

			var options = settings.GetOptions();
			Assert.Equal("collector.example.test", options.Host);
			Assert.Equal((ushort)1883, options.Port);
			Assert.Equal(1500, options.ConnectTimeoutMilliseconds);
			Assert.Equal(1500, options.ReadTimeoutMilliseconds);
			Assert.Equal(new[] { 2, 5, 8 }, options.Channels);
		});
	}

	[Fact]
	public void PlainSettingsExplicitlyConvertThroughValueToDriverOptions()
	{
		var source = new Settings("training", "Host=collector.example.test;Port=1883;Timeout=2500ms;Channels=3,5,8;Metric=power;Topic=telemetry");
		source["Port"] = "1884";

		var settings = _driver.GetSettings(source.Name, source.Value);
		Assert.Equal("training", settings.Name);
		Assert.True(settings.Properties.TryGetValue("Metric", out var metric));
		Assert.Equal("power", metric);
		Assert.True(settings.Properties.TryGetValue("Topic", out var topic));
		Assert.Equal("telemetry", topic);

		var dictionary = (IReadOnlyDictionary<string, string>)settings;
		Assert.Equal(4, dictionary.Count);
		Assert.False(dictionary.ContainsKey("Metric"));
		Assert.False(dictionary.ContainsKey("Topic"));
		Assert.Equal("collector.example.test", dictionary["Server"]);

		var options = settings.GetOptions();
		Assert.Equal("collector.example.test", options.Host);
		Assert.Equal((ushort)1884, options.Port);
		Assert.Equal(2500, options.ConnectTimeoutMilliseconds);
		Assert.Equal(2500, options.ReadTimeoutMilliseconds);
		Assert.Equal(new[] { 3, 5, 8 }, options.Channels);
		Assert.Equal("power", source["Metric"]);
		Assert.Equal("telemetry", source["Topic"]);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void XmlExternalPropertiesAppearInConnectionValue(bool specialized)
	{
		WithDriver(driver =>
		{
			var driverAttribute = specialized ? $" driver=\"{driver.Name}\"" : string.Empty;
			using var configuration = CreateConfiguration($"""
				<configuration>
					<option path="/Telemetry">
						<connectionSettings>
							<connectionSetting connectionSetting.name="Primary"{driverAttribute} metric="power" topic="telemetry" value="Server=collector.example.test;Port=1883" />
						</connectionSettings>
					</option>
				</configuration>
				""");

			var settings = configuration.GetOption<IConnectionSettings>("/Telemetry/ConnectionSettings/Primary");
			Assert.Equal(specialized ? typeof(BindingSettings) : typeof(ConnectionSettings), settings.GetType());
			Assert.True(settings.Properties.TryGetValue("metric", out var metric));
			Assert.Equal("power", metric);
			Assert.True(settings.Properties.TryGetValue("topic", out var topic));
			Assert.Equal("telemetry", topic);

			var exported = new Settings(settings.Value);
			Assert.Equal("collector.example.test", exported["Server"]);
			Assert.Equal("1883", exported["Port"]);
			Assert.Equal("power", exported["Metric"]);
			Assert.Equal("telemetry", exported["Topic"]);
		});
	}

	[Fact]
	public void XmlExternalPropertiesSurviveExplicitDriverConversion()
	{
		using var configuration = CreateConfiguration("""
			<configuration>
				<option path="/Telemetry">
					<connectionSettings>
						<connectionSetting connectionSetting.name="Primary" metric="power" value="Server=collector.example.test;Port=1883;Timeout=1500ms" />
					</connectionSettings>
				</option>
			</configuration>
			""");

		var source = configuration.GetOption<IConnectionSettings>("/Telemetry/ConnectionSettings/Primary");
		Assert.Equal("power", source.Properties["Metric"]);

		var converted = _driver.GetSettings(source.Name, source.Value);
		var options = converted.GetOptions();
		Assert.Equal("Primary", converted.Name);
		Assert.Equal("collector.example.test", options.Host);
		Assert.Equal((ushort)1883, options.Port);
		Assert.Equal(1500, options.ConnectTimeoutMilliseconds);
		Assert.Equal(1500, options.ReadTimeoutMilliseconds);
		Assert.True(converted.Properties.TryGetValue("Metric", out var metric));
		Assert.Equal("power", metric);
	}

	[Fact]
	public void ConnectionSelectionReturnsNullForMissingDefault()
	{
		using var configuration = CreateConfiguration("""
			<configuration>
				<option path="/Telemetry">
					<connectionSettings default="Missing">
						<connectionSetting connectionSetting.name="Archive" value="Server=archive.example.test" />
					</connectionSettings>
				</option>
			</configuration>
			""");

		var collection = configuration.GetOption<ConnectionSettingsCollection>("/Telemetry/ConnectionSettings");
		Assert.Single(collection);
		Assert.Null(collection.GetDefault());
		Assert.False(collection.TryGetValue(null, out _));
		Assert.True(collection.TryGetValue("ARCHIVE", out var archive));
		Assert.Equal("Archive", archive.Name);

		Assert.Null(configuration.GetConnectionSettings("/Telemetry", null));
		Assert.Null(configuration.GetConnectionSettings("/Telemetry", string.Empty));
		Assert.Equal("Archive", configuration.GetConnectionSettings("/Telemetry", "archive").Name);
	}

	[Theory]
	[InlineData("host")]
	[InlineData("HOST")]
	public void XmlAliasAttributesAreRejectedByProperties(string attributeName)
	{
		WithDriver(driver =>
		{
			using var configuration = CreateConfiguration($"""
				<configuration>
					<option path="/Telemetry">
						<connectionSettings>
							<connectionSetting connectionSetting.name="Primary" driver="{driver.Name}" {attributeName}="new.example.test" trace="on" value="Port=1883" />
						</connectionSettings>
					</option>
				</configuration>
				""");

			var exception = Record.Exception(() => configuration.GetOption<IConnectionSettings>("/Telemetry/ConnectionSettings/Primary"));
			Assert.NotNull(exception);
			Assert.Contains(GetExceptionChain(exception), error => error is ArgumentException);
		});
	}

	[Fact]
	public void XmlDeclaredPropertiesBindAndRejectAliasExtensions()
	{
		WithDriver(driver =>
		{
			using var configuration = CreateConfiguration($"""
				<configuration>
					<option path="/Telemetry">
						<connectionSettings>
							<connectionSetting connectionSetting.name="Primary" driver="{driver.Name}" server="collector.example.test" port="1883" trace="on" value="Timeout=1500ms;Channels=2,5,8" />
						</connectionSettings>
					</option>
				</configuration>
				""");

			var settings = Assert.IsType<BindingSettings>(configuration.GetOption<IConnectionSettings>("/Telemetry/ConnectionSettings/Primary"));
			Assert.Equal("collector.example.test", settings.Server);
			Assert.Equal((ushort)1883, settings.Port);
			Assert.Equal(TimeSpan.FromMilliseconds(1500), settings.Timeout);
			Assert.Equal(new[] { 2, 5, 8 }, settings.Channels);

			var options = settings.GetOptions();
			Assert.Equal("collector.example.test", options.Host);
			Assert.Equal((ushort)1883, options.Port);
			Assert.Equal(1500, options.ConnectTimeoutMilliseconds);
			Assert.Equal(1500, options.ReadTimeoutMilliseconds);
			Assert.Equal(new[] { 2, 5, 8 }, options.Channels);
			Assert.Equal("on", settings.Properties["Trace"]);

			var value = settings.Value;

			Assert.Throws<ArgumentException>(() => settings.Properties["Host"] = "rejected.example.test");

			Assert.Equal("collector.example.test", settings.Server);
			Assert.Equal((ushort)1883, settings.Port);
			Assert.False(settings.Properties.TryGetValue("Host", out _));
			Assert.False(settings.Properties.TryGetValue("Server", out _));
			Assert.Equal("on", settings.Properties["Trace"]);
			Assert.Equal(value, settings.Value);

			var exported = new Settings(settings.Value);
			Assert.Equal("collector.example.test", exported["Server"]);
			Assert.Equal("1883", exported["Port"]);
			Assert.Equal("on", exported["Trace"]);
			Assert.Equal(TimeSpan.FromMilliseconds(1500), SettingsUtility.GetValue<TimeSpan>(exported, "Timeout"));
			Assert.Equal(new[] { 2, 5, 8 }, _driver.GetSettings(exported.Value).Channels);
		});
	}

	[Fact]
	public void XmlConstructsSettingsInterfaceAndBindsNamedValue()
	{
		using var configuration = CreateConfiguration("""
			<configuration>
				<option path="/Messaging">
					<settings name="mail" value="Sender=notifications@example.test;Host=mail.example.test;Port=2525;Enabled=false" />
				</option>
			</configuration>
			""");

		var settings = configuration.GetOption<ISettings>("/Messaging/Settings");

		Assert.IsType<Settings>(settings);
		Assert.Equal("mail", settings.Name);
		Assert.Equal(4, settings.Count);
		Assert.Equal("notifications@example.test", settings["Sender"]);
		Assert.Equal("mail.example.test", settings["Host"]);
		Assert.Equal("2525", settings["Port"]);
		Assert.Equal("false", settings["Enabled"]);
		Assert.False(settings.ContainsKey("Name"));
		Assert.False(settings.ContainsKey("Value"));

		settings.Add("Retries", "0");
		Assert.True(settings.Remove("Enabled"));

		var exported = new Settings(settings.Value);
		Assert.Equal(4, exported.Count);
		Assert.Equal("notifications@example.test", exported["Sender"]);
		Assert.Equal("mail.example.test", exported["Host"]);
		Assert.Equal("2525", exported["Port"]);
		Assert.Equal("0", exported["Retries"]);
		Assert.False(exported.ContainsKey("Enabled"));
	}

	[Fact]
	public void XmlValueBindingRetainsParametersAndMetadataOnBothSidesOfValue()
	{
		WithDriver(driver =>
		{
			using var configuration = CreateConfiguration($"""
				<configuration>
					<option path="/Telemetry">
						<connectionSettings>
							<connectionSetting connectionSetting.name="Primary" driver="{driver.Name}" beforeValue="first" value="Port=1883;Timeout=1500ms;Metric=power" zAfterValue="last" server="collector.example.test" />
						</connectionSettings>
					</option>
				</configuration>
				""");

			var settings = Assert.IsType<BindingSettings>(configuration.GetOption<IConnectionSettings>("/Telemetry/ConnectionSettings/Primary"));

			Assert.Equal("Primary", settings.Name);
			Assert.Same(driver, settings.Driver);
			Assert.Equal(3, settings.Count);
			Assert.Equal("collector.example.test", settings["Server"]);
			Assert.Equal("1883", settings["Port"]);
			Assert.Equal(3, settings.Properties.Count);
			Assert.Equal("power", settings.Properties["Metric"]);
			Assert.Equal("first", settings.Properties["BeforeValue"]);
			Assert.Equal("last", settings.Properties["ZAfterValue"]);
			Assert.False(settings.ContainsKey("Metric"));
			Assert.False(settings.ContainsKey("BeforeValue"));
			Assert.False(settings.ContainsKey("ZAfterValue"));

			var options = settings.GetOptions();
			Assert.Equal("collector.example.test", options.Host);
			Assert.Equal((ushort)1883, options.Port);
			Assert.Equal(1500, options.ConnectTimeoutMilliseconds);
			Assert.Equal(1500, options.ReadTimeoutMilliseconds);

			var exported = new Settings(settings.Value);
			Assert.Equal(6, exported.Count);
			Assert.Equal("collector.example.test", exported["Server"]);
			Assert.Equal("1883", exported["Port"]);
			Assert.Equal(TimeSpan.FromMilliseconds(1500), SettingsUtility.GetValue<TimeSpan>(exported, "Timeout"));
			Assert.Equal("power", exported["Metric"]);
			Assert.Equal("first", exported["BeforeValue"]);
			Assert.Equal("last", exported["ZAfterValue"]);
		});
	}

	private static IEnumerable<Exception> GetExceptionChain(Exception exception)
	{
		for(var current = exception; current != null; current = current.InnerException)
			yield return current;
	}

	private static ConfigurationRoot CreateConfiguration(string xml)
	{
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
		return (ConfigurationRoot)new ConfigurationBuilder().AddOptionStream(stream).Build();
	}

	private static void WithDriver(Action<BindingDriver> action)
	{
		ConnectionSettings.Drivers.Add(_driver);

		try
		{
			action(_driver);
		}
		finally
		{
			ConnectionSettings.Drivers.Remove(_driver.Name);
		}
	}

	private sealed class BindingDriver : ConnectionSettingsDriver<BindingSettings>
	{
		public BindingDriver() : base("Core.ConfigurationSettingsScenario") { }
	}

	private sealed class BindingSettings : ConnectionSettingsBase<BindingDriver, BindingOptions>
	{
		public BindingSettings(BindingDriver driver, string settings) : base(driver, settings) { }
		public BindingSettings(BindingDriver driver, string name, string settings) : base(driver, name, settings) { }

		[Alias(nameof(BindingOptions.Host))]
		public string Server
		{
			get => this.GetValue<string>();
			set => this.SetValue(value);
		}

		public ushort Port
		{
			get => this.GetValue<ushort>();
			set => this.SetValue(value);
		}

		[Alias(nameof(BindingOptions.ConnectTimeoutMilliseconds))]
		[Alias(nameof(BindingOptions.ReadTimeoutMilliseconds))]
		[ConnectionSetting(typeof(Components.Converters.TimeSpanConverter.Milliseconds))]
		public TimeSpan Timeout
		{
			get => this.GetValue<TimeSpan>();
			set => this.SetValue(value);
		}

		public int[] Channels
		{
			get => this.GetValue<int[]>();
			set => this.SetValue(value);
		}
	}

	private sealed class BindingOptions
	{
		public BindingOptions() { }

		public string Host { get; set; }
		public ushort Port { get; set; }
		public int ConnectTimeoutMilliseconds { get; set; }
		public int ReadTimeoutMilliseconds { get; set; }
		public int[] Channels { get; set; }
	}
}
