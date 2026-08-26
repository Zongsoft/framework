using System;
using System.Net;
using System.Text;

using MQTTnet;
using MQTTnet.Formatter;

using Xunit;

namespace Zongsoft.Messaging.Mqtt.Tests;

public class MqttQueueFactoryTests
{
	[Fact]
	public void CreateQueueWithSettings()
	{
		var settings = Configuration.MqttConnectionSettingsDriver.Instance.GetSettings("Mqtt",
			"server=127.0.0.1:5101;client=Factory-Test;username=program;password=secret;timeout=5s;reconnectInterval=3s;keepAlive=20s;cleanSession=true;protocolVersion=V500;");

		var options = settings.GetOptions();
		var endpoint = Assert.IsType<MqttClientTcpOptions>(options.ChannelOptions);
		var remoteEndpoint = Assert.IsType<DnsEndPoint>(endpoint.RemoteEndpoint);

		Assert.Equal("127.0.0.1", remoteEndpoint.Host);
		Assert.Equal(5101, remoteEndpoint.Port);
		Assert.Equal("Factory-Test", options.ClientId);
		Assert.Equal("program", options.Credentials.GetUserName(options));
		Assert.Equal("secret", Encoding.UTF8.GetString(options.Credentials.GetPassword(options)));
		Assert.Equal(TimeSpan.FromSeconds(5), options.Timeout);
		Assert.Equal(TimeSpan.FromSeconds(3), settings.ReconnectInterval);
		Assert.Equal(TimeSpan.FromSeconds(20), options.KeepAlivePeriod);
		Assert.True(options.CleanSession);
		Assert.Equal(MqttProtocolVersion.V500, options.ProtocolVersion);

		var factory = new MqttQueueFactory();
		using var queue = factory.Create(settings);
		var mqtt = Assert.IsType<MqttQueue>(queue);
		Assert.False(mqtt.Features.Contains(MessageQueueFeature.Delay.Name));
		Assert.True(mqtt.Features.Contains(MessageQueueFeature.Compression.Name));
	}

	[Fact]
	public void CreateQueueWithConnectionString()
	{
		var factory = new MqttQueueFactory();
		using var queue = factory.Create("MQTT", "server=127.0.0.1:5101;client=Factory-ConnectionString;");

		var mqtt = Assert.IsType<MqttQueue>(queue);
		Assert.Equal("MQTT", mqtt.Name);
		Assert.Equal("127.0.0.1:5101", mqtt.Settings.Server);
		Assert.Equal("Factory-ConnectionString", mqtt.Settings.Client);
		Assert.Equal(TimeSpan.FromSeconds(2), mqtt.Settings.ReconnectInterval);
	}

	[Fact]
	public void CreateQueueWithInvalidReconnectInterval()
	{
		var settings = Configuration.MqttConnectionSettingsDriver.Instance.GetSettings("Mqtt",
			"server=127.0.0.1:5101;client=Factory-Invalid;reconnectInterval=0s;");

		Assert.Throws<ArgumentOutOfRangeException>(() => new MqttQueue("MQTT", settings));
	}
}
