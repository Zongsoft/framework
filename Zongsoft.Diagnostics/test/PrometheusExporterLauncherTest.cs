using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Text;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics.Metrics;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
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

	[Theory]
	[InlineData("")]
	[InlineData("/")]
	[InlineData("/metrics")]
	[InlineData("/metrics/")]
	[InlineData("/MeTrIcS/")]
	[InlineData("/telemetry/metrics")]
	[InlineData("/telemetry/metrics/")]
	[Trait("Category", "Integration")]
	public async Task LoopbackListenerScrapesDeclaredPrefixAndStopsOnDisposeAsync(string path)
	{
		using var reservation = new TcpListener(IPAddress.Loopback, 0);
		reservation.Start();
		var port = ((IPEndPoint)reservation.LocalEndpoint).Port;
		reservation.Stop();

		var url = $"http://127.0.0.1:{port}{path}";
		var prefix = $"http://127.0.0.1:{port}{path.TrimEnd('/')}/";
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

			if(path.Length > 1)
			{
				using var unsuffixed = await client.GetAsync(prefix.TrimEnd('/'), cancellation.Token);
				var unsuffixedContent = await unsuffixed.Content.ReadAsStringAsync(cancellation.Token);

				Assert.Equal(HttpStatusCode.OK, unsuffixed.StatusCode);
				Assert.Matches(@"(?m)^zongsoft_test_requests_total(?:\{[^\r\n]*\})? 12\r?$", unsuffixedContent);

				using var mixedCase = await client.GetAsync(prefix.ToUpperInvariant(), cancellation.Token);
				Assert.Equal(HttpStatusCode.OK, mixedCase.StatusCode);
				Assert.Matches(@"(?m)^zongsoft_test_requests_total(?:\{[^\r\n]*\})? 12\r?$", await mixedCase.Content.ReadAsStringAsync(cancellation.Token));

				foreach(var requestPath in new[] { "/", $"{path.TrimEnd('/')}-neighbor/", $"{path.TrimEnd('/')}2/" })
				{
					using var other = await client.GetAsync($"http://127.0.0.1:{port}{requestPath}", cancellation.Token);
					var otherContent = await other.Content.ReadAsStringAsync(cancellation.Token);
					output.WriteLine($"{requestPath}: {(int)other.StatusCode} {other.StatusCode}");

					Assert.Equal(HttpStatusCode.NotFound, other.StatusCode);
					Assert.DoesNotContain("zongsoft_test_requests_total", otherContent, StringComparison.Ordinal);
				}
			}
		}

		//提供程序释放后，同一端口可以重新建立独立监听。
		using var replacement = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		replacement.Bind(new IPEndPoint(IPAddress.Loopback, port));
		Assert.Equal(port, ((IPEndPoint)replacement.LocalEndPoint).Port);
	}

	[Theory]
	[InlineData("/METRICS", 200)]
	[InlineData("/Metrics/?q=/elsewhere", 200)]
	[InlineData("/metric%73/", 404)]
	[InlineData("/metrics%2Fchild/", 404)]
	[InlineData("/elsewhere/../metrics/", 404)]
	[InlineData("/metrics/../elsewhere", 404)]
	[InlineData("/metrics/child%20name", 200)]
	[InlineData("http://localhost/MeTrIcS?x=1", 200)]
	[Trait("Category", "Integration")]
	public async Task RawRequestPathControlsTheScrapeRange(string target, int expected)
	{
		using var reservation = new TcpListener(IPAddress.Loopback, 0);
		reservation.Start();
		var port = ((IPEndPoint)reservation.LocalEndpoint).Port;
		reservation.Stop();
		var builder = Sdk.CreateMeterProviderBuilder();
		new PrometheusExporterLauncher().Launch(builder, $"urls=http://127.0.0.1:{port}/metrics/");
		using var provider = builder.Build();
		using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
		using var client = new TcpClient();
		await client.ConnectAsync(IPAddress.Loopback, port, cancellation.Token);
		using var stream = client.GetStream();
		await stream.WriteAsync(Encoding.ASCII.GetBytes($"GET {target} HTTP/1.1\r\nHost: localhost\r\nConnection: close\r\n\r\n"), cancellation.Token);
		using var reader = new StreamReader(stream);
		Assert.StartsWith($"HTTP/1.1 {expected} ", await reader.ReadLineAsync(cancellation.Token), StringComparison.Ordinal);
	}

	[Fact]
	[Trait("Category", "Integration")]
	public async Task EquivalentAddressesShareTheListenerAndStartFailureReleasesPorts()
	{
		using var reservation = new TcpListener(IPAddress.Loopback, 0);
		reservation.Start();
		var port = ((IPEndPoint)reservation.LocalEndpoint).Port;
		reservation.Stop();
		var builder = Sdk.CreateMeterProviderBuilder();
		new PrometheusExporterLauncher().Launch(builder, $"urls=http://127.0.0.1:{port}/metrics,http://localhost:{port}/METRICS/");

		using(var provider = builder.Build())
		using(var client = new HttpClient(new HttpClientHandler { UseProxy = false }) { Timeout = TimeSpan.FromSeconds(10) })
		{
			using var ipv4 = await client.GetAsync($"http://127.0.0.1:{port}/Metrics");
			Assert.Equal(HttpStatusCode.OK, ipv4.StatusCode);

			if(Socket.OSSupportsIPv6)
			{
				using var ipv6 = await client.GetAsync($"http://[::1]:{port}/mETRICS/");
				Assert.Equal(HttpStatusCode.OK, ipv6.StatusCode);
			}
		}

		using var occupied = new TcpListener(IPAddress.Loopback, 0);
		occupied.Start();
		var blocked = ((IPEndPoint)occupied.LocalEndpoint).Port;
		var invalid = Sdk.CreateMeterProviderBuilder();
		new PrometheusExporterLauncher().Launch(invalid, $"urls=http://127.0.0.1:{port}/metrics,http://127.0.0.1:{blocked}/metrics");
		Assert.Throws<IOException>(() => invalid.Build());
		using var replacement = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		replacement.Bind(new IPEndPoint(IPAddress.Loopback, port));
		Assert.Equal(port, ((IPEndPoint)replacement.LocalEndPoint).Port);
	}

	[Theory]
	[InlineData("127.0.0.1")]
	[InlineData("metrics.example.invalid")]
	[Trait("Category", "Integration")]
	public async Task HttpsUsesConfiguredCertificateWithoutImportingHostEndpoints(string host)
	{
		using var reservation = new TcpListener(IPAddress.Loopback, 0);
		reservation.Start();
		var port = ((IPEndPoint)reservation.LocalEndpoint).Port;
		reservation.Stop();
		using var occupied = new TcpListener(IPAddress.Loopback, 0);
		occupied.Start();
		using var key = RSA.Create(2048);
		var request = new CertificateRequest("CN=localhost", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
		using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddHours(1));
		var file = Path.Combine(Path.GetTempPath(), $"zongsoft-prometheus-{Guid.NewGuid():N}.pfx");
		File.WriteAllBytes(file, certificate.Export(X509ContentType.Pfx));

		try
		{
			using var configuration = (ConfigurationRoot)new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
			{
				["Kestrel:Certificates:Default:Path"] = file,
				["Kestrel:Endpoints:unrelated:Url"] = $"http://127.0.0.1:{((IPEndPoint)occupied.LocalEndpoint).Port}",
			}).Build();
			var builder = Sdk.CreateMeterProviderBuilder().ConfigureServices(services => services.AddSingleton<IConfiguration>(configuration));
			new PrometheusExporterLauncher().Launch(builder, $"urls=https://{host}:{port}/metrics/");
			using var provider = builder.Build();
			using var client = CreateClient(IPAddress.Loopback, certificate);
			using var response = await client.GetAsync($"https://{host}:{port}/METRICS");
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);

			if(host != "127.0.0.1")
			{
				using var other = await client.GetAsync($"https://other.example.invalid:{port}/metrics");
				Assert.Equal(HttpStatusCode.NotFound, other.StatusCode);
			}
		}
		finally
		{
			File.Delete(file);
		}
	}

	[Fact]
	[Trait("Category", "Integration")]
	public async Task DomainListenersMatchHostsAcrossPortsAndAddressFamiliesAsync()
	{
		using var first = new TcpListener(IPAddress.Loopback, 0);
		using var second = new TcpListener(IPAddress.Loopback, 0);
		first.Start();
		second.Start();
		var port = ((IPEndPoint)first.LocalEndpoint).Port;
		var otherPort = ((IPEndPoint)second.LocalEndpoint).Port;
		first.Stop();
		second.Stop();
		using var meter = new Meter($"Zongsoft.Diagnostics.Tests.{Guid.NewGuid():N}");
		var counter = meter.CreateCounter<long>("zongsoft_domain_requests");
		var builder = Sdk.CreateMeterProviderBuilder().AddMeter(meter.Name);
		new PrometheusExporterLauncher().Launch(builder,
			$"urls=http://metrics.example.invalid.:{port}/MeTrIcS/,http://bücher.example.invalid:{port}/metrics,http://secondary.example.invalid:{otherPort}/METRICS;cacheDuration=0ms");
		using var provider = builder.Build();
		counter.Add(9);

		foreach(var address in Socket.OSSupportsIPv6 ? new[] { IPAddress.Loopback, IPAddress.IPv6Loopback } : [IPAddress.Loopback])
		{
			using var client = CreateClient(address);

			foreach(var (host, targetPort) in new[] { ("METRICS.EXAMPLE.INVALID.", port), ("xn--bcher-kva.example.invalid", port), ("XN--BCHER-KVA.EXAMPLE.INVALID.", port), ("secondary.example.invalid", otherPort) })
			{
				using var request = new HttpRequestMessage(HttpMethod.Get, $"http://{host}:{targetPort}/METRICS/child?source=test");
				request.Headers.Host = $"{host}:{targetPort}";
				using var response = await client.SendAsync(request);
				var content = await response.Content.ReadAsStringAsync();

				Assert.True(response.StatusCode == HttpStatusCode.OK, $"{host}:{targetPort} via {address}: {response.StatusCode}");
				Assert.Matches(@"(?m)^zongsoft_domain_requests_total(?:\{[^\r\n]*\})? 9\r?$", content);
			}

			foreach(var (host, targetPort, requestPath) in new[]
			{
				("other.example.invalid", port, "/metrics"),
				("metrics.example.invalid.evil.invalid", port, "/metrics"),
				("xn--bcher-kva.example.invalid.evil.invalid", port, "/metrics"),
				("secondary.example.invalid", port, "/metrics"),
				("metrics.example.invalid", otherPort, "/metrics"),
				("metrics.example.invalid", port, "/metrics-neighbor"),
			})
			{
				using var response = await client.GetAsync($"http://{host}:{targetPort}{requestPath}");
				var content = await response.Content.ReadAsStringAsync();

				Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
				Assert.DoesNotContain("zongsoft_domain_requests_total", content, StringComparison.Ordinal);
			}
		}

		provider.Dispose();

		foreach(var address in Socket.OSSupportsIPv6 ? new[] { IPAddress.Loopback, IPAddress.IPv6Loopback } : [IPAddress.Loopback])
		{
			foreach(var releasedPort in new[] { port, otherPort })
			{
				using var replacement = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
				replacement.Bind(new IPEndPoint(address, releasedPort));

				Assert.Equal(releasedPort, ((IPEndPoint)replacement.LocalEndPoint).Port);
			}
		}
	}

	[Theory]
	[InlineData("127.0.0.1", true, false, false)]
	[InlineData("0.0.0.0", true, true, false)]
	[InlineData("*", true, true, false)]
	[InlineData("localhost", true, false, true)]
	[InlineData("[::1]", false, false, true)]
	[InlineData("[::]", false, false, true)]
	[Trait("Category", "Integration")]
	public async Task MixedDomainAndIpDeclarationsPreserveInterfaceScopesAsync(string host, bool ipv4Allowed, bool otherIpv4Allowed, bool ipv6Allowed)
	{
		using var reservation = new TcpListener(IPAddress.Loopback, 0);
		reservation.Start();
		var port = ((IPEndPoint)reservation.LocalEndpoint).Port;
		reservation.Stop();
		var builder = Sdk.CreateMeterProviderBuilder();
		new PrometheusExporterLauncher().Launch(builder, $"urls=http://metrics.example.invalid:{port}/metrics,http://{host}:{port}/METRICS/");
		using var provider = builder.Build();

		foreach(var (address, allowed) in new[] { (IPAddress.Loopback, ipv4Allowed), (IPAddress.Parse("127.0.0.2"), otherIpv4Allowed), (IPAddress.IPv6Loopback, ipv6Allowed) })
		{
			using var client = CreateClient(address);
			using var declared = await client.GetAsync($"http://metrics.example.invalid:{port}/Metrics");

			Assert.Equal(HttpStatusCode.OK, declared.StatusCode);

			using var unrelated = await client.GetAsync($"http://other.example.invalid:{port}/metrics/");

			Assert.Equal(allowed ? HttpStatusCode.OK : HttpStatusCode.NotFound, unrelated.StatusCode);
		}
	}

	[Fact]
	[Trait("Category", "Integration")]
	public async Task Ipv6WildcardDoesNotOpenIpv4Async()
	{
		using var reservation = new TcpListener(IPAddress.IPv6Loopback, 0);
		reservation.Start();
		var port = ((IPEndPoint)reservation.LocalEndpoint).Port;
		reservation.Stop();
		var builder = Sdk.CreateMeterProviderBuilder();
		new PrometheusExporterLauncher().Launch(builder, $"urls=http://[::]:{port}/metrics");
		using var provider = builder.Build();
		using var ipv6 = CreateClient(IPAddress.IPv6Loopback);
		using var response = await ipv6.GetAsync($"http://localhost:{port}/metrics");

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);

		using var ipv4 = CreateClient(IPAddress.Loopback);
		await Assert.ThrowsAsync<HttpRequestException>(() => ipv4.GetAsync($"http://localhost:{port}/metrics"));
	}

	[Fact]
	[Trait("Category", "Integration")]
	public void DomainStartupFailureReleasesAllListeners()
	{
		using var reservation = new TcpListener(IPAddress.Loopback, 0);
		using var occupied = new TcpListener(IPAddress.Loopback, 0);
		reservation.Start();
		occupied.Start();
		var port = ((IPEndPoint)reservation.LocalEndpoint).Port;
		var blocked = ((IPEndPoint)occupied.LocalEndpoint).Port;
		reservation.Stop();
		var builder = Sdk.CreateMeterProviderBuilder();
		new PrometheusExporterLauncher().Launch(builder, $"urls=http://metrics.example.invalid:{port}/metrics,http://127.0.0.1:{blocked}/metrics");

		Assert.Throws<IOException>(() => builder.Build());

		foreach(var address in Socket.OSSupportsIPv6 ? new[] { IPAddress.Loopback, IPAddress.IPv6Loopback } : [IPAddress.Loopback])
		{
			using var replacement = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			replacement.Bind(new IPEndPoint(address, port));

			Assert.Equal(port, ((IPEndPoint)replacement.LocalEndPoint).Port);
		}
	}

	private static HttpClient CreateClient(IPAddress address, X509Certificate2 certificate = null)
	{
		var handler = new SocketsHttpHandler
		{
			UseProxy = false,
			AllowAutoRedirect = false,
			ConnectCallback = async (context, cancellation) =>
			{
				var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

				try
				{
					await socket.ConnectAsync(new IPEndPoint(address, context.DnsEndPoint.Port), cancellation);
					return new NetworkStream(socket, ownsSocket: true);
				}
				catch
				{
					socket.Dispose();
					throw;
				}
			},
		};

		if(certificate != null)
			handler.SslOptions.RemoteCertificateValidationCallback = (_, remote, _, _) => remote?.GetCertHashString() == certificate.Thumbprint;

		return new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };
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
