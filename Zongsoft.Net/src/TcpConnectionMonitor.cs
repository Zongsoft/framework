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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Zongsoft.Net;

internal interface ITcpMonitoredConnection
{
	#region 属性定义
	long Identifier { get; }
	TcpConnectionOptions Settings { get; }
	#endregion

	#region 方法定义
	TimeSpan CheckTimeout(long now);
	void Abort(TcpCloseReason reason, Exception exception);
	#endregion
}

//同一时钟与扫描周期共用一个维护任务；仅登记和最后一个连接移除协调任务代次。
internal static class TcpConnectionMonitor
{
	#region 静态字段
	private static readonly ConditionalWeakTable<TimeProvider, ConcurrentDictionary<TimeSpan, Group>> _groups = new();
	#endregion

	#region 内部方法
	internal static void Add(ITcpMonitoredConnection connection)
	{
		var settings = connection.Settings;
		var groups = _groups.GetValue(settings.TimeProvider, static _ => new());
		var group = groups.GetOrAdd(settings.MaintenanceInterval, interval => new(settings.TimeProvider, interval));
		group.Add(connection);
	}

	internal static void Remove(ITcpMonitoredConnection connection)
	{
		var settings = connection.Settings;
		if(_groups.TryGetValue(settings.TimeProvider, out var groups) && groups.TryGetValue(settings.MaintenanceInterval, out var group))
			group.Remove(connection.Identifier);
	}

	internal static TcpMonitorStatistics GetStatistics(TcpConnectionOptions settings) =>
		_groups.TryGetValue(settings.TimeProvider, out var groups) &&
		groups.TryGetValue(settings.MaintenanceInterval, out var group) ? group.Statistics : default;
	#endregion

	#region 嵌套类型
	private sealed class Group(TimeProvider clock, TimeSpan interval)
	{
		private readonly object _sync = new();
		private readonly ConcurrentDictionary<long, ITcpMonitoredConnection> _connections = new();
		private Runner _runner;
		private Task _maintenance = Task.CompletedTask;
		private int _diagnosticConnections;
		private long _scans;
		private long _overruns;
		private long _maximumDuration;
		private long _maximumTimeoutDelay;

		internal TcpMonitorStatistics Statistics => new(Interlocked.Read(ref _scans), Interlocked.Read(ref _overruns), TimeSpan.FromTicks(Interlocked.Read(ref _maximumDuration)), TimeSpan.FromTicks(Interlocked.Read(ref _maximumTimeoutDelay)));

		internal void Add(ITcpMonitoredConnection connection)
		{
			lock(_sync)
			{
				if(_connections.TryAdd(connection.Identifier, connection) && connection.Settings.EnableDiagnostics)
					Interlocked.Increment(ref _diagnosticConnections);

				if(_runner == null)
				{
					_runner = new(this, clock, interval);
					_maintenance = _runner.RunAsync(_maintenance);
				}
			}
		}

		internal void Remove(long identifier)
		{
			Runner runner = null;

			lock(_sync)
			{
				if(_connections.TryRemove(identifier, out var connection) && connection.Settings.EnableDiagnostics)
					Interlocked.Decrement(ref _diagnosticConnections);

				if(_connections.IsEmpty)
				{
					runner = _runner;
					_runner = null;
				}
			}

			runner?.Stop();
		}

		//每一代扫描拥有自己的取消源；分组只协调登记及代次串联。
		private sealed class Runner(Group group, TimeProvider clock, TimeSpan interval)
		{
			private readonly CancellationTokenSource _cancellation = new();

			public void Stop()
			{
				try { _cancellation.Cancel(); }
				catch(ObjectDisposedException) { }
			}

			public async Task RunAsync(Task previous)
			{
				//先发布本代登记，再等待上一代退出，避免并行扫描。
				await Task.Yield();
				System.Diagnostics.Activity.Current = null;

				try
				{
					//上一代的故障已经在其边界处理，不能阻止新代次进入自己的清理范围。
					try { await previous.ConfigureAwait(false); }
					catch { }

					_cancellation.Token.ThrowIfCancellationRequested();
					using var timer = new PeriodicTimer(interval, clock);

					while(await timer.WaitForNextTickAsync(_cancellation.Token).ConfigureAwait(false))
						await ScanAsync().ConfigureAwait(false);
				}
				catch(OperationCanceledException) when(_cancellation.IsCancellationRequested) { }
				catch(Exception exception)
				{
					ICollection<ITcpMonitoredConnection> connections;

					lock(group._sync)
					{
						//失败只影响这一快照；之后加入的连接启动下一代并等待本代退出。
						connections = ReferenceEquals(group._runner, this) ? group._connections.Values : [];
						if(ReferenceEquals(group._runner, this))
							group._runner = null;
					}

					foreach(var connection in connections)
						connection.Abort(TcpCloseReason.TransportError, exception);

					TcpDiagnostics.Report(null, "monitor", exception);
				}
				finally
				{
					lock(group._sync)
					{
						if(ReferenceEquals(group._runner, this))
							group._runner = null;
					}

					_cancellation.Dispose();
				}

				async Task ScanAsync()
				{
					var count = 0;
					var now = clock.GetTimestamp();
					var started = now;
					var diagnostics = Volatile.Read(ref group._diagnosticConnections) > 0;
					var maximumDelay = TimeSpan.Zero;

					try
					{
						foreach(var entry in group._connections)
						{
							_cancellation.Token.ThrowIfCancellationRequested();
							var delay = entry.Value.CheckTimeout(now);

							if(diagnostics && delay > maximumDelay)
								maximumDelay = delay;

							if(++count % 256 == 0)
							{
								await Task.Yield();
								now = clock.GetTimestamp();
							}
						}
					}
					finally
					{
						if(diagnostics)
						{
							var elapsed = clock.GetElapsedTime(started);
							Interlocked.Exchange(ref group._maximumDuration, Math.Max(group._maximumDuration, elapsed.Ticks));
							Interlocked.Exchange(ref group._maximumTimeoutDelay, Math.Max(group._maximumTimeoutDelay, maximumDelay.Ticks));

							if(elapsed > interval)
								Interlocked.Increment(ref group._overruns);

							Interlocked.Increment(ref group._scans);
							TcpDiagnostics.Scan(elapsed, interval);
						}
					}
				}
			}
		}
	}
	#endregion
}
