/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@qq.com>
 *
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;
using System.Buffers;

namespace Zongsoft.Communication.Tests;

public class PacketizerTest
{
	[Fact]
	public void SinglePackageRoundtrip()
	{
		IPacketizer<byte> packetizer = new BytePacketizer();
		var data = ReadOnlySequence<byte>.Empty;
		Assert.False(packetizer.Unpack(ref data, out _));

		var writer = new ArrayBufferWriter<byte>();
		packetizer.Pack(writer, 42);
		data = new(writer.WrittenMemory);
		Assert.True(packetizer.Unpack(ref data, out var package));
		Assert.Equal(42, package);
		Assert.True(data.IsEmpty);
	}

	[Fact]
	public void RequestResponseRoundtrip()
	{
		IPacketizer<byte, int> packetizer = new BytePacketizer();
		var data = ReadOnlySequence<byte>.Empty;
		Assert.False(packetizer.Unpack(ref data, out _));

		var writer = new ArrayBufferWriter<byte>();
		packetizer.Pack(writer, 43);
		data = new(writer.WrittenMemory);
		Assert.True(packetizer.Unpack(ref data, out var response));
		Assert.Equal(43, response);
		Assert.True(data.IsEmpty);
	}

	private sealed class BytePacketizer : IPacketizer<byte>, IPacketizer<byte, int>
	{
		public void Pack(IBufferWriter<byte> writer, in byte package)
		{
			writer.GetSpan(1)[0] = package;
			writer.Advance(1);
		}

		public bool Unpack(ref ReadOnlySequence<byte> data, out byte package)
		{
			var reader = new SequenceReader<byte>(data);
			if(!reader.TryRead(out package))
				return false;

			data = data.Slice(reader.Position);
			return true;
		}

		void IPacketizer<byte, int>.Pack(IBufferWriter<byte> writer, byte request) => this.Pack(writer, in request);
		bool IPacketizer<byte, int>.Unpack(ref ReadOnlySequence<byte> data, out int response)
		{
			var result = this.Unpack(ref data, out var package);
			response = package;
			return result;
		}
	}
}
