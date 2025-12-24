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
 * Copyright (C) 2015-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.Aliyun library.
 *
 * The Zongsoft.Externals.Aliyun is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Aliyun is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Aliyun library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Zongsoft.IO;

namespace Zongsoft.Externals.Aliyun.Storages
{
	public class StorageFileSystem : Zongsoft.IO.IFileSystem
	{
		#region 成员字段
		private Options.StorageOptions _options;
		private readonly StorageFileProvider _file;
		private readonly StorageDirectoryProvider _directory;
		private readonly ConcurrentDictionary<string, StorageClient> _pool;
		#endregion

		#region 构造函数
		public StorageFileSystem()
		{
			_file = new StorageFileProvider(this);
			_directory = new StorageDirectoryProvider(this);
			_pool = new ConcurrentDictionary<string, StorageClient>();
		}
		#endregion

		#region 公共属性
		/// <summary>获取文件目录系统的方案，始终返回“zfs.oss”。</summary>
		public string Scheme => "zfs.oss";
		public IFile File => _file;
		public IDirectory Directory => _directory;

		[Zongsoft.Configuration.Options.Options("Externals/Aliyun/OSS")]
		public Options.StorageOptions Options
		{
			get => _options;
			set => _options = value ?? throw new ArgumentNullException();
		}
		#endregion

		#region 公共方法
		public string GetUrl(string path)
		{
			if(string.IsNullOrEmpty(path))
				return null;

			return this.GetUrl(Zongsoft.IO.Path.Parse(path));
		}

		public string GetUrl(Zongsoft.IO.Path path)
		{
			if(!path.HasSegments)
				return null;

			//确认OSS对象存储配置
			var options = this.EnsureOptions();

			//获取当前路径对应的存储器配置项，注：BucketName即为路径中的第一节
			options.Buckets.TryGetValue(path.Segments[0], out var bucket);

			//获取当前路径对应的服务区域
			var region = this.GetRegion(bucket);

			return StorageServiceCenter.GetInstance(region, false).GetRequestUrl(path.FullPath, true);
		}
		#endregion

		#region 内部方法
		internal StorageClient GetClient(string bucketName)
		{
			if(string.IsNullOrEmpty(bucketName))
				throw new ArgumentNullException(nameof(bucketName));

			return _pool.GetOrAdd(bucketName, key =>
			{
				return this.CreateClient(bucketName);
			});
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.Synchronized)]
		private StorageClient CreateClient(string bucketName)
		{
			//确认OSS对象存储配置
			var options = this.EnsureOptions();

			//获取指定名称的存储器配置项
			options.Buckets.TryGetValue(bucketName, out var bucket);

			var region = this.GetRegion(bucket);
			var center = StorageServiceCenter.GetInstance(region, Aliyun.Options.GeneralOptions.Instance.IsIntranet);
			var certificate = this.GetCertificate(bucket);

			return new StorageClient(center, certificate);
		}
		#endregion

		#region 私有方法
		private ICertificate GetCertificate(Options.BucketOption bucket)
		{
			var certificate = bucket?.Certificate;

			if(string.IsNullOrWhiteSpace(certificate))
				certificate = _options?.Certificate;

			if(string.IsNullOrWhiteSpace(certificate))
				return Aliyun.Options.GeneralOptions.Instance.Certificates.Default;

			return Aliyun.Options.GeneralOptions.Instance.Certificates.GetCertificate(certificate);
		}

