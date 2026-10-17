using System;
using System.IO;
using System.Linq;
using System.Buffers;
using System.Buffers.Binary;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

using Xunit;

using Zongsoft.Common;
using Zongsoft.Communication;

namespace Zongsoft.Net.Tests;

public class TcpChannelBaseTest
{
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task EmptyCustomFrameMayRequestZeroHintBufferWithoutAdvancing(bool memory)
	{
		await using var server = TcpServer.Headless;
		server.Address = new(System.Net.IPAddress.Loopback, 0);
		await server.StartAsync();
		await using var client = new TcpClient<ReadOnlySequence<byte>>(new EmptyPacketizer(memory));
		var channel = await client.ConnectAsync(server.Address);
		await channel.SendAsync(ReadOnlySequence<byte>.Empty);
		Assert.False(channel.IsClosed);
		Assert.Equal(0, channel.TotalBytesSent);
		Assert.Equal(0, channel.PendingSendBytes);
	}
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task EmptySendPreservesConnectionAndFraming(bool headed)
	{
		await using var server = headed ? TcpServer.Headed : TcpServer.Headless;
		server.Address = new(System.Net.IPAddress.Loopback, 0);
		var received = new ConcurrentQueue<byte[]>();
		server.Handler = new ReceiveHandler<ReadOnlySequence<byte>>((context, _) =>
		{
			received.Enqueue(context.Package.ToArray());
			return ValueTask.CompletedTask;
		});
		await server.StartAsync();
		await using var client = headed ? TcpClient.Headed : TcpClient.Headless;
		var channel = await client.ConnectAsync(server.Address);
		await channel.SendAsync(ReadOnlySequence<byte>.Empty);
		await channel.SendAsync(new ReadOnlySequence<byte>(new byte[] { 42 }));
		await TestNetwork.UntilAsync(() => received.Count == (headed ? 2 : 1));
		Assert.Equal(headed ? new byte[][] { [], [42] } : new byte[][] { [42] }, received.ToArray());
		Assert.False(channel.IsClosed);
		Assert.Equal(0, channel.PendingSendCount);
	}
	[Theory]
	[InlineData(512, 128, false)]
	[InlineData(512, 512, true)]
	[InlineData(8, 65536, false)]
	public async Task BatchedFramesProgressWithoutNextWriteAndPreserveOrder(int count, int size, bool eof)
	{
		await using var server = TestNetwork.Server(new() { EnableDiagnostics = true, ReceiveFrameBudget = 3, ReceiveByteBudget = 128, ReceivePauseThreshold = 512, ReceiveResumeThreshold = 256 });
		var received = new ConcurrentQueue<int>();
		var completed = TestNetwork.Signal();
		server.Handler = new ReceiveHandler<ReadOnlySequence<byte>>((context, _) =>
		{
			Assert.True(context.Package.TryGetInt32BigEndian(out var value));
			received.Enqueue(value);
			if(received.Count == count)
				completed.TrySetResult();

			return ValueTask.CompletedTask;
		});
		await server.StartAsync();
		using var socket = await TestNetwork.ConnectAsync(server.Address);
		await TestNetwork.UntilAsync(() => server.Channels.Count == 1);
		var channel = Assert.Single(server.Channels);
		var writer = new ArrayBufferWriter<byte>();
		var body = new byte[size];

		for(var i = 0; i < count; i++)
		{
			BinaryPrimitives.WriteInt32BigEndian(body, i);
			server.Packetizer.Pack(writer, new ReadOnlySequence<byte>(body));
		}

		await TestNetwork.SendAsync(socket, writer.WrittenMemory);
		if(eof)
			socket.Shutdown(SocketShutdown.Send);

		await completed.Task.WaitAsync(TestNetwork.Timeout);
		Assert.Equal(Enumerable.Range(0, count), received);
		await server.StopAsync();
		Assert.Empty(server.Channels);
		Assert.Equal(count, server.TotalFramesReceived);
		Assert.True(channel.Statistics.BudgetExhaustions > 0);
		Assert.True(channel.Statistics.ReadCount > 0);
		Assert.InRange(channel.Statistics.MaximumFramesPerRead, 1, 3);
	}

