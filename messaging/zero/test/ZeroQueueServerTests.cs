using System;
using System.Text;
using System.Threading.Tasks;

using NetMQ.Sockets;

using Xunit;

namespace Zongsoft.Messaging.ZeroMQ.Tests;

public class ZeroQueueServerTests
{
	[Fact]
	public async Task QueueServerStopReleasesPortAndAllowsRestart()
	{
		if(!Global.IsTestingEnabled)
			return;

		var port = ZeroTestUtility.GetFreePort();
		var server = new ZeroQueueServer { Port = port, Storage = new MemoryMessageStorage() };

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
	public async Task QueueServerWithoutStorageDoesNotStartControlChannel()
	{
		if(!Global.IsTestingEnabled)
			return;

		var port = ZeroTestUtility.GetFreePort();
		var control = ZeroTestUtility.GetFreePort();
		var server = new ZeroQueueServer { Port = port };

		try
		{
			await server.StartAsync([$"--control:{control}"]);
			Assert.Equal(0, ZeroTestUtility.GetServerPorts(port).Control);
			Assert.True(ZeroTestUtility.CanBindZeroMq(control));
			Assert.Throws<InvalidOperationException>(() => server.Storage = new MemoryMessageStorage());

			await server.StopAsync([]);
			server.Storage = new MemoryMessageStorage();
			await server.StartAsync([$"--control:{control}"]);
			Assert.Equal(control, ZeroTestUtility.GetServerPorts(port).Control);
			Assert.False(ZeroTestUtility.CanBindZeroMq(control));
		}
		finally
		{
			await server.StopAsync([]);
			((IDisposable)server).Dispose();
		}
	}

	[Fact]
	public async Task QueueServerStartWithExplicitExchangePortsBindsAndForwardsMessages()
	{
		if(!Global.IsTestingEnabled)
			return;

		var port = ZeroTestUtility.GetFreePort();
		var incoming = ZeroTestUtility.GetFreePort();
		var outgoing = ZeroTestUtility.GetFreePort();
		var control = ZeroTestUtility.GetFreePort();
		var server = new ZeroQueueServer { Port = port, Storage = new MemoryMessageStorage() };

		try
		{
			await server.StartAsync([$"--incoming:{incoming}", $"--outgoing:{outgoing}", $"--control:{control}"]);

			var ports = ZeroTestUtility.GetServerPorts(port);
			Assert.Equal(control, ports.Control);
			Assert.Equal(incoming, ports.Incoming);
			Assert.Equal(outgoing, ports.Outgoing);
			Assert.False(ZeroTestUtility.CanBindZeroMq(incoming));
			Assert.False(ZeroTestUtility.CanBindZeroMq(outgoing));
			Assert.False(ZeroTestUtility.CanBindZeroMq(control));

			using var publisher = ZeroTestUtility.CreateQueue(port, "publisher");
			using var subscriber = ZeroTestUtility.CreateQueue(port, "subscriber");
			using var handler = new MessageBuffer();

			await subscriber.SubscribeAsync("topic/explicit", handler);
			await ZeroTestUtility.PublishUntilAcceptedAsync(publisher, "topic/explicit", Encoding.UTF8.GetBytes("explicit"));

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
	public async Task QueueServerStartFailureLeavesServerStopped()
	{
		if(!Global.IsTestingEnabled)
			return;

		var port = ZeroTestUtility.GetFreePort();
		var server = new ZeroQueueServer { Port = port };

		using var blocker = new ResponseSocket();
		blocker.Bind($"tcp://*:{port}");

		try
		{
			await server.StartAsync([]);

			Assert.Equal(Zongsoft.Components.WorkerState.Stopped, server.State);
		}
		finally
		{
			((IDisposable)server).Dispose();
		}
	}
}
