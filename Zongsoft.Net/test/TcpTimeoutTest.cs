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
using System.Linq;
using System.Buffers;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Xunit;

using Zongsoft.Components;
using Zongsoft.Diagnostics;

namespace Zongsoft.Net.Tests;

[CollectionDefinition(nameof(TcpTimeoutCollection), DisableParallelization = true)]
public sealed class TcpTimeoutCollection { }

[Collection(nameof(TcpTimeoutCollection))]
public class TcpTimeoutTest
{
	[Fact]
	public async Task MonitorRecoversWhenConnectionJoinsDuringFaultAndDiagnosticSubscriberThrows()
	{
		var logger = new TcpCountingLogger();
		var clock = new ManualClock();
		var failure = new InvalidOperationException();
		var entered = TestNetwork.Signal();
		using var release = new ManualResetEventSlim();
		EventHandler<TcpDiagnosticEventArgs> diagnostic = (_, args) =>
		{
			if(args.Operation != "monitor" || !ReferenceEquals(args.Exception, failure))
				return;

			entered.TrySetResult();
			release.Wait(TestNetwork.Timeout);
			throw new ApplicationException();
		};
		await using var server = TestNetwork.Server(new() { TimeProvider = clock, IdleTimeout = TimeSpan.FromSeconds(5) });
		var closed = new ConcurrentDictionary<long, TcpChannelEventArgs<ReadOnlySequence<byte>>>();
		server.Channels.Closed += (_, args) => closed[args.Channel.Identifier] = args;
		Logging.Loggers.Add(logger);
		TcpDiagnostics.Error += diagnostic;

		try
		{
			await server.StartAsync();
			using var first = await TestNetwork.ConnectAsync(server.Address);
			await TestNetwork.UntilAsync(() => server.Channels.Count == 1 && clock.ActiveTimers == 1);
			var original = Assert.Single(server.Channels);
			clock.TimestampFailure = failure;
			var advancing = Task.Run(() => clock.Advance(TimeSpan.FromSeconds(1)));
			await entered.Task.WaitAsync(TestNetwork.Timeout);

			using var second = await TestNetwork.ConnectAsync(server.Address);
			await TestNetwork.UntilAsync(() => server.AcceptedConnections == 2);
			release.Set();
			await advancing.WaitAsync(TestNetwork.Timeout);
			Assert.Same(failure, await Record.ExceptionAsync(() => original.WaitAsync().WaitAsync(TestNetwork.Timeout)));
			await TestNetwork.UntilAsync(() => server.Channels.Count == 1 && clock.ActiveTimers == 1);
			var replacement = Assert.Single(server.Channels);
			clock.Advance(TimeSpan.FromSeconds(6));
			await TestNetwork.UntilAsync(() => replacement.WaitAsync().IsCompleted);

			await Assert.ThrowsAsync<TimeoutException>(() => replacement.WaitAsync());
			Assert.Equal(TcpCloseReason.IdleTimeout, closed[replacement.Identifier].CloseReason);
			await TestNetwork.UntilAsync(() => clock.ActiveTimers == 0);
			Assert.Equal(0, logger.Count);
		}
		finally
		{
			release.Set();

			try { await server.StopAsync().WaitAsync(TestNetwork.Timeout); }
			finally
			{
				TcpDiagnostics.Error -= diagnostic;
				Logging.Loggers.Remove(logger);
			}
		}
	}

