using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using NetMQ;
using NetMQ.Sockets;

using Xunit;

namespace Zongsoft.Messaging.ZeroMQ.Tests;

public class ZeroQueueServerTests
{
	[Fact]
	public async Task QueueServerDisposesOwnedSynchronousStorageOnce()
	{
		if(!Global.IsTestingEnabled)
			return;

		var storage = new SynchronousStorage();
		var factory = new TestMessageStorageFactory(storage);
		var server = ZeroTestUtility.CreateServer(storages: factory);
		await server.StartAsync([]);
		await server.StopAsync([]);

		((IDisposable)server).Dispose();
		((IDisposable)server).Dispose();

		Assert.Equal(1, storage.DisposeCount);
		Assert.Equal(1, factory.Count);
		Assert.Equal("test-store", factory.LastName);
	}

	[Fact]
	public async Task QueueServerDisposesOwnedAsynchronousStorageOnce()
	{
		if(!Global.IsTestingEnabled)
			return;

		var storage = new AsynchronousStorage();
		var server = ZeroTestUtility.CreateServer(storages: new TestMessageStorageFactory(storage));
		await server.StartAsync([]);
		await server.StopAsync([]);

		((IDisposable)server).Dispose();
		((IDisposable)server).Dispose();

		Assert.Equal(1, storage.AsyncDisposeCount);
		Assert.Equal(0, storage.DisposeCount);
	}

	[Fact]
	public void QueueServerDoesNotCreateStorageBeforeStart()
	{
		var storage = new AsynchronousStorage();
		var factory = new TestMessageStorageFactory(storage);
		var server = new ZeroQueueServer { Storages = factory };

		((IDisposable)server).Dispose();

		Assert.Equal(0, factory.Count);
		Assert.Equal(0, storage.AsyncDisposeCount);
		Assert.Equal(0, storage.DisposeCount);
	}

	[Fact]
	public async Task QueueServerStopDoesNotDisposeStorageAndAllowsRestart()
	{
		if(!Global.IsTestingEnabled)
			return;

		var storage = new AsynchronousStorage();
		var factory = new TestMessageStorageFactory(storage);
		var server = ZeroTestUtility.CreateServer(storages: factory);

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
		Assert.Equal(1, factory.Count);
	}

	[Fact]
	public async Task ReplacingFactoryWhileStoppedDisposesActiveStorage()
	{
		if(!Global.IsTestingEnabled)
			return;

		var first = new AsynchronousStorage();
		var second = new AsynchronousStorage();
		var server = ZeroTestUtility.CreateServer(storages: new TestMessageStorageFactory(first));

		try
		{
			await server.StartAsync([]);
			await server.StopAsync([]);
			server.Storages = new TestMessageStorageFactory(second);
			Assert.Equal(1, first.AsyncDisposeCount);

			await server.StartAsync([]);
			await server.StopAsync([]);
			Assert.Equal(0, second.AsyncDisposeCount);
		}
		finally
		{
			((IDisposable)server).Dispose();
		}

		Assert.Equal(1, second.AsyncDisposeCount);
	}

