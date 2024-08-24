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
using System.Text;
using System.Buffers;
using System.Buffers.Binary;
using System.Threading;
using System.Runtime.InteropServices;

namespace Zongsoft.Common
{
	public static class Buffer
	{
		#region 寄租方法
		/// <summary>
		/// 获取一个空的寄存区(<see cref="IMemoryOwner{T}"/>)。
		/// </summary>
		/// <typeparam name="T">缓存元素类型。</typeparam>
		/// <returns>返回一个空的寄存区。</returns>
		public static IMemoryOwner<T> Empty<T>() => MemoryWrapper<T>.Empty;

		/// <summary>
		/// 寄存指定的缓存内容（即包装一个 <see cref="Memory{T}"/> 结构）。
		/// </summary>
		/// <typeparam name="T">缓存元素类型。</typeparam>
		/// <param name="memory">指定的缓存。</param>
		/// <returns>返回被包装好的寄存区。</returns>
		public static IMemoryOwner<T> Owned<T>(this Memory<T> memory) => new MemoryWrapper<T>(memory);

		/// <summary>
		/// 寄存指定的数组内容。
		/// </summary>
		/// <typeparam name="T">数组元素类型。</typeparam>
		/// <param name="source">指定要寄存的数组内容。</param>
		/// <param name="length">指定要寄存的长度。</param>
		/// <returns>返回寄租的<see cref="IMemoryOwner{T}"/>对象。</returns>
		public static IMemoryOwner<T> Lease<T>(this T[] source, int length = -1)
		{
			if(source == null)
				return null;

			if(length < 0)
				length = source.Length;
			else if(length > source.Length)
				throw new ArgumentOutOfRangeException(nameof(length));

			return length == 0 ? Empty<T>() : new ArrayPoolOwner<T>(source, length);
		}

		/// <summary>
		/// 寄存指定的内存区域。
		/// </summary>
		/// <typeparam name="T">缓存元素类型。</typeparam>
		/// <param name="source">指定要寄存的内存区域。</param>
		/// <returns>返回寄租的<see cref="IMemoryOwner{T}"/>对象。</returns>
		public static IMemoryOwner<T> Lease<T>(this ReadOnlySequence<T> source)
		{
			if(source.IsEmpty)
				return Empty<T>();

			int length = checked((int)source.Length);
			var buffer = ArrayPool<T>.Shared.Rent(length);
			source.CopyTo(buffer);
			return new ArrayPoolOwner<T>(buffer, length);
		}
		#endregion

		#region 字符处理
		public static IMemoryOwner<char> Decode(this ReadOnlyMemory<byte> data, Encoding encoding = null)
		{
			if(data.IsEmpty)
				return Empty<char>();

			if(encoding == null)
				encoding = Encoding.UTF8;

			if(!MemoryMarshal.TryGetArray(data, out var blob))
				throw new InvalidOperationException();

			var count = encoding.GetCharCount(blob.Array, blob.Offset, blob.Count);
			var buffer = ArrayPool<char>.Shared.Rent(count);
			encoding.GetChars(blob.Array, blob.Offset, blob.Count, buffer, 0);
			return new ArrayPoolOwner<char>(buffer, count);
		}

		public static IMemoryOwner<byte> Encode(this string text, Encoding encoding = null)
		{
			if(string.IsNullOrEmpty(text))
				return Empty<byte>();

			if(encoding == null)
				encoding = Encoding.UTF8;

			var count = encoding.GetByteCount(text);
			var buffer = ArrayPool<byte>.Shared.Rent(count);
			encoding.GetBytes(text, 0, text.Length, buffer, 0);
			return new ArrayPoolOwner<byte>(buffer, count);
		}
		#endregion

