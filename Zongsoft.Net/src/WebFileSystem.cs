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
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.IO;
using Zongsoft.Services;

namespace Zongsoft.Net;

public class WebFileSystem : Zongsoft.IO.IFileSystem
{
	#region 公共属性
	public string Scheme => "zfs.web";
	public IFile File => new FileProvider(ApplicationContext.Current.Services.ResolveRequired<IHttpClientFactory>());
	public IDirectory Directory => new DirectoryProvider(ApplicationContext.Current.Services.ResolveRequired<IHttpClientFactory>());
	#endregion

	#region 公共方法
	public string GetUrl(string virtualPath) => Zongsoft.IO.Path.TryParse(virtualPath, out var path) ? this.GetUrl(path) : virtualPath;
	public string GetUrl(Zongsoft.IO.Path path) => $"http://{path.FullPath.TrimStart('/')}";
	#endregion

	private sealed class DirectoryProvider(IHttpClientFactory factory) : Zongsoft.IO.IDirectory
	{
		private static readonly System.Text.Json.JsonSerializerOptions _serialization = new(System.Text.Json.JsonSerializerDefaults.Web);
		private readonly IHttpClientFactory _factory = factory;

		#region 公共方法
		public bool Create(string fullPath, IEnumerable<KeyValuePair<string, string>> properties = null)
		{
			using var client = this.GetClient(fullPath, out var path);
			using var request = new HttpRequestMessage(HttpMethod.Post, path);
			using var response = client.Send(request);
			return response.IsSuccessStatusCode;
		}

		public async ValueTask<bool> CreateAsync(string fullPath, IEnumerable<KeyValuePair<string, string>> properties, CancellationToken cancellation = default)
		{
			using var client = this.GetClient(fullPath, out var path);
			using var response = await client.PostAsync(path, null, cancellation).ConfigureAwait(false);
			return response.IsSuccessStatusCode;
		}

		public bool Delete(string fullPath)
		{
			using var client = this.GetClient(fullPath, out var path);
			using var request = new HttpRequestMessage(HttpMethod.Delete, path);
			using var response = client.Send(request);
			return response.IsSuccessStatusCode;
		}

		public async ValueTask<bool> DeleteAsync(string fullPath, CancellationToken cancellation = default)
		{
			using var client = this.GetClient(fullPath, out var path);
			using var response = await client.DeleteAsync(path, cancellation).ConfigureAwait(false);
			return response.IsSuccessStatusCode;
		}

		public bool Exists(string fullPath)
		{
			using var client = this.GetClient(fullPath, out var path);
			using var request = new HttpRequestMessage(HttpMethod.Head, path);
			using var response = client.Send(request);
			return response.IsSuccessStatusCode;
		}

		public async ValueTask<bool> ExistsAsync(string fullPath, CancellationToken cancellation = default)
		{
			using var client = this.GetClient(fullPath, out var path);
			using var request = new HttpRequestMessage(HttpMethod.Head, path);
			using var response = await client.SendAsync(request, cancellation).ConfigureAwait(false);
			return response.IsSuccessStatusCode;
		}

		public IEnumerable<PathInfo> GetChildren(string fullPath, string pattern, bool recursive = false)
		{
			using var client = this.GetClient(fullPath, out var path);
			using var request = new HttpRequestMessage(HttpMethod.Get, $"{path}?pattern={Uri.EscapeDataString(pattern ?? string.Empty)}&recursive={recursive}");
			using var response = client.Send(request, HttpCompletionOption.ResponseHeadersRead);

			if(!response.IsSuccessStatusCode || response.Content.Headers.ContentLength == 0)
				yield break;

			using var stream = response.Content.ReadAsStream();
			var result = System.Text.Json.JsonSerializer.Deserialize<Result>(stream, _serialization);

			if(result?.Directories != null)
			{
				foreach(var directory in result.Directories)
					yield return new IO.DirectoryInfo(directory.Name, directory.Creation, directory.Modification);
			}

			if(result?.Files != null)
			{
				foreach(var file in result.Files)
					yield return new IO.FileInfo(file.Name, file.Size, file.Type, file.Creation, file.Modification);
			}
		}

