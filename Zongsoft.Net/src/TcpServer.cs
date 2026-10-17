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

/// <summary>创建独立的二进制 TCP 服务端。</summary>
public static class TcpServer
{
	#region 公共方法
	/// <summary>获取一个新的带四字节长度前缀的服务端。</summary>
	public static TcpServer<ReadOnlySequence<byte>> Headed => CreateHeaded();
	/// <summary>获取一个新的原始字节流服务端。</summary>
	public static TcpServer<ReadOnlySequence<byte>> Headless => CreateHeadless();

	/// <summary>创建带长度前缀的独立服务端。</summary>
	/// <param name="options">连接和服务设置，构造时复制。</param>
	/// <returns>默认监听 loopback:7969 的服务端。</returns>
	public static TcpServer<ReadOnlySequence<byte>> CreateHeaded(TcpServerOptions options = null)
	{
		options = (TcpServerOptions)(options ?? new()).Snapshot();
		ArgumentOutOfRangeException.ThrowIfLessThan(options.MaximumFrameLength, (long)options.MaximumPacketLength + 4);
		return new(new HeadedPacketizer(options.MaximumPacketLength), options);
	}

	/// <summary>创建不定义业务消息边界的独立服务端。</summary>
	/// <param name="options">连接和服务设置，构造时复制。</param>
	/// <returns>默认监听 loopback:7969 的服务端。</returns>
	public static TcpServer<ReadOnlySequence<byte>> CreateHeadless(TcpServerOptions options = null)
	{
		options = (TcpServerOptions)(options ?? new()).Snapshot();
		return new(new HeadlessPacketizer(options.MaximumPacketLength), options);
	}
	#endregion
}

/// <summary>使用官方 Socket 受理连接并按当前通道派发报文的 TCP 服务端。</summary>
/// <typeparam name="T">报文类型。</typeparam>
public partial class TcpServer<T> : ListenerBase<TcpReceiveContext<T>>, ISender<T>
{
	#region 成员字段
	private readonly TcpServerOptions _options;
	private readonly Broadcaster _broadcaster;
	private IPEndPoint _address = new(IPAddress.Loopback, 7969);
	private long _accepted;
	private long _rejected;
	private long _completedBytesSent;
	private long _completedBytesReceived;
	private long _completedFramesSent;
	private long _completedFramesReceived;
	#endregion

	#region 构造函数
	/// <summary>创建 TCP 服务端。</summary>
	/// <param name="packetizer">可跨连接并发使用的分帧器。</param>
	/// <param name="options">可选连接和服务设置，构造时复制。</param>
	public TcpServer(PacketizerBase<T> packetizer, TcpServerOptions options = null) : this(nameof(TcpServer<T>), packetizer, options) { }
	/// <summary>创建具名 TCP 服务端。</summary>
	/// <param name="name">工作器名称。</param>
	/// <param name="packetizer">可跨连接并发使用的分帧器。</param>
	/// <param name="options">可选连接和服务设置，构造时复制。</param>
	public TcpServer(string name, PacketizerBase<T> packetizer, TcpServerOptions options = null) : base(name)
	{
		_options = (TcpServerOptions)(options ?? new()).Snapshot();
		this.Packetizer = packetizer ?? throw new ArgumentNullException(nameof(packetizer));
		this.Channels = new();
		_broadcaster = new(this);
	}
	#endregion

	#region 公共属性
	/// <summary>获取或设置监听端点。端口零由系统分配；成功启动后返回实际端点。</summary>
	public IPEndPoint Address
	{
		get
		{
			var address = Volatile.Read(ref _address);
			return new(address.Address, address.Port);
		}
		set
		{
			ArgumentNullException.ThrowIfNull(value);
			if(this.State != WorkerState.Stopped)
				throw new InvalidOperationException(Properties.Resources.Network_AddressImmutable_Message);

			Volatile.Write(ref _address, new(value.Address, value.Port));
		}
	}

	/// <summary>获取分帧器。</summary>
	public PacketizerBase<T> Packetizer { get; }
	/// <summary>获取连接实时视图。连接完成资源清理后移除。</summary>
	public TcpServerChannelManager<T> Channels { get; }
	/// <summary>获取累计接纳连接数。</summary>
	public long AcceptedConnections => Interlocked.Read(ref _accepted);
	/// <summary>获取累计容量拒绝连接数。</summary>
	public long RejectedConnections => Interlocked.Read(ref _rejected);
	/// <summary>获取包含已结束连接的累计 Socket 发送字节数。</summary>
	public long TotalBytesSent => this.Sum(static channel => channel.TotalBytesSent, Interlocked.Read(ref _completedBytesSent));
	/// <summary>获取包含已结束连接的累计 Socket 接收字节数。</summary>
	public long TotalBytesReceived => this.Sum(static channel => channel.TotalBytesReceived, Interlocked.Read(ref _completedBytesReceived));
	/// <summary>获取包含已结束连接的累计发送帧数。</summary>
	public long TotalFramesSent => this.Sum(static channel => channel.TotalFramesSent, Interlocked.Read(ref _completedFramesSent));
	/// <summary>获取包含已结束连接的累计解码帧数。</summary>
	public long TotalFramesReceived => this.Sum(static channel => channel.TotalFramesReceived, Interlocked.Read(ref _completedFramesReceived));
	/// <summary>获取同一时钟与维护周期的共享连接监测统计快照；读取不触发扫描，未启用 <see cref="TcpConnectionOptions.EnableDiagnostics" /> 时各项为零。</summary>
	public TcpMonitorStatistics Statistics => _options.EnableDiagnostics ? TcpConnectionMonitor.GetStatistics(_options) : default;
	#endregion

	#region 内部方法
	/// <summary>向监听器处理扩展点派发当前连接上下文。</summary>
	/// <param name="context">当前通道和处理期间有效的借用报文。</param>
	/// <param name="cancellation">取消令牌；仅取消等待的入口不影响已经开始的资源清理。</param>
	/// <returns>处理操作。</returns>
	internal ValueTask HandleAsync(TcpReceiveContext<T> context, CancellationToken cancellation) => this.OnHandleAsync(context, cancellation);

	/// <summary>注销已经清理的连接登记，累计统计并归还接入额度。</summary>
	/// <param name="channel">已经退场的连接。</param>
	internal void Unregister(TcpServerChannel<T> channel)
	{
		TcpConnectionMonitor.Remove(channel);

		if(this.Channels.Remove(channel))
		{
			Interlocked.Add(ref _completedBytesSent, channel.TotalBytesSent);
			Interlocked.Add(ref _completedBytesReceived, channel.TotalBytesReceived);
			Interlocked.Add(ref _completedFramesSent, channel.TotalFramesSent);
			Interlocked.Add(ref _completedFramesReceived, channel.TotalFramesReceived);
			Interlocked.Decrement(ref _admitted);
		}
	}

	/// <summary>将活动连接指标加入已完成连接的累计值。</summary>
	/// <param name="selector">取得当前通道所需统计值的委托。</param>
	/// <param name="total">已结束连接的累计值。</param>
	/// <returns>并发变化期间的近似累计快照。</returns>
	private long Sum(Func<TcpServerChannel<T>, long> selector, long total)
	{
		foreach(var channel in this.Channels)
			total += selector(channel);

		return total;
	}
	#endregion
}