	[Fact]
	public async Task AcceptanceContinuesWhenDiagnosticSubscriberThrows()
	{
		var logger = new TcpCountingLogger();
		var failure = new InvalidOperationException();
		var clock = new ManualClock { TimestampFailure = failure };
		var reported = TestNetwork.Signal<TcpDiagnosticEventArgs>();
		EventHandler<TcpDiagnosticEventArgs> diagnostic = (_, args) =>
		{
			if(args.Operation != "initialize" || !ReferenceEquals(args.Exception, failure))
				return;

			reported.TrySetResult(args);
			throw new ApplicationException();
		};
		var server = TestNetwork.Server(new() { TimeProvider = clock });
		Logging.Loggers.Add(logger);
		TcpDiagnostics.Error += diagnostic;

		try
		{
			await server.StartAsync();
			using var first = await TestNetwork.ConnectAsync(server.Address);
			var notification = await reported.Task.WaitAsync(TestNetwork.Timeout);
			Assert.Same(server, notification.Source);
			Assert.Same(failure, notification.Exception);
			using var second = await TestNetwork.ConnectAsync(server.Address);
			await TestNetwork.UntilAsync(() => server.Channels.Count == 1);

			Assert.Equal(1, server.AcceptedConnections);
			await server.StopAsync().WaitAsync(TestNetwork.Timeout);
			Assert.Equal(0, logger.Count);
		}
		finally
		{
			await Record.ExceptionAsync(() => server.DisposeAsync().AsTask().WaitAsync(TestNetwork.Timeout));
			TcpDiagnostics.Error -= diagnostic;
			Logging.Loggers.Remove(logger);
		}
	}

	[Fact]
	public async Task ShutdownDeadlineIncludesAcceptanceAndRetryStillWaitsForIt()
	{
		var failure = new InvalidOperationException();
		var clock = new ManualClock { TimestampFailure = failure };
		var entered = TestNetwork.Signal();
		using var release = new ManualResetEventSlim();
		EventHandler<TcpDiagnosticEventArgs> diagnostic = (_, args) =>
		{
			if(args.Operation != "initialize" || !ReferenceEquals(args.Exception, failure))
				return;

			entered.TrySetResult();
			release.Wait(TestNetwork.Timeout);
		};
		var server = TestNetwork.Server(new() { TimeProvider = clock, ShutdownTimeout = TimeSpan.FromSeconds(5) });
		TcpDiagnostics.Error += diagnostic;

		try
		{
			await server.StartAsync();
			using var first = await TestNetwork.ConnectAsync(server.Address);
			await entered.Task.WaitAsync(TestNetwork.Timeout);

			for(var i = 0; i < 2; i++)
			{
				var stopping = server.StopAsync();
				clock.Advance(TimeSpan.FromSeconds(6));
				await TestNetwork.UntilAsync(() => stopping.IsCompleted);

				await Assert.ThrowsAsync<TimeoutException>(() => stopping);
				Assert.Equal(WorkerState.Stopping, server.State);
			}

			release.Set();
			await server.StopAsync().WaitAsync(TestNetwork.Timeout);
			Assert.Equal(WorkerState.Stopped, server.State);

			await server.StartAsync();
			using var second = await TestNetwork.ConnectAsync(server.Address);
			await TestNetwork.UntilAsync(() => server.Channels.Count == 1);
			await server.StopAsync().WaitAsync(TestNetwork.Timeout);
			Assert.Empty(server.Channels);
		}
		finally
		{
			release.Set();
			await Record.ExceptionAsync(() => server.DisposeAsync().AsTask().WaitAsync(TestNetwork.Timeout));
			TcpDiagnostics.Error -= diagnostic;
		}
	}

