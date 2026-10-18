using System;

using OpenTelemetry.Exporter;

using Zongsoft.Diagnostics.Configuration;

using Xunit;

namespace Zongsoft.Diagnostics.Tests;

public class ConsoleExporterLauncherSettingsTest
{
	[Fact]
	public void ConsoleHasIndependentDefaultsAndDescriptors()
	{
		var driver = ExporterLauncherSettingsDriver.Console;
		var settings = driver.GetSettings("");
		Assert.Same(driver, settings.Driver);
		Assert.Equal("opentelemetry.console", driver.Name);
		Assert.Equal(ConsoleExporterOutputTargets.Console, settings.Target);
		Assert.Equal(TimeSpan.FromSeconds(30), settings.Timeout);
		Assert.Equal(TimeSpan.FromSeconds(10), settings.Interval);
		Assert.Empty(settings);
		Assert.False(driver.Descriptors.Contains("Server"));
		Assert.False(driver.Descriptors.Contains("Protocol"));
		Assert.False(driver.Descriptors.Contains("MaxQueueSize"));
	}

	[Fact]
	public void ParsesFlagsAndRoundTripsTypedProperties()
	{
		var settings = ExporterLauncherSettingsDriver.Console.GetSettings("local", "TARGET=console,debug;timeout=1500ms;INTERVAL=2s");
		Assert.Equal("local", settings.Name);
		Assert.Equal(ConsoleExporterOutputTargets.Console | ConsoleExporterOutputTargets.Debug, settings.Target);
		Assert.Equal(TimeSpan.FromMilliseconds(1500), settings.Timeout);
		Assert.Equal(TimeSpan.FromSeconds(2), settings.Interval);
		settings.Target = ConsoleExporterOutputTargets.Debug;
		settings.Interval = TimeSpan.FromSeconds(3);
		var copy = ExporterLauncherSettingsDriver.Console.GetSettings(settings.Value);
		Assert.Equal(ConsoleExporterOutputTargets.Debug, copy.Target);
		Assert.Equal(TimeSpan.FromSeconds(3), copy.Interval);
	}

	[Fact]
	public void InvalidTargetDoesNotSilentlyUseConsole()
	{
		var settings = ExporterLauncherSettingsDriver.Console.GetSettings("target=invalid");
		Assert.Throws<InvalidOperationException>(() => settings.Target);
	}
}
