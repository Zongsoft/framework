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
using Zongsoft.Components;
using Zongsoft.Configuration;

namespace Zongsoft.Messaging.ZeroMQ;

public sealed partial class ZeroQueue : MessageQueueBase<ZeroSubscriber>
{
	#region 私有变量
	private ushort _publisherPort;
	private ushort _subscriberPort;
	private readonly object _locker;
	#endregion

	#region 成员字段
	private NetMQTimer _timer;
	private NetMQPoller _poller;
	private NetMQQueue<Packet> _queue;
	private PublisherSocket _publisher;
	private EventChannel _channel;
	#endregion

	#region 构造函数
	public ZeroQueue(string name, IConnectionSettings connectionSettings) : base(name, connectionSettings)
	{
		if(connectionSettings == null)
			throw new ArgumentNullException(nameof(connectionSettings));

		if(string.IsNullOrEmpty(connectionSettings.Server))
			throw new ArgumentException($"The required server address is missing in the connection settings.");

		//如果未指定远程队列服务器端口号则设置默认端口号
		if(connectionSettings.Port == 0)
			connectionSettings.Port = ZeroQueueServer.PORT;

		//生成当前消息队列的唯一标识
		this.Identifier = GenerateIdentifier(connectionSettings);

		_locker = new();
		_queue = new NetMQQueue<Packet>();
		_queue.ReceiveReady += this.OnQueueReady;
		_poller = new NetMQPoller() { _queue };

		//如果没有指定心跳间隔则默认为心跳时间为10秒，如果显式指定了心跳间隔小于等于零则表示不启用心跳
		if(!connectionSettings.TryGetValue<int>("heartbeat", out var heartbeat) || heartbeat > 0)
		{
			_timer = new NetMQTimer(TimeSpan.FromSeconds(heartbeat > 0 ? heartbeat : 10));
			_timer.Elapsed += this.OnElapsed;
			_poller.Add(_timer);
		}
	}
	#endregion

	#region 公共属性
	/// <summary>获取当前队列的唯一标识。</summary>
	public string Identifier { get; }
	#endregion

	#region 公共方法
	public IEventChannel Channel => this.IsDisposed ? null : _channel ??= new EventChannel(this);
	#endregion

	#region 订阅方法
	protected override ZeroSubscriber CreateSubscriber(string topic, string tags, IHandler<Message> handler, MessageSubscribeOptions options) => new ZeroSubscriber(this, topic, handler, options);
	protected override ValueTask<bool> OnSubscribeAsync(ZeroSubscriber subscriber, CancellationToken cancellation = default)
	{
		//确保初始化完成
		this.Initialize();

		//执行网络订阅方法
		var channel = subscriber.Subscribe($"tcp://{this.ConnectionSettings.Server}:{_publisherPort}");

		//将订阅成功的网络通道加入到轮询器中
		if(channel != null)
			_poller.Add(channel);

		return ValueTask.FromResult(true);
	}

	protected override void OnUnsubscribed(ZeroSubscriber subscriber) => this.Unregister(subscriber.Channel);
	#endregion

	#region 发布方法
	public override ValueTask<string> ProduceAsync(string topic, string tags, ReadOnlyMemory<byte> data, MessageEnqueueOptions options = null, CancellationToken cancellation = default)
	{
		//确保初始化完成
		this.Initialize();

		if(string.IsNullOrEmpty(topic) || topic == "*")
		{
			foreach(var subscriber in this.Subscribers)
				_queue.Enqueue(new Packet(this.GetTopic(subscriber.Topic), data, options));
		}
		else
		{
			_queue.Enqueue(new Packet(this.GetTopic(topic), data, options));
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
					_publisher.SendMoreFrame(Packetizer.Pack(this.GetTopic(subscriber.Topic))).SendFrameEmpty();

				return;
			}

			var head = Packetizer.Pack(this.Identifier, packet.Topic, packet.Data, packet.Options, out var compressor);
			var data = string.IsNullOrEmpty(compressor) ? packet.Data.ToArray() : IO.Compression.Compressor.Compress(compressor, packet.Data.ToArray());
			_publisher.SendMoreFrame(head).SendFrame(data);
		}
	}
	#endregion

	#region 内部方法
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

	/// <summary>获取要发送和订阅的消息主题。</summary>
	/// <param name="topic">指定的原始主题。</param>
	/// <returns>返回处理过的消息主题。</returns>
	internal string GetTopic(string topic) => string.IsNullOrEmpty(this.ConnectionSettings.Group) ?
		(topic == "*" ? string.Empty : topic) : $"{this.ConnectionSettings.Group}:{(topic == "*" ? string.Empty : topic)}";
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

			//获取网络交换器的发布和订阅端口号
			(_publisherPort, _subscriberPort) = GetPorts(this.ConnectionSettings);

			//创建一个发布者套接字
			var publisher = new PublisherSocket();

			publisher.Options.SendHighWatermark = 1000;
			publisher.Options.HeartbeatInterval = TimeSpan.FromSeconds(30);
			publisher.Connect($"tcp://{this.ConnectionSettings.Server}:{_subscriberPort}");

			//将已经连接就绪的发布者保存
			_publisher = publisher;

			//启动网络轮询器
			if(!_poller.IsRunning)
				_poller.RunAsync();
		}

		static (ushort publisherPort, ushort subscriberPort) GetPorts(IConnectionSettings settings)
		{
			using var requester = new RequestSocket($"tcp://{settings.Server}:{settings.Port}");

			//发送请求获取交换器端口号
			requester.SendFrameEmpty();

			//接收返回的请求响应信息
			var response = requester.ReceiveFrameString();

			//如果响应信息为空则返回失败
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

	private static string GenerateIdentifier(IConnectionSettings connectionSettings)
	{
		if(connectionSettings == null)
			return Randomizer.GenerateString(10);

		if(string.IsNullOrEmpty(connectionSettings.Client))
			return string.IsNullOrEmpty(connectionSettings.Instance) ? Randomizer.GenerateString(10) : $"{connectionSettings.Instance}-{Randomizer.GenerateInt32():X}";
		else
			return string.IsNullOrEmpty(connectionSettings.Instance) ? $"{connectionSettings.Client}-{Randomizer.GenerateInt32():X}" : $"{connectionSettings.Client}:{connectionSettings.Instance}-{Randomizer.GenerateInt32():X}";
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
