/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@qq.com>
 *
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Net.Sockets;
using System.Buffers.Binary;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Zongsoft.Net.Benchmarks;

internal static class LoadServer
{
	public static async Task RunAsync(LoadOptions options, CancellationToken cancellation)
	{
		using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
		using var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

		listener.Bind(new IPEndPoint(IPAddress.Loopback, options.Port));
		listener.Listen(1024);

		Console.WriteLine(JsonSerializer.Serialize(new { Event = "Ready", Port = ((IPEndPoint)listener.LocalEndPoint).Port, Options = options, Environment = EnvironmentReport.Capture() }));
		var sessions = new List<Task>(options.Connections);

		try
		{
			for(var i = 0; i < options.Connections; i++)
			{
				var socket = await listener.AcceptAsync(lifetime.Token);
				sessions.Add(EchoAsync(socket, options, lifetime.Token));
			}

			await Task.WhenAll(sessions);
			Console.WriteLine(JsonSerializer.Serialize(new { Event = "Completed", Messages = (long)options.Connections * (options.Messages + options.Warmup) }));
		}
		finally
		{
			await lifetime.CancelAsync();
			await Task.WhenAll(sessions);
		}
	}

	private static async Task EchoAsync(Socket socket, LoadOptions options, CancellationToken cancellation)
	{
		using(socket)
		using(var stream = new NetworkStream(socket, false))
		{
			socket.NoDelay = true;
			var buffer = new byte[options.PayloadLength + 4];

			for(var i = 0; i < options.Warmup + options.Messages; i++)
			{
				await stream.ReadExactlyAsync(buffer, cancellation);

				if(BinaryPrimitives.ReadInt32BigEndian(buffer) != options.PayloadLength)
					throw new InvalidDataException();

				await stream.WriteAsync(buffer, cancellation);
			}

			if(await stream.ReadAsync(buffer.AsMemory(0, 1), cancellation) != 0)
				throw new InvalidDataException();
		}
	}
}
