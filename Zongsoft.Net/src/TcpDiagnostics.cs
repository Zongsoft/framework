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
using System.Threading;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Collections.Generic;

namespace Zongsoft.Net;

/// <summary>TCP 的可选故障通知、指标和跟踪源，不配置日志或遥测导出器。</summary>
public static class TcpDiagnostics
{
	#region 公共声明
	/// <summary>获取 Meter 与 ActivitySource 的订阅名称。</summary>
	public const string Name = "Zongsoft.Net";

	/// <summary>表示建连、受理、初始化、共享监测或事件订阅者故障。</summary>
	/// <remarks>同步且在管理锁外调用；订阅者必须及时返回，不得等待包含自身的停止操作。异常被隔离，重入不再次通知；不替代通道 Error 和操作返回结果。订阅者负责解除订阅。</remarks>
	public static event EventHandler<TcpDiagnosticEventArgs> Error;
	#endregion

	#region 成员字段
	private static readonly ActivitySource _activities = new(Name, typeof(TcpDiagnostics).Assembly.GetName().Version?.ToString());
	private static readonly Meter _meter = new(Name, typeof(TcpDiagnostics).Assembly.GetName().Version?.ToString());
	private static readonly Totals[] _totals = [new("client"), new("server")];

	private static readonly Histogram<double> _connectDuration = _meter.CreateHistogram<double>("zongsoft.net.tcp.connect.duration", "s");
	private static readonly Histogram<double> _connectionDuration = _meter.CreateHistogram<double>("zongsoft.net.tcp.connection.duration", "s");
	private static readonly Counter<long> _closed = _meter.CreateCounter<long>("zongsoft.net.tcp.connections.closed", "{connection}");
	private static readonly Counter<long> _rejections = _meter.CreateCounter<long>("zongsoft.net.tcp.rejections", "{operation}");
	private static readonly Counter<long> _errors = _meter.CreateCounter<long>("zongsoft.net.tcp.errors", "{error}");
	private static readonly Histogram<double> _scanDuration = _meter.CreateHistogram<double>("zongsoft.net.tcp.monitor.scan.duration", "s");
	private static readonly Histogram<double> _timeoutDelay = _meter.CreateHistogram<double>("zongsoft.net.tcp.monitor.timeout.delay", "s");
	private static readonly Counter<long> _overruns = _meter.CreateCounter<long>("zongsoft.net.tcp.monitor.overruns", "{scan}");
	[ThreadStatic] private static bool _reporting;
	#endregion

	#region 静态构造
	static TcpDiagnostics()
	{
		_meter.CreateObservableUpDownCounter("zongsoft.net.tcp.connections.active", () => Observe(static totals => Interlocked.Read(ref totals.Active)), "{connection}");
		_meter.CreateObservableCounter("zongsoft.net.tcp.connections.created", () => Observe(static totals => Interlocked.Read(ref totals.Created)), "{connection}");
		_meter.CreateObservableCounter("zongsoft.net.tcp.io.bytes", ObserveBytes, "By");
		_meter.CreateObservableCounter("zongsoft.net.tcp.frames", ObserveFrames, "{frame}");
		_meter.CreateObservableUpDownCounter("zongsoft.net.tcp.send.pending", () => Observe(static totals => Interlocked.Read(ref totals.Pending)), "{operation}");
		_meter.CreateObservableUpDownCounter("zongsoft.net.tcp.send.pending_bytes", () => Observe(static totals => Interlocked.Read(ref totals.PendingBytes)), "By");
	}
	#endregion

	#region 诊断通知
	internal static void Report(object source, string operation, Exception exception)
	{
		if(_reporting)
			return;

		_reporting = true;

		try
		{
			Failure(operation);

			var handlers = Error;
			if(handlers == null)
				return;

			var args = new TcpDiagnosticEventArgs(source, operation, exception);
			foreach(EventHandler<TcpDiagnosticEventArgs> handler in handlers.GetInvocationList())
			{
				try { handler(source, args); }
				catch { } //故障出口本身不递归报告。
			}
		}
		finally { _reporting = false; }
	}
	#endregion

	#region 操作测量
	internal static Activity StartConnect()
	{
		var parent = Activity.Current;

		try { return _activities.StartActivity("tcp.connect", ActivityKind.Client); }
		catch
		{
			try
			{
				if(Activity.Current is { } failed && failed.Source == _activities && !ReferenceEquals(failed, parent))
					failed.Dispose();
			}
			catch { }
			finally { Activity.Current = parent; }

			return null;
		}
	}

