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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.IO;

namespace Zongsoft.Externals.Amazon.Storages;

partial class S3FileSystem
{
	private sealed class DirectoryProvider(S3FileSystem fileSystem) : Zongsoft.IO.IDirectory
	{
		private readonly S3FileSystem _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));

		public bool Create(string path, IDictionary<string, object> properties = null) => this.CreateAsync(path, properties).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
		public async ValueTask<bool> CreateAsync(string path, IDictionary<string, object> properties = null)
		{
			path = Resolve(path, out var region, out var bucket);
			var client = _fileSystem.GetClient(region);
			var request = new global::Amazon.S3.Model.PutObjectRequest()
			{
				BucketName = bucket,
				Key = path,
			};

			var response = await client.PutObjectAsync(request);
			return response.HttpStatusCode.IsSucceed();
		}

		public bool Delete(string path, bool recursive = false) => this.DeleteAsync(path, recursive).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
		public async ValueTask<bool> DeleteAsync(string path, bool recursive = false)
		{
			path = Resolve(path, out var region, out var bucket);
			var client = _fileSystem.GetClient(region);
			var request = new global::Amazon.S3.Model.ListObjectsV2Request()
			{
				BucketName = bucket,
				Prefix = path,
			};

			var response = await client.ListObjectsV2Async(request);
			if(!response.HttpStatusCode.IsSucceed() || response.S3Objects.Count == 0)
				return false;

			var res = await client.DeleteObjectsAsync(new global::Amazon.S3.Model.DeleteObjectsRequest()
			{
				BucketName = bucket,
				Objects = response.S3Objects.Select(item => new global::Amazon.S3.Model.KeyVersion() { Key = item.Key }).ToList()
			});

			return res.HttpStatusCode.IsSucceed();
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

		public void Move(string source, string destination) => this.MoveAsync(source, destination).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
		public ValueTask MoveAsync(string source, string destination) => throw new NotImplementedException();

		public IO.DirectoryInfo GetInfo(string path) => this.GetInfoAsync(path).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
		public async ValueTask<IO.DirectoryInfo> GetInfoAsync(string path)
		{
			path = Resolve(path, out var region, out var bucket);
			var client = _fileSystem.GetClient(region);

			var response = await client.GetObjectMetadataAsync(new global::Amazon.S3.Model.GetObjectMetadataRequest()
			{
				BucketName = bucket,
				Key = path,
			});

			var response1 = await client.GetObjectAttributesAsync(new global::Amazon.S3.Model.GetObjectAttributesRequest()
			{
				BucketName = bucket,
				Key = path,
			});

			return new($"{_fileSystem.Scheme}:/{bucket}@{region}/{path}", null, null);
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

		public IEnumerable<PathInfo> GetChildren(string path) => this.GetChildren(path, null, false);
		public IEnumerable<PathInfo> GetChildren(string path, string pattern, bool recursive = false) => this.GetChildrenAsync(path, pattern, recursive).ToBlockingEnumerable();
		public IAsyncEnumerable<PathInfo> GetChildrenAsync(string path) => this.GetChildrenAsync(path, null, false);
		public async IAsyncEnumerable<PathInfo> GetChildrenAsync(string path, string pattern, bool recursive = false)
		{
			path = Resolve(path, out var region, out var bucket);
			var client = _fileSystem.GetClient(region);
			var request = new global::Amazon.S3.Model.ListObjectsV2Request()
			{
				BucketName = bucket,
				Prefix = path,
			};

			var response = await client.ListObjectsV2Async(request);
			if(!response.HttpStatusCode.IsSucceed() || response.S3Objects.Count == 0)
				yield break;

			foreach(var item in response.S3Objects)
				yield return new IO.FileInfo(
					item.Key,
					item.Size ?? 0,
					null,
					item.LastModified);
		}

		public IEnumerable<IO.DirectoryInfo> GetDirectories(string path) => this.GetDirectories(path, null, false);
		public IEnumerable<IO.DirectoryInfo> GetDirectories(string path, string pattern, bool recursive = false) => this.GetDirectoriesAsync(path, pattern, recursive).ToBlockingEnumerable();
		public IAsyncEnumerable<IO.DirectoryInfo> GetDirectoriesAsync(string path) => this.GetDirectoriesAsync(path, null, false);
		public async IAsyncEnumerable<IO.DirectoryInfo> GetDirectoriesAsync(string path, string pattern, bool recursive = false)
		{
			path = Resolve(path, out var region, out var bucket);
			var client = _fileSystem.GetClient(region);
			var request = new global::Amazon.S3.Model.ListObjectsV2Request()
			{
				BucketName = bucket,
				Prefix = path,
			};

			var response = await client.ListObjectsV2Async(request);
			if(!response.HttpStatusCode.IsSucceed() || response.S3Objects.Count == 0)
				yield break;

			foreach(var item in response.S3Objects)
			{
				if(item.Size.HasValue && item.Size.Value > 0)
					continue;

				yield return new IO.DirectoryInfo(
					item.Key,
					null,
					item.LastModified);
			}
		}

		public IEnumerable<IO.FileInfo> GetFiles(string path) => this.GetFiles(path, null, false);
		public IEnumerable<IO.FileInfo> GetFiles(string path, string pattern, bool recursive = false) => this.GetFilesAsync(path, pattern, recursive).ToBlockingEnumerable();
		public IAsyncEnumerable<IO.FileInfo> GetFilesAsync(string path) => this.GetFilesAsync(path, null, false);
		public async IAsyncEnumerable<IO.FileInfo> GetFilesAsync(string path, string pattern, bool recursive = false)
		{
			path = Resolve(path, out var region, out var bucket);
			var client = _fileSystem.GetClient(region);
			var request = new global::Amazon.S3.Model.ListObjectsV2Request()
			{
				BucketName = bucket,
				Prefix = path,
			};

			var response = await client.ListObjectsV2Async(request);
			if(!response.HttpStatusCode.IsSucceed() || response.S3Objects.Count == 0)
				yield break;

			foreach(var item in response.S3Objects)
			{
				if(item.Size == null || item.Size == 0)
					continue;

				yield return new IO.FileInfo(
					item.Key,
					item.Size ?? 0,
					null,
					item.LastModified);
			}
		}
	}
}
