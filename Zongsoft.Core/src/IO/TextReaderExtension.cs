﻿/*
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
using System.IO;
using System.Text;
using System.Buffers;
using System.Threading.Tasks;

namespace Zongsoft.IO;

public static class TextReaderExtension
{
	#region 常量定义
	private const int BUFFER_SIZE = 1024;
	#endregion

	public static void CopyTo(this TextReader reader, Stream destination, int bufferSize = BUFFER_SIZE) => CopyTo(reader, destination, null, bufferSize);
	public static void CopyTo(this TextReader reader, Stream destination, Encoding encoding, int bufferSize = BUFFER_SIZE)
	{
		if(reader == null)
			throw new ArgumentNullException(nameof(reader));
		if(destination == null)
			throw new ArgumentNullException(nameof(destination));

		if(!destination.CanWrite)
			throw new NotSupportedException("The destination stream does not support writing.");

		if(encoding == null)
			encoding = Encoding.UTF8;

		var buffer = ArrayPool<char>.Shared.Rent(Math.Max(bufferSize, BUFFER_SIZE));

		try
		{
			int bufferRead;

			while((bufferRead = reader.Read(buffer, 0, buffer.Length)) > 0)
			{
				var bytes = encoding.GetBytes(buffer, 0, bufferRead);
				destination.Write(bytes, 0, bytes.Length);
			}
		}
		finally
		{
			ArrayPool<char>.Shared.Return(buffer);
		}
	}

	public static ValueTask CopyToAsync(this TextReader reader, Stream destination, int bufferSize = BUFFER_SIZE) => CopyToAsync(reader, destination, null, bufferSize);
	public static async ValueTask CopyToAsync(this TextReader reader, Stream destination, Encoding encoding, int bufferSize = BUFFER_SIZE)
	{
		if(reader == null)
			throw new ArgumentNullException(nameof(reader));
		if(destination == null)
			throw new ArgumentNullException(nameof(destination));

		if(!destination.CanWrite)
			throw new NotSupportedException("The destination stream does not support writing.");

		if(encoding == null)
			encoding = Encoding.UTF8;

		var buffer = ArrayPool<char>.Shared.Rent(Math.Max(bufferSize, BUFFER_SIZE));

		try
		{
			int bufferRead;

			while((bufferRead = reader.Read(buffer, 0, buffer.Length)) > 0)
			{
				var bytes = encoding.GetBytes(buffer, 0, bufferRead);
				await destination.WriteAsync(bytes);
			}
		}
		finally
		{
			ArrayPool<char>.Shared.Return(buffer);
		}
	}
}
