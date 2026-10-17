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
using System.Net;
using System.Buffers;
using System.Threading;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections.Concurrent;

using Xunit;

namespace Zongsoft.Net.Tests;

public class TcpChannelNotificationTest
{
	[Theory]
	[InlineData(false, false)]
	[InlineData(false, true)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public async Task OwnerShutdownWaitsForFaultingHandlerWithoutRepeatingChannelFailure(bool clientSide, bool timeout)
	{
		await using var server = TestNetwork.Server();
		await using var client = TcpClient.Headed;

		var entered = TestNetwork.Signal<TcpChannelBase<ReadOnlySequence<byte>>>();
		var release = TestNetwork.Signal();

		Exception failure = timeout ? new TimeoutException() : new ApplicationException();
		var notifications = new ConcurrentQueue<string>();
		TcpChannelEventArgs<ReadOnlySequence<byte>> closed = null;
		var handler = new ReceiveHandler<ReadOnlySequence<byte>>(async (context, _) =>
		{
			entered.TrySetResult(context.Channel);
			await release.Task;
			throw failure;
		});

		if(clientSide)
		{
			client.Handler = handler;
			client.Error += (_, _) => notifications.Enqueue("error");
			client.Closed += (_, args) => { closed = args; notifications.Enqueue("closed"); };
		}
		else
		{
			server.Handler = handler;
			server.Channels.Error += (_, _) => notifications.Enqueue("error");
			server.Channels.Closed += (_, args) => { closed = args; notifications.Enqueue("closed"); };
		}

		await server.StartAsync();
		await client.ConnectAsync(server.Address);
		await TestNetwork.UntilAsync(() => server.Channels.Count == 1);

		try
		{
			if(clientSide)
				await server.BroadcastAsync(ReadOnlySequence<byte>.Empty);
			else
				await client.SendAsync(ReadOnlySequence<byte>.Empty);

			var channel = await entered.Task.WaitAsync(TestNetwork.Timeout);
			var stopping = clientSide ? client.DisconnectAsync().AsTask() : server.StopAsync();
			Assert.False(stopping.IsCompleted);
			Assert.Null(closed);

			release.TrySetResult();
			await stopping.WaitAsync(TestNetwork.Timeout);

			Assert.Equal(["error", "closed"], notifications.ToArray());
			Assert.Same(failure, closed.Error);
			Assert.Equal(TcpCloseReason.HandlerError, closed.CloseReason);
			Assert.Same(failure, await Record.ExceptionAsync(() => channel.WaitAsync()));
			Assert.Same(failure, await Record.ExceptionAsync(() => channel.WaitAsync()));

			if(clientSide)
				Assert.Null(client.Channel);
			else
				Assert.Empty(server.Channels);
		}
		finally { release.TrySetResult(); }
	}

	[Fact]
	public async Task ClientNotificationsWaitForHandlerAndIgnoreSubscriberFailures()
	{
		await using var server = TestNetwork.Server();
		server.Handler = new ReceiveHandler<ReadOnlySequence<byte>>((context, token) => context.Channel.SendAsync(context.Package, token));
		await server.StartAsync();

		await using var client = TcpClient.Headed;
		var entered = TestNetwork.Signal();
		var release = TestNetwork.Signal();
		var notifications = new ConcurrentQueue<string>();
		TcpChannelEventArgs<ReadOnlySequence<byte>> closed = null;
		var errors = 0;

		client.Handler = new ReceiveHandler<ReadOnlySequence<byte>>(async (_, _) =>
		{
			entered.TrySetResult();
			await release.Task;
		});
		client.Closing += (_, _) => throw new InvalidOperationException();
		client.Closing += (_, _) => notifications.Enqueue("closing");
		client.Closed += (_, _) => throw new InvalidOperationException();
		client.Closed += (_, args) => { closed = args; notifications.Enqueue("closed"); };
		client.Error += (_, _) => Interlocked.Increment(ref errors);
		var channel = await client.ConnectAsync(server.Address);

		try
		{
			await channel.SendAsync(new ReadOnlySequence<byte>(new byte[] { 7 }));
			await entered.Task.WaitAsync(TestNetwork.Timeout);
			await channel.CloseAsync();

			Assert.Equal(["closing"], notifications.ToArray());
			Assert.Null(closed);
			Assert.False(channel.WaitAsync().IsCompleted);

			release.TrySetResult();
			await channel.WaitAsync().WaitAsync(TestNetwork.Timeout);
			await channel.CloseAsync();
			await client.DisposeAsync();

			Assert.Equal(["closing", "closed"], notifications.ToArray());
			Assert.Same(channel, closed.Channel);
			Assert.Equal(TcpCloseReason.Local, closed.CloseReason);
			Assert.Null(closed.Error);
			Assert.Null(client.Channel);
			Assert.Equal(0, errors);
		}
		finally { release.TrySetResult(); }
	}

	[Fact]
	public async Task ManagerReportsFaultOnceAndClosesAfterRemoval()
	{
		await using var server = TestNetwork.Server();
		var notifications = new ConcurrentQueue<string>();
		TcpChannelEventArgs<ReadOnlySequence<byte>> closing = null;
		TcpChannelEventArgs<ReadOnlySequence<byte>> closed = null;
		TcpChannelEventArgs<ReadOnlySequence<byte>> failure = null;
		var closingCount = -1;
		var closedCount = -1;
		object sender = null;

		server.Channels.Closing += (_, args) =>
		{
			closing = args;
			closingCount = server.Channels.Count;
			notifications.Enqueue("closing");
		};
		server.Channels.Error += (_, _) => throw new ApplicationException();
		server.Channels.Error += (_, args) => { failure = args; notifications.Enqueue("error"); };
		server.Channels.Closed += (source, args) =>
		{
			sender = source;
			closed = args;
			closedCount = server.Channels.Count;
			notifications.Enqueue("closed");
		};

		await server.StartAsync();
		using var socket = await TestNetwork.ConnectAsync(server.Address);
		await TestNetwork.UntilAsync(() => server.Channels.Count == 1);
		var channel = Assert.Single(server.Channels);
		await TestNetwork.SendAsync(socket, new byte[] { 255, 255, 255, 255 });
		var exception = await Assert.ThrowsAsync<TcpException>(() => channel.WaitAsync().WaitAsync(TestNetwork.Timeout));
		await channel.CloseAsync();

		Assert.Equal(TcpError.InvalidFrame, exception.ErrorCode);
		Assert.Equal(["closing", "error", "closed"], notifications.ToArray());
		Assert.Equal(1, closingCount);
		Assert.Equal(0, closedCount);
		Assert.Same(server.Channels, sender);
		Assert.Same(channel, closed.Channel);
		Assert.Equal(TcpCloseReason.ProtocolError, closed.CloseReason);
		Assert.Same(exception, closing.Error);
		Assert.Same(exception, failure.Error);
		Assert.Same(exception, closed.Error);
	}

	[Fact]
	public async Task CancelingOneWaitDoesNotCloseChannelOrCancelOtherWaiters()
	{
		await using var server = TestNetwork.Server();
		await server.StartAsync();
		await using var client = TcpClient.Headed;
		var channel = await client.ConnectAsync(server.Address);
		using var cancellation = new CancellationTokenSource();
		var canceled = channel.WaitAsync(cancellation.Token);
		var remaining = channel.WaitAsync();
		cancellation.Cancel();
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceled);

		Assert.False(channel.IsClosed);
		Assert.False(remaining.IsCompleted);
		Assert.Same(channel, client.Channel);

		await channel.CloseAsync();
		await remaining.WaitAsync(TestNetwork.Timeout);
		await channel.WaitAsync().WaitAsync(TestNetwork.Timeout);
		Assert.Null(client.Channel);
	}

