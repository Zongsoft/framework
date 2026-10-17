using System;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.Components;

namespace Zongsoft.Net.Tests;

public class TcpServerTest
{
	[Fact]
	public void ConstructorRejectsNullPacketizer() => Assert.Throws<ArgumentNullException>(() => new TcpServer<string>(null));

	[Fact]
	public async Task EmptyRunningServerBroadcastReportsZero()
	{
		await using var server = TestNetwork.Server();
		await server.StartAsync();
		Assert.Equal(new TcpBroadcastResult(0, 0, 0), await server.BroadcastAsync(ReadOnlySequence<byte>.Empty));
		await server.StopAsync();
		Assert.Equal(WorkerState.Stopped, server.State);
		var exception = await Assert.ThrowsAsync<TcpException>(() => server.BroadcastAsync(ReadOnlySequence<byte>.Empty).AsTask());

		Assert.Equal(TcpError.ConnectionClosed, exception.ErrorCode);
	}

	[Fact]
	public async Task BindFailureIsObservableAndCanRetryAfterRelease()
	{
		await using var first = TestNetwork.Server();
		await first.StartAsync();
		await using var second = TestNetwork.Server();
		second.Address = first.Address;
		await Assert.ThrowsAsync<SocketException>(() => second.StartAsync());
		Assert.Equal(WorkerState.Stopped, second.State);
		await first.StopAsync();
		await second.StartAsync();
		Assert.Equal(WorkerState.Running, second.State);
	}

	[Fact]
	public async Task ConnectionCapacityRejectsExcessAndRecoversAfterCompletion()
	{
		await using var server = TestNetwork.Server(new() { MaximumConnections = 1 });
		await server.StartAsync();
		using var first = await TestNetwork.ConnectAsync(server.Address);
		await TestNetwork.UntilAsync(() => server.Channels.Count == 1);
		var original = Assert.Single(server.Channels);
		using var rejected = await TestNetwork.ConnectAsync(server.Address);
		await TestNetwork.UntilAsync(() => server.RejectedConnections == 1);
		Assert.Single(server.Channels);
		await original.CloseAsync();
		await original.WaitAsync().WaitAsync(TestNetwork.Timeout);
		using var replacement = await TestNetwork.ConnectAsync(server.Address);
		await TestNetwork.UntilAsync(() => server.AcceptedConnections == 2);
		var next = Assert.Single(server.Channels);
		Assert.NotEqual(original.Identifier, next.Identifier);
		Assert.False(server.Channels.TryGetValue(original.Identifier, out _));
		Assert.True(server.Channels.TryGetValue(next.Identifier, out var found));
		Assert.Same(next, found);
	}

	[Fact]
	public async Task BroadcastDeliversOneFramePerConnectionAndCancellationPropagates()
	{
		await using var server = TestNetwork.Server(new() { BroadcastConcurrency = 2 });
		await server.StartAsync();
		using var first = await TestNetwork.ConnectAsync(server.Address);
		using var second = await TestNetwork.ConnectAsync(server.Address);
		await TestNetwork.UntilAsync(() => server.Channels.Count == 2);
		var result = await server.BroadcastAsync(new ReadOnlySequence<byte>(new byte[] { 7, 8 }));
		Assert.Equal(new TcpBroadcastResult(2, 0, 0), result);
		Assert.Equal(TestNetwork.Frame(7, 8), await TestNetwork.ReceiveAsync(first, 6));
		Assert.Equal(TestNetwork.Frame(7, 8), await TestNetwork.ReceiveAsync(second, 6));
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => server.BroadcastAsync(ReadOnlySequence<byte>.Empty, cancellation.Token).AsTask());
		await server.StopAsync();
		Assert.Empty(server.Channels);
		Assert.Equal(12, server.TotalBytesSent);
	}

	[Theory]
	[InlineData(TcpError.SendRejected, 0, 1)]
	[InlineData(TcpError.InvalidFrame, 1, 0)]
	[InlineData(TcpError.ConnectionClosed, 1, 0)]
	public async Task BroadcastDistinguishesRejectionFromOtherTcpErrors(TcpError errorCode, int failed, int rejected)
	{
		await using var server = new TcpServer<byte>(new FailingPacketizer(errorCode)).AtLoopback();
		await server.StartAsync();

		using var socket = await TestNetwork.ConnectAsync(server.Address);
		await TestNetwork.UntilAsync(() => server.Channels.Count == 1);

		var result = await server.BroadcastAsync(1);

		Assert.Equal(new TcpBroadcastResult(0, failed, rejected), result);
		Assert.False(Assert.Single(server.Channels).IsClosed);
	}

	[Fact]
	public async Task IPv6LoopbackAcceptsAndStops()
	{
		if(!Socket.OSSupportsIPv6)
			return;

		await using var server = TcpServer.Headed;
		server.Address = new(IPAddress.IPv6Loopback, 0);
		await server.StartAsync();
		using var socket = await TestNetwork.ConnectAsync(server.Address);
		await TestNetwork.UntilAsync(() => server.Channels.Count == 1);
		Assert.Equal(AddressFamily.InterNetworkV6, Assert.Single(server.Channels).LocalEndPoint.AddressFamily);
		await server.StopAsync();
		Assert.Empty(server.Channels);
	}

	[Fact]
	public async Task AcceptStopRaceLeavesNoConnectionsAndReleasesPort()
	{
		await using var server = TestNetwork.Server();
		await server.StartAsync();
		var address = server.Address;
		var connecting = Enumerable.Range(0, 32).Select(async _ =>
		{
			try { using var socket = await TestNetwork.ConnectAsync(address); }
			catch(SocketException) { }
		}).ToArray();
		await server.StopAsync();
		await Task.WhenAll(connecting);
		Assert.Empty(server.Channels);
		using var listener = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
		listener.Bind(address);
	}

	private sealed class FailingPacketizer(TcpError errorCode) : PacketizerBase<byte>
	{
		public override long GetLength(in byte package) => 1;
		public override void Pack(IBufferWriter<byte> writer, in byte package) => throw new TcpException(errorCode);
		public override bool Unpack(ref ReadOnlySequence<byte> data, out byte package)
		{
			package = 0;
			return false;
		}
	}
}
