using System;
using System.Linq;

using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;

using OpenTelemetry;
using OpenTelemetry.Exporter;

using Zongsoft.Diagnostics.Telemetry;

using Xunit;

namespace Zongsoft.Diagnostics.Web.Tests;

public class PrometheusExporterLauncherTest
{
	[Fact]
	public void NullBuilderIsRejected()
	{
		var exception = Assert.Throws<ArgumentNullException>(() => new PrometheusExporterLauncher().Launch(null, "urls=http://localhost:9464/metrics/;protocol=http1"));

		Assert.Equal("builder", exception.ParamName);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("protocol=http1")]
	[InlineData("urls=;protocol=http1")]
	[InlineData("urls='  ';protocol=http1")]
	[InlineData("urls=http://localhost:9464/metrics/;protocol=")]
	[InlineData("urls=http://localhost:9464/metrics/;protocol=' '")]
	public void MissingDeclarationsFailBeforeRegistration(string settings) => RejectBeforeRegistration(settings);

	[Theory]
	[InlineData("path")]
	[InlineData("path=")]
	[InlineData("path=/metrics")]
	[InlineData("PATH='/metrics/'")]
	public void ObsoletePathFailsBeforeRegistration(string obsolete) =>
		RejectBeforeRegistration($"urls=http://localhost:9464/metrics/;protocol=http1;{obsolete}");

	[Theory]
	[InlineData("protocols")]
	[InlineData("protocols=")]
	[InlineData("protocols=' '")]
	[InlineData("protocols=http1")]
	[InlineData("PROTOCOLS=HTTP1")]
	[InlineData("protocol=http1;protocols=http1")]
	public void ObsoleteProtocolsSettingFailsBeforeRegistration(string obsolete) =>
		RejectBeforeRegistration($"urls=http://localhost:9464/metrics/;{obsolete}");

	[Theory]
	[InlineData("//localhost:9464/metrics/")]
	[InlineData("localhost:9464/metrics/")]
	[InlineData("ftp://localhost:9464/metrics/")]
	[InlineData("http://localhost:0/metrics/")]
	[InlineData("http://localhost:65536/metrics/")]
	[InlineData("http://user@localhost:9464/metrics/")]
	[InlineData("http://localhost:9464/metrics/?query=/")]
	[InlineData("http://localhost:9464/metrics/#fragment/")]
	[InlineData("http://localhost:9464/metric%73/")]
	[InlineData("http://localhost:9464/metrics%2Fchild/")]
	[InlineData("http://localhost:9464/指标/")]
	[InlineData("http://localhost:9464/metric name/")]
	[InlineData("http://localhost:9464/metrics+extra/")]
	[InlineData("http://localhost:9464//")]
	[InlineData("http://localhost:9464/metrics//child/")]
	[InlineData("http://localhost:9464/./metrics/")]
	[InlineData("http://localhost:9464/metrics/../")]
	[InlineData("http://localhost:9464/metrics/.")]
	[InlineData("http://localhost:9464/metrics/..")]
	[InlineData(",http://localhost:9464/metrics/")]
	[InlineData("http://localhost:9464/metrics/,")]
	[InlineData("http://localhost:9464/metrics/,,http://localhost:9465/metrics/")]
	[InlineData("http://localhost:9464/metrics/; ;http://localhost:9465/metrics/")]
	[InlineData("http://localhost:9464/metrics/| |http://localhost:9465/metrics/")]
	[InlineData("http://localhost:9464/metrics/|http://localhost:9465/metrics/?query=1")]
	public void InvalidUrlsFailBeforeRegistration(string urls) =>
		RejectBeforeRegistration($"urls='{urls}';protocol=http1");

	[Theory]
	[InlineData("http://localhost:9464/metrics/;https://127.0.0.1:9465/metrics/child/")]
	[InlineData("http://localhost:9464/|http://127.0.0.1:9465/metrics/")]
	public void DifferentLiteralPathsFailBeforeRegistration(string urls) =>
		RejectBeforeRegistration($"urls='{urls}';protocol=http1");

