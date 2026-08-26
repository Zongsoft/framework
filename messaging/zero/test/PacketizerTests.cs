using System.Collections.Generic;

using Xunit;

namespace Zongsoft.Messaging.ZeroMQ.Tests;

public class PacketizerTests
{
	[Theory]
	[InlineData("Zongsoft.Messaging.ZeroMQ\nProtocol-Version:1.0\nEpoch:0123456789abcdef0123456789abcdef\nPorts:32101,32102", 0, 32101, 32102)]
	[InlineData("Zongsoft.Messaging.ZeroMQ\nProtocol-Version:1.0\nEpoch:0123456789abcdef0123456789abcdef\nPorts:32100,32101,32102", 32100, 32101, 32102)]
	public void DiscoveryResponseParsesProtocol10Ports(string response, ushort control, ushort incoming, ushort outgoing)
	{
		Assert.True(Protocol.TryParseDiscoveryResponse(response, out var epoch, out var actualControl, out var actualIncoming, out var actualOutgoing));
		Assert.Equal("0123456789abcdef0123456789abcdef", epoch);
		Assert.Equal(control, actualControl);
		Assert.Equal(incoming, actualIncoming);
		Assert.Equal(outgoing, actualOutgoing);
	}

	[Theory]
	[InlineData("Zongsoft.Messaging.ZeroMQ\nProtocol-Version:2.0\nEpoch:0123456789abcdef0123456789abcdef\nPorts:32101,32102")]
	[InlineData("Zongsoft.Messaging.ZeroMQ\nProtocol-Version:1.0\nEpoch:0123456789abcdef0123456789abcdef\nControl:32100\nIncoming:32101\nOutgoing:32102")]
	[InlineData("Zongsoft.Messaging.ZeroMQ\nProtocol-Version:1.0\nEpoch:0123456789abcdef0123456789abcdef\nPorts:32100")]
	public void DiscoveryResponseRejectsOldOrMalformedFormats(string response)
	{
		Assert.False(Protocol.TryParseDiscoveryResponse(response, out _, out _, out _, out _));
	}

	[Fact]
	public void ZeroQueueDoesNotAdvertiseDelay()
	{
		var settings = Configuration.ZeroConnectionSettingsDriver.Instance.GetSettings("Features", "server=127.0.0.1");
		using var queue = new ZeroQueue("Features", settings);

		Assert.False(queue.Features.Contains(MessageQueueFeature.Delay.Name));
		Assert.True(queue.Features.Contains(MessageQueueFeature.Compression.Name));
	}

	[Theory]
	[InlineData("")]
	[InlineData("topic")]
	[InlineData("topic\nBroken")]
	[InlineData("topic\n:Value")]
	[InlineData("topic\nKey:")]
	[InlineData("topic\nCompression:Brotli")]
	[InlineData("topic\nProtocol-Version:2.0")]
	[InlineData("topic\nProtocol-Version:1.0\nProtocol-Version:1.0")]
	[InlineData("topic\nProtocol-Version:1.0\rInjected:Value")]
	public void TryUnpackRejectsMalformedHeaders(string header)
	{
		Assert.False(Packetizer.TryUnpack(header, out _, out _));
	}

	[Fact]
	public void TryUnpackParsesTopicAndOptions()
	{
		Assert.True(Packetizer.TryUnpack("group:topic\nProtocol-Version:1.0\nCompression:Brotli", out var topic, out var options));
		Assert.Equal("group:topic", topic);
		Assert.Collection(options,
			option => Assert.Equal(new KeyValuePair<string, string>("Protocol-Version", "1.0"), option),
			option => Assert.Equal(new KeyValuePair<string, string>("Compression", "Brotli"), option));
	}

	[Fact]
	public void PackIncludesBusinessMetadata()
	{
		var header = Packetizer.Pack("instance", "message-identifier", "group:topic", "tag-a,tag:b", "Brotli");

		Assert.True(Packetizer.TryUnpack(header, out var topic, out var options));
		Assert.Equal("group:topic", topic);
		Assert.True(Packetizer.TryGetValue(options, Protocol.Headers.Identifier, out var identifier));
		Assert.Equal("message-identifier", identifier);
		Assert.True(Packetizer.TryGetValue(options, Protocol.Headers.Identity, out var identity));
		Assert.Equal("instance", identity);
		Assert.True(Packetizer.TryGetValue(options, Protocol.Headers.Tags, out var tags));
		Assert.Equal("tag-a,tag:b", tags);
		Assert.True(Packetizer.TryGetValue(options, Protocol.Headers.Compression, out var compression));
		Assert.Equal("Brotli", compression);
	}
}
