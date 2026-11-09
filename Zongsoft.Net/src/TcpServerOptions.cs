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
		ArgumentOutOfRangeException.ThrowIfGreaterThan(this.SendTimeout, MaximumTimerDuration, nameof(this.SendTimeout));
	}
}
