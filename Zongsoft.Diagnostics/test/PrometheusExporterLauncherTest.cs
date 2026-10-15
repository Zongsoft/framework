using System;
using System.Net;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics.Metrics;

using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;

using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Exporter;

using Zongsoft.Diagnostics.Telemetry;

using Xunit;

namespace Zongsoft.Diagnostics.Tests;

public class PrometheusExporterLauncherTest(ITestOutputHelper output)
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
	[InlineData("http://localhost:9464/metrics/,http://127.0.0.1:9465/Metrics/")]
	[InlineData("http://localhost:9464/metrics/;https://127.0.0.1:9465/metrics/child/")]
	[InlineData("http://localhost:9464/|http://127.0.0.1:9465/metrics/")]
	[InlineData("http://localhost:9464/metrics,http://127.0.0.1:9465/metrics/")]
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

	[Fact]
	public void OmittedProtocolConfiguresBothExportersAndListenerPrefixes()
	{
		using var services = Configure("urls=http://127.0.0.1:9464/metrics,http://localhost:9465/metrics");
		var aspnet = services.GetRequiredService<IOptions<PrometheusAspNetCoreOptions>>().Value;
		var options = services.GetRequiredService<IOptions<PrometheusHttpListenerOptions>>().Value;
		using var listener = new HttpListener();
		listener.Prefixes.Add("http://localhost:9464/");

		Assert.Equal("/metrics", aspnet.ScrapeEndpointPath);
		Assert.Equal("/metrics", options.ScrapeEndpointPath);
		Assert.False(aspnet.DisableTotalNameSuffixForCounters);
		Assert.False(options.DisableTotalNameSuffixForCounters);
		Assert.Equal(300, aspnet.ScrapeResponseCacheDurationMilliseconds);
		Assert.Equal(300, options.ScrapeResponseCacheDurationMilliseconds);

		Assert.NotNull(options.ConfigureHttpListener);
		options.ConfigureHttpListener(options, listener);

		Assert.Equal(new[] { "http://127.0.0.1:9464/metrics/", "http://localhost:9465/metrics/" }, listener.Prefixes.Order(StringComparer.Ordinal));
		Assert.False(listener.IsListening);
	}

	[Theory]
	[InlineData("http://localhost:9464", "http1", "/", new[] { "http://localhost:9464/" })]
	[InlineData("http://localhost:9464/", "http1", "/", new[] { "http://localhost:9464/" })]
	[InlineData("http://localhost:9464/metrics", "http1", "/metrics", new[] { "http://localhost:9464/metrics/" })]
	[InlineData(" https://LocalHost:9464/Metrics-v1.2_~/ ", "HTTP1", "/Metrics-v1.2_~/", new[] { "https://LocalHost:9464/Metrics-v1.2_~/" })]
	[InlineData(" https://LocalHost:9464/Metrics-v1.2_~ ", "HTTP1", "/Metrics-v1.2_~", new[] { "https://LocalHost:9464/Metrics-v1.2_~/" })]
	[InlineData(" http://localhost:9464/metrics/ , https://127.0.0.1:9465/metrics/ ; http://localhost:9466/metrics/ | http://127.0.0.1:9467/metrics/ ", " HTTP1 ", "/metrics/", new[] { "http://localhost:9464/metrics/", "https://127.0.0.1:9465/metrics/", "http://localhost:9466/metrics/", "http://127.0.0.1:9467/metrics/" })]
	[InlineData(" http://localhost:9464/metrics , https://127.0.0.1:9465/metrics ; http://localhost:9466/metrics | http://127.0.0.1:9467/metrics ", " HTTP1 ", "/metrics", new[] { "http://localhost:9464/metrics/", "https://127.0.0.1:9465/metrics/", "http://localhost:9466/metrics/", "http://127.0.0.1:9467/metrics/" })]
	public void CompleteUrlsPreserveScrapePathAndOnlyCompleteListenerPrefixes(string urls, string protocol, string path, string[] prefixes)
	{
		using var services = Configure($"urls='{urls}';protocol='{protocol}'");
		var options = services.GetRequiredService<IOptions<PrometheusHttpListenerOptions>>().Value;
		using var listener = new HttpListener();
		listener.Prefixes.Add("http://localhost:9464/");

		Assert.NotNull(options.ConfigureHttpListener);
		options.ConfigureHttpListener(options, listener);

		Assert.Equal(prefixes.Order(StringComparer.Ordinal), listener.Prefixes.Order(StringComparer.Ordinal));
		Assert.Equal(path, options.ScrapeEndpointPath);
		Assert.Equal(path, services.GetRequiredService<IOptions<PrometheusAspNetCoreOptions>>().Value.ScrapeEndpointPath);
		Assert.False(listener.IsListening);
	}

	[Theory]
	[InlineData("", false, 300)]
	[InlineData("totalSuffix=true;cacheDuration=1500ms", false, 1500)]
	[InlineData("totalSuffix=false;cacheDuration=2s", true, 2000)]
	[InlineData("totalSuffix=false;cacheDuration=0ms", true, 0)]
	[InlineData("totalSuffix=true;cacheDuration=-1ms", false, 0)]
	[InlineData("cacheDuration=30d", false, int.MaxValue)]
	public void BothExportersShareScrapeOptions(string settings, bool disableTotalSuffix, int cacheDuration)
	{
		using var services = Configure($"urls=http://localhost:9464/metrics/v2/;protocol=http1;{settings}");
		var aspnet = services.GetRequiredService<IOptions<PrometheusAspNetCoreOptions>>().Value;
		var listener = services.GetRequiredService<IOptions<PrometheusHttpListenerOptions>>().Value;

		Assert.Equal("/metrics/v2/", aspnet.ScrapeEndpointPath);
		Assert.Equal("/metrics/v2/", listener.ScrapeEndpointPath);
		Assert.Equal(disableTotalSuffix, aspnet.DisableTotalNameSuffixForCounters);
		Assert.Equal(disableTotalSuffix, listener.DisableTotalNameSuffixForCounters);
		Assert.Equal(cacheDuration, aspnet.ScrapeResponseCacheDurationMilliseconds);
		Assert.Equal(cacheDuration, listener.ScrapeResponseCacheDurationMilliseconds);
	}

	[Theory]
	[InlineData("/metrics")]
	[InlineData("/metrics/")]
	[Trait("Category", "Integration")]
	public async Task LoopbackListenerScrapesDeclaredPrefixAndStopsOnDisposeAsync(string path)
	{
		using var reservation = new TcpListener(IPAddress.Loopback, 0);
		reservation.Start();
		var port = ((IPEndPoint)reservation.LocalEndpoint).Port;
		reservation.Stop();

		var url = $"http://127.0.0.1:{port}{path}";
		var prefix = $"http://127.0.0.1:{port}/metrics/";
		using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(15));
		using var meter = new Meter($"Zongsoft.Diagnostics.Tests.{Guid.NewGuid():N}");
		var counter = meter.CreateCounter<long>("zongsoft_test_requests");
		var builder = Sdk.CreateMeterProviderBuilder().AddMeter(meter.Name);
		new PrometheusExporterLauncher().Launch(builder, $"urls={url};cacheDuration=0ms");

		using(var provider = builder.Build())
		using(var client = new HttpClient(new HttpClientHandler { UseProxy = false, AllowAutoRedirect = false }))
		{
			counter.Add(7);
			using var response = await client.GetAsync(prefix, cancellation.Token);
			var content = await response.Content.ReadAsStringAsync(cancellation.Token);

			Assert.Equal(HttpStatusCode.OK, response.StatusCode);
			Assert.Contains("# TYPE zongsoft_test_requests_total counter", content, StringComparison.Ordinal);
			Assert.Matches(@"(?m)^zongsoft_test_requests_total(?:\{[^\r\n]*\})? 7\r?$", content);

			counter.Add(5);
			using var queried = await client.GetAsync($"{prefix}?source=test", cancellation.Token);
			var queriedContent = await queried.Content.ReadAsStringAsync(cancellation.Token);

			Assert.Equal(HttpStatusCode.OK, queried.StatusCode);
			Assert.Matches(@"(?m)^zongsoft_test_requests_total(?:\{[^\r\n]*\})? 12\r?$", queriedContent);

			using var child = await client.GetAsync($"{prefix}child/", cancellation.Token);
			var childContent = await child.Content.ReadAsStringAsync(cancellation.Token);

			Assert.Equal(HttpStatusCode.OK, child.StatusCode);
			Assert.Matches(@"(?m)^zongsoft_test_requests_total(?:\{[^\r\n]*\})? 12\r?$", childContent);

			//HTTP.sys 接纳此前缀的无尾斜杠别名；不将这一 Windows 行为约定到其他平台。
			if(OperatingSystem.IsWindows())
			{
				using var unsuffixed = await client.GetAsync(prefix.TrimEnd('/'), cancellation.Token);
				var unsuffixedContent = await unsuffixed.Content.ReadAsStringAsync(cancellation.Token);

				Assert.Equal(HttpStatusCode.OK, unsuffixed.StatusCode);
				Assert.Matches(@"(?m)^zongsoft_test_requests_total(?:\{[^\r\n]*\})? 12\r?$", unsuffixedContent);
			}

			foreach(var unrelatedPath in new[] { "/", "/metrics-neighbor/" })
			{
				using var unrelated = await client.GetAsync($"http://127.0.0.1:{port}{unrelatedPath}", cancellation.Token);
				var unrelatedContent = await unrelated.Content.ReadAsStringAsync(cancellation.Token);
				output.WriteLine($"{unrelatedPath}: {(int)unrelated.StatusCode} {unrelated.StatusCode}");

				Assert.Equal(HttpStatusCode.NotFound, unrelated.StatusCode);
				Assert.DoesNotContain("zongsoft_test_requests_total", unrelatedContent, StringComparison.Ordinal);
			}
		}

		//同一完整前缀可重新绑定，证明提供程序已释放原来的监听器。
		using var replacement = new HttpListener();
		replacement.Prefixes.Add(prefix);
		replacement.Start();

		Assert.True(replacement.IsListening);
		replacement.Stop();
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
