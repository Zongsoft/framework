using System;
using Xunit;

namespace Zongsoft.Messaging.ZeroMQ.Tests;

public class ZeroQueueHeartbeatTests
{
	[Fact]
	public void ConnectionSettingsDefaultHeartbeatIsTenSeconds()
	{
		var settings = Configuration.ZeroConnectionSettingsDriver.Instance.GetSettings("ZeroMQ",
			$"server=127.0.0.1;port={ZeroTestUtility.GetFreePort()};client=heartbeat-default-{Guid.NewGuid():N};");

		Assert.Equal(TimeSpan.FromSeconds(10), settings.Heartbeat);
	}
}
