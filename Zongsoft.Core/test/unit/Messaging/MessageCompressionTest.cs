using System;
using System.Text;

using Xunit;

using Zongsoft.Common;

namespace Zongsoft.Messaging.Tests;

public class MessageCompressionTest
{
	[Theory]
	[InlineData("", null, 0, "")]
	[InlineData("   ", null, 0, "")]
	[InlineData("none", null, 0, "")]
	[InlineData("Brotli:4096", "Brotli", 4096, "Brotli:4096")]
	[InlineData(" gzip : 0008 ", "gzip", 8, "gzip:8")]
	[InlineData("ZLib:0", "ZLib", 0, "ZLib:0")]
	public void MessageCompressionParsesAndNormalizesValidText(string text, string name, int value, string expected)
	{
		Assert.True(MessageCompression.TryParse(text, out var compression));
		Assert.Equal(name, compression.Name);
		Assert.Equal(value, compression.Value);
		Assert.Equal(expected, compression.ToString());
		Assert.Equal(compression, Parse<MessageCompression>(text));
	}

	[Theory]
	[InlineData("Brotli")]
	[InlineData(":1")]
	[InlineData("Brotli:")]
	[InlineData("Brotli:-1")]
	[InlineData("Brotli:+1")]
	[InlineData("Brotli:1.0")]
	[InlineData("Brotli:1:2")]
	public void MessageCompressionRejectsMalformedText(string text)
	{
		Assert.False(MessageCompression.TryParse(text, out var compression));
		Assert.True(compression.IsEmpty);
		Assert.Throws<FormatException>(() => MessageCompression.Parse(text));
	}

	[Fact]
	public void MessageCompressionConstructorRejectsInvalidArguments()
	{
		Assert.Throws<ArgumentNullException>(() => new MessageCompression(null, 1));
		Assert.Throws<ArgumentNullException>(() => new MessageCompression(" ", 1));
		Assert.Throws<ArgumentOutOfRangeException>(() => new MessageCompression("Brotli", -1));
	}

	[Fact]
	public void MessageCompressionEqualityIgnoresAlgorithmCase()
	{
		var first = new MessageCompression("Brotli", 4096);
		var second = new MessageCompression("brotli", 4096);
		var different = new MessageCompression("Brotli", 4097);

		Assert.Equal(first, second);
		Assert.True(first == second);
		Assert.False(first != second);
		Assert.Equal(first.GetHashCode(), second.GetHashCode());
		Assert.NotEqual(first, different);
		Assert.Equal("Brotli:4096", first.ToString());
		Assert.Equal(string.Empty, default(MessageCompression).ToString());
	}

	[Fact]
	public void MessageCompressionHonorsThresholdBoundaries()
	{
		var compression = new MessageCompression("Brotli", 4);
		var immediate = new MessageCompression("Brotli", 0);

		Assert.False(default(MessageCompression).CanCompress(4096));
		Assert.False(compression.CanCompress(0));
		Assert.False(compression.CanCompress(3));
		Assert.True(compression.CanCompress(4));
		Assert.True(compression.CanCompress(5));
		Assert.False(immediate.CanCompress(0));
		Assert.True(immediate.CanCompress(1));
	}

	[Theory]
	[InlineData("Brotli")]
	[InlineData("GZip")]
	[InlineData("ZLib")]
	[InlineData("Deflate")]
	public void MessageCompressionDirectCompressionRoundTripsAllAlgorithms(string name)
	{
		var source = Encoding.UTF8.GetBytes(new string('A', 4096));
		var compression = new MessageCompression(name, 0);

		var compressed = compression.Compress(source);
		Assert.NotEmpty(compressed);
		Assert.NotEqual(source, compressed);
		Assert.Equal(source, MessageCompression.Decompress(name, compressed));
	}

	[Fact]
	public void UnsupportedCompressionAlgorithmThrowsUnsupportedOperation()
	{
		var compression = new MessageCompression("Unknown", 0);
		var exception = Assert.Throws<OperationException>(() => compression.Compress([1, 2, 3]));

		Assert.Equal(nameof(OperationException.Unsupported), exception.Reason);
	}

	private static T Parse<T>(string text) where T : IParsable<T> => T.Parse(text, null);
}
