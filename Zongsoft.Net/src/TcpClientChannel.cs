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
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Communication;

namespace Zongsoft.Net;

/// <summary>客户端拥有的 TCP 连接通道。</summary>
/// <typeparam name="T">报文类型。</typeparam>
public sealed class TcpClientChannel<T> : TcpChannelBase<T>
{
	internal TcpClientChannel(TcpClient<T> client, TcpTransport transport, PacketizerBase<T> packetizer, TcpClientOptions options) : base(transport, packetizer, options) => this.Client = client;

	/// <summary>获取所属客户端。</summary>
	public TcpClient<T> Client { get; }

	/// <summary>向连接所有者派发上下文，保持借用报文的异步处理窗口。</summary>
	/// <param name="context">当前通道和处理期间有效的借用报文。</param>
	/// <param name="cancellation">连接关闭时通知处理器的取消令牌。</param>
	/// <returns>本次报文处理操作。</returns>
	protected override ValueTask OnReceiveAsync(TcpReceiveContext<T> context, CancellationToken cancellation) => this.Client.HandleAsync(context, cancellation);
	/// <summary>资源清理后由连接所有者归还登记与额度。</summary>
	private protected override void OnUnregister() => this.Client.Unregister(this);
	/// <summary>最终通知及等待完成后归还连接所有权。</summary>
	private protected override void OnCompleted() => this.Client.Complete(this);
	/// <summary>向连接所有者报告关闭开始。</summary>
	/// <param name="args">当前通道状态快照。</param>
	private protected override void OnClosing(TcpChannelEventArgs<T> args) => this.Client.OnClosing(args);
	/// <summary>向连接所有者报告完整退场。</summary>
	/// <param name="args">当前通道的最终状态快照。</param>
	private protected override void OnClosed(TcpChannelEventArgs<T> args) => this.Client.OnClosed(args);
	/// <summary>向连接所有者报告首个故障。</summary>
	/// <param name="args">包含首个异常的状态快照。</param>
	private protected override void OnError(TcpChannelEventArgs<T> args) => this.Client.OnError(args);
}
