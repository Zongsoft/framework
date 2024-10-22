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
using System.Buffers;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Zongsoft.Externals.Aliyun.Storages
{
	internal class StorageUploader : IAsyncDisposable
	{
		#region 常量定义
		//定义批量上传失败的重试次数
		private const int RETRY_COUNT = 2;

		//定义最大的缓存区大小
		private const int MAXIMUM_BUFFER_SIZE = 10 * 1024 * 1024;

		//定义默认的缓存区大小（注：不能低于阿里云OSS要求的最小批量包大小）
		private const int DEFAULT_MULTIPART_SIZE = 512 * 1024;

		//定义阿里云OSS批量上传要求每次批量包的大小不能低于100KB
		private const int MINIMUM_MULTIPART_SIZE = 100 * 1024;

		//定义解析批量上传初始化操作结果的正则表达式
		private static readonly Regex _Initiate_Regex_ = new(@"\<UploadId\>(?<value>\w+)\<\/UploadId\>", RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace);
		#endregion

		#region 私有变量
		private int _disposed;
		private string _identifier;
		private readonly string _path;
		private byte[] _buffer;
		private readonly int _bufferSize;
		private int _bufferedCount;
		private readonly StorageClient _client;
		private List<StorageMultipart> _multiparts;
		private readonly IDictionary<string, object> _extendedProperties;
		#endregion

		#region 构造函数
		public StorageUploader(StorageClient client, string path, int bufferSize = DEFAULT_MULTIPART_SIZE) : this(client, path, null, bufferSize) { }
		public StorageUploader(StorageClient client, string path, IDictionary<string, object> extendedProperties, int bufferSize = DEFAULT_MULTIPART_SIZE)
		{
			if(string.IsNullOrWhiteSpace(path))
				throw new ArgumentNullException(nameof(path));
			if(bufferSize > MAXIMUM_BUFFER_SIZE)
				throw new ArgumentOutOfRangeException(nameof(bufferSize));

			_bufferSize = bufferSize > 0 ? Math.Max(bufferSize, MINIMUM_MULTIPART_SIZE) : DEFAULT_MULTIPART_SIZE;
			_client = client ?? throw new ArgumentNullException(nameof(client));
			_path = path.TrimEnd('/', '\\');
			_extendedProperties = extendedProperties;
		}
		#endregion

		#region 公共属性
		public string Identifier => _identifier;
		public string Path => _path;
		public long Count => _multiparts == null ? 0 : _multiparts.Sum(part => part.Count);
		#endregion

		#region 公共方法
		public ValueTask WriteAsync(byte[] data, int offset, CancellationToken cancellation = default) => this.WriteAsync(data, offset, -1, cancellation);
		public async ValueTask WriteAsync(byte[] data, int offset, int count, CancellationToken cancellation = default)
		{
			//确认当前是否可用的
			this.EnsureDisposed();

			//如果发送的数据为空则返回
			if(data == null || data.Length == 0)
				return;

			//确保指定的偏移量没有超出范围
			if(offset < 0 || offset >= data.Length)
				throw new ArgumentOutOfRangeException(nameof(offset));

			//确保指定的写数量没有超出范围
			if(count < 0)
				count = data.Length - offset;
			else if(offset + count > data.Length)
				throw new ArgumentOutOfRangeException(nameof(count));

			/*
			 * 注意：无论待写入数据量是否满足最小批量上传大小，都必须先进行初始化，
			 *      因为后续的 Flush、Complete、Abort 都依赖该初始化后的标识！
			 */
			if(string.IsNullOrEmpty(_identifier))
			{
				//初始化批量上传并设置批量上传标识
				_identifier = await this.InitiateAsync(cancellation);
				if(string.IsNullOrEmpty(_identifier))
					return;

				//初始化缓存区
				_buffer = ArrayPool<byte>.Shared.Rent(_bufferSize);
				//初始化上传成功的分部列表
				_multiparts = new();
			}

			//如果写入的数据量与已缓存量还不够最小批次的发送量，则将写入数据追加到缓存区
			if(_bufferedCount + count < MINIMUM_MULTIPART_SIZE)
			{
				//将写入数据复制到缓存区
				Buffer.BlockCopy(data, offset, _buffer, _bufferedCount, count);

				//更新缓存的字节数
				_bufferedCount += count;

				return;
			}

			StorageMultipart multipart;

			//如果缓存区没有缓存的数据
			if(_bufferedCount == 0)
			{
				multipart = new StorageMultipart(_multiparts.Count, data, offset, count);
			}
			else
			{
				//如果缓存区的剩余空间大于待写入的数据量，则只需将待写入数据追加到缓存区
				if(count <= _buffer.Length - _bufferedCount)
				{
					Buffer.BlockCopy(data, offset, _buffer, _bufferedCount, count);
				}
				else //待写入的数据量比缓存区剩余数据区要多，则将原缓存区进行扩容
				{
					var original = _buffer;

					//重新申请一块新的缓存区
					_buffer = ArrayPool<byte>.Shared.Rent(_bufferedCount + count);

					Buffer.BlockCopy(original, 0, _buffer, 0, _bufferedCount);
					Buffer.BlockCopy(data, offset, _buffer, _bufferedCount, count);

					//将原缓存区归还给缓存池
					ArrayPool<byte>.Shared.Return(original);
				}

				//更新缓存的数据量
				_bufferedCount += count;
				//构建待批量上传的片段
				multipart = new StorageMultipart(_multiparts.Count, _buffer, 0, _bufferedCount);
			}

			//将批量上传片段进行上传
			await this.FlushAsync(multipart, cancellation);
		}

		public ValueTask<long> FlushAsync(CancellationToken cancellation = default)
		{
			//确认当前是否可用的
			this.EnsureDisposed();

			if(string.IsNullOrEmpty(_identifier) || _buffer == null || _bufferedCount == 0)
				return ValueTask.FromResult(0L);

			//构建待批量上传的片段
			var multipart = new StorageMultipart(_multiparts.Count, _buffer, 0, _bufferedCount);

			//将批量上传片段进行上传
			return this.FlushAsync(multipart, cancellation);
		}
		#endregion

		#region 私有方法
		private void Done()
		{
			_identifier = null;
			_bufferedCount = 0;

			if(_buffer != null)
			{
				ArrayPool<byte>.Shared.Return(_buffer);
				_buffer = null;
			}
		}

		private async ValueTask<string> InitiateAsync(CancellationToken cancellation)
		{
			//创建初始化请求包
			var request = _client.CreateHttpRequest(HttpMethod.Post, _path + "?uploads", _client.EnsureCreation(_extendedProperties));

			//保持长连接
			request.Headers.Connection.Add("keep-alive");

			//尝试设置文件类型
			if(_extendedProperties.TryGetValue("FileType", out var value) && value is string fileType && MediaTypeHeaderValue.TryParse(fileType, out var contentType))
			{
				request.Content = new ByteArrayContent(Array.Empty<byte>());
				request.Content.Headers.ContentType = contentType;
			}
			else if(Zongsoft.IO.Mime.TryGetMimeType(_path, out var mimeType) && MediaTypeHeaderValue.TryParse(mimeType, out contentType))
			{
				request.Content = new ByteArrayContent(Array.Empty<byte>());
				request.Content.Headers.ContentType = contentType;
			}

			var response = await _client.HttpClient.SendAsync(request, cancellation);

			//确保应答状态是成功
			response.EnsureSuccessStatusCode();

			//解析应答结果中的上传编号
			var match = _Initiate_Regex_.Match(response.Content.ReadAsStringAsync(cancellation).Result);

			return match.Success ? match.Groups["value"].Value : null;
		}

		private async ValueTask<string> UploadAsync(StorageMultipart multipart, int retries, CancellationToken cancellation = default)
		{
			//创建请求
			var request = _client.CreateHttpRequest(HttpMethod.Put, _path + $"?partNumber={multipart.Index + 1}&uploadId={_identifier}", _extendedProperties);

			//保持长连接
			request.Headers.Connection.Add("keep-alive");
			//设置请求内容
			request.Content = new ByteArrayContent(multipart.Data, multipart.Offset, multipart.Count);

			//上传数据段
			var response = await _client.HttpClient.SendAsync(request, cancellation);

			//如果应答成功，则获取其返回的上传片段的校验标识
			if(response.IsSuccessStatusCode)
				return multipart.Checksum = response.Headers.ETag.Tag;

			//如果重试次数为零则返回空
			if(retries < 1)
				return null;

			await Task.Delay(TimeSpan.FromMilliseconds(Random.Shared.Next(10, 999)), cancellation);
			return await this.UploadAsync(multipart, retries - 1, cancellation);
		}

		private async ValueTask<long> FlushAsync(StorageMultipart multipart, CancellationToken cancellation = default)
		{
			//批量上传当前片段(支持失败重试)
			multipart.Checksum = await this.UploadAsync(multipart, RETRY_COUNT, cancellation);

			//如果发送失败，则取消整个批量上传并返回
			if(string.IsNullOrEmpty(multipart.Checksum))
			{
				await this.AbortAsync(cancellation);
				return 0L;
			}

			//重置缓存数量
			_bufferedCount = 0;
			//将发送成功的片段加入到列表中
			_multiparts.Add(multipart);
			//返回发送成功的字节数
			return multipart.Count;
		}

		private async ValueTask<bool> CompleteAsync(CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(_identifier))
				return false;

			async ValueTask<bool> FlushBufferAsync(CancellationToken cancellation)
			{
				if(_bufferedCount <= 0)
					return true;

				var multipart = new StorageMultipart(_multiparts == null ? 1 : _multiparts.Count + 1, _buffer, 0, _bufferedCount);
				return (await this.FlushAsync(multipart, cancellation)) > 0;
			}

			//尝试将缓存区内的部分数据补发完成（注：补发失败则终止批量上传任务并退出）
			if(!(await FlushBufferAsync(cancellation)))
				return true;

			var text = new StringBuilder("<CompleteMultipartUpload>");

			for(int i = 0; i < _multiparts.Count; i++)
			{
				text.Append(
				$"<Part>" +
					$"<PartNumber>{_multiparts[i].Index + 1}</PartNumber>" +
					$"<ETag>\"{_multiparts[i].Checksum}\"</ETag>" +
				$"</Part>");
			}

			text.Append("</CompleteMultipartUpload>");

			var response = await _client.HttpClient.PostAsync(
				_client.ServiceCenter.GetRequestUrl(_path + "?uploadId=" + _identifier),
				new StringContent(text.ToString()),
				cancellation);

			if(response.IsSuccessStatusCode)
			{
				this.Done();
				return true;
			}

			return await this.AbortAsync(cancellation);
		}

		private async ValueTask<bool> AbortAsync(CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(_identifier))
				return false;

			try
			{
				_client.HttpClient.DefaultRequestHeaders.ConnectionClose = true;
				var response = await _client.HttpClient.DeleteAsync(_client.ServiceCenter.GetRequestUrl($"{_path}?uploadId={_identifier}"), cancellation);

				if(response.IsSuccessStatusCode)
					this.Done();

				return response.IsSuccessStatusCode;
			}
			catch
			{
				return false;
			}
		}

		private void EnsureDisposed()
		{
			if(_disposed != 0)
				throw new ObjectDisposedException(typeof(StorageUploader).FullName);
		}
		#endregion

		#region 释放资源
		public async ValueTask DisposeAsync()
		{
			await this.DisposeAsync(true);
			GC.SuppressFinalize(this);
		}

		protected virtual async Task DisposeAsync(bool disposing)
		{
			//设置释放标志
			var disposed = Interlocked.Exchange(ref _disposed, 1);

			//如果已经释放了则退出
			if(disposed != 0)
				return;

			try
			{
				//确认提交整个上传操作
				await this.CompleteAsync();
			}
			catch
			{
			}
		}
		#endregion

		#region 嵌套子类
		internal struct StorageMultipart
		{
			#region 成员字段
			private string _checksum;
			private readonly int _index;
			private readonly ArraySegment<byte> _data;
			#endregion

			#region 构造函数
			public StorageMultipart(int index, byte[] data, int offset, int count)
			{
				_index = index;
				_data = new ArraySegment<byte>(data, offset, count);
				this.Offset = offset;
				this.Count = count;
			}
			#endregion

			#region 公共属性
			/// <summary>获取批量上传的序号（从零开始）。</summary>
			public int Index => _index;

			/// <summary>获取批量上传的数据段。</summary>
			public byte[] Data => _data.Array;

			//注意：该属性不能通过Data属性动态获取，因为Data属性所指向的缓存可能已经被释放。
			public int Offset { get; }

			//注意：该属性不能通过Data属性动态获取，因为Data属性所指向的缓存可能已经被释放。
			public int Count { get; }

			/// <summary>获取或设置上传成功的校验标识。</summary>
			public string Checksum
			{
				get => _checksum;
				set => _checksum = string.IsNullOrEmpty(value) ? null : value.Trim('"');
			}
			#endregion
		}
		#endregion
	}
}
