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
using System.Buffers;
using System.Threading;

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
