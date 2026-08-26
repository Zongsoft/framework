using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using NetMQ.Sockets;

using Xunit;

using Zongsoft.Configuration;

namespace Zongsoft.Messaging.ZeroMQ.Tests;

public class ZeroQueueServerTests
{
	[Fact]
	public void QueueServerDisposesOwnedSynchronousStorageOnce()
	{
		var storage = new SynchronousStorage(true);
		var server = new ZeroQueueServer { Storage = storage };

		((IDisposable)server).Dispose();
		((IDisposable)server).Dispose();

		Assert.Equal(1, storage.DisposeCount);
	}

	[Fact]
	public void QueueServerDisposesOwnedAsynchronousStorageOnce()
	{
		var storage = new AsynchronousStorage(true);
		var server = new ZeroQueueServer { Storage = storage };

		((IDisposable)server).Dispose();
		((IDisposable)server).Dispose();

		Assert.Equal(1, storage.AsyncDisposeCount);
		Assert.Equal(0, storage.DisposeCount);
	}

	[Fact]
	public void QueueServerDoesNotDisposeExternalStorage()
	{
		var storage = new AsynchronousStorage(false);
		var server = new ZeroQueueServer { Storage = storage };

		((IDisposable)server).Dispose();

		Assert.Equal(0, storage.AsyncDisposeCount);
		Assert.Equal(0, storage.DisposeCount);
	}

	[Fact]
	public async Task QueueServerStopDoesNotDisposeStorageAndAllowsRestart()
	{
		if(!Global.IsTestingEnabled)
			return;

		var storage = new AsynchronousStorage(true);
		var server = new ZeroQueueServer { Port = ZeroTestUtility.GetFreePort(), Storage = storage };

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
	public async Task QueueServerStopReleasesPortAndAllowsRestart()
	{
		if(!Global.IsTestingEnabled)
			return;

		var port = ZeroTestUtility.GetFreePort();
		var server = new ZeroQueueServer { Port = port, Storage = new MemoryMessageStorage() };

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
	public async Task QueueServerWithoutStorageDoesNotStartControlChannel()
	{
		if(!Global.IsTestingEnabled)
			return;

		var port = ZeroTestUtility.GetFreePort();
		var control = ZeroTestUtility.GetFreePort();
		var server = new ZeroQueueServer { Port = port };

		try
		{
			await server.StartAsync([$"--control:{control}"]);
			Assert.Equal(0, ZeroTestUtility.GetServerPorts(port).Control);
			Assert.True(ZeroTestUtility.CanBindZeroMq(control));
			Assert.Throws<InvalidOperationException>(() => server.Storage = new MemoryMessageStorage());

			await server.StopAsync([]);
			server.Storage = new MemoryMessageStorage();
			await server.StartAsync([$"--control:{control}"]);
			Assert.Equal(control, ZeroTestUtility.GetServerPorts(port).Control);
			Assert.False(ZeroTestUtility.CanBindZeroMq(control));
		}
		finally
		{
			await server.StopAsync([]);
			((IDisposable)server).Dispose();
		}
	}

	[Fact]
	public async Task QueueServerStartWithExplicitExchangePortsBindsAndForwardsMessages()
	{
		if(!Global.IsTestingEnabled)
			return;

		var port = ZeroTestUtility.GetFreePort();
		var incoming = ZeroTestUtility.GetFreePort();
		var outgoing = ZeroTestUtility.GetFreePort();
		var control = ZeroTestUtility.GetFreePort();
		var server = new ZeroQueueServer { Port = port, Storage = new MemoryMessageStorage() };

		try
		{
			await server.StartAsync([$"--incoming:{incoming}", $"--outgoing:{outgoing}", $"--control:{control}"]);

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
		var server = new ZeroQueueServer { Port = port };

		using var blocker = new ResponseSocket();
		blocker.Bind($"tcp://*:{port}");

		try
		{
			await server.StartAsync([]);

			Assert.Equal(Zongsoft.Components.WorkerState.Stopped, server.State);
		}
		finally
		{
			((IDisposable)server).Dispose();
		}
	}

	private abstract class TestStorage(bool disposable) : IMessageStorage
	{
		public string Name => "test";
		public bool Disposable { get; } = disposable;
		public IConnectionSettings Settings { get; set; } = new ConnectionSettings();

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

	private sealed class SynchronousStorage(bool disposable) : TestStorage(disposable), IDisposable
	{
		private int _disposeCount;
		public int DisposeCount => _disposeCount;
		public void Dispose() => Interlocked.Increment(ref _disposeCount);
	}

	private sealed class AsynchronousStorage(bool disposable) : TestStorage(disposable), IDisposable, IAsyncDisposable
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
