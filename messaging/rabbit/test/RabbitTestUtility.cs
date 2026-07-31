using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Collections.Concurrent;

using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

using Xunit;

using Zongsoft.Collections;
using Zongsoft.Components;

namespace Zongsoft.Messaging.RabbitMQ.Tests;

internal static class RabbitTestUtility
{
	private static readonly HttpClient _management = CreateManagementClient();
	private static readonly Regex _testExchangePattern = new(
		@"^tests\.exchange(?:\.(?:concurrent|initialize|unsubscribe))?\.[0-9a-f]{32}$",
		RegexOptions.Compiled | RegexOptions.CultureInvariant);

	public static RabbitQueue CreateQueue(string client, string exchange, string queue)
	{
		var settings = Configuration.RabbitConnectionSettingsDriver.Instance.GetSettings("RabbitMQ",
			$"server=127.0.0.1;port=5672;username=program;password=xxxxxx;client={client};group={exchange};queue={queue};timeout=20s;heartbeat=10s;");

		return new RabbitQueue("RabbitMQ", settings);
	}

	public static async Task<bool> IsAvailableAsync()
	{
		var factory = new ConnectionFactory
		{
			HostName = "127.0.0.1",
			Port = 5672,
			UserName = "program",
			Password = "xxxxxx",
			RequestedConnectionTimeout = TimeSpan.FromSeconds(15),
			HandshakeContinuationTimeout = TimeSpan.FromSeconds(15),
		};

		try
		{
			using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(20));
			await using var connection = await factory.CreateConnectionAsync("RabbitMQ-Test-Probe", cancellation.Token);
			return connection.IsOpen;
		}
		catch
		{
			return false;
		}
	}

	public static IConnection GetConnection(RabbitQueue queue) => GetField<IConnection>(queue, "_connection");
	public static IChannel GetPublishingChannel(RabbitQueue queue) => GetField<IChannel>(queue, "_channel");

	public static async Task DeleteTestQueueAsync(string queueName)
	{
		if(string.IsNullOrEmpty(queueName) || !queueName.StartsWith("tests.queue.", StringComparison.Ordinal))
			throw new ArgumentException("Only exact generated tests.queue.* queue names can be deleted.", nameof(queueName));

		var factory = CreateConnectionFactory();
		using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30));
		await using var connection = await factory.CreateConnectionAsync($"RabbitMQ-Test-Cleanup:{Guid.NewGuid():N}", cancellation.Token);
		await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellation.Token);

		try
		{
			await channel.QueueDeleteAsync(queueName, false, false, false, cancellation.Token);
		}
		catch(OperationInterruptedException exception) when(exception.ShutdownReason?.ReplyCode == 404)
		{
			//The exact generated queue was never created or was already removed.
		}
	}

	public static async Task DeleteTestExchangeAsync(string exchangeName)
	{
		if(string.IsNullOrEmpty(exchangeName) || !_testExchangePattern.IsMatch(exchangeName))
			throw new ArgumentException("Only exact generated tests.exchange.* exchange names can be deleted.", nameof(exchangeName));

		var factory = CreateConnectionFactory();
		using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30));
		await using var connection = await factory.CreateConnectionAsync($"RabbitMQ-Test-Cleanup:{Guid.NewGuid():N}", cancellation.Token);
		await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellation.Token);

		try
		{
			await channel.ExchangeDeleteAsync(exchangeName, false, false, cancellation.Token);
		}
		catch(OperationInterruptedException exception) when(exception.ShutdownReason?.ReplyCode == 404)
		{
			//The exact generated exchange was never created or was already removed.
		}
	}

	public static async Task<RabbitBrokerSnapshot> GetBrokerSnapshotAsync(CancellationToken cancellation = default)
	{
		using var response = await _management.GetAsync("/api/overview", cancellation);
		response.EnsureSuccessStatusCode();
		await using var content = await response.Content.ReadAsStreamAsync(cancellation);
		using var document = await JsonDocument.ParseAsync(content, cancellationToken: cancellation);
		var root = document.RootElement;
		var objects = root.GetProperty("object_totals");
		var queues = root.GetProperty("queue_totals");

		return new RabbitBrokerSnapshot(
			objects.GetProperty("connections").GetInt32(),
			objects.GetProperty("channels").GetInt32(),
			objects.GetProperty("queues").GetInt32(),
			objects.GetProperty("exchanges").GetInt32(),
			queues.GetProperty("messages_ready").GetInt64());
	}

	public static async Task<RabbitBrokerSnapshot> WaitForBrokerRestoreAsync(RabbitBrokerSnapshot baseline, TimeSpan timeout)
	{
		using var cancellation = new CancellationTokenSource(timeout);
		RabbitBrokerSnapshot current = default;

		try
		{
			do
			{
				current = await GetBrokerSnapshotAsync(cancellation.Token);
				if(current.IsAtMost(baseline))
					return current;

				await Task.Delay(TimeSpan.FromMilliseconds(250), cancellation.Token);
			}
			while(true);
		}
		catch(OperationCanceledException)
		{
			return current;
		}
	}

	private static ConnectionFactory CreateConnectionFactory() => new()
	{
		HostName = "127.0.0.1",
		Port = 5672,
		UserName = "program",
		Password = "xxxxxx",
		RequestedConnectionTimeout = TimeSpan.FromSeconds(15),
		HandshakeContinuationTimeout = TimeSpan.FromSeconds(15),
	};

	private static HttpClient CreateManagementClient()
	{
		var client = new HttpClient
		{
			BaseAddress = new Uri("http://127.0.0.1:15672"),
			Timeout = TimeSpan.FromSeconds(45),
		};
		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes("program:xxxxxx")));
		return client;
	}

	private static T GetField<T>(object target, string name) where T : class
	{
		var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
		return field?.GetValue(target) as T;
	}
}

