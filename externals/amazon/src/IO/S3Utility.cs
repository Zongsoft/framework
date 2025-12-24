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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.Amazon library.
 *
 * The Zongsoft.Externals.Amazon is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Amazon is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Amazon library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;

using Zongsoft.IO;

namespace Zongsoft.Externals.Amazon.IO;

internal static class S3Utility
{
	public static (string region, string bucket) Resolve(ReadOnlySpan<char> text)
	{
		if(text.IsEmpty)
			return default;

		var index = text.IndexOf('@');
		if(index < 0)
			return (null, text.ToString());

		return (text[(index + 1)..].ToString(), text[..index].ToString());
	}

	public static FileInfo GetFileInfo(this S3FileSystem fileSystem, string region, string bucket, string path, long? size, DateTime? creation, DateTime? modification, IEnumerable<KeyValuePair<string, object>> properties = null) =>
		GetFileInfo(fileSystem, fileSystem.GetPath(region, bucket, path), size, creation, modification, properties);
	public static FileInfo GetFileInfo(this S3FileSystem fileSystem, string path, long? size, DateTime? creation, DateTime? modification, IEnumerable<KeyValuePair<string, object>> properties = null) =>
		new(path, size ?? 0, creation, modification, properties, fileSystem.GetUrl(path));

	public static FileInfo GetFileInfo(this S3FileSystem fileSystem, string region, string bucket, string path, long? size, string type, DateTime? creation, DateTime? modification, IEnumerable<KeyValuePair<string, object>> properties = null) =>
		GetFileInfo(fileSystem, fileSystem.GetPath(region, bucket, path), size, type, creation, modification, properties);
	public static FileInfo GetFileInfo(this S3FileSystem fileSystem, string path, long? size, string type, DateTime? creation, DateTime? modification, IEnumerable<KeyValuePair<string, object>> properties = null) =>
		new(path, size ?? 0, creation, modification, properties, fileSystem.GetUrl(path)) { Type = type };
}
