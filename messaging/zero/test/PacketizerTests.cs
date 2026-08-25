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
	public void TryUnpackRejectsMalformedHeaders(string header)
	{
		Assert.False(Packetizer.TryUnpack(header, out _, out _, out _));
	}

	[Fact]
	public void TryUnpackParsesAddressAndOptions()
	{
		Assert.True(Packetizer.TryUnpack("group:topic@instance\nCompressor:Brotli", out var identifier, out var topic, out var options));
		Assert.Equal("instance", identifier);
		Assert.Equal("group:topic", topic);
		var option = Assert.Single(options);
		Assert.Equal(new KeyValuePair<string, string>("Compressor", "Brotli"), option);
	}
}
