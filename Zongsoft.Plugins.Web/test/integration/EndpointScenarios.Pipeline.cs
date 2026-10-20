using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Zongsoft.Web.Hosting;

using Xunit;

namespace Zongsoft.Plugins.Web.IntegrationTests;

internal static partial class EndpointScenarios
{
	private static async Task PathfulEndpointsListenAndEnforceRanges(EndpointTestScope scope)
	{
		var port = EndpointTestScope.Port();
		var second = EndpointTestScope.Port();
		await using var application = await ApplicationAsync(scope, builder =>
		{
			ConfigureEndpoint(builder.Configuration, port, "/api", "api");
			ConfigureEndpoint(builder.Configuration, port, "/admin", "admin");
			ConfigureEndpoint(builder.Configuration, second, "/protected", "protected");
		}, endpoints: false);
		application.Run(context => context.Response.WriteAsync(context.Features.Get<WebEndpointFeature>().Endpoint.Name));
		await application.StartAsync();
		using var client = Client(port);
		using var other = Client(second);
		Assert.Equal("api", await client.GetStringAsync("/api/items"));
		Assert.Equal("admin", await client.GetStringAsync("/admin/items"));
		Assert.Equal("protected", await other.GetStringAsync("/protected/items"));
		await Status(client, "/protected/items", HttpStatusCode.NotFound);
		await Status(other, "/api/items", HttpStatusCode.NotFound);
		await Status(client, "/api-extra/items", HttpStatusCode.NotFound);
		var addresses = application.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>().Addresses;
		Assert.Equal(new[] { $"http://127.0.0.1:{port}", $"http://127.0.0.1:{second}" }.Order(StringComparer.Ordinal), addresses.Order(StringComparer.Ordinal));
		Assert.Equal($"http://127.0.0.1:{port}/api", application.Configuration["Kestrel:Endpoints:api:Url"]);
		await application.StopAsync();
	}

	private static async Task FactoryPipelineGuardsInitializersStaticFilesAndRoutes(EndpointTestScope scope)
	{
		var port = EndpointTestScope.Port();
		scope.Write("web.option", EndpointTestScope.Endpoint("public", $"http://127.0.0.1:{port}/public"));
		scope.Write("wwwroot/public/data.txt", "public-file");
		scope.Write("wwwroot/outside/data.txt", "private-file");
		var initializer = new EarlyResponseInitializer();
		await using var application = await ApplicationAsync(scope, builder => builder.Services.AddSingleton<Zongsoft.Services.IApplicationInitializer<IApplicationBuilder>>(initializer), endpoints: false);
		application.MapGet("/public/route", () => "public-route");
		application.MapGet("/outside/route", () => "private-route");
		await application.StartAsync();
		using var client = Client(port);
		Assert.Equal("early:http", await client.GetStringAsync("/public/early"));
		await Status(client, "/outside/early", HttpStatusCode.NotFound);
		Assert.Equal(1, initializer.Calls);
		Assert.Equal("public-file", await client.GetStringAsync("/public/data.txt"));
		await Status(client, "/outside/data.txt", HttpStatusCode.NotFound);
		Assert.Equal("public-route", await client.GetStringAsync("/public/route"));
		await Status(client, "/outside/route", HttpStatusCode.NotFound);
		await application.StopAsync();
	}

	private static async Task AutomaticIntegrationInstallsRangeMiddleware(EndpointTestScope scope)
	{
		var port = EndpointTestScope.Port();
		scope.Plugin("Feature", EndpointTestScope.Endpoint("api", $"http://127.0.0.1:{port}/api"));
		await using var application = await Builder(scope, endpoints: false).BuildAsync();
		application.Run(context => context.Response.WriteAsync(context.Features.Get<WebEndpointFeature>().Endpoint.Address.Path));
		await application.StartAsync();
		using var client = Client(port);
		Assert.Equal("/api", await client.GetStringAsync("/api/items"));
		await Status(client, "/outside", HttpStatusCode.NotFound);
		await application.StopAsync();
	}

