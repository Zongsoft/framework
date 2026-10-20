using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;

using Zongsoft.Services;
using Zongsoft.Configuration;
using Zongsoft.Diagnostics.Telemetry;

using Xunit;
using MsLogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Zongsoft.Diagnostics.Tests;

[Collection("Diagnostics")]
public class LoggingTest
{
	#region 测试方法
	[Fact]
	public void RegistrationExportsStructuredLogsAndPreservesHostFiltering()
	{
		using var configuration = CreateConfiguration("<exporter driver=\"test\" />");
		configuration["Logging:LogLevel:Default"] = "Warning";
		var launcher = new LogLauncher();
		var services = CreateServices(configuration, launcher);
		services.Register(typeof(DiagnostorWorker).Assembly, configuration);
		using var provider = services.BuildServiceProvider();
		var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("logging-test");

		logger.LogInformation("Filtered {Value}", 1);
		using var activity = new Activity("logging-test").SetIdFormat(ActivityIdFormat.W3C).Start();
		using var scope = logger.BeginScope(new Dictionary<string, object> { ["ScopeKey"] = "scope-value" });
		var exception = new InvalidOperationException();
		logger.LogError(new EventId(23, "TestEvent"), exception, "Processed {Count} records", 7);
		Assert.True(provider.GetRequiredService<LoggerProvider>().ForceFlush());

		var record = Assert.Single(launcher.Records);
		Assert.Equal("logging-test", record.CategoryName);
		Assert.Equal(MsLogLevel.Error, record.LogLevel);
		Assert.Equal(23, record.EventId.Id);
		Assert.Same(exception, record.Exception);
		Assert.Equal("Processed 7 records", record.FormattedMessage);
		Assert.Contains(record.Attributes, item => item.Key == "Count" && Equals(item.Value, 7));
		Assert.Equal(activity.TraceId, record.TraceId);
		Assert.Equal(activity.SpanId, record.SpanId);
		var scopes = new List<KeyValuePair<string, object>>();
		record.ForEachScope((item, state) =>
		{
			foreach(var attribute in item)
				state.Add(attribute);
		}, scopes);
		Assert.Contains(scopes, item => item.Key == "ScopeKey" && Equals(item.Value, "scope-value"));
		Assert.Equal(1, launcher.Launches);
		Assert.Single(provider.GetServices<ILoggerProvider>().OfType<OpenTelemetryLoggerProvider>());

		var resource = provider.GetRequiredService<LoggerProvider>().GetResource().Attributes.ToDictionary(item => item.Key, item => item.Value);
		Assert.Equal("logging-test-service", resource["service.name"]);
		Assert.False(string.IsNullOrWhiteSpace((string)resource["service.instance.id"]));
	}

	[Theory]
	[InlineData("", true)]
	[InlineData("target=console", true)]
	[InlineData("target=debug", false)]
	[InlineData("target=Console,Debug", true)]
	public void ConsoleExporterRespectsTargetAndCoexistsWithOtherExporters(string settings, bool writesConsole)
	{
		using var configuration = CreateConfiguration($"""
			<exporter exporter.name="console" driver="opentelemetry.console" settings="{settings}" />
			<exporter exporter.name="test" driver="test" />
			""");
		var launcher = new LogLauncher();
		var services = CreateServices(configuration, launcher);
		services.Register(typeof(DiagnostorWorker).Assembly, configuration);
		using var provider = services.BuildServiceProvider();
		var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("console-test");
		using var output = new StringWriter();
		var original = Console.Out;

		try
		{
			Console.SetOut(output);
			logger.LogWarning("Console probe {Value}", "diagnostics-console-value");
			Assert.True(provider.GetRequiredService<LoggerProvider>().ForceFlush());
		}
		finally
		{
			Console.SetOut(original);
		}

		Assert.Equal(writesConsole, output.ToString().Contains("diagnostics-console-value", StringComparison.Ordinal));
		Assert.Equal("Console probe diagnostics-console-value", Assert.Single(launcher.Records).FormattedMessage);
		Assert.Equal(1, launcher.Launches);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("<exporter driver=\"missing\" enabled=\"false\" settings=\"invalid\" />")]
	[InlineData("<exporter driver=\"Console\" enabled=\"false\" settings=\"target=invalid\" />")]
	public void AbsentOrDisabledExportersDoNotRegisterLogging(string exporters)
	{
		using var configuration = CreateConfiguration(exporters);
		var services = CreateServices(configuration);
		using var provider = services.BuildServiceProvider();
		Assert.DoesNotContain(provider.GetServices<ILoggerProvider>(), item => item is OpenTelemetryLoggerProvider);
		Assert.Null(provider.GetService<LoggerProvider>());
	}

