using System;
using System.Net;
using System.Text;
using System.Net.Sockets;
using System.Threading.Tasks;

using MQTTnet;
using MQTTnet.Server;

using Zongsoft.Components;
using Zongsoft.Communication;

using Xunit;

namespace Zongsoft.Messaging.Mqtt.Tests;

public class MqttQueueServerTests
{
	[Fact]
	public async Task ServerStopReleasesPortAndAllowsRestart()
	{
		if(!Global.IsTestingEnabled)
			return;

		var port = MqttTestUtility.GetFreePort();
		using var server = new TestMqttQueueServer { Port = port };

		await server.StartAsync([]);
		Assert.Equal(WorkerState.Running, server.State);
		Assert.True(server.HasServer);
		Assert.True(await MqttTestUtility.WaitUntilAsync(() => MqttTestUtility.CanConnect(port), TimeSpan.FromSeconds(5)));
		Assert.Throws<InvalidOperationException>(() => server.Port = MqttTestUtility.GetFreePort());

		await server.StopAsync([]);
		Assert.Equal(WorkerState.Stopped, server.State);
		Assert.False(server.HasServer);
		Assert.True(await MqttTestUtility.WaitUntilAsync(() => MqttTestUtility.CanBind(port), TimeSpan.FromSeconds(5)));

		await server.StartAsync([]);
		Assert.Equal(WorkerState.Running, server.State);
		Assert.True(await MqttTestUtility.WaitUntilAsync(() => MqttTestUtility.CanConnect(port), TimeSpan.FromSeconds(5)));

		await server.StopAsync([]);
		Assert.True(await MqttTestUtility.WaitUntilAsync(() => MqttTestUtility.CanBind(port), TimeSpan.FromSeconds(5)));
	}

	[Fact]
	public async Task ServerStartFailureReleasesInstance()
	{
		if(!Global.IsTestingEnabled)
			return;

		var port = MqttTestUtility.GetFreePort();
		using var blocker = new TcpListener(IPAddress.Any, port);
		using var server = new TestMqttQueueServer { Port = port };
		blocker.Start();

		await server.StartAsync([]);

		Assert.Equal(WorkerState.Stopped, server.State);
		Assert.False(server.HasServer);
	}

	[Fact]
	public async Task ServerExposesChannelsSessionsAndRetainedMessages()
	{
		if(!Global.IsTestingEnabled)
			return;

		var port = MqttTestUtility.GetFreePort();
		using var server = new TestMqttQueueServer { Port = port };
		var channels = server.Channels;
		var sessions = server.Sessions;

		Assert.Empty(channels);
		Assert.Empty(sessions);
		Assert.True((await server.GetRetainedMessageAsync()).IsEmpty);
		Assert.Empty(await server.GetRetainedMessagesAsync());

		await server.StartAsync([]);
		using var queue = MqttTestUtility.CreateQueue(port, "server-status");
		await queue.ProduceAsync($"tests/status/{Guid.NewGuid():N}", Encoding.UTF8.GetBytes("status"));

		Assert.True(await MqttTestUtility.WaitUntilAsync(
			() => server.Channels.Contains(queue.Settings.Client), TimeSpan.FromSeconds(5)));

		var channel = channels[queue.Settings.Client];

		Assert.Equal(queue.Settings.Client, channel.Identifier);
		Assert.NotNull(channel.Address);
		Assert.NotNull(channel.Session);
		Assert.Equal(channel.Identifier, channel.Session.Identifier);

		Assert.True(sessions.Contains(queue.Settings.Client));
		Assert.Equal(queue.Settings.Client, sessions[queue.Settings.Client].Identifier);

		var topic = $"tests/retained/{Guid.NewGuid():N}";
		var payload = Encoding.UTF8.GetBytes("Retained MQTT message");
		var retained = new MqttApplicationMessageBuilder()
			.WithTopic(topic)
			.WithPayloadSegment(payload)
			.WithRetainFlag()
			.Build();

		await server.UpdateRetainedMessageAsync(retained);

		var message = await server.GetRetainedMessageAsync(topic);
		Assert.Equal(topic, message.Topic);
		Assert.Equal(payload, message.Data);
		Assert.True((await server.GetRetainedMessageAsync(topic + "/missing")).IsEmpty);

		var messages = await server.GetRetainedMessagesAsync();
		var retainedMessage = Assert.Single(messages);
		Assert.Equal(topic, retainedMessage.Topic);
		Assert.Equal(payload, retainedMessage.Data);

		var abstraction = Assert.IsAssignableFrom<Zongsoft.Communication.IChannel>(channel);
		var closed = false;
		abstraction.Closed += (_, _) => closed = true;

		await abstraction.CloseAsync();
		Assert.True(closed);
		Assert.True(abstraction.IsClosed);

		await abstraction.DisposeAsync();
		Assert.True(abstraction.IsDisposed);

		queue.Dispose();
		Assert.True(await MqttTestUtility.WaitUntilAsync(
			() => !channels.Contains(channel.Identifier) && !sessions.Contains(channel.Identifier), TimeSpan.FromSeconds(5)));
		Assert.Same(channels, server.Channels);
		Assert.Same(sessions, server.Sessions);
	}

