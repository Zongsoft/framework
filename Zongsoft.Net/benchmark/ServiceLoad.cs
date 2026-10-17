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
using System.Buffers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Components;
using Zongsoft.Collections;

namespace Zongsoft.Net.Benchmarks;

internal static class ServiceLoad
{
	#region 服务负载
	internal static async Task RunAsync(LoadOptions options, CancellationToken cancellation)
	{
		await using var server = TcpServer.CreateHeaded(new()
		{
			MaximumConnections = options.TotalConnections,
			ZeroLengthReads = options.ZeroLengthReads != 0,
			ReceiveFrameBudget = options.FrameBudget,
			ReceiveByteBudget = options.ByteBudget,
			EnableDiagnostics = options.Diagnostics != 0,
		});
		server.Address = new IPEndPoint(IPAddress.Loopback, options.Port);
		server.Handler = new EchoHandler(options.PayloadLength);
		var broadcast = options.Broadcast == 0 ? null : new byte[options.PayloadLength];

		if(broadcast != null)
			Array.Fill(broadcast, (byte)255);
		await server.StartAsync(cancellation: cancellation);
		Console.WriteLine(JsonSerializer.Serialize(new { Event = "Ready", Port = server.Address.Port, Options = options, Environment = EnvironmentReport.Capture(), Process = ProcessSnapshot.Capture() }));

		using var broadcastingCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
		var broadcasting = broadcast == null ? Task.CompletedTask : BroadcastAsync();
		var reportedOverruns = 0L;

		try
		{
			do
			{
				await Task.Delay(TimeSpan.FromSeconds(1), cancellation);

				Report("Sample");
			}
			while(server.AcceptedConnections < options.TotalConnections || server.Channels.Count != 0);

			await broadcastingCancellation.CancelAsync();
			await broadcasting;
			await server.StopAsync();
			Report("Completed");
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();
			Report("ForcedGcDiagnostic");
		}
		catch
		{
			Report("Failed");
			throw;
		}
		finally
		{
			await broadcastingCancellation.CancelAsync();
			await broadcasting;
		}

		async Task BroadcastAsync()
		{
			try
			{
				while(true)
				{
					await Task.Delay(TimeSpan.FromSeconds(1), broadcastingCancellation.Token);
					var result = await server.BroadcastAsync(new ReadOnlySequence<byte>(broadcast), broadcastingCancellation.Token);
					Console.WriteLine(JsonSerializer.Serialize(new { Event = "Broadcast", Result = result }));
				}
			}
			catch(OperationCanceledException) when(broadcastingCancellation.IsCancellationRequested) { }
		}

		void Report(string stage)
		{
			var statistics = server.Statistics;
			if(statistics.OverrunCount > reportedOverruns)
			{
				Console.WriteLine(JsonSerializer.Serialize(new { Event = "MaintenanceCapacityExceeded", Maintenance = statistics }));
				reportedOverruns = statistics.OverrunCount;
			}

			var reads = 0L;
			var exhausted = 0L;
			var maximum = 0L;
			var waits = 0L;
			var pending = 0L;

			foreach(var channel in server.Channels)
			{
				var channelStatistics = channel.Statistics;
				reads += channelStatistics.ReadCount;
				exhausted += channelStatistics.BudgetExhaustions;
				maximum = Math.Max(maximum, channelStatistics.MaximumFramesPerRead);
				waits += channelStatistics.SendWaitCount;
				pending += channel.PendingSendBytes;
			}

			Console.WriteLine(JsonSerializer.Serialize(new
			{
				Event = stage,
				Connections = server.Channels.Count,
				server.AcceptedConnections,
				server.RejectedConnections,
				server.TotalFramesReceived,
				server.TotalFramesSent,
				server.TotalBytesReceived,
				server.TotalBytesSent,
				ActiveReadCount = reads,
				ActiveBudgetExhaustions = exhausted,
				ActiveMaximumFramesPerRead = maximum,
				ActiveSendWaits = waits,
				ActivePendingSendBytes = pending,
				Maintenance = statistics,
				Process = ProcessSnapshot.Capture(),
			}));
		}
	}
	#endregion

	#region 回显处理
	private sealed class EchoHandler(int length) : HandlerBase<TcpReceiveContext<ReadOnlySequence<byte>>>
	{
		protected override ValueTask OnHandleAsync(TcpReceiveContext<ReadOnlySequence<byte>> context, Parameters parameters, CancellationToken cancellation)
		{
			if(context.Package.Length != length)
				throw new InvalidDataException();

			return context.Channel.SendAsync(context.Package, cancellation);
		}
	}
	#endregion
}
