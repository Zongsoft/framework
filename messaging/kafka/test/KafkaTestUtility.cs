using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
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
	private static readonly Regex _testTopicPattern = new(
		@"^tests-(?:basic|concurrent|initialize|unsubscribe|unsubscribe-concurrent)-[0-9a-f]{32}$",
		RegexOptions.Compiled | RegexOptions.CultureInvariant);
	private static readonly Regex _testGroupPattern = new(
		@"^tests-(?:subscriber|initialize-group|concurrent-group-\d+|unsubscribe-group-\d+)-[0-9a-f]{32}$",
		RegexOptions.Compiled | RegexOptions.CultureInvariant);

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

	public static async Task CreateTopicAsync(string topic, int partitions = 1)
	{
		using var admin = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = Server }).Build();

		try
		{
			await admin.CreateTopicsAsync(
				[new TopicSpecification { Name = topic, NumPartitions = partitions, ReplicationFactor = 1 }],
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

	public static async Task WarmUpGroupsAsync(KafkaQueue publisher, string topic, KafkaMessageAudit[] audits, TimeSpan timeout)
	{
		var deadline = DateTime.UtcNow + timeout;
		var sequence = 0;

		do
		{
			await publisher.ProduceAsync(topic, Encoding.UTF8.GetBytes($"warmup:{sequence++:D4}"));
			var ready = await Task.WhenAll(audits.Select(audit => audit.WaitForTotalCountAsync(1, TimeSpan.FromMilliseconds(750))));
			if(ready.All(value => value))
				return;
		}
		while(DateTime.UtcNow < deadline);

		throw new XunitException($"Not all Kafka consumer groups became ready for topic '{topic}' within {timeout}.");
	}

	public static async Task<KafkaBrokerResources> GetResourcesAsync(string topic, IEnumerable<string> groups)
	{
		using var admin = CreateAdminClient();
		var metadata = admin.GetMetadata(TimeSpan.FromSeconds(10));
		var groupResult = await admin.ListConsumerGroupsAsync(new ListConsumerGroupsOptions { RequestTimeout = TimeSpan.FromSeconds(10) });
		var requestedGroups = groups?.ToHashSet(StringComparer.Ordinal) ?? [];
		var existingGroups = groupResult.Valid
			.Select(group => group.GroupId)
			.Where(requestedGroups.Contains)
			.OrderBy(group => group, StringComparer.Ordinal)
			.ToArray();

		return new KafkaBrokerResources(metadata.Topics.Any(item => string.Equals(item.Topic, topic, StringComparison.Ordinal)), existingGroups);
	}

	public static async Task<KafkaBrokerResources> DeleteResourcesAsync(string topic, IEnumerable<string> groups, TimeSpan timeout)
	{
		if(string.IsNullOrEmpty(topic) || !_testTopicPattern.IsMatch(topic))
			throw new ArgumentException("Only exact generated test topic names can be deleted.", nameof(topic));

		var groupArray = groups?.Distinct(StringComparer.Ordinal).ToArray() ?? [];
		if(groupArray.Any(group => string.IsNullOrEmpty(group) || !_testGroupPattern.IsMatch(group)))
			throw new ArgumentException("Only exact generated test consumer group names can be deleted.", nameof(groups));

		var deadline = DateTime.UtcNow + timeout;
		KafkaBrokerResources resources = default;
		Exception failure = null;

		do
		{
			using var admin = CreateAdminClient();

			if(groupArray.Length > 0)
			{
				try
				{
					await admin.DeleteGroupsAsync(groupArray, new DeleteGroupsOptions
					{
						RequestTimeout = TimeSpan.FromSeconds(10),
					});
				}
				catch(DeleteGroupsException exception) when(exception.Results.All(result =>
					result.Error.Code == ErrorCode.GroupIdNotFound ||
					result.Error.Code == ErrorCode.NonEmptyGroup ||
					result.Error.Code == ErrorCode.GroupSubscribedToTopic))
				{
					failure = exception;
				}
			}

			try
			{
				await admin.DeleteTopicsAsync([topic], new DeleteTopicsOptions
				{
					OperationTimeout = TimeSpan.FromSeconds(10),
					RequestTimeout = TimeSpan.FromSeconds(10),
				});
			}
			catch(DeleteTopicsException exception) when(exception.Results.All(result =>
				result.Error.Code == ErrorCode.UnknownTopicOrPart))
			{
				failure = exception;
			}

			resources = await GetResourcesAsync(topic, groupArray);
			if(!resources.TopicExists && resources.Groups.Length == 0)
				return resources;

			await Task.Delay(TimeSpan.FromMilliseconds(500));
		}
		while(DateTime.UtcNow < deadline);

		throw new XunitException($"Kafka resources were not removed within {timeout}. TopicExists={resources.TopicExists}; Groups=[{string.Join(',', resources.Groups)}]. LastError={failure?.Message}");
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

	private static IAdminClient CreateAdminClient() => new AdminClientBuilder(new AdminClientConfig
	{
		BootstrapServers = Server,
		SocketTimeoutMs = 10000,
	}).Build();

}

internal readonly record struct KafkaBrokerResources(bool TopicExists, string[] Groups);

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

internal sealed class KafkaMessageAudit : HandlerBase<Message>, IDisposable
{
	private readonly string _prefix;
	private readonly ConcurrentDictionary<string, byte> _messages = new(StringComparer.Ordinal);
	private readonly SemaphoreSlim _signal = new(0);
	private int _totalCount;
	private int _acknowledgementCount;
	private int _duplicateCount;

	public KafkaMessageAudit(string prefix) => _prefix = prefix ?? string.Empty;

	public int TotalCount => _totalCount;
	public int Count => _messages.Count;
	public int AcknowledgementCount => _acknowledgementCount;
	public int DuplicateCount => _duplicateCount;
	public string[] Payloads => _messages.Keys.OrderBy(payload => payload, StringComparer.Ordinal).ToArray();

	public Task<bool> WaitForTotalCountAsync(int count, TimeSpan timeout) => this.WaitForCountAsync(() => this.TotalCount, count, timeout);
	public Task<bool> WaitForCountAsync(int count, TimeSpan timeout) => this.WaitForCountAsync(() => this.Count, count, timeout);

	public void Dispose() => _signal.Dispose();

	protected override async ValueTask OnHandleAsync(Message message, Parameters parameters, CancellationToken cancellation)
	{
		await message.AcknowledgeAsync(cancellation);
		var payload = Encoding.UTF8.GetString(message.Data);

		if(payload.StartsWith(_prefix, StringComparison.Ordinal))
		{
			Interlocked.Increment(ref _acknowledgementCount);
			if(!_messages.TryAdd(payload, 0))
				Interlocked.Increment(ref _duplicateCount);
		}

		Interlocked.Increment(ref _totalCount);
		_signal.Release();
	}

	private async Task<bool> WaitForCountAsync(Func<int> counter, int count, TimeSpan timeout)
	{
		using var cancellation = new CancellationTokenSource(timeout);

		try
		{
			while(counter() < count)
				await _signal.WaitAsync(cancellation.Token);

			return true;
		}
		catch(OperationCanceledException)
		{
			return false;
		}
	}
}
