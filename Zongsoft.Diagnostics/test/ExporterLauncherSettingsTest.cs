using System;

using OpenTelemetry;
using OpenTelemetry.Exporter;

using Zongsoft.Diagnostics.Configuration;

using Xunit;

namespace Zongsoft.Diagnostics.Tests;

public class ExporterLauncherSettingsTest
{
	[Fact]
	public void DefaultsDoNotBecomeExplicitSettings()
	{
		var settings = ExporterLauncherSettingsDriver.Default.GetSettings("");
		Assert.Same(ExporterLauncherSettingsDriver.Default, settings.Driver);
		Assert.Equal("opentelemetry", settings.Driver.Name);
		Assert.Null(settings.Server);
		Assert.Equal(OtlpExportProtocol.Grpc, settings.Protocol);
		Assert.Equal(ExportProcessorType.Batch, settings.ProcessorType);
		Assert.Equal(TimeSpan.FromSeconds(30), settings.Timeout);
		Assert.Equal(TimeSpan.FromSeconds(10), settings.Interval);
		Assert.Equal(512, settings.MaxBatchSize);
		Assert.Equal(2048, settings.MaxQueueSize);
		Assert.Empty(settings);
		Assert.Null(settings["timeout"]);
	}

	[Fact]
	public void ParsesTypedValuesAndPreservesExtensions()
	{
		var settings = ExporterLauncherSettingsDriver.Default.GetSettings("collector",
			"SERVER='https://localhost:4318/otel';PROTOCOL=httpprotobuf;processorType=simple;timeout=1500ms;interval=2s;maxBatchSize=16;maxQueueSize=64;custom='a;b';");
		Assert.Equal("collector", settings.Name);
		Assert.Equal("https://localhost:4318/otel", settings.Server);
		Assert.Equal(OtlpExportProtocol.HttpProtobuf, settings.Protocol);
		Assert.Equal(ExportProcessorType.Simple, settings.ProcessorType);
		Assert.Equal(TimeSpan.FromMilliseconds(1500), settings.Timeout);
		Assert.Equal(TimeSpan.FromSeconds(2), settings.Interval);
		Assert.Equal(16, settings.MaxBatchSize);
		Assert.Equal(64, settings.MaxQueueSize);
		Assert.Equal("a;b", settings["custom"]);

		settings.Protocol = OtlpExportProtocol.Grpc;
		settings.Timeout = TimeSpan.FromSeconds(3);
		settings.MaxBatchSize = 32;
		var copy = ExporterLauncherSettingsDriver.Default.GetSettings(settings.Name, settings.Value);
		Assert.Equal(settings.Protocol, copy.Protocol);
		Assert.Equal(settings.Timeout, copy.Timeout);
		Assert.Equal(32, copy.MaxBatchSize);
		Assert.Equal("a;b", copy["custom"]);
	}

	[Fact]
	public void InvalidTypedValuesDoNotSilentlyUseDefaults()
	{
		var settings = ExporterLauncherSettingsDriver.Default.GetSettings("protocol=invalid;timeout=invalid;maxQueueSize=invalid");
		Assert.Throws<InvalidOperationException>(() => settings.Protocol);
		Assert.Throws<InvalidOperationException>(() => settings.Timeout);
		Assert.Throws<InvalidOperationException>(() => settings.MaxQueueSize);
	}
}
