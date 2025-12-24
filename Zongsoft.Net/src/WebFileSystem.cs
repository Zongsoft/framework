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
	public string Scheme => "zfs.web";
	public IFile File => new FileProvider(ApplicationContext.Current.Services.ResolveRequired<IHttpClientFactory>());
	public IDirectory Directory => new DirectoryProvider(ApplicationContext.Current.Services.ResolveRequired<IHttpClientFactory>());

	public string GetUrl(string virtualPath) => Zongsoft.IO.Path.TryParse(virtualPath, out var path) ? this.GetUrl(path) : virtualPath;
	public string GetUrl(Zongsoft.IO.Path path) => $"http://{path.FullPath.TrimStart('/')}";

	private sealed class DirectoryProvider(IHttpClientFactory factory) : Zongsoft.IO.IDirectory
	{
		private readonly IHttpClientFactory _factory = factory;

		public bool Create(string fullPath, IEnumerable<KeyValuePair<string, string>> properties = null)
		{
			var client = GetClient(fullPath, out var path);
			var request = new HttpRequestMessage(HttpMethod.Post, path);
			var response = client.Send(request);
			return response.IsSuccessStatusCode;
		}

		public async ValueTask<bool> CreateAsync(string fullPath, IEnumerable<KeyValuePair<string, string>> properties, CancellationToken cancellation = default)
		{
			var client = GetClient(fullPath, out var path);
			var response = await client.PostAsync(path, null, cancellation);
			return response.IsSuccessStatusCode;
		}

		public bool Delete(string fullPath)
		{
			var client = GetClient(fullPath, out var path);
			var request = new HttpRequestMessage(HttpMethod.Delete, path);
			var response = client.Send(request);
			return response.IsSuccessStatusCode;
		}

		public async ValueTask<bool> DeleteAsync(string fullPath, CancellationToken cancellation = default)
		{
			var client = GetClient(fullPath, out var path);
			var response = await client.DeleteAsync(path, cancellation);
			return response.IsSuccessStatusCode;
		}

		public bool Exists(string fullPath)
		{
			var client = GetClient(fullPath, out var path);
			var request = new HttpRequestMessage(HttpMethod.Head, path);
			var response = client.Send(request);
			return response.IsSuccessStatusCode;
		}

		public async ValueTask<bool> ExistsAsync(string fullPath, CancellationToken cancellation = default)
		{
			var client = GetClient(fullPath, out var path);
			var request = new HttpRequestMessage(HttpMethod.Head, path);
			var response = await client.SendAsync(request, cancellation);
			return response.IsSuccessStatusCode;
		}

		public IEnumerable<PathInfo> GetChildren(string fullPath, string pattern, bool recursive = false)
		{
			var client = GetClient(fullPath, out var path);
			var request = new HttpRequestMessage(HttpMethod.Get, $"{path}?pattern={pattern}&recursive={recursive}");
			var response = client.Send(request);

			if(response.Content.Headers.ContentLength == null || response.Content.Headers.ContentLength.Value == 0)
				yield break;

			var stream = response.Content.ReadAsStream();
			var result = System.Text.Json.JsonSerializer.Deserialize<Result>(stream);

			if(result.Directories != null)
			{
				foreach(var directory in result.Directories)
					yield return new IO.DirectoryInfo(directory.Name, directory.Creation, directory.Modification);
			}

			if(result.Files != null)
			{
				foreach(var file in result.Files)
					yield return new IO.FileInfo(file.Name, file.Size, file.Creation, file.Modification);
			}
		}

		public async IAsyncEnumerable<PathInfo> GetChildrenAsync(string fullPath, string pattern, bool recursive, [System.Runtime.CompilerServices.EnumeratorCancellation]CancellationToken cancellation = default)
		{
			var client = GetClient(fullPath, out var path);
			var response = await client.GetAsync($"{path}?pattern={pattern}&recursive={recursive}", cancellation);

			if(response.Content.Headers.ContentLength == null || response.Content.Headers.ContentLength.Value == 0)
				yield break;

			var stream = await response.Content.ReadAsStreamAsync(cancellation);
			var result = System.Text.Json.JsonSerializer.Deserialize<Result>(stream);

			if(result.Directories != null)
			{
				foreach(var directory in result.Directories)
					yield return new IO.DirectoryInfo(directory.Name, directory.Creation, directory.Modification);
			}

			if(result.Files != null)
			{
				foreach(var file in result.Files)
					yield return new IO.FileInfo(file.Name, file.Size, file.Creation, file.Modification);
			}
		}

		public IEnumerable<IO.DirectoryInfo> GetDirectories(string fullPath, string pattern, bool recursive = false)
		{
			var client = GetClient(fullPath, out var path);
			var request = new HttpRequestMessage(HttpMethod.Get, $"{path}?mode=directory&pattern={pattern}&recursive={recursive}");
			var response = client.Send(request);
			return response.IsSuccessStatusCode ? GetDirectoryInfos(response.Content) : [];
		}

		public async IAsyncEnumerable<IO.DirectoryInfo> GetDirectoriesAsync(string fullPath, string pattern, bool recursive, [System.Runtime.CompilerServices.EnumeratorCancellation]CancellationToken cancellation = default)
		{
			var client = GetClient(fullPath, out var path);
			var response = await client.GetAsync($"{path}?mode=directory&pattern={pattern}&recursive={recursive}", cancellation);

			if(response.IsSuccessStatusCode)
			{
				var infos = GetDirectoryInfosAsync(response.Content, cancellation);
				await foreach(var info in infos)
					yield return info;
			}
		}

		public IEnumerable<IO.FileInfo> GetFiles(string fullPath, string pattern, bool recursive = false)
		{
			var client = GetClient(fullPath, out var path);
			var request = new HttpRequestMessage(HttpMethod.Get, $"{path}?mode=file&pattern={pattern}&recursive={recursive}");
			var response = client.Send(request);
			return response.IsSuccessStatusCode ? GetFileInfos(response.Content) : [];
		}

		public async IAsyncEnumerable<IO.FileInfo> GetFilesAsync(string fullPath, string pattern, bool recursive, [System.Runtime.CompilerServices.EnumeratorCancellation]CancellationToken cancellation = default)
		{
			var client = GetClient(fullPath, out var path);
			var response = await client.GetAsync($"{path}?mode=file&pattern={pattern}&recursive={recursive}", cancellation);

			if(response.IsSuccessStatusCode)
			{
				var infos = GetFileInfosAsync(response.Content, cancellation);
				await foreach(var info in infos)
					yield return info;
			}
		}

		public IO.DirectoryInfo GetInfo(string fullPath)
		{
			var client = GetClient(fullPath, out var path);
			var request = new HttpRequestMessage(HttpMethod.Head, path);
			var response = client.Send(request);
			return GetDirectoryInfo(response);
		}

		public async ValueTask<IO.DirectoryInfo> GetInfoAsync(string fullPath, CancellationToken cancellation = default)
		{
			var client = GetClient(fullPath, out var path);
			var request = new HttpRequestMessage(HttpMethod.Head, path);
			var response = await client.SendAsync(request, cancellation);
			return GetDirectoryInfo(response);
		}

		public void Move(string source, string destination) => throw new NotSupportedException();
		public ValueTask MoveAsync(string source, string destination, CancellationToken cancellation = default) => throw new NotSupportedException();
		public bool SetInfo(string path, IEnumerable<KeyValuePair<string, string>> properties) => throw new NotSupportedException();
		public ValueTask<bool> SetInfoAsync(string path, IEnumerable<KeyValuePair<string, string>> properties, CancellationToken cancellation = default) => throw new NotSupportedException();

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
				client.BaseAddress = new Uri($"http://{fullPath.Trim('/')}/Directories/");
			}
			else
			{
				path = fullPath[(index + 1)..].ToString();
				client.BaseAddress = new Uri($"http://{fullPath[..index].Trim('/')}/Directories/");
			}

			return client;
		}

		private static Zongsoft.IO.DirectoryInfo GetDirectoryInfo(HttpResponseMessage response)
		{
			if(response == null || !response.IsSuccessStatusCode)
				return null;

			var name = response.Headers.TryGetValues("X-Directory-Name", out var value);
			var creation = response.Headers.TryGetValues("X-Directory-Creation", out value) && DateTime.TryParse(value.ToString(), out var datetime) ? datetime : DateTime.MinValue;
			var modification = response.Headers.TryGetValues("X-Directory-Modification", out value) && DateTime.TryParse(value.ToString(), out datetime) ? datetime : DateTime.MinValue;

			return new IO.DirectoryInfo(name.ToString(), creation, modification);
		}

		private static IEnumerable<Zongsoft.IO.DirectoryInfo> GetDirectoryInfos(HttpContent content)
		{
			if(content == null || content.Headers.ContentLength == null || content.Headers.ContentLength.Value == 0)
				return null;

			var stream = content.ReadAsStream();
			var infos = System.Text.Json.JsonSerializer.Deserialize<DirectoryInfo[]>(stream);
			return infos.Select(info => new IO.DirectoryInfo(info.Name, info.Creation, info.Modification));
		}

		private static async IAsyncEnumerable<Zongsoft.IO.DirectoryInfo> GetDirectoryInfosAsync(HttpContent content, [System.Runtime.CompilerServices.EnumeratorCancellation]CancellationToken cancellation)
		{
			if(content == null || content.Headers.ContentLength == null || content.Headers.ContentLength.Value == 0)
				yield break;

			var infos = await content.ReadFromJsonAsync<DirectoryInfo[]>(cancellation);
			foreach(var info in infos)
				yield return new IO.DirectoryInfo(info.Name, info.Creation, info.Modification);
		}

		private static IEnumerable<Zongsoft.IO.FileInfo> GetFileInfos(HttpContent content)
		{
			if(content == null || content.Headers.ContentLength == null || content.Headers.ContentLength.Value == 0)
				return null;

			var stream = content.ReadAsStream();
			var infos = System.Text.Json.JsonSerializer.Deserialize<FileInfo[]>(stream);
			return infos.Select(info => new IO.FileInfo(info.Name, info.Size, info.Creation, info.Modification));
		}

		private static async IAsyncEnumerable<Zongsoft.IO.FileInfo> GetFileInfosAsync(HttpContent content, [System.Runtime.CompilerServices.EnumeratorCancellation]CancellationToken cancellation)
		{
			if(content == null || content.Headers.ContentLength == null || content.Headers.ContentLength.Value == 0)
				yield break;

			var infos = await content.ReadFromJsonAsync<FileInfo[]>(cancellation);
			foreach(var info in infos)
				yield return new IO.FileInfo(info.Name, info.Size, info.Creation, info.Modification);
		}

		private sealed class Result
		{
			public DirectoryInfo[] Directories { get; set; }
			public FileInfo[] Files { get; set; }
		}

		private sealed class FileInfo
		{
			public string Name { get; set; }
			public string Type { get; set; }
			public long Size { get; set; }
			public DateTime Creation { get; set; }
			public DateTime Modification { get; set; }
		}

		private sealed class DirectoryInfo
		{
			public string Name { get; set; }
			public DateTime Creation { get; set; }
			public DateTime Modification { get; set; }
		}
	}

	private sealed class FileProvider(IHttpClientFactory factory) : Zongsoft.IO.IFile
	{
		private readonly IHttpClientFactory _factory = factory;

		public void Copy(string source, string destination, bool overwrite = true) => throw new NotSupportedException();
		public ValueTask CopyAsync(string source, string destination, bool overwrite, CancellationToken cancellation = default) => throw new NotSupportedException();
		public void Move(string source, string destination) => throw new NotSupportedException();
		public ValueTask MoveAsync(string source, string destination, CancellationToken cancellation = default) => throw new NotSupportedException();
		public bool SetInfo(string path, IEnumerable<KeyValuePair<string, string>> properties) => throw new NotSupportedException();
		public ValueTask<bool> SetInfoAsync(string path, IEnumerable<KeyValuePair<string, string>> properties, CancellationToken cancellation = default) => throw new NotSupportedException();

		public bool Delete(string fullPath)
		{
			var client = GetClient(fullPath, out var path);
			var request = new HttpRequestMessage(HttpMethod.Delete, path);
			var response = client.Send(request);
			return response.IsSuccessStatusCode;
		}

		public async ValueTask<bool> DeleteAsync(string fullPath, CancellationToken cancellation = default)
		{
			var client = GetClient(fullPath, out var path);
			var response = await client.DeleteAsync(path, cancellation);
			return response.IsSuccessStatusCode;
		}

		public bool Exists(string fullPath)
		{
			var client = GetClient(fullPath, out var path);
			var request = new HttpRequestMessage(HttpMethod.Head, path);
			var response = client.Send(request);
			return response.IsSuccessStatusCode;
		}

		public async ValueTask<bool> ExistsAsync(string fullPath, CancellationToken cancellation = default)
		{
			var client = GetClient(fullPath, out var path);
			var request = new HttpRequestMessage(HttpMethod.Head, path);
			var response = await client.SendAsync(request, cancellation);
			return response.IsSuccessStatusCode;
		}

		public Zongsoft.IO.FileInfo GetInfo(string fullPath)
		{
			var client = GetClient(fullPath, out var path);
			var request = new HttpRequestMessage(HttpMethod.Head, path);
			var response = client.Send(request);
			return GetFileInfo(response);
		}

		public async ValueTask<Zongsoft.IO.FileInfo> GetInfoAsync(string fullPath, CancellationToken cancellation = default)
		{
			var client = GetClient(fullPath, out var path);
			var request = new HttpRequestMessage(HttpMethod.Head, path);
			var response = await client.SendAsync(request, cancellation);
			return GetFileInfo(response);
		}

		public Stream Open(string path, FileMode mode, FileAccess access, FileShare share, IEnumerable<KeyValuePair<string, string>> properties = null)
		{
			var client = GetClient(path, out var p);
			bool writable = (mode != FileMode.Open) || (access & FileAccess.Write) == FileAccess.Write;

			if(writable)
				return new WebUploadStream(client, p);

			var response = client.Send(new HttpRequestMessage(HttpMethod.Get, p));
			response.EnsureSuccessStatusCode();
			return response.Content.ReadAsStream();
		}

		public async ValueTask<Stream> OpenAsync(string path, FileMode mode, FileAccess access, FileShare share, IEnumerable<KeyValuePair<string, string>> properties, CancellationToken cancellation = default)
		{
			var client = GetClient(path, out var p);
			bool writable = (mode != FileMode.Open) || (access & FileAccess.Write) == FileAccess.Write;

			if(writable)
				return new WebUploadStream(client, p);

			var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, p), cancellation);
			response.EnsureSuccessStatusCode();
			return await response.Content.ReadAsStreamAsync(cancellation);
		}

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

		private static Zongsoft.IO.FileInfo GetFileInfo(HttpResponseMessage response)
		{
			if(response == null || !response.IsSuccessStatusCode)
				return null;

			var name = response.Headers.TryGetValues("X-File-Name", out var value);
			var size = response.Headers.TryGetValues("X-File-Size", out value) && long.TryParse(value.ToString(), out var number) ? number : 0;
			var type = response.Headers.TryGetValues("X-File-Type", out value) ? value.ToString() : null;
			var creation = response.Headers.TryGetValues("X-File-Creation", out value) && DateTime.TryParse(value.ToString(), out var datetime) ? datetime : DateTime.MinValue;
			var modification = response.Headers.TryGetValues("X-File-Modification", out value) && DateTime.TryParse(value.ToString(), out datetime) ? datetime : DateTime.MinValue;

			return new IO.FileInfo(name.ToString(), size, creation, modification);
		}

		private struct FileInfo
		{
			public string Name { get; set; }
			public long Size { get; set; }
			public string Type { get; set; }
			public DateTimeOffset Creation { get; set; }
			public DateTimeOffset Modification { get; set; }
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