	[Fact]
	public async Task QueueServerStopReleasesPortAndAllowsRestart()
	{
		if(!Global.IsTestingEnabled)
			return;

		var port = ZeroTestUtility.GetFreePort();
		var server = ZeroTestUtility.CreateServer(port, new TestMessageStorageFactory(new MemoryMessageStorage()));

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

	[Fact]
	public async Task QueueServerWithoutControlDoesNotCreateStorage()
	{
		if(!Global.IsTestingEnabled)
			return;

		var port = ZeroTestUtility.GetFreePort();
		var storage = new MemoryMessageStorage();
		var factory = new TestMessageStorageFactory(storage);
		using var server = ZeroTestUtility.CreateServer(port);
		server.Storages = factory;
		await server.StartAsync([]);

		Assert.Equal(Zongsoft.Components.WorkerState.Running, server.State);
		Assert.Equal(0, ZeroTestUtility.GetServerPorts(port).Control);
		Assert.Equal(0, factory.Count);
		Assert.Equal(0, storage.GetCount);
		Assert.Throws<InvalidOperationException>(() => server.Storages = null);
	}

	[Fact]
	public async Task QueueServerStartWithExplicitExchangeAddressesBindsAndForwardsMessages()
	{
		if(!Global.IsTestingEnabled)
			return;

		var port = ZeroTestUtility.GetFreePort();
		var incoming = ZeroTestUtility.GetFreePort();
		var outgoing = ZeroTestUtility.GetFreePort();
		var control = ZeroTestUtility.GetFreePort();
		var server = ZeroTestUtility.CreateServer(port, new TestMessageStorageFactory(new MemoryMessageStorage()));

		try
		{
			server.Incoming = ZeroTestUtility.GetAddress(incoming);
			server.Outgoing = ZeroTestUtility.GetAddress(outgoing);
			server.Control = ZeroTestUtility.GetAddress(control);
			await server.StartAsync([]);

			var ports = ZeroTestUtility.GetServerPorts(port);
			Assert.Equal(control, ports.Control);
			Assert.Equal(incoming, ports.Incoming);
			Assert.Equal(outgoing, ports.Outgoing);
			Assert.False(ZeroTestUtility.CanBindZeroMq(incoming));
			Assert.False(ZeroTestUtility.CanBindZeroMq(outgoing));
			Assert.False(ZeroTestUtility.CanBindZeroMq(control));

			using var publisher = ZeroTestUtility.CreateQueue(port, "publisher");
			using var subscriber = ZeroTestUtility.CreateQueue(port, "subscriber");
			using var handler = new MessageBuffer();

			await subscriber.SubscribeAsync("topic/explicit", handler);
			await ZeroTestUtility.PublishUntilAcceptedAsync(publisher, "topic/explicit", Encoding.UTF8.GetBytes("explicit"));

			var message = await handler.ReceiveAsync(TimeSpan.FromSeconds(5));
			Assert.Equal("explicit", Encoding.UTF8.GetString(message.Data));
		}
		finally
		{
			await server.StopAsync([]);
			((IDisposable)server).Dispose();
		}
	}

	[Fact]
	public async Task QueueServerStartFailureLeavesServerStopped()
	{
		if(!Global.IsTestingEnabled)
			return;

		var port = ZeroTestUtility.GetFreePort();
		var storage = new AsynchronousStorage();
		var factory = new TestMessageStorageFactory(storage);
		var server = ZeroTestUtility.CreateServer(port, factory);

		using var blocker = new ResponseSocket();
		blocker.Bind($"tcp://127.0.0.1:{port}");

		try
		{
			await Assert.ThrowsAsync<AddressAlreadyInUseException>(() => server.StartAsync([]));

			Assert.Equal(Zongsoft.Components.WorkerState.Stopped, server.State);
			Assert.Equal(1, factory.Count);
			Assert.Equal(1, storage.AsyncDisposeCount);
		}
		finally
		{
			((IDisposable)server).Dispose();
		}
	}

	[Fact]
	public async Task StorageNameSelectsAndReplacesOwnedStorageIndependentlyOfBrokerName()
	{
		if(!Global.IsTestingEnabled)
			return;

		var first = new AsynchronousStorage();
		var second = new AsynchronousStorage();
		var factory = new TestMessageStorageFactory(name => name == "first-store" ? first : second);
		using var server = ZeroTestUtility.CreateServer(storages: factory, name: "broker-name", storage: "first-store");
		await server.StartAsync([]);
		Assert.Equal(Zongsoft.Components.WorkerState.Running, server.State);
		Assert.Equal("first-store", factory.LastName);
		Assert.Equal("broker-name", server.Name);
		await server.StopAsync([]);
		Assert.Equal(0, first.AsyncDisposeCount);
		server.Storage = " second-store ";
		Assert.Equal(1, first.AsyncDisposeCount);
		Assert.Equal("second-store", server.Storage);
		await server.StartAsync([]);
		Assert.Equal(Zongsoft.Components.WorkerState.Running, server.State);
		Assert.Equal("second-store", factory.LastName);
		Assert.Equal(2, factory.Count);
		Assert.Equal(0, second.AsyncDisposeCount);
		await server.StopAsync([]);
		((IDisposable)server).Dispose();
		Assert.Equal(1, first.AsyncDisposeCount);
		Assert.Equal(1, second.AsyncDisposeCount);
	}

	[Fact]
	public async Task DisablingControlReleasesCachedStorageAndDoesNotCreateAnother()
	{
		if(!Global.IsTestingEnabled)
			return;

		var storage = new AsynchronousStorage();
		var factory = new TestMessageStorageFactory(storage);
		using var server = ZeroTestUtility.CreateServer(storages: factory);
		await server.StartAsync([]);
		Assert.Equal(Zongsoft.Components.WorkerState.Running, server.State);
		await server.StopAsync([]);
		Assert.Equal(0, storage.AsyncDisposeCount);
		server.Control = null;
		server.Storage = null;
		Assert.Equal(1, storage.AsyncDisposeCount);
		await server.StartAsync([]);

		Assert.Equal(Zongsoft.Components.WorkerState.Running, server.State);
		Assert.Equal(0, ZeroTestUtility.GetServerPorts((ushort)new Uri(server.Discovery).Port).Control);
		Assert.Equal(1, factory.Count);
		Assert.Equal(1, storage.GetCount);
		Assert.Equal(1, storage.AsyncDisposeCount);
	}

	[Theory]
	[InlineData("Incoming")]
	[InlineData("Outgoing")]
	[InlineData("Control")]
	public async Task PartialStartupFailureReleasesSocketsAndStorage(string role)
	{
		if(!Global.IsTestingEnabled)
			return;

		var storage = new AsynchronousStorage();
		var factory = new TestMessageStorageFactory(storage);
		using var server = ZeroTestUtility.CreateServer(storages: factory);
		var blocked = role switch
		{
			"Incoming" => server.Incoming,
			"Outgoing" => server.Outgoing,
			_ => server.Control,
		};
		using var blocker = new ResponseSocket();
		blocker.Bind(blocked);
		Exception failure = null;
		server.StateChanged += (_, args) => failure = args.Exception;
		var exception = await Assert.ThrowsAsync<AddressAlreadyInUseException>(() => server.StartAsync([]));

		Assert.Equal(Zongsoft.Components.WorkerState.Stopped, server.State);
		Assert.Same(exception, failure);
		Assert.Equal(1, factory.Count);
		Assert.Equal(1, storage.AsyncDisposeCount);
		Assert.Equal(0, storage.DisposeCount);
		Assert.True(ZeroTestUtility.CanBindZeroMq((ushort)new Uri(server.Discovery).Port));

		foreach(var address in new[] { server.Incoming, server.Outgoing, server.Control })
		{
			if(address != blocked)
				Assert.True(ZeroTestUtility.CanBindZeroMq((ushort)new Uri(address).Port));
		}
	}

	private abstract class TestStorage : IMessageStorage
	{
		public string Name => "test";
		public int GetCount { get; private set; }

		public ValueTask<int> ClearAsync(CancellationToken cancellation = default) => ValueTask.FromResult(0);
		public ValueTask<int> ClearAsync(string topic, CancellationToken cancellation = default) => ValueTask.FromResult(0);
		public ValueTask SetAsync(Message message, TimeSpan expiry = default, CancellationToken cancellation = default) => ValueTask.CompletedTask;
		public ValueTask<bool> RemoveAsync(string identifier, CancellationToken cancellation = default) => ValueTask.FromResult(false);

		public async IAsyncEnumerable<Message> GetAsync([EnumeratorCancellation] CancellationToken cancellation = default)
		{
			this.GetCount++;
			await Task.CompletedTask;
			yield break;
		}

		public async IAsyncEnumerable<Message> GetAsync(string topic, [EnumeratorCancellation] CancellationToken cancellation = default)
		{
			await Task.CompletedTask;
			yield break;
		}
	}

	private sealed class SynchronousStorage : TestStorage, IDisposable
	{
		private int _disposeCount;
		public int DisposeCount => _disposeCount;
		public void Dispose() => Interlocked.Increment(ref _disposeCount);
	}

	private sealed class AsynchronousStorage : TestStorage, IDisposable, IAsyncDisposable
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
