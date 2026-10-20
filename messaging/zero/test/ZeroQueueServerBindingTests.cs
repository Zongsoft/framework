/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@qq.com>
 *
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Messaging.ZeroMQ library.
 *
 * The Zongsoft.Messaging.ZeroMQ is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Messaging.ZeroMQ is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Messaging.ZeroMQ library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Net;
using System.Text;
using System.Net.Sockets;
using System.Threading.Tasks;

using NetMQ;
using NetMQ.Sockets;

using Zongsoft.Components;

using Xunit;

namespace Zongsoft.Messaging.ZeroMQ.Tests;

public class ZeroQueueServerBindingTests
{
	[Theory]
	[InlineData("TCP://127.0.0.1:4317/", "tcp://127.0.0.1:4317")]
	[InlineData("tcp://*:4317", "tcp://0.0.0.0:4317")]
	[InlineData("tcp://[::1]:4317/", "tcp://[::1]:4317")]
	[InlineData("tcp://[::]:65535", "tcp://[::]:65535")]
	public async Task AddressesAreNormalized(string text, string expected)
	{
		await using var server = new ZeroQueueServer();
		foreach(var role in new[] { "Discovery", "Control", "Incoming", "Outgoing" })
		{
			SetAddress(server, role, text);
			Assert.Equal(expected, GetAddress(server, role));
		}
	}

	[Theory]
	[InlineData("http://127.0.0.1:4317")]
	[InlineData("udp://127.0.0.1:4317")]
	[InlineData("tcp://localhost:4317")]
	[InlineData("tcp://127.0.0.1:0")]
	[InlineData("tcp://127.0.0.1:65536")]
	[InlineData("tcp://127.0.0.1")]
	[InlineData("tcp://127.0.0.1:4317/path")]
	[InlineData("tcp://127.0.0.1:4317?mode=server")]
	[InlineData("tcp://127.0.0.1:4317#server")]
	[InlineData("127.0.0.1:4317")]
	[InlineData("tcp://user@127.0.0.1:4317")]
	public async Task InvalidAddressesDoNotReplaceExistingAddresses(string text)
	{
		await using var server = new ZeroQueueServer();
		foreach(var role in new[] { "Discovery", "Control", "Incoming", "Outgoing" })
		{
			var previous = GetAddress(server, role);
			Assert.Throws<ArgumentException>(() => SetAddress(server, role, text));
			Assert.Equal(previous, GetAddress(server, role));
		}
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData(" ")]
	public async Task RequiredAddressesRejectEmptyAndControlNormalizesEmpty(string text)
	{
		await using var server = new ZeroQueueServer { Control = "tcp://127.0.0.1:4317" };
		foreach(var role in new[] { "Discovery", "Incoming", "Outgoing" })
			Assert.Throws<ArgumentException>(() => SetAddress(server, role, text));

		server.Control = text;
		Assert.Null(server.Control);
	}

	[Theory]
	[InlineData("Discovery", "Incoming")]
	[InlineData("Discovery", "Outgoing")]
	[InlineData("Discovery", "Control")]
	[InlineData("Incoming", "Outgoing")]
	[InlineData("Incoming", "Control")]
	[InlineData("Outgoing", "Control")]
	public async Task DuplicateRolePortsFailBeforeStorageCreation(string first, string second)
	{
		if(!Global.IsTestingEnabled)
			return;

		var factory = new TestMessageStorageFactory(new MemoryMessageStorage());
		await using var server = ZeroTestUtility.CreateServer(storages: factory);
		SetAddress(server, second, $"tcp://127.0.0.2:{new Uri(GetAddress(server, first)).Port}");
		Exception failure = null;
		server.StateChanged += (_, args) => failure = args.Exception;
		var exception = await Assert.ThrowsAsync<ArgumentException>(() => server.StartAsync([]));

		Assert.Equal(WorkerState.Stopped, server.State);
		Assert.Same(exception, failure);
		Assert.Equal(0, factory.Count);
	}

	[Theory]
	[InlineData(true, false, true)]
	[InlineData(false, true, true)]
	[InlineData(true, true, false)]
	public async Task InvalidControlStorageConfigurationDoesNotOpenListener(bool control, bool storage, bool factoryAvailable)
	{
		if(!Global.IsTestingEnabled)
			return;

		var factory = new TestMessageStorageFactory(new MemoryMessageStorage());
		await using var server = ZeroTestUtility.CreateServer(storages: factory);
		server.Control = control ? server.Control : null;
		server.Storage = storage ? "pending-store" : null;
		server.Storages = factoryAvailable ? factory : null;
		Exception failure = null;
		server.StateChanged += (_, args) => failure = args.Exception;
		var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => server.StartAsync([]));