	[Fact]
	public async Task SplitHeaderBodyAndIncompleteTailResumeExactlyOnce()
	{
		await using var server = TestNetwork.Server();
		var received = new ConcurrentQueue<byte[]>();
		server.Handler = new ReceiveHandler<ReadOnlySequence<byte>>((context, _) =>
		{
			received.Enqueue(context.Package.ToArray());
			return ValueTask.CompletedTask;
		});
		await server.StartAsync();
		using var socket = await TestNetwork.ConnectAsync(server.Address);
		await TestNetwork.SendAsync(socket, new byte[] { 0, 0 });
		await TestNetwork.UntilAsync(() => server.TotalBytesReceived == 2);
		Assert.Empty(received);
		await TestNetwork.SendAsync(socket, new byte[] { 0, 3, 1 });
		await TestNetwork.UntilAsync(() => server.TotalBytesReceived == 5);
		Assert.Empty(received);
		await TestNetwork.SendAsync(socket, new byte[] { 2, 3, 0, 0, 0, 1 });
		await TestNetwork.UntilAsync(() => received.Count == 1);
		await TestNetwork.SendAsync(socket, new byte[] { 4 });
		await TestNetwork.UntilAsync(() => received.Count == 2);
		Assert.Equal(new byte[][] { [1, 2, 3], [4] }, received.ToArray());
	}

