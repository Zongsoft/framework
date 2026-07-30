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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Messaging.Kafka library.
 *
 * The Zongsoft.Messaging.Kafka is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Messaging.Kafka is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Messaging.Kafka library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Threading;
using System.Threading.Tasks;

using Confluent.Kafka;

namespace Zongsoft.Messaging.Kafka;

public class KafkaSubscriber : MessageConsumerBase<KafkaQueue>
{
	#region 私有变量
	private readonly object _syncRoot = new();
	private int _closed = 1;
	#endregion

	#region 成员字段
	private Poller _poller;
	private IConsumer<string, byte[]> _consumer;
	#endregion

	#region 构造函数
	public KafkaSubscriber(KafkaQueue queue, string topic, Components.IHandler<Message> handler, MessageSubscribeOptions options = null) : base(queue, topic, handler, options)
	{
		_poller = new Poller(this);
	}
	#endregion

	#region 重写方法
	protected override ValueTask OnCloseAsync(CancellationToken cancellation)
	{
		_poller?.Stop();

		if(Interlocked.Exchange(ref _closed, 1) == 0)
		{
			lock(_syncRoot)
				_consumer?.Close();
		}

		return ValueTask.CompletedTask;
	}
	#endregion

	#region 内部方法
	internal void Subscribe(IConsumer<string, byte[]> consumer)
	{
		if(consumer == null)
			throw new ArgumentNullException(nameof(consumer));

		lock(_syncRoot)
		{
			if(_consumer != null)
				throw new InvalidOperationException($"The message queue topic has already been subscribed.");

			try
			{
				consumer.Subscribe(this.Topic);
				_consumer = consumer;
				_closed = 0;
			}
			catch
			{
				consumer.Dispose();
				throw;
			}
		}

		_poller.Start();
	}

	internal Message Receive(MessageDequeueOptions options, CancellationToken cancellation)
	{
		CancellationTokenSource source = null;
		ConsumeResult<string, byte[]> result = null;

		if(options.Timeout > TimeSpan.Zero)
		{
			source = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
			source.CancelAfter(options.Timeout);
			cancellation = source.Token;
		}

		try
		{
			//同步方式从消息队列中拉取消息(堵塞当前线程)
			lock(_syncRoot)
			{
				if(_consumer == null)
					throw new InvalidOperationException($"The message queue topic to consume is not yet subscribed.");

				result = _consumer.Consume(cancellation);
			}
		}
		catch(OperationCanceledException)
		{
			return Message.Empty;
		}
		finally
		{
			source?.Dispose();
		}

		if(result.IsPartitionEOF)
			return Message.Empty;

		//构建接收到的消息
		return new Message(result.Message.Key, result.Topic, result.Message.Value, () => this.Commit(result))
		{
			Timestamp = result.Message.Timestamp.UtcDateTime,
		};
	}

	private void Commit(ConsumeResult<string, byte[]> result)
	{
		lock(_syncRoot)
			_consumer?.Commit(result);
	}
	#endregion

	#region 处置方法
	protected override async ValueTask DisposeAsync(bool disposing)
	{
		await base.DisposeAsync(disposing);

		var poller = Interlocked.Exchange(ref _poller, null);
		poller?.Dispose();

		lock(_syncRoot)
		{
			var consumer = Interlocked.Exchange(ref _consumer, null);
			consumer?.Dispose();
		}
	}
	#endregion

	#region 嵌套子类
	private sealed class Poller(KafkaSubscriber subscriber) : MessagePollerBase
	{
		private KafkaSubscriber _subscriber = subscriber ?? throw new ArgumentNullException(nameof(subscriber));

		protected override Message Receive(MessageDequeueOptions options, CancellationToken cancellation) => _subscriber?.Receive(options, cancellation) ?? Message.Empty;
		protected override ValueTask OnHandleAsync(Message message, CancellationToken cancellation) => _subscriber?.Handler?.HandleAsync(message, cancellation) ?? ValueTask.CompletedTask;

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			_subscriber = null;
		}
	}
	#endregion
}
