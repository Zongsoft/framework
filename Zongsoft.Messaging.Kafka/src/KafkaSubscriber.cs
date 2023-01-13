/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
 *
 * Copyright (C) 2010-2023 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Confluent.Kafka;

namespace Zongsoft.Messaging.Kafka
{
	internal class KafkaSubscriber : MessageConsumerBase
	{
		private Poller _poller;
		private IConsumer<string, byte[]> _consumer;

		public KafkaSubscriber(KafkaQueue queue, string topics, IMessageHandler handler, MessageSubscribeOptions options = null) : base(topics, null, options, handler)
		{
			if(queue == null)
				throw new ArgumentNullException(nameof(queue));

			_consumer = new ConsumerBuilder<string, byte[]>(KafkaUtility.GetConsumerOptions(queue.ConnectionSetting)).Build();
			_poller = new Poller(this);
		}

		protected override ValueTask OnSubscribeAsync(IEnumerable<string> topics, string tags, MessageSubscribeOptions options, CancellationToken cancellation)
		{
			if(topics != null && topics.Any())
			{
				if(topics.Count() > 1 || topics.First() != "*")
					throw new NotSupportedException($"This message queue does not support unsubscribing to specified topics.");
			}

			_consumer.Subscribe(topics);
			return ValueTask.CompletedTask;
		}

		protected override ValueTask OnUnsubscribeAsync(IEnumerable<string> topics, CancellationToken cancellation)
		{
			_consumer.Unsubscribe();
			return ValueTask.CompletedTask;
		}

		protected override void OnSubscribed() => _poller?.Start();
		protected override void OnUnsubscribed() => _poller?.Stop();

		internal Message Receive(MessageDequeueOptions options, CancellationToken cancellation)
		{
			if(_consumer == null)
				throw new InvalidOperationException($"The message queue topic to consume is not yet subscribed.");

			ConsumeResult<string, byte[]> result = null;

			if(options.Timeout > TimeSpan.Zero)
				cancellation = CancellationTokenSource.CreateLinkedTokenSource(new CancellationTokenSource(options.Timeout).Token, cancellation).Token;

			try
			{
				//同步方式从消息队列中拉取消息(堵塞当前线程)
				result = _consumer.Consume(cancellation);
			}
			catch(OperationCanceledException)
			{
				return Message.Empty;
			}

			if(result.IsPartitionEOF)
				return Message.Empty;

			//构建接收到的消息
			return new Message(result.Message.Key, result.Topic, result.Message.Value, cancellation => _consumer.Commit(result))
			{
				Timestamp = result.Message.Timestamp.UtcDateTime,
			};
		}

		protected override void Dispose(bool disposing)
		{
			var poller = Interlocked.Exchange(ref _poller, null);
			if(poller != null)
				poller.Dispose();

			base.Dispose(disposing);

			var consumer = Interlocked.Exchange(ref _consumer, null);
			if(consumer != null)
				consumer.Dispose();
		}

		private class Poller : MessagePollerBase
		{
			private KafkaSubscriber _subscriber;

			public Poller(KafkaSubscriber subscriber)
			{
				_subscriber = subscriber ?? throw new ArgumentNullException(nameof(subscriber));
			}

			protected override Message Receive(MessageDequeueOptions options, CancellationToken cancellation)
			{
				return _subscriber?.Receive(options, cancellation) ?? Message.Empty;
			}

			protected override ValueTask OnHandleAsync(Message message, CancellationToken cancellation)
			{
				return _subscriber?.Handler?.HandleAsync(message, cancellation) ?? ValueTask.CompletedTask;
			}

			protected override void Dispose(bool disposing)
			{
				base.Dispose(disposing);
				_subscriber = null;
			}
		}
	}
}