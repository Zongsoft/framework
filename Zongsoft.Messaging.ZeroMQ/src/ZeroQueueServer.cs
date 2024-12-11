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

using Zongsoft.Common;
using Zongsoft.Services;

namespace Zongsoft.Messaging.ZeroMQ;

public sealed class ZeroQueueServer : WorkerBase
{
	#region 常量定义
	/// <summary>消息队列服务器的默认侦听端口号。</summary>
	public const ushort PORT = 7969;
	#endregion

	#region 私有变量
	private int _publisherPort;
	private int _subscriberPort;
	#endregion

	#region 成员字段
	private ushort _port;
	private Proxy _proxy;
	private NetMQPoller _poller;
	private ResponseSocket _responser;
	private XPublisherSocket _publisher;
	private XSubscriberSocket _subscriber;
	#endregion

	#region 构造函数
	public ZeroQueueServer(string name = null) : base(name)
	{
		//设置默认的端口号
		_port = PORT;

		_responser = new ResponseSocket();
		_responser.ReceiveReady += this.Responser_ReceiveReady;
		_publisher = new XPublisherSocket();
		_subscriber = new XSubscriberSocket();
		_poller = new NetMQPoller() { _responser, _subscriber, _publisher };
		_proxy = new Proxy(_subscriber, _publisher, null, null, _poller);
	}
	#endregion

	#region 公共属性
	/// <summary>获取或设置服务器侦听的端口号，默认值为：<see cref="PORT"/>。</summary>
	public ushort Port
	{
		get => _port;
		set
		{
			if(this.State != WorkerState.Stopped)
				throw new InvalidOperationException();

			_port = value > 0 ? value : PORT;
		}
	}
	#endregion

	#region 重写方法
	protected override Task OnStartAsync(string[] args, CancellationToken cancellation)
	{
		if(!_poller.IsRunning)
		{
			_responser.Bind($"tcp://*:{_port}");
			_publisherPort = _publisher.BindRandomPort("tcp://*");
			_subscriberPort = _subscriber.BindRandomPort("tcp://*");

			_poller.RunAsync();
		}

		_proxy.Start();
		return Task.CompletedTask;
	}

	protected override Task OnStopAsync(string[] args, CancellationToken cancellation)
	{
		_proxy.Stop();
		return Task.CompletedTask;
	}
	#endregion

	#region 事件处理
	private void Responser_ReceiveReady(object sender, NetMQSocketEventArgs args)
	{
		var command = args.Socket.ReceiveFrameString();
		if(string.IsNullOrEmpty(command))
			command = "port";

		switch(command)
		{
			case "port":
			case "ports":
				args.Socket.SendFrame($"Publisher={_publisherPort};Subscriber={_subscriberPort}");
				break;
			default:
				args.Socket.SendFrameEmpty();
				break;
		}
	}
	#endregion

	#region 处置方法
	protected override void Dispose(bool disposing)
	{
		if(disposing)
		{
			this.Stop();

			_poller.Dispose();
			_publisher.Dispose();
			_subscriber.Dispose();

			NetMQConfig.Cleanup(false);
		}

		_proxy = null;
		_publisher = null;
		_subscriber = null;
	}
	#endregion
}
