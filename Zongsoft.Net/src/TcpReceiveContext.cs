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
