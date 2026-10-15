using System;
using System.Linq;
using System.Net;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace Zongsoft.Web.Hosting.Tests;

public class WebEndpointPipelineTest
{
	[Theory]
	[InlineData(9000, "/api/item", 200)]
	[InlineData(9000, "/admin/item", 404)]
	[InlineData(9001, "/admin/item", 200)]
	[InlineData(9001, "/api/item", 404)]
	[InlineData(9000, "/api-extra/item", 404)]
	public async Task OrdinaryHttpWorksWithoutProtocolExtensions(int port, string path, int expected)
	{
		using var configuration = Configuration(
			("Endpoints:api:Url", "http://127.0.0.1:9000/api"), ("Endpoints:api:Protocols", "http2"),
			("Endpoints:admin:Url", "http://127.0.0.1:9001/admin"), ("Endpoints:admin:Protocols", "http2"));
		var services = Services();
		services.AddWebEndpoints(Declarations(configuration));
		using var provider = services.BuildServiceProvider();
		var application = new ApplicationBuilder(provider);
		application.UseWebEndpoints();
		application.UseRouting();
		application.UseEndpoints(routes =>
		{
			routes.Map("/api/item", context => Task.CompletedTask).WithMetadata(new WebEndpointMetadata("/api"));
			routes.Map("/admin/item", context => Task.CompletedTask).WithMetadata(new WebEndpointMetadata("/admin"));
		});
		var request = Request(provider, path, port);
		await application.Build()(request);
		Assert.Equal(expected, request.Response.StatusCode);

		if(expected == 200)
			Assert.Equal("http", request.Features.Get<WebEndpointFeature>().Kind);
	}

	[Fact]
	public void ProtocolRegistrationIsExplicitAndFrozen()
	{
		using var configuration = Configuration(("Endpoints:a:Url", "https://127.0.0.1:9000/"), ("Endpoints:a:Protocols", "custom"));
		var services = Services();
		Assert.Throws<InvalidOperationException>(() => services.AddWebEndpoints(Declarations(configuration)));

		var protocol = new WebEndpointProtocol("custom", "http2");
		services.AddWebEndpointProtocol(protocol);
		services.AddWebEndpointProtocol(protocol);
		Assert.Throws<InvalidOperationException>(() => services.AddWebEndpointProtocol(new WebEndpointProtocol("custom")));
		Assert.Throws<InvalidOperationException>(() => services.AddWebEndpointProtocol(WebEndpointProtocol.Http));

		var endpoints = services.AddWebEndpoints(Declarations(configuration));
		Assert.Equal("custom+http2", Assert.Single(Assert.Single(endpoints).Protocols).ToString());
		Assert.Throws<InvalidOperationException>(() => services.AddWebEndpointProtocol(new WebEndpointProtocol("late")));
		Assert.Throws<InvalidOperationException>(() => services.AddWebEndpoints(Declarations(configuration)));
	}

	[Theory]
	[InlineData(false, false, "http")]
	[InlineData(false, true, "custom")]
	[InlineData(true, false, "http")]
	[InlineData(true, true, "custom")]
	public async Task CustomKindIsSelectedBeforeLongestPathRegardlessOfRegistrationOrder(bool reverse, bool specialized, string expected)
	{
		using var configuration = Configuration(
			("Endpoints:ordinary:Url", "https://127.0.0.1:9000/a/deeper"), ("Endpoints:ordinary:Protocols", "http2"),
			("Endpoints:custom:Url", "https://127.0.0.1:9000/a"), ("Endpoints:custom:Protocols", "custom"));
		var services = Services();
		var protocols = new WebEndpointProtocol[] { new CustomProtocol("custom"), new CustomProtocol("another") };

		if(reverse)
			Array.Reverse(protocols);

		foreach(var protocol in protocols)
			services.AddWebEndpointProtocol(protocol);

		services.AddWebEndpoints(Declarations(configuration));
		using var provider = services.BuildServiceProvider();
		var application = new ApplicationBuilder(provider);
		application.UseWebEndpoints();
		application.Run(context => Task.CompletedTask);
		var request = Request(provider, "/a/deeper/item");

		if(specialized)
			request.Request.Headers["custom"] = "yes";

		await application.Build()(request);
		Assert.Equal(200, request.Response.StatusCode);
		Assert.Equal(expected, request.Features.Get<WebEndpointFeature>().Kind);
	}

