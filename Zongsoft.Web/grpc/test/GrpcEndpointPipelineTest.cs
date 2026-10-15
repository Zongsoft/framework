using System;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Xunit;

using Zongsoft.Web.Hosting;

namespace Zongsoft.Web.Grpc.Tests;

public class GrpcEndpointPipelineTest
{
	[Theory]
	[InlineData("/a/demo.Service/Call", "application/grpc", "a")]
	[InlineData("/a/demo.Service/Call?q=/b/", "Application/GRPC+proto; charset=utf-8", "a")]
	[InlineData("/a/deeper/value", "application/grpc", "a")]
	[InlineData("/a/deeper/value", "application/json", "http-deeper")]
	[InlineData("/a/demo.Service/Call", "application/grpc-web+proto", "http")]
	[InlineData("/a/demo.Service/Call", "application/grpc+", "http")]
	[InlineData("/a-extra/demo.Service/Call", "application/grpc", "grpc-root")]
	[InlineData("/a/child/demo.Service/Call", "application/grpc", "child")]
	[InlineData("/a/child", "application/grpc", "child")]
	[InlineData("/A/demo.Service/Call", "application/grpc", "a")]
	[InlineData("/%61/demo.Service/Call", "application/grpc", null)]
	[InlineData("/x/../a/demo.Service/Call", "application/grpc", "grpc-root")]
	[InlineData("https://untrusted.test/a/demo.Service/Call?q=1", "application/grpc", "a")]
	[InlineData("https://untrusted.test?q=1", "application/grpc", "grpc-root")]
	public async Task SelectByKindThenLongestOriginalPath(string target, string contentType, string expected)
	{
		using var pipeline = new Pipeline(Declarations(), application => application.Run(context =>
		{
			var feature = context.Features.Get<WebEndpointFeature>();
			context.Response.Headers["Selected"] = feature.Endpoint.Name;
			context.Response.Headers["OriginalPath"] = feature.Path;
			return Task.CompletedTask;
		}));

		var context = Request(target, contentType);
		context.Request.Host = new HostString("forged.test", 9999);
		context.Request.Headers["X-Forwarded-Host"] = "forged.test:9999";
		context.Request.Headers["X-Forwarded-Port"] = "9999";
		await pipeline.Invoke(context);

		Assert.Equal(expected == null ? 404 : 200, context.Response.StatusCode);
		Assert.Equal(expected ?? string.Empty, context.Response.Headers["Selected"].ToString());
		Assert.DoesNotContain('?', context.Response.Headers["OriginalPath"].ToString());

		if(expected == null)
			Assert.Null(context.Features.Get<WebEndpointFeature>());
	}

	[Theory]
	[InlineData("HTTP/1.1", "POST", "/unknown", 400)]
	[InlineData("HTTP/2", "GET", "/unknown", 405)]
	[InlineData("HTTP/2", "post", "/a/demo.Service/Call", 405)]
	[InlineData("HTTP/2", "POST", "/bad%xx", 400)]
	[InlineData("HTTP/2", "POST", "/bad%", 400)]
	[InlineData("HTTP/2", "POST", "/bad\\path", 400)]
	[InlineData("HTTP/2", "POST", "/bad#fragment", 400)]
	[InlineData("HTTP/2", "POST", "host.test:443", 400)]
	[InlineData("HTTP/2", "POST", "https://user@host.test/a", 400)]
	[InlineData("HTTP/2", "POST", "*", 400)]
	[InlineData("HTTP/2", "POST", null, 400)]
	[InlineData("HTTP/2", "POST", "", 400)]
	public async Task RejectInvalidGrpcRequestsBeforeOrdinaryHandlers(string protocol, string method, string target, int expected)
	{
		var called = false;
		using var pipeline = new Pipeline(Declarations(), application => application.Run(context =>
		{
			called = true;
			return Task.CompletedTask;
		}));

		var context = Request(target, "application/grpc");
		context.Request.Protocol = protocol;
		context.Request.Method = method;
		await pipeline.Invoke(context);

		Assert.Equal(expected, context.Response.StatusCode);
		Assert.False(called);
		Assert.Null(context.Features.Get<WebEndpointFeature>());
	}

