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
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Zongsoft.IO;
using Zongsoft.Services;

namespace Zongsoft.Externals.Aliyun.Storages
{
	[Service(typeof(Zongsoft.IO.IFileSystem))]
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
			options.Buckets.TryGet(path.Segments[0], out var bucket);

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
			options.Buckets.TryGet(bucketName, out var bucket);

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

			return Aliyun.Options.GeneralOptions.Instance.Certificates.Get(certificate);
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
			public bool Create(string path, IDictionary<string, object> properties = null) => Utility.ExecuteTask(() => this.CreateAsync(path, properties));
			public Task<bool> CreateAsync(string path, IDictionary<string, object> properties = null)
			{
				var directory = this.EnsureDirectoryPath(path, out var bucketName);
				var client = _fileSystem.GetClient(bucketName);
				return client.CreateAsync(directory, properties);
			}

			public bool Delete(string path, bool recursive = false) => Utility.ExecuteTask(() => this.DeleteAsync(path, recursive));
			public Task<bool> DeleteAsync(string path, bool recursive = false)
			{
				if(recursive)
					throw new NotSupportedException();

				var directory = this.EnsureDirectoryPath(path, out var bucketName);
				var client = _fileSystem.GetClient(bucketName);
				return client.DeleteAsync(directory);
			}

			public void Move(string source, string destination) => throw new NotSupportedException();
			public Task MoveAsync(string source, string destination)
			{
				throw new NotSupportedException();
			}

			public bool Exists(string path) => Utility.ExecuteTask(() => this.ExistsAsync(path));
			public Task<bool> ExistsAsync(string path)
			{
				var directory = this.EnsureDirectoryPath(path, out var bucketName);
				var client = _fileSystem.GetClient(bucketName);
				return client.ExistsAsync(directory);
			}

			public Zongsoft.IO.DirectoryInfo GetInfo(string path) => Utility.ExecuteTask(() => this.GetInfoAsync(path));
			public async Task<Zongsoft.IO.DirectoryInfo> GetInfoAsync(string path)
			{
				var directory = this.EnsureDirectoryPath(path, out var bucketName);
				var client = _fileSystem.GetClient(bucketName);
				var properties = await client.GetExtendedPropertiesAsync(directory);
				return this.GenerateInfo(path, properties);
			}

			public bool SetInfo(string path, IDictionary<string, object> properties) => Utility.ExecuteTask(() => this.SetInfoAsync(path, properties));
			public Task<bool> SetInfoAsync(string path, IDictionary<string, object> properties)
			{
				var directory = this.EnsureDirectoryPath(path, out var bucketName);
				var client = _fileSystem.GetClient(bucketName);
				return client.SetExtendedPropertiesAsync(directory, properties);
			}

			public IEnumerable<Zongsoft.IO.PathInfo> GetChildren(string path) => this.GetChildren(path, null, false);
			public IEnumerable<Zongsoft.IO.PathInfo> GetChildren(string path, string pattern, bool recursive = false)
			{
				if(recursive)
					throw new NotSupportedException();

				return Utility.ExecuteTask(() => this.GetChildrenAsync(path, pattern, recursive));
			}

			public Task<IEnumerable<Zongsoft.IO.PathInfo>> GetChildrenAsync(string path) => this.GetChildrenAsync(path, null, false);
			public async Task<IEnumerable<Zongsoft.IO.PathInfo>> GetChildrenAsync(string path, string pattern, bool recursive = false)
			{
				if(recursive)
					throw new NotSupportedException();

				var directory = this.EnsurePatternPath(path, pattern, out var bucketName);
				var client = _fileSystem.GetClient(bucketName);

				var result = await client.SearchAsync(directory, _fileSystem.GetUrl);

				if(result == null)
					return Enumerable.Empty<Zongsoft.IO.PathInfo>();

				return result;
			}

			public IEnumerable<Zongsoft.IO.DirectoryInfo> GetDirectories(string path) => this.GetDirectories(path, null, false);
			public IEnumerable<Zongsoft.IO.DirectoryInfo> GetDirectories(string path, string pattern, bool recursive = false)
			{
				if(recursive)
					throw new NotSupportedException();

				return Utility.ExecuteTask(() => this.GetDirectoriesAsync(path, pattern, recursive));
			}

