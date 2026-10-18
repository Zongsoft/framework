using System;
using System.IO;
using System.Net;
using System.Text;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics.Metrics;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using OpenTelemetry;
using OpenTelemetry.Metrics;

using Zongsoft.Diagnostics.Telemetry;

using Xunit;

namespace Zongsoft.Diagnostics.Web.IntegrationTests;

[Trait("Category", "Integration")]
public class PrometheusExporterLauncherIntegrationTest(ITestOutputHelper output)
{
	[Theory]
	[InlineData("")]
	[InlineData("/")]
	[InlineData("/metrics")]
	[InlineData("/metrics/")]
	[InlineData("/MeTrIcS/")]
	[InlineData("/telemetry/metrics")]
	[InlineData("/telemetry/metrics/")]
	public async Task LoopbackListenerScrapesDeclaredPrefixAndStopsOnDisposeAsync(string path)
	{
		using var reservation = new TcpListener(IPAddress.Loopback, 0);
		reservation.Start();
		var port = ((IPEndPoint)reservation.LocalEndpoint).Port;

		var url = $"http://127.0.0.1:{port}{path}";
		var prefix = $"http://127.0.0.1:{port}{path.TrimEnd('/')}/";
		using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(15));
		using var meter = new Meter($"Zongsoft.Diagnostics.Tests.{Guid.NewGuid():N}");
		var counter = meter.CreateCounter<long>("zongsoft_test_requests");
		var builder = Sdk.CreateMeterProviderBuilder().AddMeter(meter.Name);
		new PrometheusExporterLauncher().Launch(builder, $"urls={url};cacheDuration=0ms");

		reservation.Stop();

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
	public async Task RawRequestPathControlsTheScrapeRange(string target, int expected)
	{
		using var reservation = new TcpListener(IPAddress.Loopback, 0);
		reservation.Start();
		var port = ((IPEndPoint)reservation.LocalEndpoint).Port;
		var builder = Sdk.CreateMeterProviderBuilder();
		new PrometheusExporterLauncher().Launch(builder, $"urls=http://127.0.0.1:{port}/metrics/");
		reservation.Stop();
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
	public async Task EquivalentAddressesShareTheListenerAndStartFailureReleasesPorts()
	{
		//先占用失败端口，避免它复用下方正常监听释放后的端口。
		using var occupied = new TcpListener(IPAddress.Loopback, 0);
		occupied.Start();
		var blocked = ((IPEndPoint)occupied.LocalEndpoint).Port;
		using var reservation = new TcpListener(IPAddress.Loopback, 0);
		reservation.Start();
		var port = ((IPEndPoint)reservation.LocalEndpoint).Port;
		var builder = Sdk.CreateMeterProviderBuilder();
		new PrometheusExporterLauncher().Launch(builder, $"urls=http://127.0.0.1:{port}/metrics,http://localhost:{port}/METRICS/");

		Assert.NotEqual(blocked, port);
		reservation.Stop();

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
	public async Task HttpsUsesConfiguredCertificateWithoutImportingHostEndpoints(string host)
	{
		using var reservation = new TcpListener(IPAddress.Loopback, 0);
		reservation.Start();
		var port = ((IPEndPoint)reservation.LocalEndpoint).Port;
		using var occupied = new TcpListener(IPAddress.Loopback, 0);
		occupied.Start();
		Assert.NotEqual(port, ((IPEndPoint)occupied.LocalEndpoint).Port);
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
			//证书和宿主端点准备期间保留端口，直到抓取服务器即将接管监听。
			reservation.Stop();
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
	public async Task DomainListenersMatchHostsAcrossPortsAndAddressFamiliesAsync()
	{
		using var first = new TcpListener(IPAddress.Loopback, 0);
		using var second = new TcpListener(IPAddress.Loopback, 0);
		first.Start();
		second.Start();
		var port = ((IPEndPoint)first.LocalEndpoint).Port;
		var otherPort = ((IPEndPoint)second.LocalEndpoint).Port;
		using var meter = new Meter($"Zongsoft.Diagnostics.Tests.{Guid.NewGuid():N}");
		var counter = meter.CreateCounter<long>("zongsoft_domain_requests");
		var builder = Sdk.CreateMeterProviderBuilder().AddMeter(meter.Name);
		new PrometheusExporterLauncher().Launch(builder,
			$"urls=http://metrics.example.invalid.:{port}/MeTrIcS/,http://bücher.example.invalid:{port}/metrics,http://secondary.example.invalid:{otherPort}/METRICS;cacheDuration=0ms");
		first.Stop();
		second.Stop();
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
	public async Task MixedDomainAndIpDeclarationsPreserveInterfaceScopesAsync(string host, bool ipv4Allowed, bool otherIpv4Allowed, bool ipv6Allowed)
	{
		using var reservation = new TcpListener(IPAddress.Loopback, 0);
		reservation.Start();
		var port = ((IPEndPoint)reservation.LocalEndpoint).Port;
		var builder = Sdk.CreateMeterProviderBuilder();
		new PrometheusExporterLauncher().Launch(builder, $"urls=http://metrics.example.invalid:{port}/metrics,http://{host}:{port}/METRICS/");
		reservation.Stop();
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
	public async Task Ipv6WildcardDoesNotOpenIpv4Async()
	{
		using var reservation = new TcpListener(IPAddress.IPv6Loopback, 0);
		reservation.Start();
		var port = ((IPEndPoint)reservation.LocalEndpoint).Port;
		var builder = Sdk.CreateMeterProviderBuilder();
		new PrometheusExporterLauncher().Launch(builder, $"urls=http://[::]:{port}/metrics");
		reservation.Stop();
		using var provider = builder.Build();
		using var ipv6 = CreateClient(IPAddress.IPv6Loopback);
		using var response = await ipv6.GetAsync($"http://localhost:{port}/metrics");

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);

		using var ipv4 = CreateClient(IPAddress.Loopback);
		await Assert.ThrowsAsync<HttpRequestException>(() => ipv4.GetAsync($"http://localhost:{port}/metrics"));
	}

	[Fact]
	public void DomainStartupFailureReleasesAllListeners()
	{
		using var reservation = new TcpListener(IPAddress.Loopback, 0);
		using var occupied = new TcpListener(IPAddress.Loopback, 0);
		reservation.Start();
		occupied.Start();
		var port = ((IPEndPoint)reservation.LocalEndpoint).Port;
		var blocked = ((IPEndPoint)occupied.LocalEndpoint).Port;
		var builder = Sdk.CreateMeterProviderBuilder();
		new PrometheusExporterLauncher().Launch(builder, $"urls=http://metrics.example.invalid:{port}/metrics,http://127.0.0.1:{blocked}/metrics");

		reservation.Stop();
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
}
