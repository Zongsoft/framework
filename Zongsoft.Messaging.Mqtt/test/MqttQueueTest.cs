using System;

using Zongsoft.Messaging;
using Zongsoft.Configuration;

using Xunit;

namespace Zongsoft.Messaging.Mqtt.Tests;

public class MqttQueueTest
{
	private const string TOPIC = "TopicX";
	private const string CONNECTION_STRING = $"Server=127.0.0.1;UserName=program;Password=MyMQTT2-Password;Topic={TOPIC}";

	[Fact]
	public void TestProduce()
	{
		var settings = new ConnectionSettings("MyMqtt", CONNECTION_STRING);
		var queue = new MqttQueue(string.Empty, settings);
		Assert.NotNull(queue);

		var topic = settings.Values.TryGetValue("Topic", out var value) ? value : TOPIC;
		var data = System.Text.Encoding.UTF8.GetBytes("Hello, World!");
		var result = queue.Produce(topic, data);
	}
}