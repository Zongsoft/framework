extern alias client;

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Xml.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;
using System.Collections.Generic;

using Grpc.Core;
using Grpc.Net.Client;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Zongsoft.Components;
using Zongsoft.Collections;
using Zongsoft.Web.Hosting;

using Xunit;

namespace Zongsoft.Diagnostics.Protocols.Server.Tests;

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
		var plugins = Path.Combine(directory, "plugins", "telemetry");
		Directory.CreateDirectory(plugins);
		foreach(var file in Directory.EnumerateFiles(Path.Combine(AppContext.BaseDirectory, "fixtures")))
			File.Copy(file, Path.Combine(Path.GetFileName(file) == "Main.plugin" ? Path.GetDirectoryName(plugins) : plugins, Path.GetFileName(file)));

		foreach(var assembly in new[] { typeof(Zongsoft.Web.Grpc.GrpcInitializer).Assembly, typeof(Listener).Assembly, typeof(Samples.MetricHandler).Assembly })
			File.Copy(assembly.Location, Path.Combine(plugins, Path.GetFileName(assembly.Location)));

		var mainPath = Path.Combine(directory, "plugins", "Main.plugin");
		var main = XDocument.Load(mainPath);
		main.Root.Elements("extension").Remove();
		main.Save(mainPath);

		using var reservation = new TcpListener(IPAddress.Loopback, 0);
		reservation.Start();
		var port = ((IPEndPoint)reservation.LocalEndpoint).Port;
		reservation.Stop();
		var optionPath = Path.Combine(plugins, "Zongsoft.Diagnostics.Protocols.Server.option");
		var option = XDocument.Load(optionPath);
		var endpoint = Assert.Single(option.Descendants("telemetry"));
		var original = (string)endpoint.Attribute("url");
		Assert.Equal("http://*:4317/opentelemetry", original);
		endpoint.SetAttributeValue("url", $"http://127.0.0.1:{port}/opentelemetry");
		option.Save(optionPath);

		var application = Zongsoft.Web.Application.Web(typeof(TestProgram).Assembly.GetName().Name,
			[$"--contentRoot={directory}", "--environment=Development"], builder =>
			{
				builder.Logging.ClearProviders();
				builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>
				{
					["urls"] = "",
					["http_ports"] = "",
					["https_ports"] = "",
				});
			});

		var host = new OtlpTestHost(application, port);

		try
		{
			var context = application.Services.GetRequiredService<Zongsoft.Web.WebApplicationContext>();
			context.Workbench.Closing += (_, args) => args.Cancel = true;
			host._sample = Assert.Single(Listener.Metrics.Handlers.OfType<Samples.MetricHandler>());
			Listener.Metrics.Handlers.Add(host.Metrics);
			Listener.Traces.Handlers.Add(host.Traces);
			Listener.Logs.Handlers.Add(host.Logs);
			var endpoints = application.Services.GetRequiredService<WebEndpointCollection>();
			Assert.Single(endpoints);
			Assert.Equal($"http://127.0.0.1:{port}/opentelemetry", application.Configuration["Kestrel:Endpoints:telemetry:Url"]);
			Assert.Equal("grpc+http2", application.Configuration["Kestrel:Endpoints:telemetry:Protocols"]);
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
