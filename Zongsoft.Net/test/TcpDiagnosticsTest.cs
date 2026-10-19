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
using System.Linq;
using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Xunit;

using Zongsoft.Diagnostics;

namespace Zongsoft.Net.Tests;

//全局日志注册、Meter 和 ActivitySource 监听需要与其他网络测试隔离。
[Collection(nameof(TcpTimeoutCollection))]
public class TcpDiagnosticsTest
{
	[Fact]
	public async Task ChannelFaultAndThrowingSubscribersDoNotCallGlobalLogger()
	{
		await using var server = TestNetwork.Server();
		var failure = new ApplicationException();
		var subscriberFailure = new InvalidOperationException();
		server.Handler = new ReceiveHandler<ReadOnlySequence<byte>>((_, _) => throw failure);
		server.Channels.Error += (_, _) => throw subscriberFailure;
		var errors = new ConcurrentQueue<TcpChannelEventArgs<ReadOnlySequence<byte>>>();
		server.Channels.Error += (_, args) => errors.Enqueue(args);
		var diagnostics = new ConcurrentQueue<TcpDiagnosticEventArgs>();
		EventHandler<TcpDiagnosticEventArgs> first = (_, _) => throw new NotSupportedException();
		EventHandler<TcpDiagnosticEventArgs> second = (_, args) => diagnostics.Enqueue(args);
		var logger = new TcpCountingLogger();
		Logging.Loggers.Add(logger);
		TcpDiagnostics.Error += first;
		TcpDiagnostics.Error += second;

		try
		{
			await server.StartAsync();
			using var socket = await TestNetwork.ConnectAsync(server.Address);
			await TestNetwork.UntilAsync(() => server.Channels.Count == 1);
			var channel = Assert.Single(server.Channels);
			await TestNetwork.SendAsync(socket, TestNetwork.Frame(1, 2, 3));
			Assert.Same(failure, await Record.ExceptionAsync(() => channel.WaitAsync().WaitAsync(TestNetwork.Timeout)));
			Assert.Same(failure, Assert.Single(errors).Error);
			var diagnostic = Assert.Single(diagnostics);
			Assert.Equal("notification", diagnostic.Operation);
			Assert.Same(server.Channels, diagnostic.Source);
			Assert.Same(subscriberFailure, diagnostic.Exception);
			Assert.Empty(server.Channels);
			Assert.Equal(0, logger.Count);
		}
		finally
		{
			TcpDiagnostics.Error -= first;
			TcpDiagnostics.Error -= second;
			Logging.Loggers.Remove(logger);
		}
	}

