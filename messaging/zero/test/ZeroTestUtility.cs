using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

using NetMQ;
using NetMQ.Sockets;

using Xunit;

using Zongsoft.Collections;
using Zongsoft.Communication;
using Zongsoft.Components;

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
			socket.SendFrameEmpty();

			return socket.TryReceiveFrameString(TimeSpan.FromMilliseconds(250), out var response) &&
				response != null &&
				response.Contains("Publisher=", StringComparison.OrdinalIgnoreCase) &&
				response.Contains("Subscriber=", StringComparison.OrdinalIgnoreCase);
		}
		catch
		{
			return false;
		}
	}

	public static (ushort Publisher, ushort Subscriber) GetServerPorts(ushort port)
	{
		//部分测试需要绕过 ZeroQueue，使用原生 NetMQ socket 注入非本库格式的消息。
		using var socket = new RequestSocket();
		socket.Connect($"tcp://127.0.0.1:{port}");
		socket.SendFrameEmpty();

		if(!socket.TryReceiveFrameString(TimeSpan.FromSeconds(5), out var response) || string.IsNullOrEmpty(response))
			throw new InvalidOperationException($"Failed to query ZeroMQ server ports from '{port}'.");

		ushort publisher = 0;
		ushort subscriber = 0;

		foreach(var entry in response.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
		{
			var index = entry.IndexOf('=');

			if(index <= 0 || index >= entry.Length - 1)
				continue;

			var name = entry[..index];
			var value = entry[(index + 1)..];

			if(string.Equals(name, "Publisher", StringComparison.OrdinalIgnoreCase))
				ushort.TryParse(value, out publisher);
			else if(string.Equals(name, "Subscriber", StringComparison.OrdinalIgnoreCase))
				ushort.TryParse(value, out subscriber);
		}

		if(publisher == 0 || subscriber == 0)
			throw new InvalidOperationException($"Invalid ZeroMQ server port response: '{response}'.");

		return (publisher, subscriber);
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