	[Theory]
	[InlineData("http")]
	[InlineData("http2")]
	[InlineData("http3")]
	[InlineData("tcp")]
	[InlineData("udp")]
	[InlineData("grpc")]
	[InlineData("grpc+http1")]
	[InlineData("http1,http1")]
	[InlineData("http1,http2")]
	[InlineData("http2,http1")]
	[InlineData("http1,grpc+http2")]
	[InlineData("http1,")]
	[InlineData(",http1")]
	[InlineData("http1,,http1")]
	[InlineData("http1;http1")]
	[InlineData("unknown")]
	public void UnsupportedProtocolFailsBeforeRegistration(string protocol) =>
		RejectBeforeRegistration($"urls=http://localhost:9464/metrics/;protocol='{protocol}'");

	[Theory]
	[InlineData("http://localhost:9464", "/")]
	[InlineData("http://localhost:9464/", "/")]
	[InlineData("http://localhost:9464/metrics", "/metrics")]
	[InlineData("http://localhost:9464/metrics/", "/metrics")]
	[InlineData("https://localhost:9464/Metrics-v1.2_~/", "/Metrics-v1.2_~")]
	[InlineData("http://metrics.example.invalid:9464/metrics", "/metrics")]
	[InlineData("https://bücher.example.invalid:9464/METRICS/", "/METRICS")]
	[InlineData(" http://localhost:9464/metrics , https://127.0.0.1:9465/METRICS/ ; http://localhost:9466/Metrics | http://127.0.0.1:9467/metrics/ ", "/metrics")]
	public void EquivalentUrlsConfigureOneScrapePath(string urls, string path)
	{
		using var services = Configure($"urls='{urls}'");
		var options = services.GetRequiredService<IOptions<PrometheusAspNetCoreOptions>>().Value;
		Assert.Equal(path, options.ScrapeEndpointPath);
		Assert.False(options.DisableTotalNameSuffixForCounters);
		Assert.Equal(300, options.ScrapeResponseCacheDurationMilliseconds);
	}

	[Theory]
	[InlineData("", false, 300)]
	[InlineData("totalSuffix=true;cacheDuration=1500ms", false, 1500)]
	[InlineData("totalSuffix=false;cacheDuration=2s", true, 2000)]
	[InlineData("totalSuffix=false;cacheDuration=0ms", true, 0)]
	[InlineData("totalSuffix=true;cacheDuration=-1ms", false, 0)]
	[InlineData("cacheDuration=30d", false, int.MaxValue)]
	public void ExporterPreservesScrapeOptions(string settings, bool disableTotalSuffix, int cacheDuration)
	{
		using var services = Configure($"urls=http://localhost:9464/metrics/v2/;protocol=http1;{settings}");
		var aspnet = services.GetRequiredService<IOptions<PrometheusAspNetCoreOptions>>().Value;

		Assert.Equal("/metrics/v2", aspnet.ScrapeEndpointPath);
		Assert.Equal(disableTotalSuffix, aspnet.DisableTotalNameSuffixForCounters);
		Assert.Equal(cacheDuration, aspnet.ScrapeResponseCacheDurationMilliseconds);
	}

	private static ServiceProvider Configure(string settings)
	{
		var services = new ServiceCollection();
		services.AddOpenTelemetry().WithMetrics(builder => new PrometheusExporterLauncher().Launch(builder, settings));
		return services.BuildServiceProvider();
	}

	private static void RejectBeforeRegistration(string settings)
	{
		var services = new ServiceCollection();
		services.AddOpenTelemetry().WithMetrics(builder =>
		{
			var registrations = services.ToArray();
			var exception = Assert.Throws<ArgumentException>(() => new PrometheusExporterLauncher().Launch(builder, settings));

			Assert.Equal("settings", exception.ParamName);
			Assert.Equal(registrations, services);
		});
	}
}
