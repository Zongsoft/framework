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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Xunit;

namespace Zongsoft.Net.Tests;

public class TcpTimeoutTest
{
	[Fact]
	public async Task SharedIdleScanUsesMonotonicTimeAndReleasesTimerAfterLastConnection()
	{
		var clock = new ManualClock();
		await using var server = TestNetwork.Server(new() { TimeProvider = clock, IdleTimeout = TimeSpan.FromSeconds(5), EnableDiagnostics = true });
		var closed = new ConcurrentDictionary<long, TcpChannelEventArgs<ReadOnlySequence<byte>>>();
		server.Channels.Closed += (_, args) => closed[args.Channel.Identifier] = args;
		await server.StartAsync();
		using var first = await TestNetwork.ConnectAsync(server.Address);
		using var second = await TestNetwork.ConnectAsync(server.Address);
		await TestNetwork.UntilAsync(() => server.Channels.Count == 2 && clock.ActiveTimers == 1);
		var channels = new List<TcpServerChannel<ReadOnlySequence<byte>>>(server.Channels);
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
		await TestNetwork.UntilAsync(() => server.Statistics.ScanCount == 2);
		Assert.Equal(TimeSpan.FromSeconds(6), server.Statistics.MaximumTimeoutDelay);
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

		await server.StopAsync();
		await TestNetwork.UntilAsync(() => clock.ActiveTimers == 0);
		Assert.Equal(1, server.Statistics.OverrunCount);
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
	private sealed class ManualClock : TimeProvider
	{
		private readonly object _sync = new();
		private readonly List<ManualTimer> _timers = [];
		private long _ticks;
		private long _step;
		private int _reads;
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
