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
using System.Net;
using System.Linq;
using System.Buffers;

using Zongsoft.Communication;

namespace Zongsoft.Net.Samples;

internal static class SampleProtocol
{
	#region 协议与地址
	internal static PacketizerBase<ReadOnlySequence<byte>> Create(bool custom) => custom ? new LinePacketizer() : TcpClient.Headed.Packetizer;
	internal static IPEndPoint GetAddress(string[] args)
	{
		var positional = args.TakeWhile(argument => !argument.StartsWith("--", StringComparison.Ordinal)).ToArray();
		return new(positional.Length > 0 ? IPAddress.Parse(positional[0]) : IPAddress.Loopback, positional.Length > 1 ? int.Parse(positional[1]) : 7969);
	}
	#endregion

	#region 自定义分帧
	//文本协议示例：LF 结束一帧，不允许正文包含 LF，正文最大 64 KiB。
	private sealed class LinePacketizer : PacketizerBase<ReadOnlySequence<byte>>
	{
		public override long GetLength(in ReadOnlySequence<byte> package)
		{
			if(package.Length > 65536)
				throw new InvalidDataException();

			foreach(var segment in package)
			{
				if(segment.Span.Contains((byte)'\n'))
					throw new InvalidDataException();
			}

			return package.Length + 1;
		}

		public override void Pack(IBufferWriter<byte> writer, in ReadOnlySequence<byte> package)
		{
			_ = this.GetLength(package);

			foreach(var segment in package)
				writer.Write(segment.Span);

			writer.GetSpan(1)[0] = (byte)'\n';
			writer.Advance(1);
		}

		public override bool Unpack(ref ReadOnlySequence<byte> data, out ReadOnlySequence<byte> package)
		{
			var reader = new SequenceReader<byte>(data);
			if(!reader.TryReadTo(out package, (byte)'\n'))
			{
				if(data.Length > 65536)
					throw new InvalidDataException();

				return false;
			}

			if(package.Length > 65536)
				throw new InvalidDataException();

			data = data.Slice(reader.Position);
			return true;
		}
	}
	#endregion
}
