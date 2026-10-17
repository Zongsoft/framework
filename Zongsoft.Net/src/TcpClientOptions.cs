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
