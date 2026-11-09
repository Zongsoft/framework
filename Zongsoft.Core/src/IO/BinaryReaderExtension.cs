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
using System.Text;
using System.Buffers;
using System.Threading.Tasks;

namespace Zongsoft.IO;

public static class BinaryReaderExtension
{
	#region 常量定义
	private const int BUFFER_SIZE = 1024;
	#endregion

	public static void CopyTo(this BinaryReader reader, Stream destination, int bufferSize = BUFFER_SIZE)
	{
		if(reader == null)
			throw new ArgumentNullException(nameof(reader));
		if(destination == null)
			throw new ArgumentNullException(nameof(destination));

		if(!destination.CanWrite)
			throw new NotSupportedException(Properties.Resources.Stream_NotWritable_Message);

		var buffer = ArrayPool<byte>.Shared.Rent(Math.Max(bufferSize, BUFFER_SIZE));

		try
		{
			int bufferRead;

			while((bufferRead = reader.Read(buffer, 0, buffer.Length)) > 0)
			{
				destination.Write(buffer, 0, bufferRead);
			}
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(buffer);
		}
	}

	public static async ValueTask CopyToAsync(this BinaryReader reader, Stream destination, int bufferSize = BUFFER_SIZE)
	{
		if(reader == null)
			throw new ArgumentNullException(nameof(reader));
		if(destination == null)
			throw new ArgumentNullException(nameof(destination));

		if(!destination.CanWrite)
			throw new NotSupportedException(Properties.Resources.Stream_NotWritable_Message);

		var buffer = ArrayPool<byte>.Shared.Rent(Math.Max(bufferSize, BUFFER_SIZE));

		try
		{
			int bufferRead;

			while((bufferRead = reader.Read(buffer, 0, buffer.Length)) > 0)
			{
				await destination.WriteAsync(buffer.AsMemory(0, bufferRead));
			}
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(buffer);
		}
	}
}
