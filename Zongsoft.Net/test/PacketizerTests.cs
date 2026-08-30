using System;
using System.Linq;
using System.Buffers;
using System.Buffers.Binary;

using Xunit;

namespace Zongsoft.Net.Tests;

public class PacketizerTests
{
	[Fact]
	public void HeadedPackWritesBigEndianLengthAndSegmentedPayload()
	{
		var writer = new ArrayBufferWriter<byte>();
		var packetizer = TcpClient.Headed.Packetizer;
		var sequence = CreateSequence([1, 2], [3, 4, 5]);

		packetizer.Pack(writer, sequence);

		Assert.Equal(9, writer.WrittenCount);
		Assert.Equal(5, BinaryPrimitives.ReadInt32BigEndian(writer.WrittenSpan[..4]));
		Assert.Equal([1, 2, 3, 4, 5], writer.WrittenSpan[4..].ToArray());
	}

	[Theory]
	[InlineData(new byte[] { })]
	[InlineData(new byte[] { 0, 0, 0 })]
	[InlineData(new byte[] { 0, 0, 0, 3, 1, 2 })]
	public void HeadedUnpackRejectsIncompleteFrameWithoutConsumingInput(byte[] bytes)
	{
		var input = new ReadOnlySequence<byte>(bytes);
		var originalLength = input.Length;

		Assert.False(TcpClient.Headed.Packetizer.Unpack(ref input, out var package));
		Assert.True(package.IsEmpty);
		Assert.Equal(originalLength, input.Length);
	}

	[Fact]
	public void HeadedUnpackExtractsFrameAndLeavesFollowingData()
	{
		var bytes = new byte[]
		{
			0, 0, 0, 3, 1, 2, 3,
			0, 0, 0, 2, 4, 5,
		};
		var input = new ReadOnlySequence<byte>(bytes);

		Assert.True(TcpClient.Headed.Packetizer.Unpack(ref input, out var first));
		Assert.Equal([1, 2, 3], first.ToArray());
		Assert.Equal(6, input.Length);
		Assert.True(TcpClient.Headed.Packetizer.Unpack(ref input, out var second));
		Assert.Equal([4, 5], second.ToArray());
		Assert.True(input.IsEmpty);
	}

	[Fact]
	public void HeadedUnpackAcceptsEmptyFrame()
	{
		var input = new ReadOnlySequence<byte>(new byte[4]);

		Assert.True(TcpClient.Headed.Packetizer.Unpack(ref input, out var package));
		Assert.True(package.IsEmpty);
		Assert.True(input.IsEmpty);
	}

	[Fact]
	public void HeadlessPackWritesOwnerMemoryAndIgnoresNull()
	{
		var writer = new ArrayBufferWriter<byte>();
		using var owner = new TestMemoryOwner([1, 2, 3]);

		TcpClient.Headless.Packetizer.Pack(writer, owner);
		Assert.Equal([1, 2, 3], writer.WrittenSpan.ToArray());

		IMemoryOwner<byte> empty = null;
		TcpClient.Headless.Packetizer.Pack(writer, empty);
		Assert.Equal(3, writer.WrittenCount);
	}

	[Fact]
	public void HeadlessUnpackLeasesCompleteInput()
	{
		var source = new byte[] { 1, 2, 3, 4 };
		var input = new ReadOnlySequence<byte>(source);

		Assert.True(TcpClient.Headless.Packetizer.Unpack(ref input, out var package));
		using(package)
		{
			Assert.Equal(source, package.Memory.ToArray());
			source[0] = 9;
			Assert.Equal(1, package.Memory.Span[0]);
		}
	}

	private static ReadOnlySequence<byte> CreateSequence(byte[] first, byte[] second)
	{
		var head = new SequenceSegment(first);
		var tail = head.Append(second);
		return new ReadOnlySequence<byte>(head, 0, tail, tail.Memory.Length);
	}

	private sealed class SequenceSegment : ReadOnlySequenceSegment<byte>
	{
		public SequenceSegment(ReadOnlyMemory<byte> memory) => this.Memory = memory;

		public SequenceSegment Append(ReadOnlyMemory<byte> memory)
		{
			var segment = new SequenceSegment(memory)
			{
				RunningIndex = this.RunningIndex + this.Memory.Length,
			};

			this.Next = segment;
			return segment;
		}
	}

	private sealed class TestMemoryOwner(byte[] bytes) : IMemoryOwner<byte>
	{
		public Memory<byte> Memory { get; } = bytes;
		public void Dispose() { }
	}
}
