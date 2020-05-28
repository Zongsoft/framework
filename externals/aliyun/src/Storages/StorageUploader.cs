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
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Net.Http;
using System.Threading;

namespace Zongsoft.Externals.Aliyun.Storages
{
	public class StorageUploader : MarshalByRefObject, IDisposable
	{
		#region 常量定义
		private const int MINIMUM_BUFFER_SIZE = 100 * 1024;
		private const int MAXIMUM_BUFFER_SIZE = 10 * 1024 * 1024;
		private const int DEFAULT_BUFFER_SIZE = 512 * 1024;

		private static readonly StorageMultipart[] EMPTY_MULTIPARTS = new StorageMultipart[0];
		private static readonly Regex _initiate_regex = new Regex(@"\<UploadId\>(?<value>\w+)\<\/UploadId\>", RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace);
		#endregion

		#region 私有字段
		private string _id;
		private string _path;
		private long _length;	//表示累计上传的字节总数(包括上传成功和失败)
		private int _offset;	//表示当前缓存的写入偏移量(即当前缓存中已写入字节数)
		private byte[] _buffer;	//表示当前的缓存
		private List<StorageMultipart> _multiparts;
		private IDictionary<string, object> _extendedProperties;
		private StorageClient _client;

		private int _isFinish;
		private int _isDisposed;
		#endregion

		#region 构造函数
		public StorageUploader(StorageClient client, string path) : this(client, path, null, DEFAULT_BUFFER_SIZE)
		{
		}

		public StorageUploader(StorageClient client, string path, int bufferSize) : this(client, path, null, bufferSize)
		{
		}

		public StorageUploader(StorageClient client, string path, IDictionary<string, object> extendedProperties, int bufferSize = DEFAULT_BUFFER_SIZE)
		{
			if(client == null)
				throw new ArgumentNullException("client");

			if(string.IsNullOrWhiteSpace(path))
				throw new ArgumentNullException("path");

			if(bufferSize > MAXIMUM_BUFFER_SIZE)
				throw new ArgumentOutOfRangeException("bufferSize");

			_client = client;
			_path = path.Trim().TrimEnd('/', '\\');

			_length = 0;
			_offset = 0;
			_buffer = new byte[Math.Max(bufferSize, MINIMUM_BUFFER_SIZE)];
			_multiparts = new List<StorageMultipart>();
			_extendedProperties = extendedProperties;
		}
		#endregion

		#region 公共属性
		public string Id
		{
			get => _id;
		}

		public string Path
		{
			get => _path;
		}

		public long Length
		{
			get => _length + _offset;
		}

		public long Position
		{
			get
			{
				//确认当前是否可用的
				this.EnsureDisposed();
				return _length + _offset;
			}
		}

		public int BufferSize
		{
			get
			{
				//确认当前是否可用的
				this.EnsureDisposed();
				return _buffer.Length;
			}
		}

		public StorageMultipart[] Multiparts
		{
			get
			{
				//确认当前是否可用的
				this.EnsureDisposed();

				if(_multiparts == null || _multiparts.Count < 1)
					return EMPTY_MULTIPARTS;

				return _multiparts.ToArray();
			}
		}
		#endregion

		#region 公共方法
		public void Write(byte[] buffer, int offset, int count)
		{
			//确认当前是否可用的
			this.EnsureDisposed();

			//如果当前上传已经提交或终止，则不能再写入
			if(this.EnsureFinish(false))
				throw new InvalidOperationException("The operation was completed or aborted.");

			if(buffer == null)
				throw new ArgumentNullException("buffer");

			if(offset < 0 || offset >= buffer.Length)
				throw new ArgumentOutOfRangeException("offset");

			if(offset + count > buffer.Length)
				throw new ArgumentOutOfRangeException("count");

			for(int i = 0; i < count; i++, _offset++)
			{
				if(_offset == _buffer.Length)
				{
					//重置缓存偏移量
					_offset = 0;
					//累加已满的总长度(即累加已上传的总长度)
					_length += _buffer.Length;

					//缓存满，上传
					this.Flush(_buffer, _buffer.Length, true);
				}

				_buffer[_offset] = buffer[offset + i];
			}
		}

