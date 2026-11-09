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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
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
