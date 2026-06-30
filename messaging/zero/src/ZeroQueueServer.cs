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
using Zongsoft.Configuration;

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
		try
		{
			this.Initialize();

			if(!_poller.IsRunning)
			{
				(var incoming, var outgoing) = GetPorts(this.Name, args);

				_responser.Bind($"tcp://*:{_port}");
				_publisherPort = Bind(_publisher, outgoing);
				_subscriberPort = Bind(_subscriber, incoming);

				_poller.RunAsync();
			}

			_proxy.Start();
			return Task.CompletedTask;
		}
		catch
		{
			//任意端口绑定或 proxy 启动失败都必须释放已创建的 socket，否则后续重试会继续占用端口
			this.Release();
			throw;
		}

		static (int incoming, int outgoing) GetPorts(string name, string[] args)
		{
			if(args != null && args.Length > 0)
			{
				var incoming = 0;
				var outgoing = 0;

				for(int i = 0; i < args.Length; i++)
				{
					var parts = args[i].Split(['=', ':'], StringSplitOptions.TrimEntries);

					if(parts.Length == 2)
					{
						if(parts[0].StartsWith("--"))
							parts[0] = parts[0][2..];

						if(parts[0].StartsWith("incoming", StringComparison.OrdinalIgnoreCase))
							incoming = int.Parse(parts[1]);
						else if(parts[0].StartsWith("outgoing", StringComparison.OrdinalIgnoreCase))
							outgoing = int.Parse(parts[1]);
					}
				}

				if(incoming > 0 && outgoing > 0)
					return (incoming, outgoing);
			}

			var servers = ApplicationContext.Current?.Configuration.GetOption<Configuration.ServerOptionsCollection>("/Messaging/ZeroMQ/Servers");
			if(servers == null)
				return default;

			if(name != null && servers.TryGetValue(name, out var server))
				return (server.Port.Incoming, server.Port.Outgoing);

			return (servers.Port.Incoming, servers.Port.Outgoing);
		}

		static int Bind(NetMQSocket socket, int port)
		{
			if(port > 0)
			{
				socket.Bind($"tcp://*:{port}");
				return port;
			}

			return socket.BindRandomPort("tcp://*");
		}
	}

	protected override Task OnStopAsync(string[] args, CancellationToken cancellation)
	{
		this.Release();
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

	#region 私有方法
	private void Initialize()
	{
		if(_poller != null && !_poller.IsDisposed)
			return;

		_responser = new ResponseSocket();
		_responser.ReceiveReady += this.Responser_ReceiveReady;

		_publisher = new XPublisherSocket();
		_subscriber = new XSubscriberSocket();
		_poller = new NetMQPoller() { _responser, _subscriber, _publisher };
		_proxy = new Proxy(_subscriber, _publisher, null, null, _poller);
	}

	private void Release()
	{
		var proxy = _proxy;
		if(proxy != null)
		{
			try { proxy.Stop(); }
			catch(InvalidOperationException) { }
		}

		var poller = _poller;
		if(poller != null && !poller.IsDisposed)
		{
			if(poller.IsRunning)
				poller.Stop();

			poller.Dispose();
		}

		var responser = _responser;
		if(responser != null)
		{
			responser.ReceiveReady -= this.Responser_ReceiveReady;

			if(!responser.IsDisposed)
				responser.Dispose();
		}

		var publisher = _publisher;
		if(publisher != null && !publisher.IsDisposed)
			publisher.Dispose();

		var subscriber = _subscriber;
		if(subscriber != null && !subscriber.IsDisposed)
			subscriber.Dispose();

		_proxy = null;
		_poller = null;
		_responser = null;
		_publisher = null;
		_subscriber = null;
		_publisherPort = 0;
		_subscriberPort = 0;
	}
	#endregion

	#region 处置方法
	protected override void Dispose(bool disposing)
	{
		if(disposing)
		{
			base.Dispose(disposing);
			this.Release();
		}

		_proxy = null;
		_poller = null;
		_responser = null;
		_publisher = null;
		_subscriber = null;
	}
	#endregion
}