	[Fact]
	public async Task DedicatedGrpcListenerRejectsOrdinaryRequestsAndUnknownRanges()
	{
		var declarations = new Dictionary<string, string>
		{
			["Endpoints:grpc:Url"] = "http://127.0.0.1:9000/a",
			["Endpoints:grpc:Protocols"] = "grpc",
		};

		var called = false;
		using var pipeline = new Pipeline(declarations, application => application.Run(context =>
		{
			called = true;
			return Task.CompletedTask;
		}));

		var ordinary = Request("/a/file.txt", "text/plain");
		ordinary.Request.Method = "GET";
		await pipeline.Invoke(ordinary);
		Assert.Equal(404, ordinary.Response.StatusCode);
		Assert.False(called);

		var grpc = Request("/outside/demo.Service/Call", "application/grpc");
		await pipeline.Invoke(grpc);
		Assert.Equal(200, grpc.Response.StatusCode);
		Assert.Equal("application/grpc", grpc.Response.ContentType);
		Assert.Equal("12", grpc.Response.Headers["grpc-status"].ToString());
		Assert.Equal(0, grpc.Response.Body.Length);
		Assert.False(called);
	}

	[Theory]
	[InlineData("127.0.0.2", 9000)]
	[InlineData("127.0.0.1", 9001)]
	[InlineData("::1", 9000)]
	[InlineData("::ffff:127.0.0.1", 9000)]
	public async Task RequestCannotSelectAnotherConnection(string address, int port)
	{
		using var pipeline = new Pipeline(Declarations(), application => application.Run(context => throw new InvalidOperationException("Unreachable.")));
		var context = Request("/", "text/plain");
		context.Connection.LocalIpAddress = IPAddress.Parse(address);
		context.Connection.LocalPort = port;
		await pipeline.Invoke(context);
		Assert.Equal(404, context.Response.StatusCode);
	}

	[Fact]
	public async Task SelectedRangeDoesNotFallBackForUnsupportedVersion()
	{
		var declarations = Declarations();
		declarations["Endpoints:child:Protocols"] = "grpc+http3";
		using var pipeline = new Pipeline(declarations, application => application.Run(context => throw new InvalidOperationException("Unreachable.")));
		var context = Request("/a/child/Service/Call", "application/grpc");
		await pipeline.Invoke(context);

		Assert.Equal(400, context.Response.StatusCode);
		Assert.Null(context.Features.Get<WebEndpointFeature>());
	}

	[Theory]
	[InlineData("/a/%64eeper/file", "/a/deeper/file", 404)]
	[InlineData("/a/deeper/../../outside", "/outside", 404)]
	[InlineData("/a/deeper/item%20name", "/a/deeper/item name", 200)]
	public async Task ParsedPathsCannotEscapeScopeBeforeEarlyHandlers(string target, string parsed, int expected)
	{
		var called = false;
		using var pipeline = new Pipeline(Declarations(), application => application.Run(context =>
		{
			called = true;
			return Task.CompletedTask;
		}));

		var context = Request(target, "text/plain");
		context.Request.Path = parsed;
		await pipeline.Invoke(context);
		Assert.Equal(expected, context.Response.StatusCode);
		Assert.Equal(expected == 200, called);
	}