		public async IAsyncEnumerable<PathInfo> GetChildrenAsync(string fullPath, string pattern, bool recursive, [System.Runtime.CompilerServices.EnumeratorCancellation]CancellationToken cancellation = default)
		{
			using var client = this.GetClient(fullPath, out var path);
			using var response = await client.GetAsync($"{path}?pattern={Uri.EscapeDataString(pattern ?? string.Empty)}&recursive={recursive}", HttpCompletionOption.ResponseHeadersRead, cancellation).ConfigureAwait(false);

			if(!response.IsSuccessStatusCode || response.Content.Headers.ContentLength == 0)
				yield break;

			using var stream = await response.Content.ReadAsStreamAsync(cancellation).ConfigureAwait(false);
			var result = await System.Text.Json.JsonSerializer.DeserializeAsync<Result>(stream, _serialization, cancellation).ConfigureAwait(false);

			if(result?.Directories != null)
			{
				foreach(var directory in result.Directories)
					yield return new IO.DirectoryInfo(directory.Name, directory.Creation, directory.Modification);
			}

			if(result?.Files != null)
			{
				foreach(var file in result.Files)
					yield return new IO.FileInfo(file.Name, file.Size, file.Type, file.Creation, file.Modification);
			}
		}

		public IEnumerable<IO.DirectoryInfo> GetDirectories(string fullPath, string pattern, bool recursive = false)
		{
			using var client = this.GetClient(fullPath, out var path);
			using var request = new HttpRequestMessage(HttpMethod.Get, $"{path}?mode=directory&pattern={Uri.EscapeDataString(pattern ?? string.Empty)}&recursive={recursive}");
			using var response = client.Send(request, HttpCompletionOption.ResponseHeadersRead);

			if(!response.IsSuccessStatusCode || response.Content.Headers.ContentLength == 0)
				return [];

			using var stream = response.Content.ReadAsStream();
			var infos = System.Text.Json.JsonSerializer.Deserialize<DirectoryInfo[]>(stream, _serialization);
			return infos?.Select(info => new IO.DirectoryInfo(info.Name, info.Creation, info.Modification)) ?? [];
		}

		public async IAsyncEnumerable<IO.DirectoryInfo> GetDirectoriesAsync(string fullPath, string pattern, bool recursive, [System.Runtime.CompilerServices.EnumeratorCancellation]CancellationToken cancellation = default)
		{
			using var client = this.GetClient(fullPath, out var path);
			using var response = await client.GetAsync($"{path}?mode=directory&pattern={Uri.EscapeDataString(pattern ?? string.Empty)}&recursive={recursive}", HttpCompletionOption.ResponseHeadersRead, cancellation).ConfigureAwait(false);

			if(!response.IsSuccessStatusCode || response.Content.Headers.ContentLength == 0)
				yield break;

			var infos = await response.Content.ReadFromJsonAsync<DirectoryInfo[]>(cancellation).ConfigureAwait(false);
			foreach(var info in infos ?? [])
				yield return new IO.DirectoryInfo(info.Name, info.Creation, info.Modification);
		}

		public IEnumerable<IO.FileInfo> GetFiles(string fullPath, string pattern, bool recursive = false)
		{
			using var client = this.GetClient(fullPath, out var path);
			using var request = new HttpRequestMessage(HttpMethod.Get, $"{path}?mode=file&pattern={Uri.EscapeDataString(pattern ?? string.Empty)}&recursive={recursive}");
			using var response = client.Send(request, HttpCompletionOption.ResponseHeadersRead);

			if(!response.IsSuccessStatusCode || response.Content.Headers.ContentLength == 0)
				return [];

			using var stream = response.Content.ReadAsStream();
			var infos = System.Text.Json.JsonSerializer.Deserialize<FileInfo[]>(stream, _serialization);
			return infos?.Select(info => new IO.FileInfo(info.Name, info.Size, info.Type, info.Creation, info.Modification)) ?? [];
		}