	[Fact]
	public async Task ConcurrentSendsNeverInterleaveAndMemoryOverloadUsesFraming()
	{
		await using var server = TestNetwork.Server();
		var received = new ConcurrentQueue<int>();
		server.Handler = new ReceiveHandler<ReadOnlySequence<byte>>((context, _) =>
		{
			Assert.True(context.Package.TryGetInt32BigEndian(out var value));
			received.Enqueue(value);
			return ValueTask.CompletedTask;
		});
		await server.StartAsync();
		await using var client = TcpClient.Headed;
		var channel = await client.ConnectAsync(server.Address);
		await Task.WhenAll(Enumerable.Range(0, 32).Select(async value =>
		{
			var bytes = new byte[4];
			BinaryPrimitives.WriteInt32BigEndian(bytes, value);
			await channel.SendAsync(bytes.AsMemory());
		}));
		await TestNetwork.UntilAsync(() => received.Count == 32);
		Assert.Equal(Enumerable.Range(0, 32), received.Order());
		await client.DisconnectAsync();
		Assert.Equal(0, channel.PendingSendCount);
		Assert.Equal(0, channel.PendingSendBytes);
		Assert.Equal(256, channel.TotalBytesSent);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task InvalidOrTruncatedFrameClosesWithProtocolFailure(bool truncated)
	{
		await using var server = TestNetwork.Server(new() { EnableDiagnostics = true });
		TcpChannelEventArgs<ReadOnlySequence<byte>> closed = null;
		server.Channels.Closed += (_, args) => closed = args;
		await server.StartAsync();
		using var socket = await TestNetwork.ConnectAsync(server.Address);
		await TestNetwork.UntilAsync(() => server.Channels.Count == 1);
		var channel = Assert.Single(server.Channels);
		await TestNetwork.SendAsync(socket, truncated ? new byte[] { 0, 0, 0, 5, 1 } : new byte[] { 255, 255, 255, 255 });

		if(truncated)
		{
			await TestNetwork.UntilAsync(() => channel.Statistics.ReadCount > 0);
			socket.Shutdown(SocketShutdown.Send);
		}

		var exception = await Assert.ThrowsAsync<TcpException>(() => channel.WaitAsync().WaitAsync(TestNetwork.Timeout));

		Assert.Equal(truncated ? TcpError.IncompleteFrame : TcpError.InvalidFrame, exception.ErrorCode);
		Assert.Equal(TcpCloseReason.ProtocolError, closed.CloseReason);
		Assert.Empty(server.Channels);
	}

	[Fact]
	public async Task ShutdownAfterExaminingIncompleteFrameCompletesNormally()
	{
		await using var server = TestNetwork.Server(new() { EnableDiagnostics = true });
		TcpChannelEventArgs<ReadOnlySequence<byte>> closed = null;
		server.Channels.Closed += (_, args) => closed = args;
		await server.StartAsync();
		using var socket = await TestNetwork.ConnectAsync(server.Address);
		await TestNetwork.UntilAsync(() => server.Channels.Count == 1);
		var channel = Assert.Single(server.Channels);
		await TestNetwork.SendAsync(socket, new byte[] { 0, 0 });
		await TestNetwork.UntilAsync(() => channel.Statistics.ReadCount > 0);
		await server.StopAsync().WaitAsync(TestNetwork.Timeout);
		await channel.WaitAsync().WaitAsync(TestNetwork.Timeout);

		Assert.Null(closed.Error);
		Assert.Equal(TcpCloseReason.Local, closed.CloseReason);
		Assert.Equal(0, channel.TotalFramesReceived);
		Assert.Empty(server.Channels);
	}

	[Fact]
	public async Task HandlerCanAwaitCloseWithoutWaitingForItself()
	{
		await using var server = TestNetwork.Server();
		var handled = TestNetwork.Signal<TcpChannelBase<ReadOnlySequence<byte>>>();
		server.Handler = new ReceiveHandler<ReadOnlySequence<byte>>(async (context, _) =>
		{
			await context.Channel.CloseAsync();
			Assert.Equal(new byte[] { 7 }, context.Package.ToArray());
			handled.TrySetResult(context.Channel);
		});
		await server.StartAsync();
		using var socket = await TestNetwork.ConnectAsync(server.Address);
		await TestNetwork.SendAsync(socket, TestNetwork.Frame(7));
		var channel = await handled.Task.WaitAsync(TestNetwork.Timeout);
		await channel.WaitAsync().WaitAsync(TestNetwork.Timeout);
		Assert.True(channel.IsClosed);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(2)]
	public async Task MismatchedEncodedLengthClosesAfterAnyWrite(int declared)
	{
		await using var server = TestNetwork.Server();
		await server.StartAsync();
		await using var client = new TcpClient<byte>(new BadPacketizer(declared));
		var channel = await client.ConnectAsync(server.Address);
		var error = await Assert.ThrowsAsync<TcpException>(() => channel.SendAsync(1).AsTask());

		Assert.Equal(TcpError.InvalidFrame, error.ErrorCode);

		if(declared == 0)
			Assert.False(channel.IsClosed);
		else
		{
			var exception = await Assert.ThrowsAsync<TcpException>(() => channel.WaitAsync().WaitAsync(TestNetwork.Timeout));

			Assert.Equal(TcpError.InvalidFrame, exception.ErrorCode);
			Assert.True(channel.IsClosed);
		}

		Assert.Equal(0, channel.PendingSendCount);
	}

	[Fact]
	public async Task RemoteHalfCloseStillDrainsCurrentReply()
	{
		await using var server = TestNetwork.Server();
		server.Handler = new ReceiveHandler<ReadOnlySequence<byte>>((context, token) => context.Channel.SendAsync(context.Package, token));
		TcpChannelEventArgs<ReadOnlySequence<byte>> closed = null;
		server.Channels.Closed += (_, args) => closed = args;
		await server.StartAsync();
		using var socket = await TestNetwork.ConnectAsync(server.Address);
		await TestNetwork.UntilAsync(() => server.Channels.Count == 1);
		var channel = Assert.Single(server.Channels);
		await TestNetwork.SendAsync(socket, TestNetwork.Frame(42));
		socket.Shutdown(SocketShutdown.Send);
		Assert.Equal(TestNetwork.Frame(42), await TestNetwork.ReceiveAsync(socket, 5));
		await channel.WaitAsync().WaitAsync(TestNetwork.Timeout);
		Assert.Equal(TcpCloseReason.Remote, closed.CloseReason);
		Assert.Equal(5, channel.TotalBytesSent);
		Assert.Equal(0, channel.PendingSendBytes);
	}
	private sealed class BadPacketizer(int declared) : PacketizerBase<byte>
	{
		public override long GetLength(in byte package) => declared;
		public override void Pack(IBufferWriter<byte> writer, in byte package)
		{
			writer.GetSpan(1)[0] = package;
			writer.Advance(1);
		}
		public override bool Unpack(ref ReadOnlySequence<byte> data, out byte package) { package = 0; return false; }
	}

	private sealed class EmptyPacketizer(bool memory) : PacketizerBase<ReadOnlySequence<byte>>
	{
		public override long GetLength(in ReadOnlySequence<byte> package) => 0;
		public override void Pack(IBufferWriter<byte> writer, in ReadOnlySequence<byte> package)
		{
			if(memory)
			{
				Assert.False(writer.GetMemory().IsEmpty);
				writer.Advance(0);
			}
			else
				writer.Write(ReadOnlySpan<byte>.Empty);
		}

		public override bool Unpack(ref ReadOnlySequence<byte> data, out ReadOnlySequence<byte> package)
		{
			package = default;
			return false;
		}
	}
}
