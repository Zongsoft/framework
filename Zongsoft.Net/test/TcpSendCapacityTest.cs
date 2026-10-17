/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Net library.
 *
 * The Zongsoft.Net is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Net is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Net library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Buffers;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.Communication;

namespace Zongsoft.Net.Tests;

public class TcpSendCapacityTest
{
	[Theory]
	[InlineData(2, 100)]
	[InlineData(10, 8)]
	public async Task ReservedOperationsAndBytesAreBoundedAndCanceledWaitReturnsCapacity(int operations, int bytes)
	{
		await using var server = TestNetwork.Server();
		await server.StartAsync();
		using var gate = new ManualResetEventSlim();
		var entered = TestNetwork.Signal();
		var packetizer = new BlockingPacketizer(gate, entered);
		await using var client = new TcpClient<int>(packetizer, new()
		{
			EnableDiagnostics = true,
			MaximumPendingSends = operations,
			MaximumPacketLength = 4,
			MaximumFrameLength = 4,
			MaximumPendingSendBytes = bytes,
		});
		var channel = await client.ConnectAsync(server.Address);
		var first = Task.Run(async () => await channel.SendAsync(1));
		await entered.Task.WaitAsync(TestNetwork.Timeout);
		using var cancellation = new CancellationTokenSource();
		var waiting = channel.SendAsync(2, cancellation.Token).AsTask();

		try
		{
			Assert.Equal(2, channel.PendingSendCount);
			Assert.Equal(8, channel.PendingSendBytes);
			var exception = await Assert.ThrowsAsync<TcpException>(() => channel.SendAsync(3).AsTask());

			Assert.Equal(TcpError.SendRejected, exception.ErrorCode);
			Assert.True(channel.Statistics.SendWaitCount > 0);

			cancellation.Cancel();
			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiting);
			Assert.Equal(1, channel.PendingSendCount);
			Assert.Equal(4, channel.PendingSendBytes);
			Assert.False(channel.IsClosed);
		}
		finally { gate.Set(); }