		public async IAsyncEnumerable<IO.FileInfo> GetFilesAsync(string fullPath, string pattern, bool recursive, [System.Runtime.CompilerServices.EnumeratorCancellation]CancellationToken cancellation = default)
		{
			using var client = this.GetClient(fullPath, out var path);
			using var response = await client.GetAsync($"{path}?mode=file&pattern={Uri.EscapeDataString(pattern ?? string.Empty)}&recursive={recursive}", HttpCompletionOption.ResponseHeadersRead, cancellation).ConfigureAwait(false);

			if(!response.IsSuccessStatusCode || response.Content.Headers.ContentLength == 0)
				yield break;

			var infos = await response.Content.ReadFromJsonAsync<FileInfo[]>(cancellation).ConfigureAwait(false);
			foreach(var info in infos ?? [])
				yield return new IO.FileInfo(info.Name, info.Size, info.Type, info.Creation, info.Modification);
		}

		public IO.DirectoryInfo GetInfo(string fullPath)
		{
			using var client = this.GetClient(fullPath, out var path);
			using var request = new HttpRequestMessage(HttpMethod.Head, path);
			using var response = client.Send(request);
			return GetDirectoryInfo(response);
		}

		public async ValueTask<IO.DirectoryInfo> GetInfoAsync(string fullPath, CancellationToken cancellation = default)
		{
			using var client = this.GetClient(fullPath, out var path);
			using var request = new HttpRequestMessage(HttpMethod.Head, path);
			using var response = await client.SendAsync(request, cancellation).ConfigureAwait(false);
			return GetDirectoryInfo(response);
		}

		public void Move(string source, string destination) => throw new NotSupportedException();
		public ValueTask MoveAsync(string source, string destination, CancellationToken cancellation = default) => throw new NotSupportedException();
		public bool SetInfo(string path, IEnumerable<KeyValuePair<string, string>> properties) => throw new NotSupportedException();
		public ValueTask<bool> SetInfoAsync(string path, IEnumerable<KeyValuePair<string, string>> properties, CancellationToken cancellation = default) => throw new NotSupportedException();
		#endregion

		#region 私有方法
		private HttpClient GetClient(ReadOnlySpan<char> fullPath, out string path) => WebFileSystem.GetClient(_factory, fullPath, "Directories", out path);

		private static Zongsoft.IO.DirectoryInfo GetDirectoryInfo(HttpResponseMessage response)
		{
			if(response == null || !response.IsSuccessStatusCode)
				return null;

			var name = GetHeader(response, "X-Directory-Name");
			var creation = DateTime.TryParse(GetHeader(response, "X-Directory-Creation"), out var datetime) ? datetime : DateTime.MinValue;
			var modification = DateTime.TryParse(GetHeader(response, "X-Directory-Modification"), out datetime) ? datetime : DateTime.MinValue;

			return new IO.DirectoryInfo(name, creation, modification);
		}
		#endregion

		private sealed class Result
		{
			#region 公共属性
			public DirectoryInfo[] Directories { get; set; }
			public FileInfo[] Files { get; set; }
			#endregion
		}

		private sealed class FileInfo
		{
			#region 公共属性
			public string Name { get; set; }
			public string Type { get; set; }
			public long Size { get; set; }
			public DateTime Creation { get; set; }
			public DateTime Modification { get; set; }
			#endregion
		}

		private sealed class DirectoryInfo
		{
			#region 公共属性
			public string Name { get; set; }
			public DateTime Creation { get; set; }
			public DateTime Modification { get; set; }
			#endregion
		}
	}

	private sealed class FileProvider(IHttpClientFactory factory) : Zongsoft.IO.IFile
	{
		private readonly IHttpClientFactory _factory = factory;

