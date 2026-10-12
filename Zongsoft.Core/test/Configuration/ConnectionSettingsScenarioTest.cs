using System;
using System.ComponentModel;
using System.Collections.Generic;

using Xunit;

using Zongsoft.Components;

namespace Zongsoft.Configuration.Tests;

public class ConnectionSettingsScenarioTest
{
	[Fact]
	public void DriverDescriptorsPreserveAliasesDefaultsAndIgnoredMetadata()
	{
		var driver = new ScenarioDriver();

		Assert.True(driver.Descriptors.TryGetValue(nameof(ScenarioSettings.Server), out var server));
		Assert.True(driver.Descriptors.TryGetValue("host", out var host));
		Assert.Same(server, host);
		Assert.True(server.Required);

		Assert.True(driver.Descriptors.TryGetValue(nameof(ScenarioSettings.Port), out var port));
		Assert.Equal(typeof(ushort), port.Type);
		Assert.True(port.HasDefaultValue(out var defaultPort));
		Assert.Equal((ushort)2525, Assert.IsType<ushort>(defaultPort));

		Assert.True(driver.Descriptors.TryGetValue(nameof(ScenarioOptions.ConnectTimeoutMilliseconds), out var timeout));
		Assert.True(driver.Descriptors.TryGetValue(nameof(ScenarioOptions.ReadTimeoutMilliseconds), out var readTimeout));
		Assert.Same(timeout, readTimeout);
		Assert.Equal(TimeSpan.FromSeconds(2), Assert.IsType<TimeSpan>(timeout.DefaultValue));
		Assert.IsType<Components.Converters.TimeSpanConverter.Milliseconds>(timeout.Populator);

		Assert.True(driver.Descriptors.TryGetValue(nameof(ScenarioSettings.IgnoredText), out var ignored));
		Assert.True(ignored.Ignored);
	}

	[Fact]
	public void DictionaryMembershipAndEnumerationContainOnlyExplicitParameters()
	{
		IReadOnlyDictionary<string, string> settings = new ScenarioDriver().GetSettings("Server=collector.test");

		Assert.True(settings.ContainsKey("server"));
		Assert.False(settings.ContainsKey("Port"));
		Assert.False(settings.TryGetValue("Port", out var missing));
		Assert.Null(missing);
		Assert.True(settings.TryGetValue("SERVER", out var server));
		Assert.Equal("collector.test", server);

		var entry = Assert.Single(settings);
		Assert.Equal("Server", entry.Key);
		Assert.Equal("collector.test", entry.Value);
		Assert.Equal("Server", Assert.Single(settings.Keys));
		Assert.Equal("collector.test", Assert.Single(settings.Values));
	}

	[Theory]
	[InlineData("concrete")]
	[InlineData("connection")]
	[InlineData("dictionary")]
	public void MissingPortIsAbsentFromEveryStringIndexer(string view)
	{
		var settings = new ScenarioDriver().GetSettings("Server=collector.test");

		var value = view switch
		{
			"connection" => ((IConnectionSettings)settings)["Port"],
			"dictionary" => ((IReadOnlyDictionary<string, string>)settings)["Port"],
			_ => settings["Port"],
		};

		Assert.Null(value);
	}

	[Fact]
	public void MissingPortUsesDriverDefaultInTypedReadsAndOptions()
	{
		var settings = new ScenarioDriver().GetSettings("Server=collector.test");

		Assert.Equal((ushort)2525, settings.Port);
		Assert.Equal((ushort)2525, settings.ReadPort());
		Assert.Equal((ushort)2525, ((IConnectionSettings)settings).GetValue<ushort>("Port"));

		var options = settings.GetOptions();
		Assert.Equal((ushort)2525, options.Port);
		Assert.Equal(2000, options.ConnectTimeoutMilliseconds);
		Assert.Equal(2000, options.ReadTimeoutMilliseconds);
		Assert.Equal("collector.test:2525", options.Endpoint);

		var dictionary = (IReadOnlyDictionary<string, string>)settings;
		Assert.False(dictionary.ContainsKey("Port"));
		Assert.Single(dictionary);
	}

