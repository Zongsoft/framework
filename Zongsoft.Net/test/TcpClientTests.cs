using System;
using System.Net;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.Collections;
using Zongsoft.Communication;
using Zongsoft.Components;

namespace Zongsoft.Net.Tests;

public class TcpClientTests
{
	[Fact]
	public void ConstructorRejectsNullPacketizer()
	{
		Assert.Throws<ArgumentNullException>(() => new TcpClient<string>(null));
	}

	[Fact]
	public async Task SendAsyncWithoutAddressRejectsBeforeConnecting()
	{
		var client = new TcpClient<string>(new StringPacketizer());

		await Assert.ThrowsAsync<InvalidOperationException>(() => client.SendAsync("payload").AsTask());
	}

	[Fact]
	public void NonGenericHandlerRejectsIncompatibleType()
	{
		IHandleable handleable = new TcpClient<string>(new StringPacketizer());

		Assert.Throws<ArgumentException>(() => handleable.Handler = new ObjectHandler());
	}

	[Fact]
	public void StaticClientsDefaultToLoopbackPort7969()
	{
		Assert.Equal(new IPEndPoint(IPAddress.Loopback, 7969), TcpClient.Headless.Address);
		Assert.Equal(new IPEndPoint(IPAddress.Loopback, 7969), TcpClient.Headed.Address);
		Assert.NotSame(TcpClient.Headless.Packetizer, TcpClient.Headed.Packetizer);
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

	private sealed class ObjectHandler : IHandler
	{
		public ValueTask HandleAsync(object argument, CancellationToken cancellation = default) => ValueTask.CompletedTask;
		public ValueTask HandleAsync(object argument, Parameters parameters, CancellationToken cancellation = default) => ValueTask.CompletedTask;
	}
}