		#region 公共方法
		public void Copy(string source, string destination, bool overwrite = true) => throw new NotSupportedException();
		public ValueTask CopyAsync(string source, string destination, bool overwrite, CancellationToken cancellation = default) => throw new NotSupportedException();
		public void Move(string source, string destination) => throw new NotSupportedException();
		public ValueTask MoveAsync(string source, string destination, CancellationToken cancellation = default) => throw new NotSupportedException();
		public bool SetInfo(string path, IEnumerable<KeyValuePair<string, string>> properties) => throw new NotSupportedException();
		public ValueTask<bool> SetInfoAsync(string path, IEnumerable<KeyValuePair<string, string>> properties, CancellationToken cancellation = default) => throw new NotSupportedException();

		public bool Delete(string fullPath)
		{
			using var client = this.GetClient(fullPath, out var path);
			using var request = new HttpRequestMessage(HttpMethod.Delete, path);
			using var response = client.Send(request);
			return response.IsSuccessStatusCode;
		}

		public async ValueTask<bool> DeleteAsync(string fullPath, CancellationToken cancellation = default)
		{
			using var client = this.GetClient(fullPath, out var path);
			using var response = await client.DeleteAsync(path, cancellation).ConfigureAwait(false);
			return response.IsSuccessStatusCode;
		}

		public bool Exists(string fullPath)
		{
			using var client = this.GetClient(fullPath, out var path);
			using var request = new HttpRequestMessage(HttpMethod.Head, path);
			using var response = client.Send(request);
			return response.IsSuccessStatusCode;
		}

		public async ValueTask<bool> ExistsAsync(string fullPath, CancellationToken cancellation = default)
		{
			using var client = this.GetClient(fullPath, out var path);
			using var request = new HttpRequestMessage(HttpMethod.Head, path);
			using var response = await client.SendAsync(request, cancellation).ConfigureAwait(false);
			return response.IsSuccessStatusCode;
		}

		public Zongsoft.IO.FileInfo GetInfo(string fullPath)
		{
			using var client = this.GetClient(fullPath, out var path);
			using var request = new HttpRequestMessage(HttpMethod.Head, path);
			using var response = client.Send(request);
			return GetFileInfo(response);
		}

		public async ValueTask<Zongsoft.IO.FileInfo> GetInfoAsync(string fullPath, CancellationToken cancellation = default)
		{
			using var client = this.GetClient(fullPath, out var path);
			using var request = new HttpRequestMessage(HttpMethod.Head, path);
			using var response = await client.SendAsync(request, cancellation).ConfigureAwait(false);
			return GetFileInfo(response);
		}

		public Stream Open(string path, FileMode mode, FileAccess access, FileShare share, IEnumerable<KeyValuePair<string, string>> properties = null)
		{
			ValidateOpen(mode, access);
			var client = this.GetClient(path, out var p);
			HttpResponseMessage response = null;

			try
			{
				if(mode == FileMode.Create)
					return new UploadStream(client, p);

				using var request = new HttpRequestMessage(HttpMethod.Get, p);
				response = client.Send(request, HttpCompletionOption.ResponseHeadersRead);
				response.EnsureSuccessStatusCode();
				return new DownloadStream(client, response, response.Content.ReadAsStream());
			}
			catch
			{
				response?.Dispose();
				client.Dispose();
				throw;
			}
		}

		public async ValueTask<Stream> OpenAsync(string path, FileMode mode, FileAccess access, FileShare share, IEnumerable<KeyValuePair<string, string>> properties, CancellationToken cancellation = default)
		{
			cancellation.ThrowIfCancellationRequested();
			ValidateOpen(mode, access);
			var client = this.GetClient(path, out var p);
			HttpResponseMessage response = null;

			try
			{
				if(mode == FileMode.Create)
					return new UploadStream(client, p);

				using var request = new HttpRequestMessage(HttpMethod.Get, p);
				response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellation).ConfigureAwait(false);
				response.EnsureSuccessStatusCode();
				var stream = await response.Content.ReadAsStreamAsync(cancellation).ConfigureAwait(false);
				return new DownloadStream(client, response, stream);
			}
			catch
			{
				response?.Dispose();
				client.Dispose();
				throw;
			}
		}
		#endregion