	[Fact]
	public void DuplicateKindsAndDeferredFactoriesCannotEnterSnapshot()
	{
		using var configuration = Configuration(("Endpoints:a:Url", "https://127.0.0.1:9000/"), ("Endpoints:a:Protocols", "http2"));
		var services = Services();
		services.AddWebEndpointProtocol(new CustomProtocol("first", "shared"));
		Assert.Throws<InvalidOperationException>(() => services.AddWebEndpointProtocol(new CustomProtocol("second", "shared")));
		Assert.Throws<InvalidOperationException>(() => new WebEndpointCollection(Declarations(configuration),
			[new CustomProtocol("first", "shared"), new CustomProtocol("second", "shared")]));

		var deferred = Services();
		deferred.AddSingleton<WebEndpointProtocol>(_ => new CustomProtocol("custom"));
		Assert.Throws<InvalidOperationException>(() => deferred.AddWebEndpoints(Declarations(configuration)));
	}

	[Fact]
	public void NormalizationCannotReplaceApplicationProtocolIdentity()
	{
		using var configuration = Configuration(("Endpoints:a:Url", "https://127.0.0.1:9000/"), ("Endpoints:a:Protocols", "custom"));
		Assert.Throws<InvalidOperationException>(() => new WebEndpointCollection(Declarations(configuration), [new RenamingProtocol()]));
		Assert.Equal("custom", configuration["Endpoints:a:Protocols"]);
	}

	[Fact]
	public async Task AmbiguousRequestKindsDoNotUseRegistrationOrder()
	{
		using var configuration = Configuration(("Endpoints:a:Url", "https://127.0.0.1:9000/"), ("Endpoints:a:Protocols", "http2"));
		var services = Services();
		services.AddWebEndpointProtocol(new CustomProtocol("first"));
		services.AddWebEndpointProtocol(new CustomProtocol("second"));
		services.AddWebEndpoints(Declarations(configuration));
		using var provider = services.BuildServiceProvider();
		var application = new ApplicationBuilder(provider);
		application.UseWebEndpoints();
		application.Run(context => throw new InvalidOperationException("Unexpected dispatch."));
		var request = Request(provider, "/");
		request.Request.Headers["first"] = "yes";
		request.Request.Headers["second"] = "yes";

		await Assert.ThrowsAsync<InvalidOperationException>(() => application.Build()(request));
		Assert.Null(request.Features.Get<WebEndpointFeature>());
	}

	[Fact]
	public void PipelineCannotBeInstalledTwiceOrAfterRouting()
	{
		using var provider = Services().BuildServiceProvider();
		var application = new ApplicationBuilder(provider);
		application.UseWebEndpoints();
		Assert.Throws<InvalidOperationException>(() => application.UseWebEndpoints());
		var routed = new ApplicationBuilder(provider);
		routed.UseRouting();
		Assert.Throws<InvalidOperationException>(() => routed.UseWebEndpoints());
	}

	[Fact]
	public async Task LogicalEndpointsWorkWithoutConfigurationOrKestrel()
	{
		var services = Services();
		services.AddWebEndpoints([
			new WebEndpoint("api", Zongsoft.Communication.NetworkAddress.Parse("http://127.0.0.1:9000/api"), [Zongsoft.Communication.NetworkProtocol.Parse("http2")]),
		]);
		using var provider = services.BuildServiceProvider();
		var application = new ApplicationBuilder(provider);
		application.UseWebEndpoints();
		application.Run(context =>
		{
			context.Response.Headers["Selected"] = context.Features.Get<WebEndpointFeature>().Endpoint.Name;
			return Task.CompletedTask;
		});
		var pipeline = application.Build();
		var accepted = Request(provider, "/api/item");
		await pipeline(accepted);
		Assert.Equal(200, accepted.Response.StatusCode);
		Assert.Equal("api", accepted.Response.Headers["Selected"].ToString());
		var rejected = Request(provider, "/elsewhere");
		await pipeline(rejected);
		Assert.Equal(404, rejected.Response.StatusCode);
		Assert.False(rejected.Response.Headers.ContainsKey("Selected"));
	}

