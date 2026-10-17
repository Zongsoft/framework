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
using System.Net;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Components;
using Zongsoft.Communication;

namespace Zongsoft.Net;

/// <summary>创建独立的二进制 TCP 客户端。</summary>
public static class TcpClient
{
	#region 公共方法
	/// <summary>获取一个新的带四字节长度前缀的客户端。</summary>
	public static TcpClient<ReadOnlySequence<byte>> Headed => CreateHeaded();
	/// <summary>获取一个新的原始字节流客户端。</summary>
	public static TcpClient<ReadOnlySequence<byte>> Headless => CreateHeadless();

	/// <summary>创建带长度前缀的独立客户端。</summary>
	/// <param name="options">连接设置，构造时复制。</param>
	/// <returns>默认地址为 loopback:7969 的客户端。</returns>
	public static TcpClient<ReadOnlySequence<byte>> CreateHeaded(TcpClientOptions options = null)
	{
		options = (TcpClientOptions)(options ?? new()).Snapshot();
		ArgumentOutOfRangeException.ThrowIfLessThan(options.MaximumFrameLength, (long)options.MaximumPacketLength + 4);
		return new(new HeadedPacketizer(options.MaximumPacketLength), options) { Address = new IPEndPoint(IPAddress.Loopback, 7969) };
	}

	/// <summary>创建不定义业务消息边界的独立客户端。</summary>
	/// <param name="options">连接设置，构造时复制。</param>
	/// <returns>默认地址为 loopback:7969 的客户端。</returns>
	public static TcpClient<ReadOnlySequence<byte>> CreateHeadless(TcpClientOptions options = null)
	{
		options = (TcpClientOptions)(options ?? new()).Snapshot();
		return new(new HeadlessPacketizer(options.MaximumPacketLength), options) { Address = new IPEndPoint(IPAddress.Loopback, 7969) };
	}
	#endregion
}

/// <summary>具有合并建连、惰性连接和异步释放能力的 TCP 客户端。</summary>
/// <typeparam name="T">报文类型。</typeparam>
public partial class TcpClient<T> : IHandleable<TcpReceiveContext<T>>, ISender<T>, IAsyncDisposable
{
	#region 事件声明
	/// <summary>表示通道开始关闭；不可取消，事件参数是该时刻的状态快照。</summary>
	public event EventHandler<TcpChannelEventArgs<T>> Closing { add => _notifier.Closing += value; remove => _notifier.Closing -= value; }
	/// <summary>表示处理器、发送及缓冲清理完成，通道已经移除登记。</summary>
	public event EventHandler<TcpChannelEventArgs<T>> Closed { add => _notifier.Closed += value; remove => _notifier.Closed -= value; }
	/// <summary>表示通道结束过程中的首个故障；每条通道最多通知一次，不包含发送容量拒绝。</summary>
	public event EventHandler<TcpChannelEventArgs<T>> Error { add => _notifier.Error += value; remove => _notifier.Error -= value; }
	#endregion

	#region 成员字段
	private readonly TcpClientOptions _options;
	private readonly Connector _connector;
	private readonly TcpChannelNotifier<T> _notifier;
	private IHandler<TcpReceiveContext<T>> _handler;
	#endregion

	#region 构造函数
	/// <summary>创建使用指定分帧契约的客户端。</summary>
	/// <param name="packetizer">分帧器；调用期间必须支持跨连接并发使用。</param>
	/// <param name="options">可选设置，构造时复制。</param>
	public TcpClient(PacketizerBase<T> packetizer, TcpClientOptions options = null)
	{
		this.Packetizer = packetizer ?? throw new ArgumentNullException(nameof(packetizer));
		_options = (TcpClientOptions)(options ?? new()).Snapshot();
		_connector = new(this, _options);
		_notifier = new(this);
	}
	#endregion

	#region 公共属性
	/// <summary>获取或设置目标地址；更改地址会使在途建连失效并关闭原连接。</summary>
	public EndPoint Address { get => _connector.Address; set => _connector.Address = value; }
	/// <summary>获取分帧器。</summary>
	public PacketizerBase<T> Packetizer { get; }
	/// <summary>获取当前连接，尚未连接或已经退场时为空。</summary>
	public TcpClientChannel<T> Channel => _connector.Channel;
	/// <summary>获取全部连接累计完成 Socket 发送的字节数。</summary>
	public long TotalBytesSent => _connector.TotalBytesSent;
	/// <summary>获取全部连接累计从 Socket 接收的字节数。</summary>
	public long TotalBytesReceived => _connector.TotalBytesReceived;
	/// <summary>获取同一时钟与维护周期的共享连接监测统计快照；读取不触发扫描，未启用 <see cref="TcpConnectionOptions.EnableDiagnostics" /> 时各项为零。</summary>
	public TcpMonitorStatistics Statistics => _options.EnableDiagnostics ? TcpConnectionMonitor.GetStatistics(_options) : default;