		#region 私有方法
		private HttpClient GetClient(ReadOnlySpan<char> fullPath, out string path) => WebFileSystem.GetClient(_factory, fullPath, "Files", out path);

		private static void ValidateOpen(FileMode mode, FileAccess access)
		{
			if(!Enum.IsDefined(mode))
				throw new ArgumentOutOfRangeException(nameof(mode));
			if(!Enum.IsDefined(access))
				throw new ArgumentOutOfRangeException(nameof(access));

			//Web 文件服务仅提供完整下载与覆盖上传，不模拟追加、独占创建或远程随机读写。
			if((mode, access) is not ((FileMode.Open, FileAccess.Read) or (FileMode.Create, FileAccess.Write)))
				throw new NotSupportedException();
		}

		private static Zongsoft.IO.FileInfo GetFileInfo(HttpResponseMessage response)
		{
			if(response == null || !response.IsSuccessStatusCode)
				return null;

			var name = GetHeader(response, "X-File-Name");
			var size = long.TryParse(GetHeader(response, "X-File-Size"), out var number) ? number : 0;
			var type = GetHeader(response, "X-File-Type");
			var creation = DateTime.TryParse(GetHeader(response, "X-File-Creation"), out var datetime) ? datetime : DateTime.MinValue;
			var modification = DateTime.TryParse(GetHeader(response, "X-File-Modification"), out datetime) ? datetime : DateTime.MinValue;

			return new IO.FileInfo(name, size, type, creation, modification);
		}
		#endregion

		private sealed class DownloadStream(HttpClient client, HttpResponseMessage response, Stream stream) : Stream
		{
			#region 成员字段
			private Stream _stream = stream;
			#endregion

			#region 成员属性
			private Stream Content => _stream ?? throw new ObjectDisposedException(nameof(DownloadStream));

			public override bool CanRead => _stream?.CanRead == true;
			public override bool CanSeek => _stream?.CanSeek == true;
			public override bool CanWrite => false;
			public override long Length => this.Content.Length;
			public override long Position { get => this.Content.Position; set => this.Content.Position = value; }
			#endregion

