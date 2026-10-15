using System;
using System.Net;
using System.Linq;
using System.Net.Sockets;
using System.Collections.Generic;

using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;

using Xunit;

namespace Zongsoft.Web.Hosting.Tests;

public class KestrelUtilityTest
{
	[Fact]
	public void SnapshotProjectsSharedListenersWithoutChangingSource()
	{
		using var configuration = Configure(
			("Endpoints:web:Url", "http://*:8080/"), ("Endpoints:web:Protocols", "http1"),
			("Endpoints:a:Url", "http://127.0.0.1:4317/a"), ("Endpoints:a:Protocols", "http2"),
			("Endpoints:b:Url", "http://127.0.0.1:4317/b/"), ("Endpoints:b:Protocols", "HTTP2"),
			("Endpoints:quic:Url", "https://[::]:8443/telemetry"), ("Endpoints:quic:Protocols", "http3"),
			("Endpoints:quic:Certificate:Path", "certificate.pfx"),
			("Endpoints:quic:Sni:example.test:Protocols", "Http3"),
			("EndpointDefaults:SslProtocols", "Tls12,Tls13"),
			("Certificates:Default:Path", "default.pfx"));

		using var projection = KestrelUtility.Convert(configuration, out var endpoints);
		Assert.Equal(4, endpoints.Count);
		Assert.Equal(3, projection.GetSection("Endpoints").GetChildren().Count());
		Assert.Equal("http://0.0.0.0:8080", projection["Endpoints:web:Url"]);
		Assert.Equal("http://127.0.0.1:4317", projection["Endpoints:a:Url"]);
		Assert.Equal("Http2", projection["Endpoints:a:Protocols"]);
		Assert.Equal("Http3", projection["Endpoints:quic:Protocols"]);
		Assert.Equal("certificate.pfx", projection["Endpoints:quic:Certificate:Path"]);
		Assert.Equal("Http3", projection["Endpoints:quic:Sni:example.test:Protocols"]);
		Assert.Equal("Tls12,Tls13", projection["EndpointDefaults:SslProtocols"]);
		Assert.Equal("default.pfx", projection["Certificates:Default:Path"]);
		Assert.Equal("http://*:8080/", configuration["Endpoints:web:Url"]);
		Assert.Equal("http2", configuration["Endpoints:a:Protocols"]);
		Assert.Equal("/b/", endpoints.Single(endpoint => endpoint.Name == "b").Address.Path);

		configuration["Endpoints:a:Url"] = "http://127.0.0.1:4318/changed";
		Assert.Equal("http://127.0.0.1:4317/a", endpoints.Single(endpoint => endpoint.Name == "a").Address.ToString());
		Assert.Equal("http://127.0.0.1:4317", projection["Endpoints:a:Url"]);
		projection["Endpoints:a:Url"] = "http://127.0.0.1:1";
		Assert.Equal("http://127.0.0.1:4318/changed", configuration["Endpoints:a:Url"]);
		Assert.Equal("http://127.0.0.1:4317/a", endpoints.Single(endpoint => endpoint.Name == "a").Address.ToString());
	}

	[Theory]
	[InlineData("protobuf,protobuf+http2", "protobuf+http2", HttpProtocols.Http2)]
	[InlineData("http,http1,http2,http3", "http1,http2,http3", HttpProtocols.Http1AndHttp2AndHttp3)]
	[InlineData("custom+http2,http3+custom", "custom+http2,custom+http3", HttpProtocols.Http2 | HttpProtocols.Http3)]
	public void NormalizeDeclaredCapabilities(string protocols, string expected, HttpProtocols http)
	{
		using var configuration = Configure(("Endpoints:a:Url", "https://127.0.0.1:9000/a"), ("Endpoints:a:Protocols", protocols));
		using var projection = KestrelUtility.Convert(configuration, out var snapshot, [new WebEndpointProtocol("protobuf"), new WebEndpointProtocol("custom")]);
		var endpoint = Assert.Single(snapshot);

		Assert.Equal(expected.Split(','), endpoint.Protocols.Select(protocol => protocol.ToString()));
		Assert.Equal(http.ToString(), projection["Endpoints:a:Protocols"]);
		Assert.Throws<NotSupportedException>(() => ((IList<Zongsoft.Communication.NetworkProtocol>)endpoint.Protocols).Clear());
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("http")]
	[InlineData("protobuf")]
	[InlineData("custom")]
	[InlineData("grpc+tcp")]
	[InlineData("custom+udp")]
	[InlineData("Http1AndHttp2")]
	[InlineData("grpc,http1,")]
	public void RejectUnusableCapabilities(string protocols)
	{
		using var configuration = Configure(("Endpoints:a:Url", "https://127.0.0.1:9000/a"), ("Endpoints:a:Protocols", protocols));
		Assert.Throws<InvalidOperationException>(() => Convert(configuration));
	}

