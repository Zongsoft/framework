using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;

using Xunit;

using Zongsoft.Diagnostics.Protocols.Client.IntegrationTests.Contracts;

namespace Zongsoft.Diagnostics.Protocols.Client.IntegrationTests;

[Trait("Category", "Integration")]
public class GrpcClientHandlerIntegrationTest
{
	[Fact]
	public async Task UnaryPreservesLargeMessagesHeadersTrailersAndStatusAsync()
	{
		await using var host = await GrpcPathHost.StartAsync();
		using var handler = new GrpcClientHandler("/telemetry/", new SocketsHttpHandler { UseProxy = false });
		using var channel = GrpcChannel.ForAddress(host.Address, new GrpcChannelOptions
		{
			HttpHandler = handler,
			DisposeHttpClient = true,
			MaxSendMessageSize = 8 * 1024 * 1024,
			MaxReceiveMessageSize = 8 * 1024 * 1024,
		});
		var client = new PathProbe.PathProbeClient(channel);
		var payload = new byte[5 * 1024 * 1024];
		new Random(1729).NextBytes(payload);
		using var call = client.UnaryAsync(new Frame { Text = "large", Payload = ByteString.CopyFrom(payload), Sequence = 17 }, new Metadata { { "request-id", "trace-fixture" } }, Deadline());

		var response = await call.ResponseAsync;
		var headers = await call.ResponseHeadersAsync;

		Assert.Equal("reply:large", response.Text);
		Assert.Equal(17, response.Sequence);
		Assert.Equal(payload, response.Payload.ToByteArray());
		Assert.Equal("trace-fixture", headers.GetValue("request-id"));
		Assert.Equal("/telemetry/pathfixture.PathProbe/Unary", headers.GetValue("observed-path"));
		Assert.Equal("retained", call.GetTrailers().GetValue("fixture-trailer"));
		Assert.Equal(StatusCode.OK, call.GetStatus().StatusCode);
		Assert.Equal("/telemetry/pathfixture.PathProbe/Unary", Assert.Single(host.Probe.Paths));

		using var failure = client.UnaryAsync(new Frame { Text = "fail" }, deadline: Deadline());
		var error = await Assert.ThrowsAsync<RpcException>(() => failure.ResponseAsync);
		Assert.Equal(StatusCode.PermissionDenied, error.StatusCode);
		Assert.Equal("fixture-denied", error.Status.Detail);
		Assert.Equal("retained", error.Trailers.GetValue("failure"));
		Assert.Equal(StatusCode.PermissionDenied, failure.GetStatus().StatusCode);
	}

	[Fact]
	public async Task ServerStreamingDeliversBeforeProducerCompletesAsync()
	{
		await using var host = await GrpcPathHost.StartAsync();
		using var handler = new GrpcClientHandler("/telemetry", new SocketsHttpHandler { UseProxy = false });
		using var channel = GrpcChannel.ForAddress(host.Address, new GrpcChannelOptions { HttpHandler = handler, DisposeHttpClient = true });
		var client = new PathProbe.PathProbeClient(channel);
		using var call = client.ServerStream(new Frame { Text = "stream" }, deadline: Deadline());

		Assert.True(await call.ResponseStream.MoveNext(CancellationToken.None));
		Assert.Equal("stream", call.ResponseStream.Current.Text);
		Assert.Equal(1, call.ResponseStream.Current.Sequence);
		Assert.False(host.Probe.AllowServerNext.Task.IsCompleted);
		host.Probe.AllowServerNext.TrySetResult();
		Assert.True(await call.ResponseStream.MoveNext(CancellationToken.None));
		Assert.Equal(2, call.ResponseStream.Current.Sequence);
		Assert.False(await call.ResponseStream.MoveNext(CancellationToken.None));
		Assert.Equal("server-stream", call.GetTrailers().GetValue("fixture-trailer"));
		Assert.Equal(StatusCode.OK, call.GetStatus().StatusCode);
		Assert.Equal("/telemetry/pathfixture.PathProbe/ServerStream", Assert.Single(host.Probe.Paths));
	}

	[Fact]
	public async Task ClientStreamingReachesServerBeforeCompletionAsync()
	{
		await using var host = await GrpcPathHost.StartAsync();
		using var handler = new GrpcClientHandler("/telemetry", new SocketsHttpHandler { UseProxy = false });
		using var channel = GrpcChannel.ForAddress(host.Address, new GrpcChannelOptions { HttpHandler = handler, DisposeHttpClient = true });
		var client = new PathProbe.PathProbeClient(channel);
		using var call = client.ClientStream(deadline: Deadline());

		await call.RequestStream.WriteAsync(new Frame { Text = "first" });
		await host.Probe.FirstUploadReceived.Task.WaitAsync(TimeSpan.FromSeconds(10));
		Assert.False(call.ResponseAsync.IsCompleted);
		await call.RequestStream.WriteAsync(new Frame { Text = "second" });
		await call.RequestStream.CompleteAsync();
		var response = await call.ResponseAsync;

		Assert.Equal("first|second", response.Text);
		Assert.Equal(2, response.Sequence);
		Assert.Equal("client-stream", call.GetTrailers().GetValue("fixture-trailer"));
		Assert.Equal(StatusCode.OK, call.GetStatus().StatusCode);
		Assert.Equal("/telemetry/pathfixture.PathProbe/ClientStream", Assert.Single(host.Probe.Paths));
	}