	private static async Task FinalProjectionOverridesEarlierPostConfiguration(EndpointTestScope scope)
	{
		var port = EndpointTestScope.Port();
		var alternate = EndpointTestScope.Port();
		await using var application = await Builder(scope, builder =>
		{
			ConfigureEndpoint(builder.Configuration, port);
			builder.Services.PostConfigure<KestrelServerOptions>(options =>
			{
				var configuration = new ConfigurationManager();
				configuration["Endpoints:other:Url"] = $"http://127.0.0.1:{alternate}";
				configuration["Endpoints:other:Protocols"] = "Http1";
				options.Configure(configuration);
			});
		}).BuildAsync();
		application.Run(context => context.Response.WriteAsync("final"));
		await application.StartAsync();
		using var client = Client(port);
		Assert.Equal("final", await client.GetStringAsync("/api/item"));
		AssertPortAvailable(alternate);
		Assert.Equal($"http://127.0.0.1:{port}", Assert.Single(application.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>().Addresses));
		await application.StopAsync();
	}

	private static async Task IPv6SocketProjectionPreservesAddressFamily(EndpointTestScope scope)
	{
		var port = EndpointTestScope.Port();
		await using var application = await Builder(scope, builder =>
		{
			builder.Configuration["Kestrel:Endpoints:ipv6:Url"] = $"http://[::]:{port}/ipv6";
			builder.Configuration["Kestrel:Endpoints:ipv6:Protocols"] = "http1";
		}).BuildAsync();
		var options = application.Services.GetRequiredService<IOptions<SocketTransportOptions>>().Value;
		using var socket = options.CreateBoundListenSocket(new IPEndPoint(IPAddress.IPv6Any, port));
		Assert.False(socket.DualMode);
		Assert.Equal(new IPEndPoint(IPAddress.IPv6Any, port), socket.LocalEndPoint);
		using var ipv4 = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		ipv4.Bind(new IPEndPoint(IPAddress.Loopback, port));
		Assert.Equal(new IPEndPoint(IPAddress.Loopback, port), ipv4.LocalEndPoint);
	}

	private static async Task EmptyExplicitEndpointsRejectRequests(EndpointTestScope scope)
	{
		var port = EndpointTestScope.Port();
		await using var application = await Builder(scope, builder => builder.WebHost.UseUrls($"http://127.0.0.1:{port}")).BuildAsync();
		Assert.Empty(application.Services.GetRequiredService<WebEndpointCollection>());
		application.Run(context => context.Response.WriteAsync("unreachable"));
		await application.StartAsync();
		using var client = Client(port);
		await Status(client, "/anything", HttpStatusCode.NotFound);
		await application.StopAsync();
	}

	private static async Task NativeBuilderUsesAddressSourcesWithoutEndpointServices(EndpointTestScope scope, string key)
	{
		var port = EndpointTestScope.Port();
		await using var application = await Builder(scope, builder => builder.Configuration[key] = key == "urls" ? $"http://127.0.0.1:{port}" : port.ToString(), endpoints: false).BuildAsync();
		Assert.Null(application.Services.GetService<WebEndpointCollection>());
		Assert.Equal(key == "urls" ? $"http://127.0.0.1:{port}" : port.ToString(), application.Configuration[key]);

		//HTTPS 的原生证书加载由服务器负责，此处只验证构建器不禁止该地址来源。
		if(key == "https_ports")
			return;
		application.Run(context => context.Response.WriteAsync("native"));
		await application.StartAsync();
		using var client = Client(port);
		Assert.Equal("native", await client.GetStringAsync("/outside"));
		await application.StopAsync();
	}

	private static async Task NativeBuilderUsesLateUrls(EndpointTestScope scope)
	{
		var port = EndpointTestScope.Port();
		await using var application = await ApplicationAsync(scope, endpoints: false);
		application.Urls.Add($"http://127.0.0.1:{port}");
		application.Run(context => context.Response.WriteAsync("native"));
		await application.StartAsync();
		using var client = Client(port);
		Assert.Equal("native", await client.GetStringAsync("/outside"));
		Assert.Null(application.Services.GetService<WebEndpointCollection>());
		await application.StopAsync();
	}

