using System;
using System.Threading.Tasks;

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
}
