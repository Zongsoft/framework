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
using System.Linq;
using System.Buffers;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Diagnostics;
using System.Buffers.Binary;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Zongsoft.Net.Benchmarks;

internal static class DeviceLoad
{
	#region 负载协调
	internal static async Task RunAsync(LoadOptions options, CancellationToken cancellation)
	{
		ArgumentOutOfRangeException.ThrowIfZero(options.Port);
		ArgumentOutOfRangeException.ThrowIfNotEqual(options.Broadcast, 0);
		ArgumentOutOfRangeException.ThrowIfNotEqual(options.Disturbance, 0);

		using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
		using var connecting = new SemaphoreSlim(Math.Min(128, options.TotalConnections));
		var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var closing = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var starts = Enumerable.Range(0, options.Rounds).Select(_ => new TaskCompletionSource<long>(TaskCreationOptions.RunContinuationsAsynchronously)).ToArray();
		var finishes = Enumerable.Range(0, options.Rounds).Select(_ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously)).ToArray();
		var remaining = Enumerable.Repeat(options.Connections, options.Rounds).ToArray();
		var available = options.TotalConnections;
		var latencies = new long[options.Rounds][];
		var wireLatencies = new long[options.Rounds][];
		var accumulation = new long[options.Rounds][];
		var heartbeat = new ConcurrentQueue<long>();
		var ordinaryProbe = new List<long>();
		var heartbeatProbe = new List<long>();
		var tasks = new Task[options.TotalConnections];
		Exception failure = null;

		for(var round = 0; round < options.Rounds; round++)
		{
			latencies[round] = new long[options.Connections * options.Messages];
			wireLatencies[round] = new long[options.Connections * options.Messages];
			accumulation[round] = new long[options.Connections * options.Messages];
		}

		Console.WriteLine(JsonSerializer.Serialize(new { Event = "Starting", Options = options, Environment = EnvironmentReport.Capture(), Process = ProcessSnapshot.Capture() }));

		for(var index = 0; index < options.Connections; index++)
			tasks[index] = RunConnectionAsync(index);

		if(options.Mixed != 0)
		{
			tasks[^2] = RunProbeAsync(options.Connections, 100, ordinaryProbe);
			tasks[^1] = RunProbeAsync(options.Connections + 1, 1000, heartbeatProbe);
		}

		try
		{
			await ready.Task.WaitAsync(lifetime.Token);
			Console.WriteLine(JsonSerializer.Serialize(new { Event = "Connected", Connections = options.TotalConnections, Process = ProcessSnapshot.Capture() }));

			await Task.Delay(TimeSpan.FromSeconds(options.SettleSeconds), lifetime.Token);

			for(var round = 0; round < options.Rounds; round++)
			{
				var beginning = ProcessSnapshot.Capture();
				var started = Stopwatch.GetTimestamp();
				starts[round].TrySetResult(started);
				await finishes[round].Task.WaitAsync(lifetime.Token);
				var elapsed = Stopwatch.GetElapsedTime(started);

				Console.WriteLine(JsonSerializer.Serialize(new
				{
					Event = "HighCompleted",
					Round = round + 1,
					Messages = latencies[round].Length,
					ElapsedSeconds = elapsed.TotalSeconds,
					MessagesPerSecond = latencies[round].Length / elapsed.TotalSeconds,
					ScheduledLatency = Describe(latencies[round]),
					WireLatency = Describe(wireLatencies[round]),
					AccumulationAndQueue = Describe(accumulation[round]),
					Before = beginning,
					After = ProcessSnapshot.Capture(),
				}));

				if(round + 1 < options.Rounds)
				{
					Console.WriteLine(JsonSerializer.Serialize(new { Event = "IdleStarted", Round = round + 1, Process = ProcessSnapshot.Capture() }));
					await Task.Delay(TimeSpan.FromSeconds(options.IdleSeconds), lifetime.Token);
					Console.WriteLine(JsonSerializer.Serialize(new { Event = "IdleCompleted", Round = round + 1, Heartbeats = heartbeat.Count, Process = ProcessSnapshot.Capture() }));
				}
			}

			closing.TrySetResult();
			await Task.WhenAll(tasks);

			Console.WriteLine(JsonSerializer.Serialize(new
			{
				Event = "Completed",
				Messages = (long)options.Connections * options.Messages * options.Rounds,
				Errors = 0,
				Rejected = 0,
				Timeouts = 0,
				HeartbeatLatency = Describe(heartbeat.ToArray()),
				OrdinaryProbeLatency = Describe(ordinaryProbe.ToArray()),
				HeartbeatProbeLatency = Describe(heartbeatProbe.ToArray()),
				Process = ProcessSnapshot.Capture(),
			}));

			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();
			Console.WriteLine(JsonSerializer.Serialize(new { Event = "ForcedGcDiagnostic", Process = ProcessSnapshot.Capture() }));
		}
		catch(Exception exception)
		{
			Console.WriteLine(JsonSerializer.Serialize(new { Event = "Failed", Error = (failure ?? exception).GetType().FullName, SocketError = (failure as SocketException)?.SocketErrorCode.ToString(), ConnectionsReady = options.TotalConnections - available, Process = ProcessSnapshot.Capture() }));
			throw;
		}
		finally
		{
			await lifetime.CancelAsync();
			closing.TrySetResult();
			await Task.WhenAll(tasks);
		}

