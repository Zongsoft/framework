using System;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.IO;

namespace Zongsoft.Net.Tests;

public class WebFileSystemTests
{
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

	private static IFile CreateFileProvider(IHttpClientFactory factory)
	{
		var type = typeof(WebFileSystem).GetNestedType("FileProvider", BindingFlags.NonPublic);
		return (IFile)Activator.CreateInstance(type, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, [factory], null);
	}

	private sealed class TestHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
	{
		public HttpClient CreateClient(string name) => new(handler, false);
	}

	private sealed class StubHandler(HttpResponseMessage response) : HttpMessageHandler
	{
		protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken) => response;
		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(response);
	}
}
