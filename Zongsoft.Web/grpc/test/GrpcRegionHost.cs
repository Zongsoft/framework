using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Grpc.Core;
using Grpc.Net.Client;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

using Zongsoft.Services;
using Zongsoft.Components;
using Zongsoft.Communication;
using Zongsoft.Web.Hosting;

using P = Zongsoft.Web.Grpc.Tests.Protocols;

namespace Zongsoft.Web.Grpc.Tests;

internal sealed class GrpcRegionHost(WebApplication application, RegionProbe probe) : IAsyncDisposable
{
	private readonly List<GrpcChannel> _channels = [];
	public RegionProbe Probe { get; } = probe;
	public IServiceProvider Services => application.Services;

	public static int Port()
	{
		using var listener = new TcpListener(IPAddress.Loopback, 0);
		listener.Start();
		return ((IPEndPoint)listener.LocalEndpoint).Port;
	}

	public static async Task<GrpcRegionHost> StartAsync(Type[] types, (int Port, string Path)[] endpoints, bool authorize = false, Action<IServiceCollection> configure = null)
	{
		var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ContentRootPath = AppContext.BaseDirectory, EnvironmentName = "Development" });
		builder.Configuration.Sources.Clear();
		builder.Logging.ClearProviders();
		builder.Host.UseServiceProviderFactory(new ServiceProviderFactory());
		builder.WebHost.ConfigureKestrel(options =>
		{
			foreach(var port in endpoints.Select(endpoint => endpoint.Port).Distinct())
				options.Listen(IPAddress.Loopback, port, listener => listener.Protocols = HttpProtocols.Http2);
		});

		builder.Services.Register(typeof(GrpcInitializer).Assembly, builder.Configuration);
		builder.Services.Register(new SelectedAssembly(types), builder.Configuration);

		var probe = new RegionProbe();
		builder.Services.AddSingleton(probe);
		builder.Services.AddGrpc(options =>
		{
			options.MaxReceiveMessageSize = 8 * 1024 * 1024;
			options.MaxSendMessageSize = 8 * 1024 * 1024;
		});

		builder.Services.AddWebEndpoints(endpoints.Select((endpoint, index) => new WebEndpoint($"listener{index}", NetworkAddress.Parse($"http://127.0.0.1:{endpoint.Port}{endpoint.Path}"), NetworkProtocol.Parse("grpc+http2", ','))));

		if(authorize)
		{
			builder.Services.AddAuthentication("fixture").AddScheme<AuthenticationSchemeOptions, FixtureAuthenticationHandler>("fixture", _ => { });
			builder.Services.AddAuthorization(options => options.FallbackPolicy = new AuthorizationPolicyBuilder("fixture").RequireAuthenticatedUser().Build());
		}

		configure?.Invoke(builder.Services);
		var application = builder.Build();

		try
		{
			application.UseWebEndpoints();
			application.UseRouting();

			if(authorize)
			{
				application.UseAuthentication();
				application.UseAuthorization();
			}

			new GrpcInitializer().Initialize(application);
			await application.StartAsync();
			return new GrpcRegionHost(application, probe);
		}
		catch
		{
			await application.DisposeAsync();
			throw;
		}
	}

	public GrpcChannel Channel(int port, string path, string query = null)
	{
		var channel = GrpcChannel.ForAddress($"http://127.0.0.1:{port}", new GrpcChannelOptions
		{
			HttpHandler = new PrefixHandler(path, query, this.Probe),
			DisposeHttpClient = true,
			MaxReceiveMessageSize = 8 * 1024 * 1024,
			MaxSendMessageSize = 8 * 1024 * 1024,
		});

		_channels.Add(channel);
		return channel;
	}

	public async ValueTask DisposeAsync()
	{
		foreach(var channel in _channels)
			channel.Dispose();

		await application.StopAsync();
		await application.DisposeAsync();
	}

	private sealed class SelectedAssembly(Type[] types) : Assembly
	{
		public override IEnumerable<TypeInfo> DefinedTypes => types.Select(type => type.GetTypeInfo());
	}

	private sealed class PrefixHandler(string path, string query, RegionProbe probe) : DelegatingHandler(new SocketsHttpHandler())
	{
		protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			request.RequestUri = new Uri(request.RequestUri.GetLeftPart(UriPartial.Authority) + path.TrimEnd('/') + request.RequestUri.PathAndQuery + query);
			var response = await base.SendAsync(request, cancellationToken);
			probe.Responses.Enqueue(response.StatusCode);
			return response;
		}
	}

	private sealed class FixtureAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
	{
		protected override Task<AuthenticateResult> HandleAuthenticateAsync() => Task.FromResult(
			this.Request.Headers.Authorization == "Bearer fixture" ?
			AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "fixture")], "fixture")), "fixture")) :
			AuthenticateResult.NoResult());
	}
}

public sealed class RegionProbe
{
	public ConcurrentQueue<HttpStatusCode> Responses { get; } = new();
	public ConcurrentDictionary<string, int> Created { get; } = new();
	public ConcurrentDictionary<string, int> Disposed { get; } = new();
	public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
	public TaskCompletionSource Cancelled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
}

public static class GrpcRegionServices
{
	public abstract class EchoHandler : P.Echo.EchoBase, IDisposable
	{
		private readonly string _name;
		private readonly RegionProbe _probe;
		private readonly string _instance = Guid.NewGuid().ToString("N");

