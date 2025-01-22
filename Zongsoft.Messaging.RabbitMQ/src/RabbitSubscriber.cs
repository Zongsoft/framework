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
	private IChannel _channel;
	#endregion

	#region 构造函数
	public RabbitSubscriber(RabbitQueue queue, IChannel channel, string topic, string tags, Components.IHandler<Message> handler, MessageSubscribeOptions options = null) : base(queue, topic, tags, handler, options)
	{
		_channel = channel;
	}
	#endregion

	#region 公共属性
	public IChannel Channel { get => _channel; internal set => _channel = value; }
	#endregion

	#region 重写方法
	protected override ValueTask OnCloseAsync(CancellationToken cancellation) => new (_channel.CloseAsync(cancellation));
	#endregion

	#region 内部方法
	internal Task<string> SubscribeAsync(string queue, CancellationToken cancellation)
	{
		return _channel.BasicConsumeAsync(queue, false, string.Join(',', this.Tags), this, cancellation);
	}
	#endregion

	#region 事件处理
	public Task HandleBasicCancelAsync(string tag, CancellationToken cancellation = default)
	{
		return Task.CompletedTask;
	}
	public Task HandleBasicCancelOkAsync(string tag, CancellationToken cancellation = default)
	{
		return Task.CompletedTask;
	}
	public Task HandleBasicConsumeOkAsync(string tag, CancellationToken cancellation = default)
	{
		return Task.CompletedTask;
	}
	public async Task HandleBasicDeliverAsync(string tag, ulong delivery, bool redelivered, string exchange, string topic, IReadOnlyBasicProperties properties, ReadOnlyMemory<byte> data, CancellationToken cancellation = default)
	{
		var message = new Message(topic, data.ToArray(), cancellation => _channel.BasicAckAsync(delivery, false, cancellation));
		await this.Handler.HandleAsync(message, cancellation);
	}
	public Task HandleChannelShutdownAsync(object channel, ShutdownEventArgs reason)
	{
		return Task.CompletedTask;
	}
	#endregion

	#region 处置方法
	protected override ValueTask DisposeAsync(bool disposing)
	{
		var channel = Interlocked.Exchange(ref _channel, null);

		if(disposing)
		{
			channel?.CloseAsync();
			channel?.Dispose();
		}

		return base.DisposeAsync(disposing);
	}
	#endregion
}