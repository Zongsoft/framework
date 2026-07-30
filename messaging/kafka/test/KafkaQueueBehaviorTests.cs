using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Confluent.Kafka;

using Xunit;

namespace Zongsoft.Messaging.Kafka.Tests;

public class KafkaQueueBehaviorTests
{
	[Fact]
	public async Task ProduceMapsTopicPayloadAndReturnsPartitionIdentifier()
	{
		var settings = Configuration.KafkaConnectionSettingsDriver.Instance.GetSettings("Kafka", "server=127.0.0.1:9092;client=Producer-Unit;");
		using var queue = new KafkaQueue("Kafka", settings);
		var producer = DispatchProxy.Create<IProducer<Null, byte[]>, ProducerProxy>();
		var proxy = (ProducerProxy)(object)producer;
		ReplaceField(queue, "_producer", producer);
		var payload = new byte[] { 1, 3, 5, 7 };

		var identifier = await queue.ProduceAsync("unit-produce", payload);

		Assert.Equal("unit-produce", proxy.Topic);
		Assert.Equal(payload, proxy.Data);
		Assert.Equal(new TopicPartition("unit-produce", new Partition(2)).ToString(), identifier);
	}

	[Fact]
	public async Task ReceiveMapsMessageAndAcknowledgementCommitsOffset()
	{
		var settings = Configuration.KafkaConnectionSettingsDriver.Instance.GetSettings("Kafka", "server=127.0.0.1:9092;client=Consumer-Unit;group=Consumer-Unit;");
		using var queue = new KafkaQueue("Kafka", settings);
		var timestamp = DateTime.UtcNow.AddMinutes(-1);
		var consumed = new ConsumeResult<string, byte[]>
		{
			Topic = "unit-receive",
			Partition = new Partition(1),
			Offset = new Offset(42),
			Message = new Confluent.Kafka.Message<string, byte[]>
			{
				Key = "message-key",
				Value = [2, 4, 6, 8],
				Timestamp = new Timestamp(timestamp),
			},
		};
		var consumer = DispatchProxy.Create<IConsumer<string, byte[]>, ConsumerProxy>();
		var proxy = (ConsumerProxy)(object)consumer;
		proxy.Result = consumed;

		var subscriber = new KafkaSubscriber(queue, consumed.Topic, null);
		ReplaceField(subscriber, "_consumer", consumer);

		try
		{
			var message = Receive(subscriber, new MessageDequeueOptions(), CancellationToken.None);

			Assert.False(message.IsEmpty);
			Assert.Equal("message-key", message.Identifier);
			Assert.Equal(consumed.Topic, message.Topic);
			Assert.Equal(consumed.Message.Value, message.Data);
			Assert.Equal(consumed.Message.Timestamp.UtcDateTime, message.Timestamp);
			Assert.Null(proxy.Committed);

			await message.AcknowledgeAsync();
			Assert.Same(consumed, proxy.Committed);
		}
		finally
		{
			await ((IAsyncDisposable)subscriber).DisposeAsync();
		}
	}

	[Fact]
	public async Task ReceiveCancellationReturnsEmptyMessage()
	{
		var settings = Configuration.KafkaConnectionSettingsDriver.Instance.GetSettings("Kafka", "server=127.0.0.1:9092;client=Consumer-Cancel;group=Consumer-Cancel;");
		using var queue = new KafkaQueue("Kafka", settings);
		var consumer = DispatchProxy.Create<IConsumer<string, byte[]>, ConsumerProxy>();
		var proxy = (ConsumerProxy)(object)consumer;
		proxy.Exception = new OperationCanceledException();

		var subscriber = new KafkaSubscriber(queue, "unit-cancel", null);
		ReplaceField(subscriber, "_consumer", consumer);

		try
		{
			var message = Receive(subscriber, new MessageDequeueOptions(TimeSpan.FromMilliseconds(10)), CancellationToken.None);

			Assert.True(message.IsEmpty);
			Assert.Null(proxy.Committed);
		}
		finally
		{
			await ((IAsyncDisposable)subscriber).DisposeAsync();
		}
	}

