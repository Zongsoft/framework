using System;
using System.IO;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.Communication;

namespace Zongsoft.Net.Tests;

public class TcpServerChannelLifecycleTest
{
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task LeaseIsReleasedOnceAfterAsyncHandlerEvenWhenClosed(bool fail)
	{
		var packetizer = new LeasePacketizer();
		await using var server = new TcpServer<Lease>(packetizer).AtLoopback();
		TcpChannelEventArgs<Lease> closing = null;
		TcpChannelEventArgs<Lease> closed = null;
		TcpChannelEventArgs<Lease> error = null;
		var releasedAtNotification = 0;
		server.Channels.Closing += (_, args) => closing = args;
		server.Channels.Closed += (_, args) => { closed = args; releasedAtNotification = packetizer.Last.ReleaseCount; };
		server.Channels.Error += (_, args) => error = args;
		var entered = TestNetwork.Signal<TcpReceiveContext<Lease>>();
		var release = TestNetwork.Signal();
		server.Handler = new ReceiveHandler<Lease>(async (context, _) =>
		{
			entered.TrySetResult(context);
			await release.Task;
			Assert.Equal(0, context.Package.ReleaseCount);
			Assert.Equal(17, context.Package.Bytes.FirstSpan[0]);

			if(fail)
				throw new InvalidOperationException();
		});
		await server.StartAsync();
		using var socket = await TestNetwork.ConnectAsync(server.Address);
		await TestNetwork.SendAsync(socket, new byte[] { 17 });
		var received = await entered.Task.WaitAsync(TestNetwork.Timeout);
		await received.Channel.CloseAsync();
		Assert.False(received.Channel.WaitAsync().IsCompleted);
		Assert.Equal(TcpCloseReason.Local, closing.CloseReason);
		Assert.Null(closing.Error);
		Assert.Null(closed);
		Assert.Equal(0, received.Package.ReleaseCount);
		release.TrySetResult();

		if(fail)
			await Assert.ThrowsAsync<InvalidOperationException>(() => received.Channel.WaitAsync().WaitAsync(TestNetwork.Timeout));
		else
			await received.Channel.WaitAsync().WaitAsync(TestNetwork.Timeout);

		Assert.Equal(1, received.Package.ReleaseCount);
		Assert.Equal(1, releasedAtNotification);
		Assert.Same(received.Channel, closed.Channel);
		Assert.Equal(TcpCloseReason.Local, closed.CloseReason);
		Assert.Null(closing.Error);

		if(fail)
		{
			Assert.IsType<InvalidOperationException>(closed.Error);
			Assert.Same(closed.Error, error.Error);
		}
		else
		{
			Assert.Null(closed.Error);
			Assert.Null(error);
		}

		Assert.Empty(server.Channels);
	}

	[Fact]
	public async Task ShutdownTimeoutRetainsBorrowedMemoryUntilUncooperativeHandlerExits()
	{
		var packetizer = new LeasePacketizer();
		await using var server = new TcpServer<Lease>(packetizer, new() { ShutdownTimeout = TimeSpan.FromMilliseconds(100) }).AtLoopback();
		TcpChannelEventArgs<Lease> error = null;
		server.Channels.Error += (_, args) => error = args;
		var entered = TestNetwork.Signal<TcpReceiveContext<Lease>>();
		var release = TestNetwork.Signal();
		server.Handler = new ReceiveHandler<Lease>(async (context, _) =>
		{
			entered.TrySetResult(context);
			await release.Task;
			Assert.Equal(23, context.Package.Bytes.FirstSpan[0]);
		});
		await server.StartAsync();
		using var socket = await TestNetwork.ConnectAsync(server.Address);
		await TestNetwork.SendAsync(socket, new byte[] { 23 });
		var received = await entered.Task.WaitAsync(TestNetwork.Timeout);
		await Assert.ThrowsAsync<TimeoutException>(() => server.StopAsync());
		Assert.NotNull(error);
		Assert.Equal(TcpCloseReason.ShutdownTimeout, error.CloseReason);
		Assert.IsType<TimeoutException>(error.Error);
		Assert.Equal(0, received.Package.ReleaseCount);
		Assert.False(received.Channel.WaitAsync().IsCompleted);
		Assert.Single(server.Channels);
		release.TrySetResult();
		await Assert.ThrowsAsync<TimeoutException>(() => received.Channel.WaitAsync().WaitAsync(TestNetwork.Timeout));
		Assert.Equal(1, received.Package.ReleaseCount);
		await server.StopAsync();
	}

	[Fact]
	public async Task SuccessfulDecodeWithoutProgressIsReleasedAndRejected()
	{
		var packetizer = new LeasePacketizer(false);
		await using var server = new TcpServer<Lease>(packetizer).AtLoopback();
		await server.StartAsync();
		using var socket = await TestNetwork.ConnectAsync(server.Address);
		await TestNetwork.UntilAsync(() => server.Channels.Count == 1);
		var channel = Assert.Single(server.Channels);
		await TestNetwork.SendAsync(socket, new byte[] { 1 });
		var exception = await Assert.ThrowsAsync<TcpException>(() => channel.WaitAsync().WaitAsync(TestNetwork.Timeout));

		Assert.Equal(TcpError.InvalidFrame, exception.ErrorCode);
		Assert.Equal(1, packetizer.Last.ReleaseCount);
	}

	private sealed class Lease(ReadOnlySequence<byte> bytes)
	{
		internal ReadOnlySequence<byte> Bytes { get; } = bytes;
		internal int ReleaseCount;
	}

	private sealed class LeasePacketizer(bool consume = true) : PacketizerBase<Lease>
	{
		internal Lease Last { get; private set; }
		public override long GetLength(in Lease package) => package.Bytes.Length;
		public override void Pack(IBufferWriter<byte> writer, in Lease package)
		{
			foreach(var segment in package.Bytes)
				writer.Write(segment.Span);
		}
		public override void Release(in Lease package) => Interlocked.Increment(ref package.ReleaseCount);
		public override bool Unpack(ref ReadOnlySequence<byte> data, out Lease package)
		{
			package = this.Last = new(data.Slice(0, 1));
			if(consume)
				data = data.Slice(1);

			return true;
		}
	}
}
