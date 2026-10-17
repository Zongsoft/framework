using System;
using System.Linq;
using System.Buffers;
using System.Buffers.Binary;

using Xunit;

namespace Zongsoft.Net.Tests;

public class PacketizerTest
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
	public void HeadlessPackUsesBorrowedSequenceAndEmptyAddsNothing()
	{
		var writer = new ArrayBufferWriter<byte>();
		var packetizer = TcpClient.Headless.Packetizer;
		packetizer.Pack(writer, new ReadOnlySequence<byte>(new byte[] { 1, 2, 3 }));
		packetizer.Pack(writer, ReadOnlySequence<byte>.Empty);
		Assert.Equal(new byte[] { 1, 2, 3 }, writer.WrittenSpan.ToArray());
	}

	[Fact]
	public void HeadlessUnpackBorrowsBoundedInputWithoutCopy()
	{
		var source = new byte[] { 1, 2, 3, 4 };
		var input = new ReadOnlySequence<byte>(source);
		var packetizer = TcpClient.CreateHeadless(new() { MaximumPacketLength = 2 }).Packetizer;
		Assert.True(packetizer.Unpack(ref input, out var package));
		Assert.Equal(2, package.Length);
		Assert.Equal(2, input.Length);
		source[0] = 9;
		Assert.Equal(9, package.FirstSpan[0]);
	}

	[Fact]
	public void HeadlessUnpackEmptyInputIsIncomplete()
	{
		var input = ReadOnlySequence<byte>.Empty;
		Assert.False(TcpClient.Headless.Packetizer.Unpack(ref input, out _));
	}

	[Theory]
	[InlineData(65537U)]
	[InlineData(2147483648U)]
	[InlineData(4294967295U)]
	public void HeadedRejectsOversizedUnsignedLengthWithoutConsumption(uint length)
	{
		var bytes = new byte[4];
		BinaryPrimitives.WriteUInt32BigEndian(bytes, length);
		var input = new ReadOnlySequence<byte>(bytes);
		var exception = Assert.Throws<TcpException>(() => TcpClient.Headed.Packetizer.Unpack(ref input, out _));

		Assert.Equal(TcpError.InvalidFrame, exception.ErrorCode);
		Assert.Equal(4, input.Length);
	}

	[Fact]
	public void HeadedHeaderAndPayloadMaySpanSegments()
	{
		var input = CreateSequence([0, 0], [0, 3, 1, 2, 3]);
		Assert.True(TcpClient.Headed.Packetizer.Unpack(ref input, out var package));
		Assert.Equal(new byte[] { 1, 2, 3 }, package.ToArray());
		Assert.True(input.IsEmpty);
		Assert.Equal(7, TcpClient.Headed.Packetizer.GetLength(package));
	}
	[Theory]
	[InlineData(512, 128)]
	[InlineData(128, 1024)]
	public void HeadedUnpackBatchPreservesFrameOrderAndIncompleteTail(int count, int length)
	{
		var writer = new ArrayBufferWriter<byte>();
		var packetizer = TcpClient.Headed.Packetizer;
		var payload = new byte[length];

		for(var i = 0; i < count; i++)
		{
			BinaryPrimitives.WriteInt32BigEndian(payload, i);
			packetizer.Pack(writer, new ReadOnlySequence<byte>(payload));
		}

		writer.Write(new byte[] { 0, 0 });
		var input = CreateSequence(writer.WrittenSpan[..2].ToArray(), writer.WrittenSpan[2..].ToArray());

		for(var i = 0; i < count; i++)
		{
			Assert.True(packetizer.Unpack(ref input, out var package));
			Assert.Equal(length, package.Length);
			Assert.Equal(i, BinaryPrimitives.ReadInt32BigEndian(package.FirstSpan));
		}

		Assert.Equal(2, input.Length);
		Assert.False(packetizer.Unpack(ref input, out _));
		Assert.Equal(2, input.Length);
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

}
