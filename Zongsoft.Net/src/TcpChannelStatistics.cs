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

/// <summary>连接的可选诊断快照；并发读取不提供跨指标原子性。</summary>
public readonly struct TcpChannelStatistics
{
	internal TcpChannelStatistics(long reads, long exhausted, long maximum, long waits)
	{
		this.ReadCount = reads;
		this.BudgetExhaustions = exhausted;
		this.MaximumFramesPerRead = maximum;
		this.SendWaitCount = waits;
	}

	/// <summary>获取已推进接收游标的读取次数。</summary>
	public long ReadCount { get; }
	/// <summary>获取同步处理达到帧数或字节预算的次数。</summary>
	public long BudgetExhaustions { get; }
	/// <summary>获取单次读取中已处理的最多完整帧数。</summary>
	public long MaximumFramesPerRead { get; }
	/// <summary>获取未能立即取得单写者权限、进入异步等待的发送次数。</summary>
	public long SendWaitCount { get; }
}
