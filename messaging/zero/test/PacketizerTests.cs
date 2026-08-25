using System.Collections.Generic;

using Xunit;

namespace Zongsoft.Messaging.ZeroMQ.Tests;

public class PacketizerTests
{
	[Theory]
	[InlineData("")]
	[InlineData("topic")]
	[InlineData("@identifier")]
	[InlineData("topic@identifier\nBroken")]
	[InlineData("topic@identifier\n:Value")]
	[InlineData("topic@identifier\nKey:")]
	[InlineData("topic@identifier\nCompressor:Brotli")]
	[InlineData("topic@identifier\nProtocol-Version:1.0")]
	[InlineData("topic@identifier\nProtocol-Version:2.0\nProtocol-Version:2.0")]
	public void TryUnpackRejectsMalformedHeaders(string header)
	{
		Assert.False(Packetizer.TryUnpack(header, out _, out _, out _));
	}

	[Fact]
	public void TryUnpackParsesAddressAndOptions()
	{
		Assert.True(Packetizer.TryUnpack("group:topic@instance\nProtocol-Version:2.0\nCompressor:Brotli", out var identifier, out var topic, out var options));
		Assert.Equal("instance", identifier);
		Assert.Equal("group:topic", topic);
		Assert.Collection(options,
			option => Assert.Equal(new KeyValuePair<string, string>("Protocol-Version", "2.0"), option),
			option => Assert.Equal(new KeyValuePair<string, string>("Compressor", "Brotli"), option));
	}

	[Fact]
	public void PackIncludesMessageIdentifier()
	{
		var header = Packetizer.Pack("instance", "message-identifier", "group:topic", null);

		Assert.True(Packetizer.TryUnpack(header, out var identity, out var topic, out var options));
		Assert.Equal("instance", identity);
		Assert.Equal("group:topic", topic);
		Assert.True(Packetizer.Options.TryGetValue(options, Packetizer.Options.Identifier, out var identifier));
		Assert.Equal("message-identifier", identifier);
	}
}