	[Fact]
	public async Task ClientReportsRemoteCloseWithFinalSnapshot()
	{
		await using var server = TestNetwork.Server();
		await server.StartAsync();
		await using var client = TcpClient.Headed;
		TcpChannelEventArgs<ReadOnlySequence<byte>> closed = null;
		object sender = null;
		var errors = 0;

		client.Closed += (source, args) => { sender = source; closed = args; };
		client.Error += (_, _) => Interlocked.Increment(ref errors);
		var channel = await client.ConnectAsync(server.Address);
		await TestNetwork.UntilAsync(() => server.Channels.Count == 1);
		await Assert.Single(server.Channels).CloseAsync();
		await channel.WaitAsync().WaitAsync(TestNetwork.Timeout);

		Assert.Same(client, sender);
		Assert.Same(channel, closed.Channel);
		Assert.Equal(TcpCloseReason.Remote, closed.CloseReason);
		Assert.Null(closed.Error);
		Assert.Null(client.Channel);
		Assert.Equal(0, errors);
	}

	[Fact]
	public async Task FailedConnectHasNoChannelNotificationsAndAllowsRetry()
	{
		using var reserved = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		reserved.Bind(new IPEndPoint(IPAddress.Loopback, 0));
		await using var client = TcpClient.Headed;
		var notifications = 0;

		client.Closing += (_, _) => Interlocked.Increment(ref notifications);
		client.Closed += (_, _) => Interlocked.Increment(ref notifications);
		client.Error += (_, _) => Interlocked.Increment(ref notifications);
		await Assert.ThrowsAsync<SocketException>(() => client.ConnectAsync(reserved.LocalEndPoint).WaitAsync(TestNetwork.Timeout));

		Assert.Null(client.Channel);
		Assert.Equal(0, notifications);

		await using var server = TestNetwork.Server();
		await server.StartAsync();
		var channel = await client.ConnectAsync(server.Address).WaitAsync(TestNetwork.Timeout);
		Assert.Same(channel, client.Channel);
	}
}
