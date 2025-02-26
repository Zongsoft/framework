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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

using Zongsoft.Components;
using Zongsoft.Configuration;

namespace Zongsoft.Messaging.RabbitMQ;

/// <summary>
/// 提供 RabbitMQ 消息队列的生成和订阅相关功能。
/// 有关 RabbitMQ 的详细技术说明，请参考：https://rabbitmq.cn/tutorials
/// </summary>
public class RabbitQueue : MessageQueueBase<RabbitSubscriber, Configuration.RabbitConnectionSettings>
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
	public RabbitQueue(string name, Configuration.RabbitConnectionSettings settings) : base(name, settings)
	{
		_connectionFactory = settings.GetOptions();
	}
	#endregion

	#region 公共属性
	internal string Exchanger => string.IsNullOrEmpty(this.Settings.Group) ? "/" : this.Settings.Group;
	internal string QueueName => this.Settings.Queue;
	#endregion

	#region 生成方法
	protected override async ValueTask<string> OnProduceAsync(string topic, string tags, ReadOnlyMemory<byte> data, MessageEnqueueOptions options, CancellationToken cancellation)
	{
		if(string.IsNullOrEmpty(topic))
			throw new ArgumentNullException(nameof(topic));

		//尝试初始化环境
		await this.InitializeAsync(cancellation);

		//确保主题格式正确
		topic = topic.Replace('/', '.');

		//构建消息参数
		BasicProperties properties = new BasicProperties
		{
			MessageId = Common.Randomizer.GenerateString(12),
		};

		if(options != null)
		{
			//设置优先级
			properties.Priority = options.Priority;

			//设置消息的有效期
			if(options.Expiration > TimeSpan.Zero)
				properties.Expiration = options.Expiration.TotalMilliseconds.ToString("#");

			//设置消息的可选属性集
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

		//发送消息
		await _channel.BasicPublishAsync(this.Exchanger, topic, false, properties, data, cancellation);

		//返回消息编号
		return properties.MessageId;
	}
	#endregion

	#region 订阅方法
	protected override async ValueTask<bool> OnSubscribeAsync(RabbitSubscriber subscriber, CancellationToken cancellation = default)
	{
		var identifier = await subscriber.SubscribeAsync(this.QueueName, cancellation);
		return !string.IsNullOrEmpty(identifier);
	}

	protected override async ValueTask<RabbitSubscriber> CreateSubscriberAsync(string topic, string tags, IHandler<Message> handler, MessageSubscribeOptions options, CancellationToken cancellation)
	{
		//确保主题格式正确
		topic = string.IsNullOrEmpty(topic) ? "#" : topic.Replace('/', '.');

		//尝试初始化环境
		await this.InitializeAsync(cancellation);

		//绑定队列订阅主题
		await _channel.QueueBindAsync(_channel.CurrentQueue, this.Exchanger, topic, null, false, cancellation);

		//返回创建的订阅者
		return new RabbitSubscriber(this, _channel, topic, tags, handler, options);
	}
	#endregion

	#region 初始方法
	private async ValueTask InitializeAsync(CancellationToken cancellation)
	{
		if(_channel != null && _channel.IsOpen)
			return;

		if(_connection == null || !_connection.IsOpen)
			_connection = await _connectionFactory.CreateConnectionAsync(GetClient(this.Settings), cancellation);

		if(_channel == null || _channel.IsClosed)
		{
			_channel = await _connection.CreateChannelAsync(new CreateChannelOptions(false, false), cancellation);

			//通过QoS开启工作者模式
			//await _channel.BasicQosAsync(0, 1, false, cancellation);

			//定义消息交换器
			await _channel.ExchangeDeclareAsync(this.Exchanger, ExchangeType.Topic, true, false, null, false, cancellation);

			//定义消息队列
			var queue = string.IsNullOrEmpty(this.QueueName) ?
				await _channel.QueueDeclareAsync(cancellationToken: cancellation) :
				await _channel.QueueDeclareAsync(this.QueueName, true, false, false, null, false, cancellation);
		}

		static string GetClient(Configuration.RabbitConnectionSettings settings) => string.IsNullOrEmpty(settings.Client) ? $"Zongsoft.RabbitMQ:{Common.Randomizer.GenerateString()}" : settings.Client;
	}
	#endregion

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