			public Task<IEnumerable<Zongsoft.IO.DirectoryInfo>> GetDirectoriesAsync(string path) => this.GetDirectoriesAsync(path, null, false);
			public async Task<IEnumerable<Zongsoft.IO.DirectoryInfo>> GetDirectoriesAsync(string path, string pattern, bool recursive = false)
			{
				if(recursive)
					throw new NotSupportedException();

				var directory = this.EnsurePatternPath(path, pattern, out var bucketName);
				var client = _fileSystem.GetClient(bucketName);

				var result = await client.SearchAsync(directory, _fileSystem.GetUrl);

				if(result == null)
					return Enumerable.Empty<Zongsoft.IO.DirectoryInfo>();

				return result.Where(item => item.IsDirectory).Select(item => (Zongsoft.IO.DirectoryInfo)item);
			}

			public IEnumerable<Zongsoft.IO.FileInfo> GetFiles(string path) => this.GetFiles(path, null, false);
			public IEnumerable<Zongsoft.IO.FileInfo> GetFiles(string path, string pattern, bool recursive = false)
			{
				if(recursive)
					throw new NotSupportedException();

				return Utility.ExecuteTask(() => this.GetFilesAsync(path, pattern, recursive));
			}

			public Task<IEnumerable<Zongsoft.IO.FileInfo>> GetFilesAsync(string path) => this.GetFilesAsync(path, null, false);
			public async Task<IEnumerable<Zongsoft.IO.FileInfo>> GetFilesAsync(string path, string pattern, bool recursive = false)
			{
				if(recursive)
					throw new NotSupportedException();

				var directory = this.EnsurePatternPath(path, pattern, out var bucketName);
				var client = _fileSystem.GetClient(bucketName);

				var result = await client.SearchAsync(directory, _fileSystem.GetUrl);

				if(result == null)
					return Enumerable.Empty<Zongsoft.IO.FileInfo>();

				return result.Where(item => item.IsFile).Select(item => (Zongsoft.IO.FileInfo)item);
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

			private Zongsoft.IO.DirectoryInfo GenerateInfo(string path, IDictionary<string, object> properties)
			{
				if(properties == null)
					return null;

				DateTimeOffset createdTimeOffset, modifiedTimeOffset;
				DateTime? createdTime = null, modifiedTime = null;

				object value;

				if(properties.TryGetValue(StorageHeaders.ZFS_CREATION_PROPERTY, out value))
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
			public bool Delete(string path) => Utility.ExecuteTask(() => this.DeleteAsync(path));
			public Task<bool> DeleteAsync(string path)
			{
				path = this.EnsureFilePath(path, out var bucketName);
				var client = _fileSystem.GetClient(bucketName);
				return client.DeleteAsync(path);
			}

			public bool Exists(string path) => Utility.ExecuteTask(() => this.ExistsAsync(path));
			public Task<bool> ExistsAsync(string path)
			{
				path = this.EnsureFilePath(path, out var bucketName);
				var client = _fileSystem.GetClient(bucketName);
				return client.ExistsAsync(path);
			}

			public void Copy(string source, string destination) => this.Copy(source, destination, false);
			public void Copy(string source, string destination, bool overwrite) => this.CopyAsync(source, destination, overwrite).Wait();
			public Task CopyAsync(string source, string destination) => this.CopyAsync(source, destination, false);
			public async Task CopyAsync(string source, string destination, bool overwrite)
			{
				source = this.EnsureFilePath(source, out var sourceBucket);
				destination = this.EnsureFilePath(destination, out var destinationBucket);

				if(string.Equals(sourceBucket, destinationBucket, StringComparison.OrdinalIgnoreCase))
				{
					var client = _fileSystem.GetClient(sourceBucket);

					if(!overwrite && await client.ExistsAsync(destination))
						return;

					await client.CopyAsync(source, destination);
				}
				else
				{
					var sourceClient = _fileSystem.GetClient(sourceBucket);
					var destinationClient = _fileSystem.GetClient(destinationBucket);

					if(!overwrite && await destinationClient.ExistsAsync(destination))
						return;

					using(var sourceStream = await sourceClient.DownloadAsync(source))
					{
						await using(var uploader = destinationClient.GetUploader(destination))
						{
							var buffer = new byte[1024 * 64];
							var bytesRead = 0;

							while((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
							{
								await uploader.WriteAsync(buffer, 0, bytesRead);
							}
						}
					}
				}
			}

			public void Move(string source, string destination) => this.MoveAsync(source, destination).Wait();
			public async Task MoveAsync(string source, string destination)
			{
				source = this.EnsureFilePath(source, out var sourceBucket);
				destination = this.EnsureFilePath(destination, out var destinationBucket);

				if(string.Equals(sourceBucket, destinationBucket, StringComparison.OrdinalIgnoreCase))
				{
					var client = _fileSystem.GetClient(sourceBucket);

					if(await client.CopyAsync(source, destination))
						await client.DeleteAsync(source);
				}
				else
				{
					var sourceClient = _fileSystem.GetClient(sourceBucket);
					var destinationClient = _fileSystem.GetClient(destinationBucket);

					using(var sourceStream = await sourceClient.DownloadAsync(source))
					{
						await using(var uploader = destinationClient.GetUploader(destination))
						{
							var buffer = new byte[1024 * 64];
							var bytesRead = 0;

							while((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
							{
								await uploader.WriteAsync(buffer, 0, bytesRead);
							}
						}
					}

					await sourceClient.DeleteAsync(source);
				}
			}

			public Zongsoft.IO.FileInfo GetInfo(string path) => Utility.ExecuteTask(() => this.GetInfoAsync(path));
			public async Task<Zongsoft.IO.FileInfo> GetInfoAsync(string path)
			{
				path = this.EnsureFilePath(path, out var bucketName);
				var client = _fileSystem.GetClient(bucketName);

				var properties = await client.GetExtendedPropertiesAsync(path);

				return this.GenerateInfo(path, properties);
			}

			public bool SetInfo(string path, IDictionary<string, object> properties) => Utility.ExecuteTask(() => this.SetInfoAsync(path, properties));
			public async Task<bool> SetInfoAsync(string path, IDictionary<string, object> properties)
			{
				path = this.EnsureFilePath(path, out var bucketName);
				var client = _fileSystem.GetClient(bucketName);
				return await client.SetExtendedPropertiesAsync(path, properties);
			}

			public Stream Open(string path, IDictionary<string, object> properties = null) => this.Open(path, FileMode.Open, FileAccess.Read, FileShare.None, properties);
			public Stream Open(string path, FileMode mode, IDictionary<string, object> properties = null) => this.Open(path, mode, FileAccess.Read, FileShare.None, properties);
			public Stream Open(string path, FileMode mode, FileAccess access, IDictionary<string, object> properties = null) => this.Open(path, mode, access, FileShare.None, properties);
			public Stream Open(string path, FileMode mode, FileAccess access, FileShare share, IDictionary<string, object> properties = null)
			{
				path = this.EnsureFilePath(path, out var bucketName);
				var client = _fileSystem.GetClient(bucketName);

				bool writable = (mode != FileMode.Open) || (access & FileAccess.Write) == FileAccess.Write;

				if(writable)
					return new StorageFileStream(client.GetUploader(path, properties));

				return Utility.ExecuteTask(() => client.DownloadAsync(path, properties));
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

			private Zongsoft.IO.FileInfo GenerateInfo(string path, IDictionary<string, object> properties)
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
					uploader.WriteAsync(buffer, offset, count).GetAwaiter().GetResult();
				}

				public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellation)
				{
					var uploader = this.EnsureUploader();
					return uploader.WriteAsync(buffer, offset, count, cancellation);
				}

				public override void Flush()
				{
					var uploader = this.EnsureUploader();
					uploader.FlushAsync().GetAwaiter().GetResult();
				}

				public override Task FlushAsync(CancellationToken cancellation)
				{
					var uploader = this.EnsureUploader();
					return uploader.FlushAsync(cancellation);
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
