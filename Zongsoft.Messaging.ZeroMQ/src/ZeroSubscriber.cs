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

public class ZeroSubscriber : MessageConsumerBase<ZeroQueue>
{
	private volatile SubscriberSocket _consumer;

	#region 构造函数
	public ZeroSubscriber(ZeroQueue queue, string topic, IHandler<Message> handler, MessageSubscribeOptions options = null) : base(queue, topic, handler, options)
	{
	}
	#endregion

	#region 内部属性
	public SubscriberSocket Consumer => _consumer;
	#endregion

	internal SubscriberSocket Subscribe(string address)
	{
		if(_consumer != null)
			return _consumer;

		lock(this)
		{
			if(_consumer == null)
			{
				var consumer = _consumer = new SubscriberSocket(address);
				consumer.Options.ReceiveHighWatermark = 1000;
				consumer.ReceiveReady += this.OnReceiveReady;
				consumer.Connect(address);
				consumer.Subscribe(this.Topic);
			}

			return _consumer;
		}
	}

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

	#region 取消订阅
	protected override ValueTask OnUnsubscribeAsync(CancellationToken cancellation)
	{
		var consumer = Interlocked.Exchange(ref _consumer, null);

		if(consumer != null)
			consumer.ReceiveReady -= this.OnReceiveReady;

		return ValueTask.CompletedTask;
	}
	#endregion
}
