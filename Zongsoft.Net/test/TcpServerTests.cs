using System;
using System.Net;
using System.Buffers;
using System.Reflection;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.Communication;

namespace Zongsoft.Net.Tests;

public class TcpServerTests
{
	[Fact]
	public void ConstructorRejectsNullPacketizer()
	{
		Assert.Throws<ArgumentNullException>(() => new TcpServer<string>(null));
	}

	[Fact]
	public async Task AcceptAsyncRejectsNullTransport()
	{
		using var server = new TcpServer<string>(new StringPacketizer());

		await Assert.ThrowsAsync<ArgumentNullException>(() => server.AcceptAsync(null, new IPEndPoint(IPAddress.Loopback, 7969)));
	}

	[Fact]
	public async Task EmptyServerBroadcastsToZeroChannels()
	{
		using var server = new TcpServer<string>(new StringPacketizer());

		Assert.Empty(server.Channels);
		Assert.Equal(0, await server.BroadcastAsync("payload"));
		await ((ISender<string>)server).SendAsync("payload");
	}

	[Fact]
	public async Task ChannelManagerRemovesSpecifiedChannelOnly()
	{
		using var server = new TcpServer<string>(new StringPacketizer());
		var manager = server.Channels;
		var add = manager.GetType().GetMethod("Add", BindingFlags.Instance | BindingFlags.NonPublic);
		var remove = manager.GetType().GetMethod("Remove", BindingFlags.Instance | BindingFlags.NonPublic);
		await using var first = new TcpServerChannel<string>(manager, new TestDuplexPipe(), new IPEndPoint(IPAddress.Loopback, 32101));
		await using var second = new TcpServerChannel<string>(manager, new TestDuplexPipe(), new IPEndPoint(IPAddress.Loopback, 32102));

		add.Invoke(manager, [first]);
		add.Invoke(manager, [second]);
		var removed = (bool)remove.Invoke(manager, [first]);

		Assert.True(removed);
		Assert.DoesNotContain(first, manager);
		Assert.Contains(second, manager);
	}

	[Fact]
	public async Task RemovingClosedChannelDoesNotRemoveReplacementAtSameAddress()
	{
		using var server = new TcpServer<string>(new StringPacketizer());
		var manager = server.Channels;
		var add = manager.GetType().GetMethod("Add", BindingFlags.Instance | BindingFlags.NonPublic);
		var remove = manager.GetType().GetMethod("Remove", BindingFlags.Instance | BindingFlags.NonPublic);
		var address = new IPEndPoint(IPAddress.Loopback, 32103);
		await using var closed = new TcpServerChannel<string>(manager, new TestDuplexPipe(), address);
		await using var replacement = new TcpServerChannel<string>(manager, new TestDuplexPipe(), address);

		add.Invoke(manager, [closed]);
		Assert.True((bool)remove.Invoke(manager, [closed]));
		add.Invoke(manager, [replacement]);

		Assert.False((bool)remove.Invoke(manager, [closed]));
		Assert.Same(replacement, Assert.Single(manager));
	}

	[Fact]
	public void ChannelManagerRejectsNullServer()
	{
		Assert.Throws<ArgumentNullException>(() => new TcpServerChannelManager<string>(null));
	}

	[Fact]
	public async Task ChannelIdentityUsesRemoteAddress()
	{
		using var server = new TcpServer<string>(new StringPacketizer());
		var firstTransport = new TestDuplexPipe();
		var secondTransport = new TestDuplexPipe();
		var address = new IPEndPoint(IPAddress.Loopback, 32123);
		await using var first = new TcpServerChannel<string>(server.Channels, firstTransport, address);
		await using var second = new TcpServerChannel<string>(server.Channels, secondTransport, new IPEndPoint(IPAddress.Loopback, 32123));

		Assert.Equal(address, first.Address);
		Assert.Equal(first, second);
		Assert.Equal(first.GetHashCode(), second.GetHashCode());
		Assert.Equal(address.ToString(), first.ToString());
		Assert.False(first.Equals(null));
		Assert.False(first.Equals(new object()));
	}

	private sealed class StringPacketizer : IPacketizer<string>
	{
		public void Pack(IBufferWriter<byte> writer, in string package) { }
		public bool Unpack(ref ReadOnlySequence<byte> data, out string package)
		{
			package = null;
			return false;
		}
	}

	private sealed class TestDuplexPipe : IDuplexPipe
	{
		private readonly Pipe _input = new();
		private readonly Pipe _output = new();

		public PipeReader Input => _input.Reader;
		public PipeWriter Output => _output.Writer;
	}
}