	/// <summary>获取或设置接收处理器，不取得共享处理器的释放所有权。</summary>
	public IHandler<TcpReceiveContext<T>> Handler
	{
		get => Volatile.Read(ref _handler);
		set => Volatile.Write(ref _handler, value);
	}

	/// <summary>获取或设置与 <see cref="TcpReceiveContext{T}" /> 匹配的处理器。</summary>
	IHandler IHandleable.Handler
	{
		get => this.Handler;
		set => this.Handler = value == null ? null : value as IHandler<TcpReceiveContext<T>> ?? throw new ArgumentException(string.Format(Properties.Resources.Network_HandlerMismatch_Message, value));
	}
	#endregion

	#region 连接方法
	/// <summary>显式建立连接；并发调用共享尝试，首次发起者取消该尝试，其余调用者只取消各自等待。</summary>
	/// <param name="address">可选目标地址；指定时更新 <see cref="Address" />。</param>
	/// <param name="cancellation">取消令牌。</param>
	/// <returns>已登记且开始接收的当前通道。</returns>
	public Task<TcpClientChannel<T>> ConnectAsync(EndPoint address = null, CancellationToken cancellation = default) => _connector.ConnectAsync(address, cancellation);

	/// <summary>停止接收新报文，限时等待当前处理和发送排空。再次发送会重新连接，不自动重放消息。</summary>
	/// <param name="cancellation">取消本调用方的等待。</param>
	/// <returns>当前连接全部退场的结果，超时强制关闭传输并抛出异常。</returns>
	public ValueTask DisconnectAsync(CancellationToken cancellation = default) => _connector.DisconnectAsync(cancellation);
	#endregion

	#region 收发处理
	/// <summary>向当前连接发送；没有连接时共用显式建连流程，不重放失败消息。</summary>
	/// <param name="package">发送完成前保持稳定的报文。</param>
	/// <param name="cancellation">取消令牌。</param>
	/// <returns>本地发送提交结果。</returns>
	public async ValueTask SendAsync(T package, CancellationToken cancellation = default)
	{
		cancellation.ThrowIfCancellationRequested();

		var channel = this.Channel;
		if(channel == null || !channel.IsActive)
			channel = await this.ConnectAsync(cancellation: cancellation).ConfigureAwait(false);

		await channel.SendAsync(package, cancellation).ConfigureAwait(false);
	}

	/// <summary>检查取消并将上下文交给共享处理器。</summary>
	/// <param name="context">当前通道和处理期间有效的借用报文。</param>
	/// <param name="cancellation">取消令牌；仅取消等待的入口不影响已经开始的资源清理。</param>
	/// <returns>恰好消费一次的处理操作。</returns>
	internal ValueTask HandleAsync(TcpReceiveContext<T> context, CancellationToken cancellation)
	{
		cancellation.ThrowIfCancellationRequested();
		return this.Handler?.HandleAsync(context, cancellation) ?? ValueTask.CompletedTask;
	}

	/// <summary>注销已经清理的连接登记并累计其字节统计。</summary>
	/// <param name="channel">已经退场的连接。</param>
	internal void Unregister(TcpClientChannel<T> channel) => _connector.Unregister(channel);
	#endregion

	#region 事件通知
	/// <summary>转发开始关闭通知。</summary>
	/// <param name="args">当前通道的状态快照。</param>
	internal void OnClosing(TcpChannelEventArgs<T> args) => _notifier.OnClosing(args);
	/// <summary>转发完整退场通知。</summary>
	/// <param name="args">移除登记后的最终状态快照。</param>
	internal void OnClosed(TcpChannelEventArgs<T> args) => _notifier.OnClosed(args);
	/// <summary>转发并记录通道的首个结束故障。</summary>
	/// <param name="args">包含首个异常的状态快照。</param>
	internal void OnError(TcpChannelEventArgs<T> args) => _notifier.OnError(args);
	#endregion

	#region 释放资源
	/// <summary>取消建连并等待所有已拥有的连接清理；并发调用共享结果。</summary>
	/// <returns>释放完成结果。</returns>
	public ValueTask DisposeAsync() => _connector.DisposeAsync();
	#endregion
}
