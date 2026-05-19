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
 * The MIT License (MIT)
 *
 * Copyright (C) 2020-2026 Zongsoft Corporation <http://www.zongsoft.com>
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
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