	[Theory]
	[InlineData("/a//b")]
	[InlineData("/a/../b")]
	[InlineData("/%61")]
	[InlineData("/a/{name}")]
	[InlineData("/路径")]
	[InlineData("/*")]
	public void RejectInvalidDeclaredPaths(string path)
	{
		using var configuration = Configure(("Endpoints:a:Url", "http://localhost:9000" + path), ("Endpoints:a:Protocols", "http1"));
		Assert.Throws<InvalidOperationException>(() => Convert(configuration));
		Assert.Throws<ArgumentException>(() => new WebEndpointMetadata(path));
	}

	[Theory]
	[InlineData("tcp://127.0.0.1:9000/")]
	[InlineData("http://example.test:9000/")]
	[InlineData("http://127.0.0.1:0/")]
	[InlineData("http://127.0.0.1:9000/?q=1")]
	public void RejectNonWebListeners(string address)
	{
		using var configuration = Configure(("Endpoints:a:Url", address), ("Endpoints:a:Protocols", "http1"));
		Assert.Throws<InvalidOperationException>(() => Convert(configuration));
	}

	[Theory]
	[InlineData("http://*:9000/a", "http1", "http://127.0.0.1:9000/b", "http1")]
	[InlineData("http://localhost:9000/a", "http1", "http://127.0.0.1:9000/b", "http1")]
	[InlineData("http://127.0.0.1:9000/a", "http1", "https://127.0.0.1:9000/b", "http2")]
	[InlineData("http://127.0.0.1:9000/a", "http1", "http://127.0.0.1:9000/b", "http2")]
	[InlineData("http://127.0.0.1:9000/a", "http3", "http://127.0.0.1:9001/b", "http2")]
	public void RejectConflictingDeclarations(string first, string firstProtocols, string second, string secondProtocols)
	{
		using var configuration = Configure(
			("Endpoints:a:Url", first),
			("Endpoints:a:Protocols", firstProtocols),
			("Endpoints:b:Url", second),
			("Endpoints:b:Protocols", secondProtocols));

		Assert.Throws<InvalidOperationException>(() => Convert(configuration));
	}

	[Fact]
	public void ListenerOptionsAndSniCannotContradictCapabilities()
	{
		using var configuration = Configure(
			("Endpoints:a:Url", "https://127.0.0.1:9000/a"), ("Endpoints:a:Protocols", "http2"),
			("Endpoints:b:Url", "https://127.0.0.1:9000/b"), ("Endpoints:b:Protocols", "http2"),
			("Endpoints:a:Certificate:Path", "a.pfx"), ("Endpoints:b:Certificate:Path", "b.pfx"));

		Assert.Throws<InvalidOperationException>(() => Convert(configuration));
		configuration["Endpoints:b:Certificate:Path"] = "a.pfx";
		using var converted = Convert(configuration);
		Assert.Single(converted.GetSection("Endpoints").GetChildren());
		configuration["Endpoints:a:Sni:example.test:Protocols"] = "Http3";
		configuration["Endpoints:b:Sni:example.test:Protocols"] = "Http3";
		Assert.Throws<InvalidOperationException>(() => Convert(configuration));
	}

	[Fact]
	public void ExplicitDefaultsAndDistinctTransportsRemainIndependent()
	{
		using var configuration = Configure(
			("EndpointDefaults:Protocols", "http1"),
			("Endpoints:tcp:Url", "http://127.0.0.1:9000/"),
			("Endpoints:udp:Url", "https://127.0.0.1:9000/"),
			("Endpoints:udp:Protocols", "http3"));
		using var projection = KestrelUtility.Convert(configuration, out var snapshot, [new WebEndpointProtocol("protobuf"), new WebEndpointProtocol("custom")]);

		Assert.Equal(2, snapshot.Count);
		Assert.True(snapshot.Single(endpoint => endpoint.Name == "tcp").Supports(1));
		Assert.True(snapshot.Single(endpoint => endpoint.Name == "udp").Supports(3));
	}

