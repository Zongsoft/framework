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

/// <summary>TCP 服务端接入、广播及连接设置。</summary>
public class TcpServerOptions : TcpConnectionOptions
{
	/// <summary>获取或设置监听队列长度，实际容量受操作系统限制。</summary>
	public int Backlog { get; set; } = 1024;
	/// <summary>获取或设置接入及活动连接的总上限。</summary>
	public int MaximumConnections { get; set; } = 10000;
	/// <summary>获取或设置一次广播同时发送的最大连接数。</summary>
	public int BroadcastConcurrency { get; set; } = 32;
	/// <summary>获取或设置同时执行的广播上限，超额立即拒绝。</summary>
	public int MaximumConcurrentBroadcasts { get; set; } = 1;

	internal override void Validate()
	{
		base.Validate();

		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(this.Backlog);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(this.MaximumConnections);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(this.BroadcastConcurrency);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(this.MaximumConcurrentBroadcasts);
	}
}