	[Fact]
	public async Task DuplexStreamingRespondsBeforeRequestCompletionAsync()
	{
		await using var host = await GrpcPathHost.StartAsync();
		using var handler = new GrpcClientHandler("/telemetry", new SocketsHttpHandler { UseProxy = false });
		using var channel = GrpcChannel.ForAddress(host.Address, new GrpcChannelOptions { HttpHandler = handler, DisposeHttpClient = true });
		var client = new PathProbe.PathProbeClient(channel);
		using var call = client.Duplex(deadline: Deadline());

		for(var index = 1; index <= 2; index++)
		{
			await call.RequestStream.WriteAsync(new Frame { Text = "item" + index, Sequence = index });
			Assert.True(await call.ResponseStream.MoveNext(CancellationToken.None));
			Assert.Equal("reply:item" + index, call.ResponseStream.Current.Text);
			Assert.Equal(index, call.ResponseStream.Current.Sequence);
		}

		await call.RequestStream.CompleteAsync();
		Assert.False(await call.ResponseStream.MoveNext(CancellationToken.None));
		Assert.Equal("duplex", call.GetTrailers().GetValue("fixture-trailer"));
		Assert.Equal(StatusCode.OK, call.GetStatus().StatusCode);
		Assert.Equal("/telemetry/pathfixture.PathProbe/Duplex", Assert.Single(host.Probe.Paths));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task CancellationAndDeadlineReachServiceAsync(bool deadline)
	{
		await using var host = await GrpcPathHost.StartAsync();
		using var handler = new GrpcClientHandler("/telemetry", new SocketsHttpHandler { UseProxy = false });
		using var channel = GrpcChannel.ForAddress(host.Address, new GrpcChannelOptions { HttpHandler = handler, DisposeHttpClient = true });
		var client = new PathProbe.PathProbeClient(channel);
		using var cancellation = new CancellationTokenSource();
		using var call = client.UnaryAsync(new Frame { Text = "wait" }, deadline: deadline ? DateTime.UtcNow.AddSeconds(2) : Deadline(), cancellationToken: cancellation.Token);
		await host.Probe.Started.Task.WaitAsync(TimeSpan.FromSeconds(10));

		if(!deadline)
			cancellation.Cancel();

		var error = await Assert.ThrowsAsync<RpcException>(() => call.ResponseAsync);
		await host.Probe.Cancelled.Task.WaitAsync(TimeSpan.FromSeconds(10));
		Assert.Equal(deadline ? StatusCode.DeadlineExceeded : StatusCode.Cancelled, error.StatusCode);
		Assert.Equal(error.StatusCode, call.GetStatus().StatusCode);
		Assert.Equal("/telemetry/pathfixture.PathProbe/Unary", Assert.Single(host.Probe.Paths));
	}

	[Theory]
	[InlineData(false, false)]
	[InlineData(false, true)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public async Task ChannelOwnershipFollowsDisposeHttpClientAsync(bool disposeHttpClient, bool useHttpClient)
	{
		await using var host = await GrpcPathHost.StartAsync();
		using var terminal = new DisposalHandler(new SocketsHttpHandler { UseProxy = false });
		using var handler = new GrpcClientHandler("/telemetry", terminal);
		using var httpClient = useHttpClient ? new HttpClient(handler, true) : null;
		var options = new GrpcChannelOptions
		{
			HttpClient = httpClient,
			HttpHandler = useHttpClient ? null : handler,
			DisposeHttpClient = disposeHttpClient,
		};

		using(var channel = GrpcChannel.ForAddress(host.Address, options))
		{
			var client = new PathProbe.PathProbeClient(channel);
			Assert.Equal("reply:owned", (await client.UnaryAsync(new Frame { Text = "owned" }, deadline: Deadline())).Text);
		}

		Assert.Equal(disposeHttpClient, terminal.Disposed);

		if(!disposeHttpClient)
		{
			options.DisposeHttpClient = true;

			using(var channel = GrpcChannel.ForAddress(host.Address, options))
			{
				var client = new PathProbe.PathProbeClient(channel);
				Assert.Equal("reply:reused", (await client.UnaryAsync(new Frame { Text = "reused" }, deadline: Deadline())).Text);
			}

			Assert.True(terminal.Disposed);
		}
	}

	private static DateTime Deadline() => DateTime.UtcNow.AddSeconds(15);

	private sealed class DisposalHandler(HttpMessageHandler inner) : DelegatingHandler(inner)
	{
		public bool Disposed { get; private set; }
		protected override void Dispose(bool disposing)
		{
			if(disposing)
				this.Disposed = true;

			base.Dispose(disposing);
		}
	}
}
