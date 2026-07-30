using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

using Confluent.Kafka;
using Confluent.Kafka.Admin;

using Xunit.Sdk;

using Zongsoft.Collections;
using Zongsoft.Components;

namespace Zongsoft.Messaging.Kafka.Tests;

internal static class KafkaTestUtility
{
	public const string Server = "127.0.0.1:9092";

	public static KafkaQueue CreateQueue(string client, string group)
	{
		var settings = Configuration.KafkaConnectionSettingsDriver.Instance.GetSettings("Kafka",
			$"server={Server};client={client};group={group};heartbeat=3s;timeout=10s;");

		return new KafkaQueue("Kafka", settings);
	}

	public static bool IsAvailable()
	{
		try
		{
			using var admin = new AdminClientBuilder(new AdminClientConfig
			{
				BootstrapServers = Server,
				SocketTimeoutMs = 5000,
			}).Build();

			return admin.GetMetadata(TimeSpan.FromSeconds(10)).Brokers.Count > 0;
		}
		catch
		{
			return false;
		}
	}

	public static async Task CreateTopicAsync(string topic)
	{
		using var admin = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = Server }).Build();

		try
		{
			await admin.CreateTopicsAsync(
				[new TopicSpecification { Name = topic, NumPartitions = 1, ReplicationFactor = 1 }],
				new CreateTopicsOptions
				{
					OperationTimeout = TimeSpan.FromSeconds(5),
					RequestTimeout = TimeSpan.FromSeconds(5),
				});
		}
		catch(CreateTopicsException exception) when(exception.Results.All(result => result.Error.Code == ErrorCode.TopicAlreadyExists))
		{
		}
	}

	public static async Task<(string Identifier, Message Message)> ProduceAndReceiveAsync(
		KafkaQueue publisher, string topic, string text, KafkaMessageBuffer messages, TimeSpan timeout)
	{
		var deadline = DateTime.UtcNow + timeout;

		do
		{
			var identifier = await publisher.ProduceAsync(topic, Encoding.UTF8.GetBytes(text));
			var message = await messages.TryReceiveAsync(TimeSpan.FromMilliseconds(750));

			if(message.HasValue)
				return (identifier, message.Value);
		}
		while(DateTime.UtcNow < deadline);

		throw new XunitException($"No message was received from Kafka topic '{topic}' within {timeout}.");
	}
}

internal sealed class KafkaMessageBuffer : HandlerBase<Message>, IDisposable
{
	private readonly ConcurrentQueue<Message> _messages = new();
	private readonly SemaphoreSlim _signal = new(0);
	private int _acknowledgementCount;

	public int AcknowledgementCount => _acknowledgementCount;

	public async Task<Message?> TryReceiveAsync(TimeSpan timeout)
	{
		using var cancellation = new CancellationTokenSource(timeout);

		try
		{
			await _signal.WaitAsync(cancellation.Token);
		}
		catch(OperationCanceledException)
		{
			return null;
		}

		return _messages.TryDequeue(out var message) ? message : null;
	}

	public async Task DrainAsync(TimeSpan quietPeriod)
	{
		while(await this.TryReceiveAsync(quietPeriod) != null)
		{
		}
	}

	public void Dispose() => _signal.Dispose();

	protected override async ValueTask OnHandleAsync(Message message, Parameters parameters, CancellationToken cancellation)
	{
		await message.AcknowledgeAsync(cancellation);
		Interlocked.Increment(ref _acknowledgementCount);
		_messages.Enqueue(message);
		_signal.Release();
	}
}
