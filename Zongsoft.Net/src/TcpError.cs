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
