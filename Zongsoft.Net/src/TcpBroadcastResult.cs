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

/// <summary>广播的本地发送结果，不表示远端接收或业务确认。</summary>
/// <param name="Succeeded">成功完成本地提交的连接数。</param>
/// <param name="Failed">发送失败的连接数，不包含 <see cref="TcpError.SendRejected" /> 对应的容量拒绝。</param>
/// <param name="Rejected">以 <see cref="TcpError.SendRejected" /> 拒绝发送的连接数。</param>
public readonly record struct TcpBroadcastResult(int Succeeded, int Failed, int Rejected);
