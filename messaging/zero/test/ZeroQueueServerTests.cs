using System;
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