	[Fact]
	public void Ipv6SocketIsBoundWithoutDualModeAndOtherFactoryIsPreserved()
	{
		Assert.SkipUnless(Socket.OSSupportsIPv6, "IPv6 sockets are not available on this platform.");

		using var reservation = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		reservation.Bind(new IPEndPoint(IPAddress.Loopback, 0));
		var port = ((IPEndPoint)reservation.LocalEndPoint).Port;
		using var configuration = Configure(("Endpoints:v6:Url", $"http://[::]:{port}/"), ("Endpoints:v6:Protocols", "http1"));
		using var projection = KestrelUtility.Convert(configuration, out var snapshot, [new WebEndpointProtocol("protobuf"), new WebEndpointProtocol("custom")]);
		var delegated = false;

		var options = new SocketTransportOptions
		{
			CreateBoundListenSocket = endpoint =>
			{
				delegated = true;
				return SocketTransportOptions.CreateDefaultBoundListenSocket(endpoint);
			},
		};

		KestrelUtility.Configure(options, snapshot);
		using var ipv6 = options.CreateBoundListenSocket(new IPEndPoint(IPAddress.IPv6Any, port));
		Assert.False(ipv6.DualMode);
		Assert.False(delegated);
		Assert.Equal(port, ((IPEndPoint)ipv6.LocalEndPoint).Port);
		using var other = options.CreateBoundListenSocket(new IPEndPoint(IPAddress.Loopback, 0));
		Assert.True(delegated);
		Assert.Equal(AddressFamily.InterNetwork, other.AddressFamily);
	}

	[Theory]
	[InlineData("grpc+http2", "http://0.0.0.0:4317", "Http2")]
	[InlineData("http2+grpc", "http://0.0.0.0:4317", "Http2")]
	[InlineData("custom+http2", "http://0.0.0.0:4317", "Http2")]
	public void ConvertExplicitApplicationProtocolsWithoutRuntimeRegistration(string protocol, string expectedUrl, string expectedProtocols)
	{
		using var configuration = Configure(("Endpoints:telemetry:Url", "http://*:4317/opentelemetry"), ("Endpoints:telemetry:Protocols", protocol));
		using var projection = KestrelUtility.Convert(configuration, out var endpoints);
		Assert.Equal(expectedUrl, projection["Endpoints:telemetry:Url"]);
		Assert.Equal(expectedProtocols, projection["Endpoints:telemetry:Protocols"]);
		Assert.Equal("/opentelemetry", Assert.Single(endpoints).Address.Path);
		Assert.Throws<InvalidOperationException>(() => new WebEndpointCollection(endpoints));
	}

	[Fact]
	public void ConvertUsesSuppliedDefaultsWithoutRequestPolicy()
	{
		using var configuration = Configure(("Endpoints:a:Url", "http://127.0.0.1:4317/a"), ("Endpoints:a:Protocols", "custom"));
		Assert.Throws<InvalidOperationException>(() => KestrelUtility.Convert(configuration));
		using var projection = KestrelUtility.Convert(configuration, out var endpoints, [new WebEndpointProtocol("custom", "http2")]);
		Assert.Equal("Http2", projection["Endpoints:a:Protocols"]);
		Assert.Equal("custom+http2", Assert.Single(Assert.Single(endpoints).Protocols).ToString());
		Assert.Equal("custom", configuration["Endpoints:a:Protocols"]);
	}

	[Fact]
	public void ConversionDoesNotOwnRequestRangePolicy()
	{
		using var configuration = Configure(
			("Endpoints:a:Url", "https://127.0.0.1:9000/a"), ("Endpoints:a:Protocols", "http1"),
			("Endpoints:b:Url", "https://127.0.0.1:9000/a"), ("Endpoints:b:Protocols", "http2"));
		using var projection = KestrelUtility.Convert(configuration, out var endpoints);
		Assert.Single(projection.GetSection("Endpoints").GetChildren());
		Assert.Equal("Http1AndHttp2", projection["Endpoints:a:Protocols"]);
		Assert.Throws<InvalidOperationException>(() => new WebEndpointCollection(endpoints));
	}

	[Fact]
	public void EmptyEndpointsAndCallerSelectedSectionRemainIndependent()
	{
		using var empty = Configure(("EndpointDefaults:Protocols", "Http1AndHttp2"), ("Limits:MaxRequestBodySize", "1024"));
		using var converted = KestrelUtility.Convert(empty, out var endpoints);
		Assert.Empty(endpoints);
		Assert.Equal("Http1AndHttp2", converted["EndpointDefaults:Protocols"]);
		Assert.Equal("1024", converted["Limits:MaxRequestBodySize"]);
		using var source = Configure(("Custom:Endpoints:a:Url", "http://127.0.0.1:9000/api"), ("Custom:Endpoints:a:Protocols", "http1"));
		using var projection = KestrelUtility.Convert(source.GetSection("Custom"));
		Assert.Equal("http://127.0.0.1:9000", projection["Endpoints:a:Url"]);
		Assert.Equal("http://127.0.0.1:9000/api", source["Custom:Endpoints:a:Url"]);
	}

	private static ConfigurationRoot Convert(IConfiguration configuration) => KestrelUtility.Convert(configuration, [new WebEndpointProtocol("protobuf"), new WebEndpointProtocol("custom")]);

	private static ConfigurationRoot Configure(params (string Key, string Value)[] values) =>
		(ConfigurationRoot)new ConfigurationBuilder().AddInMemoryCollection(values.Select(value => new KeyValuePair<string, string>(value.Key, value.Value))).Build();
}
