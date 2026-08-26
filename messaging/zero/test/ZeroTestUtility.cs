using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Collections.Concurrent;

using NetMQ;
using NetMQ.Sockets;

using Xunit;

using Zongsoft.Collections;
using Zongsoft.Communication;
using Zongsoft.Components;
using Zongsoft.Configuration;

namespace Zongsoft.Messaging.ZeroMQ.Tests;

internal static class ZeroTestUtility
{
	public static ZeroQueue CreateQueue(ushort serverPort, string client, Action<Configuration.ZeroConnectionSettings> configure = null)
	{
		var settings = Configuration.ZeroConnectionSettingsDriver.Instance.GetSettings("ZeroMQ",
			$"server=127.0.0.1;port={serverPort};client={client}-{Guid.NewGuid():N};Timeout=5s;");

		configure?.Invoke(settings);

		return new ZeroQueue("ZeroMQ", settings);
	}

	public static ushort GetFreePort()
	{
		using var listener = new TcpListener(IPAddress.Loopback, 0);
		listener.Start();
		return (ushort)((IPEndPoint)listener.LocalEndpoint).Port;
	}

	public static bool CanBindZeroMq(ushort port)
	{
		try
		{
			using var socket = new ResponseSocket();
			socket.Bind($"tcp://*:{port}");
			return true;
		}
		catch
		{
			return false;
		}
	}

	public static bool CanQueryServer(ushort port)
	{
		try
		{
			using var socket = new RequestSocket();
			socket.Connect($"tcp://127.0.0.1:{port}");
			socket.SendFrame(GetDiscoveryRequest());

			return socket.TryReceiveFrameString(TimeSpan.FromMilliseconds(250), out var response) && TryGetServerPorts(response, out _);
		}
		catch
		{
			return false;
		}
	}

	public static (ushort Control, ushort Incoming, ushort Outgoing) GetServerPorts(ushort port)
	{
		//部分测试需要绕过 ZeroQueue，使用原生 NetMQ socket 注入非本库格式的消息。
		using var socket = new RequestSocket();
		socket.Connect($"tcp://127.0.0.1:{port}");
		socket.SendFrame(GetDiscoveryRequest());

		if(!socket.TryReceiveFrameString(TimeSpan.FromSeconds(5), out var response) || string.IsNullOrEmpty(response))
			throw new InvalidOperationException($"Failed to query ZeroMQ server ports from '{port}'.");

		if(!TryGetServerPorts(response, out var ports))
			throw new InvalidOperationException($"Invalid ZeroMQ server port response: '{response}'.");

		return ports;
	}

	private static string GetDiscoveryRequest() => Protocol.GetDiscoveryRequest($"tests-{Guid.NewGuid():N}");

	private static bool TryGetServerPorts(string response, out (ushort Control, ushort Incoming, ushort Outgoing) result)
	{
		result = default;
		if(string.IsNullOrEmpty(response))
			return false;

		var lines = response.Split('\n', StringSplitOptions.TrimEntries);
		if(lines.Length != 4 || Array.Exists(lines, string.IsNullOrEmpty) ||
		   !Protocol.TryParseDiscoveryResponse(response, out _, out var control, out var incoming, out var outgoing))
			return false;

		result = (control, incoming, outgoing);
		return true;
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

	public static async Task<string> PublishUntilAcceptedAsync(ZeroQueue queue, string topic, ReadOnlyMemory<byte> data, MessageEnqueueOptions options = null, TimeSpan timeout = default)
	{
		var deadline = DateTime.UtcNow + (timeout > TimeSpan.Zero ? timeout : TimeSpan.FromSeconds(5));
		do
		{
			var identifier = await queue.ProduceAsync(topic, data, options);
			if(identifier != null)
				return identifier;

			await Task.Delay(25);
		}
		while(DateTime.UtcNow < deadline);

		throw new TimeoutException($"No subscription for topic '{topic}' became visible before the test timeout.");
	}

	public static async Task<T[]> CollectAsync<T>(IAsyncEnumerable<T> source)
	{
		var result = new List<T>();
		await foreach(var item in source)
			result.Add(item);
		return result.ToArray();
	}

}

internal sealed class MemoryMessageStorage : IMessageStorage
{
	private readonly ConcurrentDictionary<string, Entry> _messages = new(StringComparer.Ordinal);
	private int _setCount;
	private int _removeCount;
	private int _getCount;

	public string Name => "memory";
	public bool Disposable => false;
	public IConnectionSettings Settings { get; set; } = new ConnectionSettings();
	public int SetCount => _setCount;
	public int RemoveCount => _removeCount;
	public int GetCount => _getCount;

	public ValueTask<int> ClearAsync(CancellationToken cancellation = default)
	{
		cancellation.ThrowIfCancellationRequested();
		var count = _messages.Count;
		_messages.Clear();
		return ValueTask.FromResult(count);
	}

