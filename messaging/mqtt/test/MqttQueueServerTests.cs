using System;
using System.Net;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using MQTTnet;
using MQTTnet.Server;

using Zongsoft.Components;
using Zongsoft.Communication;
using Zongsoft.Configuration;

using Xunit;

namespace Zongsoft.Messaging.Mqtt.Tests;

public class MqttQueueServerTests
{
	[Fact]
	public void ServerUrlDefaultsToWildcardTcp()
	{
		using var server = new MqttQueueServer();
		Assert.Equal("tcp://0.0.0.0:1883/", server.Url);
	}

	[Theory]
	[InlineData("tcp://127.0.0.1:1", "tcp://127.0.0.1:1/")]
	[InlineData("tcp://127.0.0.1:65535/", "tcp://127.0.0.1:65535/")]
	[InlineData("TCP://*:1883", "tcp://0.0.0.0:1883/")]
	[InlineData("tcp://0.0.0.0:1883", "tcp://0.0.0.0:1883/")]
	[InlineData("tcp://[0:0:0:0:0:0:0:1]:1883", "tcp://[::1]:1883/")]
	[InlineData("tcp://[::]:1883", "tcp://[::]:1883/")]
	[InlineData("tcp://[fe80::1%253]:1883", "tcp://[fe80::1%253]:1883/")]
	public void ServerUrlAcceptsExplicitTcpAddresses(string url, string expected)
	{
		using var server = new MqttQueueServer { Url = url };
		Assert.Equal(expected, server.Url);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("tcp://localhost:1883")]
	[InlineData("tcp://mqtt.example:1883")]
	[InlineData("mqtt://127.0.0.1:1883")]
	[InlineData("udp://127.0.0.1:1883")]
	[InlineData("http://127.0.0.1:1883")]
	[InlineData("tcp://127.0.0.1")]
	[InlineData("tcp://127.0.0.1:0")]
	[InlineData("tcp://127.0.0.1:65536")]
	[InlineData("tcp://127.0.0.1:1883/mqtt")]
	[InlineData("tcp://127.0.0.1:1883/?key=value")]
	[InlineData("tcp://127.0.0.1:1883/#fragment")]
	[InlineData("tcp://user@127.0.0.1:1883")]
	[InlineData("tcp://255.255.255.255:1883")]
	[InlineData("tcp://::1:1883")]
	public void ServerUrlRejectsInvalidListenerAddresses(string url)
	{
		using var server = new MqttQueueServer { Url = "tcp://127.0.0.1:1884" };
		Assert.Throws<ArgumentException>("value", () => server.Url = url);
		Assert.Equal("tcp://127.0.0.1:1884/", server.Url);
	}

	[Fact]
	public async Task ServerBindsOnlyConfiguredIpv4Address()
	{
		if(!Global.IsTestingEnabled)
			return;

		var port = MqttTestUtility.GetFreePort();
		using var server = new MqttQueueServer { Url = $"tcp://127.0.0.1:{port}" };
		await server.StartAsync([]);

		Assert.Equal(WorkerState.Running, server.State);
		Assert.True(MqttTestUtility.CanConnect(port));
		Assert.False(MqttTestUtility.CanBind(port, IPAddress.Loopback));
		Assert.True(MqttTestUtility.CanBind(port, IPAddress.Parse("127.0.0.2")));
		Assert.True(MqttTestUtility.CanBind(port, IPAddress.IPv6Loopback));

		await server.StopAsync([]);
		Assert.True(MqttTestUtility.CanBind(port));
	}

	[Theory]
	[InlineData("::1")]
	[InlineData("::")]
	public async Task ServerBindsOnlyConfiguredIpv6Address(string host)
	{
		if(!Global.IsTestingEnabled)
			return;

		var port = MqttTestUtility.GetFreePort(IPAddress.IPv6Loopback);
		using var server = new MqttQueueServer { Url = $"tcp://[{host}]:{port}" };
		await server.StartAsync([]);

		Assert.Equal(WorkerState.Running, server.State);
		Assert.True(MqttTestUtility.CanConnect(port, IPAddress.IPv6Loopback));
		Assert.False(MqttTestUtility.CanBind(port, IPAddress.Parse(host)));
		Assert.False(MqttTestUtility.CanConnect(port, IPAddress.Loopback));
		Assert.True(MqttTestUtility.CanBind(port, IPAddress.Loopback));

		await server.StopAsync([]);
		Assert.True(MqttTestUtility.CanBind(port, IPAddress.IPv6Loopback));
	}

	[Fact]
	public async Task DisabledServerDoesNotListen()
	{
		if(!Global.IsTestingEnabled)
			return;

		var port = MqttTestUtility.GetFreePort();
		using var server = new TestMqttQueueServer { Enabled = false, Url = $"tcp://127.0.0.1:{port}" };
		await server.StartAsync([]);

		Assert.Equal(WorkerState.Stopped, server.State);
		Assert.False(server.HasServer);
		Assert.True(MqttTestUtility.CanBind(port));
	}

