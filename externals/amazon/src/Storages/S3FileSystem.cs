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

using Amazon.S3;

namespace Zongsoft.Externals.Amazon.Storages;

public sealed partial class S3FileSystem : Zongsoft.IO.IFileSystem
{
	#region 构造函数
	public S3FileSystem(Microsoft.Extensions.Configuration.IConfiguration configuration = null)
	{
		this.Configuration = configuration;
		this.File = new FileProvider(this);
		this.Directory = new DirectoryProvider(this);
	}
	#endregion

	#region 公共属性
	/// <summary>获取文件目录系统的方案，始终返回“zfs.s3”。</summary>
	public string Scheme => "zfs.s3";
	public Zongsoft.IO.IFile File { get; }
	public Zongsoft.IO.IDirectory Directory { get; }
	public Microsoft.Extensions.Configuration.IConfiguration Configuration { get; set; }
	#endregion

	#region 公共方法
	public string GetUrl(string path) => string.IsNullOrEmpty(path) ? null : this.GetUrl(Zongsoft.IO.Path.Parse(path));
	public string GetUrl(IO.Path path)
	{
		if(!path.HasSegments)
			return null;

		var bucketName = path.Segments[0];

		return null;
	}
	#endregion

	private string GetUrl(string region, string bucket, string path)
	{
		return string.IsNullOrEmpty(region) ?
			$"{this.Scheme}:/{bucket}/{path?.TrimStart('/')}" :
			$"{this.Scheme}:/{bucket}/{path?.TrimStart('/')}";
	}

	#region 私有方法
	private AmazonS3Client GetClient(string region) => S3ClientFactory.GetClient(this.Configuration, region);
	private static string Resolve(ReadOnlySpan<char> text, out string region, out string bucket)
	{
		if(text.IsEmpty)
			throw new ArgumentNullException(nameof(text));

		var start = 0;
		var index = text.IndexOf('/');

		if(index == 0)
		{
			start = 1;
			index = text[start..].IndexOf('/');
		}

		if(index < 0)
		{
			region = null;
			bucket = null;
			return null;
		}

		(region, bucket) = S3Utility.Parse(text.Slice(start, index));

		if(string.IsNullOrEmpty(bucket))
			throw new ArgumentException($"The specified ‘{text}’ scheme value does not contain the bucket name.", nameof(bucket));

		return text[(index + 1)..].TrimStart('/').ToString();
	}
	#endregion
}
