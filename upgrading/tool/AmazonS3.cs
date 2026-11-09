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
 * The above copyright notice and this permission notice shall be included in all
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Runtime;

namespace Zongsoft.Upgrading;

internal class AmazonS3 : IDisposable
{
	#region 成员字段
	private readonly AmazonS3Client _client;
	#endregion

	#region 构造函数
	public AmazonS3(string url, string access, string secret, string region = null)
	{
		var config = new AmazonS3Config()
		{
			ServiceURL = url,
			UseHttp = true,
			ForcePathStyle = true,
			AllowAutoRedirect = true,
			ClientAppId = "Zongsoft.Upgrading.Packager",
			DefaultAWSCredentials = new BasicAWSCredentials(access, secret),
		};

		if(!string.IsNullOrEmpty(region))
			config.RegionEndpoint = RegionEndpoint.GetBySystemName(region);

		_client = new AmazonS3Client(config);
	}
	#endregion

	#region 公共方法
	public async ValueTask UploadAsync(Stream source, string destination, CancellationToken cancellation = default)
	{
		(var bucket, var path) = Resolve(destination);

		var response = await _client.PutObjectAsync(new PutObjectRequest
		{
			Key = path,
			BucketName = bucket,
			InputStream = source,
			AutoCloseStream = true,
		}, cancellation);
	}
	#endregion

	#region 私有方法
	private static (string bucket, string path) Resolve(string destination)
	{
		if(string.IsNullOrEmpty(destination))
			return default;

		destination = destination.Trim('/');
		var index = destination.IndexOf('/');

		return index < 0 ?
			(destination, string.Empty):
			(destination[..index], destination[(index + 1)..]);
	}
	#endregion

	#region 处置方法
	public void Dispose()
	{
		this.Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if(disposing)
		{
			_client.Dispose();
		}
	}
	#endregion
}
