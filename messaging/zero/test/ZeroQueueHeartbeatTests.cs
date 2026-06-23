using System;
using System.Reflection;

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

		using var queue = new ZeroQueue("ZeroMQ", settings);
		Assert.NotNull(GetTimer(queue));
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	public void NonPositiveHeartbeatDisablesTimer(int seconds)
	{
		using var queue = ZeroTestUtility.CreateQueue(ZeroTestUtility.GetFreePort(), "heartbeat-disabled",
			settings => settings.Heartbeat = TimeSpan.FromSeconds(seconds));

		Assert.Null(GetTimer(queue));
	}

	private static object GetTimer(ZeroQueue queue) => typeof(ZeroQueue)
		.GetField("_timer", BindingFlags.Instance | BindingFlags.NonPublic)!
		.GetValue(queue);
}
