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