		#region 大端读数
		public static bool TryGetInt16BigEndian(this ReadOnlySequence<byte> buffer, out short value)
		{
			const int SIZE = sizeof(short);

			if(buffer.Length < SIZE)
			{
				value = 0;
				return false;
			}

			if(buffer.First.Length >= SIZE)
				return BinaryPrimitives.TryReadInt16BigEndian(buffer.FirstSpan, out value);

			Span<byte> local = stackalloc byte[SIZE];
			buffer.Slice(0, SIZE).CopyTo(local);
			return BinaryPrimitives.TryReadInt16BigEndian(local, out value);
		}
		public static bool TryGetInt32BigEndian(this ReadOnlySequence<byte> buffer, out int value)
		{
			const int SIZE = sizeof(int);

			if(buffer.Length < SIZE)
			{
				value = 0;
				return false;
			}

			if(buffer.First.Length >= SIZE)
				return BinaryPrimitives.TryReadInt32BigEndian(buffer.FirstSpan, out value);

			Span<byte> local = stackalloc byte[SIZE];
			buffer.Slice(0, SIZE).CopyTo(local);
			return BinaryPrimitives.TryReadInt32BigEndian(local, out value);
		}
		public static bool TryGetInt64BigEndian(this ReadOnlySequence<byte> buffer, out long value)
		{
			const int SIZE = sizeof(long);

			if(buffer.Length < SIZE)
			{
				value = 0;
				return false;
			}

			if(buffer.First.Length >= SIZE)
				return BinaryPrimitives.TryReadInt64BigEndian(buffer.FirstSpan, out value);

			Span<byte> local = stackalloc byte[SIZE];
			buffer.Slice(0, SIZE).CopyTo(local);
			return BinaryPrimitives.TryReadInt64BigEndian(local, out value);
		}
		public static bool TryGetUInt16BigEndian(this ReadOnlySequence<byte> buffer, out ushort value)
		{
			const int SIZE = sizeof(ushort);

			if(buffer.Length < SIZE)
			{
				value = 0;
				return false;
			}

			if(buffer.First.Length >= SIZE)
				return BinaryPrimitives.TryReadUInt16BigEndian(buffer.FirstSpan, out value);

			Span<byte> local = stackalloc byte[SIZE];
			buffer.Slice(0, SIZE).CopyTo(local);
			return BinaryPrimitives.TryReadUInt16BigEndian(local, out value);
		}
		public static bool TryGetUInt32BigEndian(this ReadOnlySequence<byte> buffer, out uint value)
		{
			const int SIZE = sizeof(uint);

			if(buffer.Length < SIZE)
			{
				value = 0;
				return false;
			}

			if(buffer.First.Length >= SIZE)
				return BinaryPrimitives.TryReadUInt32BigEndian(buffer.FirstSpan, out value);

			Span<byte> local = stackalloc byte[SIZE];
			buffer.Slice(0, SIZE).CopyTo(local);
			return BinaryPrimitives.TryReadUInt32BigEndian(local, out value);
		}
		public static bool TryGetUInt64BigEndian(this ReadOnlySequence<byte> buffer, out ulong value)
		{
			const int SIZE = sizeof(ulong);

			if(buffer.Length < SIZE)
			{
				value = 0;
				return false;
			}

			if(buffer.First.Length >= SIZE)
				return BinaryPrimitives.TryReadUInt64BigEndian(buffer.FirstSpan, out value);

			Span<byte> local = stackalloc byte[SIZE];
			buffer.Slice(0, SIZE).CopyTo(local);
			return BinaryPrimitives.TryReadUInt64BigEndian(local, out value);
		}
		public static bool TryGetSingleBigEndian(this ReadOnlySequence<byte> buffer, out float value)
		{
			const int SIZE = sizeof(float);

			if(buffer.Length < SIZE)
			{
				value = 0;
				return false;
			}

			if(buffer.First.Length >= SIZE)
				return BinaryPrimitives.TryReadSingleBigEndian(buffer.FirstSpan, out value);

			Span<byte> local = stackalloc byte[SIZE];
			buffer.Slice(0, SIZE).CopyTo(local);
			return BinaryPrimitives.TryReadSingleBigEndian(local, out value);
		}
		public static bool TryGetDoubleBigEndian(this ReadOnlySequence<byte> buffer, out double value)
		{
			const int SIZE = sizeof(double);

			if(buffer.Length < SIZE)
			{
				value = 0;
				return false;
			}

			if(buffer.First.Length >= SIZE)
				return BinaryPrimitives.TryReadDoubleBigEndian(buffer.FirstSpan, out value);

			Span<byte> local = stackalloc byte[SIZE];
			buffer.Slice(0, SIZE).CopyTo(local);
			return BinaryPrimitives.TryReadDoubleBigEndian(local, out value);
		}
		#endregion

