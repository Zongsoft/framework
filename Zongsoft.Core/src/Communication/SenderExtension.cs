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
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Communication;

public static class SenderExtension
{
	public static void Send(this ISender sender, byte[] data)
	{
		ArgumentNullException.ThrowIfNull(sender);
		ArgumentNullException.ThrowIfNull(data);
		sender.SendAsync(data).AsTask().GetAwaiter().GetResult();
	}
	public static void Send(this ISender sender, byte[] data, int offset)
	{
		ArgumentNullException.ThrowIfNull(sender);
		ArgumentNullException.ThrowIfNull(data);
		sender.SendAsync(data.AsMemory(offset)).AsTask().GetAwaiter().GetResult();
	}
	public static void Send(this ISender sender, byte[] data, int offset, int count)
	{
		ArgumentNullException.ThrowIfNull(sender);
		ArgumentNullException.ThrowIfNull(data);
		sender.SendAsync(data.AsMemory(offset, count)).AsTask().GetAwaiter().GetResult();
	}
	public static void Send(this ISender sender, string text, Encoding encoding = null)
	{
		ArgumentNullException.ThrowIfNull(sender);

		if(string.IsNullOrEmpty(text))
			return;

		encoding ??= Encoding.UTF8;
		var count = encoding.GetByteCount(text);
		var buffer = ArrayPool<byte>.Shared.Rent(count);

		try
		{
			var written = encoding.GetBytes(text.AsSpan(), buffer);
			sender.SendAsync(buffer.AsMemory(0, written)).AsTask().GetAwaiter().GetResult();
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(buffer, true);
		}
	}
	public static void Send(this ISender sender, IMemoryOwner<byte> data)
	{
		ArgumentNullException.ThrowIfNull(sender);
		ArgumentNullException.ThrowIfNull(data);

		try
		{
			sender.SendAsync(data.Memory).AsTask().GetAwaiter().GetResult();
		}
		finally { data.Dispose(); }
	}

	public static ValueTask SendAsync(this ISender sender, byte[] data, CancellationToken cancellation = default)
	{
		ArgumentNullException.ThrowIfNull(sender);
		ArgumentNullException.ThrowIfNull(data);
		return sender.SendAsync(data.AsMemory(), cancellation);
	}
	public static ValueTask SendAsync(this ISender sender, byte[] data, int offset, CancellationToken cancellation = default)
	{
		ArgumentNullException.ThrowIfNull(sender);
		ArgumentNullException.ThrowIfNull(data);
		return sender.SendAsync(data.AsMemory(offset), cancellation);
	}
	public static ValueTask SendAsync(this ISender sender, byte[] data, int offset, int count, CancellationToken cancellation = default)
	{
		ArgumentNullException.ThrowIfNull(sender);
		ArgumentNullException.ThrowIfNull(data);
		return sender.SendAsync(data.AsMemory(offset, count), cancellation);
	}
	public static async ValueTask SendAsync(this ISender sender, string text, Encoding encoding = null, CancellationToken cancellation = default)
	{
		ArgumentNullException.ThrowIfNull(sender);

		if(string.IsNullOrEmpty(text))
			return;

		encoding ??= Encoding.UTF8;
		var count = encoding.GetByteCount(text);
		var buffer = ArrayPool<byte>.Shared.Rent(count);

		try
		{
			var written = encoding.GetBytes(text.AsSpan(), buffer);
			await sender.SendAsync(buffer.AsMemory(0, written), cancellation);
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(buffer, true);
		}
	}
	public static ValueTask SendAsync(this ISender sender, IMemoryOwner<byte> data, CancellationToken cancellation = default)
	{
		static async ValueTask Awaited(IMemoryOwner<byte> mmemory, ValueTask write)
		{
			using(mmemory)
			{
				await write;
			}
		}

		ArgumentNullException.ThrowIfNull(sender);
		ArgumentNullException.ThrowIfNull(data);

		try
		{
			var result = sender.SendAsync(data.Memory, cancellation);

			if(result.IsCompletedSuccessfully)
				return default;

			var final = Awaited(data, result);
			data = null;
			return final;
		}
		finally
		{
			using(data) { }
		}
	}
}
