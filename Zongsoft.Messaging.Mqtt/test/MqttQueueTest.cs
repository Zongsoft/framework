using System;

using Zongsoft.Messaging;
using Zongsoft.Configuration;

using Xunit;

namespace Zongsoft.Messaging.Mqtt.Tests;

public class MqttQueueTest
{
	[Fact]
	public void TestProduce()
	{
		var settings = new ConnectionSettings("MyMqtt", @"Server=192.168.2.200;UserName=program;Password=Yuanshan.MQTT@2024;Topic=Topic1");
		var queue = new MqttQueue(string.Empty, settings);
		Assert.NotNull(queue);

		var topic = settings.Values.TryGetValue("Topic", out var value) ? value : "Topic1";
		var data = System.Text.Encoding.UTF8.GetBytes("Hello, World!");
		var result = queue.Produce(topic, data);
	}
}