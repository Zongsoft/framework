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
