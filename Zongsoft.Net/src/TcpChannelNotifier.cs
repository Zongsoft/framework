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
