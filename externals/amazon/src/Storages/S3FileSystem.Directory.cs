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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Amazon.S3;
using Amazon.S3.Model;

namespace Zongsoft.Externals.Amazon.Storages;

partial class S3FileSystem
{
	private sealed class DirectoryProvider(S3FileSystem fileSystem) : Zongsoft.IO.IDirectory
	{
		private readonly S3FileSystem _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));

		public bool Create(string path, IDictionary<string, object> properties = null) => true;
		public ValueTask<bool> CreateAsync(string path, IDictionary<string, object> properties = null) => ValueTask.FromResult(true);

		public bool Delete(string path, bool recursive = false) => this.DeleteAsync(path, recursive).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
		public async ValueTask<bool> DeleteAsync(string path, bool recursive = false)
		{
			path = Resolve(path, out var region, out var bucket);
			var client = _fileSystem.GetClient(region);

			try
			{
				var count = 0;
				string continuation = null;

				do
				{
					var response = await client.ListObjectsV2Async(new ListObjectsV2Request()
					{
						BucketName = bucket,
						Prefix = path,
						ContinuationToken = continuation,
					});

					if(response.HttpStatusCode.IsSucceed() && response.S3Objects != null && response.S3Objects.Count > 0)
					{
						var deleted = await client.DeleteObjectsAsync(new DeleteObjectsRequest()
						{
							BucketName = bucket,
							Objects = [.. response.S3Objects.Select(obj => new KeyVersion() { Key = obj.Key })],
						});

						if(deleted.DeletedObjects != null)
							count += deleted.DeletedObjects.Count;
					}

					continuation = response.NextContinuationToken;
				} while(continuation != null);

				return count > 0;
			}
			catch(AmazonS3Exception ex) when(ex.StatusCode == System.Net.HttpStatusCode.NotFound)
			{
				return false;
			}
		}

		public bool Exists(string path) => this.ExistsAsync(path).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
		public async ValueTask<bool> ExistsAsync(string path)
		{
			path = Resolve(path, out var region, out var bucket);
			var client = _fileSystem.GetClient(region);

			try
			{
				var response = await client.ListObjectsV2Async(new ListObjectsV2Request()
				{
					BucketName = bucket,
					Prefix = path,
					MaxKeys = 1,
				});

				return response.HttpStatusCode.IsSucceed() && response.KeyCount > 0;
			}
			catch(AmazonS3Exception ex) when(ex.StatusCode == System.Net.HttpStatusCode.NotFound)
			{
				return false;
			}
		}

		public void Move(string source, string destination) => this.MoveAsync(source, destination).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
		public async ValueTask MoveAsync(string source, string destination)
		{
			var srcPath = Resolve(source, out var srcRegion, out var srcBucket);
			var destPath = Resolve(destination, out var destRegion, out var destBucket);

			if(string.Equals(srcRegion, destRegion, StringComparison.OrdinalIgnoreCase) && destPath.StartsWith(srcPath))
				return;

			try
			{
				var count = 0;
				string continuation = null;
				var srcClient = _fileSystem.GetClient(srcRegion);

				do
				{
					var response = await srcClient.ListObjectsV2Async(new ListObjectsV2Request()
					{
						BucketName = srcBucket,
						Prefix = srcPath,
						ContinuationToken = continuation,
					});

					if(!response.HttpStatusCode.IsSucceed() || response.S3Objects == null || response.S3Objects.Count == 0)
						return;

					if(string.Equals(srcRegion, destRegion, StringComparison.OrdinalIgnoreCase))
					{
						foreach(var obj in response.S3Objects)
						{
							var result = await srcClient.CopyObjectAsync(new CopyObjectRequest()
							{
								SourceBucket = srcBucket,
								SourceKey = obj.Key,
								DestinationBucket = destBucket,
								DestinationKey = System.IO.Path.Combine(destPath, System.IO.Path.GetFileName(obj.Key)),
							});

							count += result.HttpStatusCode.IsSucceed() ? 1 : 0;
						}
					}
					else
					{
						var destClient = _fileSystem.GetClient(destRegion);

						foreach(var obj in response.S3Objects)
						{
							var getResponse = await srcClient.GetObjectAsync(new GetObjectRequest()
							{
								BucketName = srcBucket,
								Key = obj.Key,
							});

							if(getResponse.HttpStatusCode.IsSucceed() && getResponse.ResponseStream != null)
							{
								var putResponse = await destClient.PutObjectAsync(new PutObjectRequest()
								{
									BucketName = destBucket,
									Key = System.IO.Path.Combine(destPath, System.IO.Path.GetFileName(obj.Key)),
									InputStream = getResponse.ResponseStream,
								});

								count += putResponse.HttpStatusCode.IsSucceed() ? 1 : 0;
								getResponse.ResponseStream.Dispose();

								await srcClient.DeleteObjectAsync(new DeleteObjectRequest()
								{
									BucketName = srcBucket,
									Key = obj.Key,
								});
							}
						}
					}

					continuation = response.NextContinuationToken;
				} while(continuation != null);
			}
			catch(AmazonS3Exception ex) when(ex.StatusCode == System.Net.HttpStatusCode.NotFound)
			{
				return;
			}
		}

		public IO.DirectoryInfo GetInfo(string path) => this.GetInfoAsync(path).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
		public ValueTask<IO.DirectoryInfo> GetInfoAsync(string path) => ValueTask.FromResult<IO.DirectoryInfo>(null);

		public bool SetInfo(string path, IDictionary<string, object> properties) => this.SetInfoAsync(path, properties).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
		public ValueTask<bool> SetInfoAsync(string path, IDictionary<string, object> properties) => ValueTask.FromResult(false);

		public IEnumerable<IO.PathInfo> GetChildren(string path) => this.GetChildren(path, null, false);
		public IEnumerable<IO.PathInfo> GetChildren(string path, string pattern, bool recursive = false) => this.GetChildrenAsync(path, pattern, recursive).ToBlockingEnumerable();
		public IAsyncEnumerable<IO.PathInfo> GetChildrenAsync(string path) => this.GetChildrenAsync(path, null, false);
		public async IAsyncEnumerable<IO.PathInfo> GetChildrenAsync(string path, string pattern, bool recursive = false)
		{
			path = Resolve(path, out var region, out var bucket);
			var client = _fileSystem.GetClient(region);

			string continuation = null;
			ListObjectsV2Response response;

			do
			{
				try
				{
					response = await client.ListObjectsV2Async(new()
					{
						BucketName = bucket,
						Prefix = path,
						ContinuationToken = continuation,
					});

					if(!response.HttpStatusCode.IsSucceed() || response.KeyCount == 0 || response.S3Objects == null || response.S3Objects.Count == 0)
						yield break;

					continuation = response.NextContinuationToken;
				}
				catch(AmazonS3Exception ex) when(ex.StatusCode == System.Net.HttpStatusCode.NotFound)
				{
					yield break;
				}

				foreach(var item in response.S3Objects)
					yield return new IO.FileInfo(
						_fileSystem.GetPath(region, bucket, item.Key),
						item.Size ?? 0,
						null,
						item.LastModified);
			} while(continuation != null);
		}

		public IEnumerable<IO.DirectoryInfo> GetDirectories(string path) => this.GetDirectories(path, null, false);
		public IEnumerable<IO.DirectoryInfo> GetDirectories(string path, string pattern, bool recursive = false) => this.GetDirectoriesAsync(path, pattern, recursive).ToBlockingEnumerable();
		public IAsyncEnumerable<IO.DirectoryInfo> GetDirectoriesAsync(string path) => this.GetDirectoriesAsync(path, null, false);
		public async IAsyncEnumerable<IO.DirectoryInfo> GetDirectoriesAsync(string path, string pattern, bool recursive = false)
		{
			var children = this.GetChildrenAsync(path, pattern, recursive);

			await foreach(var child in children)
			{
				if(child.IsDirectory)
					yield return (IO.DirectoryInfo)child;
			}
		}

		public IEnumerable<IO.FileInfo> GetFiles(string path) => this.GetFiles(path, null, false);
		public IEnumerable<IO.FileInfo> GetFiles(string path, string pattern, bool recursive = false) => this.GetFilesAsync(path, pattern, recursive).ToBlockingEnumerable();
		public IAsyncEnumerable<IO.FileInfo> GetFilesAsync(string path) => this.GetFilesAsync(path, null, false);
		public async IAsyncEnumerable<IO.FileInfo> GetFilesAsync(string path, string pattern, bool recursive = false)
		{
			var children = this.GetChildrenAsync(path, pattern, recursive);

			await foreach(var child in children)
			{
				if(child.IsFile)
					yield return (IO.FileInfo)child;
			}
		}
	}
}
