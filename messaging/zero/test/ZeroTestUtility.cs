using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using NetMQ;
using NetMQ.Sockets;

using Xunit;

using Zongsoft.Collections;
using Zongsoft.Communication;
using Zongsoft.Components;

namespace Zongsoft.Messaging.ZeroMQ.Tests;

internal static class ZeroTestUtility
{
	public static ZeroQueue CreateQueue(ushort serverPort, string client)
	{
		var settings = Configuration.ZeroConnectionSettingsDriver.Instance.GetSettings("ZeroMQ",
			$"server=127.0.0.1;port={serverPort};client={client}-{Guid.NewGuid():N};Timeout=5s;");

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
