using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Text.Json;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Diagnostics.Metrics;
using System.Collections.Generic;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

using Zongsoft.Services;
using Zongsoft.Configuration;
using Zongsoft.Diagnostics.Telemetry;

using Xunit;

namespace Zongsoft.Diagnostics.Tests;

[Collection("Diagnostics")]
public class DiagnostorWorkerTest
{
	[Fact]
	public void CommonLibraryRunsAndRegistersWithoutAspNetCore()
	{
		using var runtime = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Zongsoft.Diagnostics.Tests.runtimeconfig.json")));
		var options = runtime.RootElement.GetProperty("runtimeOptions");
		var frameworks = options.TryGetProperty("frameworks", out var values) ? values.EnumerateArray().ToArray() : [options.GetProperty("framework")];
		Assert.Equal("Microsoft.NETCore.App", Assert.Single(frameworks).GetProperty("name").GetString());

		var services = new ServiceCollection();
		services.Register(typeof(DiagnostorWorker).Assembly, new ConfigurationBuilder().Build());
		using var provider = (IDisposable)new ServiceProviderFactory().CreateServiceProvider(services);

		foreach(var name in new[] { null, "", "telemetry", "OpenTelemetry", "opentelemetry", "OPENTELEMETRY" })
		{
			Assert.Same(ExporterLauncher.Default.Meters, ((IServiceProvider)provider).Find<IExporterLauncher<MeterProviderBuilder>>(name));
			Assert.Same(ExporterLauncher.Default.Tracers, ((IServiceProvider)provider).Find<IExporterLauncher<TracerProviderBuilder>>(name));
			Assert.Same(ExporterLauncher.Default.Logs, ((IServiceProvider)provider).Find<IExporterLauncher<OpenTelemetryLoggerOptions>>(name));
		}

		foreach(var name in new[] { "Console", "console", "opentelemetry.console", "OPENTELEMETRY.CONSOLE" })
		{
			Assert.Same(ExporterLauncher.Console.Meters, ((IServiceProvider)provider).Find<IExporterLauncher<MeterProviderBuilder>>(name));
			Assert.Same(ExporterLauncher.Console.Tracers, ((IServiceProvider)provider).Find<IExporterLauncher<TracerProviderBuilder>>(name));
			Assert.Same(ExporterLauncher.Console.Logs, ((IServiceProvider)provider).Find<IExporterLauncher<OpenTelemetryLoggerOptions>>(name));
		}

		Assert.Null(((IServiceProvider)provider).Find<IExporterLauncher<MeterProviderBuilder>>("prometheus"));
		Assert.DoesNotContain(typeof(DiagnostorWorker).Assembly.GetReferencedAssemblies(), assembly => assembly.Name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
	}

	[Fact]
	public async Task WorkerExportsBothSignalsAndSharesApplicationIdentity()
	{
		using var environment = new ResourceEnvironment();
		var metrics = new MetricsLauncher();
		var traces = new TracesLauncher();
		using var context = CreateContext(metrics, traces);
		await using var worker = new DiagnostorWorker("test", new TestConfigurator());
		await worker.StartAsync();

		using var meter = new Meter("diagnostics-test");
		meter.CreateCounter<long>("test.counter").Add(37);
		Assert.True(metrics.Provider.ForceFlush());
		Assert.Contains(metrics.Exported, metric => metric.Name == "test.counter");

		using(var source = new ActivitySource("diagnostics-test"))
		using(var activity = source.StartActivity("test.operation"))
			Assert.NotNull(activity);

		Assert.Single(traces.Exported);

		var identity = metrics.Provider.GetResource().Attributes.ToDictionary(item => item.Key, item => item.Value);
		var traceIdentity = traces.Provider.GetResource().Attributes.ToDictionary(item => item.Key, item => item.Value);
		Assert.Equal("diagnostics-test-service", identity["service.name"]);
		Assert.False(string.IsNullOrWhiteSpace((string)identity["service.instance.id"]));
		Assert.Equal(identity["service.instance.id"], traceIdentity["service.instance.id"]);
		Assert.Equal(identity["service.name"], traceIdentity["service.name"]);

		await worker.StopAsync();
		Assert.Equal(1, metrics.Disposed);
		Assert.Equal(1, traces.Disposed);
		await worker.StartAsync();
		await worker.DisposeAsync();
		Assert.Equal(2, metrics.Disposed);
		Assert.Equal(2, traces.Disposed);
	}

	[Fact]
	public async Task ResourceEnvironmentOverridesApplicationDefaults()
	{
		using var environment = new ResourceEnvironment("configured-service", "service.namespace=test,service.instance.id=instance-one");
		var metrics = new MetricsLauncher();
		using var context = CreateContext(metrics, new TracesLauncher());
		await using var worker = new DiagnostorWorker("test", new TestConfigurator());
		await worker.StartAsync();
		var resource = metrics.Provider.GetResource().Attributes.ToDictionary(item => item.Key, item => item.Value);
		Assert.Equal("configured-service", resource["service.name"]);
		Assert.Equal("test", resource["service.namespace"]);
		Assert.Equal("instance-one", resource["service.instance.id"]);
	}

	[Theory]
	[InlineData("meters")]
	[InlineData("traces")]
	public async Task MissingExporterFailsBeforeEitherProviderStarts(string signal)
	{
		var metrics = new MetricsLauncher();
		var traces = new TracesLauncher();
		using var context = CreateContext(metrics, traces);
		await using var worker = new DiagnostorWorker("test", new TestConfigurator(signal));
		var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => worker.StartAsync());
		Assert.Equal("missing", exception.Data["ExporterDriver"]);
		Assert.Equal(signal, exception.Data["Signal"]);
		Assert.Null(metrics.Provider);
		Assert.Null(traces.Provider);
	}

