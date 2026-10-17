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

/// <summary>当前连接和本次借用报文组成的接收实参。</summary>
/// <typeparam name="T">报文类型。</typeparam>
/// <remarks>复制上下文不会延长报文寿命；报文仅在处理器返回的异步操作完成前有效。</remarks>
public readonly struct TcpReceiveContext<T>
{
	internal TcpReceiveContext(TcpChannelBase<T> channel, T package)
	{
		this.Channel = channel;
		this.Package = package;
	}

	/// <summary>获取收到当前报文的通道，可以向该通道回复。</summary>
	public TcpChannelBase<T> Channel { get; }
	/// <summary>获取本次借用的报文。</summary>
	public T Package { get; }
}
