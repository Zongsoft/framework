using System;

using Zongsoft.Messaging;
using Zongsoft.Configuration;

using Xunit;

namespace Zongsoft.Messaging.Mqtt.Tests;

public class MqttQueueFactoryTest
{
	[Fact]
	public void TestCreate()
	{
		var settings = Configuration.MqttConnectionSettingsDriver.Instance.GetSettings("Mqtt", "server=127.0.0.1;username=program;password=xxxxxx;");
		var factory = new MqttQueueFactory();
		var queue = factory.Create(settings);
		Assert.NotNull(queue);
	}
}