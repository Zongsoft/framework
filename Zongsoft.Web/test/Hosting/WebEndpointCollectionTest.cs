using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;

using Xunit;

namespace Zongsoft.Web.Hosting.Tests;

public class WebEndpointCollectionTest
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

		var snapshot = new WebEndpointCollection(configuration, [new WebEndpointProtocol("protobuf"), new WebEndpointProtocol("custom")]);
		Assert.Equal(4, snapshot.Count);
		Assert.Equal(["quic", "b", "a", "web"], snapshot.Select(endpoint => endpoint.Name));
		Assert.Equal("http://127.0.0.1:4317/a", snapshot[2].Address.ToString());
		Assert.Equal(["http2"], snapshot[2].Protocols.Select(protocol => protocol.ToString()));
		Assert.Equal(HttpProtocols.Http2, snapshot[2].HttpProtocols);
		Assert.Equal(HttpProtocols.Http2, snapshot[2].GetProtocols());
		Assert.False(snapshot.HasChanged(configuration));

		using var host = new HostBuilder().ConfigureWebHost(web => web.UseKestrel().Configure(_ => { })).Build();
		var options = new KestrelServerOptions { ApplicationServices = host.Services };
		snapshot.Configure(options);
		var projection = options.ConfigurationLoader.Configuration;
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

		configuration["Endpoints:a:Url"] = "http://127.0.0.1:4318/changed";
		Assert.True(snapshot.HasChanged(configuration));
		Assert.Equal("http://127.0.0.1:4317/a", snapshot[2].Address.ToString());
		Assert.Equal("http://127.0.0.1:4317", projection["Endpoints:a:Url"]);
		projection["Endpoints:a:Url"] = "http://127.0.0.1:1";
		snapshot.Configure(options);
		Assert.Equal("http://127.0.0.1:4317", options.ConfigurationLoader.Configuration["Endpoints:a:Url"]);
	}

	[Theory]
	[InlineData("protobuf,protobuf+http2", "protobuf+http2", HttpProtocols.Http2)]
	[InlineData("http,http1,http2,http3", "http1,http2,http3", HttpProtocols.Http1AndHttp2AndHttp3)]
	[InlineData("custom+http2,http3+custom", "custom+http2,custom+http3", HttpProtocols.Http2 | HttpProtocols.Http3)]
	public void NormalizeDeclaredCapabilities(string protocols, string expected, HttpProtocols http)
	{
		using var configuration = Configure(("Endpoints:a:Url", "https://127.0.0.1:9000/a"), ("Endpoints:a:Protocols", protocols));
		var snapshot = new WebEndpointCollection(configuration, [new WebEndpointProtocol("protobuf"), new WebEndpointProtocol("custom")]);
		var endpoint = Assert.Single(snapshot);

		Assert.Equal(expected.Split(','), endpoint.Protocols.Select(protocol => protocol.ToString()));
		Assert.Equal(http, endpoint.HttpProtocols);
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
	[InlineData("grpc+http1")]
	[InlineData("Http1AndHttp2")]
	[InlineData("grpc,http1,")]
	public void RejectUnusableCapabilities(string protocols)
	{
		using var configuration = Configure(("Endpoints:a:Url", "https://127.0.0.1:9000/a"), ("Endpoints:a:Protocols", protocols));
		Assert.Throws<InvalidOperationException>(() => new WebEndpointCollection(configuration, [new WebEndpointProtocol("protobuf"), new WebEndpointProtocol("custom")]));
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
		Assert.Throws<InvalidOperationException>(() => new WebEndpointCollection(configuration, [new WebEndpointProtocol("protobuf"), new WebEndpointProtocol("custom")]));
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
		Assert.Throws<InvalidOperationException>(() => new WebEndpointCollection(configuration, [new WebEndpointProtocol("protobuf"), new WebEndpointProtocol("custom")]));
	}

	[Theory]
	[InlineData("http://*:9000/a", "http1", "http://127.0.0.1:9000/b", "http1")]
	[InlineData("http://localhost:9000/a", "http1", "http://127.0.0.1:9000/b", "http1")]
	[InlineData("http://127.0.0.1:9000/a", "http1", "https://127.0.0.1:9000/b", "http2")]
	[InlineData("http://127.0.0.1:9000/a", "http1", "http://127.0.0.1:9000/b", "http2")]
	[InlineData("https://127.0.0.1:9000/a", "http1", "https://127.0.0.1:9000/a", "protobuf+http2")]
	[InlineData("http://127.0.0.1:9000/a", "http2", "http://127.0.0.1:9001/A", "http2")]
	[InlineData("http://127.0.0.1:9000/a", "http3", "http://127.0.0.1:9001/b", "http2")]
	public void RejectConflictingDeclarations(string first, string firstProtocols, string second, string secondProtocols)
	{
		using var configuration = Configure(
			("Endpoints:a:Url", first),
			("Endpoints:a:Protocols", firstProtocols),
			("Endpoints:b:Url", second),
			("Endpoints:b:Protocols", secondProtocols));

		Assert.Throws<InvalidOperationException>(() => new WebEndpointCollection(configuration, [new WebEndpointProtocol("protobuf"), new WebEndpointProtocol("custom")]));
	}

	[Fact]
	public void ListenerOptionsAndSniCannotContradictCapabilities()
	{
		using var configuration = Configure(
			("Endpoints:a:Url", "https://127.0.0.1:9000/a"), ("Endpoints:a:Protocols", "http2"),
			("Endpoints:b:Url", "https://127.0.0.1:9000/b"), ("Endpoints:b:Protocols", "http2"),
			("Endpoints:a:Certificate:Path", "a.pfx"), ("Endpoints:b:Certificate:Path", "b.pfx"));

		Assert.Throws<InvalidOperationException>(() => new WebEndpointCollection(configuration, [new WebEndpointProtocol("protobuf"), new WebEndpointProtocol("custom")]));
		configuration["Endpoints:b:Certificate:Path"] = "a.pfx";
		Assert.Equal(2, new WebEndpointCollection(configuration, [new WebEndpointProtocol("protobuf"), new WebEndpointProtocol("custom")]).Count);
		configuration["Endpoints:a:Sni:example.test:Protocols"] = "Http3";
		configuration["Endpoints:b:Sni:example.test:Protocols"] = "Http3";
		Assert.Throws<InvalidOperationException>(() => new WebEndpointCollection(configuration, [new WebEndpointProtocol("protobuf"), new WebEndpointProtocol("custom")]));
	}

	[Fact]
	public void ExplicitDefaultsAndDistinctTransportsRemainIndependent()
	{
		using var configuration = Configure(
			("EndpointDefaults:Protocols", "http1"),
			("Endpoints:tcp:Url", "http://127.0.0.1:9000/"),
			("Endpoints:udp:Url", "https://127.0.0.1:9000/"),
			("Endpoints:udp:Protocols", "http3"));
		var snapshot = new WebEndpointCollection(configuration, [new WebEndpointProtocol("protobuf"), new WebEndpointProtocol("custom")]);

		Assert.Equal(2, snapshot.Count);
		Assert.Equal(HttpProtocols.Http1, snapshot.Single(endpoint => endpoint.Name == "tcp").HttpProtocols);
		Assert.Equal(HttpProtocols.Http3, snapshot.Single(endpoint => endpoint.Name == "udp").HttpProtocols);
	}

	[Fact]
	public void Ipv6SocketIsBoundWithoutDualModeAndOtherFactoryIsPreserved()
	{
		Assert.SkipUnless(Socket.OSSupportsIPv6, "IPv6 sockets are not available on this platform.");

		using var reservation = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		reservation.Bind(new IPEndPoint(IPAddress.Loopback, 0));
		var port = ((IPEndPoint)reservation.LocalEndPoint).Port;
		using var configuration = Configure(("Endpoints:v6:Url", $"http://[::]:{port}/"), ("Endpoints:v6:Protocols", "http1"));
		var snapshot = new WebEndpointCollection(configuration, [new WebEndpointProtocol("protobuf"), new WebEndpointProtocol("custom")]);
		var delegated = false;

		var options = new SocketTransportOptions
		{
			CreateBoundListenSocket = endpoint =>
			{
				delegated = true;
				return SocketTransportOptions.CreateDefaultBoundListenSocket(endpoint);
			},
		};

		snapshot.Configure(options);
		using var ipv6 = options.CreateBoundListenSocket(new IPEndPoint(IPAddress.IPv6Any, port));
		Assert.False(ipv6.DualMode);
		Assert.False(delegated);
		Assert.Equal(port, ((IPEndPoint)ipv6.LocalEndPoint).Port);
		using var other = options.CreateBoundListenSocket(new IPEndPoint(IPAddress.Loopback, 0));
		Assert.True(delegated);
		Assert.Equal(AddressFamily.InterNetwork, other.AddressFamily);
	}

	private static ConfigurationRoot Configure(params (string Key, string Value)[] values) =>
		(ConfigurationRoot)new ConfigurationBuilder().AddInMemoryCollection(values.Select(value => new KeyValuePair<string, string>(value.Key, value.Value))).Build();
}