	public ValueTask<int> ClearAsync(string topic, CancellationToken cancellation = default)
	{
		ArgumentNullException.ThrowIfNull(topic);
		cancellation.ThrowIfCancellationRequested();
		var count = 0;
		foreach(var entry in _messages)
		{
			if(string.Equals(entry.Value.Message.Topic, topic, StringComparison.Ordinal) && _messages.TryRemove(entry.Key, out _))
				count++;
		}

		return ValueTask.FromResult(count);
	}

	public ValueTask SetAsync(Message message, TimeSpan expiry = default, CancellationToken cancellation = default)
	{
		cancellation.ThrowIfCancellationRequested();
		ArgumentException.ThrowIfNullOrWhiteSpace(message.Identifier);
		var expiration = expiry > TimeSpan.Zero ? DateTime.UtcNow + expiry : default;
		_messages[message.Identifier] = new Entry(Clone(message), expiration);
		Interlocked.Increment(ref _setCount);
		return ValueTask.CompletedTask;
	}

	public ValueTask<bool> RemoveAsync(string identifier, CancellationToken cancellation = default)
	{
		cancellation.ThrowIfCancellationRequested();
		Interlocked.Increment(ref _removeCount);
		return ValueTask.FromResult(_messages.TryRemove(identifier, out _));
	}

	public async IAsyncEnumerable<Message> GetAsync([EnumeratorCancellation] CancellationToken cancellation = default)
	{
		Interlocked.Increment(ref _getCount);
		await Task.Yield();
		foreach(var entry in _messages)
		{
			cancellation.ThrowIfCancellationRequested();
			if(entry.Value.Expiration != default && entry.Value.Expiration <= DateTime.UtcNow)
			{
				_messages.TryRemove(entry.Key, out _);
				continue;
			}

			yield return Clone(entry.Value.Message);
		}
	}

	public async IAsyncEnumerable<Message> GetAsync(string topic, [EnumeratorCancellation] CancellationToken cancellation = default)
	{
		ArgumentNullException.ThrowIfNull(topic);
		Interlocked.Increment(ref _getCount);
		await Task.Yield();
		foreach(var entry in _messages)
		{
			cancellation.ThrowIfCancellationRequested();
			if(entry.Value.Expiration != default && entry.Value.Expiration <= DateTime.UtcNow)
			{
				_messages.TryRemove(entry.Key, out _);
				continue;
			}

			if(string.Equals(entry.Value.Message.Topic, topic, StringComparison.Ordinal))
				yield return Clone(entry.Value.Message);
		}
	}

	private static Message Clone(Message message) => new(message.Identifier, message.Topic, message.Data == null ? null : (byte[])message.Data.Clone())
	{
		Identity = message.Identity,
		Tags = message.Tags,
		Timestamp = message.Timestamp,
	};

	private readonly record struct Entry(Message Message, DateTime Expiration);
}

internal sealed class ZeroServerScope : IDisposable
{
	private readonly ZeroQueueServer _server;

	private ZeroServerScope(ZeroQueueServer server) => _server = server;

	public ushort Port => _server.Port;

	public static async Task<ZeroServerScope> StartAsync()
	{
		var server = new ZeroQueueServer { Port = ZeroTestUtility.GetFreePort() };
		await server.StartAsync([]);
		return new ZeroServerScope(server);
	}

	public void Dispose()
	{
		_server.Stop();
		((IDisposable)_server).Dispose();
	}
}

internal sealed class MessageCollector : HandlerBase<Message>
{
	private readonly TaskCompletionSource<Message> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

	public async Task<Message> ReceiveAsync(TimeSpan timeout)
	{
		using var cancellation = new CancellationTokenSource(timeout);
		await using var registration = cancellation.Token.Register(() => _completion.TrySetCanceled(cancellation.Token));

		return await _completion.Task;
	}

	protected override ValueTask OnHandleAsync(Message message, Parameters parameters, CancellationToken cancellation)
	{
		_completion.TrySetResult(message);
		return ValueTask.CompletedTask;
	}
}

internal sealed class MessageBuffer : HandlerBase<Message>, IDisposable
{
	//用于验证突发消息和“没有收到消息”的场景，单个 TaskCompletionSource 无法覆盖这些断言。
	private readonly ConcurrentQueue<Message> _messages = new();
	private readonly SemaphoreSlim _signal = new(0);

	public int Count => _messages.Count;

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

		for(int i = 0; i < count; i++)
			messages[i] = await this.ReceiveAsync(timeout);

		return messages;
	}

	public void Dispose() => _signal.Dispose();

	protected override ValueTask OnHandleAsync(Message message, Parameters parameters, CancellationToken cancellation)
	{
		_messages.Enqueue(message);
		_signal.Release();
		return ValueTask.CompletedTask;
	}
}

[Handler("rpc/echo")]
internal sealed class EchoHandler : HandlerBase<IRequest>
{
	protected override async ValueTask OnHandleAsync(IRequest request, Parameters parameters, CancellationToken cancellation)
	{
		var responder = parameters.GetValue<IResponder>();
		Assert.NotNull(responder);
		await responder.RespondAsync(request.Response(request.Data), cancellation);
	}
}
