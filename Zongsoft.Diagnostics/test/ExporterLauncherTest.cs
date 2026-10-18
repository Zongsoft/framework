using System;
using System.IO;
using System.Reflection;
using System.Xml.Linq;
using System.Runtime.ExceptionServices;

using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Exporter;

using Zongsoft.Configuration;
using Zongsoft.Diagnostics.Configuration;
using Zongsoft.Diagnostics.Telemetry;

using Xunit;

namespace Zongsoft.Diagnostics.Tests;

public class ExporterLauncherTest
{
	[Fact]
	public void PluginRegistersBothStaticDriverEntries()
	{
		var plugin = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "Zongsoft.Diagnostics.plugin"));
		var entries = plugin.Root.Elements("extension");
		var count = 0;

		foreach(var extension in entries)
		{
			if((string)extension.Attribute("path") != "/Workbench/Configuration/ConnectionSettings/Drivers")
				continue;

			foreach(var entry in extension.Elements("object"))
			{
				var expression = (string)entry.Attribute("value");
				Assert.StartsWith("{static:", expression);
				var parts = expression[8..^1].Split(',', 2);
				var separator = parts[0].LastIndexOf('.');
				var type = Type.GetType(parts[0][..separator] + "," + parts[1], true);
				var driver = Assert.IsAssignableFrom<IConnectionSettingsDriver>(
					type.GetField(parts[0][(separator + 1)..], BindingFlags.Static | BindingFlags.Public).GetValue(null));
				Assert.Equal((string)entry.Attribute("name"), driver.Name);
				var settings = driver.GetSettings("");

				if(driver.Name == "opentelemetry")
				{
					Assert.Same(ExporterLauncherSettingsDriver.Default, driver);
					Assert.IsType<ExporterLauncherSettings>(settings);
				}
				else
				{
					Assert.Same(ExporterLauncherSettingsDriver.Console, driver);
					Assert.IsType<ExporterLauncherSettings.Console>(settings);
				}

				count++;
			}
		}

		Assert.Equal(2, count);
	}

	[Theory]
	[InlineData("grpc", "http://localhost:4317/")]
	[InlineData("HttpProtobuf", "http://localhost:4318/")]
	public void MissingEndpointUsesProtocolDefault(string protocol, string endpoint)
	{
		var options = new OtlpExporterOptions();
		Configure("protocol=" + protocol, options, new MetricReaderOptions());
		Assert.Equal(new Uri(endpoint), options.Endpoint);
		Assert.Equal(30000, options.TimeoutMilliseconds);
	}

	[Fact]
	public void OtlpProjectsSharedSettingsToAllThreeSignals()
	{
		const string TEXT = "server=https://localhost:4318/opentelemetry;protocol=HttpProtobuf;processorType=simple;timeout=1500ms;interval=2s;maxBatchSize=16;maxQueueSize=64";
		var metrics = new OtlpExporterOptions();
		var reader = new MetricReaderOptions();
		Configure(TEXT, metrics, reader);
		var traces = new OtlpExporterOptions();
		Configure(TEXT, traces, null);
		var logs = new OtlpExporterOptions();
		var processor = new LogRecordExportProcessorOptions();
		ConfigureLogs(TEXT, logs, processor);

		foreach(var options in new[] { metrics, traces, logs })
		{
			Assert.Equal(new Uri("https://localhost:4318/opentelemetry"), options.Endpoint);
			Assert.Equal(OtlpExportProtocol.HttpProtobuf, options.Protocol);
			Assert.Equal(1500, options.TimeoutMilliseconds);
		}

		Assert.Equal(2000, reader.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds);
		Assert.Equal(ExportProcessorType.Simple, traces.ExportProcessorType);
		Assert.Equal(16, traces.BatchExportProcessorOptions.MaxExportBatchSize);
		Assert.Equal(64, traces.BatchExportProcessorOptions.MaxQueueSize);
		Assert.Equal(1500, traces.BatchExportProcessorOptions.ExporterTimeoutMilliseconds);
		Assert.Equal(2000, traces.BatchExportProcessorOptions.ScheduledDelayMilliseconds);
		Assert.Equal(ExportProcessorType.Simple, processor.ExportProcessorType);
		Assert.Equal(16, processor.BatchExportProcessorOptions.MaxExportBatchSize);
		Assert.Equal(64, processor.BatchExportProcessorOptions.MaxQueueSize);
		Assert.Equal(1500, processor.BatchExportProcessorOptions.ExporterTimeoutMilliseconds);
		Assert.Equal(2000, processor.BatchExportProcessorOptions.ScheduledDelayMilliseconds);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	public void EmptySettingsPreserveSdkOrHostOptions(string settings)
	{
		var options = new OtlpExporterOptions
		{
			Endpoint = new Uri("https://localhost:12345/custom"),
			Protocol = OtlpExportProtocol.HttpProtobuf,
			TimeoutMilliseconds = 1234,
			ExportProcessorType = ExportProcessorType.Simple,
		};
		options.BatchExportProcessorOptions.MaxExportBatchSize = 17;
		var reader = new MetricReaderOptions();
		reader.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 6789;
		Configure(settings, options, reader);
		Configure(settings, options, null);
		var processor = new LogRecordExportProcessorOptions { ExportProcessorType = ExportProcessorType.Simple };
		processor.BatchExportProcessorOptions.MaxExportBatchSize = 19;
		ConfigureLogs(settings, options, processor);
		Assert.Equal(new Uri("https://localhost:12345/custom"), options.Endpoint);
		Assert.Equal(OtlpExportProtocol.HttpProtobuf, options.Protocol);
		Assert.Equal(1234, options.TimeoutMilliseconds);
		Assert.Equal(ExportProcessorType.Simple, options.ExportProcessorType);
		Assert.Equal(17, options.BatchExportProcessorOptions.MaxExportBatchSize);
		Assert.Equal(6789, reader.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds);
		Assert.Equal(ExportProcessorType.Simple, processor.ExportProcessorType);
		Assert.Equal(19, processor.BatchExportProcessorOptions.MaxExportBatchSize);

		var console = new ConsoleExporterOptions { Targets = ConsoleExporterOutputTargets.Debug };
		reader.PeriodicExportingMetricReaderOptions.ExportTimeoutMilliseconds = 2345;
		ConfigureConsole(settings, console, reader);
		Assert.Equal(ConsoleExporterOutputTargets.Debug, console.Targets);
		Assert.Equal(2345, reader.PeriodicExportingMetricReaderOptions.ExportTimeoutMilliseconds);
		Assert.Equal(6789, reader.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds);
	}

	[Theory]
	[InlineData("timeout=0ms", "Timeout")]
	[InlineData("timeout=-1ms", "Timeout")]
	[InlineData("timeout=30d", "Timeout")]
	[InlineData("interval=0ms", "Interval")]
	[InlineData("interval=30d", "Interval")]
	[InlineData("maxQueueSize=0", "MaxQueueSize")]
	[InlineData("maxBatchSize=0", "MaxBatchSize")]
	[InlineData("maxBatchSize=65;maxQueueSize=64", "MaxBatchSize")]
	[InlineData("protocol=99", "Protocol")]
	[InlineData("processorType=99", "ProcessorType")]
	public void RejectsInvalidRangesForAllSignals(string settings, string parameter)
	{
		Assert.Equal(parameter, Assert.Throws<ArgumentOutOfRangeException>(() =>
			Configure(settings, new OtlpExporterOptions(), new MetricReaderOptions())).ParamName);
		Assert.Equal(parameter, Assert.Throws<ArgumentOutOfRangeException>(() =>
			Configure(settings, new OtlpExporterOptions(), null)).ParamName);
		Assert.Equal(parameter, Assert.Throws<ArgumentOutOfRangeException>(() =>
			ConfigureLogs(settings, new OtlpExporterOptions(), new LogRecordExportProcessorOptions())).ParamName);
	}

	[Theory]
	[InlineData("relative")]
	[InlineData("ftp://localhost")]
	[InlineData("")]
	public void RejectsInvalidServer(string server) =>
		Assert.Equal("Server", Assert.Throws<ArgumentException>(() =>
			Configure("server=" + server, new OtlpExporterOptions(), null)).ParamName);

	[Fact]
	public void ConsoleOnlyReadsReaderSettingsForMetrics()
	{
		var options = new ConsoleExporterOptions();
		var reader = new MetricReaderOptions();
		ConfigureConsole("target=Console,Debug;timeout=1500ms;interval=2s", options, reader);
		Assert.Equal(ConsoleExporterOutputTargets.Console | ConsoleExporterOutputTargets.Debug, options.Targets);
		Assert.Equal(1500, reader.PeriodicExportingMetricReaderOptions.ExportTimeoutMilliseconds);
		Assert.Equal(2000, reader.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds);

		ConfigureConsole("target=Debug;timeout=invalid;interval=invalid", options, null);
		Assert.Equal(ConsoleExporterOutputTargets.Debug, options.Targets);
		Assert.Throws<InvalidOperationException>(() => ConfigureConsole("target=invalid", options, null));
		Assert.Equal("Target", Assert.Throws<ArgumentOutOfRangeException>(() => ConfigureConsole("target=4", options, null)).ParamName);
		Assert.Equal("Interval", Assert.Throws<ArgumentOutOfRangeException>(() => ConfigureConsole("interval=30d", options, reader)).ParamName);
		Assert.Equal("Timeout", Assert.Throws<ArgumentOutOfRangeException>(() => ConfigureConsole("timeout=0ms", options, reader)).ParamName);
	}

	//仅投影选项，不启动提供程序或访问网络；保持实现方法为 private。
	private static void Configure(string settings, OtlpExporterOptions options, MetricReaderOptions reader) =>
		Invoke([typeof(string), typeof(OtlpExporterOptions), typeof(MetricReaderOptions)], [settings, options, reader]);
	private static void ConfigureLogs(string settings, OtlpExporterOptions options, LogRecordExportProcessorOptions processor) =>
		Invoke([typeof(string), typeof(OtlpExporterOptions), typeof(LogRecordExportProcessorOptions)], [settings, options, processor]);
	private static void ConfigureConsole(string settings, ConsoleExporterOptions options, MetricReaderOptions reader) =>
		Invoke([typeof(string), typeof(ConsoleExporterOptions), typeof(MetricReaderOptions)], [settings, options, reader]);
	private static void Invoke(Type[] types, object[] arguments)
	{
		var method = typeof(ExporterLauncher).GetMethod("Configure", BindingFlags.NonPublic | BindingFlags.Static, types);
		Assert.NotNull(method);

		try
		{
			method.Invoke(null, arguments);
		}
		catch(TargetInvocationException exception)
		{
			ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
		}
	}
}
