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
using System.Collections.Generic;

namespace Zongsoft.Communication.Net
{
	public class TcpPacketizer<TPackage>
	{
		#region 常量定义
		private const int HEAD_LENGTH = 4;
		#endregion

		#region 私有变量
		private byte _headOffset;
		private readonly byte[] _headBuffer;
		private int _position;
		private Memory<byte> _buffer;
		private int _bufferId;
		private readonly IPacketizer<TPackage> _packetizer;
		#endregion

		#region 构造函数
		public TcpPacketizer(IPacketizer<TPackage> packetizer)
		{
			_packetizer = packetizer;
			_headBuffer = new byte[HEAD_LENGTH];
		}
		#endregion

		#region 公共属性
		public IPacketizer<TPackage> Packetizer { get => _packetizer; }
		#endregion

		#region 拆包方法
		public IList<ArraySegment<byte>> Pack(ReadOnlySpan<byte> data)
		{
			return new ArraySegment<byte>[] { BitConverter.GetBytes(data.Length), data.ToArray() };
		}

		public IEnumerable<TPackage> Unpack(ReadOnlyMemory<byte> data, int offset = 0)
		{
			if(data.IsEmpty)
				yield break;

			while(_headOffset++ < 4)
			{
				_headBuffer[_headOffset] = data.Span[offset++];

				if(_headOffset == 3)
				{
					_bufferId = this.Allocate(BitConverter.ToUInt32(_headBuffer), out _buffer);
				}
			}

			if(_bufferId != 0 && !_buffer.IsEmpty)
			{
				var length = Math.Min(data.Length - offset, _buffer.Length - _position);

				if(length > 0)
				{
					data.Slice(offset, length).CopyTo(_buffer.Slice(_position));
					offset += length;
					_position += length;
				}

				if(_position >= _buffer.Length)
				{
					if(_packetizer.TryUnpack(_buffer.Span, out var package))
					{
						this.Free(_bufferId);
						yield return package;
					}

					_buffer = null;
					_position = 0;
					_headOffset = 0;

					if(offset < data.Length)
					{
						var packages = this.Unpack(data, offset);

						foreach(var item in packages)
							yield return item;
					}
				}
			}
		}
		#endregion

		#region 分配缓存
		private int Allocate(uint length, out Memory<byte> buffer)
		{
			if(length == 0)
			{
				buffer = Memory<byte>.Empty;
				return 0;
			}

			buffer = new byte[length];
			return 1;
		}

		private void Free(int identifier)
		{
		}
		#endregion
	}
}
