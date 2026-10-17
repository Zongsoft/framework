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
