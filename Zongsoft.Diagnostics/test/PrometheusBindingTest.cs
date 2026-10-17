using System;
using System.IO;
using System.Net;
using System.Linq;

using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;

using OpenTelemetry;
using OpenTelemetry.Exporter;

using Zongsoft.Common;
using Zongsoft.Configuration;
using Zongsoft.Configuration.Xml;
using Zongsoft.Text.Templating;
using Zongsoft.Diagnostics.Telemetry;

using Xunit;

namespace Zongsoft.Diagnostics.Tests;

public class PrometheusBindingTest
{
	[Fact]
	public void BundledBindingPreservesUrlsAndSharesTheScrapePath()
	{
		var path = Path.Combine(AppContext.BaseDirectory, "Zongsoft.Diagnostics.option");
		using var stream = File.OpenRead(path);
		var configuration = XmlStreamConfigurationProvider.Read(stream, out var instructions, path);
		var binding = Assert.Single(instructions, instruction => instruction.Name == "zongsoft.tools.binding");
		var attributes = binding.GetAttributes();

		Assert.Equal("prometheus", attributes["name"]);
		Assert.Equal("Diagnostics/Diagnostor/Meters/Exporters/*", attributes["path"]);
		Assert.Equal("driver:prometheus", attributes["filter"]);
		Assert.Equal("http1", attributes["protocols"]);
		Assert.False(attributes.ContainsKey("separators"));

		var settings = configuration["Diagnostics:diagnostor:meters:exporters:prometheus:settings"];
		var connectionSettings = new ConnectionSettings(settings);
		Assert.False(connectionSettings.Contains("protocol"));
		Assert.False(connectionSettings.Contains("protocols"));

		var evaluator = new TemplateEvaluator();
		evaluator.Providers.Add(new Variables { ["this"] = new { Settings = connectionSettings } });
		var urls = evaluator.Evaluate(attributes["template"]);
		var protocols = evaluator.Evaluate(attributes["protocols"]);

		Assert.Equal("http://127.0.0.1:9464/metrics,http://localhost:9464/metrics", urls);
		Assert.Equal("http1", protocols);

		var services = new ServiceCollection();
		services.AddOpenTelemetry().WithMetrics(builder => new PrometheusExporterLauncher().Launch(builder, settings));
		using var provider = services.BuildServiceProvider();
		var options = provider.GetRequiredService<IOptions<PrometheusAspNetCoreOptions>>().Value;
		Assert.Equal("/metrics", options.ScrapeEndpointPath);
		Assert.Equal(urls, evaluator.Evaluate(attributes["template"]));
	}
}
