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

/// <summary>TCP 客户端的连接设置。</summary>
public class TcpClientOptions : TcpConnectionOptions
{
	/// <summary>获取或设置 DNS 解析及连接尝试的共同期限；须大于零且不超过 4,294,967,294 毫秒。</summary>
	public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(10);

	internal override void Validate()
	{
		base.Validate();
		ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(this.ConnectTimeout, TimeSpan.Zero);
		ArgumentOutOfRangeException.ThrowIfGreaterThan(this.ConnectTimeout, MaximumTimerDuration, nameof(this.ConnectTimeout));
	}
}