	[Fact]
	public async Task PrecanceledStartReleasesListenerAndAllowsRetry()
	{
		if(!Global.IsTestingEnabled)
			return;

		var port = MqttTestUtility.GetFreePort();
		using var server = new TestMqttQueueServer { Url = $"tcp://127.0.0.1:{port}" };
		using var cancellation = new CancellationTokenSource();
		var states = new List<WorkerStateChangedEventArgs>();
		server.StateChanged += (_, args) => states.Add(args);
		cancellation.Cancel();
		var failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => server.StartAsync([], cancellation.Token));

		Assert.Empty(states);
		Assert.Equal(cancellation.Token, failure.CancellationToken);
		Assert.Equal(WorkerState.Stopped, server.State);
		Assert.False(server.HasServer);
		Assert.Empty(server.Channels);
		Assert.Empty(server.Sessions);
		Assert.True(MqttTestUtility.CanBind(port));

		await server.StartAsync([]);
		Assert.Equal(WorkerState.Running, server.State);
		Assert.True(MqttTestUtility.CanConnect(port));
		await server.StopAsync([]);
		Assert.True(MqttTestUtility.CanBind(port));
	}

	[Fact]
	public async Task CanceledStopKeepsListenerUntilStopped()
	{
		if(!Global.IsTestingEnabled)
			return;

		var port = MqttTestUtility.GetFreePort();
		using var server = new TestMqttQueueServer { Url = $"tcp://127.0.0.1:{port}" };
		await server.StartAsync([]);
		using var cancellation = new CancellationTokenSource();
		WorkerStateChangedEventArgs failure = null;
		server.StateChanged += (_, args) => failure = args;
		cancellation.Cancel();
		var canceled = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => server.StopAsync([], cancellation.Token));

		Assert.Null(failure);
		Assert.Equal(cancellation.Token, canceled.CancellationToken);
		Assert.Equal(WorkerState.Running, server.State);
		Assert.True(server.HasServer);
		Assert.True(MqttTestUtility.CanConnect(port));

		await server.StopAsync([]);
		Assert.Equal(WorkerState.Stopped, server.State);
		Assert.False(server.HasServer);
		Assert.True(MqttTestUtility.CanBind(port));
	}

	[Fact]
	public void QueueServerDisposesOwnedSynchronousStorageOnce()
	{
		var storage = new SynchronousStorage();
		var server = new MqttQueueServer { Storage = storage };

		((IDisposable)server).Dispose();
		((IDisposable)server).Dispose();

		Assert.Equal(1, storage.DisposeCount);
	}

	[Fact]
	public void QueueServerDisposesOwnedAsynchronousStorageOnce()
	{
		var storage = new AsynchronousStorage();
		var server = new MqttQueueServer { Storage = storage };

		((IDisposable)server).Dispose();
		((IDisposable)server).Dispose();

		Assert.Equal(1, storage.AsyncDisposeCount);
		Assert.Equal(0, storage.DisposeCount);
	}

	[Fact]
	public void QueueServerOwnsAndDisposesAssignedStorage()
	{
		var storage = new AsynchronousStorage();
		var server = new MqttQueueServer { Storage = storage };

		((IDisposable)server).Dispose();

		Assert.Equal(1, storage.AsyncDisposeCount);
		Assert.Equal(0, storage.DisposeCount);
	}

	[Fact]
	public async Task QueueServerStopDoesNotDisposeStorageAndAllowsRestart()
	{
		if(!Global.IsTestingEnabled)
			return;

		var storage = new AsynchronousStorage();
		var server = new MqttQueueServer { Url = $"tcp://127.0.0.1:{MqttTestUtility.GetFreePort()}", Storage = storage };

		try
		{
			await server.StartAsync([]);
			await server.StopAsync([]);
			Assert.Equal(0, storage.AsyncDisposeCount);

			await server.StartAsync([]);
			await server.StopAsync([]);
			Assert.Equal(0, storage.AsyncDisposeCount);
		}
		finally
		{
			((IDisposable)server).Dispose();
		}

		Assert.Equal(1, storage.AsyncDisposeCount);
		Assert.Equal(0, storage.DisposeCount);
	}

	[Fact]
	public async Task ServerStorageCanChangeOnlyWhileStopped()
	{
		if(!Global.IsTestingEnabled)
			return;

		using var server = new MqttQueueServer { Url = $"tcp://127.0.0.1:{MqttTestUtility.GetFreePort()}" };
		var first = new TestMessageStorage();
		server.Storage = first;
		Assert.Same(first, server.Storage);

		await server.StartAsync([]);
		Assert.Throws<InvalidOperationException>(() => server.Storage = new TestMessageStorage());

		await server.StopAsync([]);
		var second = new TestMessageStorage();
		server.Storage = second;
		Assert.Same(second, server.Storage);
	}

	[Fact]
	public async Task ServerStopReleasesPortAndAllowsRestart()
	{
		if(!Global.IsTestingEnabled)
			return;

		var port = MqttTestUtility.GetFreePort();
		using var server = new TestMqttQueueServer { Url = $"tcp://127.0.0.1:{port}" };

		await server.StartAsync([]);
		Assert.Equal(WorkerState.Running, server.State);
		Assert.True(server.HasServer);
		Assert.True(await MqttTestUtility.WaitUntilAsync(() => MqttTestUtility.CanConnect(port), TimeSpan.FromSeconds(5)));
		Assert.Throws<InvalidOperationException>(() => server.Url = $"tcp://127.0.0.1:{MqttTestUtility.GetFreePort()}");

		await server.StopAsync([]);
		Assert.Equal(WorkerState.Stopped, server.State);
		Assert.False(server.HasServer);
		Assert.True(await MqttTestUtility.WaitUntilAsync(() => MqttTestUtility.CanBind(port), TimeSpan.FromSeconds(5)));

		await server.StartAsync([]);
		Assert.Equal(WorkerState.Running, server.State);
		Assert.True(await MqttTestUtility.WaitUntilAsync(() => MqttTestUtility.CanConnect(port), TimeSpan.FromSeconds(5)));

		await server.StopAsync([]);
		Assert.True(await MqttTestUtility.WaitUntilAsync(() => MqttTestUtility.CanBind(port), TimeSpan.FromSeconds(5)));
		server.Url = "tcp://127.0.0.1:1884";
		Assert.Equal("tcp://127.0.0.1:1884/", server.Url);
	}

	[Fact]
	public async Task ServerStartFailureReleasesInstance()
	{
		if(!Global.IsTestingEnabled)
			return;

		var port = MqttTestUtility.GetFreePort();
		using var blocker = new TcpListener(IPAddress.Any, port);
		using var server = new TestMqttQueueServer { Url = $"tcp://127.0.0.1:{port}" };
		WorkerStateChangedEventArgs failure = null;
		server.StateChanged += (_, args) => failure = args;
		blocker.Server.ExclusiveAddressUse = true;
		blocker.Start();

		await Assert.ThrowsAnyAsync<Exception>(() => server.StartAsync([]));

		Assert.Equal(WorkerState.Stopped, server.State);
		Assert.False(server.HasServer);
		Assert.Equal("Start", failure.ActionName);
		Assert.Equal(WorkerState.Stopped, failure.State);
		Assert.NotNull(failure.Exception);
		Assert.Empty(server.Channels);
		Assert.Empty(server.Sessions);

		blocker.Stop();
		await server.StartAsync([]);
		Assert.Equal(WorkerState.Running, server.State);
		Assert.True(server.HasServer);
		Assert.True(MqttTestUtility.CanConnect(port));
		await server.StopAsync([]);
		Assert.True(MqttTestUtility.CanBind(port));
	}

	[Fact]
	public async Task ServerExposesChannelsSessionsAndRetainedMessages()
	{
		if(!Global.IsTestingEnabled)
			return;

		var port = MqttTestUtility.GetFreePort();
		using var server = new TestMqttQueueServer { Url = $"tcp://127.0.0.1:{port}" };
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
		using var server = new MqttQueueServer { Url = $"tcp://127.0.0.1:{port}" };
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
		using var server = new MqttQueueServer { Url = $"tcp://127.0.0.1:{port}" };
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

	private class TestMessageStorage : IMessageStorage
	{
		public string Name => "test";

		public ValueTask<int> ClearAsync(CancellationToken cancellation = default) => ValueTask.FromResult(0);
		public ValueTask<int> ClearAsync(string topic, CancellationToken cancellation = default) => ValueTask.FromResult(0);
		public ValueTask SetAsync(Message message, TimeSpan expiry = default, CancellationToken cancellation = default) => ValueTask.CompletedTask;
		public ValueTask<bool> RemoveAsync(string identifier, CancellationToken cancellation = default) => ValueTask.FromResult(false);

		public async IAsyncEnumerable<Message> GetAsync([EnumeratorCancellation] CancellationToken cancellation = default)
		{
			await Task.CompletedTask;
			yield break;
		}

		public async IAsyncEnumerable<Message> GetAsync(string topic, [EnumeratorCancellation] CancellationToken cancellation = default)
		{
			await Task.CompletedTask;
			yield break;
		}
	}

	private sealed class SynchronousStorage : TestMessageStorage, IDisposable
	{
		private int _disposeCount;
		public int DisposeCount => _disposeCount;
		public void Dispose() => Interlocked.Increment(ref _disposeCount);
	}

	private sealed class AsynchronousStorage : TestMessageStorage, IDisposable, IAsyncDisposable
	{
		private int _disposeCount;
		private int _asyncDisposeCount;

		public int DisposeCount => _disposeCount;
		public int AsyncDisposeCount => _asyncDisposeCount;
		public void Dispose() => Interlocked.Increment(ref _disposeCount);
		public ValueTask DisposeAsync()
		{
			Interlocked.Increment(ref _asyncDisposeCount);
			return ValueTask.CompletedTask;
		}
	}
}
