using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

using MQTTnet;

using Xunit;

using Zongsoft.Collections;
using Zongsoft.Components;

namespace Zongsoft.Messaging.Mqtt.Tests;

internal static class MqttTestUtility
{
	public static MqttQueue CreateQueue(ushort serverPort, string client)
	{
		var settings = Configuration.MqttConnectionSettingsDriver.Instance.GetSettings("Mqtt",
			$"server=127.0.0.1:{serverPort};client={client}-{Guid.NewGuid():N};timeout=5s;reconnectInterval=200ms;keepAlive=2s;cleanSession=true;");

		return new MqttQueue("MQTT", settings);
	}

	public static ushort GetFreePort()
	{
		using var listener = new TcpListener(IPAddress.Loopback, 0);
		listener.Start();
		return (ushort)((IPEndPoint)listener.LocalEndpoint).Port;
	}

	public static bool CanBind(ushort port)
	{
		try
		{
			using var listener = new TcpListener(IPAddress.Any, port);
			listener.Start();
			return true;
		}
		catch
		{
			return false;
		}
	}

	public static bool CanConnect(ushort port)
	{
		try
		{
			using var client = new TcpClient();
			return client.ConnectAsync(IPAddress.Loopback, port).Wait(TimeSpan.FromMilliseconds(250));
		}
		catch
		{
			return false;
		}
	}

	public static async Task<bool> WaitUntilAsync(Func<bool> predicate, TimeSpan timeout)
	{
		var deadline = DateTime.UtcNow + timeout;

		do
		{
			if(predicate())
				return true;

			await Task.Delay(50);
		}
		while(DateTime.UtcNow < deadline);

		return predicate();
	}

	public static async Task<bool> WaitUntilAsync(Func<Task<bool>> predicate, TimeSpan timeout)
	{
		var deadline = DateTime.UtcNow + timeout;

		do
		{
			if(await predicate())
				return true;

			await Task.Delay(50);
		}
		while(DateTime.UtcNow < deadline);

		return await predicate();
	}

}

internal sealed class MqttServerScope : IDisposable
{
	private readonly MqttQueueServer _server;

	private MqttServerScope(MqttQueueServer server) => _server = server;

	public ushort Port => _server.Port;

	public static async Task<MqttServerScope> StartAsync()
	{
		var server = new MqttQueueServer { Port = MqttTestUtility.GetFreePort() };
		await server.StartAsync([]);
		Assert.Equal(Zongsoft.Components.WorkerState.Running, server.State);
		return new MqttServerScope(server);
	}

	public void Dispose() => ((IDisposable)_server).Dispose();
}

internal sealed class MqttMessageBuffer : HandlerBase<Message>, IDisposable
{
	private readonly ConcurrentQueue<Message> _messages = new();
	private readonly SemaphoreSlim _signal = new(0);

	public async Task<Message> ReceiveAsync(TimeSpan timeout)
	{
		using var cancellation = new CancellationTokenSource(timeout);
		await _signal.WaitAsync(cancellation.Token);

		Assert.True(_messages.TryDequeue(out var message));
		return message;
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

		Assert.True(_messages.TryDequeue(out var message));
		return message;
	}

	public async Task<Message[]> ReceiveManyAsync(int count, TimeSpan timeout)
	{
		var messages = new Message[count];
		using var cancellation = new CancellationTokenSource(timeout);

		for(int i = 0; i < count; i++)
		{
			await _signal.WaitAsync(cancellation.Token);
			Assert.True(_messages.TryDequeue(out messages[i]));
		}

		return messages;
	}

	public void Dispose() => _signal.Dispose();

	protected override async ValueTask OnHandleAsync(Message message, Parameters parameters, CancellationToken cancellation)
	{
		_messages.Enqueue(message);
		_signal.Release();
		await message.AcknowledgeAsync(cancellation);
	}
}

internal sealed class ConcurrentMessageHandler(int expectedCount) : HandlerBase<Message>
{
	private readonly TaskCompletionSource _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
	private readonly int _expectedCount = expectedCount;
	private int _active;
	private int _count;
	private int _maximumConcurrency;

	public int Count => _count;
	public int MaximumConcurrency => _maximumConcurrency;

	public async Task WaitAsync(TimeSpan timeout)
	{
		using var cancellation = new CancellationTokenSource(timeout);
		await _completion.Task.WaitAsync(cancellation.Token);
	}

	protected override async ValueTask OnHandleAsync(Message message, Parameters parameters, CancellationToken cancellation)
	{
		var active = Interlocked.Increment(ref _active);
		UpdateMaximum(active);

		try
		{
			await Task.Delay(100, cancellation);
			await message.AcknowledgeAsync(cancellation);
		}
		finally
		{
			Interlocked.Decrement(ref _active);

			if(Interlocked.Increment(ref _count) == _expectedCount)
				_completion.TrySetResult();
		}
	}

	private void UpdateMaximum(int value)
	{
		var maximum = _maximumConcurrency;

		while(value > maximum)
		{
			var current = Interlocked.CompareExchange(ref _maximumConcurrency, value, maximum);
			if(current == maximum)
				return;

			maximum = current;
		}
	}
}