	[Fact]
	public async Task DiagnosticReentryDoesNotRecursivelyNotifyOrPreventChannelCompletion()
	{
		await using var server = TestNetwork.Server();
		await server.StartAsync();
		await using var first = TcpClient.Headed;
		await using var second = TcpClient.Headed;
		var firstChannel = await first.ConnectAsync(server.Address);
		var secondChannel = await second.ConnectAsync(server.Address);
		first.Closing += (_, _) => throw new InvalidOperationException();
		second.Closing += (_, _) => throw new InvalidOperationException();
		var count = 0;
		Task nested = null;
		EventHandler<TcpDiagnosticEventArgs> diagnostic = (_, args) =>
		{
			if(args.Operation != "notification")
				return;

			Interlocked.Increment(ref count);
			if(ReferenceEquals(args.Source, first))
				nested = secondChannel.CloseAsync().AsTask();
		};
		TcpDiagnostics.Error += diagnostic;

		try
		{
			await firstChannel.CloseAsync();
			await firstChannel.WaitAsync().WaitAsync(TestNetwork.Timeout);
			Assert.NotNull(nested);
			await nested.WaitAsync(TestNetwork.Timeout);
			await secondChannel.WaitAsync().WaitAsync(TestNetwork.Timeout);
			Assert.Equal(1, count);
			Assert.Null(first.Channel);
			Assert.Null(second.Channel);
		}
		finally { TcpDiagnostics.Error -= diagnostic; }
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task ConnectTracePreservesParentAndDoesNotBecomeReceiveLoopParent(bool sampled)
	{
		using var metrics = new TcpMeasurements();
		var stopped = new ConcurrentQueue<Activity>();
		using var listener = new ActivityListener
		{
			ShouldListenTo = source => source.Name == TcpDiagnostics.Name,
			Sample = (ref ActivityCreationOptions<ActivityContext> _) => sampled ? ActivitySamplingResult.AllDataAndRecorded : ActivitySamplingResult.None,
			ActivityStopped = stopped.Enqueue,
		};
		ActivitySource.AddActivityListener(listener);
		await using var server = TestNetwork.Server();
		await server.StartAsync();
		await using var client = TcpClient.Headed;
		var received = TestNetwork.Signal<ActivityContext>();
		client.Handler = new ReceiveHandler<ReadOnlySequence<byte>>((_, _) =>
		{
			received.TrySetResult(Activity.Current?.Context ?? default);
			return ValueTask.CompletedTask;
		});
		using var parent = new Activity("caller").SetIdFormat(ActivityIdFormat.W3C).Start();
		var channels = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => client.ConnectAsync(server.Address)));
		Assert.All(channels, channel => Assert.Same(channels[0], channel));
		Assert.Same(parent, Activity.Current);
		await TestNetwork.UntilAsync(() => server.Channels.Count == 1);
		await server.BroadcastAsync(ReadOnlySequence<byte>.Empty);
		var handlerContext = await received.Task.WaitAsync(TestNetwork.Timeout);
		Assert.Equal(default(ActivityContext), handlerContext);
		var measurement = Assert.Single(metrics.Find("zongsoft.net.tcp.connect.duration"));
		Assert.Equal("success", measurement.Tag("zongsoft.net.outcome"));
		Assert.Equal("s", measurement.Instrument.Unit);
		Assert.True(measurement.Value >= 0);

