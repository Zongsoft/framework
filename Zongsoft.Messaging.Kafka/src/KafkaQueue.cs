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
using System.Threading;
using System.Threading.Tasks;

using Confluent.Kafka;

using Zongsoft.Components;
using Zongsoft.Configuration;

namespace Zongsoft.Messaging.Kafka
{
	public class KafkaQueue : MessageQueueBase<KafkaSubscriber>
	{
		#region 成员字段
		private IProducer<Null, byte[]> _producer;
		private ConsumerBuilder<string, byte[]> _builder;
		#endregion

		#region 构造函数
		public KafkaQueue(string name, IConnectionSettings connectionSettings) : base(name, connectionSettings)
		{
			_producer = new ProducerBuilder<Null, byte[]>(KafkaUtility.GetProducerOptions(connectionSettings)).Build();
			_builder = new ConsumerBuilder<string, byte[]>(KafkaUtility.GetConsumerOptions(this.ConnectionSettings));
		}
		#endregion

		#region 生成方法
		public override async ValueTask<string> ProduceAsync(string topic, string tags, ReadOnlyMemory<byte> data, MessageEnqueueOptions options = null, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(topic))
				throw new ArgumentNullException(nameof(topic));

			var result = await _producer.ProduceAsync(topic, new Message<Null, byte[]> { Value = data.ToArray() }, cancellation);
			return result.TopicPartition.ToString();
		}
		#endregion

		#region 订阅方法
		protected override ValueTask<bool> OnSubscribeAsync(KafkaSubscriber subscriber, CancellationToken cancellation = default)
		{
			subscriber.Subscribe(_builder.Build());
			return ValueTask.FromResult(true);
		}

		protected override ValueTask<KafkaSubscriber> CreateSubscriberAsync(string topic, string tags, IHandler<Message> handler, MessageSubscribeOptions options, CancellationToken cancellation)
		{
			return ValueTask.FromResult(new KafkaSubscriber(this, topic, handler, options));
		}
		#endregion

		#region 资源释放
		protected override void Dispose(bool disposing)
		{
			var producer = Interlocked.Exchange(ref _producer, null);
			if(producer != null)
				producer.Dispose();

			_builder = null;
		}
		#endregion
	}
}