		async Task RunConnectionAsync(int index)
		{
			using var session = new Session(index, options);

			try
			{
				await ConnectAsync(session);
				await session.ExchangeAsync(options.Warmup, 1, lifetime.Token);

				if(Interlocked.Decrement(ref available) == 0)
					ready.TrySetResult();

				for(var round = 0; round < options.Rounds; round++)
				{
					var started = await starts[round].Task.WaitAsync(lifetime.Token);
					await session.MeasureAsync(started, latencies[round], wireLatencies[round], accumulation[round], lifetime.Token);

					if(Interlocked.Decrement(ref remaining[round]) == 0)
						finishes[round].TrySetResult();

					if(round + 1 < options.Rounds)
					{
						var random = new Random(index + round);

						while(!starts[round + 1].Task.IsCompleted)
						{
							try
							{
								await starts[round + 1].Task.WaitAsync(TimeSpan.FromSeconds(options.HeartbeatSeconds * (1 + random.NextDouble() * 0.05)), lifetime.Token);
							}
							catch(TimeoutException)
							{
								var timestamp = Stopwatch.GetTimestamp();
								await session.ExchangeAsync(1, 1, lifetime.Token);
								heartbeat.Enqueue(Stopwatch.GetTimestamp() - timestamp);
							}
						}
					}
				}

				await closing.Task.WaitAsync(lifetime.Token);
				session.Shutdown();
			}
			catch(Exception exception)
			{
				Interlocked.CompareExchange(ref failure, exception, null);
				await lifetime.CancelAsync();
				throw;
			}
		}

		async Task RunProbeAsync(int index, int interval, List<long> samples)
		{
			using var session = new Session(index, options with { Batch = 1 });

			try
			{
				await ConnectAsync(session);
				if(Interlocked.Decrement(ref available) == 0)
					ready.TrySetResult();

				await starts[0].Task.WaitAsync(lifetime.Token);
				while(!closing.Task.IsCompleted)
				{
					var timestamp = Stopwatch.GetTimestamp();
					await session.ExchangeAsync(1, 1, lifetime.Token);
					samples.Add(Stopwatch.GetTimestamp() - timestamp);

					try { await closing.Task.WaitAsync(TimeSpan.FromMilliseconds(interval), lifetime.Token); }
					catch(TimeoutException) { }
				}

				session.Shutdown();
			}
			catch(Exception exception)
			{
				Interlocked.CompareExchange(ref failure, exception, null);
				await lifetime.CancelAsync();
				throw;
			}
		}