	[Fact]
	public async Task ServerSessionCanBeAbandoned()
	{
		if(!Global.IsTestingEnabled)
			return;

		var port = MqttTestUtility.GetFreePort();
		var client = $"session-abandon-{Guid.NewGuid():N}";
		using var server = new MqttQueueServer { Port = port };
		await server.StartAsync([]);

		var settings = Configuration.MqttConnectionSettingsDriver.Instance.GetSettings("Mqtt",
			$"server=127.0.0.1:{port};client={client};timeout=5s;reconnectInterval=1m;keepAlive=2s;cleanSession=true;");
		var queue = new MqttQueue("MQTT", settings);

		try
		{
			await queue.ProduceAsync($"tests/session/{Guid.NewGuid():N}", Encoding.UTF8.GetBytes("session"));

			Assert.True(await MqttTestUtility.WaitUntilAsync(
				() => server.Sessions.Contains(client), TimeSpan.FromSeconds(5)));

			var sessions = server.Sessions;
			await sessions[client].AbandonAsync();

			Assert.True(await MqttTestUtility.WaitUntilAsync(
				() => !server.Sessions.Contains(client), TimeSpan.FromSeconds(5)));
		}
		finally
		{
			queue.Dispose();
		}
	}

	[Fact]
	public async Task ServerImplementsListenerAndHandlesPublishedMessages()
	{
		if(!Global.IsTestingEnabled)
			return;

		var port = MqttTestUtility.GetFreePort();
		using var server = new MqttQueueServer { Port = port };
		var listener = Assert.IsAssignableFrom<IListener<Message>>(server);
		var messages = new MqttMessageBuffer();
		server.Handler = messages;

		await server.StartAsync([]);
		Assert.True(listener.IsListening);

		using var queue = MqttTestUtility.CreateQueue(port, "server-listener");
		var topic = $"tests/listener/{Guid.NewGuid():N}";
		var payload = Encoding.UTF8.GetBytes("listener");
		await queue.ProduceAsync(topic, payload);

		var message = await messages.ReceiveAsync(TimeSpan.FromSeconds(5));
		Assert.Equal(topic, message.Topic);
		Assert.Equal(payload, message.Data);
		Assert.Equal(queue.Settings.Client, message.Identity);

		await server.StopAsync([]);
		Assert.False(listener.IsListening);
	}

	private sealed class TestMqttQueueServer : MqttQueueServer
	{
		public bool HasServer => this.Server != null;
		public Task UpdateRetainedMessageAsync(MqttApplicationMessage message) => this.Server.UpdateRetainedMessageAsync(message);
	}
}
