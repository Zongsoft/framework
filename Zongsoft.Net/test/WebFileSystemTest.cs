using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.IO;

namespace Zongsoft.Net.Tests;

public class WebFileSystemTest
{
	[Theory]
	[InlineData((FileMode)0, FileAccess.Read, "mode")]
	[InlineData(FileMode.Open, (FileAccess)0, "access")]
	public async Task InvalidOpenArgumentsFailBeforeCreatingClient(FileMode mode, FileAccess access, string parameter)
	{
		using var handler = new UploadHandler(200);
		var factory = new TestHttpClientFactory(handler);
		var provider = CreateFileProvider(factory);

		var synchronous = Assert.Throws<ArgumentOutOfRangeException>(() => provider.Open("example.test/report.txt", mode, access, FileShare.None));
		var asynchronous = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => provider.OpenAsync("example.test/report.txt", mode, access, FileShare.None, null).AsTask());

		Assert.Equal(parameter, synchronous.ParamName);
		Assert.Equal(parameter, asynchronous.ParamName);
		Assert.Equal(0, factory.Calls);
	}

	[Theory]
	[InlineData(FileMode.Append, FileAccess.Write)]
	[InlineData(FileMode.CreateNew, FileAccess.Write)]
	[InlineData(FileMode.Open, FileAccess.ReadWrite)]
	[InlineData(FileMode.Open, FileAccess.Write)]
	[InlineData(FileMode.OpenOrCreate, FileAccess.Read)]
	[InlineData(FileMode.OpenOrCreate, FileAccess.Write)]
	[InlineData(FileMode.Create, FileAccess.Read)]
	[InlineData(FileMode.Create, FileAccess.ReadWrite)]
	[InlineData(FileMode.Truncate, FileAccess.Write)]
	public async Task UnsupportedOpenModesFailBeforeCreatingClient(FileMode mode, FileAccess access)
	{
		using var handler = new UploadHandler(200);
		var factory = new TestHttpClientFactory(handler);
		var provider = CreateFileProvider(factory);

		foreach(var asynchronous in new[] { false, true })
		{
			Stream stream = null;

			try
			{
				await Assert.ThrowsAsync<NotSupportedException>(async () => stream = asynchronous ?
					await provider.OpenAsync("example.test/report.txt", mode, access, FileShare.None, null) :
					provider.Open("example.test/report.txt", mode, access, FileShare.None));

				Assert.Equal(0, factory.Calls);
				Assert.Equal(0, handler.SynchronousCalls + handler.AsynchronousCalls);
			}
			finally
			{
				if(stream != null)
					await stream.DisposeAsync();
			}
		}
	}

	[Fact]
	public void SchemeIsWebFileSystemScheme()
	{
		Assert.Equal("zfs.web", new WebFileSystem().Scheme);
	}

	[Theory]
	[InlineData("zfs.web:/example.test/files/document.txt", "http://example.test/files/document.txt")]
	[InlineData("/example.test/files/document.txt", "http://example.test/files/document.txt")]
	[InlineData("example.test/files/document.txt", "http://example.test/files/document.txt")]
	[InlineData("", "")]
	public void GetUrlTranslatesVirtualPath(string virtualPath, string expected)
	{
		Assert.Equal(expected, new WebFileSystem().GetUrl(virtualPath));
	}

	[Fact]
	public void FileHeadMetadataReturnsHeaderValues()
	{
		var created = new DateTime(2026, 8, 29, 10, 11, 12, DateTimeKind.Utc);
		var modified = new DateTime(2026, 8, 30, 11, 12, 13, DateTimeKind.Utc);
		var response = new HttpResponseMessage(HttpStatusCode.OK);

		response.Headers.TryAddWithoutValidation("X-File-Name", "report.zongsoft-test");
		response.Headers.TryAddWithoutValidation("X-File-Size", "1234");
		response.Headers.TryAddWithoutValidation("X-File-Type", "application/x-zongsoft-test");
		response.Headers.TryAddWithoutValidation("X-File-Creation", created.ToString("O"));
		response.Headers.TryAddWithoutValidation("X-File-Modification", modified.ToString("O"));

		var provider = CreateFileProvider(new TestHttpClientFactory(new StubHandler(response)));
		var info = provider.GetInfo("example.test/reports/report.txt");

		Assert.Equal("report.zongsoft-test", info.Name);
		Assert.Equal(1234, info.Size);
		Assert.Equal("application/x-zongsoft-test", info.Type);
		Assert.Equal(created, info.CreatedTime.ToUniversalTime());
		Assert.Equal(modified, info.ModifiedTime.ToUniversalTime());
	}

	[Theory]
	[InlineData(false, 200)]
	[InlineData(true, 200)]
	[InlineData(false, 500)]
	[InlineData(true, 500)]
	[InlineData(false, 0)]
	[InlineData(true, 0)]
	public async Task UploadDisposesResourcesOnceAndReportsFailure(bool asynchronous, int status)
	{
		using var handler = new UploadHandler(status);
		var provider = CreateFileProvider(new TestHttpClientFactory(handler));
		var stream = await provider.OpenAsync("example.test/reports/nested/report.txt", FileMode.Create, FileAccess.Write, FileShare.None, null);
		stream.Write(new byte[] { 1, 2, 3 });
		var failure = asynchronous ? await Record.ExceptionAsync(() => stream.DisposeAsync().AsTask()) : Record.Exception(stream.Dispose);

		try
		{
			if(status == 200)
				Assert.Null(failure);
			else
				Assert.IsType<HttpRequestException>(failure);

			Assert.Equal(asynchronous ? 0 : 1, handler.SynchronousCalls);
			Assert.Equal(asynchronous ? 1 : 0, handler.AsynchronousCalls);
			Assert.Equal(new Uri("http://example.test/Files/reports/nested"), handler.Address);
			Assert.Equal(new byte[] { 1, 2, 3 }, handler.Body);
			Assert.False(handler.Upload.CanRead);
			Assert.False(stream.CanWrite);
			Assert.True(status == 0 || handler.Content.IsDisposed);
			stream.Dispose();
			await stream.DisposeAsync();
			Assert.Equal(1, handler.SynchronousCalls + handler.AsynchronousCalls);
		}
		finally
		{
			await Record.ExceptionAsync(() => stream.DisposeAsync().AsTask());
		}
	}

	[Theory]
	[InlineData(false, 200)]
	[InlineData(true, 200)]
	[InlineData(false, 404)]
	[InlineData(true, 404)]
	public async Task DownloadOwnsResponseUntilStreamIsDisposed(bool asynchronous, int status)
	{
		var content = new TrackingContent([1, 2, 3]);
		var provider = CreateFileProvider(new TestHttpClientFactory(new StubHandler(new((HttpStatusCode)status) { Content = content })));
		Stream stream = null;
		var failure = await Record.ExceptionAsync(async () => stream = asynchronous ?
			await provider.OpenAsync("example.test/report.txt", FileMode.Open, FileAccess.Read, FileShare.Read, null) :
			provider.Open("example.test/report.txt", FileMode.Open, FileAccess.Read, FileShare.Read));

		if(status != 200)
		{
			Assert.IsType<HttpRequestException>(failure);
			Assert.True(content.IsDisposed);
			return;
		}

		Assert.Null(failure);
		Assert.False(content.IsDisposed);

		using(var buffer = new MemoryStream())
		{
			await stream.CopyToAsync(buffer);
			Assert.Equal(new byte[] { 1, 2, 3 }, buffer.ToArray());
		}

		if(asynchronous)
			await stream.DisposeAsync();
		else
			stream.Dispose();

		Assert.True(content.IsDisposed);
		Assert.False(stream.CanRead);
		stream.Dispose();
		await stream.DisposeAsync();
	}

	[Theory]
	[InlineData(false, "children")]
	[InlineData(true, "children")]
	[InlineData(false, "directories")]
	[InlineData(true, "directories")]
	[InlineData(false, "files")]
	[InlineData(true, "files")]
	public async Task DirectoryEnumerationReadsWebJsonAndDisposesResponse(bool asynchronous, string mode)
	{
		var json = mode == "children" ? "{\"directories\":[{\"name\":\"folder/\"}],\"files\":[{\"name\":\"file.txt\",\"size\":3}]}" :
			mode == "directories" ? "[{\"name\":\"folder/\"}]" : "[{\"name\":\"file.txt\",\"size\":3}]";
		var content = new TrackingContent(Encoding.UTF8.GetBytes(json));
		Assert.Null(content.Headers.ContentLength);
		using var handler = new StubHandler(new(HttpStatusCode.OK) { Content = content });
		var type = typeof(WebFileSystem).GetNestedType("DirectoryProvider", BindingFlags.NonPublic);
		var provider = (IDirectory)Activator.CreateInstance(type, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, [new TestHttpClientFactory(handler)], null);
		var items = new System.Collections.Generic.List<PathInfo>();
		const string PATH = "example.test/root";
		const string PATTERN = "a&b*.txt";

		if(asynchronous)
		{
			var entries = mode == "children" ? provider.GetChildrenAsync(PATH, PATTERN, false) :
				mode == "directories" ? provider.GetDirectoriesAsync(PATH, PATTERN, false) : provider.GetFilesAsync(PATH, PATTERN, false);

			await foreach(var entry in entries)
				items.Add(entry);
		}
		else
		{
			items.AddRange(mode == "children" ? provider.GetChildren(PATH, PATTERN) :
				mode == "directories" ? provider.GetDirectories(PATH, PATTERN) : provider.GetFiles(PATH, PATTERN));
		}

		Assert.Equal(mode == "children" ? 2 : 1, items.Count);
		Assert.All(items, item => Assert.False(string.IsNullOrEmpty(item.Name)));
		Assert.Contains("pattern=a%26b%2A.txt", handler.Address.Query);
		Assert.True(content.IsDisposed);
	}

	private static IFile CreateFileProvider(IHttpClientFactory factory)
	{
		var type = typeof(WebFileSystem).GetNestedType("FileProvider", BindingFlags.NonPublic);
		return (IFile)Activator.CreateInstance(type, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, [factory], null);
	}

	private sealed class TestHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
	{
		internal int Calls;
		public HttpClient CreateClient(string name)
		{
			this.Calls++;
			return new(handler, false);
		}
	}

	private sealed class StubHandler(HttpResponseMessage response) : HttpMessageHandler
	{
		internal Uri Address;
		protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			this.Address = request.RequestUri;
			return response;
		}
		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			this.Address = request.RequestUri;
			return Task.FromResult(response);
		}
	}

	private sealed class UploadHandler(int status) : HttpMessageHandler
	{
		internal int SynchronousCalls;
		internal int AsynchronousCalls;
		internal Uri Address;
		internal byte[] Body;
		internal Stream Upload;
		internal TrackingContent Content { get; } = new();

		protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			this.SynchronousCalls++;
			return this.Respond(request);
		}

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			this.AsynchronousCalls++;
			return Task.FromResult(this.Respond(request));
		}

		private HttpResponseMessage Respond(HttpRequestMessage request)
		{
			this.Address = request.RequestUri;
			this.Upload = Assert.IsType<MultipartFormDataContent>(request.Content).Single().ReadAsStream();
			using var buffer = new MemoryStream();
			this.Upload.CopyTo(buffer);
			this.Body = buffer.ToArray();

			if(status == 0)
				throw new HttpRequestException();

			return new((HttpStatusCode)status) { Content = this.Content };
		}
	}

	private sealed class TrackingContent(byte[] data = null) : ByteArrayContent(data ?? [])
	{
		internal bool IsDisposed { get; private set; }
		protected override bool TryComputeLength(out long length)
		{
			length = 0;
			return false;
		}
		protected override void Dispose(bool disposing)
		{
			this.IsDisposed = true;
			base.Dispose(disposing);
		}
	}
}
