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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Messaging.RabbitMQ library.
 *
 * The Zongsoft.Messaging.RabbitMQ is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Messaging.RabbitMQ is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Messaging.RabbitMQ library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

using Zongsoft.Components;
using Zongsoft.Configuration;

namespace Zongsoft.Messaging.RabbitMQ;

public class RabbitQueue : MessageQueueBase<RabbitSubscriber>
{
	#region 常量定义
	internal const string NAME = "RabbitMQ";
	#endregion

	#region 成员字段
	private readonly IConnectionFactory _connectionFactory;
	private IConnection _connection;
	private IChannel _channel;
	#endregion

	#region 构造函数
	public RabbitQueue(string name, IConnectionSettings connectionSettings) : base(name, connectionSettings)
	{
		_connectionFactory = Configuration.RabbitConnectionSettingsDriver.Instance.GetOptions(connectionSettings);
	}
	#endregion

	#region 公共属性
	internal string Exchanger => string.IsNullOrEmpty(this.ConnectionSettings.Group) ? "/" : this.ConnectionSettings.Group;

	internal string QueueName
	{
		get
		{
			if(this.ConnectionSettings.TryGetValue("Queue", out var value) && value != null)
				return value.ToString();

			return string.IsNullOrEmpty(this.ConnectionSettings.Name) ? this.Name : this.ConnectionSettings.Name;
		}
	}
	#endregion

	#region 生成方法
	public override async ValueTask<string> ProduceAsync(string topic, string tags, ReadOnlyMemory<byte> data, MessageEnqueueOptions options = null, CancellationToken cancellation = default)
	{
		await this.InitializeAsync(cancellation);

		if(string.IsNullOrEmpty(topic))
			throw new ArgumentNullException(nameof(topic));

		if(!string.IsNullOrEmpty(topic))
			topic = topic.Replace('/', '.');

		BasicProperties properties = null;

		if(options != null)
		{
			properties = new BasicProperties();
			properties.MessageId = Guid.NewGuid().ToString("N");
			properties.Priority = options.Priority;
			properties.Expiration = options.Expiry.ToString();

			if(options.Properties != null && options.Properties.HasValue)
			{
				properties.Headers ??= new Dictionary<string, object>();

				foreach(var property in options.Properties)
				{
					if(property.Key is string name)
						properties.Headers[name] = property.Value;
				}
			}
		}

		if(properties == null)
			await _channel.BasicPublishAsync(this.Exchanger, topic, false, data, cancellation);
		else
			await _channel.BasicPublishAsync(this.Exchanger, topic, false, properties, data, cancellation);

		return null;
	}
	#endregion

	#region 订阅方法
	protected override async ValueTask<bool> OnSubscribeAsync(RabbitSubscriber subscriber, CancellationToken cancellation = default)
	{
		await this.InitializeAsync(cancellation);
		subscriber.Channel = _channel;

		var identifier = await subscriber.SubscribeAsync(this.QueueName, cancellation);
		return !string.IsNullOrEmpty(identifier);
	}

	protected override RabbitSubscriber CreateSubscriber(string topic, string tags, IHandler<Message> handler, MessageSubscribeOptions options)
	{
		return new RabbitSubscriber(this, _channel, topic, tags, handler, options);
	}
	#endregion

	private async ValueTask InitializeAsync(CancellationToken cancellation)
	{
		if(_channel != null && _channel.IsOpen)
			return;

		if(_connection == null || !_connection.IsOpen)
			_connection = await _connectionFactory.CreateConnectionAsync(cancellation);

		if(_channel == null || _channel.IsClosed)
		{
			_channel = await _connection.CreateChannelAsync(new CreateChannelOptions(false, false), cancellation);

			//通过Qos开启工作者模式
			//await _channel.BasicQosAsync(0, 1, false, cancellation);

			//定义消息交换器
			await _channel.ExchangeDeclareAsync(this.Exchanger, ExchangeType.Topic, true, false, null, false, cancellation);

			//定义消息队列
			var queue = string.IsNullOrEmpty(this.QueueName) ?
				await _channel.QueueDeclareAsync(cancellationToken: cancellation) :
				await _channel.QueueDeclareAsync(this.QueueName, true, false, false, null, false, cancellation);

			//绑定消息队列
			await _channel.QueueBindAsync(queue.QueueName, this.Exchanger, "#", null, false, cancellation);
			await _channel.QueueBindAsync(queue.QueueName, this.Exchanger, string.Empty, null, false, cancellation);
		}
	}

	#region 资源释放
	protected override void Dispose(bool disposing)
	{
		if(disposing)
		{
			var channel = Interlocked.Exchange(ref _channel, null);
			channel?.Dispose();

			var connection = Interlocked.Exchange(ref _connection, null);
			connection.Dispose();
		}

		_channel = null;
		_connection = null;
	}
	#endregion
}