		Assert.Equal(WorkerState.Stopped, server.State);
		Assert.Same(exception, failure);
		Assert.Equal(0, factory.Count);
		Assert.True(ZeroTestUtility.CanBindZeroMq((ushort)new Uri(server.Discovery).Port));
	}

	[Fact]
	public async Task MissingStorageInstanceDoesNotOpenListener()
	{
		if(!Global.IsTestingEnabled)
			return;

		var factory = new TestMessageStorageFactory(_ => null);
		await using var server = ZeroTestUtility.CreateServer(storages: factory, name: "broker", storage: "missing-store");
		Exception failure = null;
		server.StateChanged += (_, args) => failure = args.Exception;
		var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => server.StartAsync([]));

		Assert.Equal(WorkerState.Stopped, server.State);
		Assert.Same(exception, failure);
		Assert.Equal("missing-store", factory.LastName);
		Assert.Equal(1, factory.Count);
		Assert.True(ZeroTestUtility.CanBindZeroMq((ushort)new Uri(server.Discovery).Port));
	}

	[Fact]
	public async Task StartArgumentsDoNotOverrideAddresses()
	{
		if(!Global.IsTestingEnabled)
			return;

		await using var server = ZeroTestUtility.CreateServer();
		var discovery = (ushort)new Uri(server.Discovery).Port;
		await server.StartAsync(["--incoming:1", "--outgoing:2", "--control:3"]);
		var ports = ZeroTestUtility.GetServerPorts(discovery);

		Assert.Equal(WorkerState.Running, server.State);
		Assert.Equal(new Uri(server.Incoming).Port, ports.Incoming);
		Assert.Equal(new Uri(server.Outgoing).Port, ports.Outgoing);
		Assert.Equal(0, ports.Control);
	}

	[Fact]
	public async Task RunningConfigurationIsImmutableAndStoppedAddressesCanChange()
	{
		if(!Global.IsTestingEnabled)
			return;

		await using var server = ZeroTestUtility.CreateServer();
		await server.StartAsync([]);
		Assert.Equal(WorkerState.Running, server.State);

		foreach(var role in new[] { "Discovery", "Control", "Incoming", "Outgoing" })
			Assert.Throws<InvalidOperationException>(() => SetAddress(server, role, "tcp://127.0.0.1:4317"));

		Assert.Throws<InvalidOperationException>(() => server.Storage = "changed");
		Assert.Throws<InvalidOperationException>(() => server.Storages = new TestMessageStorageFactory(new MemoryMessageStorage()));
		await server.StopAsync([]);
		await using var changed = ZeroTestUtility.CreateServer();
		server.Discovery = changed.Discovery;
		server.Incoming = changed.Incoming;
		server.Outgoing = changed.Outgoing;
		await server.StartAsync([]);
		var ports = ZeroTestUtility.GetServerPorts((ushort)new Uri(changed.Discovery).Port);

		Assert.Equal(new Uri(changed.Incoming).Port, ports.Incoming);
		Assert.Equal(new Uri(changed.Outgoing).Port, ports.Outgoing);
	}

	[Fact]
	public async Task IPv4LoopbackBindsExactAddress()
	{
		if(!Global.IsTestingEnabled)
			return;

		await using var server = ZeroTestUtility.CreateServer(storages: new TestMessageStorageFactory(new MemoryMessageStorage()));
		await server.StartAsync([]);
		Assert.Equal(WorkerState.Running, server.State);

		foreach(var role in new[] { server.Discovery, server.Control, server.Incoming, server.Outgoing })
		{
			var port = new Uri(role).Port;
			using var exact = new TcpClient();
			await exact.ConnectAsync(IPAddress.Loopback, port).WaitAsync(TimeSpan.FromSeconds(2));
			Assert.True(exact.Connected);
			using var other = new TcpClient();
			await Assert.ThrowsAsync<SocketException>(() => other.ConnectAsync(IPAddress.Parse("127.0.0.2"), port));
		}
	}

	[Fact]
	public async Task IPv6EndpointsDiscoverAndForwardRawMessages()
	{
		if(!Global.IsTestingEnabled || !Socket.OSSupportsIPv6)
			return;

		await using var server = CreateIPv6Server();
		await server.StartAsync([]);
		Assert.Equal(WorkerState.Running, server.State);
		await Task.Run(() =>
		{
			using var discovery = new RequestSocket();
			discovery.Options.IPv4Only = false;
			discovery.Connect(server.Discovery);
			discovery.SendFrame(Protocol.GetDiscoveryRequest("ipv6-tests"));
			Assert.True(discovery.TryReceiveFrameString(TimeSpan.FromSeconds(5), out var response));
			Assert.True(Protocol.TryParseDiscoveryResponse(response, out _, out var control, out var incoming, out var outgoing));
			Assert.Equal(0, control);
			Assert.Equal(new Uri(server.Incoming).Port, incoming);
			Assert.Equal(new Uri(server.Outgoing).Port, outgoing);

			using var publisher = new XPublisherSocket();
			using var subscriber = new SubscriberSocket();
			publisher.Options.IPv4Only = false;
			subscriber.Options.IPv4Only = false;
			publisher.Connect(server.Incoming);
			subscriber.Connect(server.Outgoing);
			subscriber.Subscribe("topic/ipv6");
			Assert.True(publisher.TryReceiveFrameBytes(TimeSpan.FromSeconds(5), out var subscription));
			Assert.Equal(1, subscription[0]);
			Assert.Equal("topic/ipv6", Encoding.UTF8.GetString(subscription.AsSpan(1)));
			publisher.SendMoreFrame("topic/ipv6").SendFrame("ipv6-message");
			var message = new NetMQMessage();
			Assert.True(subscriber.TryReceiveMultipartMessage(TimeSpan.FromSeconds(5), ref message));
			Assert.Equal(2, message.FrameCount);
			Assert.Equal("topic/ipv6", message[0].ConvertToString());
			Assert.Equal("ipv6-message", message[1].ConvertToString());
		});
	}

	[Theory]
	[InlineData(MessageReliability.MostOnce)]
	[InlineData(MessageReliability.LeastOnce)]
	public async Task IPv6QueueClientsExchangeMessages(MessageReliability reliability)
	{
		if(!Global.IsTestingEnabled || !Socket.OSSupportsIPv6)
			return;

		var storage = new MemoryMessageStorage();
		await using var server = CreateIPv6Server(reliability == MessageReliability.LeastOnce ? new TestMessageStorageFactory(storage) : null);
		await server.StartAsync([]);
		Assert.Equal(WorkerState.Running, server.State);
		var port = (ushort)new Uri(server.Discovery).Port;
		using var publisher = ZeroTestUtility.CreateQueue(port, "ipv6-publisher", settings => settings.Server = "[::1]");
		using var subscriber = ZeroTestUtility.CreateQueue(port, "ipv6-subscriber", settings => settings.Server = "[::1]");
		using var handler = new MessageBuffer();
		await subscriber.SubscribeAsync("topic/ipv6-client", handler, new MessageSubscribeOptions(reliability));
		var identifier = await ZeroTestUtility.PublishUntilAcceptedAsync(publisher, "topic/ipv6-client", Encoding.UTF8.GetBytes("ipv6-client-message"), new MessageEnqueueOptions(reliability));
		var message = await handler.ReceiveAsync(TimeSpan.FromSeconds(5));

		Assert.Equal(identifier, message.Identifier);
		Assert.Equal("topic/ipv6-client", message.Topic);
		Assert.Equal("ipv6-client-message", Encoding.UTF8.GetString(message.Data));

		if(reliability == MessageReliability.LeastOnce)
		{
			Assert.Equal(1, storage.SetCount);
			Assert.Equal(0, storage.RemoveCount);
			await message.AcknowledgeAsync();
			Assert.True(await ZeroTestUtility.WaitUntilAsync(() => storage.RemoveCount == 1, TimeSpan.FromSeconds(5)));
			Assert.Empty(await ZeroTestUtility.CollectAsync(storage.GetAsync()));
		}
	}

	private static ZeroQueueServer CreateIPv6Server(IMessageStorageFactory storages = null)
	{
		var server = ZeroTestUtility.CreateServer(storages: storages);
		server.Discovery = server.Discovery.Replace("127.0.0.1", "[::1]");
		server.Incoming = server.Incoming.Replace("127.0.0.1", "[::1]");
		server.Outgoing = server.Outgoing.Replace("127.0.0.1", "[::1]");
		server.Control = server.Control?.Replace("127.0.0.1", "[::1]");
		return server;
	}

	private static string GetAddress(ZeroQueueServer server, string role) => role switch
	{
		"Discovery" => server.Discovery,
		"Control" => server.Control,
		"Incoming" => server.Incoming,
		"Outgoing" => server.Outgoing,
		_ => throw new ArgumentOutOfRangeException(nameof(role)),
	};

	private static void SetAddress(ZeroQueueServer server, string role, string text)
	{
		switch(role)
		{
			case "Discovery":
				server.Discovery = text;
				break;
			case "Control":
				server.Control = text;
				break;
			case "Incoming":
				server.Incoming = text;
				break;
			case "Outgoing":
				server.Outgoing = text;
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(role));
		}
	}
}