		public void Flush()
		{
			//确认当前是否可用的
			this.EnsureDisposed();

			//将当前剩余的缓存数据上传
			this.FlushRemainder();
		}

		public void Complete()
		{
			//如果已经被终止或者尚未开始上传则退出
			if(this.EnsureFinish(true))
				return;

			//将当前剩余的缓存数据上传
			this.FlushRemainder();

			var text = new StringBuilder("<CompleteMultipartUpload>");

			foreach(var multipart in _multiparts)
			{
				//第一步：如果有部分内容是上传失败的则重试一次
				if(multipart.Status == StorageMultipartStatus.Failed)
					multipart.Checksum = this.Flush(multipart.Data, multipart.Data.Length, false);

				//第二步：如果还有部分内容重发失败则取消整个上传操作
				if(string.IsNullOrEmpty(multipart.Checksum))
				{
					this.Abort();
					return;
				}

				text.AppendFormat("<Part><PartNumber>{0}</PartNumber><ETag>\"{1}\"</ETag></Part>\r\n", multipart.Index.ToString(), multipart.Checksum);
			}

			text.Append("</CompleteMultipartUpload>");

			try
			{
				_client.HttpClient.PostAsync(_client.ServiceCenter.GetRequestUrl(_path + "?uploadId=" + _id), new System.Net.Http.StringContent(text.ToString())).ContinueWith(t =>
				{
					t.Result.EnsureSuccessStatusCode();

					_id = null;
					_length = 0;
					_offset = 0;

					var multiparts = _multiparts;

					if(multiparts != null)
						multiparts.Clear();
				}).Wait();
			}
			catch
			{
				//重置完结状态
				_isFinish = 0;

				//取消整个上传任务
				this.Abort();

				//重抛异常
				throw;
			}
		}

		public void Abort()
		{
			//如果已经被终止或者尚未开始上传则退出
			if(this.EnsureFinish(true))
				return;

			try
			{
				_client.HttpClient.DefaultRequestHeaders.ConnectionClose = true;

				_client.HttpClient.DeleteAsync(_client.ServiceCenter.GetRequestUrl(_path + "?uploadId=" + _id)).ContinueWith(t =>
				{
					t.Result.EnsureSuccessStatusCode();

					_id = null;
					_length = 0;
					_offset = 0;

					var multiparts = _multiparts;

					if(multiparts != null)
						multiparts.Clear();
				}).Wait();
			}
			catch
			{
				//重置完结状态
				_isFinish = 0;

				//重抛异常
				throw;
			}
		}
		#endregion

		#region 私有方法
		private void Initiate()
		{
			//如果初始化已经成功则返回
			if(!string.IsNullOrWhiteSpace(_id))
				return;

			//创建初始化请求包
			var request = _client.CreateHttpRequest(HttpMethod.Post, _path + "?uploads", _client.EnsureCreatedTime(_extendedProperties));

			//保持长连接
			request.Headers.Connection.Add("keep-alive");

			_client.HttpClient.SendAsync(request).ContinueWith(t =>
			{
				//初始化方法必须确保返回状态是成功的
				t.Result.EnsureSuccessStatusCode();

				var match = _initiate_regex.Match(t.Result.Content.ReadAsStringAsync().Result);

				if(!match.Success)
					throw new InvalidDataException();

				_id = match.Groups["value"].Value;
			}).Wait();
		}

		private void FlushRemainder()
		{
			if(_offset < 1)
				return;

			//本地上传的字节数
			var count = _offset;

			//重置缓存偏移量
			_offset = 0;
			//累加已满的总长度(即累加已上传的总长度)
			_length += count;

			//上传本次的缓存
			this.Flush(_buffer, count, true);
		}

