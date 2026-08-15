using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

using StackExchange.Redis;

using Zongsoft.Components;
using Zongsoft.Externals.Redis.Configuration;
using Zongsoft.Externals.Redis.Messaging;

namespace Zongsoft.Externals.Redis.Tests;

internal static class RedisTestUtility
{
	public const string Server = "127.0.0.1:6379";
	public const string Password = "xxxxxx";

	public static bool IsTestingEnabled
	{
		get
		{
			var value = Environment.GetEnvironmentVariable("ZONGSOFT_REDIS_TESTS");
			return System.Diagnostics.Debugger.IsAttached ||
				string.Equals(value, "1", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
		}
	}

	public static RedisQueue CreateQueue(string name, string group = null, string client = null, int deadline = 3, string idleTimeout = "2s")
	{
		var settings = RedisConnectionSettingsDriver.Instance.GetSettings(name,
			$"server={Server};password={Password};group={group};client={client};timeout=5s;deadline={deadline};idleTimeout={idleTimeout};");

		return new RedisQueue(name, settings);
	}

	public static bool IsAvailable()
	{
		try
		{
			using var connection = ConnectionMultiplexer.Connect($"{Server},password={Password},connectTimeout=2000");
			return connection.GetDatabase().Ping() >= TimeSpan.Zero;
		}
		catch
		{
			return false;
		}
	}

	public static string GetQueueKey(string name, string topic) => $"Zongsoft.Queue:{name}:{topic}";
}

internal sealed class RedisMessageBuffer : HandlerBase<Zongsoft.Messaging.Message>, IDisposable
{
	private readonly ConcurrentQueue<Zongsoft.Messaging.Message> _messages = new();
	private readonly SemaphoreSlim _signal = new(0);
	private readonly bool _acknowledge;
	private int _acknowledgementCount;

	public RedisMessageBuffer(bool acknowledge = true) => _acknowledge = acknowledge;

	public int AcknowledgementCount => _acknowledgementCount;

	public async Task<Zongsoft.Messaging.Message?> ReceiveAsync(TimeSpan timeout)
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

	protected override async ValueTask OnHandleAsync(Zongsoft.Messaging.Message message, Zongsoft.Collections.Parameters parameters, CancellationToken cancellation)
	{
		if(_acknowledge)
		{
			await message.AcknowledgeAsync(cancellation);
			Interlocked.Increment(ref _acknowledgementCount);
		}

		_messages.Enqueue(message);
		_signal.Release();
	}
}
