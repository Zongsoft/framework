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

/// <summary>表示 TCP 通讯库识别的错误，不表示连接是否已经关闭。</summary>
public enum TcpError
{
	/// <summary>发送或广播超过容量，本次报文尚未被接纳。</summary>
	SendRejected = 1,
	/// <summary>报文长度、编码写入量或解码消费不符合分帧契约。</summary>
	InvalidFrame = 2,
	/// <summary>对端已结束发送，但仍有未完成的报文。</summary>
	IncompleteFrame = 3,
	/// <summary>连接已经关闭或服务已经停止，无法继续当前操作。</summary>
	ConnectionClosed = 4,
}
