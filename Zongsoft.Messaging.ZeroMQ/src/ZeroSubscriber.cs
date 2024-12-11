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
		if(_channel != null)
			return _channel;

		lock(this)
		{
			if(_channel == null)
			{
				var channel = _channel = new SubscriberSocket();
				channel.Options.ReceiveHighWatermark = 1000;
				channel.ReceiveReady += this.OnReceiveReady;
				channel.Connect(address);
				channel.Subscribe(this.Topic);
			}

			return _channel;
		}
	}
	#endregion

	#region 取消订阅
	protected override ValueTask OnUnsubscribeAsync(CancellationToken cancellation)
	{
		var channel = Interlocked.Exchange(ref _channel, null);

		if(channel != null)
		{
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
		var topic = args.Socket.ReceiveFrameString();
		var data = args.Socket.ReceiveFrameBytes();

		FireAndForget(this.Handler.HandleAsync(new Message(this.Topic, data)).AsTask());

		//for(int i = 0; i < 1000; i++)
		//{
		//	if(!args.Socket.TryReceiveFrameBytes(out var data, out var more))
		//		break;

		//	FireAndForget(this.Handler.HandleAsync(new Message(this.Topic, data)).AsTask());

		//	if(!more)
		//		break;
		//}

		static async void FireAndForget(Task task)
		{
			try
			{
				await task;
			}
			catch { }
		}
	}
	#endregion
}