	[Fact]
	public async Task ReceivePartitionEndReturnsEmptyMessage()
	{
		var settings = Configuration.KafkaConnectionSettingsDriver.Instance.GetSettings("Kafka", "server=127.0.0.1:9092;client=Consumer-Eof;group=Consumer-Eof;");
		using var queue = new KafkaQueue("Kafka", settings);
		var consumer = DispatchProxy.Create<IConsumer<string, byte[]>, ConsumerProxy>();
		var proxy = (ConsumerProxy)(object)consumer;
		proxy.Result = new ConsumeResult<string, byte[]>
		{
			Topic = "unit-eof",
			Partition = new Partition(0),
			Offset = Offset.End,
			IsPartitionEOF = true,
		};

		var subscriber = new KafkaSubscriber(queue, "unit-eof", null);
		ReplaceField(subscriber, "_consumer", consumer);

		try
		{
			var message = Receive(subscriber, new MessageDequeueOptions(), CancellationToken.None);

			Assert.True(message.IsEmpty);
			Assert.Null(proxy.Committed);
		}
		finally
		{
			await ((IAsyncDisposable)subscriber).DisposeAsync();
		}
	}

	[Fact]
	public async Task SubscribeNullConsumerThrowsArgumentNullException()
	{
		var settings = Configuration.KafkaConnectionSettingsDriver.Instance.GetSettings("Kafka", "server=127.0.0.1:9092;client=Consumer-Null;group=Consumer-Null;");
		using var queue = new KafkaQueue("Kafka", settings);
		var subscriber = new KafkaSubscriber(queue, "unit-null", null);

		try
		{
			var method = typeof(KafkaSubscriber).GetMethod("Subscribe", BindingFlags.Instance | BindingFlags.NonPublic);
			var exception = Assert.Throws<TargetInvocationException>(() => method.Invoke(subscriber, [null]));
			Assert.IsType<ArgumentNullException>(exception.InnerException);
		}
		finally
		{
			await ((IAsyncDisposable)subscriber).DisposeAsync();
		}
	}

	private static Message Receive(KafkaSubscriber subscriber, MessageDequeueOptions options, CancellationToken cancellation)
	{
		var method = typeof(KafkaSubscriber).GetMethod("Receive", BindingFlags.Instance | BindingFlags.NonPublic);
		return Assert.IsType<Message>(method.Invoke(subscriber, [options, cancellation]));
	}

	private static void ReplaceField<T>(object target, string name, T replacement)
	{
		var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
		var original = field.GetValue(target) as IDisposable;
		field.SetValue(target, replacement);
		original?.Dispose();
	}

	private class ProducerProxy : DispatchProxy
	{
		public string Topic { get; private set; }
		public byte[] Data { get; private set; }

		protected override object Invoke(MethodInfo targetMethod, object[] args)
		{
			if(targetMethod.Name == nameof(IProducer<Null, byte[]>.ProduceAsync))
			{
				this.Topic = Assert.IsType<string>(args[0]);
				var message = Assert.IsType<Confluent.Kafka.Message<Null, byte[]>>(args[1]);
				this.Data = message.Value;

				return Task.FromResult(new DeliveryResult<Null, byte[]>
				{
					Topic = this.Topic,
					Partition = new Partition(2),
					Offset = new Offset(17),
					Message = message,
				});
			}

			return null;
		}
	}

	private class ConsumerProxy : DispatchProxy
	{
		public ConsumeResult<string, byte[]> Result { get; set; }
		public Exception Exception { get; set; }
		public ConsumeResult<string, byte[]> Committed { get; private set; }

		protected override object Invoke(MethodInfo targetMethod, object[] args)
		{
			if(targetMethod.Name == nameof(IConsumer<string, byte[]>.Consume) && args is [CancellationToken])
			{
				if(this.Exception != null)
					throw this.Exception;

				return this.Result;
			}

			if(targetMethod.Name == nameof(IConsumer<string, byte[]>.Commit) && args is [ConsumeResult<string, byte[]> result])
			{
				this.Committed = result;
				return null;
			}

			return null;
		}
	}
}
