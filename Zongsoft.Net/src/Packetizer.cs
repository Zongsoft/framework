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
