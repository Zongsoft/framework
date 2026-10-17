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
using System.Buffers;
using System.Text.Json;
using System.Threading;
using System.Diagnostics;
using System.Buffers.Binary;
using System.Threading.Tasks;
using System.Threading.Channels;

using Zongsoft.Components;
using Zongsoft.Collections;

namespace Zongsoft.Net.Benchmarks;

internal static class LoadClient
{
	public static async Task RunAsync(LoadOptions options, CancellationToken cancellation)
	{
		ArgumentOutOfRangeException.ThrowIfZero(options.Port);
		ArgumentOutOfRangeException.ThrowIfLessThan(options.PayloadLength, 8);

		using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
		var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var latencies = new long[checked(options.Connections * options.Messages)];
		var sessions = new Task[options.Connections];
		var remaining = options.Connections;
		var running = options.Connections;

		for(var i = 0; i < sessions.Length; i++)
		{
			sessions[i] = RunConnectionAsync(i);
		}

		try
		{
			await ready.Task.WaitAsync(lifetime.Token);
			using var process = Process.GetCurrentProcess();
			var allocation = GC.GetTotalAllocatedBytes(true);
			var cpu = process.TotalProcessorTime;
			var timestamp = Stopwatch.GetTimestamp();

			start.SetResult();

			await completed.Task.WaitAsync(lifetime.Token);
			var elapsed = Stopwatch.GetElapsedTime(timestamp);
			var allocatedBytes = GC.GetTotalAllocatedBytes(true) - allocation;
			var cpuMilliseconds = (process.TotalProcessorTime - cpu).TotalMilliseconds;
			await Task.WhenAll(sessions);

			process.Refresh();
			Array.Sort(latencies);

			Console.WriteLine(JsonSerializer.Serialize(new
			{
				Event = "Completed",
				Options = options,
				Environment = EnvironmentReport.Capture(),
				Messages = latencies.Length,
				ElapsedSeconds = elapsed.TotalSeconds,
				MessagesPerSecond = latencies.Length / elapsed.TotalSeconds,
				P50Microseconds = Percentile(0.5),
				P95Microseconds = Percentile(0.95),
				P99Microseconds = Percentile(0.99),
				P999Microseconds = Percentile(0.999),
				ProcessAllocatedBytes = allocatedBytes,
				CpuMilliseconds = cpuMilliseconds,
				process.WorkingSet64,
				process.PrivateMemorySize64,
				process.HandleCount,
			}));
		}
		finally
		{
			await lifetime.CancelAsync();
			await Task.WhenAll(sessions);
		}

		double Percentile(double quantile) => latencies[(int)Math.Ceiling(latencies.Length * quantile) - 1] * 1_000_000.0 / Stopwatch.Frequency;
		async Task RunConnectionAsync(int index)
		{
			var payload = new byte[options.PayloadLength];
			new Random(index).NextBytes(payload);
			BinaryPrimitives.WriteInt32BigEndian(payload, index);

			var handler = new ResponseHandler(payload);
			await using var client = new TcpClient<ReadOnlySequence<byte>>(TcpClient.Headed.Packetizer)
			{
				Address = new IPEndPoint(IPAddress.Loopback, options.Port),
				Handler = handler,
			};
			var package = new ReadOnlySequence<byte>(payload);

			try
			{
				await client.ConnectAsync(cancellation: lifetime.Token);

				for(var i = 0; i < options.Warmup; i++)
					await RequestAsync(i);

				if(Interlocked.Decrement(ref remaining) == 0)
					ready.TrySetResult();

				await start.Task.WaitAsync(lifetime.Token);

				for(var i = 0; i < options.Messages; i++)
					latencies[index * options.Messages + i] = await RequestAsync(options.Warmup + i);

				if(Interlocked.Decrement(ref running) == 0)
					completed.TrySetResult();
			}
			catch
			{
				await lifetime.CancelAsync();
				throw;
			}
			finally
			{
				await client.DisconnectAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5));
			}

			async ValueTask<long> RequestAsync(int sequence)
			{
				BinaryPrimitives.WriteInt32BigEndian(payload.AsSpan(4), sequence);
				var timestamp = Stopwatch.GetTimestamp();
				await client.SendAsync(package, lifetime.Token);
				var received = await handler.Responses.ReadAsync(lifetime.Token);
				return received - timestamp;
			}
		}
	}

	private sealed class ResponseHandler(byte[] expected) : HandlerBase<TcpReceiveContext<ReadOnlySequence<byte>>>
	{
		private readonly Channel<long> _responses = Channel.CreateBounded<long>(new BoundedChannelOptions(1)
		{
			SingleReader = true,
			SingleWriter = true,
			AllowSynchronousContinuations = false,
		});

		public ChannelReader<long> Responses => _responses.Reader;

		protected override ValueTask OnHandleAsync(TcpReceiveContext<ReadOnlySequence<byte>> context, Parameters parameters, CancellationToken cancellation)
		{
			var argument = context.Package;
			var offset = 0;
			if(argument.Length != expected.Length)
				return this.Fail();

			foreach(var segment in argument)
			{
				if(!segment.Span.SequenceEqual(expected.AsSpan(offset, segment.Length)))
					return this.Fail();

				offset += segment.Length;
			}

			if(!_responses.Writer.TryWrite(Stopwatch.GetTimestamp()))
				return this.Fail();

			return ValueTask.CompletedTask;
		}

		private ValueTask Fail()
		{
			_responses.Writer.TryComplete(new InvalidDataException());
			return ValueTask.CompletedTask;
		}
	}
}
