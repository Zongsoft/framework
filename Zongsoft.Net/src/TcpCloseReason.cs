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
