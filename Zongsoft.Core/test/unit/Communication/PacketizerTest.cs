/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Buffers;

using Xunit;

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
