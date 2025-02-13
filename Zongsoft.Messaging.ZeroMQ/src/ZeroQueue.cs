﻿/*
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
using System.Collections.Generic;

using NetMQ;
using NetMQ.Sockets;

using Zongsoft.Common;
using Zongsoft.Components;
using Zongsoft.Configuration;

namespace Zongsoft.Messaging.ZeroMQ;

public sealed partial class ZeroQueue : MessageQueueBase<ZeroSubscriber, Configuration.ZeroConnectionSettings>
{
	#region 私有变量
	private ushort _publisherPort;
	private ushort _subscriberPort;
	private readonly object _locker;
	private HashSet<string> _exclusion;
	private HashSet<string> _inclusion;
	#endregion

	#region 成员字段
	private NetMQTimer _timer;
	private NetMQPoller _poller;
	private NetMQQueue<Packet> _queue;
	private PublisherSocket _publisher;
	private EventChannel _channel;
	#endregion

	#region 构造函数
	public ZeroQueue(string name, Configuration.ZeroConnectionSettings settings) : base(name, settings)
	{
		if(settings == null)
			throw new ArgumentNullException(nameof(settings));

		if(string.IsNullOrEmpty(settings.Server))
			throw new ArgumentException($"The required server address is missing in the connection settings.");

		//如果未指定远程队列服务器端口号则设置默认端口号
		if(settings.Port == 0)
			settings.Port = ZeroQueueServer.PORT;

		//生成当前消息队列的实例标识
		this.Instance = GenerateIdentifier(settings);

		//初始化消息实例过滤器
		this.SetFilter(settings.Filter, this.Instance);

		_locker = new();
		_queue = new NetMQQueue<Packet>();
		_queue.ReceiveReady += this.OnQueueReady;
		_poller = new NetMQPoller() { _queue };

		//如果没有指定心跳间隔则默认为心跳时间为10秒，如果显式指定了心跳间隔小于等于零则表示不启用心跳
		_timer = new NetMQTimer(settings.Heartbeat > TimeSpan.Zero ? settings.Heartbeat : TimeSpan.FromSeconds(10));
		_timer.Elapsed += this.OnElapsed;
		_poller.Add(_timer);
	}
	#endregion

	#region 公共属性
	/// <summary>获取当前队列的实例标识。</summary>
	public string Instance { get; }
	#endregion

	#region 公共方法
	public IEventChannel Channel => this.IsDisposed ? null : _channel ??= new EventChannel(this);
	#endregion

	#region 订阅方法
	protected override ValueTask<ZeroSubscriber> CreateSubscriberAsync(string topic, string tags, IHandler<Message> handler, MessageSubscribeOptions options, CancellationToken cancellation) => ValueTask.FromResult(new ZeroSubscriber(this, topic, handler, options));
	protected override ValueTask<bool> OnSubscribeAsync(ZeroSubscriber subscriber, CancellationToken cancellation = default)
	{
		//确保初始化完成
		this.Initialize();

		//执行网络订阅方法
		var channel = subscriber.Subscribe(ZeroUtility.GetTcpAddress(this.Settings.Server, _publisherPort));

		//将订阅成功的网络通道加入到轮询器中
		if(channel != null)
			_poller.Add(channel);

		return ValueTask.FromResult(true);
	}

	protected override void OnUnsubscribed(ZeroSubscriber subscriber) => this.Unregister(subscriber.Channel);
	#endregion

	#region 发布方法
	protected override ValueTask<string> OnProduceAsync(string topic, string tags, ReadOnlyMemory<byte> data, MessageEnqueueOptions options, CancellationToken cancellation)
	{
		//确保初始化完成
		this.Initialize();

		if(string.IsNullOrEmpty(topic))
		{
			foreach(var subscriber in this.Subscribers)
				_queue.Enqueue(new Packet(subscriber.Topic, data, options));
		}
		else
		{
			_queue.Enqueue(new Packet(topic, data, options));
		}

		return ValueTask.FromResult<string>(null);
	}

	private void OnElapsed(object sender, NetMQTimerEventArgs e)
	{
		if(this.IsDisposed)
			return;

		_queue.Enqueue(default);
	}

	private void OnQueueReady(object sender, NetMQQueueEventArgs<Packet> e)
	{
		if(e.Queue.TryDequeue(out var packet, TimeSpan.Zero))
		{
			//如果主题为空则直接发送心跳包
			if(string.IsNullOrEmpty(packet.Topic))
			{
				//方案一：直接发送空包
				//_publisher.SendMoreFrameEmpty().SendFrameEmpty();

				//方案二：依次向所有订阅者发送匿名空包
				foreach(var subscriber in this.Subscribers)
					_publisher.SendMoreFrame(Packetizer.Pack(subscriber.Topic)).SendFrameEmpty();

				return;
			}

			var head = Packetizer.Pack(this.Instance, packet.Topic, packet.Data, packet.Options, out var compressor);
			var data = string.IsNullOrEmpty(compressor) ? packet.Data.ToArray() : IO.Compression.Compressor.Compress(compressor, packet.Data.ToArray());
			_publisher.SendMoreFrame(head).SendFrame(data);
		}
	}
	#endregion

	#region 内部方法
	internal bool Validate(string identifier)
	{
		if(_exclusion != null && _exclusion.Contains(identifier))
			return false;

		return _inclusion == null || _inclusion.Count == 0 || _inclusion.Contains(identifier);
	}

	internal void Unregister(SubscriberSocket channel)
	{
		if(channel != null && !channel.IsDisposed)
		{
			//将指定的通道从轮询器中删除
			_poller.Remove(channel);

			//注意：由于上述操作内部为异步，因此可能需要稍等一会确保其删除操作已完成
			if(_poller.ContainsAsync(channel).Result)
				Thread.Sleep(10);
		}
	}
	#endregion

	#region 重写方法
	protected override string GetTopic(string topic)
	{
		topic = base.GetTopic(topic);

		if(topic == "*")
			topic = string.Empty;

		return string.IsNullOrEmpty(this.Settings.Group) ? topic : $"{this.Settings.Group}:{topic}";
	}
	#endregion

	#region 私有方法
	private void Initialize()
	{
		if(_publisher != null)
			return;

		lock(_locker)
		{
			if(_publisher != null)
				return;

			//获取队列交换器的发布和订阅端口号
			(_publisherPort, _subscriberPort) = GetPorts(this.Settings);

			//如果队列交换器的端口信息获取失败则抛出异常
			if(_publisherPort == 0 || _subscriberPort == 0)
				throw new InvalidOperationException($"Failed to acquire queue exchange information from the '{this.Settings.Server}:{this.Settings.Port}' server.");

			//创建一个发布者套接字
			var publisher = new PublisherSocket();

			publisher.Options.SendHighWatermark = 1000;
			publisher.Options.HeartbeatInterval = TimeSpan.FromSeconds(30);
			publisher.Connect(ZeroUtility.GetTcpAddress(this.Settings.Server, _subscriberPort));

			//将已经连接就绪的发布者保存
			_publisher = publisher;

			//启动网络轮询器
			if(!_poller.IsRunning)
				_poller.RunAsync();
		}

		static (ushort publisherPort, ushort subscriberPort) GetPorts(Configuration.ZeroConnectionSettings settings)
		{
			using var requester = new RequestSocket(ZeroUtility.GetTcpAddress(settings.Server, settings.Port));

			//发送请求获取交换器端口号
			requester.SendFrameEmpty();

			//获取连接超时
			var timeout = settings.Timeout > TimeSpan.Zero ? settings.Timeout : TimeSpan.FromSeconds(10);

			//定义请求的响应内容
			string response = null;

			//接收返回的请求响应信息
			//注意：TryReceiveFrame(...) 方法并不会等待指定的超时，因此需要通过 SpinWait 轮询响应内容
			SpinWait.SpinUntil(() => requester.TryReceiveFrameString(timeout, out response), timeout);

			//如果请求的响应内容为空则返回失败
			if(string.IsNullOrEmpty(response))
				return default;

			ushort publisherPort = 0, subscriberPort = 0;
			var entries = response.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

			foreach(var entry in entries)
			{
				int index = entry.IndexOf('=');

				if(index > 0 && index < entry.Length - 1)
				{
					var span = entry.AsSpan();

					switch(span[..index])
					{
						case "publisher":
						case "Publisher":
							if(ushort.TryParse(span[(index + 1)..], out var port1))
								publisherPort = port1;
							break;
						case "subscriber":
						case "Subscriber":
							if(ushort.TryParse(span[(index + 1)..], out var port2))
								subscriberPort = port2;
							break;
					}
				}
			}

			return (publisherPort, subscriberPort);
		}
	}

	private void SetFilter(string filter, string instance)
	{
		if(string.IsNullOrWhiteSpace(filter))
		{
			_inclusion = null;
			_exclusion = new HashSet<string>([instance]);
			return;
		}

		var parts = filter.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

		for(int i = 0; i < parts.Length; i++)
		{
			switch(parts[i])
			{
				case "*":
					_exclusion?.Clear();
					_inclusion?.Clear();
					break;
				case ".":
				case "~":
					_exclusion?.Remove(instance);

					if(_inclusion == null)
						_inclusion = new HashSet<string>([instance]);
					else
						_inclusion.Add(instance);

					break;
				default:
					if(parts[i][0] == '!')
					{
						if(parts[i].Length == 1)
							_exclusion?.Clear();
						else
						{
							var part = parts[i][1..];

							if(part == "." || part == "~")
								part = instance;

							if(_exclusion == null)
								_exclusion = new HashSet<string>([part]);
							else
								_exclusion.Add(part);
						}
					}
					else
					{
						_exclusion?.Remove(parts[i]);

						if(_inclusion == null)
							_inclusion = new HashSet<string>([parts[i]]);
						else
							_inclusion.Add(parts[i]);
					}
					break;
			}

		}
	}

	private static string GenerateIdentifier(Configuration.ZeroConnectionSettings settings)
	{
		if(string.IsNullOrEmpty(settings.Instance) || settings.Instance == "*")
		{
			return string.IsNullOrEmpty(settings?.Client) ?
				Randomizer.GenerateString(10) :
				$"{settings.Client}-{Math.Abs(Randomizer.GenerateInt32()):X}";
		}

		return settings.Instance;
	}
	#endregion

	#region 处置方法
	protected override void Dispose(bool disposing)
	{
		if(disposing)
		{
			var poller = _poller;
			if(poller != null && !poller.IsDisposed)
				poller.Dispose();

			var timer = _timer;
			if(timer != null)
			{
				timer.Enable = false;
				timer.Elapsed -= this.OnElapsed;
			}

			var queue = _queue;
			if(queue != null && !queue.IsDisposed)
			{
				queue.ReceiveReady -= this.OnQueueReady;
				queue.Dispose();
			}

			var publisher = _publisher;
			if(publisher != null && !publisher.IsDisposed)
				publisher.Dispose();

			NetMQConfig.Cleanup(false);
		}

		_timer = null;
		_queue = null;
		_poller = null;
		_channel = null;
		_publisher = null;
	}
	#endregion

	#region 嵌套结构
	private readonly struct Packet(string topic, ReadOnlyMemory<byte> data, MessageEnqueueOptions options)
	{
		public readonly string Topic = topic;
		public readonly ReadOnlyMemory<byte> Data = data;
		public readonly MessageEnqueueOptions Options = options;
	}
	#endregion
}