		private string Flush(byte[] buffer, int count, bool generateRequires)
		{
			if(buffer == null)
				throw new ArgumentNullException("buffer");

			if(count > buffer.Length)
				throw new ArgumentOutOfRangeException("count");

			if(count < 1)
				return null;

			//按需发起初始化请求
			this.Initiate();

			StorageMultipart multipart = null;

			if(generateRequires)
			{
				//创建一个上传的数据块
				multipart = new StorageMultipart((int)Math.Ceiling((double)_length / _buffer.Length));

				//将该数据块加入到列表中
				_multiparts.Add(multipart);
			}

			//创建请求
			var request = _client.CreateHttpRequest(HttpMethod.Put, _path + string.Format("?partNumber={0}&uploadId={1}", multipart.Index, _id), _extendedProperties);

			//保持长连接
			request.Headers.Connection.Add("keep-alive");
			//设置请求内容
			request.Content = new ByteArrayContent(_buffer, 0, count);

			return _client.HttpClient.SendAsync(request).ContinueWith(t =>
			{
				if(t.Result.IsSuccessStatusCode)
				{
					if(multipart != null)
						multipart.Checksum = t.Result.Headers.ETag.Tag;

					return t.Result.Headers.ETag.Tag;
				}
				else
				{
					if(multipart != null)
						multipart.SetData(_buffer, 0, count);

					return null;
				}
			}).Result;
		}

		private void EnsureDisposed()
		{
			if(_isDisposed != 0)
				throw new ObjectDisposedException(typeof(StorageUploader).FullName);
		}

		private bool EnsureFinish(bool updateFlag)
		{
			const int FINISH_FLAG = -1;

			var isFinish = _isFinish;

			if(updateFlag)
				isFinish = Interlocked.Exchange(ref _isFinish, FINISH_FLAG);

			return isFinish == FINISH_FLAG;
		}
		#endregion

		#region 释放资源
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			const int DISPOSED_FLAG = -1;

			//设置释放标志
			int isDisposed = Interlocked.Exchange(ref _isDisposed, DISPOSED_FLAG);

			//如果已经释放了则退出
			if(isDisposed == DISPOSED_FLAG)
				return;

			try
			{
				//确认提交整个上传操作
				this.Complete();
			}
			finally
			{
				_id = null;
				_length = 0;
				_offset = 0;
				_buffer = null;
				_multiparts = null;
				_extendedProperties = null;
				_client = null;
			}
		}
		#endregion

		#region 嵌套子类
		public enum StorageMultipartStatus
		{
			None,
			Succeed,
			Failed,
		}

		public class StorageMultipart
		{
			#region 成员字段
			private int _index;
			private string _checksum;
			private byte[] _data;
			#endregion

			#region 构造函数
			internal StorageMultipart(int index)
			{
				_index = index;
			}
			#endregion

			#region 公共属性
			public int Index
			{
				get
				{
					return _index;
				}
			}

			public string Checksum
			{
				get
				{
					return _checksum;
				}
				internal set
				{
					if(string.IsNullOrEmpty(value))
						_checksum = null;
					else
					{
						_checksum = value.Trim('"');
						_data = null;
					}
				}
			}

			public byte[] Data
			{
				get
				{
					return _data;
				}
			}

			public StorageMultipartStatus Status
			{
				get
				{
					if(string.IsNullOrEmpty(_checksum))
						return _data == null ? StorageMultipartStatus.None : StorageMultipartStatus.Failed;

					return StorageMultipartStatus.Succeed;
				}
			}
			#endregion

			#region 内部方法
			internal void SetData(byte[] buffer, int offset, int count)
			{
				if(buffer == null || buffer.Length < 1)
				{
					_data = null;
					return;
				}

				_data = new byte[buffer.Length];
				Buffer.BlockCopy(buffer, offset, _data, 0, count);
			}
			#endregion
		}
		#endregion
	}
}