	[Fact]
	public async Task FailedStartupReleasesBuiltProviderAndCanRetry()
	{
		var metrics = new MetricsLauncher();
		var traces = new TracesLauncher { Fail = true };
		using var context = CreateContext(metrics, traces);
		await using var worker = new DiagnostorWorker("test", new TestConfigurator());
		await Assert.ThrowsAsync<InvalidOperationException>(() => worker.StartAsync());
		Assert.Equal(1, metrics.Disposed);
		Assert.Equal(0, traces.Disposed);

		traces.Fail = false;
		await worker.StartAsync();
		await worker.DisposeAsync();
		Assert.Equal(2, metrics.Disposed);
		Assert.Equal(1, traces.Disposed);
	}

	[Theory]
	[InlineData(null, true)]
	[InlineData("true", true)]
	[InlineData("false", false)]
	public async Task ConfiguredExportersRespectEnabled(string enabled, bool expected)
	{
		var attribute = enabled == null ? string.Empty : $"enabled=\"{enabled}\"";
		using var configuration = CreateConfiguration($"<exporter exporter.name=\"test\" driver=\"test\" {attribute} />");
		var metrics = new MetricsLauncher();
		var traces = new TracesLauncher();
		using var context = CreateContext(metrics, traces, configuration);
		await using var worker = new DiagnostorWorker("test", new Configuration.Configurator("/Diagnostics/Diagnostor"));
		await worker.StartAsync();

		if(expected)
		{
			Assert.NotNull(metrics.Provider);
			Assert.NotNull(traces.Provider);
		}
		else
		{
			Assert.Null(metrics.Provider);
			Assert.Null(traces.Provider);
		}

		await worker.StopAsync();
		Assert.Equal(expected ? 1 : 0, metrics.Disposed);
		Assert.Equal(expected ? 1 : 0, traces.Disposed);
	}

	[Fact]
	public void ConfigurationFiltersDisabledExportersAndPreservesEnabledSettings()
	{
		using var configuration = CreateConfiguration("""
			<exporter exporter.name="legacy" driver="first" settings="key=one;" />
			<exporter exporter.name="disabled" driver="missing" enabled="false" settings="invalid settings" />
			<exporter exporter.name="enabled" driver="second" enabled="true" settings="key=two;" />
			""");
		using var context = CreateContext(new MetricsLauncher(), new TracesLauncher(), configuration);
		var diagnostor = new Diagnostor("test", new Configuration.Configurator("/Diagnostics/Diagnostor"));

		foreach(var filtering in new[] { diagnostor.Meters, diagnostor.Traces })
		{
			Assert.Equal("diagnostics-test", Assert.Single(filtering.Filters));
			Assert.Equal(2, filtering.Exporters.Count);
			Assert.Contains(filtering.Exporters, exporter => exporter.Driver == "first" && exporter.Settings == "key=one;");
			Assert.Contains(filtering.Exporters, exporter => exporter.Driver == "second" && exporter.Settings == "key=two;");
		}
	}

