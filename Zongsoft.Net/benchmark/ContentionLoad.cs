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
using System.Linq;
using System.Buffers;
using System.Text.Json;
using System.Threading;
using System.Diagnostics;
using System.Buffers.Binary;
using System.Threading.Tasks;
using System.Threading.Channels;

using Zongsoft.Components;

namespace Zongsoft.Net.Benchmarks;

internal static class ContentionLoad
{
	#region 发送竞争
	internal static async Task RunAsync(LoadOptions options, CancellationToken cancellation)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(options.PayloadLength, 12);
		ArgumentOutOfRangeException.ThrowIfNotEqual(options.Mixed, 0);
		ArgumentOutOfRangeException.ThrowIfGreaterThan((long)options.Connections * options.Senders * options.Messages, 10_000_000);

		using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
		var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var finish = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var close = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var latencies = new long[options.Connections * options.Senders * options.Messages];
		var remaining = options.TotalConnections;
		var running = options.Connections;
		var waits = 0L;
		var succeeded = 0L;
		var rejected = 0L;
		var broadcasts = 0L;
		var reconnects = 0L;
		var peerClosures = 0L;
		var disturbances = options.Disturbance == 0 ? [] : Enumerable.Range(0, 4).Select(DisturbAsync).ToArray();
		var tasks = Enumerable.Range(0, options.Connections).Select(RunConnectionAsync).ToArray();

		try
		{
			await ready.Task.WaitAsync(lifetime.Token);
			var before = ProcessSnapshot.Capture();
			var timestamp = Stopwatch.GetTimestamp();
			start.TrySetResult();

			await finish.Task.WaitAsync(lifetime.Token);
			var elapsed = Stopwatch.GetElapsedTime(timestamp);
			var after = ProcessSnapshot.Capture();
			close.TrySetResult();

			await Task.WhenAll(tasks);
			Array.Sort(latencies);

			Console.WriteLine(JsonSerializer.Serialize(new
			{
				Event = "Completed",
				Options = options,
				Environment = EnvironmentReport.Capture(),
				Requests = latencies.Length,
				Succeeded = succeeded,
				Rejected = rejected,
				Errors = 0,
				SendWaits = waits,
				BroadcastsReceived = broadcasts,
				Reconnects = reconnects,
				DisturbancePeerClosures = peerClosures,
				ElapsedSeconds = elapsed.TotalSeconds,
				MessagesPerSecond = succeeded / elapsed.TotalSeconds,
				P50Microseconds = Percentile(0.5),
				P95Microseconds = Percentile(0.95),
				P99Microseconds = Percentile(0.99),
				P999Microseconds = Percentile(0.999),
				Before = before,
				After = after,
				Closed = ProcessSnapshot.Capture(),
			}));
		}
		catch(Exception exception)
		{
			Console.WriteLine(JsonSerializer.Serialize(new { Event = "Failed", Succeeded = succeeded, Rejected = rejected, Error = exception.GetType().FullName, Process = ProcessSnapshot.Capture() }));
			throw;
		}
		finally
		{
			await lifetime.CancelAsync();
			close.TrySetResult();
			await Task.WhenAll(tasks);
			await Task.WhenAll(disturbances);
		}