		#region 小端读数
		public static bool TryGetInt16LittleEndian(this ReadOnlySequence<byte> buffer, out short value)
		{
			const int SIZE = sizeof(short);

			if(buffer.Length < SIZE)
			{
				value = 0;
				return false;
			}

			if(buffer.First.Length >= SIZE)
				return BinaryPrimitives.TryReadInt16LittleEndian(buffer.FirstSpan, out value);

			Span<byte> local = stackalloc byte[SIZE];
			buffer.Slice(0, SIZE).CopyTo(local);
			return BinaryPrimitives.TryReadInt16LittleEndian(local, out value);
		}
		public static bool TryGetInt32LittleEndian(this ReadOnlySequence<byte> buffer, out int value)
		{
			const int SIZE = sizeof(int);

			if(buffer.Length < SIZE)
			{
				value = 0;
				return false;
			}

			if(buffer.First.Length >= SIZE)
				return BinaryPrimitives.TryReadInt32LittleEndian(buffer.FirstSpan, out value);

			Span<byte> local = stackalloc byte[SIZE];
			buffer.Slice(0, SIZE).CopyTo(local);
			return BinaryPrimitives.TryReadInt32LittleEndian(local, out value);
		}
		public static bool TryGetInt64LittleEndian(this ReadOnlySequence<byte> buffer, out long value)
		{
			const int SIZE = sizeof(long);

			if(buffer.Length < SIZE)
			{
				value = 0;
				return false;
			}

			if(buffer.First.Length >= SIZE)
				return BinaryPrimitives.TryReadInt64LittleEndian(buffer.FirstSpan, out value);

			Span<byte> local = stackalloc byte[SIZE];
			buffer.Slice(0, SIZE).CopyTo(local);
			return BinaryPrimitives.TryReadInt64LittleEndian(local, out value);
		}
		public static bool TryGetUInt16LittleEndian(this ReadOnlySequence<byte> buffer, out ushort value)
		{
			const int SIZE = sizeof(ushort);

			if(buffer.Length < SIZE)
			{
				value = 0;
				return false;
			}

			if(buffer.First.Length >= SIZE)
				return BinaryPrimitives.TryReadUInt16LittleEndian(buffer.FirstSpan, out value);

			Span<byte> local = stackalloc byte[SIZE];
			buffer.Slice(0, SIZE).CopyTo(local);
			return BinaryPrimitives.TryReadUInt16LittleEndian(local, out value);
		}
		public static bool TryGetUInt32LittleEndian(this ReadOnlySequence<byte> buffer, out uint value)
		{
			const int SIZE = sizeof(uint);

			if(buffer.Length < SIZE)
			{
				value = 0;
				return false;
			}

			if(buffer.First.Length >= SIZE)
				return BinaryPrimitives.TryReadUInt32LittleEndian(buffer.FirstSpan, out value);

			Span<byte> local = stackalloc byte[SIZE];
			buffer.Slice(0, SIZE).CopyTo(local);
			return BinaryPrimitives.TryReadUInt32LittleEndian(local, out value);
		}
		public static bool TryGetUInt64LittleEndian(this ReadOnlySequence<byte> buffer, out ulong value)
		{
			const int SIZE = sizeof(ulong);

			if(buffer.Length < SIZE)
			{
				value = 0;
				return false;
			}

			if(buffer.First.Length >= SIZE)
				return BinaryPrimitives.TryReadUInt64LittleEndian(buffer.FirstSpan, out value);

			Span<byte> local = stackalloc byte[SIZE];
			buffer.Slice(0, SIZE).CopyTo(local);
			return BinaryPrimitives.TryReadUInt64LittleEndian(local, out value);
		}
		public static bool TryGetSingleLittleEndian(this ReadOnlySequence<byte> buffer, out float value)
		{
			const int SIZE = sizeof(float);

			if(buffer.Length < SIZE)
			{
				value = 0;
				return false;
			}

			if(buffer.First.Length >= SIZE)
				return BinaryPrimitives.TryReadSingleLittleEndian(buffer.FirstSpan, out value);

			Span<byte> local = stackalloc byte[SIZE];
			buffer.Slice(0, SIZE).CopyTo(local);
			return BinaryPrimitives.TryReadSingleLittleEndian(local, out value);
		}
		public static bool TryGetDoubleLittleEndian(this ReadOnlySequence<byte> buffer, out double value)
		{
			const int SIZE = sizeof(double);

			if(buffer.Length < SIZE)
			{
				value = 0;
				return false;
			}

			if(buffer.First.Length >= SIZE)
				return BinaryPrimitives.TryReadDoubleLittleEndian(buffer.FirstSpan, out value);

			Span<byte> local = stackalloc byte[SIZE];
			buffer.Slice(0, SIZE).CopyTo(local);
			return BinaryPrimitives.TryReadDoubleLittleEndian(local, out value);
		}
		#endregion

		#region 嵌套子类
		private sealed class MemoryWrapper<T> : IMemoryOwner<T>
		{
			public static readonly IMemoryOwner<T> Empty = new MemoryWrapper<T>(Array.Empty<T>());
			public MemoryWrapper(Memory<T> memory) => this.Memory = memory;
			public Memory<T> Memory { get; }
			public void Dispose() { }
		}

		private sealed class ArrayPoolOwner<T> : IMemoryOwner<T>
		{
			private readonly int _length;
			private T[] _buffer;

			internal ArrayPoolOwner(T[] buffer, int length)
			{
				_buffer = buffer;
				_length = length;
			}

			public Memory<T> Memory => new Memory<T>(this.GetArray(), 0, _length);

			private T[] GetArray() =>
				Interlocked.CompareExchange(ref _buffer, null, null)
				?? throw new ObjectDisposedException(ToString());

			public void Dispose()
			{
				GC.SuppressFinalize(this);

				var buffer = Interlocked.Exchange(ref _buffer, null);
				if(buffer != null)
					ArrayPool<T>.Shared.Return(buffer);
			}
		}
		#endregion
	}
}