	[Fact]
	public void DeferredEndpointsUseFinalRegistrations()
	{
		using var configuration = Configuration(("Endpoints:a:Url", "https://127.0.0.1:9000/old"), ("Endpoints:a:Protocols", "custom"));
		var services = Services();
		var calls = 0;
		services.AddWebEndpoints(provider =>
		{
			calls++;
			return Declarations(provider.GetRequiredService<IConfiguration>());
		});
		services.AddSingleton<IConfiguration>(configuration);
		services.AddWebEndpointProtocol(new WebEndpointProtocol("custom", "http2"));
		configuration["Endpoints:a:Url"] = "https://127.0.0.1:9000/final";
		Assert.Equal(0, calls);
		Assert.Throws<InvalidOperationException>(() => services.AddWebEndpoints(_ => []));
		using var provider = services.BuildServiceProvider();
		Assert.Equal(0, calls);
		var endpoints = provider.GetRequiredService<WebEndpointCollection>();
		var endpoint = Assert.Single(endpoints);
		Assert.Equal("/final", endpoint.Address.Path);
		Assert.Equal("custom+http2", Assert.Single(endpoint.Protocols).ToString());
		configuration["Endpoints:a:Url"] = "https://127.0.0.1:9000/later";
		Assert.Same(endpoints, provider.GetRequiredService<WebEndpointCollection>());
		Assert.Equal("/final", endpoint.Address.Path);
		Assert.Equal(1, calls);
	}

	[Fact]
	public void DeferredEndpointsRequireRuntimeProtocolRules()
	{
		using var configuration = Configuration(("Endpoints:a:Url", "https://127.0.0.1:9000/"), ("Endpoints:a:Protocols", "custom+http2"));
		var services = Services();
		services.AddWebEndpoints(_ => Declarations(configuration));
		using var provider = services.BuildServiceProvider();
		Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<WebEndpointCollection>());
	}

	private static IServiceCollection Services() => new ServiceCollection().AddLogging().AddRouting().AddSingleton(new DiagnosticListener("Web.Tests"));

	private static ConfigurationRoot Configuration(params (string Key, string Value)[] values)
	{
		var entries = new Dictionary<string, string>();

		foreach(var (key, value) in values)
			entries.Add(key, value);

		return (ConfigurationRoot)new ConfigurationBuilder().AddInMemoryCollection(entries).Build();
	}

	private static DefaultHttpContext Request(IServiceProvider services, string path, int port = 9000)
	{
		var request = new DefaultHttpContext { RequestServices = services };
		request.Connection.LocalIpAddress = IPAddress.Loopback;
		request.Connection.LocalPort = port;
		request.Request.Protocol = "HTTP/2";
		request.Request.Path = path;
		request.Features.Get<IHttpRequestFeature>().RawTarget = path;
		return request;
	}

	private sealed class CustomProtocol(string name, string kind = null) : WebEndpointProtocol(name, "http2", kind ?? name)
	{
		public override bool Matches(HttpContext context) => context.Request.Headers.ContainsKey(this.Name);
	}

	private sealed class RenamingProtocol() : WebEndpointProtocol("custom")
	{
		public override Zongsoft.Communication.NetworkProtocol Normalize(Zongsoft.Communication.NetworkProtocol protocol) =>
			Zongsoft.Communication.NetworkProtocol.Parse("replacement+http2");
	}
	private static IEnumerable<WebEndpoint> Declarations(IConfiguration configuration) => configuration.GetSection("Endpoints").GetChildren().Select(section =>
		new WebEndpoint(section.Key, Zongsoft.Communication.NetworkAddress.Parse(section["Url"]), Zongsoft.Communication.NetworkProtocol.Parse(section["Protocols"], ',')));

}
