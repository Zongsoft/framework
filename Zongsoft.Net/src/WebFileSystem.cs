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
 * Copyright (C) 2010-2024 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Net library.
 *
 * The Zongsoft.Net is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Net is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Net library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.IO;
using Zongsoft.Services;

namespace Zongsoft.Net;

public class WebFileSystem : Zongsoft.IO.IFileSystem
{
	public string Scheme => "zfs.web";
	public IFile File => new FileProvider(ApplicationContext.Current.Services.ResolveRequired<IHttpClientFactory>());
	public IDirectory Directory => null;

	public string GetUrl(string virtualPath) => Zongsoft.IO.Path.TryParse(virtualPath, out var path) ? this.GetUrl(path) : virtualPath;
	public string GetUrl(Zongsoft.IO.Path path) => $"http://{path.FullPath.TrimStart('/')}";

	private sealed class FileProvider(IHttpClientFactory factory) : Zongsoft.IO.IFile
	{
		private readonly IHttpClientFactory _factory = factory;

		public void Copy(string source, string destination) => this.Copy(source, destination, true);
		public void Copy(string source, string destination, bool overwrite) => throw new NotImplementedException();
		public Task CopyAsync(string source, string destination) => this.CopyAsync(source, destination, true);
		public Task CopyAsync(string source, string destination, bool overwrite) => throw new NotImplementedException();
		public bool Delete(string path) => throw new NotImplementedException();
		public Task<bool> DeleteAsync(string path) => throw new NotImplementedException();
		public bool Exists(string path) => throw new NotImplementedException();
		public Task<bool> ExistsAsync(string path) => throw new NotImplementedException();
		public Zongsoft.IO.FileInfo GetInfo(string path) => throw new NotImplementedException();
		public Task<Zongsoft.IO.FileInfo> GetInfoAsync(string path) => throw new NotImplementedException();
		public void Move(string source, string destination) => throw new NotImplementedException();
		public Task MoveAsync(string source, string destination) => throw new NotImplementedException();
		public Stream Open(string path, IDictionary<string, object> properties = null) => this.Open(path, FileMode.Open, FileAccess.Read, FileShare.None, properties);
		public Stream Open(string path, FileMode mode, IDictionary<string, object> properties = null) => this.Open(path, mode, FileAccess.Read, FileShare.None, properties);
		public Stream Open(string path, FileMode mode, FileAccess access, IDictionary<string, object> properties = null) => this.Open(path, mode, access, FileShare.None, properties);
		public Stream Open(string path, FileMode mode, FileAccess access, FileShare share, IDictionary<string, object> properties = null)
		{
			var client = this.GetClient(path, out var p);
			bool writable = (mode != FileMode.Open) || (access & FileAccess.Write) == FileAccess.Write;

			if(writable)
				return new WebUploadStream(client, p);

			var response = client.Send(new HttpRequestMessage(HttpMethod.Get, p));
			response.EnsureSuccessStatusCode();
			return response.Content.ReadAsStream();
		}

		public bool SetInfo(string path, IDictionary<string, object> properties) => throw new NotImplementedException();
		public Task<bool> SetInfoAsync(string path, IDictionary<string, object> properties) => throw new NotImplementedException();

		private HttpClient GetClient(ReadOnlySpan<char> fullPath, out string path)
		{
			if(fullPath.IsEmpty)
				throw new ArgumentNullException(nameof(fullPath));

			var client = _factory.CreateClient();
			var index = fullPath.IndexOf('/');

			if(index == 0)
			{
				fullPath = fullPath[1..];
				index = fullPath.IndexOf('/');
			}

			if(index < 0)
			{
				path = string.Empty;
				client.BaseAddress = new Uri($"http://{fullPath.Trim('/')}/Files/");
			}
			else
			{
				path = fullPath[(index + 1)..].ToString();
				client.BaseAddress = new Uri($"http://{fullPath[..index].Trim('/')}/Files/");
			}

			return client;
		}
	}

	private sealed class WebUploadStream : Stream
	{
		private readonly HttpClient _client;
		private readonly string _path;
		private readonly string _filePath;
		private readonly FileStream _fileStream;

		public WebUploadStream(HttpClient client, string path)
		{
			_client = client;
			_path = path;
			_filePath = System.IO.Path.GetTempFileName();
			_fileStream = System.IO.File.Open(_filePath, FileMode.Create, FileAccess.ReadWrite);
		}

		public override bool CanSeek => false;
		public override bool CanRead => false;
		public override bool CanWrite => true;
		public override long Length => _fileStream.Length;
		public override long Position { get => _fileStream.Position; set => _fileStream.Position = value; }

		public override void Flush() { }
		public override int Read(byte[] buffer, int offset, int count) => _fileStream.Read(buffer, offset, count);
		public override long Seek(long offset, SeekOrigin origin) => _fileStream.Seek(offset, origin);
		public override void SetLength(long value) => _fileStream.SetLength(value);
		public override void Write(byte[] buffer, int offset, int count) => _fileStream.Write(buffer, offset,count);

		protected override void Dispose(bool disposing)
		{
			_fileStream.Seek(0, SeekOrigin.Begin);

			var content = new MultipartFormDataContent
			{
				{ new StreamContent(_fileStream), "File", System.IO.Path.GetFileName(_path) }
			};

			var request = new HttpRequestMessage(HttpMethod.Post, System.IO.Path.GetDirectoryName(_path))
			{
				Content = content,
			};

			_client.Send(request);
			_client.Dispose();
			_fileStream.Dispose();
			System.IO.File.Delete(_filePath);
		}
	}
}
