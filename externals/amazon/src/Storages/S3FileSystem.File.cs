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
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Amazon.S3;
using Amazon.S3.Model;

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

			if(string.Equals(srcRegion, destRegion))
			{
				var client = _fileSystem.GetClient(srcRegion);
				var request = new CopyObjectRequest()
				{
					SourceBucket = srcBucket,
					SourceKey = srcPath,
					DestinationBucket = destBucket,
					DestinationKey = destPath,
				};

				return new ValueTask(client.CopyObjectAsync(request));
			}

			return CopyToAsync(_fileSystem, srcRegion, srcBucket, srcPath, destRegion, destBucket, destPath);

			static async ValueTask CopyToAsync(S3FileSystem fileSystem, string srcRegion, string srcBucket, string destRegion, string srcPath, string destBucket, string destPath)
			{
				var srcClient = fileSystem.GetClient(srcRegion);
				var response = await srcClient.GetObjectAsync(new GetObjectRequest()
				{
					BucketName = srcBucket,
					Key = srcPath,
				});

				var destClient = fileSystem.GetClient(destRegion);
				await destClient.PutObjectAsync(new PutObjectRequest()
				{
					BucketName = destBucket,
					Key = destPath,
					InputStream = response.ResponseStream
				});
			}
		}

		public void Move(string source, string destination) => this.MoveAsync(source, destination).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
		public ValueTask MoveAsync(string source, string destination)
		{
			var srcPath = Resolve(source, out var srcRegion, out var srcBucket);
			var destPath = Resolve(destination, out var destRegion, out var destBucket);

			if(string.Equals(srcRegion, destRegion))
			{
				var client = _fileSystem.GetClient(srcRegion);
				var request = new CopyObjectRequest()
				{
					SourceBucket = srcBucket,
					SourceKey = srcPath,
					DestinationBucket = destBucket,
					DestinationKey = destPath,
				};

				return new ValueTask(client.CopyObjectAsync(request)
					.ContinueWith(async task =>
					{
						await client.DeleteObjectAsync(srcBucket, srcPath);
					}));
			}

			return MoveToAsync(_fileSystem, srcRegion, srcBucket, srcPath, destRegion, destBucket, destPath);

			static async ValueTask MoveToAsync(S3FileSystem fileSystem, string srcRegion, string srcBucket, string destRegion, string srcPath, string destBucket, string destPath)
			{
				var srcClient = fileSystem.GetClient(srcRegion);
				var response = await srcClient.GetObjectAsync(new GetObjectRequest()
				{
					BucketName = srcBucket,
					Key = srcPath,
				});

				var destClient = fileSystem.GetClient(destRegion);
				await destClient.PutObjectAsync(new PutObjectRequest()
				{
					BucketName = destBucket,
					Key = destPath,
					InputStream = response.ResponseStream
				});

				await srcClient.DeleteObjectAsync(srcBucket, srcPath);
			}
		}

		public bool Delete(string path) => this.DeleteAsync(path).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
		public async ValueTask<bool> DeleteAsync(string path)
		{
			path = Resolve(path, out var region, out var bucket);
			var client = _fileSystem.GetClient(region);

			try
			{
				var response = await client.DeleteObjectAsync(bucket, path);
				return response.HttpStatusCode.IsSucceed() && response.DeleteMarker != null;
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
				var response = await client.GetObjectMetadataAsync(bucket, path);
				return response.HttpStatusCode.IsSucceed();
			}
			catch(AmazonS3Exception ex) when(ex.StatusCode == System.Net.HttpStatusCode.NotFound)
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
				IEnumerable<KeyValuePair<string, object>> tags = null;
				var response = await client.GetObjectAsync(bucket, path);

				if(response.TagCount > 0 || response.Metadata.Keys.Contains("x-amz-meta-x-amz-tagging"))
				{
					var tagging = await client.GetObjectTaggingAsync(new GetObjectTaggingRequest()
					{
						BucketName = bucket,
						Key = path,
					});

					tags = tagging.Tagging?.Select(tag => new KeyValuePair<string, object>(tag.Key, tag.Value));
				}

				string contentType = null;

				if(response.Metadata.Keys.Contains("x-amz-meta-content-type"))
					contentType = response.Metadata["x-amz-meta-content-type"];
				if(string.IsNullOrEmpty(contentType))
					contentType = response.Headers.ContentType;

				return _fileSystem.GetFileInfo(region, bucket, path, response.ContentLength, contentType, null, response.LastModified, tags);
			}
			catch(AmazonS3Exception ex) when(ex.StatusCode == System.Net.HttpStatusCode.NotFound)
			{
				return null;
			}
		}

		public bool SetInfo(string path, IDictionary<string, object> properties) => this.SetInfoAsync(path, properties).AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
		public async ValueTask<bool> SetInfoAsync(string path, IDictionary<string, object> properties)
		{
			if(properties == null)
				return false;

			var tags = properties.Select(property => new Tag()
			{
				Key = property.Key,
				Value = Common.Convert.ConvertValue<string>(property.Value)
			}).ToList();

			if(tags.Count == 0)
				return false;

			path = Resolve(path, out var region, out var bucket);
			var client = _fileSystem.GetClient(region);

			try
			{
				var response = await client.PutObjectTaggingAsync(new PutObjectTaggingRequest()
				{
					BucketName = bucket,
					Key = path,
					Tagging = new Tagging() { TagSet = tags }
				});

				return response.HttpStatusCode.IsSucceed();
			}
			catch(AmazonS3Exception ex) when(ex.StatusCode == System.Net.HttpStatusCode.NotFound)
			{
				return false;
			}
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
				return mode == FileMode.Append ?
					new S3Stream(client, OpenStream(client, bucket, path), bucket, path, properties) :
					new S3Stream(client, null, bucket, path, properties);
			}

			return OpenStream(client, bucket, path);

			static Stream OpenStream(AmazonS3Client client, string bucket, string path)
			{
				try
				{
					var response = client.GetObjectAsync(new GetObjectRequest()
					{
						BucketName = bucket,
						Key = path,
					}).ConfigureAwait(false).GetAwaiter().GetResult();

					return response.ResponseStream;
				}
				catch(AmazonS3Exception ex) when(ex.StatusCode == System.Net.HttpStatusCode.NotFound)
				{
					return null;
				}
			}
		}
	}

	internal sealed class S3Stream : Stream
	{
		private const int MB = 1024 * 1024;

		private AmazonS3Client _client;
		private Stream _stream;
		private readonly string _bucket;
		private readonly string _path;
		private readonly IEnumerable<KeyValuePair<string, object>> _properties;
		private byte[] _buffer;
		private int _bufferSize;
		private long _length;
		private long _position;
		private string _uploadId;
		private int _partNumber;
		private List<PartETag> _parts;

		public S3Stream(AmazonS3Client client, Stream stream, string bucket, string path, IEnumerable<KeyValuePair<string, object>> properties)
		{
			_client = client;
			_stream = stream;
			_bucket = bucket;
			_path = path;
			_properties = properties;
			_parts = new();
			_buffer = ArrayPool<byte>.Shared.Rent(5 * MB);
		}

		public override bool CanRead => true;
		public override bool CanSeek => false;
		public override bool CanWrite => true;
		public override long Length => _length;
		public override long Position { get => _position; set => throw new NotSupportedException(); }

		public override void Flush() { }
		public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
		public override void SetLength(long value) => throw new NotSupportedException();
		public override int Read(byte[] buffer, int offset, int count)
		{
			_stream ??= _client.GetObjectAsync(new GetObjectRequest()
			{
				BucketName = _bucket,
				Key = _path,
				ByteRange = new ByteRange(offset, count)
			}).ConfigureAwait(false).GetAwaiter().GetResult().ResponseStream;

			return _stream.Read(buffer, offset, count);
		}

		public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellation)
		{
			_stream ??= (await _client.GetObjectAsync(new GetObjectRequest()
			{
				BucketName = _bucket,
				Key = _path,
				ByteRange = new ByteRange(offset, count)
			}, cancellation).ConfigureAwait(false)).ResponseStream;

			return await _stream.ReadAsync(buffer, offset, count, cancellation);
		}

		public override void Write(byte[] buffer, int offset, int count) => this.WriteAsync(buffer, offset, count, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

		public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellation)
		{
			if(string.IsNullOrEmpty(_uploadId))
			{
				var argument = new InitiateMultipartUploadRequest()
				{
					BucketName = _bucket,
					Key = _path,
				};

				if(Zongsoft.IO.Mime.TryGetMimeType(_path, out var type))
					argument.ContentType = type;

				if(_properties != null)
					argument.TagSet = [.. _properties.Select(property => new Tag()
					{
						Key = property.Key,
						Value = Common.Convert.ConvertValue<string>(property.Value)
					})];

				_uploadId = (await _client.InitiateMultipartUploadAsync(argument, cancellation)).UploadId;
			}

			if(_stream != null)
			{
				int bytesRead;
				var bytes = ArrayPool<byte>.Shared.Rent(5 * MB);

				while((bytesRead = _stream.Read(bytes, 0, bytes.Length)) > 0)
					await this.UploadAsync(bytes, 0, bytesRead, cancellation);

				ArrayPool<byte>.Shared.Return(bytes);
				_stream.Dispose();
				_stream = null;
			}

			await this.UploadAsync(buffer, offset, count, cancellation);
		}

		public override void Close()
		{
			if(_uploadId != null)
			{
				if(_bufferSize > 0)
					this.UploadPartAsync().AsTask().ConfigureAwait(false).GetAwaiter().GetResult();

				if(_length > 0)
					_client.CompleteMultipartUploadAsync(new()
					{
						UploadId = _uploadId,
						BucketName = _bucket,
						Key = _path,
						PartETags = _parts,
					}).ConfigureAwait(false).GetAwaiter().GetResult();
				else
					_client.AbortMultipartUploadAsync(new()
					{
						UploadId = _uploadId,
						BucketName = _bucket,
						Key = _path,
					}).ConfigureAwait(false).GetAwaiter().GetResult();
			}
		}

		private async ValueTask UploadAsync(byte[] buffer, int offset, int count, CancellationToken cancellation = default)
		{
			_length += count;

			while(count > 0)
			{
				var remainder = _buffer.Length - _bufferSize;
				var length = Math.Min(count, remainder);

				Array.Copy(buffer, offset, _buffer, _bufferSize, length);

				offset += length;
				count -= length;
				_bufferSize += length;

				if(_bufferSize == _buffer.Length)
				{
					await this.UploadPartAsync(cancellation);
					_bufferSize = 0;
				}
			}
		}

		private async ValueTask UploadPartAsync(CancellationToken cancellation = default)
		{
			if(_bufferSize < 1)
				return;

			var request = new UploadPartRequest()
			{
				UploadId = _uploadId,
				BucketName = _bucket,
				Key = _path,
				PartNumber = ++_partNumber,
				PartSize = _bufferSize,
				InputStream = new MemoryStream(_buffer, 0, _bufferSize),
			};

			var response = await _client.UploadPartAsync(request, cancellation);
			_parts.Add(new PartETag(response.PartNumber.Value, response.ETag));
			_position += _bufferSize;
		}

		protected override void Dispose(bool disposing)
		{
			ArrayPool<byte>.Shared.Return(_buffer);
			_stream?.Dispose();
			_stream = null;
			_client = null;
			_buffer = null;
			_parts = null;
		}
	}
}
