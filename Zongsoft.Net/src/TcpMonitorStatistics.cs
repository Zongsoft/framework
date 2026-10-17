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

namespace Zongsoft.Net;

/// <summary>共享连接监测的累计统计快照；并发读取不保证跨指标原子性。</summary>
/// <remarks>同一 <see cref="TimeProvider" /> 和 <see cref="TcpConnectionOptions.MaintenanceInterval" /> 的连接共享统计，只在组内存在启用诊断的连接时累计，所有连接退出后仍可读取已有统计。</remarks>
public readonly struct TcpMonitorStatistics
{
	internal TcpMonitorStatistics(long scans, long overruns, TimeSpan duration, TimeSpan delay)
	{
		this.ScanCount = scans;
		this.OverrunCount = overruns;
		this.MaximumScanDuration = duration;
		this.MaximumTimeoutDelay = delay;
	}

	/// <summary>获取已结束的扫描次数，包含被取消的扫描。</summary>
	public long ScanCount { get; }
	/// <summary>获取扫描耗时超过维护周期的次数，非零表示需要检查扫描容量或调度负载。</summary>
	public long OverrunCount { get; }
	/// <summary>获取扫描的最大耗时，包含分批让出线程后的调度等待。</summary>
	public TimeSpan MaximumScanDuration { get; }
	/// <summary>获取超时检测相对于连接记录期限的最大延迟，不包含传输关闭及业务处理退场耗时。</summary>
	public TimeSpan MaximumTimeoutDelay { get; }
}
