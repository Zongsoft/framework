extern alias client;

using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;
using System.Collections.Generic;

using Grpc.Core;
using Grpc.Net.Client;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Zongsoft.Services;
using Zongsoft.Components;
using Zongsoft.Collections;
using Zongsoft.Communication;
using Zongsoft.Web.Grpc;
using Zongsoft.Web.Hosting;

namespace Zongsoft.Diagnostics.Protocols.Server.IntegrationTests;

internal sealed class OtlpTestHost(WebApplication application, int port) : IAsyncDisposable
{
	private readonly List<GrpcChannel> _channels = [];
	private Samples.MetricHandler _sample;
	public int Port { get; } = port;
	public string Address => $"http://127.0.0.1:{this.Port}";
	public RecordingHandler Metrics { get; } = new();
	public RecordingHandler Traces { get; } = new();
	public RecordingHandler Logs { get; } = new();

	public static async Task<OtlpTestHost> StartAsync(string directory)
	{
		using var reservation = new TcpListener(IPAddress.Loopback, 0);
		reservation.Start();
		var port = ((IPEndPoint)reservation.LocalEndpoint).Port;
		reservation.Stop();

		var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ContentRootPath = directory, EnvironmentName = "Development" });
		builder.Configuration.Sources.Clear();
		builder.Logging.ClearProviders();
		builder.Host.UseServiceProviderFactory(new ServiceProviderFactory());
		builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, port, listener => listener.Protocols = HttpProtocols.Http2));
		builder.Services.Register(typeof(GrpcInitializer).Assembly, builder.Configuration);
		builder.Services.Register(typeof(Listener).Assembly, builder.Configuration);
		builder.Services.AddWebEndpoints([new WebEndpoint("telemetry", NetworkAddress.Parse($"http://127.0.0.1:{port}/opentelemetry"), NetworkProtocol.Parse("grpc+http2", ','))]);
		var application = builder.Build();

		var host = new OtlpTestHost(application, port);

		try
		{
			application.UseWebEndpoints();
			application.UseRouting();
			new GrpcInitializer().Initialize(application);
			host._sample = new Samples.MetricHandler();
			Listener.Metrics.Handlers.Add(host._sample);
			Listener.Metrics.Handlers.Add(host.Metrics);
			Listener.Traces.Handlers.Add(host.Traces);
			Listener.Logs.Handlers.Add(host.Logs);
			await application.StartAsync();
			return host;
		}
		catch
		{
			await host.DisposeAsync();
			throw;
		}
	}

	public GrpcChannel Channel(string path = "/opentelemetry")
	{
		var channel = GrpcChannel.ForAddress(this.Address, new GrpcChannelOptions
		{
			HttpHandler = new client::Zongsoft.Diagnostics.Protocols.Client.GrpcClientHandler(path, new SocketsHttpHandler()),
			DisposeHttpClient = true,
		});
		_channels.Add(channel);
		return channel;
	}

	public async ValueTask DisposeAsync()
	{
		foreach(var channel in _channels)
			channel.Dispose();

		try
		{
			await application.StopAsync();
		}
		finally
		{
			try
			{
				await application.DisposeAsync();
			}
			finally
			{
				Listener.Metrics.Handlers.Remove(this.Metrics);
				Listener.Traces.Handlers.Remove(this.Traces);
				Listener.Logs.Handlers.Remove(this.Logs);

				if(_sample != null)
					Listener.Metrics.Handlers.Remove(_sample);
			}
		}
	}

	internal sealed class RecordingHandler : IHandler
	{
		private readonly Channel<Received> _messages = System.Threading.Channels.Channel.CreateUnbounded<Received>();
		private int _count;
		public int Count => Volatile.Read(ref _count);
		public bool Block { get; set; }
		public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource Cancelled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public ValueTask HandleAsync(object argument, CancellationToken cancellation = default) => this.HandleAsync(argument, null, cancellation);
		public async ValueTask HandleAsync(object argument, Parameters parameters, CancellationToken cancellation = default)
		{
			Interlocked.Increment(ref _count);
			await _messages.Writer.WriteAsync(new Received(argument, parameters), cancellation);
			this.Entered.TrySetResult();

			if(this.Block)
			{
				try
				{
					await Task.Delay(Timeout.Infinite, cancellation);
				}
				catch(OperationCanceledException)
				{
					this.Cancelled.TrySetResult();
					throw;
				}
			}
		}

		public Task<Received> ReadAsync() => _messages.Reader.ReadAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));
	}

	internal sealed record Received(object Argument, Parameters Parameters);
}
