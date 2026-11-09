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

using NetMQ;
using NetMQ.Sockets;

using Zongsoft.Communication;

namespace Zongsoft.Messaging.ZeroMQ;

public sealed partial class ZeroQueueServer
{
	private sealed partial class ServerAgent
	{
		private sealed class ZeroBroadcastServer
		{
			#region 成员字段
			private readonly NetworkAddress _incoming;
			private readonly NetworkAddress _outgoing;
			private readonly string _epoch;
			private readonly NetMQPoller _poller;
			private XPublisherSocket _publisher;
			private XSubscriberSocket _subscriber;
			#endregion

			#region 构造函数
			public ZeroBroadcastServer(NetworkAddress incoming, NetworkAddress outgoing, string epoch, NetMQPoller poller)
			{
				_incoming = incoming;
				_outgoing = outgoing;
				_epoch = epoch;
				_poller = poller;
			}
			#endregion

			#region 公共属性
			public int Incoming { get; private set; }
			public int Outgoing { get; private set; }
			#endregion

			#region 公共方法
			public void Start()
			{
				_publisher = new XPublisherSocket();
				_subscriber = new XSubscriberSocket();

				try
				{
					_publisher.ReceiveReady += this.OnPublisherReady;
					_subscriber.ReceiveReady += this.OnSubscriberReady;
					_publisher.SetWelcomeMessage(Protocol.GetWelcome(_epoch));
					this.Outgoing = Bind(_publisher, _outgoing);
					this.Incoming = Bind(_subscriber, _incoming);
					_poller.Add(_publisher);
					_poller.Add(_subscriber);
				}
				catch
				{
					this.Stop();
					throw;
				}
			}

			public void Stop()
			{
				Release(ref _publisher, socket => socket.ReceiveReady -= this.OnPublisherReady);
				Release(ref _subscriber, socket => socket.ReceiveReady -= this.OnSubscriberReady);
				this.Incoming = 0;
				this.Outgoing = 0;

				void Release<TSocket>(ref TSocket socket, Action<TSocket> releasing) where TSocket : NetMQSocket
				{
					var current = socket;
					socket = null;
					if(current == null || current.IsDisposed)
						return;

					releasing(current);
					_poller.RemoveAndDispose(current);
				}
			}
			#endregion

			#region 事件处理
			private void OnPublisherReady(object sender, NetMQSocketEventArgs args) => this.Forward(args.Socket, _subscriber);
			private void OnSubscriberReady(object sender, NetMQSocketEventArgs args) => this.Forward(args.Socket, _publisher);

			private void Forward(NetMQSocket source, NetMQSocket destination)
			{
				try
				{
					var message = new NetMQMessage();
					while(source.TryReceiveMultipartMessage(ref message))
					{
						destination.SendMultipartMessage(message);
						message = new NetMQMessage();
					}
				}
				catch(Exception exception) { Diagnostics.Logging.GetLogging(this).Error(exception); }
			}
			#endregion
		}
	}
}