		if(sampled)
		{
			var activity = Assert.Single(stopped);
			Assert.Equal("tcp.connect", activity.OperationName);
			Assert.Equal(ActivityKind.Client, activity.Kind);
			Assert.Equal(parent.TraceId, activity.TraceId);
			Assert.Equal(parent.SpanId, activity.ParentSpanId);
			Assert.Equal(ActivityStatusCode.Unset, activity.Status);
			Assert.Equal("tcp", activity.GetTagItem("network.transport"));
			Assert.Equal("success", activity.GetTagItem("zongsoft.net.outcome"));
			Assert.NotEqual(activity.SpanId, handlerContext.SpanId);
		}
		else
			Assert.Empty(stopped);
	}

	[Fact]
	public async Task RefusedConnectionReportsOriginalFailureAndCorrelatedErrorTrace()
	{
		using var metrics = new TcpMeasurements();
		using var reserved = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		reserved.Bind(new IPEndPoint(IPAddress.Loopback, 0));
		var stopped = new ConcurrentQueue<Activity>();
		using var listener = new ActivityListener
		{
			ShouldListenTo = source => source.Name == TcpDiagnostics.Name,
			Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
			ActivityStopped = stopped.Enqueue,
		};
		ActivitySource.AddActivityListener(listener);
		await using var client = TcpClient.Headed;
		var reports = new ConcurrentQueue<TcpDiagnosticEventArgs>();
		EventHandler<TcpDiagnosticEventArgs> diagnostic = (_, args) => reports.Enqueue(args);
		TcpDiagnostics.Error += diagnostic;
		var before = DateTimeOffset.UtcNow;

		try
		{
			var failure = await Assert.ThrowsAsync<SocketException>(() => client.ConnectAsync(reserved.LocalEndPoint));
			var activity = Assert.Single(stopped);
			var report = Assert.Single(reports);
			Assert.Same(client, report.Source);
			Assert.Same(failure, report.Exception);
			Assert.Equal("connect", report.Operation);
			Assert.Equal(activity.Context, report.ActivityContext);
			Assert.InRange(report.Timestamp, before, DateTimeOffset.UtcNow);
			Assert.Equal(ActivityStatusCode.Error, activity.Status);
			Assert.Equal("error", activity.GetTagItem("zongsoft.net.outcome"));
			Assert.Equal(typeof(SocketException).FullName, activity.GetTagItem("error.type"));
			Assert.Equal("error", Assert.Single(metrics.Find("zongsoft.net.tcp.connect.duration")).Tag("zongsoft.net.outcome"));
			Assert.Equal(1, Assert.Single(metrics.Find("zongsoft.net.tcp.errors")).Value);
			Assert.Null(client.Channel);
		}
		finally { TcpDiagnostics.Error -= diagnostic; }
	}

	[Fact]
	public async Task CanceledConnectIsMeasuredWithoutFailureEventOrErrorStatus()
	{
		using var metrics = new TcpMeasurements();
		using var cancellation = new CancellationTokenSource();
		var stopped = new ConcurrentQueue<Activity>();
		using var listener = new ActivityListener
		{
			ShouldListenTo = source => source.Name == TcpDiagnostics.Name,
			Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
			ActivityStarted = _ => cancellation.Cancel(),
			ActivityStopped = stopped.Enqueue,
		};
		ActivitySource.AddActivityListener(listener);
		await using var server = TestNetwork.Server();
		await server.StartAsync();
		await using var client = TcpClient.Headed;
		var reports = new ConcurrentQueue<TcpDiagnosticEventArgs>();
		EventHandler<TcpDiagnosticEventArgs> diagnostic = (_, args) => reports.Enqueue(args);
		TcpDiagnostics.Error += diagnostic;

		try
		{
			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.ConnectAsync(server.Address, cancellation.Token));
			var activity = Assert.Single(stopped);
			Assert.Equal(ActivityStatusCode.Unset, activity.Status);
			Assert.Equal("canceled", activity.GetTagItem("zongsoft.net.outcome"));
			Assert.Null(activity.GetTagItem("error.type"));
			Assert.Equal("canceled", Assert.Single(metrics.Find("zongsoft.net.tcp.connect.duration")).Tag("zongsoft.net.outcome"));
			Assert.Empty(reports);
			Assert.Empty(metrics.Find("zongsoft.net.tcp.errors"));
			Assert.Null(client.Channel);
		}
		finally { TcpDiagnostics.Error -= diagnostic; }
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task ThrowingTelemetryListenersDoNotChangeConnectionOrAmbientContext(bool throwOnStart)
	{
		using var meter = new MeterListener();
		//.NET 8 的 Dispose 需要完成回调才能移除仪表订阅。
		meter.MeasurementsCompleted = static (_, _) => { };
		var measurements = 0;
		meter.InstrumentPublished = (instrument, listener) =>
		{
			if(instrument.Meter.Name == TcpDiagnostics.Name)
				listener.EnableMeasurementEvents(instrument);
		};
		meter.SetMeasurementEventCallback<long>((_, _, _, _) => { Interlocked.Increment(ref measurements); throw new ApplicationException(); });
		meter.SetMeasurementEventCallback<double>((_, _, _, _) => { Interlocked.Increment(ref measurements); throw new ApplicationException(); });
		meter.Start();
		Activity started = null;
		var stopped = 0;
		using var listener = new ActivityListener
		{
			ShouldListenTo = source => source.Name == TcpDiagnostics.Name,
			Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
			ActivityStarted = activity =>
			{
				started = activity;
				if(throwOnStart)
					throw new ApplicationException();
			},
			ActivityStopped = _ =>
			{
				Interlocked.Increment(ref stopped);
				if(!throwOnStart)
					throw new ApplicationException();
			},
		};
		ActivitySource.AddActivityListener(listener);
		await using var server = TestNetwork.Server();
		await server.StartAsync();
		var clock = new ActivityRecordingClock();
		await using var client = TcpClient.CreateHeaded(new() { TimeProvider = clock });
		using var parent = new Activity("caller").SetIdFormat(ActivityIdFormat.W3C).Start();
		var channel = await client.ConnectAsync(server.Address);
		Assert.Same(parent, Activity.Current);
		Assert.NotNull(started);

		if(throwOnStart)
		{
			Assert.Contains(parent.Context, clock.Contexts);
			Assert.DoesNotContain(started.Context, clock.Contexts);
		}
		else
			Assert.Equal(1, stopped);

		await client.DisconnectAsync().AsTask().WaitAsync(TestNetwork.Timeout);
		await channel.WaitAsync().WaitAsync(TestNetwork.Timeout);
		Assert.Same(parent, Activity.Current);
		Assert.True(measurements > 0);
		Assert.Null(client.Channel);
	}

	[Fact]
	public async Task CompletedConnectionsRemainObservableToLateListenersWithoutDoubleCounting()
	{
		using var baseline = new TcpMeasurements();
		baseline.Observe();
		await using var server = TestNetwork.Server(new() { MaintenanceInterval = TimeSpan.FromHours(1) });
		server.Handler = new ReceiveHandler<ReadOnlySequence<byte>>((context, cancellation) => context.Channel.SendAsync(context.Package, cancellation));
		await server.StartAsync();
		await using var client = TcpClient.CreateHeaded(new() { MaintenanceInterval = TimeSpan.FromHours(1) });
		var received = TestNetwork.Signal<byte[]>();
		client.Handler = new ReceiveHandler<ReadOnlySequence<byte>>((context, _) =>
		{
			received.TrySetResult(context.Package.ToArray());
			return ValueTask.CompletedTask;
		});
		var channel = await client.ConnectAsync(server.Address);
		await TestNetwork.UntilAsync(() => server.Channels.Count == 1);
		var serverChannel = Assert.Single(server.Channels);

		using(var active = new TcpMeasurements())
		{
			active.Observe();
			foreach(var role in new[] { "client", "server" })
			{
				Assert.Equal(1, active.Read("connections.active", role) - baseline.Read("connections.active", role));
				Assert.Equal(1, active.Read("connections.created", role) - baseline.Read("connections.created", role));
			}
		}

		await channel.SendAsync(new ReadOnlySequence<byte>(new byte[] { 1, 2, 3 }));
		Assert.Equal(new byte[] { 1, 2, 3 }, await received.Task.WaitAsync(TestNetwork.Timeout));
		await client.DisconnectAsync();
		await serverChannel.WaitAsync().WaitAsync(TestNetwork.Timeout);
		await server.StopAsync();
		await channel.CloseAsync();
		await channel.WaitAsync();

		using var late = new TcpMeasurements();
		late.Observe();

		foreach(var role in new[] { "client", "server" })
		{
			Assert.Equal(baseline.Read("connections.active", role), late.Read("connections.active", role));
			Assert.Equal(1, late.Read("connections.created", role) - baseline.Read("connections.created", role));
			Assert.Equal(0, late.Read("send.pending", role));
			Assert.Equal(0, late.Read("send.pending_bytes", role));

			foreach(var direction in new[] { "transmit", "receive" })
			{
				Assert.Equal(7, late.Read("io.bytes", role, direction) - baseline.Read("io.bytes", role, direction));
				Assert.Equal(1, late.Read("frames", role, direction) - baseline.Read("frames", role, direction));
			}

			var closed = Assert.Single(baseline.Find("zongsoft.net.tcp.connections.closed"), item => Equals(item.Tag("zongsoft.net.role"), role));
			Assert.Equal(1, closed.Value);
			Assert.Equal(role == "client" ? "Local" : "Remote", closed.Tag("zongsoft.net.close_reason"));
			Assert.True(Assert.Single(baseline.Find("zongsoft.net.tcp.connection.duration"), item => Equals(item.Tag("zongsoft.net.role"), role)).Value >= 0);
		}

		Assert.IsType<ObservableUpDownCounter<long>>(Assert.Single(late.Instruments, instrument => instrument.Name == "zongsoft.net.tcp.connections.active"));
		Assert.IsType<ObservableCounter<long>>(Assert.Single(late.Instruments, instrument => instrument.Name == "zongsoft.net.tcp.connections.created"));
		var bytes = Assert.IsType<ObservableCounter<long>>(Assert.Single(late.Instruments, instrument => instrument.Name == "zongsoft.net.tcp.io.bytes"));
		Assert.Equal("By", bytes.Unit);
		Assert.IsType<ObservableCounter<long>>(Assert.Single(late.Instruments, instrument => instrument.Name == "zongsoft.net.tcp.frames"));
		Assert.IsType<ObservableUpDownCounter<long>>(Assert.Single(late.Instruments, instrument => instrument.Name == "zongsoft.net.tcp.send.pending"));
		Assert.IsType<ObservableUpDownCounter<long>>(Assert.Single(late.Instruments, instrument => instrument.Name == "zongsoft.net.tcp.send.pending_bytes"));
	}

	[Fact]
	public async Task ConnectionAdmissionRejectionIsMeasuredOnceAndCapacityRecovers()
	{
		using var metrics = new TcpMeasurements();
		await using var server = TestNetwork.Server(new() { MaximumConnections = 1 });
		await server.StartAsync();
		using var first = await TestNetwork.ConnectAsync(server.Address);
		await TestNetwork.UntilAsync(() => server.Channels.Count == 1);
		var channel = Assert.Single(server.Channels);
		using var rejected = await TestNetwork.ConnectAsync(server.Address);
		await TestNetwork.UntilAsync(() => metrics.Find("zongsoft.net.tcp.rejections").Any());
		var rejection = Assert.Single(metrics.Find("zongsoft.net.tcp.rejections"));
		Assert.Equal(1, rejection.Value);
		Assert.Equal("server", rejection.Tag("zongsoft.net.role"));
		Assert.Equal("accept", rejection.Tag("zongsoft.net.operation"));
		Assert.Equal(1, server.RejectedConnections);
		Assert.Same(channel, Assert.Single(server.Channels));
		await channel.CloseAsync();
		await channel.WaitAsync().WaitAsync(TestNetwork.Timeout);
		using var next = await TestNetwork.ConnectAsync(server.Address);
		await TestNetwork.UntilAsync(() => server.AcceptedConnections == 2);
		Assert.NotSame(channel, Assert.Single(server.Channels));
		Assert.Single(metrics.Find("zongsoft.net.tcp.rejections"));
	}

	[Fact]
	public async Task PendingCapacityAndRejectedSendsAreMeasuredAndReturnToZero()
	{
		using var metrics = new TcpMeasurements();
		await using var server = TestNetwork.Server();
		await server.StartAsync();
		using var release = new ManualResetEventSlim();
		var entered = TestNetwork.Signal();
		await using var client = new TcpClient<int>(new BlockingPacketizer(release, entered), new()
		{
			MaximumPendingSends = 2,
			MaximumPacketLength = 4,
			MaximumFrameLength = 4,
			MaximumPendingSendBytes = 8,
			MaintenanceInterval = TimeSpan.FromMilliseconds(20),
		});
		var channel = await client.ConnectAsync(server.Address);
		var sending = Task.Run(async () => await channel.SendAsync(1));
		await entered.Task.WaitAsync(TestNetwork.Timeout);
		using var cancellation = new CancellationTokenSource();
		var waiting = channel.SendAsync(2, cancellation.Token).AsTask();

		try
		{
			await TestNetwork.UntilAsync(() => { metrics.Observe(); return metrics.Read("send.pending", "client") == 2; });
			Assert.Equal(8, metrics.Read("send.pending_bytes", "client"));
			var rejected = await Assert.ThrowsAsync<TcpException>(() => channel.SendAsync(3).AsTask());
			Assert.Equal(TcpError.SendRejected, rejected.ErrorCode);
			var rejection = Assert.Single(metrics.Find("zongsoft.net.tcp.rejections"));
			Assert.Equal(1, rejection.Value);
			Assert.Equal("client", rejection.Tag("zongsoft.net.role"));
			Assert.Equal("send", rejection.Tag("zongsoft.net.operation"));
			cancellation.Cancel();
			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiting);
			await TestNetwork.UntilAsync(() => { metrics.Observe(); return metrics.Read("send.pending", "client") == 1; });
			Assert.Equal(4, metrics.Read("send.pending_bytes", "client"));
		}
		finally { release.Set(); }

		await sending.WaitAsync(TestNetwork.Timeout);
		await client.DisconnectAsync();
		metrics.Observe();
		Assert.Equal(0, metrics.Read("send.pending", "client"));
		Assert.Equal(0, metrics.Read("send.pending_bytes", "client"));
	}

	private sealed class BlockingPacketizer(ManualResetEventSlim release, TaskCompletionSource entered) : PacketizerBase<int>
	{
		public override long GetLength(in int package) => 4;
		public override void Pack(IBufferWriter<byte> writer, in int package)
		{
			entered.TrySetResult();
			if(!release.Wait(TestNetwork.Timeout))
				throw new TimeoutException();
			writer.Write(new byte[4]);
		}
		public override bool Unpack(ref ReadOnlySequence<byte> data, out int package) { package = 0; return false; }
	}

	private sealed class ActivityRecordingClock : TimeProvider
	{
		internal readonly ConcurrentQueue<ActivityContext> Contexts = new();
		public override long GetTimestamp()
		{
			this.Contexts.Enqueue(Activity.Current?.Context ?? default);
			return base.GetTimestamp();
		}
	}
}

