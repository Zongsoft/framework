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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Zongsoft.Externals.Amazon.Storages;

partial class S3FileSystem
{
	private sealed class FileProvider(S3FileSystem fileSystem) : Zongsoft.IO.IFile
	{
		private readonly S3FileSystem _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));

		public void Copy(string source, string destination) => this.Copy(source, destination, true);
		public void Copy(string source, string destination, bool overwrite) => this.CopyAsync(source, destination, overwrite).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();

		public ValueTask CopyAsync(string source, string destination) => this.CopyAsync(source, destination, false);
		public ValueTask CopyAsync(string source, string destination, bool overwrite)
		{
			var srcPath = Resolve(source, out var srcRegion, out var srcBucket);
			var destPath = Resolve(destination, out var destRegion, out var destBucket);

			if(!string.Equals(srcRegion, destRegion))
				throw new InvalidOperationException($"");

			var client = _fileSystem.GetClient(srcRegion);
			var request = new global::Amazon.S3.Model.CopyObjectRequest()
			{
				SourceBucket = srcBucket,
				SourceKey = srcPath,
				DestinationBucket = destBucket,
				DestinationKey = destPath,
			};

			return new ValueTask(client.CopyObjectAsync(request));
		}

		public void Move(string source, string destination) => this.MoveAsync(source, destination).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
		public async ValueTask MoveAsync(string source, string destination)
		{
			var srcPath = Resolve(source, out var srcRegion, out var srcBucket);
			var destPath = Resolve(destination, out var destRegion, out var destBucket);

			if(!string.Equals(srcRegion, destRegion))
				throw new InvalidOperationException($"");

			var client = _fileSystem.GetClient(srcRegion);
			var request = new global::Amazon.S3.Model.CopyObjectRequest()
			{
				SourceBucket = srcBucket,
				SourceKey = srcPath,
				DestinationBucket = destBucket,
				DestinationKey = destPath,
			};

			var response = await client.CopyObjectAsync(request);
			if(response.HttpStatusCode.IsSucceed())
				await client.DeleteObjectAsync(srcBucket, srcPath);
		}

		public bool Delete(string path) => this.DeleteAsync(path).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
		public async ValueTask<bool> DeleteAsync(string path)
		{
			path = Resolve(path, out var region, out var bucket);
			var client = _fileSystem.GetClient(region);
			var request = new global::Amazon.S3.Model.DeleteObjectRequest()
			{
				BucketName = bucket,
				Key = path,
			};

			var response = await client.DeleteObjectAsync(request);
			return response.HttpStatusCode.IsSucceed();
		}

		public bool Exists(string path) => this.ExistsAsync(path).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
		public async ValueTask<bool> ExistsAsync(string path)
		{
			path = Resolve(path, out var region, out var bucket);
			var client = _fileSystem.GetClient(region);
			var request = new global::Amazon.S3.Model.GetObjectMetadataRequest()
			{
				BucketName = bucket,
				Key = path,
			};

			var response = await client.GetObjectMetadataAsync(request);
			return response.HttpStatusCode.IsSucceed();
		}

		public IO.FileInfo GetInfo(string path) => this.GetInfoAsync(path).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
		public async ValueTask<IO.FileInfo> GetInfoAsync(string path)
		{
			path = Resolve(path, out var region, out var bucket);
			var client = _fileSystem.GetClient(region);

			var response = await client.GetObjectMetadataAsync(new global::Amazon.S3.Model.GetObjectMetadataRequest()
			{
				BucketName = bucket,
				Key = path.Trim('/'),
			});

			var response1 = await client.GetObjectAttributesAsync(new global::Amazon.S3.Model.GetObjectAttributesRequest()
			{
				BucketName = bucket,
				Key = path,
			});

			return new($"{_fileSystem.Scheme}:/{bucket}@{region}/{path}", 0, null, null);
		}

		public bool SetInfo(string path, IDictionary<string, object> properties) => this.SetInfoAsync(path, properties).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
		public async ValueTask<bool> SetInfoAsync(string path, IDictionary<string, object> properties)
		{
			if(properties == null)
				return false;

			path = Resolve(path, out var region, out var bucket);
			var client = _fileSystem.GetClient(region);
			var request = new global::Amazon.S3.Model.PutObjectRequest()
			{
				BucketName = bucket,
				Key = path,
			};

			foreach(var property in properties)
				request.Metadata.Add(property.Key, Zongsoft.Common.Convert.ConvertValue<string>(property.Value));

			var response = await client.PutObjectAsync(request);
			return response.HttpStatusCode.IsSucceed();
		}

		public Stream Open(string path, IDictionary<string, object> properties = null) => this.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, properties);
		public Stream Open(string path, FileMode mode, IDictionary<string, object> properties = null) => this.Open(path, mode, FileAccess.ReadWrite, FileShare.None, properties);
		public Stream Open(string path, FileMode mode, FileAccess access, IDictionary<string, object> properties = null) => this.Open(path, mode, access, FileShare.None, properties);
		public Stream Open(string path, FileMode mode, FileAccess access, FileShare share, IDictionary<string, object> properties = null)
		{
			path = Resolve(path, out var region, out var bucket);
			var client = _fileSystem.GetClient(region);
			var request = new global::Amazon.S3.Model.GetObjectRequest()
			{
				BucketName = bucket,
				Key = path,
			};

			var response = client.GetObjectAsync(request).ConfigureAwait(false).GetAwaiter().GetResult();
			return response.ResponseStream;
		}
	}
}