	internal static void CompleteConnect(Activity activity, long started, Exception error)
	{
		var outcome = error == null ? "success" : error is OperationCanceledException ? "canceled" : "error";

		try
		{
			_connectDuration.Record(Stopwatch.GetElapsedTime(started).TotalSeconds, new KeyValuePair<string, object>("zongsoft.net.outcome", outcome));
		}
		catch { }

		try
		{
			activity?.SetTag("network.transport", "tcp");
			activity?.SetTag("zongsoft.net.outcome", outcome);

			if(error != null && error is not OperationCanceledException)
			{
				activity?.SetTag("error.type", error.GetType().FullName);
				activity?.SetStatus(ActivityStatusCode.Error);
			}
		}
		finally
		{
			try { activity?.Dispose(); }
			catch { }
			finally
			{
				if(activity != null && ReferenceEquals(Activity.Current, activity))
					Activity.Current = activity.Parent;
			}
		}
	}

	internal static void Reject(bool server, string operation)
	{
		try { _rejections.Add(1, new("zongsoft.net.role", server ? "server" : "client"), new("zongsoft.net.operation", operation)); }
		catch { }
	}

	internal static void Failure(string operation)
	{
		try { _errors.Add(1, new KeyValuePair<string, object>("zongsoft.net.operation", operation)); }
		catch { } //诊断监听器不能改写通信结果。
	}

	internal static void Scan(TimeSpan duration, TimeSpan interval)
	{
		try { _scanDuration.Record(duration.TotalSeconds); }
		catch { }

		if(duration > interval)
		{
			try { _overruns.Add(1); }
			catch { }
		}
	}

	internal static void Timeout(TimeSpan delay)
	{
		try { _timeoutDelay.Record(Math.Max(0, delay.TotalSeconds)); }
		catch { }
	}
	#endregion

	#region 采样回调
	private static IEnumerable<Measurement<long>> Observe(Func<Totals, long> selector)
	{
		foreach(var totals in _totals)
			yield return new(selector(totals), new KeyValuePair<string, object>("zongsoft.net.role", totals.Role));
	}

	private static IEnumerable<Measurement<long>> ObserveBytes()
	{
		foreach(var totals in _totals)
		{
			yield return new(Interlocked.Read(ref totals.Sent), new("zongsoft.net.role", totals.Role), new("network.io.direction", "transmit"));
			yield return new(Interlocked.Read(ref totals.Received), new("zongsoft.net.role", totals.Role), new("network.io.direction", "receive"));
		}
	}

	private static IEnumerable<Measurement<long>> ObserveFrames()
	{
		foreach(var totals in _totals)
		{
			yield return new(Interlocked.Read(ref totals.FramesSent), new("zongsoft.net.role", totals.Role), new("network.io.direction", "transmit"));
			yield return new(Interlocked.Read(ref totals.FramesReceived), new("zongsoft.net.role", totals.Role), new("network.io.direction", "receive"));
		}
	}
	#endregion

	#region 嵌套类型
	private sealed class Totals(string role)
	{
		public readonly string Role = role;
		public long Active, Created, Sent, Received, FramesSent, FramesReceived, Pending, PendingBytes;
	}

	//每条已启动连接只保存采样差量；采集回调不持有连接、不遍历活动连接或执行 I/O。
	internal sealed class Connection
	{
		private readonly object _sync = new();
		private readonly Totals _totals;
		private readonly long _started = Stopwatch.GetTimestamp();

		private long _sent, _received, _framesSent, _framesReceived, _pending, _pendingBytes;
		private bool _completed;

		public Connection(bool server)
		{
			_totals = TcpDiagnostics._totals[server ? 1 : 0];
			Interlocked.Increment(ref _totals.Created);
			Interlocked.Increment(ref _totals.Active);
		}

		public void Sample(long sent, long received, long framesSent, long framesReceived, int pending, long pendingBytes)
		{
			lock(_sync)
			{
				if(_completed)
					return;

				Accumulate(ref _sent, Math.Max(_sent, sent), ref _totals.Sent);
				Accumulate(ref _received, Math.Max(_received, received), ref _totals.Received);
				Accumulate(ref _framesSent, Math.Max(_framesSent, framesSent), ref _totals.FramesSent);
				Accumulate(ref _framesReceived, Math.Max(_framesReceived, framesReceived), ref _totals.FramesReceived);
				Accumulate(ref _pending, pending, ref _totals.Pending);
				Accumulate(ref _pendingBytes, pendingBytes, ref _totals.PendingBytes);
			}

			static void Accumulate(ref long previous, long current, ref long total)
			{
				Interlocked.Add(ref total, current - previous);
				previous = current;
			}
		}

		public void Complete(TcpCloseReason reason)
		{
			lock(_sync)
			{
				if(_completed)
					return;

				_completed = true;
				Interlocked.Decrement(ref _totals.Active);
				Interlocked.Add(ref _totals.Pending, -_pending);
				Interlocked.Add(ref _totals.PendingBytes, -_pendingBytes);
			}

			try { _connectionDuration.Record(Stopwatch.GetElapsedTime(_started).TotalSeconds, new KeyValuePair<string, object>("zongsoft.net.role", _totals.Role)); }
			catch { }

			try { _closed.Add(1, new("zongsoft.net.role", _totals.Role), new("zongsoft.net.close_reason", reason.ToString())); }
			catch { }
		}
	}
	#endregion
}