		await first.WaitAsync(TestNetwork.Timeout);
		await channel.SendAsync(4);
		await client.DisconnectAsync();
		Assert.Equal(0, channel.PendingSendCount);
		Assert.Equal(0, channel.PendingSendBytes);
	}

	[Fact]
	public async Task CloseDuringEncodingWaitsForWriterAndRejectsQueuedSend()
	{
		await using var server = TestNetwork.Server();
		await server.StartAsync();
		using var gate = new ManualResetEventSlim();
		var entered = TestNetwork.Signal();
		await using var client = new TcpClient<int>(new BlockingPacketizer(gate, entered));
		var channel = await client.ConnectAsync(server.Address);
		var first = Task.Run(async () => await channel.SendAsync(1));
		await entered.Task.WaitAsync(TestNetwork.Timeout);
		var second = channel.SendAsync(2).AsTask();
		var closing = channel.CloseAsync().AsTask();
		Assert.False(closing.IsCompleted);
		gate.Set();

		try { await first.WaitAsync(TestNetwork.Timeout); }
		catch(Exception exception) when(exception is IOException or OperationCanceledException) { }

		var closed = await Assert.ThrowsAsync<TcpException>(() => second);

		Assert.Equal(TcpError.ConnectionClosed, closed.ErrorCode);

		await closing.WaitAsync(TestNetwork.Timeout);

		try { await channel.WaitAsync().WaitAsync(TestNetwork.Timeout); }
		catch(Exception exception) when(exception is IOException or OperationCanceledException) { }

		Assert.Equal(0, channel.PendingSendCount);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task NonReadingPeerCannotExceedSendBudgetAndAbortReturnsAllReservations(bool cancel)
	{
		using var listener = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
		listener.Bind(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 0));
		listener.Listen(1);
		await using var client = TcpClient.CreateHeaded(new()
		{
			MaximumPacketLength = 1024 * 1024,
			MaximumFrameLength = 1024 * 1024 + 4,
			MaximumBufferedReceiveBytes = 2 * 1024 * 1024,
			MaximumPendingSendBytes = 2 * 1024 * 1024 + 8,
			MaximumPendingSends = 2,
			SendPauseThreshold = 1024,
			SendResumeThreshold = 512,
		});
		var connecting = client.ConnectAsync(listener.LocalEndPoint);
		using var peer = await listener.AcceptAsync();
		peer.ReceiveBufferSize = 1024;
		var channel = await connecting;
		using var cancellation = new CancellationTokenSource();
		var body = new ReadOnlySequence<byte>(new byte[1024 * 1024]);
		var sends = new System.Collections.Generic.List<Task>();

		for(var i = 0; i < 32; i++)
		{
			sends.Add(channel.SendAsync(body, cancellation.Token).AsTask());
			Assert.InRange(channel.PendingSendCount, 0, 2);
			Assert.InRange(channel.PendingSendBytes, 0, 2 * 1024 * 1024 + 8);
		}

		Assert.Contains(sends, task => task.IsFaulted && task.Exception.InnerException is TcpException { ErrorCode: TcpError.SendRejected });
		Assert.Contains(sends, task => !task.IsCompleted);

		if(cancel)
			cancellation.Cancel();
		else
			await channel.CloseAsync();

		foreach(var send in sends)
		{
			try { await send.WaitAsync(TestNetwork.Timeout); }
			catch(Exception exception) when(exception is TcpException { ErrorCode: TcpError.SendRejected } || IsShutdown(exception)) { }
		}

		try { await channel.WaitAsync().WaitAsync(TestNetwork.Timeout); }
		catch(Exception exception) when(IsShutdown(exception)) { }

		Assert.True(channel.IsClosed);
		Assert.Equal(0, channel.PendingSendCount);
		Assert.Equal(0, channel.PendingSendBytes);

		static bool IsShutdown(Exception exception) => exception is IOException or OperationCanceledException ||
			exception is SocketException { SocketErrorCode: SocketError.OperationAborted or SocketError.ConnectionAborted or SocketError.ConnectionReset or SocketError.Interrupted or SocketError.Shutdown or SocketError.NotSocket };
	}
	[Fact]
	public async Task StopWaitsForAdmittedBroadcastAndRejectsAdditionalBroadcasts()
	{
		using var gate = new ManualResetEventSlim();
		var entered = TestNetwork.Signal();
		await using var server = new TcpServer<int>(new BlockingPacketizer(gate, entered), new() { MaximumConcurrentBroadcasts = 1, BroadcastConcurrency = 1 }).AtLoopback();
		await server.StartAsync();
		using var socket = await TestNetwork.ConnectAsync(server.Address);
		await TestNetwork.UntilAsync(() => server.Channels.Count == 1);
		var broadcast = Task.Run(async () => await server.BroadcastAsync(1));
		await entered.Task.WaitAsync(TestNetwork.Timeout);
		Task stopping;

		try
		{
			var exception = await Assert.ThrowsAsync<TcpException>(() => server.BroadcastAsync(2).AsTask());

			Assert.Equal(TcpError.SendRejected, exception.ErrorCode);

			stopping = server.StopAsync();
			Assert.False(stopping.IsCompleted);
		}
		finally { gate.Set(); }

		Assert.Equal(new TcpBroadcastResult(1, 0, 0), await broadcast.WaitAsync(TestNetwork.Timeout));
		await stopping.WaitAsync(TestNetwork.Timeout);
		Assert.True(broadcast.IsCompletedSuccessfully);
		Assert.Empty(server.Channels);
		Assert.Equal(new byte[4], await TestNetwork.ReceiveAsync(socket, 4));
	}
	private sealed class BlockingPacketizer(ManualResetEventSlim gate, TaskCompletionSource entered) : PacketizerBase<int>
	{
		public override long GetLength(in int package) => 4;
		public override void Pack(IBufferWriter<byte> writer, in int package)
		{
			entered.TrySetResult();
			if(!gate.Wait(TestNetwork.Timeout))
				throw new TimeoutException();

			writer.Write(new byte[4]);
		}
		public override bool Unpack(ref ReadOnlySequence<byte> data, out int package) { package = 0; return false; }
	}
}
