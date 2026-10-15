using System;
using System.Linq;
using System.Collections.Generic;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Zongsoft.Web.Hosting;

using Xunit;

namespace Zongsoft.Web.Grpc.Tests;

public class GrpcEndpointProtocolTest
{
	[Theory]
	[InlineData("grpc", "grpc+http2", 0, 2)]
	[InlineData("grpc,HTTP2+grpc", "grpc+http2", 0, 2)]
	[InlineData("grpc+http3", "grpc+http3", 0, 3)]
	[InlineData("http2,grpc+http2", "grpc+http2,http2", 2, 2)]
	public void GrpcDeclarationRequiresAndUsesItsRegisteredExtension(string text, string expected, int http, int grpc)
	{
		using var configuration = Configuration(text);
		var services = new ServiceCollection();
		Assert.Throws<InvalidOperationException>(() => services.AddWebEndpoints(Declarations(configuration)));
		services.AddGrpcWebEndpoints();
		services.AddGrpcWebEndpoints();
		var snapshot = services.AddWebEndpoints(Declarations(configuration));
		var endpoint = Assert.Single(snapshot);

		Assert.Equal(expected.Split(','), endpoint.Protocols.Select(protocol => protocol.ToString()));
		Assert.True(endpoint.Supports(grpc));
		Assert.Equal(http > 0, endpoint.Protocols.Any(protocol => protocol.Name == null && protocol.HttpVersion == http));
		Assert.Contains(endpoint.Protocols, protocol => protocol.Name == "grpc" && protocol.HttpVersion == grpc);
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
		Assert.Throws<InvalidOperationException>(() => services.AddWebEndpoints(Declarations(configuration)));
	}

	[Fact]
	public void PluginRegistrationIncludesEndpointPolicyWithoutManualServiceLists()
	{
		using var configuration = Configuration("grpc");
		var services = new ServiceCollection();
		new GrpcInitializer.Registration().Register(services, configuration);
		var snapshot = services.AddWebEndpoints(Declarations(configuration));
		Assert.Contains(Assert.Single(snapshot).Protocols, protocol => protocol.Name == "grpc" && protocol.HttpVersion == 2);
	}

	private static ConfigurationRoot Configuration(string protocol) =>
		(ConfigurationRoot)new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
		{
			["Endpoints:a:Url"] = "https://127.0.0.1:9000/telemetry",
			["Endpoints:a:Protocols"] = protocol,
		}).Build();
	private static IEnumerable<WebEndpoint> Declarations(IConfiguration configuration) => configuration.GetSection("Endpoints").GetChildren().Select(section =>
		new WebEndpoint(section.Key, Zongsoft.Communication.NetworkAddress.Parse(section["Url"]), Zongsoft.Communication.NetworkProtocol.Parse(section["Protocols"], ',')));

}
