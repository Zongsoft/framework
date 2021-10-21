/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
 *
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Buffers;
using System.Buffers.Binary;
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Net
{
	public class TcpPacketizer : Zongsoft.Communication.IPacketizer<ReadOnlySequence<byte>>
	{
		#region 常量定义
		private const int HEAD_SIZE = 4;
		#endregion

		#region 单例字段
		public static readonly TcpPacketizer Instance = new TcpPacketizer();
		#endregion

		#region 公共属性
		public string Name { get => nameof(TcpPacketizer); }
		#endregion

		#region 打包方法
		public ValueTask PackAsync(IBufferWriter<byte> writer, in ReadOnlySequence<byte> package, CancellationToken cancellation = default)
		{
			var header = writer.GetSpan(HEAD_SIZE);
			BinaryPrimitives.WriteInt32BigEndian(header, (int)package.Length);
			writer.Advance(HEAD_SIZE);

			foreach(var segment in package)
				writer.Write(segment.Span);

			return ValueTask.CompletedTask;
		}

		public async ValueTask<System.IO.Pipelines.FlushResult> PackAsync(System.IO.Pipelines.PipeWriter writer, ReadOnlySequence<byte> package, CancellationToken cancellation)
		{
			var header = writer.GetMemory(HEAD_SIZE);
			BinaryPrimitives.WriteInt32BigEndian(header.Span, (int)package.Length);
			writer.Advance(HEAD_SIZE);

			System.IO.Pipelines.FlushResult result = default;

			foreach(var segment in package)
			{
				result = await writer.WriteAsync(segment, cancellation);

				if(result.IsCanceled)
					return result;
			}

			return result;
		}
		#endregion

		#region 解包方法
		public bool Unpack(ref ReadOnlySequence<byte> input, out ReadOnlySequence<byte> package)
		{
			if(!Zongsoft.Common.Buffer.TryGetUInt32BigEndian(input, out var length))
			{
				package = default;
				return false;
			}

			if(input.Length < length + HEAD_SIZE)
			{
				package = default;
				return false;
			}

			package = input.Slice(HEAD_SIZE, length);
			input = input.Slice(package.End);

			this.OnUnpacked(package);

			return true;
		}

		protected virtual void OnUnpacked(ReadOnlySequence<byte> result) { }

		private static int UnpackHeader(ReadOnlySpan<byte> input, out PackageHeader header)
		{
			var length = BinaryPrimitives.ReadInt32LittleEndian(input);
			header = default;
			return length;
		}
		#endregion

		#region 包头结构
		private readonly struct PackageHeader { }
		#endregion
	}
}
