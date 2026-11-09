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

/// <summary>提供通道生命周期通知的不可变快照。</summary>
/// <typeparam name="T">通道报文类型。</typeparam>
/// <remarks>开始关闭时的快照不包含后续清理故障；完整关闭通知包含最终记录的首个异常。</remarks>
public class TcpChannelEventArgs<T> : EventArgs
{
	/// <summary>创建通道生命周期通知。</summary>
	/// <param name="channel">发生通知的通道。</param>
	/// <param name="closeReason">连接结束的首个原因。</param>
	/// <param name="error">首个故障；正常关闭时为 <see langword="null" />。</param>
	public TcpChannelEventArgs(TcpChannelBase<T> channel, TcpCloseReason closeReason, Exception error = null)
	{
		this.Channel = channel ?? throw new ArgumentNullException(nameof(channel));
		this.CloseReason = closeReason;
		this.Error = error;
	}

	/// <summary>获取发生通知的通道。</summary>
	public TcpChannelBase<T> Channel { get; }
	/// <summary>获取连接结束的首个原因。</summary>
	public TcpCloseReason CloseReason { get; }
	/// <summary>获取首个故障；正常关闭时为 <see langword="null" />。</summary>
	public Exception Error { get; }
}
