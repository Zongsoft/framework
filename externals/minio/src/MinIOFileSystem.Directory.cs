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
 * This file is part of Zongsoft.Externals.MinIO library.
 *
 * The Zongsoft.Externals.MinIO is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.MinIO is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.MinIO library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Minio;
using Minio.DataModel;
using Minio.DataModel.Args;

using Zongsoft.IO;

namespace Zongsoft.Externals.MinIO;

partial class MinIOFileSystem
{
	private sealed class DirectoryProvider(MinIOFileSystem fileSystem) : Zongsoft.IO.IDirectory
	{
		private readonly MinIOFileSystem _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));

		public bool Create(string path, IDictionary<string, object> properties = null) => this.CreateAsync(path, properties).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
		public async ValueTask<bool> CreateAsync(string path, IDictionary<string, object> properties = null)
		{
			path = Resolve(path, out var region, out var bucket);
			var client = _fileSystem.GetClient(region);
			var request = new PutObjectArgs();
			request.WithBucket(bucket);
			request.WithFileName(path);

			var response = await client.PutObjectAsync(request);
			return response.ResponseStatusCode.IsSucceed();
		}

		public bool Delete(string path, bool recursive = false) => this.DeleteAsync(path, recursive).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
		public async ValueTask<bool> DeleteAsync(string path, bool recursive = false)
		{
			path = Resolve(path, out var region, out var bucket);
			var client = _fileSystem.GetClient(region);
			var request = new ListObjectsArgs();
			request.WithBucket(bucket);
			request.WithPrefix(path);

			var paths = new List<string>();
			var items = client.ListObjectsEnumAsync(request);
			await foreach(var item in items)
				paths.Add(item.Key);

			if(paths.Count == 0)
				return false;

			var arguments = new RemoveObjectsArgs();
			arguments.WithBucket(bucket);
			arguments.WithObjects(paths);
			var res = await client.RemoveObjectsAsync(arguments);
			return res.Count == 0;
		}

		public bool Exists(string path) => this.ExistsAsync(path).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
		public async ValueTask<bool> ExistsAsync(string path)
		{
			path = Resolve(path, out var region, out var bucket);
			var client = _fileSystem.GetClient(region);
			var request = new GetObjectArgs();
			request.WithBucket(bucket);
			request.WithObject(path);

			var response = await client.GetObjectAsync(request);
			return response.ObjectName != null;
		}

		public void Move(string source, string destination) => this.MoveAsync(source, destination).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
		public ValueTask MoveAsync(string source, string destination) => throw new NotImplementedException();

		public IO.DirectoryInfo GetInfo(string path) => this.GetInfoAsync(path).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
		public async ValueTask<IO.DirectoryInfo> GetInfoAsync(string path)
		{
			path = Resolve(path, out var region, out var bucket);
			var client = _fileSystem.GetClient(region);

			var request = new GetObjectArgs();
			request.WithBucket(bucket);
			request.WithObject(path);
			var response = await client.GetObjectAsync(request);

			var taggingArgument = new GetObjectTagsArgs();
			taggingArgument.WithBucket(bucket);
			taggingArgument.WithObject(path);
			var response1 = await client.GetObjectTagsAsync(taggingArgument);

			return new($"{_fileSystem.Scheme}:/{bucket}@{region}/{path}", null, null);
		}

		public bool SetInfo(string path, IDictionary<string, object> properties) => this.SetInfoAsync(path, properties).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
		public async ValueTask<bool> SetInfoAsync(string path, IDictionary<string, object> properties)
		{
			if(properties == null)
				return false;

			path = Resolve(path, out var region, out var bucket);
			var client = _fileSystem.GetClient(region);
			var request = new SetObjectTagsArgs();
			request.WithBucket(bucket);
			request.WithObject(path);

			var tagging = new Minio.DataModel.Tags.Tagging();
			foreach(var property in properties)
				tagging.Tags.TryAdd(property.Key, Zongsoft.Common.Convert.ConvertValue<string>(property.Value));
			request.WithTagging(tagging);

			try
			{
				await client.SetObjectTagsAsync(request);
				return true;
			}
			catch { return false; }
		}

		public IEnumerable<PathInfo> GetChildren(string path) => this.GetChildren(path, null, false);
		public IEnumerable<PathInfo> GetChildren(string path, string pattern, bool recursive = false) => this.GetChildrenAsync(path, pattern, recursive).ToBlockingEnumerable();
		public IAsyncEnumerable<PathInfo> GetChildrenAsync(string path) => this.GetChildrenAsync(path, null, false);
		public async IAsyncEnumerable<PathInfo> GetChildrenAsync(string path, string pattern, bool recursive = false)
		{
			path = Resolve(path, out var region, out var bucket);
			var client = _fileSystem.GetClient(region);
			var request = new ListObjectsArgs();
			request.WithBucket(bucket);
			request.WithPrefix(path);

			var items = client.ListObjectsEnumAsync(request);

			await foreach(var item in items)
				yield return item.IsDir ?
					new IO.DirectoryInfo(item.Key, null, item.LastModifiedDateTime) :
					new IO.FileInfo(item.Key, (uint)item.Size, null, item.LastModifiedDateTime);
		}

		public IEnumerable<IO.DirectoryInfo> GetDirectories(string path) => this.GetDirectories(path, null, false);
		public IEnumerable<IO.DirectoryInfo> GetDirectories(string path, string pattern, bool recursive = false) => this.GetDirectoriesAsync(path, pattern, recursive).ToBlockingEnumerable();
		public IAsyncEnumerable<IO.DirectoryInfo> GetDirectoriesAsync(string path) => this.GetDirectoriesAsync(path, null, false);
		public async IAsyncEnumerable<IO.DirectoryInfo> GetDirectoriesAsync(string path, string pattern, bool recursive = false)
		{
			path = Resolve(path, out var region, out var bucket);
			var client = _fileSystem.GetClient(region);
			var request = new ListObjectsArgs();
			request.WithBucket(bucket);
			request.WithPrefix(path);

			var items = client.ListObjectsEnumAsync(request);

			await foreach(var item in items)
			{
				if(item.IsDir)
					yield return new IO.DirectoryInfo(item.Key, null, item.LastModifiedDateTime);
			}
		}

		public IEnumerable<IO.FileInfo> GetFiles(string path) => this.GetFiles(path, null, false);
		public IEnumerable<IO.FileInfo> GetFiles(string path, string pattern, bool recursive = false) => this.GetFilesAsync(path, pattern, recursive).ToBlockingEnumerable();
		public IAsyncEnumerable<IO.FileInfo> GetFilesAsync(string path) => this.GetFilesAsync(path, null, false);
		public async IAsyncEnumerable<IO.FileInfo> GetFilesAsync(string path, string pattern, bool recursive = false)
		{
			path = Resolve(path, out var region, out var bucket);
			var client = _fileSystem.GetClient(region);
			var request = new ListObjectsArgs();
			request.WithBucket(bucket);
			request.WithPrefix(path);

			var items = client.ListObjectsEnumAsync(request);

			await foreach(var item in items)
			{
				if(!item.IsDir)
					yield return new IO.FileInfo(item.Key, (uint)item.Size, null, item.LastModifiedDateTime);
			}
		}
	}
}
