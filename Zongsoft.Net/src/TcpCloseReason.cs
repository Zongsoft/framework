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

/// <summary>连接结束的首个原因。</summary>
public enum TcpCloseReason
{
	/// <summary>连接尚未关闭。</summary>
	None,
	/// <summary>本地请求关闭。</summary>
	Local,
	/// <summary>对端正常结束发送。</summary>
	Remote,
	/// <summary>报文或分帧契约不合法。</summary>
	ProtocolError,
	/// <summary>业务处理器发生异常。</summary>
	HandlerError,
	/// <summary>传输发生异常。</summary>
	TransportError,
	/// <summary>接收空闲超时。</summary>
	IdleTimeout,
	/// <summary>从首个残留字节开始，在 <see cref="TcpConnectionOptions.FrameTimeout" /> 内未能接收完整帧。</summary>
	FrameTimeout,
	/// <summary>发送进度停滞超时。</summary>
	SendTimeout,
	/// <summary>停止期限届满。</summary>
	ShutdownTimeout,
}
