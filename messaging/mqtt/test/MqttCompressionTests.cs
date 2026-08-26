using System;
using System.Buffers;
using System.Linq;
using System.Text;

using MQTTnet;

using Xunit;

using Zongsoft.Common;

namespace Zongsoft.Messaging.Mqtt.Tests;

public class MqttCompressionTests
{
	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void PayloadCompressionRoundTripsAcrossProtocolEncodings(bool supportsProperties)
	{
		var source = Enumerable.Repeat((byte)'A', 16 * 1024).ToArray();
		var builder = new MqttApplicationMessageBuilder().WithTopic("topic/compressed");
		builder.SetPayload(source, new MessageCompression("Brotli", 1), supportsProperties);
		var message = builder.Build();

		Assert.Equal(source, message.GetPayload());
		if(supportsProperties)
		{
			Assert.Contains(message.UserProperties, property =>
				property.Name == "Zongsoft-Compression" && Encoding.UTF8.GetString(property.ValueBuffer.Span) == "Brotli");
			Assert.False(message.Payload.ToArray().AsSpan().StartsWith("ZCMP"u8));
		}
		else
		{
			Assert.Null(message.UserProperties);
			Assert.Equal("ZCMP"u8.ToArray(), message.Payload.ToArray()[..4]);
		}
	}

	[Fact]
	public void NonEnvelopePayloadRemainsUnchanged()
	{
		var payload = new byte[] { 1, 2, 3, 4 };
		var message = new MqttApplicationMessageBuilder().WithTopic("topic/plain").WithPayload(payload).Build();

		Assert.Equal(payload, message.GetPayload());
	}

	[Theory]
	[InlineData("Brotli")]
	[InlineData("GZip")]
	[InlineData("ZLib")]
	[InlineData("Deflate")]
	public void PrivateEnvelopeRoundTripsAllAlgorithms(string name)
	{
		var source = Enumerable.Repeat((byte)'A', 16 * 1024).ToArray();
		var builder = new MqttApplicationMessageBuilder().WithTopic("topic/private-envelope");
		builder.SetPayload(source, new MessageCompression(name, 0), false);
		var message = builder.Build();
		var envelope = message.Payload.ToArray();

		Assert.Equal("ZCMP"u8.ToArray(), envelope[..4]);
		Assert.NotEqual(source, envelope);
		Assert.Equal(source, message.GetPayload());
	}

	[Theory]
	[MemberData(nameof(GetMalformedPrivateEnvelopes))]
	public void PrivateEnvelopeRejectsMalformedPayload(byte[] payload)
	{
		var message = new MqttApplicationMessageBuilder().WithTopic("topic/malformed-envelope").WithPayload(payload).Build();

		Assert.Throws<FormatException>(() => message.GetPayload());
	}

	[Fact]
	public void PrivateEnvelopeRejectsUnsupportedAlgorithm()
	{
		var payload = new byte[] { 0x5A, 0x43, 0x4D, 0x50, 1, 7, (byte)'U', (byte)'n', (byte)'k', (byte)'n', (byte)'o', (byte)'w', (byte)'n', 1 };
		var message = new MqttApplicationMessageBuilder().WithTopic("topic/unsupported-envelope").WithPayload(payload).Build();

		var exception = Assert.Throws<OperationException>(() => message.GetPayload());
		Assert.Equal(nameof(OperationException.Unsupported), exception.Reason);
	}

	[Fact]
	public void EmptyAndBelowThresholdPayloadsRemainUncompressed()
	{
		var compression = new MessageCompression("GZip", 8);
		var empty = new MqttApplicationMessageBuilder().WithTopic("empty");
		empty.SetPayload(ReadOnlyMemory<byte>.Empty, compression, true);
		var small = new MqttApplicationMessageBuilder().WithTopic("small");
		small.SetPayload(new byte[] { 1, 2, 3, 4 }, compression, false);

		Assert.Empty(empty.Build().Payload.ToArray());
		Assert.Equal([1, 2, 3, 4], small.Build().Payload.ToArray());
	}

	public static TheoryData<byte[]> GetMalformedPrivateEnvelopes() => new()
	{
		new byte[] { 0x5A, 0x43, 0x4D, 0x50, 2, 6, (byte)'B', (byte)'r', (byte)'o', (byte)'t', (byte)'l', (byte)'i', 1 },
		new byte[] { 0x5A, 0x43, 0x4D, 0x50, 1, 7, (byte)'B', (byte)'r', (byte)'o' },
		new byte[] { 0x5A, 0x43, 0x4D, 0x50, 1, 0, 1 },
		new byte[] { 0x5A, 0x43, 0x4D, 0x50, 1, 6, (byte)'B', (byte)'r', (byte)'o', (byte)'t', (byte)'l', (byte)'i' },
	};
}
