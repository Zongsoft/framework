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

	public static FileInfo GetFileInfo(this S3FileSystem fileSystem, string region, string bucket, string path, long? size, DateTime? creation, DateTime? modification, IEnumerable<KeyValuePair<string, string>> properties = null) =>
		GetFileInfo(fileSystem, fileSystem.GetPath(region, bucket, path), size, creation, modification, properties);
	public static FileInfo GetFileInfo(this S3FileSystem fileSystem, string path, long? size, DateTime? creation, DateTime? modification, IEnumerable<KeyValuePair<string, string>> properties = null) =>
		new(path, size ?? 0, creation, modification, properties, fileSystem.GetUrl(path));

	public static FileInfo GetFileInfo(this S3FileSystem fileSystem, string region, string bucket, string path, long? size, string type, DateTime? creation, DateTime? modification, IEnumerable<KeyValuePair<string, string>> properties = null) =>
		GetFileInfo(fileSystem, fileSystem.GetPath(region, bucket, path), size, type, creation, modification, properties);
	public static FileInfo GetFileInfo(this S3FileSystem fileSystem, string path, long? size, string type, DateTime? creation, DateTime? modification, IEnumerable<KeyValuePair<string, string>> properties = null) =>
		new(path, size ?? 0, type, creation, modification, properties, fileSystem.GetUrl(path));
}
