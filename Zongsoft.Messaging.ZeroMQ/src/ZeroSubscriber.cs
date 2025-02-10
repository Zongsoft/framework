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
 * Copyright (C) 2010-2024 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Messaging.ZeroMQ library.
 *
 * The Zongsoft.Messaging.ZeroMQ is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Messaging.ZeroMQ is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Messaging.ZeroMQ library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Threading;
using System.Threading.Tasks;

using NetMQ;
using NetMQ.Sockets;

using Zongsoft.Services;
using Zongsoft.Components;

namespace Zongsoft.Messaging.ZeroMQ;

public sealed class ZeroSubscriber(ZeroQueue queue, string topic, IHandler<Message> handler, MessageSubscribeOptions options = null) : MessageConsumerBase<ZeroQueue>(queue, topic, handler, options)
{
	#region 成员字段
	private volatile SubscriberSocket _channel;
	#endregion

	#region 内部属性
	public SubscriberSocket Channel => _channel;
	#endregion

	#region 订阅方法
	internal SubscriberSocket Subscribe(string address)
	{
		if(_channel != null && !_channel.IsDisposed)
			return _channel;

		lock(this)
		{
			if(_channel == null)
			{
				var channel = _channel = new SubscriberSocket();
				channel.Options.ReceiveHighWatermark = 1000;
				channel.Options.HeartbeatInterval = TimeSpan.FromSeconds(30);
				channel.ReceiveReady += this.OnReceiveReady;
				channel.Connect(address);
				channel.Subscribe(this.Topic);
			}

			return _channel;
		}
	}
	#endregion

	#region 取消订阅
	protected override ValueTask OnCloseAsync(CancellationToken cancellation)
	{
		var orginal = _channel;

		//将当前通道对应设置为空
		var channel = Interlocked.Exchange(ref _channel, null);

		if(channel != null && !channel.IsDisposed)
		{
			//必须先通过队列的注销方法将当前订阅的通道从轮询器中移除
			this.Queue.Unregister(orginal);

			channel.ReceiveReady -= this.OnReceiveReady;
			channel.Unsubscribe(this.Topic);
			channel.Dispose();
		}

		return ValueTask.CompletedTask;
	}
	#endregion

	#region 事件处理
	private void OnReceiveReady(object sender, NetMQSocketEventArgs args)
	{
		var round = Math.Max(_channel.Options.ReceiveHighWatermark, 100);

		for(int i = 0; i < round; i++)
		{
			//尝试接收首帧消息
			if(!args.Socket.TryReceiveFrameString(out var header, out var more))
				break;

			//如果是空帧则忽略
			if(string.IsNullOrEmpty(header))
				continue;

			//解包收到的首帧消息
			var identifier = Packetizer.Unpack(header, out var topic, out var options);

			//如果接收到的消息是本队列发出的则忽略它并跳过随后的数据帧
			if(string.Equals(identifier, this.Queue.Identifier))
			{
				args.Socket.TrySkipFrame();
				continue;
			}

			//接收数据帧的内容
			if(!args.Socket.TryReceiveFrameBytes(out var data, out more))
				break;

			//如果是匿名消息并且数据帧内容为空则当作心跳消息处理（即忽略它）
			if(string.IsNullOrEmpty(identifier) && data == null || data.Length == 0)
				continue;

			//如果接收到的首帧消息包含压缩选项，则必须对收到的消息内容进行解压
			if(Packetizer.Options.TryGetValue(options, Packetizer.Options.Compressor, out var compressor))
				data = Zongsoft.IO.Compression.Compressor.Decompress(compressor, data);

			//调用处理器进行消息处理
			FireAndForget(Task.Run(() => this.Handler.HandleAsync(new Message(topic, data))));

			if(!more)
				break;
		}

		static async void FireAndForget(Task task)
		{
			try { await task; } catch { }
		}
	}
	#endregion
}