		async Task DisturbAsync(int role)
		{
			try
			{
				var first = true;
				do
				{
					using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true, ReceiveBufferSize = 1024 };
					await socket.ConnectAsync(new IPEndPoint(IPAddress.Loopback, options.Port), lifetime.Token);
					using var stream = new NetworkStream(socket, false);

					if(first && Interlocked.Decrement(ref remaining) == 0)
						ready.TrySetResult();

					first = false;
					if(role == 3)
					{
						Interlocked.Increment(ref reconnects);
						await Task.Delay(20, lifetime.Token);
						continue;
					}

					var frame = new byte[options.PayloadLength + 4];
					BinaryPrimitives.WriteInt32BigEndian(frame, options.PayloadLength);

					if(role == 2)
					{
						await stream.WriteAsync(frame.AsMemory(0, 4), lifetime.Token);
						while(true)
						{
							await Task.Delay(1000, lifetime.Token);
							await stream.WriteAsync(frame.AsMemory(4, 1), lifetime.Token);
						}
					}

					var reading = role == 1 ? ReadSlowlyAsync() : Task.CompletedTask;

					try
					{
						while(true)
							await stream.WriteAsync(frame, lifetime.Token);
					}
					finally
					{
						socket.Dispose();
						await reading;
					}

					async Task ReadSlowlyAsync()
					{
						var buffer = new byte[16];

						try
						{
							while(await stream.ReadAsync(buffer, lifetime.Token) != 0)
								await Task.Delay(20, lifetime.Token);
						}
						catch(Exception exception) when(exception is OperationCanceledException or IOException or ObjectDisposedException) { }
					}
				}
				while(!lifetime.IsCancellationRequested);
			}
			catch(OperationCanceledException) when(lifetime.IsCancellationRequested) { }
			catch(Exception exception) when(exception is IOException or SocketException or ObjectDisposedException)
			{
				Interlocked.Increment(ref peerClosures);
				Console.WriteLine(JsonSerializer.Serialize(new { Event = "DisturbancePeerClosed", Role = role, Error = exception.GetType().FullName }));
			}
		}

		double Percentile(double quantile) => latencies[(int)Math.Ceiling(latencies.Length * quantile) - 1] * 1_000_000.0 / Stopwatch.Frequency;
		async Task RunConnectionAsync(int index)
		{
			var senders = Enumerable.Range(0, options.Senders).Select(sender => new Sender(index, sender, options.PayloadLength)).ToArray();
			await using var client = TcpClient.CreateHeaded(new() { EnableDiagnostics = true, ZeroLengthReads = options.ZeroLengthReads != 0 });
			client.Handler = new ResponseHandler(senders, () => Interlocked.Increment(ref broadcasts));

			try
			{
				var channel = await client.ConnectAsync(new IPEndPoint(IPAddress.Loopback, options.Port), lifetime.Token);
				await Task.WhenAll(senders.Select(sender => RunSenderAsync(sender, true)));
				if(Interlocked.Decrement(ref remaining) == 0)
					ready.TrySetResult();

				await start.Task.WaitAsync(lifetime.Token);
				var beforeWaits = channel.Statistics.SendWaitCount;
				await Task.WhenAll(senders.Select(sender => RunSenderAsync(sender, false)));
				Interlocked.Add(ref waits, channel.Statistics.SendWaitCount - beforeWaits);

				if(Interlocked.Decrement(ref running) == 0)
					finish.TrySetResult();

				await close.Task.WaitAsync(lifetime.Token);
				await client.DisconnectAsync();
			}
			catch
			{
				await lifetime.CancelAsync();
				throw;
			}

			async Task RunSenderAsync(Sender sender, bool warming)
			{
				for(var sequence = 0; sequence < (warming ? options.Warmup : options.Messages); sequence++)
				{
					BinaryPrimitives.WriteInt32BigEndian(sender.Payload.AsSpan(8), sequence);
					var timestamp = Stopwatch.GetTimestamp();
					long finished;

					try
					{
						await client.SendAsync(new ReadOnlySequence<byte>(sender.Payload), lifetime.Token);
						finished = await sender.Responses.Reader.ReadAsync(lifetime.Token);
						if(!warming)
							Interlocked.Increment(ref succeeded);
					}
					catch(TcpException exception) when(exception.ErrorCode == TcpError.SendRejected)
					{
						finished = Stopwatch.GetTimestamp();
						if(!warming)
							Interlocked.Increment(ref rejected);
					}

					if(!warming)
						latencies[(index * options.Senders + sender.Identifier) * options.Messages + sequence] = finished - timestamp;
				}
			}
		}
	}
	#endregion

	#region 测试会话
	private sealed class Sender
	{
		internal Sender(int connection, int identifier, int length)
		{
			this.Identifier = identifier;
			this.Payload = new byte[length];
			new Random(connection * 64 + identifier).NextBytes(this.Payload);
			BinaryPrimitives.WriteInt32BigEndian(this.Payload, connection);
			BinaryPrimitives.WriteInt32BigEndian(this.Payload.AsSpan(4), identifier);
		}

		internal int Identifier { get; }
		internal byte[] Payload { get; }
		internal Channel<long> Responses { get; } = Channel.CreateBounded<long>(new BoundedChannelOptions(1) { SingleReader = true, SingleWriter = true, AllowSynchronousContinuations = false });
	}

	private sealed class ResponseHandler(Sender[] senders, Action broadcast) : HandlerBase<TcpReceiveContext<ReadOnlySequence<byte>>>
	{
		protected override ValueTask OnHandleAsync(TcpReceiveContext<ReadOnlySequence<byte>> context, Zongsoft.Collections.Parameters parameters, CancellationToken cancellation)
		{
			var reader = new SequenceReader<byte>(context.Package);
			if(!reader.TryReadBigEndian(out int connection))
				throw new InvalidDataException();

			if(context.Package.Length == senders[0].Payload.Length && connection == -1)
			{
				foreach(var segment in context.Package)
				{
					foreach(var value in segment.Span)
					{
						if(value != 255)
							throw new InvalidDataException();
					}
				}

				broadcast();
				return ValueTask.CompletedTask;
			}

			if(!reader.TryReadBigEndian(out int identifier) || (uint)identifier >= senders.Length)
				throw new InvalidDataException();

			var sender = senders[identifier];
			var offset = 0;
			if(context.Package.Length != sender.Payload.Length)
				throw new InvalidDataException();

			foreach(var segment in context.Package)
			{
				if(!segment.Span.SequenceEqual(sender.Payload.AsSpan(offset, segment.Length)))
					throw new InvalidDataException();

				offset += segment.Length;
			}

			if(!sender.Responses.Writer.TryWrite(Stopwatch.GetTimestamp()))
				throw new InvalidDataException();

			return ValueTask.CompletedTask;
		}
	}
	#endregion
}
