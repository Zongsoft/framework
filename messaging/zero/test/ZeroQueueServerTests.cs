using System;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;

using NetMQ.Sockets;

using Xunit;

namespace Zongsoft.Messaging.ZeroMQ.Tests;

public class ZeroQueueServerTests
{
	[Fact]
	public async Task QueueServerStopReleasesPortAndAllowsRestart()
	{
		var port = ZeroTestUtility.GetFreePort();
		var server = new ZeroQueueServer { Port = port };

		try
		{
			await server.StartAsync([]);
			Assert.True(await ZeroTestUtility.WaitUntilAsync(() => ZeroTestUtility.CanQueryServer(port), TimeSpan.FromSeconds(5)));

			await server.StopAsync([]);
			Assert.True(await ZeroTestUtility.WaitUntilAsync(() => !ZeroTestUtility.CanQueryServer(port), TimeSpan.FromSeconds(5)));
			Assert.True(await ZeroTestUtility.WaitUntilAsync(() => ZeroTestUtility.CanBindZeroMq(port), TimeSpan.FromSeconds(5)));

			await server.StartAsync([]);
			Assert.True(await ZeroTestUtility.WaitUntilAsync(() => ZeroTestUtility.CanQueryServer(port), TimeSpan.FromSeconds(5)));

			await server.StopAsync([]);
			Assert.True(await ZeroTestUtility.WaitUntilAsync(() => !ZeroTestUtility.CanQueryServer(port), TimeSpan.FromSeconds(5)));
			Assert.True(await ZeroTestUtility.WaitUntilAsync(() => ZeroTestUtility.CanBindZeroMq(port), TimeSpan.FromSeconds(5)));
		}
		finally
		{
			((IDisposable)server).Dispose();
		}
	}

	[Fact]
	public async Task QueueServerStartWithExplicitExchangePortsBindsAndForwardsMessages()
	{
		var port = ZeroTestUtility.GetFreePort();
		var incoming = ZeroTestUtility.GetFreePort();
		var outgoing = ZeroTestUtility.GetFreePort();
		var server = new ZeroQueueServer { Port = port };

		try
		{
			await server.StartAsync([$"--incoming:{incoming}", $"--outgoing:{outgoing}"]);

			var ports = ZeroTestUtility.GetServerPorts(port);
			Assert.Equal(outgoing, ports.Publisher);
			Assert.Equal(incoming, ports.Subscriber);
			Assert.False(ZeroTestUtility.CanBindZeroMq(incoming));
			Assert.False(ZeroTestUtility.CanBindZeroMq(outgoing));

			using var publisher = ZeroTestUtility.CreateQueue(port, "publisher");
			using var subscriber = ZeroTestUtility.CreateQueue(port, "subscriber");
			using var handler = new MessageBuffer();

			await subscriber.SubscribeAsync("topic/explicit", handler);
			await Task.Delay(750);

			for(int i = 0; i < 3; i++)
			{
				await publisher.ProduceAsync("topic/explicit", Encoding.UTF8.GetBytes("explicit"));
				await Task.Delay(100);
			}

			var message = await handler.ReceiveAsync(TimeSpan.FromSeconds(5));
			Assert.Equal("explicit", Encoding.UTF8.GetString(message.Data));
		}
		finally
		{
			await server.StopAsync([]);
			((IDisposable)server).Dispose();
		}
	}

	[Fact]
	public async Task QueueServerStartFailureReleasesInitializedSockets()
	{
		var port = ZeroTestUtility.GetFreePort();
		var server = new ZeroQueueServer { Port = port };

		using var blocker = new ResponseSocket();
		blocker.Bind($"tcp://*:{port}");

		try
		{
			await server.StartAsync([]);

			Assert.Equal(Zongsoft.Components.WorkerState.Stopped, server.State);
			Assert.Null(GetField("_proxy"));
			Assert.Null(GetField("_poller"));
			Assert.Null(GetField("_responser"));
			Assert.Null(GetField("_publisher"));
			Assert.Null(GetField("_subscriber"));
		}
		finally
		{
			((IDisposable)server).Dispose();
		}

		object GetField(string name) => typeof(ZeroQueueServer).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(server);
	}
}