	[Theory]
	[InlineData("abc")]
	[InlineData("70000")]
	public void InvalidExplicitPortRemainsAvailableInDictionaryViews(string port)
	{
		var settings = new ScenarioDriver().GetSettings($"Port={port}");
		var dictionary = (IReadOnlyDictionary<string, string>)settings;

		Assert.Equal(port, settings["Port"]);
		Assert.Equal(port, ((IConnectionSettings)settings)["Port"]);
		Assert.True(dictionary.ContainsKey("port"));
		Assert.True(dictionary.TryGetValue("PORT", out var value));
		Assert.Equal(port, value);

		var entry = Assert.Single(dictionary);
		Assert.Equal("Port", entry.Key);
		Assert.Equal(port, entry.Value);
	}

	[Theory]
	[InlineData("abc")]
	[InlineData("70000")]
	public void InvalidExplicitPortFailsTypedProperty(string port)
	{
		var settings = new ScenarioDriver().GetSettings($"Port={port}");

		Assert.Throws<InvalidOperationException>(() => settings.Port);
	}

	[Theory]
	[InlineData("abc")]
	[InlineData("70000")]
	public void InvalidExplicitPortFailsProtectedGenericRead(string port)
	{
		var settings = new ScenarioDriver().GetSettings($"Port={port}");

		Assert.Throws<InvalidOperationException>(() => settings.ReadPort());
	}

	[Theory]
	[InlineData("abc")]
	[InlineData("70000")]
	public void InvalidExplicitPortFailsGenericRead(string port)
	{
		var settings = new ScenarioDriver().GetSettings($"Port={port}");

		Assert.Throws<InvalidOperationException>(() => ((IConnectionSettings)settings).GetValue<ushort>("Port"));
	}

	[Theory]
	[InlineData("abc")]
	[InlineData("70000")]
	public void InvalidExplicitPortFailsOptionsPopulation(string port)
	{
		var settings = new ScenarioDriver().GetSettings($"Port={port}");

		Assert.Throws<InvalidOperationException>(() => settings.GetOptions());
	}

	[Theory]
	[InlineData("abc")]
	[InlineData("70000")]
	public void InvalidExplicitPortDoesNotSucceedInTryRead(string port)
	{
		var settings = new ScenarioDriver().GetSettings($"Port={port}");

		Assert.False(((IConnectionSettings)settings).TryGetValue<ushort>("Port", out _));
	}

	[Fact]
	public void AliasesConvertersAndPopulateProduceDriverOptions()
	{
		var settings = new ScenarioDriver().GetSettings("host=collector.test;Port=4840;Timeout=1500ms;Channels=3, 5,8;IgnoredText=kept");

		Assert.Equal("collector.test", settings.Server);
		Assert.Equal((ushort)4840, settings.Port);
		Assert.Equal(TimeSpan.FromMilliseconds(1500), settings.Timeout);
		Assert.Equal(new[] { 3, 5, 8 }, settings.Channels);
		Assert.Equal("kept", settings.IgnoredText);

		var options = settings.GetOptions();
		Assert.Equal("collector.test", options.Host);
		Assert.Equal((ushort)4840, options.Port);
		Assert.Equal(1500, options.ConnectTimeoutMilliseconds);
		Assert.Equal(1500, options.ReadTimeoutMilliseconds);
		Assert.Equal(new[] { 3, 5, 8 }, options.Channels);
		Assert.Equal("factory", options.IgnoredText);
		Assert.Equal("collector.test:4840", options.Endpoint);

		var interfaceOptions = ConnectionSettingsUtility.GetOptions<ScenarioOptions>(settings);
		Assert.NotNull(interfaceOptions);
		Assert.NotSame(options, interfaceOptions);
		Assert.Equal(options.Endpoint, interfaceOptions.Endpoint);
		Assert.Equal(1500, interfaceOptions.ReadTimeoutMilliseconds);
		Assert.Equal(new[] { 3, 5, 8 }, interfaceOptions.Channels);
	}

	[Fact]
	public void TypedUpdatesReachFreshOptionsAndRoundTripThroughValue()
	{
		var driver = new ScenarioDriver();
		var settings = driver.GetSettings("Host=collector.test;Port=4840;Timeout=1500ms;Channels=3,5,8");
		var original = settings.GetOptions();

		settings.Port = 4841;
		settings.Timeout = TimeSpan.FromMilliseconds(2500);
		settings.Channels = [13, 21];

		var updated = settings.GetOptions();
		Assert.NotSame(original, updated);
		Assert.Equal("collector.test:4840", original.Endpoint);
		Assert.Equal(1500, original.ConnectTimeoutMilliseconds);
		Assert.Equal(new[] { 3, 5, 8 }, original.Channels);
		Assert.Equal("collector.test:4841", updated.Endpoint);
		Assert.Equal(2500, updated.ConnectTimeoutMilliseconds);
		Assert.Equal(2500, updated.ReadTimeoutMilliseconds);
		Assert.Equal(new[] { 13, 21 }, updated.Channels);

		var reparsed = driver.GetSettings(settings.Value);
		Assert.Equal("collector.test", reparsed.Server);
		Assert.Equal((ushort)4841, reparsed.Port);
		Assert.Equal(TimeSpan.FromMilliseconds(2500), reparsed.Timeout);
		Assert.Equal(new[] { 13, 21 }, reparsed.Channels);
		Assert.Equal(updated.Endpoint, reparsed.GetOptions().Endpoint);
	}