internal sealed class TcpCountingLogger : ILogger
{
	private int _count;
	public int Count => Volatile.Read(ref _count);
	public string Name => nameof(TcpCountingLogger);
	public ValueTask FlushAsync(CancellationToken cancellation = default) => ValueTask.CompletedTask;
	public ValueTask LogAsync<TLog>(TLog log, CancellationToken cancellation = default) where TLog : ILog
	{
		Interlocked.Increment(ref _count);
		return ValueTask.CompletedTask;
	}
}

internal sealed class TcpMeasurements : IDisposable
{
	private readonly MeterListener _listener = new();
	private readonly ConcurrentQueue<Sample> _samples = new();
	private readonly ConcurrentDictionary<(string Name, string Role, string Direction), long> _observations = new();
	internal readonly ConcurrentQueue<Instrument> Instruments = new();

	internal TcpMeasurements()
	{
		//确保 .NET 8 的 Dispose 解除同步仪表订阅。
		_listener.MeasurementsCompleted = static (_, _) => { };
		_listener.InstrumentPublished = (instrument, listener) =>
		{
			if(instrument.Meter.Name == TcpDiagnostics.Name)
			{
				this.Instruments.Enqueue(instrument);
				listener.EnableMeasurementEvents(instrument);
			}
		};
		_listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
		{
			if(instrument.IsObservable)
			{
				string role = null, direction = null;
				foreach(var tag in tags)
				{
					if(tag.Key == "zongsoft.net.role")
						role = (string)tag.Value;
					if(tag.Key == "network.io.direction")
						direction = (string)tag.Value;
				}

				_observations[(instrument.Name, role, direction)] = value;
			}
			else
				_samples.Enqueue(new(instrument, value, tags.ToArray()));
		});
		_listener.SetMeasurementEventCallback<double>((instrument, value, tags, _) => _samples.Enqueue(new(instrument, value, tags.ToArray())));
		_listener.Start();
	}

	internal void Observe() => _listener.RecordObservableInstruments();
	internal long Read(string name, string role, string direction = null) => _observations.TryGetValue(("zongsoft.net.tcp." + name, role, direction), out var value) ? value : 0;
	internal IEnumerable<Sample> Find(string name) => _samples.Where(sample => sample.Instrument.Name == name);
	public void Dispose() => _listener.Dispose();

	internal sealed record Sample(Instrument Instrument, double Value, KeyValuePair<string, object>[] Tags)
	{
		internal object Tag(string key) => this.Tags.FirstOrDefault(tag => tag.Key == key).Value;
	}
}
