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
using System.Net;
using System.Net.Sockets;
using System.Buffers;
using System.Buffers.Binary;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Collections;
using Zongsoft.Components;

namespace Zongsoft.Net.Tests;

internal static class TestNetwork
{
	internal static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);
	internal static TaskCompletionSource Signal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
	internal static TaskCompletionSource<T> Signal<T>() => new(TaskCreationOptions.RunContinuationsAsynchronously);
	internal static TcpServer<ReadOnlySequence<byte>> Server(TcpServerOptions options = null) => TcpServer.CreateHeaded(options).AtLoopback();
	internal static TcpServer<T> AtLoopback<T>(this TcpServer<T> server)
	{
		server.Address = new(IPAddress.Loopback, 0);
		return server;
	}

	internal static async Task<Socket> ConnectAsync(EndPoint endpoint)
	{
		var socket = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };

		try
		{
			await socket.ConnectAsync(endpoint).WaitAsync(Timeout);
			return socket;
		}
		catch
		{
			socket.Dispose();
			throw;
		}
	}

	internal static async Task SendAsync(Socket socket, ReadOnlyMemory<byte> bytes)
	{
		using var cancellation = new CancellationTokenSource(Timeout);
		while(!bytes.IsEmpty)
		{
			var count = await socket.SendAsync(bytes, SocketFlags.None, cancellation.Token);
			if(count == 0)
				throw new EndOfStreamException();

			bytes = bytes[count..];
		}
	}

	internal static async Task<byte[]> ReceiveAsync(Socket socket, int length)
	{
		var bytes = new byte[length];
		var offset = 0;
		using var cancellation = new CancellationTokenSource(Timeout);

		while(offset < bytes.Length)
		{
			var count = await socket.ReceiveAsync(bytes.AsMemory(offset), SocketFlags.None, cancellation.Token);
			if(count == 0)
				throw new EndOfStreamException();

			offset += count;
		}

		return bytes;
	}

	internal static byte[] Frame(params byte[] body)
	{
		var bytes = new byte[body.Length + 4];
		BinaryPrimitives.WriteInt32BigEndian(bytes, body.Length);
		body.CopyTo(bytes, 4);
		return bytes;
	}

	internal static async Task UntilAsync(Func<bool> predicate)
	{
		using var cancellation = new CancellationTokenSource(Timeout);
		while(!predicate())
			await Task.Delay(5, cancellation.Token);
	}
}

internal sealed class ReceiveHandler<T>(Func<TcpReceiveContext<T>, CancellationToken, ValueTask> handle) : HandlerBase<TcpReceiveContext<T>>
{
	protected override ValueTask OnHandleAsync(TcpReceiveContext<T> argument, Parameters parameters, CancellationToken cancellation) => handle(argument, cancellation);
}