	[Fact]
	public void CompositeSettingsPreserveNestedStructsAndPopulateOptions()
	{
		var settings = new ScenarioDriver().GetSettings("Cluster.Address=cluster.test;cluster.Timing.Heartbeat=30s;Cluster.Timing.Retries=3");

		var cluster = settings.Cluster;
		Assert.Equal("cluster.test", cluster.Address);
		Assert.Equal(TimeSpan.FromSeconds(30), cluster.Timing.Heartbeat);
		Assert.Equal(3, cluster.Timing.Retries);

		var options = settings.GetOptions();
		Assert.Equal("cluster.test", options.Cluster.Address);
		Assert.Equal(TimeSpan.FromSeconds(30), options.Cluster.Timing.Heartbeat);
		Assert.Equal(3, options.Cluster.Timing.Retries);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void DriverFactoryParsesOnlyAfterDriverInitialization(bool named)
	{
		var driver = new ScenarioDriver();
		var settings = named ? driver.GetSettings("telemetry", "Host=collector.test;Port=4840") : driver.GetSettings("Host=collector.test;Port=4840");

		Assert.Same(driver, settings.Driver);
		Assert.True(settings.ParsedWithDriver);
		Assert.Equal(named ? "telemetry" : string.Empty, settings.Name);
		Assert.Equal("collector.test", settings.Server);
		Assert.Equal((ushort)4840, settings.Port);
	}

	[Fact]
	public void NonGenericSettingsParseAfterProtocolAndDriverInitialization()
	{
		var driver = new ScenarioDriver();
		var settings = new ProtocolSettings("telemetry", driver.Name, "host=collector.test;Port=04840;Timeout=1500ms", driver);

		Assert.Equal("telemetry", settings.Name);
		Assert.Equal(driver.Name, settings.Protocol);
		Assert.Same(driver, ((IConnectionSettings)settings).Driver);
		Assert.Equal("collector.test", settings["Server"]);
		Assert.Equal("collector.test", settings["Host"]);
		Assert.Equal((ushort)4840, ((IConnectionSettings)settings).GetValue<ushort>("Port"));
		Assert.Equal(TimeSpan.FromMilliseconds(1500), ((IConnectionSettings)settings).GetValue<TimeSpan>("Timeout"));

		var dictionary = (IReadOnlyDictionary<string, string>)settings;
		Assert.Equal(3, dictionary.Count);
		Assert.True(dictionary.TryGetValue("Server", out var server));
		Assert.Equal("collector.test", server);
		Assert.True(dictionary.TryGetValue("Port", out var port));
		Assert.Equal("04840", port);
		Assert.Contains(new KeyValuePair<string, string>("Timeout", "1500ms"), dictionary);
	}

	[Fact]
	public void DeclaredDefaultsSucceedInTypedTryReadsWithoutCreatingRawEntries()
	{
		var settings = new ScenarioDriver().GetSettings("Server=collector.test");
		Assert.True(settings.Driver.Descriptors["Port"].HasDefaultValue(out _));
		Assert.True(settings.Driver.Descriptors["KeepAlive"].HasDefaultValue(out _));
		Assert.True(settings.Driver.Descriptors["Retries"].HasDefaultValue(out _));

		Assert.True(((IConnectionSettings)settings).TryGetValue<ushort>("Port", out var port));
		Assert.Equal((ushort)2525, port);
		Assert.True(((IConnectionSettings)settings).TryGetValue<bool>("KeepAlive", out var keepAlive));
		Assert.False(keepAlive);
		Assert.True(((IConnectionSettings)settings).TryGetValue<int>("Retries", out var retries));
		Assert.Equal(0, retries);

		var dictionary = (IReadOnlyDictionary<string, string>)settings;
		Assert.False(dictionary.ContainsKey("Port"));
		Assert.False(dictionary.ContainsKey("KeepAlive"));
		Assert.False(dictionary.ContainsKey("Retries"));
		Assert.Single(dictionary);
	}

	[Fact]
	public void MissingBooleanWithoutDeclaredDefaultDoesNotSucceedInTypedTryRead()
	{
		var settings = new ScenarioDriver().GetSettings("Server=collector.test");
		Assert.False(settings.Driver.Descriptors["Compressible"].HasDefaultValue(out _));

		Assert.False(((IConnectionSettings)settings).TryGetValue<bool>("Compressible", out _));
		Assert.False(((IReadOnlyDictionary<string, string>)settings).ContainsKey("Compressible"));
	}

	[Fact]
	public void MissingIntegerWithoutDeclaredDefaultDoesNotSucceedInTypedTryRead()
	{
		var settings = new ScenarioDriver().GetSettings("Server=collector.test");
		Assert.False(settings.Driver.Descriptors["Workers"].HasDefaultValue(out _));

		Assert.False(((IConnectionSettings)settings).TryGetValue<int>("Workers", out _));
		Assert.False(((IReadOnlyDictionary<string, string>)settings).ContainsKey("Workers"));
	}

	[Fact]
	public void MissingValuesWithoutDeclaredDefaultsKeepTypedClrDefaults()
	{
		var settings = new ScenarioDriver().GetSettings("Server=collector.test");

		Assert.False(settings.Compressible);
		Assert.Equal(0, settings.Workers);
		Assert.False(((IConnectionSettings)settings).GetValue<bool>("Compressible"));
		Assert.Equal(0, ((IConnectionSettings)settings).GetValue<int>("Workers"));

		var dictionary = (IReadOnlyDictionary<string, string>)settings;
		Assert.False(dictionary.ContainsKey("Compressible"));
		Assert.False(dictionary.ContainsKey("Workers"));
		Assert.Single(dictionary);
	}

	[Fact]
	public void ExplicitFalseAndZeroSucceedInTypedTryReadsWithoutDeclaredDefaults()
	{
		var settings = new ScenarioDriver().GetSettings("Server=collector.test;Compressible=false;Workers=0");
		Assert.False(settings.Driver.Descriptors["Compressible"].HasDefaultValue(out _));
		Assert.False(settings.Driver.Descriptors["Workers"].HasDefaultValue(out _));

		Assert.True(((IConnectionSettings)settings).TryGetValue<bool>("Compressible", out var compressible));
		Assert.False(compressible);
		Assert.True(((IConnectionSettings)settings).TryGetValue<int>("Workers", out var workers));
		Assert.Equal(0, workers);
		Assert.False(settings.Compressible);
		Assert.Equal(0, settings.Workers);

		var dictionary = (IReadOnlyDictionary<string, string>)settings;
		Assert.True(dictionary.TryGetValue("Compressible", out var compressibleText));
		Assert.Equal("false", compressibleText);
		Assert.True(dictionary.TryGetValue("Workers", out var workersText));
		Assert.Equal("0", workersText);
		Assert.Equal(3, dictionary.Count);
	}

	[Theory]
	[InlineData(false, "Server=node-a;Host=node-b;Server=node-c;Port=4840")]
	[InlineData(true, "Server=node-a;Host=node-b;Server=node-c;Port=4840")]
	[InlineData(false, "Host=node-a;sErVeR=node-b;HOST=node-c;Port=4840")]
	[InlineData(true, "Host=node-a;sErVeR=node-b;HOST=node-c;Port=4840")]
	public void RepeatedCanonicalAndAliasParametersUseOriginalTextOrder(bool replaceValue, string value)
	{
		var driver = new ScenarioDriver();
		var settings = driver.GetSettings(replaceValue ? "Server=old-node;Port=2525" : value);
		if(replaceValue)
			settings.Value = value;

		var dictionary = (IReadOnlyDictionary<string, string>)settings;
		Assert.Equal(2, dictionary.Count);
		Assert.True(dictionary.TryGetValue("Server", out var server));
		Assert.Equal("node-c", server);
		Assert.Equal("node-c", settings.Server);
		Assert.Equal((ushort)4840, settings.Port);
		Assert.Empty(settings.Properties);

		var options = settings.GetOptions();
		Assert.Equal("node-c", options.Host);
		Assert.Equal((ushort)4840, options.Port);
		Assert.Equal("node-c:4840", options.Endpoint);
	}

	private sealed class ScenarioDriver : ConnectionSettingsDriver<ScenarioSettings>
	{
		public ScenarioDriver() : base("Core.ConnectionSettingsScenario") { }
	}

	private sealed class ProtocolSettings : ConnectionSettingsBase
	{
		private readonly IConnectionSettingsDriver _driver;

		public ProtocolSettings(string name, string protocol, string value, IConnectionSettingsDriver driver) : base(name, value)
		{
			this.Protocol = protocol;
			_driver = driver;

			if(!string.IsNullOrEmpty(value))
				this.OnValueChanged(value);
		}

		public string Protocol { get; }

		protected override IConnectionSettingsDriver GetDriver()
		{
			if(_driver == null || !string.Equals(this.Protocol, _driver.Name, StringComparison.OrdinalIgnoreCase))
				throw new InvalidOperationException("The protocol and driver must be initialized before settings are parsed.");

			return _driver;
		}
	}

	private sealed class ScenarioSettings : ConnectionSettingsBase<ScenarioDriver, ScenarioOptions>
	{
		private bool _parsedWithDriver;

		public ScenarioSettings(ScenarioDriver driver, string settings) : base(driver, settings) { }
		public ScenarioSettings(ScenarioDriver driver, string name, string settings) : base(driver, name, settings) { }

		[Alias(nameof(ScenarioOptions.Host))]
		[ConnectionSetting(true)]
		public string Server
		{
			get => this.GetValue<string>();
			set => this.SetValue(value);
		}

		[DefaultValue((ushort)2525)]
		public ushort Port
		{
			get => this.GetValue<ushort>();
			set => this.SetValue(value);
		}

		public bool Compressible
		{
			get => this.GetValue<bool>();
			set => this.SetValue(value);
		}

		public int Workers
		{
			get => this.GetValue<int>();
			set => this.SetValue(value);
		}

		[DefaultValue(false)]
		public bool KeepAlive
		{
			get => this.GetValue<bool>();
			set => this.SetValue(value);
		}

		[DefaultValue(0)]
		public int Retries
		{
			get => this.GetValue<int>();
			set => this.SetValue(value);
		}

		[DefaultValue("2s")]
		[Alias(nameof(ScenarioOptions.ConnectTimeoutMilliseconds))]
		[Alias(nameof(ScenarioOptions.ReadTimeoutMilliseconds))]
		[ConnectionSetting(typeof(Components.Converters.TimeSpanConverter.Milliseconds))]
		public TimeSpan Timeout
		{
			get => this.GetValue<TimeSpan>();
			set => this.SetValue(value);
		}

		[TypeConverter(typeof(Components.Converters.CollectionConverter))]
		public int[] Channels
		{
			get => this.GetValue<int[]>();
			set => this.SetValue(value);
		}

		[ConnectionSetting(Ignored = true)]
		public string IgnoredText
		{
			get => this.GetValue<string>();
			set => this.SetValue(value);
		}

		public ClusterOptions Cluster
		{
			get => this.GetValue<ClusterOptions>();
			set => this.SetValue(value);
		}

		public bool ParsedWithDriver => _parsedWithDriver;

		public ushort ReadPort() => this.GetValue<ushort>(nameof(this.Port));

		protected override void Populate(ScenarioOptions options)
		{
			base.Populate(options);
			options.Endpoint = $"{options.Host}:{options.Port}";
		}

		protected override void OnValueChanged(string value)
		{
			if(this.Driver == null)
				throw new InvalidOperationException("The driver must be initialized before settings are parsed.");

			_parsedWithDriver = true;
			base.OnValueChanged(value);
		}
	}

	private sealed class ScenarioOptions
	{
		public ScenarioOptions() { }

		public string Host { get; set; }
		public ushort Port { get; set; }
		public int ConnectTimeoutMilliseconds { get; set; }
		public int ReadTimeoutMilliseconds { get; set; }
		public int[] Channels { get; set; }
		public string IgnoredText { get; set; } = "factory";
		public ClusterOptions Cluster { get; set; }
		public string Endpoint { get; set; }
	}

	private struct ClusterOptions
	{
		public string Address { get; set; }
		public TimingOptions Timing { get; set; }
	}

	private struct TimingOptions
	{
		public TimeSpan Heartbeat { get; set; }
		public int Retries { get; set; }
	}
}