		protected EchoHandler(RegionProbe probe, string name)
		{
			_probe = probe;
			_name = name;
			probe.Created.AddOrUpdate(name, 1, (_, count) => count + 1);
		}

		public override async Task<P.Frame> Unary(P.Frame request, ServerCallContext context)
		{
			if(request.Text == "wait")
			{
				_probe.Started.TrySetResult();

				try
				{
					await Task.Delay(Timeout.Infinite, context.CancellationToken);
				}
				finally
				{
					_probe.Cancelled.TrySetResult();
				}
			}

			if(request.Text == "fail")
				throw new RpcException(new Status(StatusCode.PermissionDenied, "fixture-denied"), new Metadata { { "failure", "retained" } });

			await context.WriteResponseHeadersAsync(new Metadata
			{
				{ "request-id", context.RequestHeaders.GetValue("request-id") ?? "none" },
				{ "query", context.GetHttpContext().Request.Query["fixture"].ToString() },
			});
			context.ResponseTrailers.Add("instance", _instance);
			return this.Reply(request);
		}

		public override async Task ServerStream(P.Frame request, IServerStreamWriter<P.Frame> responseStream, ServerCallContext context)
		{
			for(var index = 0; index < 3; index++)
				await responseStream.WriteAsync(this.Reply(new P.Frame { Text = request.Text + index }));
		}

		public override async Task<P.Frame> ClientStream(IAsyncStreamReader<P.Frame> requestStream, ServerCallContext context)
		{
			var messages = new List<string>();

			while(await requestStream.MoveNext(context.CancellationToken))
				messages.Add(requestStream.Current.Text);

			return this.Reply(new P.Frame { Text = string.Join(',', messages) });
		}

		public override async Task Duplex(IAsyncStreamReader<P.Frame> requestStream, IServerStreamWriter<P.Frame> responseStream, ServerCallContext context)
		{
			while(await requestStream.MoveNext(context.CancellationToken))
				await responseStream.WriteAsync(this.Reply(requestStream.Current));
		}

		private P.Frame Reply(P.Frame request) => new() { Text = _name + ":" + request.Text, Payload = request.Payload, Instance = _instance };
		public void Dispose() => _probe.Disposed.AddOrUpdate(_name, 1, (_, count) => count + 1);
	}

	[Service(Tags = "gRPC"), Handler("/a"), Handler("/mirror"), Handler("/a")]
	public sealed class Alpha(RegionProbe probe) : EchoHandler(probe, "alpha") { }

	[Service(Tags = "gRPC"), Handler("/b")]
	public sealed class Beta(RegionProbe probe) : EchoHandler(probe, "beta") { }

	[Service(Tags = "gRPC"), Handler("/a")]
	public sealed class Auxiliary : P.Auxiliary.AuxiliaryBase
	{
		public override Task<P.Frame> Ping(P.Frame request, ServerCallContext context) => Task.FromResult(new P.Frame { Text = "auxiliary" });
	}

	[Service(Tags = "gRPC"), Handler("/a")]
	public sealed class Third : P.Third.ThirdBase
	{
		public override Task<P.Frame> Ping(P.Frame request, ServerCallContext context) => Task.FromResult(new P.Frame { Text = "third" });
	}

	[Service(Tags = "gRPC"), Handler("/c")]
	public sealed class Other : P.Other.OtherBase
	{
		public override Task<P.Frame> Ping(P.Frame request, ServerCallContext context) => Task.FromResult(new P.Frame { Text = "other" });
	}

	[Service(Tags = "gRPC"), Handler("/a/child")]
	public sealed class Child : P.Other.OtherBase
	{
		public override Task<P.Frame> Ping(P.Frame request, ServerCallContext context) => Task.FromResult(new P.Frame { Text = "child" });
	}

	[Service(Tags = "gRPC"), Handler("/")]
	public sealed class Root(RegionProbe probe) : EchoHandler(probe, "root") { }

	[Service(Tags = "gRPC"), Handler("/a/")]
	public sealed class Trailing(RegionProbe probe) : EchoHandler(probe, "trailing") { }

	[Handler("/inherited")]
	public abstract class InheritedHandler(RegionProbe probe) : EchoHandler(probe, "inherited") { }

	[Service(Tags = "gRPC")]
	public sealed class Inherited(RegionProbe probe) : InheritedHandler(probe) { }

	[Service(Tags = "gRPC")]
	public sealed class MissingHandler(RegionProbe probe) : EchoHandler(probe, "missing") { }

	[Service(Tags = "gRPC"), Handler("/missing")]
	public sealed class MissingEndpoint(RegionProbe probe) : EchoHandler(probe, "missing") { }

	[Service(Tags = "gRPC"), Handler("/a")]
	public sealed class Duplicate(RegionProbe probe) : EchoHandler(probe, "duplicate") { }

	[Service(Tags = "gRPC"), Handler("relative")]
	public sealed class InvalidPath(RegionProbe probe) : EchoHandler(probe, "invalid") { }

	[Service(Tags = "gRPC"), Handler("/a", 1)]
	public sealed class Ordered(RegionProbe probe) : EchoHandler(probe, "ordered") { }

	[Service(Tags = "gRPC"), Handler("/A")]
	public sealed class CaseMismatch(RegionProbe probe) : EchoHandler(probe, "case") { }

	[Service(Tags = "gRPC"), Handler("/a/")]
	public sealed class SlashMismatch(RegionProbe probe) : EchoHandler(probe, "slash") { }

	[Service(Tags = "gRPC"), Handler("/a")]
	public sealed class NotGrpc { }
}
