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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;
using System.Threading;
using System.Threading.Tasks;

using Confluent.Kafka;

using Zongsoft.Components;
using Zongsoft.Configuration;

namespace Zongsoft.Messaging.Kafka;

public class KafkaQueue : MessageQueueBase<KafkaSubscriber, Configuration.KafkaConnectionSettings>
{
	#region 常量定义
	internal const string COMPRESSION_HEADER = "Zongsoft-Compression";
	#endregion

	#region 成员字段
	private IProducer<Null, byte[]> _producer;
	private ConsumerBuilder<string, byte[]> _builder;
	#endregion

	#region 构造函数
	public KafkaQueue(string name, Configuration.KafkaConnectionSettings settings) : base(name, settings)
	{
		_producer = new ProducerBuilder<Null, byte[]>(settings.GetProducerOptions()).Build();
		_builder = new ConsumerBuilder<string, byte[]>(settings.GetConsumerOptions());
		this.Features.Add(MessageQueueFeature.Compression);
	}
	#endregion

	#region 生成方法
	protected override async ValueTask<string> OnProduceAsync(string topic, string tags, ReadOnlyMemory<byte> data, MessageEnqueueOptions options, CancellationToken cancellation)
	{
		if(string.IsNullOrEmpty(topic))
			throw new ArgumentNullException(nameof(topic));

		var payload = data.ToArray();
		var message = new Message<Null, byte[]> { Value = payload };
		var compression = options?.Compression ?? default;

		if(compression.CanCompress(payload.Length))
		{
			message.Value = compression.Compress(payload);
			message.Headers = new Headers { new Header(COMPRESSION_HEADER, System.Text.Encoding.UTF8.GetBytes(compression.Name)) };
		}

		var result = await _producer.ProduceAsync(topic, message, cancellation);
		return result.TopicPartition.ToString();
	}
	#endregion

	#region 订阅方法
	protected override async ValueTask<bool> OnSubscribeAsync(KafkaSubscriber subscriber, CancellationToken cancellation = default)
	{
		await subscriber.SubscribeAsync(_builder.Build(), cancellation);
		return true;
	}

	protected override ValueTask<KafkaSubscriber> CreateSubscriberAsync(string topic, string tags, IHandler<Message> handler, MessageSubscribeOptions options, CancellationToken cancellation)
	{
		return ValueTask.FromResult(new KafkaSubscriber(this, topic, handler, options));
	}
	#endregion

	#region 重写方法
	protected override MessageReliability Reliability => MessageReliability.LeastOnce;
	#endregion

	#region 资源释放
	protected override ValueTask DisposeAsync(bool disposing)
	{
		var producer = Interlocked.Exchange(ref _producer, null);
		if(producer != null)
			producer.Dispose();

		_builder = null;
		return ValueTask.CompletedTask;
	}
	#endregion
}