		async Task ConnectAsync(Session session)
		{
			await connecting.WaitAsync(lifetime.Token);

			try { await session.ConnectAsync(lifetime.Token); }
			finally { connecting.Release(); }
		}
	}

	private static object Describe(long[] samples)
	{
		Array.Sort(samples);
		return new { Count = samples.Length, P50Microseconds = Percentile(0.5), P95Microseconds = Percentile(0.95), P99Microseconds = Percentile(0.99), P999Microseconds = Percentile(0.999), MaximumMicroseconds = Percentile(1) };

		double Percentile(double quantile) => samples.Length == 0 ? 0 : samples[(int)Math.Ceiling(samples.Length * quantile) - 1] * 1_000_000.0 / Stopwatch.Frequency;
	}
	#endregion

	#region 设备会话
	private sealed class Session : IDisposable
	{
		private readonly int _index;
		private readonly LoadOptions _options;
		private readonly Socket _socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
		private readonly byte[] _output;
		private readonly byte[] _input;
		private NetworkStream _stream;
		private int _sequence;

		internal Session(int index, LoadOptions options)
		{
			_index = index;
			_options = options;
			_input = new byte[options.PayloadLength + 4];
			_output = new byte[_input.Length * options.Batch];
			new Random(index).NextBytes(_output);

			for(var offset = 0; offset < _output.Length; offset += _input.Length)
				BinaryPrimitives.WriteInt32BigEndian(_output.AsSpan(offset), options.PayloadLength);
		}

		internal async Task ConnectAsync(CancellationToken cancellation)
		{
			await _socket.ConnectAsync(new IPEndPoint(IPAddress.Loopback, _options.Port), cancellation);
			_stream = new NetworkStream(_socket, false);
		}

		internal async Task ExchangeAsync(int messages, int batch, CancellationToken cancellation)
		{
			for(var offset = 0; offset < messages; offset += batch)
			{
				var count = Math.Min(batch, messages - offset);
				this.Prepare(count);
				await _stream.WriteAsync(_output.AsMemory(0, count * _input.Length), cancellation);

				for(var frame = 0; frame < count; frame++)
					await this.ReceiveAsync(frame, cancellation);
			}
		}

		internal async Task MeasureAsync(long started, long[] latencies, long[] wire, long[] accumulation, CancellationToken cancellation)
		{
			for(var offset = 0; offset < _options.Messages; offset += _options.Batch)
			{
				var count = Math.Min(_options.Batch, _options.Messages - offset);
				var created = Stopwatch.GetTimestamp();
				this.Prepare(count);

				if(_options.AccumulationMilliseconds > 0)
					await Task.Delay(_options.AccumulationMilliseconds, cancellation);

				if(_options.Rate > 0)
				{
					var due = Scheduled(offset + count - 1);
					var remaining = (due - Stopwatch.GetTimestamp()) * 1000.0 / Stopwatch.Frequency;

					if(remaining > 0)
						await Task.Delay(TimeSpan.FromMilliseconds(Math.Max(1, remaining)), cancellation);
				}

				var sent = Stopwatch.GetTimestamp();
				await _stream.WriteAsync(_output.AsMemory(0, count * _input.Length), cancellation);

				for(var frame = 0; frame < count; frame++)
				{
					await this.ReceiveAsync(frame, cancellation);
					var finished = Stopwatch.GetTimestamp();
					var scheduled = _options.Rate > 0 ? Scheduled(offset + frame) : created;
					var index = _index * _options.Messages + offset + frame;
					latencies[index] = finished - scheduled;
					wire[index] = finished - sent;
					accumulation[index] = sent - scheduled;
				}
			}

			long Scheduled(int sequence) => started + (long)(((long)sequence * _options.Connections + _index) * (double)Stopwatch.Frequency / _options.Rate);
		}

		private void Prepare(int count)
		{
			if(_options.PayloadLength < 8)
				return;

			for(var frame = 0; frame < count; frame++)
			{
				var body = _output.AsSpan(frame * _input.Length + 4);
				BinaryPrimitives.WriteInt32BigEndian(body, _index);
				BinaryPrimitives.WriteInt32BigEndian(body[4..], _sequence++);
			}
		}

		private async Task ReceiveAsync(int frame, CancellationToken cancellation)
		{
			await _stream.ReadExactlyAsync(_input, cancellation);
			if(!_input.AsSpan().SequenceEqual(_output.AsSpan(frame * _input.Length, _input.Length)))
				throw new InvalidDataException();
		}

		internal void Shutdown() => _socket.Shutdown(SocketShutdown.Send);
		public void Dispose()
		{
			_stream?.Dispose();
			_socket.Dispose();
		}
	}
	#endregion
}
