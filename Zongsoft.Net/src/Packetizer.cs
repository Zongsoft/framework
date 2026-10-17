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
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Net library.
 *
 * The Zongsoft.Net is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Net is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Net library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Buffers;
using System.Buffers.Binary;

namespace Zongsoft.Net;

internal sealed class HeadlessPacketizer(int maximumLength) : PacketizerBase<ReadOnlySequence<byte>>
{
	public override long GetLength(in ReadOnlySequence<byte> package)
	{
		if(package.Length > maximumLength)
			throw new TcpException(TcpError.InvalidFrame);

		return package.Length;
	}

	public override void Pack(IBufferWriter<byte> writer, in ReadOnlySequence<byte> package)
	{
		this.GetLength(in package);

		foreach(var segment in package)
		{
			if(!segment.IsEmpty)
				writer.Write(segment.Span);
		}
	}

	public override bool Unpack(ref ReadOnlySequence<byte> data, out ReadOnlySequence<byte> package)
	{
		if(data.IsEmpty)
		{
			package = default;
			return false;
		}

		package = data.Slice(0, Math.Min(data.Length, maximumLength));
		data = data.Slice(package.End);
		return true;
	}
}

internal sealed class HeadedPacketizer(int maximumLength) : PacketizerBase<ReadOnlySequence<byte>>
{
	private const int HEAD_SIZE = 4;

	public override long GetLength(in ReadOnlySequence<byte> package)
	{
		if(package.Length > maximumLength)
			throw new TcpException(TcpError.InvalidFrame);

		return checked(package.Length + HEAD_SIZE);
	}

	public override void Pack(IBufferWriter<byte> writer, in ReadOnlySequence<byte> package)
	{
		this.GetLength(in package);
		BinaryPrimitives.WriteUInt32BigEndian(writer.GetSpan(HEAD_SIZE), checked((uint)package.Length));
		writer.Advance(HEAD_SIZE);

		foreach(var segment in package)
		{
			if(!segment.IsEmpty)
				writer.Write(segment.Span);
		}
	}

	public override bool Unpack(ref ReadOnlySequence<byte> data, out ReadOnlySequence<byte> package)
	{
		package = default;

		if(!Zongsoft.Common.Buffer.TryGetUInt32BigEndian(data, out var length))
			return false;

		if(length > maximumLength)
			throw new TcpException(TcpError.InvalidFrame);

		var frameLength = checked((long)length + HEAD_SIZE);
		if(data.Length < frameLength)
			return false;

		package = data.Slice(HEAD_SIZE, length);
		data = data.Slice(frameLength);
		return true;
	}
}
