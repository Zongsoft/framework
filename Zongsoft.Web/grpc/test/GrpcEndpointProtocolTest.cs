using System;
using System.Linq;
using System.Collections.Generic;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Server.Kestrel.Core;

using Zongsoft.Web.Hosting;

using Xunit;

namespace Zongsoft.Web.Grpc.Tests;

public class GrpcEndpointProtocolTest
{
	[Theory]
	[InlineData("grpc", "grpc+http2", HttpProtocols.None, HttpProtocols.Http2)]
	[InlineData("grpc,HTTP2+grpc", "grpc+http2", HttpProtocols.None, HttpProtocols.Http2)]
	[InlineData("grpc+http3", "grpc+http3", HttpProtocols.None, HttpProtocols.Http3)]
	[InlineData("http2,grpc+http2", "grpc+http2,http2", HttpProtocols.Http2, HttpProtocols.Http2)]
	public void GrpcDeclarationRequiresAndUsesItsRegisteredExtension(string text, string expected, HttpProtocols http, HttpProtocols grpc)
	{
		using var configuration = Configuration(text);
		var services = new ServiceCollection();
		Assert.Throws<InvalidOperationException>(() => services.AddWebEndpoints(configuration));
		services.AddGrpcWebEndpoints();
		services.AddGrpcWebEndpoints();
		var snapshot = services.AddWebEndpoints(configuration);
		var endpoint = Assert.Single(snapshot);

		Assert.Equal(expected.Split(','), endpoint.Protocols.Select(protocol => protocol.ToString()));
		Assert.Equal(http | grpc, endpoint.HttpProtocols);
		Assert.Equal(http, endpoint.GetProtocols());
		Assert.Equal(grpc, endpoint.GetProtocols("grpc"));
		Assert.Throws<InvalidOperationException>(() => services.AddGrpcWebEndpoints());
	}

	[Theory]
	[InlineData("grpc+http1")]
	[InlineData("grpc+tcp")]
	[InlineData("grpc+udp")]
	[InlineData("grpc+http")]
	public void GrpcPolicyRejectsUnsupportedCarriers(string protocol)
	{
		using var configuration = Configuration(protocol);
		var services = new ServiceCollection();
		services.AddGrpcWebEndpoints();
		Assert.Throws<InvalidOperationException>(() => services.AddWebEndpoints(configuration));
	}

	[Fact]
	public void PluginRegistrationIncludesEndpointPolicyWithoutManualServiceLists()
	{
		using var configuration = Configuration("grpc");
		var services = new ServiceCollection();
		new GrpcInitializer.Registration().Register(services, configuration);
		var snapshot = services.AddWebEndpoints(configuration);
		Assert.Equal(HttpProtocols.Http2, Assert.Single(snapshot).GetProtocols("grpc"));
	}

	private static ConfigurationRoot Configuration(string protocol) =>
		(ConfigurationRoot)new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
		{
			["Endpoints:a:Url"] = "https://127.0.0.1:9000/telemetry",
			["Endpoints:a:Protocols"] = protocol,
		}).Build();
}
