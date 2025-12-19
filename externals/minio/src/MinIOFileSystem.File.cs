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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Minio;
using Minio.Exceptions;
using Minio.DataModel.Args;
using Zongsoft.Data;
using System.Buffers;

namespace Zongsoft.Externals.MinIO;

partial class MinIOFileSystem
{
	private sealed class FileProvider(MinIOFileSystem fileSystem) : Zongsoft.IO.IFile
	{
		private readonly MinIOFileSystem _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));

		public void Copy(string source, string destination) => this.Copy(source, destination, true);
		public void Copy(string source, string destination, bool overwrite) => this.CopyAsync(source, destination, overwrite).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();

		public ValueTask CopyAsync(string source, string destination) => this.CopyAsync(source, destination, false);
		public ValueTask CopyAsync(string source, string destination, bool overwrite)
		{
			var srcPath = Resolve(source, out var srcRegion, out var srcBucket);
			var destPath = Resolve(destination, out var destRegion, out var destBucket);
			var client = _fileSystem.GetClient(srcRegion);

			if(string.Equals(srcRegion, destRegion))
			{
				var argument = new CopySourceObjectArgs()
					.WithBucket(srcBucket)
					.WithObject(srcPath);

				return new ValueTask(client.CopyObjectAsync(new CopyObjectArgs()
					.WithBucket(destBucket)
					.WithObject(destPath)
					.WithCopyObjectSource(argument)));
			}

			var request = new GetObjectArgs()
				.WithBucket(srcBucket)
				.WithObject(srcPath)
				.WithCallbackStream((stream, cancellation) =>
				{
					return client.PutObjectAsync(
						new PutObjectArgs()
							.WithBucket(destBucket)
							.WithObject(destPath)
							.WithStreamData(stream),
						cancellation);
				});

			return new ValueTask(client.GetObjectAsync(request));
		}

		public void Move(string source, string destination) => this.MoveAsync(source, destination).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
		public ValueTask MoveAsync(string source, string destination)
		{
			var srcPath = Resolve(source, out var srcRegion, out var srcBucket);
			var destPath = Resolve(destination, out var destRegion, out var destBucket);
			var client = _fileSystem.GetClient(srcRegion);

			if(string.Equals(srcRegion, destRegion))
			{
				var argument = new CopySourceObjectArgs()
					.WithBucket(srcBucket)
					.WithObject(srcPath);

				return new ValueTask(client.CopyObjectAsync(new CopyObjectArgs()
					.WithBucket(destBucket)
					.WithObject(destPath)
					.WithCopyObjectSource(argument))
				.ContinueWith(task =>
					client.RemoveObjectAsync(new RemoveObjectArgs()
						.WithBucket(srcBucket)
						.WithObject(srcPath))
				));
			}

			var request = new GetObjectArgs()
				.WithBucket(srcBucket)
				.WithObject(srcPath)
				.WithCallbackStream((stream, cancellation) =>
				{
					return client.PutObjectAsync(
						new PutObjectArgs()
							.WithBucket(destBucket)
							.WithObject(destPath)
							.WithStreamData(stream),
						cancellation);
				});

			var task = client.GetObjectAsync(request)
				.ContinueWith(task =>
					client.RemoveObjectAsync(new RemoveObjectArgs()
						.WithBucket(srcBucket)
						.WithObject(srcPath)));

			return new ValueTask(task);
		}

		public bool Delete(string path) => this.DeleteAsync(path).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
		public async ValueTask<bool> DeleteAsync(string path)
		{
			path = Resolve(path, out var region, out var bucket);
			var client = _fileSystem.GetClient(region);

			try
			{
				await client.RemoveObjectAsync(new RemoveObjectArgs()
					.WithBucket(bucket)
					.WithObject(path));

				return true;
			}
			catch { return false; }
		}

		public bool Exists(string path) => this.ExistsAsync(path).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
		public async ValueTask<bool> ExistsAsync(string path)
		{
			path = Resolve(path, out var region, out var bucket);
			var client = _fileSystem.GetClient(region);

			try
			{
				var response = await client.StatObjectAsync(new StatObjectArgs()
					.WithBucket(bucket)
					.WithObject(path));

				return response.ObjectName != null;
			}
			catch(BucketNotFoundException)
			{
				return false;
			}
			catch(ObjectNotFoundException)
			{
				return false;
			}
		}

		public IO.FileInfo GetInfo(string path) => this.GetInfoAsync(path).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
		public async ValueTask<IO.FileInfo> GetInfoAsync(string path)
		{
			path = Resolve(path, out var region, out var bucket);
			var client = _fileSystem.GetClient(region);

			try
			{
				var response = await client.StatObjectAsync(new StatObjectArgs()
					.WithBucket(bucket)
					.WithObject(path));

				var tagging = await client.GetObjectTagsAsync(
					new GetObjectTagsArgs()
					.WithBucket(bucket)
					.WithObject(path));

				return new(_fileSystem.GetUrl(region, bucket, path), response.Size, null, response.LastModified, tagging.Tags?.Select(tag => new KeyValuePair<string, object>(tag.Key, tag.Value)))
				{
					Type = response.ContentType,
				};
			}
			catch(BucketNotFoundException)
			{
				return null;
			}
			catch(ObjectNotFoundException)
			{
				return null;
			}
		}

		public bool SetInfo(string path, IDictionary<string, object> properties) => this.SetInfoAsync(path, properties).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
		public async ValueTask<bool> SetInfoAsync(string path, IDictionary<string, object> properties)
		{
			if(properties == null)
				return false;

			path = Resolve(path, out var region, out var bucket);
			var client = _fileSystem.GetClient(region);

			var tagging = new Minio.DataModel.Tags.Tagging();
			foreach(var property in properties)
				tagging.Tags.TryAdd(property.Key, Zongsoft.Common.Convert.ConvertValue<string>(property.Value));

			if(tagging == null || tagging.Tags == null || tagging.Tags.Count == 0)
				return false;

			try
			{
				await client.SetObjectTagsAsync(new SetObjectTagsArgs()
					.WithBucket(bucket)
					.WithObject(path)
					.WithTagging(tagging));

				return true;
			}
			catch { return false; }
		}

		public Stream Open(string path, IDictionary<string, object> properties = null) => this.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, properties);
		public Stream Open(string path, FileMode mode, IDictionary<string, object> properties = null) => this.Open(path, mode, FileAccess.ReadWrite, FileShare.None, properties);
		public Stream Open(string path, FileMode mode, FileAccess access, IDictionary<string, object> properties = null) => this.Open(path, mode, access, FileShare.None, properties);
		public Stream Open(string path, FileMode mode, FileAccess access, FileShare share, IDictionary<string, object> properties = null)
		{
			path = Resolve(path, out var region, out var bucket);
			var client = _fileSystem.GetClient(region);

			if((access & FileAccess.Write) == FileAccess.Write)
			{
				if(mode == FileMode.Append)
				{
					var content = Stream.Null;
					var request = new GetObjectArgs()
						.WithBucket(bucket)
						.WithObject(path)
						.WithCallbackStream(stream => content = stream);

					client.GetObjectAsync(request).ConfigureAwait(false).GetAwaiter().GetResult();
					return new MinIOStream(client, content, bucket, path);
				}

				return new MinIOStream(client, null, bucket, path);
			}
			else
			{
				try
				{
					Stream result = null;
					var response = client.GetObjectAsync(new GetObjectArgs()
						.WithBucket(bucket)
						.WithObject(path)
						.WithCallbackStream(stream => result = stream))
					.ConfigureAwait(false).GetAwaiter().GetResult();

					return result;
				}
				catch(BucketNotFoundException)
				{
					return mode == FileMode.Open ? null : Stream.Null;
				}
				catch(ObjectNotFoundException)
				{
					return mode == FileMode.Open ? null : Stream.Null;
				}
			}
		}
	}

	internal sealed class MinIOStream : Stream
	{
		private IMinioClient _client;
		private Stream _stream;
		private readonly string _bucket;
		private readonly string _path;
		private PutObjectArgs _request;
		private byte[] _buffer;
		private long _length;
		private long _position;

		public MinIOStream(IMinioClient client, Stream stream, string bucket, string path)
		{
			_client = client;
			_stream = stream;
			_bucket = bucket;
			_path = path;
			_buffer = ArrayPool<byte>.Shared.Rent(1024 * 512);
			_request = new PutObjectArgs()
				.WithBucket(bucket)
				.WithObject(path);
		}

		public override bool CanRead => true;
		public override bool CanSeek => false;
		public override bool CanWrite => true;
		public override long Length => _length;
		public override long Position { get => _position; set => throw new NotSupportedException(); }

		public override void Flush() { }
		public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
		public override void SetLength(long value) => throw new NotSupportedException();
		public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
		public override void Write(byte[] buffer, int offset, int count)
		{
			if(_stream != null)
			{
				var bytesRead = _stream.Read(_buffer, 0, _buffer.Length);

				while(bytesRead > 0)
				{
					_length += bytesRead;
					_position += bytesRead;

					_request.WithRequestBody(_buffer.AsMemory(0, bytesRead));
					_client.PutObjectAsync(_request).ConfigureAwait(false).GetAwaiter().GetResult();
				}
			}

			_length += count;
			_position += count;

			//_request.WithRequestBody(buffer.AsMemory(offset, count));
			_request.WithStreamData(new MemoryStream(buffer, offset, count));
			var response = _client.PutObjectAsync(_request).ConfigureAwait(false).GetAwaiter().GetResult();
		}

		public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => base.WriteAsync(buffer, offset, count, cancellationToken);

		protected override void Dispose(bool disposing)
		{
			ArrayPool<byte>.Shared.Return(_buffer);
			_stream?.Dispose();
			_stream = null;
			_client = null;
			_buffer = null;
		}
	}
}
