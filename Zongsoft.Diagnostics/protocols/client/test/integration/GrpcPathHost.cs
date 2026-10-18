using System;
using System.Net;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Grpc.Core;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

using Zongsoft.Diagnostics.Protocols.Client.IntegrationTests.Contracts;

namespace Zongsoft.Diagnostics.Protocols.Client.IntegrationTests;

internal sealed class GrpcPathHost(WebApplication application, Uri address, GrpcPathProbe probe) : IAsyncDisposable
{
	public Uri Address { get; } = address;
	public GrpcPathProbe Probe { get; } = probe;

	public static async Task<GrpcPathHost> StartAsync()
	{
		var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ContentRootPath = AppContext.BaseDirectory, EnvironmentName = "Development" });
		builder.Configuration.Sources.Clear();
		builder.Logging.ClearProviders();
		builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0, listener => listener.Protocols = HttpProtocols.Http2));

		var probe = new GrpcPathProbe();
		builder.Services.AddSingleton(probe);
		builder.Services.AddGrpc(options =>
		{
			options.MaxReceiveMessageSize = 8 * 1024 * 1024;
			options.MaxSendMessageSize = 8 * 1024 * 1024;
		});
		var application = builder.Build();

		try
		{
			application.Use(async (context, next) =>
			{
				probe.Paths.Enqueue(context.Request.Path.Value);

				if(!context.Request.Path.StartsWithSegments("/telemetry", out var remainder))
				{
					context.Response.StatusCode = StatusCodes.Status404NotFound;
					return;
				}

				context.Request.PathBase = "/telemetry";
				context.Request.Path = remainder;
				await next(context);
			});
			application.UseRouting();
			application.MapGrpcService<GrpcPathService>();
			await application.StartAsync();
			var addresses = application.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>().Addresses;
			return new GrpcPathHost(application, new Uri(addresses.Single()), probe);
		}
		catch
		{
			await application.DisposeAsync();
			throw;
		}
	}

	public async ValueTask DisposeAsync()
	{
		this.Probe.AllowServerNext.TrySetResult();
		using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));

		try
		{
			await application.StopAsync(timeout.Token);
		}
		finally
		{
			await application.DisposeAsync();
		}
	}
}

internal sealed class GrpcPathProbe
{
	public ConcurrentQueue<string> Paths { get; } = new();
	public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
	public TaskCompletionSource Cancelled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
	public TaskCompletionSource FirstUploadReceived { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
	public TaskCompletionSource AllowServerNext { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
}

internal sealed class GrpcPathService(GrpcPathProbe probe) : PathProbe.PathProbeBase
{
	public override async Task<Frame> Unary(Frame request, ServerCallContext context)
	{
		if(request.Text == "wait")
		{
			probe.Started.TrySetResult();

			try
			{
				await Task.Delay(Timeout.Infinite, context.CancellationToken);
			}
			finally
			{
				probe.Cancelled.TrySetResult();
			}
		}

		if(request.Text == "fail")
			throw new RpcException(new Status(StatusCode.PermissionDenied, "fixture-denied"), new Metadata { { "failure", "retained" } });

		var httpRequest = context.GetHttpContext().Request;
		await context.WriteResponseHeadersAsync(new Metadata
		{
			{ "request-id", context.RequestHeaders.GetValue("request-id") ?? "none" },
			{ "observed-path", httpRequest.PathBase + httpRequest.Path },
		});
		context.ResponseTrailers.Add("fixture-trailer", "retained");
		var response = request.Clone();
		response.Text = "reply:" + request.Text;
		return response;
	}

	public override async Task ServerStream(Frame request, IServerStreamWriter<Frame> responseStream, ServerCallContext context)
	{
		await responseStream.WriteAsync(new Frame { Text = request.Text, Sequence = 1 });
		await probe.AllowServerNext.Task.WaitAsync(context.CancellationToken);
		await responseStream.WriteAsync(new Frame { Text = request.Text, Sequence = 2 });
		context.ResponseTrailers.Add("fixture-trailer", "server-stream");
	}

	public override async Task<Frame> ClientStream(IAsyncStreamReader<Frame> requestStream, ServerCallContext context)
	{
		var values = new List<string>();

		while(await requestStream.MoveNext(context.CancellationToken))
		{
			values.Add(requestStream.Current.Text);
			probe.FirstUploadReceived.TrySetResult();
		}

		context.ResponseTrailers.Add("fixture-trailer", "client-stream");
		return new Frame { Text = string.Join('|', values), Sequence = values.Count };
	}

	public override async Task Duplex(IAsyncStreamReader<Frame> requestStream, IServerStreamWriter<Frame> responseStream, ServerCallContext context)
	{
		while(await requestStream.MoveNext(context.CancellationToken))
		{
			var response = requestStream.Current.Clone();
			response.Text = "reply:" + response.Text;
			await responseStream.WriteAsync(response);
		}

		context.ResponseTrailers.Add("fixture-trailer", "duplex");
	}
}