			#region 重写方法
			public override void Flush() => this.Content.Flush();
			public override Task FlushAsync(CancellationToken cancellation) => this.Content.FlushAsync(cancellation);
			public override int Read(byte[] buffer, int offset, int count) => this.Content.Read(buffer, offset, count);
			public override int Read(Span<byte> buffer) => this.Content.Read(buffer);
			public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellation) => this.Content.ReadAsync(buffer, offset, count, cancellation);
			public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellation = default) => this.Content.ReadAsync(buffer, cancellation);
			public override long Seek(long offset, SeekOrigin origin) => this.Content.Seek(offset, origin);
			public override void SetLength(long value) => throw new NotSupportedException();
			public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
			#endregion

			#region 释放资源
			protected override void Dispose(bool disposing)
			{
				if(disposing && Interlocked.Exchange(ref _stream, null) != null)
				{
					try { response.Dispose(); }
					finally { client.Dispose(); }
				}

				base.Dispose(disposing);
			}

			public override async ValueTask DisposeAsync()
			{
				var content = Interlocked.Exchange(ref _stream, null);
				if(content == null)
					return;

				try { await content.DisposeAsync().ConfigureAwait(false); }
				finally
				{
					try { response.Dispose(); }
					finally { client.Dispose(); }

					base.Dispose(true);
					GC.SuppressFinalize(this);
				}
			}
			#endregion
		}

		private sealed class UploadStream : Stream
		{
			#region 成员字段
			private readonly HttpClient _client;
			private readonly string _path;
			private FileStream _fileStream;
			#endregion

			#region 构造函数
			public UploadStream(HttpClient client, string path)
			{
				_client = client;
				_path = path;
				var temporary = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName());
				_fileStream = new FileStream(temporary, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 4096,
					FileOptions.Asynchronous | FileOptions.SequentialScan | FileOptions.DeleteOnClose);
			}
			#endregion

			#region 成员属性
			private FileStream Content => _fileStream ?? throw new ObjectDisposedException(nameof(UploadStream));
			public override bool CanSeek => _fileStream != null;
			public override bool CanRead => false;
			public override bool CanWrite => _fileStream != null;
			public override long Length => this.Content.Length;
			public override long Position { get => this.Content.Position; set => this.Content.Position = value; }
			#endregion

			#region 重写方法
			public override void Flush() => this.Content.Flush();
			public override Task FlushAsync(CancellationToken cancellation) => this.Content.FlushAsync(cancellation);
			public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
			public override long Seek(long offset, SeekOrigin origin) => this.Content.Seek(offset, origin);
			public override void SetLength(long value) => this.Content.SetLength(value);
			public override void Write(byte[] buffer, int offset, int count) => this.Content.Write(buffer, offset, count);
			public override void Write(ReadOnlySpan<byte> buffer) => this.Content.Write(buffer);
			public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellation) => this.Content.WriteAsync(buffer, offset, count, cancellation);
			public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellation = default) => this.Content.WriteAsync(buffer, cancellation);
			#endregion

			#region 释放资源
			protected override void Dispose(bool disposing)
			{
				if(!disposing)
					return;

				var stream = Interlocked.Exchange(ref _fileStream, null);
				if(stream == null)
					return;

				try
				{
					using var request = this.CreateRequest(stream);
					using var response = _client.Send(request);
					response.EnsureSuccessStatusCode();
				}
				finally
				{
					try { stream.Dispose(); }
					finally { _client.Dispose(); }

					base.Dispose(disposing);
				}
			}

			public override async ValueTask DisposeAsync()
			{
				var stream = Interlocked.Exchange(ref _fileStream, null);
				if(stream == null)
					return;

				try
				{
					using var request = this.CreateRequest(stream);
					using var response = await _client.SendAsync(request).ConfigureAwait(false);
					response.EnsureSuccessStatusCode();
				}
				finally
				{
					try { await stream.DisposeAsync().ConfigureAwait(false); }
					finally { _client.Dispose(); }

					base.Dispose(true);
					GC.SuppressFinalize(this);
				}
			}
			#endregion

			#region 私有方法
			private HttpRequestMessage CreateRequest(FileStream stream)
			{
				stream.Seek(0, SeekOrigin.Begin);
				var index = _path.LastIndexOf('/');
				return new HttpRequestMessage(HttpMethod.Post, index < 0 ? string.Empty : _path[..index])
				{
					Content = new MultipartFormDataContent
					{
						{ new StreamContent(stream), "File", _path[(index + 1)..] }
					},
				};
			}
			#endregion
		}
	}

	#region 私有方法
	private static HttpClient GetClient(IHttpClientFactory factory, ReadOnlySpan<char> fullPath, string category, out string path)
	{
		if(fullPath.IsEmpty)
			throw new ArgumentNullException(nameof(fullPath));

		fullPath = fullPath.TrimStart('/');
		var index = fullPath.IndexOf('/');
		path = index < 0 ? string.Empty : fullPath[(index + 1)..].ToString();
		var authority = index < 0 ? fullPath : fullPath[..index];
		var address = new Uri($"http://{authority}/{category}/");
		var client = factory.CreateClient();

		try
		{
			client.BaseAddress = address;
			return client;
		}
		catch
		{
			client.Dispose();
			throw;
		}
	}

	private static string GetHeader(HttpResponseMessage response, string name) =>
		response.Headers.TryGetValues(name, out var values) ? values.FirstOrDefault() : null;
	#endregion
}