	[Theory]
	[InlineData("/a/demo.Service/Call", "/a/demo.Service/Call", "application/grpc", "a")]
	[InlineData("/A/demo.Service/Call", "/A/demo.Service/Call", "application/grpc", "a")]
	[InlineData("/A/demo.Service/call", "/A/demo.Service/call", "application/grpc", "a-fallback")]
	[InlineData("/A/CHILD/demo.Service/Call", "/A/CHILD/demo.Service/Call", "application/grpc", "child")]
	[InlineData("/a/demo.service/Call", "/a/demo.service/Call", "application/grpc", "a-fallback")]
	[InlineData("/a/demo.Service/%43all", "/a/demo.Service/Call", "application/grpc", "a-fallback")]
	[InlineData("/x/../a/demo.Service/Call", "/a/demo.Service/Call", "application/grpc", null)]
	[InlineData("/a/child/demo.Service/Call", "/a/child/demo.Service/Call", "application/grpc", "child")]
	[InlineData("/a/child/demo.Service/Unknown", "/a/child/demo.Service/Unknown", "application/grpc", null)]
	[InlineData("/a/demo.Service/Call", "/a/demo.Service/Call", "text/plain", null)]
	[InlineData("/ordinary", "/ordinary", "text/plain", "ordinary")]
	[InlineData("/ordinary", "/ordinary", "application/grpc", null)]
	public async Task RoutingMetadataEnforcesRegionAndExactRpcPath(string target, string path, string contentType, string expected)
	{
		using var pipeline = new Pipeline(Declarations(), application =>
		{
			application.UseRouting();
			application.UseEndpoints(routes =>
			{
				routes.Map("/ordinary", Respond("ordinary"));
				routes.MapGroup("/a").WithMetadata(new WebEndpointMetadata("/a", "grpc")).Map("/demo.Service/Call", Respond("a"));
				routes.MapGroup("/a/child").WithMetadata(new WebEndpointMetadata("/a/child/", "grpc")).Map("/demo.Service/Call", Respond("child"));

				// 更短区域的兜底不能处理已选中子区域的未知方法。
				routes.Map("/a/{**unknown}", Respond("a-fallback")).WithMetadata(new WebEndpointMetadata("/a", "grpc"));
			});
		});

		var context = Request(target, contentType);
		context.Request.Path = path;
		await pipeline.Invoke(context);
		Assert.Equal(expected == null ? 404 : 200, context.Response.StatusCode);
		Assert.Equal(expected ?? string.Empty, context.Response.Headers["Selected"].ToString());
	}

	[Fact]
	public async Task OptionsStarDoesNotEnterPathMapping()
	{
		using var pipeline = new Pipeline(Declarations(), application => application.Run(context => throw new InvalidOperationException("Unreachable.")));
		var context = Request("*", null);
		context.Request.Method = "OPTIONS";
		await pipeline.Invoke(context);
		Assert.Equal(404, context.Response.StatusCode);
	}

	[Fact]
	public async Task RequestBodyHeadersQueryAndCancellationRemainOwnedByTheCaller()
	{
		var context = Request("/a/demo.Service/Call?key=value", "application/grpc");
		using var body = new MemoryStream([1, 2, 3, 4]);
		using var cancellation = new System.Threading.CancellationTokenSource();
		context.Request.Body = body;
		context.Request.QueryString = new QueryString("?key=value");
		context.Request.Headers["Custom"] = "preserved";
		context.RequestAborted = cancellation.Token;
		var originalPath = context.Request.Path;

		using var pipeline = new Pipeline(Declarations(), application => application.Run(request =>
		{
			Assert.Same(body, request.Request.Body);
			Assert.Equal(0, body.Position);
			Assert.Equal(originalPath, request.Request.Path);
			Assert.Equal("?key=value", request.Request.QueryString.Value);
			Assert.Equal("preserved", request.Request.Headers["Custom"].ToString());
			Assert.Equal(cancellation.Token, request.RequestAborted);
			return Task.CompletedTask;
		}));
		await pipeline.Invoke(context);
		Assert.True(body.CanRead);
		Assert.Equal("/a/demo.Service/Call", context.Features.Get<WebEndpointFeature>().Path);
	}

