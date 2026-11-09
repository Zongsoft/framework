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
using System.Text;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Communication;

public static class SenderExtension
{
	#region 静态方法
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
	public static async ValueTask SendAsync(this ISender sender, IMemoryOwner<byte> data, CancellationToken cancellation = default)
	{
		ArgumentNullException.ThrowIfNull(sender);
		ArgumentNullException.ThrowIfNull(data);

		using(data)
			await sender.SendAsync(data.Memory, cancellation).ConfigureAwait(false);
	}
	#endregion
}