	[Fact]
	public async Task SharedIdleScanUsesMonotonicTimeAndReleasesTimerAfterLastConnection()
	{
		using var metrics = new TcpMeasurements();
		var clock = new ManualClock();
		await using var server = TestNetwork.Server(new() { TimeProvider = clock, IdleTimeout = TimeSpan.FromSeconds(5), EnableDiagnostics = true });
		var closed = new ConcurrentDictionary<long, TcpChannelEventArgs<ReadOnlySequence<byte>>>();
		server.Channels.Closed += (_, args) => closed[args.Channel.Identifier] = args;
		await server.StartAsync();
		using var first = await TestNetwork.ConnectAsync(server.Address);
		using var second = await TestNetwork.ConnectAsync(server.Address);
		await TestNetwork.UntilAsync(() => server.Channels.Count == 2 && clock.ActiveTimers == 1);
		var channels = new List<TcpServerChannel<ReadOnlySequence<byte>>>(server.Channels);
		var timestampReads = clock.TimestampReads;
		metrics.Observe();
		Assert.Equal(timestampReads, clock.TimestampReads);
		clock.Utc = DateTimeOffset.UnixEpoch.AddYears(200);
		clock.Advance(TimeSpan.FromSeconds(1));
		await TestNetwork.UntilAsync(() => server.Statistics.ScanCount == 1);
		Assert.All(channels, channel => Assert.False(channel.IsClosed));
		clock.Utc = DateTimeOffset.UnixEpoch;
		clock.Advance(TimeSpan.FromSeconds(10));

		foreach(var channel in channels)
		{
			await Assert.ThrowsAsync<TimeoutException>(() => channel.WaitAsync().WaitAsync(TestNetwork.Timeout));
			Assert.Equal(TcpCloseReason.IdleTimeout, closed[channel.Identifier].CloseReason);
		}

		await TestNetwork.UntilAsync(() => clock.ActiveTimers == 0);
		Assert.Empty(server.Channels);
		await TestNetwork.UntilAsync(() => server.Statistics.ScanCount == 2 && metrics.Find("zongsoft.net.tcp.monitor.scan.duration").Count() == 2);
		Assert.Equal(TimeSpan.FromSeconds(6), server.Statistics.MaximumTimeoutDelay);
		Assert.Equal(2, metrics.Find("zongsoft.net.tcp.monitor.scan.duration").Count());
		Assert.Equal(2, metrics.Find("zongsoft.net.tcp.monitor.timeout.delay").Count());
		Assert.All(metrics.Find("zongsoft.net.tcp.monitor.timeout.delay"), sample =>
		{
			Assert.Equal("s", sample.Instrument.Unit);
			Assert.Equal(6, sample.Value);
		});
	}

	[Fact]
	public async Task DrippingBytesDoNotRenewFrameDeadline()
	{
		var clock = new ManualClock();
		await using var server = TestNetwork.Server(new()
		{
			TimeProvider = clock,
			EnableDiagnostics = true,
			IdleTimeout = TimeSpan.FromSeconds(100),
			FrameTimeout = TimeSpan.FromSeconds(5),
		});
		TcpChannelEventArgs<ReadOnlySequence<byte>> closed = null;
		server.Channels.Closed += (_, args) => closed = args;
		await server.StartAsync();
		using var socket = await TestNetwork.ConnectAsync(server.Address);
		await TestNetwork.UntilAsync(() => server.Channels.Count == 1 && clock.ActiveTimers == 1);
		var channel = Assert.Single(server.Channels);
		await TestNetwork.SendAsync(socket, new byte[] { 0 });
		await TestNetwork.UntilAsync(() => channel.Statistics.ReadCount > 0);
		clock.Advance(TimeSpan.FromSeconds(3));
		await TestNetwork.UntilAsync(() => server.Statistics.ScanCount == 1);
		await TestNetwork.SendAsync(socket, new byte[] { 0 });
		await TestNetwork.UntilAsync(() => channel.TotalBytesReceived == 2);
		clock.Advance(TimeSpan.FromSeconds(3));
		await Assert.ThrowsAsync<TimeoutException>(() => channel.WaitAsync().WaitAsync(TestNetwork.Timeout));
		Assert.Equal(TcpCloseReason.FrameTimeout, closed.CloseReason);
	}

	[Fact]
	public async Task MonitorReportsScansThatExceedTheirPeriod()
	{
		using var metrics = new TcpMeasurements();
		var clock = new ManualClock();
		await using var server = TestNetwork.Server(new() { TimeProvider = clock, EnableDiagnostics = true });
		await server.StartAsync();
		using var socket = await TestNetwork.ConnectAsync(server.Address);
		await TestNetwork.UntilAsync(() => server.Channels.Count == 1 && clock.ActiveTimers == 1);
		clock.TimestampStep = TimeSpan.FromSeconds(2);
		clock.Advance(TimeSpan.FromSeconds(1));
		await TestNetwork.UntilAsync(() => server.Statistics.ScanCount == 1);
		clock.TimestampStep = TimeSpan.Zero;

		Assert.Equal(1, server.Statistics.OverrunCount);
		Assert.Equal(TimeSpan.FromSeconds(2), server.Statistics.MaximumScanDuration);
		Assert.Equal(TimeSpan.Zero, server.Statistics.MaximumTimeoutDelay);
		Assert.False(Assert.Single(server.Channels).IsClosed);
		await TestNetwork.UntilAsync(() => metrics.Find("zongsoft.net.tcp.monitor.overruns").Any());
		var scan = Assert.Single(metrics.Find("zongsoft.net.tcp.monitor.scan.duration"));
		Assert.Equal(2, scan.Value);
		Assert.Equal("s", scan.Instrument.Unit);
		Assert.Equal(1, Assert.Single(metrics.Find("zongsoft.net.tcp.monitor.overruns")).Value);

		await server.StopAsync();
		await TestNetwork.UntilAsync(() => clock.ActiveTimers == 0);
		Assert.Equal(1, server.Statistics.OverrunCount);
	}