	[Fact]
	public void RegistrationAndPipelineOrderMustBeUnambiguous()
	{
		using var configuration = (ConfigurationRoot)new ConfigurationBuilder().AddInMemoryCollection(Declarations()).Build();
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddRouting();
		services.AddGrpcWebEndpoints();
		services.AddWebEndpoints(Declarations(configuration));
		Assert.Throws<InvalidOperationException>(() => services.AddWebEndpoints(Declarations(configuration)));

		using var provider = services.BuildServiceProvider();
		var application = new ApplicationBuilder(provider);
		application.UseRouting();
		Assert.Throws<InvalidOperationException>(() => application.UseWebEndpoints());
		var duplicate = new ApplicationBuilder(provider);
		duplicate.UseWebEndpoints();
		Assert.Throws<InvalidOperationException>(() => duplicate.UseWebEndpoints());
	}

	[Fact]
	public async Task IsolatedKestrelUsesProjectedListenersAndPreservesConfiguration()
	{
		var webPort = FreePort();
		var grpcPort = FreePort();
		while(grpcPort == webPort)
			grpcPort = FreePort();

		var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = [], EnvironmentName = "Production" });
		builder.Configuration.Sources.Clear();
		builder.Logging.ClearProviders();
		builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>
		{
			["Kestrel:Endpoints:web:Url"] = $"http://127.0.0.1:{webPort}/ordinary",
			["Kestrel:Endpoints:web:Protocols"] = "http1",
			["Kestrel:Endpoints:a:Url"] = $"http://127.0.0.1:{grpcPort}/a",
			["Kestrel:Endpoints:a:Protocols"] = "grpc",
			["Kestrel:Endpoints:b:Url"] = $"http://127.0.0.1:{grpcPort}/b",
			["Kestrel:Endpoints:b:Protocols"] = "grpc",
		});

		builder.Services.AddGrpcWebEndpoints();
		var rules = builder.Services.Where(descriptor => descriptor.ServiceType == typeof(WebEndpointProtocol))
			.Select(descriptor => (WebEndpointProtocol)descriptor.ImplementationInstance);
		using var projection = KestrelUtility.Convert(builder.Configuration.GetSection("Kestrel"), out var declarations, rules);
		var endpoints = builder.Services.AddWebEndpoints(declarations);
		builder.WebHost.ConfigureKestrel(options => options.Configure(projection, reloadOnChange: false));
		builder.Services.Configure<SocketTransportOptions>(options => KestrelUtility.Configure(options, endpoints));
		await using var application = builder.Build();
		application.UseWebEndpoints();
		application.UseRouting();
		application.MapGet("/ordinary", () => "ordinary");
		application.MapPost("/a/demo.Service/Call", () => "a").WithMetadata(new WebEndpointMetadata("/a", "grpc"));
		application.MapPost("/b/demo.Service/Call", () => "b").WithMetadata(new WebEndpointMetadata("/b", "grpc"));
		await application.StartAsync();

		try
		{
			using var handler = new SocketsHttpHandler { UseProxy = false };
			using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };
			Assert.Equal("ordinary", await client.GetStringAsync($"http://127.0.0.1:{webPort}/ordinary"));
			using var request = new HttpRequestMessage(HttpMethod.Post, $"http://127.0.0.1:{grpcPort}/b/demo.Service/Call")
			{
				Version = HttpVersion.Version20,
				VersionPolicy = HttpVersionPolicy.RequestVersionExact,
				Content = new StringContent(string.Empty),
			};

			request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/grpc");
			using var response = await client.SendAsync(request);
			Assert.Equal(HttpStatusCode.OK, response.StatusCode);
			Assert.Equal(HttpVersion.Version20, response.Version);
			Assert.Equal("b", await response.Content.ReadAsStringAsync());
			Assert.Equal(2, application.Urls.Count);
			Assert.Equal($"http://127.0.0.1:{grpcPort}/a", builder.Configuration["Kestrel:Endpoints:a:Url"]);

			builder.Configuration["Kestrel:Endpoints:a:Url"] = $"http://127.0.0.1:{grpcPort}/changed";
			((IConfigurationRoot)builder.Configuration).Reload();
			Assert.Contains(endpoints, endpoint => endpoint.Name == "a" && endpoint.Address.Path == "/a");
			Assert.Equal("ordinary", await client.GetStringAsync($"http://127.0.0.1:{webPort}/ordinary"));
			using var afterReload = new HttpRequestMessage(HttpMethod.Post, $"http://127.0.0.1:{grpcPort}/a/demo.Service/Call")
			{
				Version = HttpVersion.Version20,
				VersionPolicy = HttpVersionPolicy.RequestVersionExact,
				Content = new StringContent(string.Empty),
			};

			afterReload.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/grpc");
			using var unchanged = await client.SendAsync(afterReload);
			Assert.Equal(HttpStatusCode.OK, unchanged.StatusCode);
			Assert.Equal("a", await unchanged.Content.ReadAsStringAsync());
			Assert.Equal(2, application.Urls.Count);
		}
		finally
		{
			await application.StopAsync();
		}
	}

	private static RequestDelegate Respond(string name) => context =>
	{
		context.Response.Headers["Selected"] = name;
		return Task.CompletedTask;
	};

	private static DefaultHttpContext Request(string target, string contentType)
	{
		var context = new DefaultHttpContext();
		context.Connection.LocalIpAddress = IPAddress.Loopback;
		context.Connection.LocalPort = 9000;
		context.Request.Method = "POST";
		context.Request.Protocol = "HTTP/2";
		context.Request.ContentType = contentType;
		context.Request.Path = target?.StartsWith('/') == true ? target.Split('?')[0] :
			Uri.TryCreate(target, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https" ? uri.AbsolutePath : "/";
		context.Features.Get<IHttpRequestFeature>().RawTarget = target;
		context.Response.Body = new MemoryStream();
		return context;
	}

	private static Dictionary<string, string> Declarations() => new()
	{
		["Endpoints:http:Url"] = "https://127.0.0.1:9000/",
		["Endpoints:http:Protocols"] = "http2",
		["Endpoints:http-deeper:Url"] = "https://127.0.0.1:9000/a/deeper",
		["Endpoints:http-deeper:Protocols"] = "http2",
		["Endpoints:grpc-root:Url"] = "https://127.0.0.1:9000/",
		["Endpoints:grpc-root:Protocols"] = "grpc",
		["Endpoints:a:Url"] = "https://127.0.0.1:9000/a",
		["Endpoints:a:Protocols"] = "grpc",
		["Endpoints:child:Url"] = "https://127.0.0.1:9000/a/child/",
		["Endpoints:child:Protocols"] = "grpc",
	};

	private static int FreePort()
	{
		using var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
		return ((IPEndPoint)listener.LocalEndPoint).Port;
	}

	private sealed class Pipeline : IDisposable
	{
		private readonly ServiceProvider _services;
		private readonly ConfigurationRoot _configuration;
		private readonly RequestDelegate _pipeline;

		public Pipeline(Dictionary<string, string> values, Action<IApplicationBuilder> configure)
		{
			_configuration = (ConfigurationRoot)new ConfigurationBuilder().AddInMemoryCollection(values).Build();
			var services = new ServiceCollection();
			services.AddLogging();
			services.AddRouting();
			services.AddSingleton(new DiagnosticListener("Zongsoft.Web.Tests"));
			services.AddGrpcWebEndpoints();
			services.AddWebEndpoints(Declarations(_configuration));
			_services = services.BuildServiceProvider();
			var application = new ApplicationBuilder(_services);
			application.UseWebEndpoints();
			configure(application);
			_pipeline = application.Build();
		}

		public Task Invoke(HttpContext context)
		{
			context.RequestServices = _services;
			return _pipeline(context);
		}

		public void Dispose()
		{
			_services.Dispose();
			_configuration.Dispose();
		}
	}
	private static IEnumerable<WebEndpoint> Declarations(IConfiguration configuration) => configuration.GetSection("Endpoints").GetChildren().Select(section =>
		new WebEndpoint(section.Key, Zongsoft.Communication.NetworkAddress.Parse(section["Url"]), Zongsoft.Communication.NetworkProtocol.Parse(section["Protocols"], ',')));

}
