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

public sealed class ZeroQueue : MessageQueueBase<ZeroSubscriber>
{
	#region 成员字段
	private NetMQPoller _poller;
	private NetMQQueue<Packet> _queue;
	private PublisherSocket _publisher;
	#endregion

	#region 构造函数
	public ZeroQueue(string name, IConnectionSettings connectionSettings) : base(name, connectionSettings)
	{
		if(connectionSettings == null)
			throw new ArgumentNullException(nameof(connectionSettings));

		if(string.IsNullOrEmpty(connectionSettings.Server))
			throw new ArgumentException($"The required server address is missing in the connection settings.");

		this.Identifier = GetIdentifier(connectionSettings);

		_queue = new NetMQQueue<Packet>();
		_queue.ReceiveReady += this.OnQueueReady;

		_publisher = new PublisherSocket();
		_publisher.Options.SendHighWatermark = 1000;
		_publisher.Connect($"tcp://{connectionSettings.Server}:5678");
		_poller = new NetMQPoller() { _queue };
	}
	#endregion

	#region 公共属性
	/// <summary>获取当前队列的唯一标识。</summary>
	public string Identifier { get; }
	#endregion

	#region 订阅方法
	protected override ZeroSubscriber CreateSubscriber(string topic, string tags, IHandler<Message> handler, MessageSubscribeOptions options) => new ZeroSubscriber(this, topic, handler, options);
	protected override ValueTask<bool> OnSubscribeAsync(ZeroSubscriber subscriber, CancellationToken cancellation = default)
	{
		var channel = subscriber.Subscribe($"tcp://{this.ConnectionSettings.Server}:1234");

		if(channel != null)
		{
			_poller.Add(channel);

			if(!_poller.IsRunning)
				_poller.RunAsync();
		}

		return ValueTask.FromResult(true);
	}

	protected override void OnUnsubscribed(ZeroSubscriber subscriber) => _poller.Remove(subscriber.Channel);
	#endregion

	#region 发布方法
	public override ValueTask<string> ProduceAsync(string topic, string tags, ReadOnlyMemory<byte> data, MessageEnqueueOptions options = null, CancellationToken cancellation = default)
	{
		_queue.Enqueue(new Packet(topic, data));
		return ValueTask.FromResult<string>(null);
	}

	private void OnQueueReady(object sender, NetMQQueueEventArgs<Packet> e)
	{
		if(e.Queue.TryDequeue(out var packet, TimeSpan.Zero))
			_publisher.SendMoreFrame(Utility.Pack(packet.Topic, this.Identifier)).SendFrame(packet.Data.ToArray());
	}
	#endregion

	#region 私有方法
	private static string GetIdentifier(IConnectionSettings connectionSettings)
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

		_queue = null;
		_poller = null;
		_publisher = null;
	}
	#endregion

	#region 嵌套结构
	private readonly struct Packet(string topic, ReadOnlyMemory<byte> data)
	{
		public readonly string Topic = topic;
		public readonly ReadOnlyMemory<byte> Data = data;
	}
	#endregion
}