	[Fact]
	public async Task DisabledDiagnosticsDoNotPublishMonitorTimingMetrics()
	{
		using var metrics = new TcpMeasurements();
		var clock = new ManualClock();
		await using var server = TestNetwork.Server(new() { TimeProvider = clock });
		await server.StartAsync();
		using var socket = await TestNetwork.ConnectAsync(server.Address);
		await TestNetwork.UntilAsync(() => server.Channels.Count == 1 && clock.ActiveTimers == 1);
		var reads = clock.TimestampReads;
		clock.Advance(TimeSpan.FromSeconds(1));
		await TestNetwork.UntilAsync(() => clock.TimestampReads > reads);
		await server.StopAsync();
		Assert.Empty(metrics.Find("zongsoft.net.tcp.monitor.scan.duration"));
		Assert.Empty(metrics.Find("zongsoft.net.tcp.monitor.timeout.delay"));
		Assert.Empty(metrics.Find("zongsoft.net.tcp.monitor.overruns"));
	}

	[Fact]
	public async Task DisabledDiagnosticsDoNotExposeAnotherOwnersSharedStatistics()
	{
		var clock = new ManualClock();
		await using var first = TestNetwork.Server(new() { TimeProvider = clock, EnableDiagnostics = true });
		await using var second = TestNetwork.Server(new() { TimeProvider = clock });
		await first.StartAsync();
		await second.StartAsync();
		using var firstSocket = await TestNetwork.ConnectAsync(first.Address);
		using var secondSocket = await TestNetwork.ConnectAsync(second.Address);
		await TestNetwork.UntilAsync(() => first.Channels.Count == 1 && second.Channels.Count == 1 && clock.ActiveTimers == 1);
		clock.Advance(TimeSpan.FromSeconds(1));
		await TestNetwork.UntilAsync(() => first.Statistics.ScanCount == 1);

		Assert.Equal(0, first.Statistics.OverrunCount);
		Assert.Equal(0, second.Statistics.ScanCount);
		Assert.Equal(0, second.Statistics.OverrunCount);
		Assert.Equal(TimeSpan.Zero, second.Statistics.MaximumScanDuration);
		Assert.Equal(TimeSpan.Zero, second.Statistics.MaximumTimeoutDelay);
	}
	[Fact]
	public async Task ExpiredConnectDeadlineIsObservableAndTheClientCanRetry()
	{
		var clock = new ManualClock { ExpireTimersOnCreation = true };
		await using var server = TestNetwork.Server();
		await server.StartAsync();
		await using var client = TcpClient.CreateHeaded(new() { TimeProvider = clock });
		await Assert.ThrowsAsync<TimeoutException>(() => client.ConnectAsync(server.Address));

		Assert.Null(client.Channel);
		Assert.Equal(0, clock.ActiveTimers);

		clock.ExpireTimersOnCreation = false;
		var channel = await client.ConnectAsync(server.Address).WaitAsync(TestNetwork.Timeout);
		Assert.Same(channel, client.Channel);
		Assert.False(channel.IsClosed);
	}
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task DisconnectDeadlineSurvivesCanceledWaitWithoutClosingNewConnection(bool cancelWait)
	{
		var clock = new ManualClock();
		await using var server = TestNetwork.Server();
		server.Handler = new ReceiveHandler<ReadOnlySequence<byte>>((context, cancellation) => context.Channel.SendAsync(context.Package, cancellation));
		await server.StartAsync();
		await using var client = TcpClient.CreateHeaded(new() { TimeProvider = clock, ShutdownTimeout = TimeSpan.FromSeconds(5) });
		var entered = TestNetwork.Signal();
		var release = TestNetwork.Signal();
		var received = TestNetwork.Signal();
		var closing = new TaskCompletionSource<TcpChannelEventArgs<ReadOnlySequence<byte>>>(TaskCreationOptions.RunContinuationsAsynchronously);
		var calls = 0;
		client.Handler = new ReceiveHandler<ReadOnlySequence<byte>>(async (_, _) =>
		{
			if(Interlocked.Increment(ref calls) == 1)
			{
				entered.TrySetResult();
				await release.Task;
			}
			else
				received.TrySetResult();
		});
		var original = await client.ConnectAsync(server.Address).WaitAsync(TestNetwork.Timeout);
		client.Closing += (_, args) =>
		{
			if(ReferenceEquals(args.Channel, original))
				closing.TrySetResult(args);
		};

		try
		{
			await original.SendAsync(new ReadOnlySequence<byte>(new byte[] { 1 }));
			await entered.Task.WaitAsync(TestNetwork.Timeout);
			using var cancellation = new CancellationTokenSource();
			var disconnecting = client.DisconnectAsync(cancellation.Token).AsTask();

			if(cancelWait)
			{
				cancellation.Cancel();
				await Assert.ThrowsAnyAsync<OperationCanceledException>(() => disconnecting);
			}

			var current = await client.ConnectAsync().WaitAsync(TestNetwork.Timeout);
			Assert.NotSame(original, current);
			clock.Advance(TimeSpan.FromSeconds(6));

			if(!cancelWait)
				await Assert.ThrowsAsync<TimeoutException>(() => disconnecting.WaitAsync(TestNetwork.Timeout));

			var notification = await closing.Task.WaitAsync(TestNetwork.Timeout);
			Assert.Equal(TcpCloseReason.ShutdownTimeout, notification.CloseReason);
			Assert.IsType<TimeoutException>(notification.Error);
			Assert.False(original.WaitAsync().IsCompleted);
			Assert.Same(current, client.Channel);
			Assert.False(current.IsClosed);
			await current.SendAsync(new ReadOnlySequence<byte>(new byte[] { 2 }));
			await received.Task.WaitAsync(TestNetwork.Timeout);
		}
		finally
		{
			release.TrySetResult();
			await Record.ExceptionAsync(() => original.WaitAsync().WaitAsync(TestNetwork.Timeout));
		}
	}

