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
 * Copyright (C) 2010-2024 Zongsoft Studio <http://www.zongsoft.com>
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
using System.IO.Compression;

namespace Zongsoft.IO.Compression;

public static class Compressor
{
	#region 常量定义
	public const string Brotli = nameof(Brotli);
	public const string GZip = nameof(GZip);
	public const string ZLib = nameof(ZLib);
	public const string Deflate = nameof(Deflate);
	#endregion

	#region 公共方法
	public static byte[] Compress(byte[] data, CompressionLevel level = CompressionLevel.Optimal) => Compress(null, data, level);
	public static byte[] Compress(string compressor, byte[] data, CompressionLevel level = CompressionLevel.Optimal)
	{
		if(data == null || data.Length == 0)
			return data;

		using var destination = new MemoryStream();
		var count = Compress(compressor, data, destination, level);
		return count > 0 ? destination.ToArray() : [];
	}

	public static long Compress(byte[] data, Stream destination, CompressionLevel level = CompressionLevel.Optimal) => Compress(null, data, destination, level);
	public static long Compress(string compressor, byte[] data, Stream destination, CompressionLevel level = CompressionLevel.Optimal)
	{
		if(destination == null)
			throw new ArgumentNullException(nameof(destination));

		if(data == null || data.Length == 0)
			return 0;

		var position = Math.Max(destination.Position, 0);
		using var source = new MemoryStream(data);
		using var compression = CreateCompressor(compressor, destination, level);
		source.CopyTo(compression);
		compression.Close();
		destination.Seek(position, SeekOrigin.Begin);
		return destination.Length - position;
	}

	public static byte[] Decompress(byte[] data) => Decompress(null, data);
	public static byte[] Decompress(string compressor, byte[] data)
	{
		if(data == null || data.Length == 0)
			return data;

		using var destination = new MemoryStream();
		var count = Decompress(compressor, data, destination);
		return count > 0 ? destination.ToArray() : [];
	}

	public static long Decompress(byte[] data, Stream destination) => Decompress(null, data, destination);
	public static long Decompress(string compressor, byte[] data, Stream destination)
	{
		if(destination == null)
			throw new ArgumentNullException(nameof(destination));

		if(data == null || data.Length == 0)
			return 0;

		var position = Math.Max(destination.Position, 0);
		using var source = new MemoryStream(data);
		using var compression = CreateDecompressor(compressor, source);
		compression.CopyTo(destination);
		destination.Seek(position, SeekOrigin.Begin);
		return destination.Length - position;
	}
	#endregion

	#region 私有方法
	private static Stream CreateCompressor(string name, Stream destination, CompressionLevel level = CompressionLevel.Optimal)
	{
		if(string.IsNullOrEmpty(name))
			return new BrotliStream(destination, level, true);

		return name.ToLowerInvariant() switch
		{
			"br" or "brotli" => new BrotliStream(destination, level, true),
			"gzip" => new GZipStream(destination, level, true),
			"zlib" => new ZLibStream(destination, level, true),
			"deflate" => new DeflateStream(destination, level, true),
			_ => throw new InvalidOperationException($"The specified '{name}' compressor is undefined."),
		};
	}

	private static Stream CreateDecompressor(string name, Stream source)
	{
		if(string.IsNullOrEmpty(name))
			return new BrotliStream(source, CompressionMode.Decompress);

		return name.ToLowerInvariant() switch
		{
			"br" or "brotli" => new BrotliStream(source, CompressionMode.Decompress),
			"gzip" => new GZipStream(source, CompressionMode.Decompress),
			"zlib" => new ZLibStream(source, CompressionMode.Decompress),
			"deflate" => new DeflateStream(source, CompressionMode.Decompress),
			_ => throw new InvalidOperationException($"The specified '{name}' decompressor is undefined."),
		};
	}
	#endregion
}