	[Fact]
	public void MissingEnabledDriverFailsBeforeLaunchingAnyExporter()
	{
		using var configuration = CreateConfiguration("""
			<exporter exporter.name="test" driver="test" />
			<exporter exporter.name="missing" driver="missing" />
			""");
		var launcher = new LogLauncher();
		using var provider = CreateServices(configuration, launcher).BuildServiceProvider();
		var exception = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<ILoggerFactory>());
		Assert.Equal("missing", exception.Data["ExporterDriver"]);
		Assert.Equal("logs", exception.Data["Signal"]);
		Assert.Equal(0, launcher.Launches);
	}

	[Fact]
	public void LoggerOptionsCanDisableScopesAndFormattedMessages()
	{
		using var configuration = CreateConfiguration("<exporter driver=\"test\" />", "includeScopes=\"false\" includeFormattedMessage=\"false\"");
		var launcher = new LogLauncher();
		using var provider = CreateServices(configuration, launcher).BuildServiceProvider();
		var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("logging-test");
		using var scope = logger.BeginScope("test-scope");
		logger.LogWarning("Value {Value}", 4);
		var record = Assert.Single(launcher.Records);
		Assert.Null(record.FormattedMessage);
		var scopes = new List<LogRecordScope>();
		record.ForEachScope((item, state) => state.Add(item), scopes);
		Assert.Empty(scopes);
		Assert.Contains(record.Attributes, item => item.Key == "Value" && Equals(item.Value, 4));
	}

	[Fact]
	public void DisposingHostServicesFlushesBatchAndDisposesExporter()
	{
		using var configuration = CreateConfiguration("<exporter driver=\"batch\" />");
		var launcher = new BatchLauncher();
		var provider = CreateServices(configuration, launcher).BuildServiceProvider();

		try
		{
			provider.GetRequiredService<ILoggerFactory>().CreateLogger("logging-test").LogWarning("Queued log");
			Assert.Equal(0, launcher.Exporter.Count);
		}
		finally
		{
			provider.Dispose();
		}

		Assert.Equal(1, launcher.Exporter.Count);
		Assert.Equal(1, launcher.Exporter.Disposals);
	}

	[Fact]
	public void EnvironmentOverridesServiceIdentity()
	{
		var originalName = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME");
		var originalAttributes = Environment.GetEnvironmentVariable("OTEL_RESOURCE_ATTRIBUTES");

		try
		{
			Environment.SetEnvironmentVariable("OTEL_SERVICE_NAME", "configured-logs");
			Environment.SetEnvironmentVariable("OTEL_RESOURCE_ATTRIBUTES", "service.instance.id=logs-instance,service.namespace=test");
			using var configuration = CreateConfiguration("<exporter driver=\"test\" />");
			using var provider = CreateServices(configuration, new LogLauncher()).BuildServiceProvider();
			var resource = provider.GetRequiredService<LoggerProvider>().GetResource().Attributes.ToDictionary(item => item.Key, item => item.Value);
			Assert.Equal("configured-logs", resource["service.name"]);
			Assert.Equal("logs-instance", resource["service.instance.id"]);
			Assert.Equal("test", resource["service.namespace"]);
		}
		finally
		{
			Environment.SetEnvironmentVariable("OTEL_SERVICE_NAME", originalName);
			Environment.SetEnvironmentVariable("OTEL_RESOURCE_ATTRIBUTES", originalAttributes);
		}
	}
	#endregion

	#region 辅助方法
	private static ServiceCollection CreateServices(IConfiguration configuration, IExporterLauncher<OpenTelemetryLoggerOptions> launcher = null)
	{
		var services = new ServiceCollection();
		services.AddSingleton(configuration);
		services.AddSingleton<IHostEnvironment>(new TestEnvironment());
		services.AddLogging(logging => logging.AddConfiguration(configuration.GetSection("Logging")));

		if(launcher != null)
			services.AddSingleton(launcher);

		services.Register(typeof(DiagnostorWorker).Assembly, configuration);
		return services;
	}

	private static ConfigurationRoot CreateConfiguration(string exporters, string attributes = "")
	{
		var logs = exporters == null ? string.Empty : $"<logs {attributes}><exporters>{exporters}</exporters></logs>";
		var xml = $"<options><option path=\"/Diagnostics\"><diagnostor>{logs}</diagnostor></option></options>";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
		return (ConfigurationRoot)new ConfigurationBuilder().AddOptionStream(stream).AddEnvironmentVariables().Build();
	}
	#endregion

	#region 嵌套子类
	private sealed class LogLauncher() : ExporterLauncherBase<OpenTelemetryLoggerOptions>("test")
	{
		public List<LogRecord> Records { get; } = [];
		public int Launches { get; private set; }
		public override void Launch(OpenTelemetryLoggerOptions options, string settings)
		{
			this.Launches++;
			options.AddInMemoryExporter(this.Records);
		}
	}

	private sealed class BatchLauncher() : ExporterLauncherBase<OpenTelemetryLoggerOptions>("batch")
	{
		public RecordingExporter Exporter { get; } = new();
		public override void Launch(OpenTelemetryLoggerOptions options, string settings) => options.AddProcessor(
			new BatchLogRecordExportProcessor(this.Exporter, maxQueueSize: 64, scheduledDelayMilliseconds: 60000, exporterTimeoutMilliseconds: 1000, maxExportBatchSize: 16));
	}

	private sealed class RecordingExporter : BaseExporter<LogRecord>
	{
		public int Count { get; private set; }
		public int Disposals { get; private set; }
		public override ExportResult Export(in Batch<LogRecord> batch)
		{
			foreach(var record in batch)
				this.Count++;
			return ExportResult.Success;
		}
		protected override void Dispose(bool disposing)
		{
			if(disposing)
				this.Disposals++;
			base.Dispose(disposing);
		}
	}

	private sealed class TestEnvironment : IHostEnvironment
	{
		public string ApplicationName { get; set; } = "logging-test-service";
		public string EnvironmentName { get; set; } = "Development";
		public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
		public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
	}
	#endregion
}