	[Fact]
	public async Task ClosingTransportEndsDrainWithoutWaitingForShutdownDeadline()
	{
		var clock = new ManualClock();
		using var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) { ReceiveBufferSize = 1024 };
		listener.Bind(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 0));
		listener.Listen(1);
		await using var client = TcpClient.CreateHeaded(new()
		{
			TimeProvider = clock,
			MaximumPacketLength = 8 * 1024 * 1024,
			MaximumFrameLength = 8 * 1024 * 1024 + 4,
			MaximumBufferedReceiveBytes = 16 * 1024 * 1024,
			MaximumPendingSendBytes = 16 * 1024 * 1024,
			SendPauseThreshold = 16 * 1024 * 1024,
		});
		var connecting = client.ConnectAsync(listener.LocalEndPoint);
		using var peer = await listener.AcceptAsync();
		var channel = await connecting.WaitAsync(TestNetwork.Timeout);

		try
		{
			await channel.SendAsync(new ReadOnlySequence<byte>(new byte[8 * 1024 * 1024]));
			Assert.True(channel.PendingSendBytes > 0);
			peer.Shutdown(SocketShutdown.Send);
			await TestNetwork.UntilAsync(() => clock.ActiveTimers >= 2);
			await channel.CloseAsync().AsTask().WaitAsync(TestNetwork.Timeout);
			await channel.WaitAsync().WaitAsync(TestNetwork.Timeout);
			Assert.Equal(0, channel.PendingSendBytes);
		}
		finally
		{
			clock.Advance(TimeSpan.FromMinutes(1));
			await Record.ExceptionAsync(() => channel.WaitAsync().WaitAsync(TestNetwork.Timeout));
		}
	}

	[Fact]
	public async Task FailedChannelInitializationClosesConnectedSocketAndAllowsRetry()
	{
		var failure = new InvalidOperationException();
		var clock = new ManualClock { TimestampFailure = failure };
		using var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		listener.Bind(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 0));
		listener.Listen(1);
		await using var client = TcpClient.CreateHeaded(new() { TimeProvider = clock });
		var connecting = client.ConnectAsync(listener.LocalEndPoint);
		using var peer = await listener.AcceptAsync();
		Assert.Same(failure, await Record.ExceptionAsync(() => connecting.WaitAsync(TestNetwork.Timeout)));
		Assert.Null(client.Channel);
		Assert.Equal(0, await peer.ReceiveAsync(new byte[1], SocketFlags.None).WaitAsync(TestNetwork.Timeout));
		Assert.Equal(0, clock.ActiveTimers);

		var retry = client.ConnectAsync();
		using var replacement = await listener.AcceptAsync();
		Assert.Same(await retry.WaitAsync(TestNetwork.Timeout), client.Channel);
	}

	[Fact]
	public async Task ShutdownDuringInitializationReleasesSocketAndAdmissionBeforeRestart()
	{
		var clock = new ManualClock();
		var entered = TestNetwork.Signal();
		using var release = new ManualResetEventSlim();
		clock.TimestampCallback = () =>
		{
			entered.TrySetResult();
			Assert.True(release.Wait(TestNetwork.Timeout));
		};
		await using var server = TestNetwork.Server(new()
		{
			TimeProvider = clock,
			ShutdownTimeout = TimeSpan.FromSeconds(5),
			MaximumConnections = 1,
		});
		await server.StartAsync();
		using var original = await TestNetwork.ConnectAsync(server.Address);

		try
		{
			await entered.Task.WaitAsync(TestNetwork.Timeout);
			var stopping = server.StopAsync();
			clock.Advance(TimeSpan.FromSeconds(6));
			await TestNetwork.UntilAsync(() => stopping.IsCompleted);
			await Assert.ThrowsAsync<TimeoutException>(() => stopping);

			release.Set();
			await server.StopAsync().WaitAsync(TestNetwork.Timeout);
			Assert.Equal(0, server.AcceptedConnections);
			Assert.Empty(server.Channels);
			Assert.Equal(0, await original.ReceiveAsync(new byte[1], SocketFlags.None).WaitAsync(TestNetwork.Timeout));

			await server.StartAsync();
			using var replacement = await TestNetwork.ConnectAsync(server.Address);
			await TestNetwork.UntilAsync(() => server.Channels.Count == 1);
			Assert.Equal(1, server.AcceptedConnections);
			Assert.Equal(0, server.RejectedConnections);
		}
		finally { release.Set(); }
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task NotificationTimeoutDoesNotChangeClosedChannelResult(bool clientSide)
	{
		var clock = new ManualClock();
		await using var server = TestNetwork.Server(new() { TimeProvider = clock, ShutdownTimeout = TimeSpan.FromSeconds(5) });
		await using var client = TcpClient.CreateHeaded(new() { TimeProvider = clock, ShutdownTimeout = TimeSpan.FromSeconds(5) });
		var entered = TestNetwork.Signal<TcpChannelEventArgs<ReadOnlySequence<byte>>>();
		using var release = new ManualResetEventSlim();
		EventHandler<TcpChannelEventArgs<ReadOnlySequence<byte>>> closed = (_, args) =>
		{
			entered.TrySetResult(args);
			release.Wait(TestNetwork.Timeout);
		};

		if(clientSide)
			client.Closed += closed;
		else
			server.Channels.Closed += closed;

		await server.StartAsync();
		var connected = await client.ConnectAsync(server.Address);
		await TestNetwork.UntilAsync(() => server.Channels.Count == 1);
		TcpChannelBase<ReadOnlySequence<byte>> channel = clientSide ? connected : Assert.Single(server.Channels);

		try
		{
			await channel.CloseAsync();
			var notification = await entered.Task.WaitAsync(TestNetwork.Timeout);
			var stopping = clientSide ? client.DisconnectAsync().AsTask() : server.StopAsync();
			clock.Advance(TimeSpan.FromSeconds(6));
			await TestNetwork.UntilAsync(() => stopping.IsCompleted);

			await Assert.ThrowsAsync<TimeoutException>(() => stopping);
			Assert.Null(notification.Error);
			Assert.False(channel.WaitAsync().IsCompleted);

			release.Set();
			await channel.WaitAsync().WaitAsync(TestNetwork.Timeout);

			if(clientSide)
				await client.DisconnectAsync();
			else
				await server.StopAsync();
		}
		finally { release.Set(); }
	}

	private sealed class ManualClock : TimeProvider
	{
		private readonly object _sync = new();
		private readonly List<ManualTimer> _timers = [];
		private long _ticks;
		private long _step;
		private int _reads;
		internal Exception TimestampFailure;
		internal Action TimestampCallback;
		internal bool ExpireTimersOnCreation { get; set; }
		internal DateTimeOffset Utc { get; set; } = DateTimeOffset.UnixEpoch;
		internal TimeSpan TimestampStep
		{
			get => TimeSpan.FromTicks(Interlocked.Read(ref _step));
			set => Interlocked.Exchange(ref _step, value.Ticks);
		}
		internal int TimestampReads => Volatile.Read(ref _reads);
		internal int ActiveTimers
		{
			get
			{
				lock(_sync)
					return _timers.Count;
			}
		}
		public override long TimestampFrequency => TimeSpan.TicksPerSecond;
		public override DateTimeOffset GetUtcNow() => this.Utc;
		public override long GetTimestamp()
		{
			Interlocked.Exchange(ref this.TimestampCallback, null)?.Invoke();
			var failure = Interlocked.Exchange(ref this.TimestampFailure, null);
			if(failure != null)
				throw failure;

			Interlocked.Increment(ref _reads);
			return Interlocked.Add(ref _ticks, Interlocked.Read(ref _step));
		}

		public override ITimer CreateTimer(TimerCallback callback, object state, TimeSpan dueTime, TimeSpan period)
		{
			lock(_sync)
			{
				var timer = new ManualTimer(this, callback, state);
				_timers.Add(timer);
				timer.Change(dueTime, period);

				if(this.ExpireTimersOnCreation && period == Timeout.InfiniteTimeSpan)
					timer.Invoke();

				return timer;
			}
		}

		internal void Advance(TimeSpan elapsed)
		{
			var callbacks = new List<ManualTimer>();

			lock(_sync)
			{
				_ticks += elapsed.Ticks;
				foreach(var timer in _timers)
				{
					if(timer.Due <= _ticks)
					{
						callbacks.Add(timer);
						timer.Due = timer.Period > 0 ? _ticks + timer.Period : long.MaxValue;
					}
				}
			}

			foreach(var timer in callbacks)
				timer.Invoke();
		}

		private sealed class ManualTimer(ManualClock clock, TimerCallback callback, object state) : ITimer
		{
			private bool _disposed;
			internal long Due;
			internal long Period;
			internal void Invoke() => callback(state);
			public bool Change(TimeSpan dueTime, TimeSpan period)
			{
				lock(clock._sync)
				{
					if(_disposed)
						return false;

					this.Due = dueTime < TimeSpan.Zero ? long.MaxValue : clock._ticks + dueTime.Ticks;
					this.Period = period.Ticks;
					return true;
				}
			}
			public void Dispose()
			{
				lock(clock._sync)
				{
					_disposed = true;
					clock._timers.Remove(this);
				}
			}
			public ValueTask DisposeAsync() { this.Dispose(); return ValueTask.CompletedTask; }
		}
	}
}