	private static async Task ExtraCodeListenerStartsButRequestsAreRejected(EndpointTestScope scope)
	{
		var port = EndpointTestScope.Port();
		var extra = EndpointTestScope.Port();
		await using var application = await Builder(scope, builder =>
		{
			ConfigureEndpoint(builder.Configuration, port);
			builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, extra, listener => listener.Protocols = HttpProtocols.Http1));
		}).BuildAsync();
		application.Run(context => context.Response.WriteAsync("declared"));
		await application.StartAsync();
		using var client = Client(port);
		using var other = Client(extra);
		Assert.Equal("declared", await client.GetStringAsync("/api/item"));
		await Status(other, "/api/item", HttpStatusCode.NotFound);
		Assert.Equal(2, application.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>().Addresses.Count);
		await application.StopAsync();
	}

	private static async Task NativeBuilderUsesCodeListeners(EndpointTestScope scope)
	{
		var port = EndpointTestScope.Port();
		await using var application = await Builder(scope, builder => builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, port)), endpoints: false).BuildAsync();
		application.Run(context => context.Response.WriteAsync("code-listener"));
		await application.StartAsync();
		using var client = Client(port);
		Assert.Equal("code-listener", await client.GetStringAsync("/outside"));
		Assert.Null(application.Services.GetService<WebEndpointCollection>());
		await application.StopAsync();
	}

	private static async Task MissingDeclarationsKeepNativePluginConfiguration(EndpointTestScope scope)
	{
		scope.Plugin("Alpha", "<options><option path='Kestrel'><fixture value='alpha' /></option></options>");
		scope.Plugin("Beta", "<options><option path='Kestrel'><fixture value='beta' /></option></options>");
		await using var application = await Builder(scope, endpoints: false).BuildAsync();
		Assert.Null(application.Services.GetService<WebEndpointCollection>());
		Assert.Contains(application.Configuration["Kestrel:fixture:value"], new[] { "alpha", "beta" });
		Assert.Empty(application.Configuration.GetSection("Kestrel:Endpoints").GetChildren());
	}

	private static async Task CallerSelectedConfigurationSectionIsSupported(EndpointTestScope scope)
	{
		var port = EndpointTestScope.Port();
		var other = EndpointTestScope.Port();
		scope.Plugin("Feature", EndpointTestScope.Endpoint("api", $"http://127.0.0.1:{port}/custom").Replace("Kestrel/Endpoints", "Server/Endpoints", StringComparison.Ordinal));
		scope.Plugin("Default", EndpointTestScope.Endpoint("other", $"http://127.0.0.1:{other}/default"));
		await using var application = await Builder(scope, builder => builder.Services.AddWebEndpoints(builder.Configuration, "Server"), endpoints: false).BuildAsync();
		application.Run(context => context.Response.WriteAsync("custom-section"));
		await application.StartAsync();
		using var client = Client(port);
		Assert.Equal("custom-section", await client.GetStringAsync("/custom/item"));
		await Status(client, "/outside", HttpStatusCode.NotFound);
		Assert.Equal("/custom", Assert.Single(application.Services.GetRequiredService<WebEndpointCollection>()).Address.Path);
		Assert.Equal($"http://127.0.0.1:{other}/default", application.Configuration["Kestrel:Endpoints:other:Url"]);
		Assert.Equal($"http://127.0.0.1:{port}", Assert.Single(application.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>().Addresses));
		AssertPortAvailable(other);
		await application.StopAsync();
	}

	private static HttpClient Client(int port) => new() { BaseAddress = new Uri($"http://127.0.0.1:{port}"), Timeout = TimeSpan.FromSeconds(10) };

	private static async Task Status(HttpClient client, string path, HttpStatusCode expected)
	{
		using var response = await client.GetAsync(path);
		Assert.Equal(expected, response.StatusCode);
	}

	private static void AssertPortAvailable(int port)
	{
		using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		socket.Bind(new IPEndPoint(IPAddress.Loopback, port));
		Assert.Equal(port, ((IPEndPoint)socket.LocalEndPoint).Port);
	}

	private sealed class EarlyResponseInitializer : Zongsoft.Services.IApplicationInitializer<IApplicationBuilder>
	{
		public int Calls { get; private set; }

		public void Initialize(IApplicationBuilder application) => application.Use(async (context, next) =>
		{
			if(context.Request.Path.Value.EndsWith("/early", StringComparison.Ordinal))
			{
				this.Calls++;
				await context.Response.WriteAsync("early:" + context.Features.Get<WebEndpointFeature>().Kind);
			}
			else
				await next(context);
		});
	}
}
