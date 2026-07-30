using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

using RabbitMQ.Client;

using Xunit;

using Zongsoft.Collections;
using Zongsoft.Components;

namespace Zongsoft.Messaging.RabbitMQ.Tests;

internal static class RabbitTestUtility
{
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
