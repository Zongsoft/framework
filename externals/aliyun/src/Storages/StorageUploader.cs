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
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Zongsoft.Externals.Aliyun.Storages
{
	public class StorageUploader : MarshalByRefObject, IAsyncDisposable
	{
		#region 常量定义
		//注意：阿里云OSS批量上传的要求是每个包大小必须是100KB~5GB
		private const int MINIMUM_BUFFER_SIZE = 100 * 1024;
		private const int MAXIMUM_BUFFER_SIZE = 100 * 1024 * 1024;
		private const int DEFAULT_BUFFER_SIZE = 512 * 1024;

		private static readonly Regex _Initiate_Regex_ = new(@"\<UploadId\>(?<value>\w+)\<\/UploadId\>", RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace);
		#endregion

		#region 私有字段
		private int _disposed;
		private string _identifier;
		private readonly string _path;
		private ArraySegment<byte> _data;
		private readonly int _blockSize;
		private long _count;
		private StorageClient _client;
		private StorageMultipart?[] _multiparts;
		private readonly IDictionary<string, object> _extendedProperties;
		#endregion

		#region 构造函数
		public StorageUploader(StorageClient client, string path) : this(client, path, null, DEFAULT_BUFFER_SIZE) { }
		public StorageUploader(StorageClient client, string path, int bufferSize) : this(client, path, null, bufferSize) { }
		public StorageUploader(StorageClient client, string path, IDictionary<string, object> extendedProperties, int bufferSize = DEFAULT_BUFFER_SIZE)
		{
			if(string.IsNullOrWhiteSpace(path))
				throw new ArgumentNullException(nameof(path));
			if(bufferSize > MAXIMUM_BUFFER_SIZE)
				throw new ArgumentOutOfRangeException(nameof(bufferSize));

			_path = path.Trim().TrimEnd('/', '\\');
			_client = client ?? throw new ArgumentNullException(nameof(client));
			_blockSize = Math.Max(bufferSize, MINIMUM_BUFFER_SIZE);
			_extendedProperties = extendedProperties;
		}
		#endregion

		#region 公共属性
		public string Identifier => _identifier;
		public string Path => _path;
		public long Count => _count;
		#endregion

		#region 公共方法
		public Task<long> UploadAsync(byte[] data, int offset, CancellationToken cancellation = default) => this.UploadAsync(data, offset, -1, cancellation);
		public async Task<long> UploadAsync(byte[] data, int offset, int count, CancellationToken cancellation = default)
		{
			//确认当前是否可用的
			this.EnsureDisposed();

			if(data == null)
				throw new ArgumentNullException(nameof(data));

			if(offset < 0 || offset >= data.Length)
				throw new ArgumentOutOfRangeException(nameof(offset));

			if(count < 0)
				count = data.Length - offset;
			else if(offset + count > data.Length)
				throw new ArgumentOutOfRangeException(nameof(count));

			_identifier = await this.InitiateAsync(cancellation);
			if(string.IsNullOrEmpty(_identifier))
				return 0L;

			_data = new ArraySegment<byte>(data, offset, count);
			_multiparts = new StorageMultipart?[(int)Math.Ceiling((double)count / _blockSize)];

			for(int i = 0; i < _multiparts.Length; i++)
			{
				var multipart = this.GetMultipart(i);
				multipart.Checksum = await this.FlushAsync(multipart, 1, cancellation);

				if(string.IsNullOrEmpty(multipart.Checksum))
				{
					await this.AbortAsync(cancellation);
					return 0;
				}

				_count += multipart.Count;
				_multiparts[i] = multipart;
			}

			await this.CompleteAsync(cancellation);
			return _count;
		}

		public async Task<long> FlushAsync(CancellationToken cancellation = default)
		{
			//确认当前是否可用的
			this.EnsureDisposed();

			long count = 0;

			for(int i = 0; i < _multiparts.Length; i++)
			{
				if(_multiparts[i] == null)
				{
					var multipart = this.GetMultipart(i);
					multipart.Checksum = await this.FlushAsync(multipart, 1, cancellation);

					if(!string.IsNullOrEmpty(multipart.Checksum))
					{
						_multiparts[i] = multipart;
						count += multipart.Count;
					}
				}
				else
					count += _multiparts[i].Value.Count;
			}

			return _count = count;
		}

		public async Task<bool> CompleteAsync(CancellationToken cancellation = default)
		{
			var text = new StringBuilder("<CompleteMultipartUpload>");

			for(int i = 0; i < _multiparts.Length; i++)
			{
				var multipart = _multiparts[i];

				if(multipart != null)
					text.Append($"<Part><PartNumber>{multipart.Value.Index + 1}</PartNumber><ETag>\"{multipart.Value.Checksum}\"</ETag></Part>");
			}

			text.Append("</CompleteMultipartUpload>");

			var response = await _client.HttpClient.PostAsync(
				_client.ServiceCenter.GetRequestUrl(_path + "?uploadId=" + _identifier),
				new StringContent(text.ToString()),
				cancellation);

			return response.IsSuccessStatusCode || await this.AbortAsync(cancellation);
		}

		public async Task<bool> AbortAsync(CancellationToken cancellation = default)
		{
			try
			{
				_client.HttpClient.DefaultRequestHeaders.ConnectionClose = true;
				return (await _client.HttpClient.DeleteAsync(_client.ServiceCenter.GetRequestUrl(_path + "?uploadId=" + _identifier), cancellation)).IsSuccessStatusCode;
			}
			catch
			{
				return false;
			}
		}
		#endregion

		#region 私有方法
		private StorageMultipart GetMultipart(int index)
		{
			if(index < 0 || index >= _multiparts.Length)
				throw new ArgumentOutOfRangeException(nameof(index));

			var offset = (index * _blockSize);
			var remainder = _data.Count % _blockSize;
			var count = (index == _multiparts.Length - 1) ? (remainder == 0 ? _blockSize : remainder) : _blockSize;

			return new(index, new ArraySegment<byte>(_data.Array, _data.Offset + offset, count));
		}

		private async Task<string> InitiateAsync(CancellationToken cancellation)
		{
			//创建初始化请求包
			var request = _client.CreateHttpRequest(HttpMethod.Post, _path + "?uploads", _client.EnsureCreatedTime(_extendedProperties));

			//保持长连接
			request.Headers.Connection.Add("keep-alive");

			var response = await _client.HttpClient.SendAsync(request, cancellation);

			//确保应答状态是成功
			response.EnsureSuccessStatusCode();

			//解析应答结果中的上传编号
			var match = _Initiate_Regex_.Match(response.Content.ReadAsStringAsync(cancellation).Result);

			return match.Success ? match.Groups["value"].Value : null;
		}

		private async Task<string> FlushAsync(StorageMultipart multipart, int retries, CancellationToken cancellation = default)
		{
			//创建请求
			var request = _client.CreateHttpRequest(HttpMethod.Put, _path + $"?partNumber={multipart.Index + 1}&uploadId={_identifier}", _extendedProperties);

			//保持长连接
			request.Headers.Connection.Add("keep-alive");
			//设置请求内容
			request.Content = new ByteArrayContent(multipart.Data, multipart.Offset, multipart.Count);

			//上传数据段
			var response = await _client.HttpClient.SendAsync(request, cancellation);

			//如果应答成功，则获取其返回的上传段的唯一标识
			if(response.IsSuccessStatusCode)
				return multipart.Checksum = response.Headers.ETag.Tag;

			//如果重试次数为零则返回空
			if(retries < 1)
				return null;

			await Task.Delay(TimeSpan.FromMilliseconds(Random.Shared.Next(10, 999)), cancellation);
			return await this.FlushAsync(multipart, retries - 1, cancellation);
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
			finally
			{
			}
		}
		#endregion

		#region 嵌套子类
		internal struct StorageMultipart
		{
			#region 成员字段
			private readonly int _index;
			private string _checksum;
			private readonly ArraySegment<byte> _data;
			#endregion

			#region 构造函数
			internal StorageMultipart(int index, ArraySegment<byte> data)
			{
				_index = index;
				_data = data;
			}
			#endregion

			#region 公共属性
			public int Index => _index;
			public byte[] Data => _data.Array;
			public int Offset => _data.Offset;
			public int Count => _data.Count;
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
