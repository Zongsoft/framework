using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;

using Xunit;

namespace Zongsoft.Net.Tests;

public class TcpClientTest
{
	[Fact]
	public void ConstructorRejectsNullPacketizer() => Assert.Throws<ArgumentNullException>(() => new TcpClient<string>(null));

	[Fact]
	public async Task StaticClientsAreIndependentAndOptionsAreCopied()
	{
		await using var server = TestNetwork.Server();
		var options = new TcpClientOptions();
		await using var first = TcpClient.CreateHeaded(options);
		await using var second = TcpClient.Headed;
		options.MaximumPendingSends = 0;
		Assert.NotSame(first, second);
		Assert.Equal(new IPEndPoint(IPAddress.Loopback, 7969), first.Address);
		var address = (IPEndPoint)first.Address;
		address.Port = 1;
		Assert.Equal(7969, ((IPEndPoint)first.Address).Port);
		await server.StartAsync();
		await first.ConnectAsync(server.Address);
		await first.SendAsync(new ReadOnlySequence<byte>(new byte[] { 1 }));
	}

	[Fact]
	public async Task ConcurrentConnectsShareChannelAndDisconnectAllowsFreshConnection()
	{
		await using var server = TestNetwork.Server();
		await server.StartAsync();
		await using var client = TcpClient.Headed;
		client.Address = server.Address;
		var channels = await Task.WhenAll(Enumerable.Range(0, 64).Select(_ => client.ConnectAsync()));
		Assert.All(channels, channel => Assert.Same(channels[0], channel));
		await client.DisconnectAsync();
		Assert.True(channels[0].WaitAsync().IsCompletedSuccessfully);
		var next = await client.ConnectAsync();
		Assert.NotEqual(channels[0].Identifier, next.Identifier);
		Assert.NotSame(channels[0], next);
	}

	[Fact]
	public async Task PreCanceledConnectionDoesNotCreateOrPublishChannel()
	{
		await using var client = TcpClient.Headed;
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.ConnectAsync(cancellation: cancellation.Token));
		Assert.Null(client.Channel);
	}

	[Fact]
	public async Task ConnectDisposeRaceCannotLeavePublishedChannel()
	{
		await using var server = TestNetwork.Server();
		await server.StartAsync();

		for(var i = 0; i < 100; i++)
		{
			var client = TcpClient.Headed;
			var connecting = client.ConnectAsync(server.Address);
			var disposing = client.DisposeAsync().AsTask();

			try { await connecting; }
			catch(OperationCanceledException) { }

			await disposing.WaitAsync(TestNetwork.Timeout);
			Assert.Null(client.Channel);
			await Assert.ThrowsAsync<ObjectDisposedException>(() => client.ConnectAsync());
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task AddressChangeRetiresOriginalChannelAndRoutesNextSendToNewServer(bool zeroLengthReads)
	{
		await using var first = TestNetwork.Server();
		await using var second = TestNetwork.Server();
		var received = Channel.CreateUnbounded<byte[]>();
		second.Handler = new ReceiveHandler<ReadOnlySequence<byte>>((context, _) =>
		{
			received.Writer.TryWrite(context.Package.ToArray());
			return ValueTask.CompletedTask;
		});
		await first.StartAsync();
		await second.StartAsync();
		await using var client = TcpClient.CreateHeaded(new() { ZeroLengthReads = zeroLengthReads });

		for(var i = 0; i < 100; i++)
		{
			var original = await client.ConnectAsync(first.Address).WaitAsync(TestNetwork.Timeout);
			client.Address = second.Address;
			var current = await client.ConnectAsync().WaitAsync(TestNetwork.Timeout);
			await original.WaitAsync().WaitAsync(TestNetwork.Timeout);
			Assert.Equal(second.Address, current.RemoteEndPoint);
			Assert.Same(current, client.Channel);

			var bytes = new byte[] { (byte)i };
			await current.SendAsync(new ReadOnlySequence<byte>(bytes));
			Assert.Equal(bytes, await received.Reader.ReadAsync().AsTask().WaitAsync(TestNetwork.Timeout));
			await client.DisconnectAsync().AsTask().WaitAsync(TestNetwork.Timeout);
			await current.WaitAsync().WaitAsync(TestNetwork.Timeout);
		}
	}

	[Fact]
	public async Task RepliesReturnOnlyToOriginatingClient()
	{
		await using var server = TestNetwork.Server();
		server.Handler = new ReceiveHandler<ReadOnlySequence<byte>>((context, token) => context.Channel.SendAsync(context.Package, token));
		await server.StartAsync();
		await using var first = TcpClient.Headed;
		await using var second = TcpClient.Headed;
		var reply1 = TestNetwork.Signal<byte[]>();
		var reply2 = TestNetwork.Signal<byte[]>();
		first.Handler = new ReceiveHandler<ReadOnlySequence<byte>>((context, _) => { reply1.TrySetResult(context.Package.ToArray()); return ValueTask.CompletedTask; });
		second.Handler = new ReceiveHandler<ReadOnlySequence<byte>>((context, _) => { reply2.TrySetResult(context.Package.ToArray()); return ValueTask.CompletedTask; });
		first.Address = second.Address = server.Address;
		await Task.WhenAll(first.SendAsync(new ReadOnlySequence<byte>(new byte[] { 1, 2 })).AsTask(), second.SendAsync(new ReadOnlySequence<byte>(new byte[] { 3, 4 })).AsTask());
		Assert.Equal(new byte[] { 1, 2 }, await reply1.Task.WaitAsync(TestNetwork.Timeout));
		Assert.Equal(new byte[] { 3, 4 }, await reply2.Task.WaitAsync(TestNetwork.Timeout));
	}
}
