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

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Zongsoft.Messaging.RabbitMQ;

public class RabbitSubscriber : MessageConsumerBase<RabbitQueue>, IAsyncBasicConsumer
{
	#region 成员字段
	private string _consumerTag;
	private readonly string _queue;
	private IChannel _channel;
	#endregion

	#region 构造函数
	public RabbitSubscriber(RabbitQueue queue, IChannel channel, string topic, string tags, Components.IHandler<Message> handler, MessageSubscribeOptions options = null) : this(queue, channel, queue?.QueueName, topic, tags, handler, options) { }
	internal RabbitSubscriber(RabbitQueue queue, IChannel channel, string queueName, string topic, string tags, Components.IHandler<Message> handler, MessageSubscribeOptions options = null) : base(queue, topic, tags, handler, options)
	{
		_channel = channel ?? throw new ArgumentNullException(nameof(channel));
		_queue = queueName ?? string.Empty;
	}
	#endregion

	#region 公共属性
	public IChannel Channel => _channel;
	#endregion

	#region 重写方法
	protected override async ValueTask OnCloseAsync(CancellationToken cancellation)
	{
		var channel = _channel;
		if(channel == null || channel.IsClosed)
			return;

		var consumerTag = Interlocked.Exchange(ref _consumerTag, null);

		try
		{
			if(!string.IsNullOrEmpty(consumerTag))
				await channel.BasicCancelAsync(consumerTag, false, cancellation);
		}
		finally
		{
			if(channel.IsOpen)
				await channel.CloseAsync(cancellation);
		}
	}
	#endregion

	#region 内部方法
	internal async Task<string> SubscribeAsync(CancellationToken cancellation)
	{
		var consumerTag = await _channel.BasicConsumeAsync(_queue, false, string.Join(',', this.Tags), this, cancellation);
		_consumerTag = consumerTag;
		return consumerTag;
	}
	#endregion

	#region 事件处理
	Task IAsyncBasicConsumer.HandleChannelShutdownAsync(object channel, ShutdownEventArgs reason)
	{
		return Task.CompletedTask;
	}
	Task IAsyncBasicConsumer.HandleBasicCancelAsync(string tag, CancellationToken cancellation)
	{
		Interlocked.CompareExchange(ref _consumerTag, null, tag);
		return Task.CompletedTask;
	}
	Task IAsyncBasicConsumer.HandleBasicCancelOkAsync(string tag, CancellationToken cancellation)
	{
		Interlocked.CompareExchange(ref _consumerTag, null, tag);
		return Task.CompletedTask;
	}
	Task IAsyncBasicConsumer.HandleBasicConsumeOkAsync(string tag, CancellationToken cancellation)
	{
		_consumerTag = tag;
		return Task.CompletedTask;
	}

	async Task IAsyncBasicConsumer.HandleBasicDeliverAsync(string tag, ulong delivery, bool redelivered, string exchange, string topic, IReadOnlyBasicProperties properties, ReadOnlyMemory<byte> data, CancellationToken cancellation)
	{
		var channel = _channel;
		if(channel == null)
			return;

		var message = properties == null || string.IsNullOrEmpty(properties.MessageId) ?
			new Message(topic, data.ToArray(), cancellation => channel.BasicAckAsync(delivery, false, cancellation)) :
			new Message(properties.MessageId, topic, data.ToArray(), cancellation => channel.BasicAckAsync(delivery, false, cancellation));

		await this.Handler.HandleAsync(message, cancellation);
	}
	#endregion

	#region 处置方法
	protected override async ValueTask DisposeAsync(bool disposing)
	{
		try
		{
			await base.DisposeAsync(disposing);
		}
		finally
		{
			if(disposing)
			{
				var channel = Interlocked.Exchange(ref _channel, null);
				channel?.Dispose();
			}
		}
	}
	#endregion
}