internal readonly record struct RabbitBrokerSnapshot(int Connections, int Channels, int Queues, int Exchanges, long MessagesReady)
{
	public bool IsAtMost(RabbitBrokerSnapshot other) =>
		this.Connections <= other.Connections &&
		this.Channels <= other.Channels &&
		this.Queues <= other.Queues &&
		this.Exchanges <= other.Exchanges &&
		this.MessagesReady <= other.MessagesReady;
}

internal sealed class RabbitMessageBuffer : HandlerBase<Message>, IDisposable
{
	private readonly ConcurrentQueue<Message> _messages = new();
	private readonly SemaphoreSlim _signal = new(0);
	private int _acknowledgementCount;

	public int AcknowledgementCount => _acknowledgementCount;

	public async Task<Message> ReceiveAsync(TimeSpan timeout)
	{
		var message = await this.TryReceiveAsync(timeout);
		Assert.True(message.HasValue);
		return message.Value;
	}

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

	public void Dispose() => _signal.Dispose();

	protected override async ValueTask OnHandleAsync(Message message, Parameters parameters, CancellationToken cancellation)
	{
		await message.AcknowledgeAsync(cancellation);
		Interlocked.Increment(ref _acknowledgementCount);
		_messages.Enqueue(message);
		_signal.Release();
	}
}

internal sealed class RabbitMessageAudit : HandlerBase<Message>, IDisposable
{
	private readonly ConcurrentDictionary<string, string> _messages = new(StringComparer.Ordinal);
	private readonly SemaphoreSlim _signal = new(0);
	private int _acknowledgementCount;
	private int _duplicateCount;
	private int _invalidIdentifierCount;

	public int Count => _messages.Count;
	public int AcknowledgementCount => _acknowledgementCount;
	public int DuplicateCount => _duplicateCount;
	public int InvalidIdentifierCount => _invalidIdentifierCount;
	public string[] Payloads => _messages.Keys.OrderBy(payload => payload, StringComparer.Ordinal).ToArray();

	public async Task<bool> WaitForCountAsync(int count, TimeSpan timeout)
	{
		using var cancellation = new CancellationTokenSource(timeout);

		try
		{
			while(this.Count < count)
				await _signal.WaitAsync(cancellation.Token);

			return true;
		}
		catch(OperationCanceledException)
		{
			return false;
		}
	}

	public void Dispose() => _signal.Dispose();

	protected override async ValueTask OnHandleAsync(Message message, Parameters parameters, CancellationToken cancellation)
	{
		await message.AcknowledgeAsync(cancellation);
		Interlocked.Increment(ref _acknowledgementCount);

		var payload = Encoding.UTF8.GetString(message.Data);
		if(!_messages.TryAdd(payload, message.Identifier))
			Interlocked.Increment(ref _duplicateCount);
		if(string.IsNullOrEmpty(message.Identifier) || message.Identifier.Length != 12)
			Interlocked.Increment(ref _invalidIdentifierCount);

		_signal.Release();
	}
}
