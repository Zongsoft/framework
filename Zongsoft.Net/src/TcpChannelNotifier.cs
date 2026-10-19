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

//客户端与服务端管理器共用通知规则；订阅者故障不改变通道结果，也不阻止其余订阅者。
internal sealed class TcpChannelNotifier<T>(object owner)
{
	#region 事件声明
	/// <summary>表示通道开始关闭。</summary>
	internal event EventHandler<TcpChannelEventArgs<T>> Closing;
	/// <summary>表示通道全部清理完成。</summary>
	internal event EventHandler<TcpChannelEventArgs<T>> Closed;
	/// <summary>表示导致通道结束的首个故障。</summary>
	internal event EventHandler<TcpChannelEventArgs<T>> Error;
	#endregion

	#region 事件通知
	/// <summary>通知通道开始关闭。</summary>
	/// <param name="args">关闭开始时的状态快照。</param>
	internal void OnClosing(TcpChannelEventArgs<T> args) => this.Raise(this.Closing, args);

	/// <summary>通知通道完整退场。</summary>
	/// <param name="args">通道移除后的最终状态快照。</param>
	internal void OnClosed(TcpChannelEventArgs<T> args) => this.Raise(this.Closed, args);

	/// <summary>通知导致通道结束的首个故障。</summary>
	/// <param name="args">包含非空 <see cref="TcpChannelEventArgs{T}.Error" /> 的快照。</param>
	internal void OnError(TcpChannelEventArgs<T> args)
	{
		this.Raise(this.Error, args);
	}

	/// <summary>依订阅顺序通知，隔离每个订阅者的异常。</summary>
	/// <param name="handlers">通知开始时的订阅者快照。</param>
	/// <param name="args">当前通知的状态快照。</param>
	private void Raise(EventHandler<TcpChannelEventArgs<T>> handlers, TcpChannelEventArgs<T> args)
	{
		if(handlers == null)
			return;

		foreach(EventHandler<TcpChannelEventArgs<T>> handler in handlers.GetInvocationList())
		{
			try { handler(owner, args); }
			catch(Exception exception) { TcpDiagnostics.Report(owner, "notification", exception); }
		}
	}
	#endregion
}