	[Fact]
	public async Task DisabledMissingExportersDoNotRequireRegisteredDrivers()
	{
		using var configuration = CreateConfiguration("""
			<exporter exporter.name="missing" driver="missing" enabled="false" settings="invalid settings" />
			""");
		var metrics = new MetricsLauncher();
		var traces = new TracesLauncher();
		using var context = CreateContext(metrics, traces, configuration);
		await using var worker = new DiagnostorWorker("test", new Configuration.Configurator("/Diagnostics/Diagnostor"));
		await worker.StartAsync();
		Assert.Null(metrics.Provider);
		Assert.Null(traces.Provider);
	}

	private static TestContext CreateContext(MetricsLauncher metrics, TracesLauncher traces, IConfiguration configuration = null)
	{
		var services = new ServiceCollection();
		if(configuration != null)
			services.AddSingleton<IConfiguration>(configuration);
		services.AddSingleton<IExporterLauncher<MeterProviderBuilder>>(metrics);
		services.AddSingleton<IExporterLauncher<TracerProviderBuilder>>(traces);
		return new TestContext(new ServiceProviderFactory().CreateServiceProvider(services));
	}

	private static ConfigurationRoot CreateConfiguration(string exporters)
	{
		var xml = $"""
			<options>
				<option path="/Diagnostics">
					<diagnostor>
						<meters>
							<filter>diagnostics-test</filter>
							<exporters>{exporters}</exporters>
						</meters>
						<traces>
							<filter>diagnostics-test</filter>
							<exporters>{exporters}</exporters>
						</traces>
					</diagnostor>
				</option>
			</options>
			""";
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
		return (ConfigurationRoot)new ConfigurationBuilder().AddOptionStream(stream).Build();
	}

	private sealed class TestContext(IServiceProvider services) : ApplicationContext(services)
	{
		protected override string GetName() => "diagnostics-test-service";
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			((IDisposable)this.Services).Dispose();
		}
	}

	private sealed class TestConfigurator(string missing = null) : Diagnostor.Configurator
	{
		public override void Configure(Diagnostor diagnostor)
		{
			diagnostor.Meters = new(["diagnostics-test"], new[] { new Diagnostor.Filtering.Exporter(missing == "meters" ? "missing" : "test", "") });
			diagnostor.Traces = new(["diagnostics-test"], new[] { new Diagnostor.Filtering.Exporter(missing == "traces" ? "missing" : "test", "") });
		}
	}

	private sealed class MetricsLauncher() : ExporterLauncherBase<MeterProviderBuilder>("test")
	{
		public readonly List<Metric> Exported = [];
		public OpenTelemetry.Metrics.MeterProvider Provider { get; private set; }
		public int Disposed { get; private set; }
		public override void Launch(MeterProviderBuilder builder, string settings)
		{
			builder.AddInMemoryExporter(this.Exported);
			builder.AddInstrumentation((_, provider) =>
			{
				this.Provider = provider;
				return new Cleanup(() => this.Disposed++);
			});
		}
	}

	private sealed class TracesLauncher() : ExporterLauncherBase<TracerProviderBuilder>("test")
	{
		public readonly List<Activity> Exported = [];
		public TracerProvider Provider { get; private set; }
		public int Disposed { get; private set; }
		public bool Fail { get; set; }
		public override void Launch(TracerProviderBuilder builder, string settings)
		{
			if(this.Fail)
				throw new InvalidOperationException();
			builder.AddInMemoryExporter(this.Exported);
			builder.AddInstrumentation((_, provider) =>
			{
				this.Provider = provider;
				return new Cleanup(() => this.Disposed++);
			});
		}
	}

	private sealed class Cleanup(Action dispose) : IDisposable
	{
		public void Dispose() => dispose();
	}

	private sealed class ResourceEnvironment : IDisposable
	{
		private readonly string _name = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME");
		private readonly string _attributes = Environment.GetEnvironmentVariable("OTEL_RESOURCE_ATTRIBUTES");
		public ResourceEnvironment(string name = null, string attributes = null)
		{
			Environment.SetEnvironmentVariable("OTEL_SERVICE_NAME", name);
			Environment.SetEnvironmentVariable("OTEL_RESOURCE_ATTRIBUTES", attributes);
		}
		public void Dispose()
		{
			Environment.SetEnvironmentVariable("OTEL_SERVICE_NAME", _name);
			Environment.SetEnvironmentVariable("OTEL_RESOURCE_ATTRIBUTES", _attributes);
		}
	}
}