		private ServiceCenterName GetRegion(Options.BucketOption bucket)
		{
			return bucket?.Region ?? _options?.Region ?? Aliyun.Options.GeneralOptions.Instance.Name;
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private Options.StorageOptions EnsureOptions()
		{
			return this.Options ?? throw new InvalidOperationException("Missing required configuration of the OSS file-system(aliyun).");
		}
		#endregion

		#region 嵌套子类
		private sealed class StorageDirectoryProvider : IDirectory
		{
			#region 成员字段
			private StorageFileSystem _fileSystem;
			#endregion

			#region 私有构造
			internal StorageDirectoryProvider(StorageFileSystem fileSystem)
			{
				_fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
			}
			#endregion

			#region 公共方法
			public bool Create(string path, IEnumerable<KeyValuePair<string, string>> properties = null) => this.CreateAsync(path, properties).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
			public ValueTask<bool> CreateAsync(string path, IEnumerable<KeyValuePair<string, string>> properties, CancellationToken cancellation = default)
			{
				var directory = this.EnsureDirectoryPath(path, out var bucketName);
				var client = _fileSystem.GetClient(bucketName);
				return client.CreateAsync(directory, properties, cancellation);
			}

			public bool Delete(string path) => this.DeleteAsync(path).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
			public ValueTask<bool> DeleteAsync(string path, CancellationToken cancellation = default)
			{
				var directory = this.EnsureDirectoryPath(path, out var bucketName);
				var client = _fileSystem.GetClient(bucketName);
				return client.DeleteAsync(directory, cancellation);
			}

			public void Move(string source, string destination) => throw new NotSupportedException();
			public ValueTask MoveAsync(string source, string destination, CancellationToken cancellation = default) => throw new NotSupportedException();

			public bool Exists(string path) => this.ExistsAsync(path).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
			public ValueTask<bool> ExistsAsync(string path, CancellationToken cancellation = default)
			{
				var directory = this.EnsureDirectoryPath(path, out var bucketName);
				var client = _fileSystem.GetClient(bucketName);
				return client.ExistsAsync(directory, cancellation);
			}

			public Zongsoft.IO.DirectoryInfo GetInfo(string path) => this.GetInfoAsync(path).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
			public async ValueTask<Zongsoft.IO.DirectoryInfo> GetInfoAsync(string path, CancellationToken cancellation = default)
			{
				var directory = this.EnsureDirectoryPath(path, out var bucketName);
				var client = _fileSystem.GetClient(bucketName);
				var properties = await client.GetExtendedPropertiesAsync(directory, cancellation);
				return this.GenerateInfo(path, properties);
			}

			public bool SetInfo(string path, IEnumerable<KeyValuePair<string, string>> properties) => this.SetInfoAsync(path, properties).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
			public ValueTask<bool> SetInfoAsync(string path, IEnumerable<KeyValuePair<string, string>> properties, CancellationToken cancellation = default)
			{
				var directory = this.EnsureDirectoryPath(path, out var bucketName);
				var client = _fileSystem.GetClient(bucketName);
				return client.SetExtendedPropertiesAsync(directory, properties, cancellation);
			}

			public IEnumerable<Zongsoft.IO.PathInfo> GetChildren(string path, string pattern, bool recursive = false)
			{
				if(recursive)
					throw new NotSupportedException();

				return Zongsoft.Collections.Enumerable.Synchronize(this.GetChildrenAsync(path, pattern, recursive));
			}

			public async IAsyncEnumerable<Zongsoft.IO.PathInfo> GetChildrenAsync(string path, string pattern, bool recursive, [System.Runtime.CompilerServices.EnumeratorCancellation]CancellationToken cancellation = default)
			{
				if(recursive)
					throw new NotSupportedException();

				var directory = this.EnsurePatternPath(path, pattern, out var bucketName);
				var client = _fileSystem.GetClient(bucketName);

				var result = await client.SearchAsync(directory, _fileSystem.GetUrl, cancellation);
				if(result == null)
					yield break;

				foreach(var item in result)
					yield return item;
			}

			public IEnumerable<Zongsoft.IO.DirectoryInfo> GetDirectories(string path, string pattern, bool recursive = false)
			{
				if(recursive)
					throw new NotSupportedException();

				return Zongsoft.Collections.Enumerable.Synchronize(this.GetDirectoriesAsync(path, pattern, recursive));
			}

			public async IAsyncEnumerable<Zongsoft.IO.DirectoryInfo> GetDirectoriesAsync(string path, string pattern, bool recursive, [System.Runtime.CompilerServices.EnumeratorCancellation]CancellationToken cancellation = default)
			{
				if(recursive)
					throw new NotSupportedException();

				var directory = this.EnsurePatternPath(path, pattern, out var bucketName);
				var client = _fileSystem.GetClient(bucketName);

				var result = await client.SearchAsync(directory, _fileSystem.GetUrl, cancellation);
				if(result == null)
					yield break;

				foreach(var item in result)
				{
					if(item.IsDirectory)
						yield return (Zongsoft.IO.DirectoryInfo)item;
				}
			}

			public IEnumerable<Zongsoft.IO.FileInfo> GetFiles(string path, string pattern, bool recursive = false)
			{
				if(recursive)
					throw new NotSupportedException();

				return Zongsoft.Collections.Enumerable.Synchronize(this.GetFilesAsync(path, pattern, recursive));
			}

			public async IAsyncEnumerable<Zongsoft.IO.FileInfo> GetFilesAsync(string path, string pattern, bool recursive, [System.Runtime.CompilerServices.EnumeratorCancellation]CancellationToken cancellation = default)
			{
				if(recursive)
					throw new NotSupportedException();

				var directory = this.EnsurePatternPath(path, pattern, out var bucketName);
				var client = _fileSystem.GetClient(bucketName);

				var result = await client.SearchAsync(directory, _fileSystem.GetUrl, cancellation);
				if(result == null)
					yield break;

				foreach(var item in result)
				{
					if(item.IsFile)
						yield return (Zongsoft.IO.FileInfo)item;
				}
			}
			#endregion

			#region 私有方法
			private string EnsureDirectoryPath(string text, out string bucketName)
			{
				var path = Zongsoft.IO.Path.Parse(text);

				if(!path.IsDirectory)
					throw new PathException("Invalid directory path.");

				if(path.Segments.Length == 0)
					throw new PathException("Missing bucket name of the directory path.");

				bucketName = path.Segments[0];
				return path.FullPath;
			}

			private string EnsurePatternPath(string path, string pattern, out string bucketName)
			{
				if(string.IsNullOrWhiteSpace(pattern))
					return this.EnsureDirectoryPath(path, out bucketName);

				return this.EnsureDirectoryPath(path, out bucketName) + pattern.Trim('*', ' ', '\t', '\r', '\n').TrimStart('/');
			}

			private Zongsoft.IO.DirectoryInfo GenerateInfo(string path, IDictionary<string, string> properties)
			{
				if(properties == null)
					return null;

				DateTimeOffset createdTimeOffset, modifiedTimeOffset;
				DateTime? createdTime = null, modifiedTime = null;

				if(properties.TryGetValue(StorageHeaders.ZFS_CREATION_PROPERTY, out var value))
				{
					if(Zongsoft.Common.Convert.TryConvertValue(value, out createdTimeOffset))
						createdTime = createdTimeOffset.LocalDateTime;
				}

				if(properties.TryGetValue(StorageHeaders.HTTP_LAST_MODIFIED_PROPERTY, out value))
				{
					if(Zongsoft.Common.Convert.TryConvertValue(value, out modifiedTimeOffset))
						modifiedTime = modifiedTimeOffset.LocalDateTime;
				}

				var info = new Zongsoft.IO.DirectoryInfo(path, createdTime, modifiedTime, _fileSystem.GetUrl(path));

				foreach(var property in properties)
				{
					info.Properties[property.Key] = property.Value;
				}

				return info;
			}
			#endregion
		}

		private sealed class StorageFileProvider : IFile
		{
			#region 成员字段
			private StorageFileSystem _fileSystem;
			#endregion

			#region 私有构造
			internal StorageFileProvider(StorageFileSystem fileSystem)
			{
				_fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
			}
			#endregion

			#region 公共方法
			public bool Delete(string path) => this.DeleteAsync(path).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
			public ValueTask<bool> DeleteAsync(string path, CancellationToken cancellation = default)
			{
				path = this.EnsureFilePath(path, out var bucketName);
				var client = _fileSystem.GetClient(bucketName);
				return client.DeleteAsync(path, cancellation);
			}

			public bool Exists(string path) => this.ExistsAsync(path).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
			public ValueTask<bool> ExistsAsync(string path, CancellationToken cancellation = default)
			{
				path = this.EnsureFilePath(path, out var bucketName);
				var client = _fileSystem.GetClient(bucketName);
				return client.ExistsAsync(path, cancellation);
			}

			public void Copy(string source, string destination, bool overwrite = true) => this.CopyAsync(source, destination, overwrite).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
			public async ValueTask CopyAsync(string source, string destination, bool overwrite, CancellationToken cancellation = default)
			{
				source = this.EnsureFilePath(source, out var sourceBucket);
				destination = this.EnsureFilePath(destination, out var destinationBucket);

				if(string.Equals(sourceBucket, destinationBucket, StringComparison.OrdinalIgnoreCase))
				{
					var client = _fileSystem.GetClient(sourceBucket);

					if(!overwrite && await client.ExistsAsync(destination, cancellation))
						return;

					await client.CopyAsync(source, destination, cancellation);
				}
				else
				{
					var sourceClient = _fileSystem.GetClient(sourceBucket);
					var destinationClient = _fileSystem.GetClient(destinationBucket);

					if(!overwrite && await destinationClient.ExistsAsync(destination, cancellation))
						return;

					using(var sourceStream = await sourceClient.DownloadAsync(source, null, cancellation))
					{
						await using(var uploader = destinationClient.GetUploader(destination))
						{
							var buffer = new byte[1024 * 64];
							var bytesRead = 0;

							while((bytesRead = await sourceStream.ReadAsync(buffer, cancellation)) > 0)
							{
								await uploader.WriteAsync(buffer, 0, bytesRead, cancellation);
							}
						}
					}
				}
			}

			public void Move(string source, string destination) => this.MoveAsync(source, destination).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
			public async ValueTask MoveAsync(string source, string destination, CancellationToken cancellation = default)
			{
				source = this.EnsureFilePath(source, out var sourceBucket);
				destination = this.EnsureFilePath(destination, out var destinationBucket);

				if(string.Equals(sourceBucket, destinationBucket, StringComparison.OrdinalIgnoreCase))
				{
					var client = _fileSystem.GetClient(sourceBucket);

					if(await client.CopyAsync(source, destination, cancellation))
						await client.DeleteAsync(source, cancellation);
				}
				else
				{
					var sourceClient = _fileSystem.GetClient(sourceBucket);
					var destinationClient = _fileSystem.GetClient(destinationBucket);

					using(var sourceStream = await sourceClient.DownloadAsync(source, null, cancellation))
					{
						await using(var uploader = destinationClient.GetUploader(destination))
						{
							var buffer = new byte[1024 * 64];
							var bytesRead = 0;

							while((bytesRead = await sourceStream.ReadAsync(buffer, cancellation)) > 0)
							{
								await uploader.WriteAsync(buffer, 0, bytesRead, cancellation);
							}
						}
					}

					await sourceClient.DeleteAsync(source, cancellation);
				}
			}

			public Zongsoft.IO.FileInfo GetInfo(string path) => this.GetInfoAsync(path).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
			public async ValueTask<Zongsoft.IO.FileInfo> GetInfoAsync(string path, CancellationToken cancellation = default)
			{
				path = this.EnsureFilePath(path, out var bucketName);
				var client = _fileSystem.GetClient(bucketName);
				var properties = await client.GetExtendedPropertiesAsync(path, cancellation);
				return this.GenerateInfo(path, properties);
			}

			public bool SetInfo(string path, IEnumerable<KeyValuePair<string, string>> properties) => this.SetInfoAsync(path, properties).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
			public async ValueTask<bool> SetInfoAsync(string path, IEnumerable<KeyValuePair<string, string>> properties, CancellationToken cancellation = default)
			{
				path = this.EnsureFilePath(path, out var bucketName);
				var client = _fileSystem.GetClient(bucketName);
				return await client.SetExtendedPropertiesAsync(path, properties, cancellation);
			}

			public Stream Open(string path, FileMode mode, FileAccess access, FileShare share, IEnumerable<KeyValuePair<string, string>> properties = null)
			{
				path = this.EnsureFilePath(path, out var bucketName);
				var client = _fileSystem.GetClient(bucketName);

				bool writable = (mode != FileMode.Open) || (access & FileAccess.Write) == FileAccess.Write;

				if(writable)
					return new StorageFileStream(client.GetUploader(path, properties));

				return client.DownloadAsync(path, properties as IDictionary<string, string>, default).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
			}

			public ValueTask<Stream> OpenAsync(string path, FileMode mode, FileAccess access, FileShare share, IEnumerable<KeyValuePair<string, string>> properties, CancellationToken cancellation = default)
			{
				path = this.EnsureFilePath(path, out var bucketName);
				var client = _fileSystem.GetClient(bucketName);

				bool writable = (mode != FileMode.Open) || (access & FileAccess.Write) == FileAccess.Write;

				if(writable)
					return ValueTask.FromResult<Stream>(new StorageFileStream(client.GetUploader(path, properties)));

				return client.DownloadAsync(path, properties as IDictionary<string, string>, cancellation);
			}
			#endregion

			#region 私有方法
			private string EnsureFilePath(string text, out string bucketName)
			{
				var path = Zongsoft.IO.Path.Parse(text);

				if(!path.IsFile)
					throw new PathException("Invalid file path.");

				bucketName = path.Segments[0];
				return path.FullPath;
			}

			private Zongsoft.IO.FileInfo GenerateInfo(string path, IDictionary<string, string> properties)
			{
				if(properties == null)
					return null;

				DateTime? createdTime = null, modifiedTime = null;
				int size = 0;

				if(properties.TryGetValue(StorageHeaders.ZFS_CREATION_PROPERTY, out var value))
				{
					if(Zongsoft.Common.Convert.TryConvertValue(value, out DateTimeOffset createdTimeOffset))
						createdTime = createdTimeOffset.LocalDateTime;
				}

				if(properties.TryGetValue(StorageHeaders.HTTP_LAST_MODIFIED_PROPERTY, out value))
				{
					if(Zongsoft.Common.Convert.TryConvertValue(value, out DateTimeOffset modifiedTimeOffset))
						modifiedTime = modifiedTimeOffset.LocalDateTime;
				}

				if(properties.TryGetValue(StorageHeaders.HTTP_CONTENT_LENGTH_PROPERTY, out value))
					Zongsoft.Common.Convert.TryConvertValue(value, out size);

				var info = new Zongsoft.IO.FileInfo(path, size, createdTime, modifiedTime, _fileSystem.GetUrl(path));

				foreach(var property in properties)
				{
					info.Properties[property.Key] = property.Value;
				}

				return info;
			}
			#endregion

			#region 嵌套子类
			private class StorageFileStream : Stream
			{
				#region 成员字段
				private StorageUploader _uploader;
				#endregion

				#region 构造函数
				internal StorageFileStream(StorageUploader uploader) => _uploader = uploader;
				#endregion

				#region 公共属性
				public override bool CanRead => false;
				public override bool CanSeek => false;
				public override bool CanWrite => true;
				public override long Length => _uploader?.Count ?? 0;
				public override long Position
				{
					get => -1;
					set => throw new NotSupportedException();
				}
				#endregion

				#region 公共方法
				public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
				public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
				public override void SetLength(long value) => throw new NotSupportedException();

				public override void Write(byte[] buffer, int offset, int count)
				{
					var uploader = this.EnsureUploader();
					uploader.WriteAsync(buffer, offset, count).AsTask().GetAwaiter().GetResult();
				}

				public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellation)
				{
					var uploader = this.EnsureUploader();
					return uploader.WriteAsync(buffer, offset, count, cancellation).AsTask();
				}

				public override void Flush()
				{
					var uploader = this.EnsureUploader();
					uploader.FlushAsync().AsTask().GetAwaiter().GetResult();
				}

				public override Task FlushAsync(CancellationToken cancellation)
				{
					var uploader = this.EnsureUploader();
					return uploader.FlushAsync(cancellation).AsTask();
				}
				#endregion

				#region 释放资源
				public override ValueTask DisposeAsync()
				{
					var uploader = Interlocked.Exchange(ref _uploader, null);
					return uploader == null ? default : uploader.DisposeAsync();
				}

				protected override void Dispose(bool disposing)
				{
					var uploader = Interlocked.Exchange(ref _uploader, null);
					if(uploader == null)
						return;

					var task = uploader.DisposeAsync();
					if(task.IsCompleted)
						return;

					task.AsTask().GetAwaiter().GetResult();
				}
				#endregion

				#region 私有方法
				private StorageUploader EnsureUploader() => _uploader ?? throw new ObjectDisposedException(typeof(StorageFileStream).FullName);
				#endregion
			}
			#endregion
		}
		#endregion
	}
}
