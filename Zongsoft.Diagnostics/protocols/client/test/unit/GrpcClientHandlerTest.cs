using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace Zongsoft.Diagnostics.Protocols.Client.Tests;

public class GrpcClientHandlerTest
{
	[Theory]
	[InlineData("")]
	[InlineData("telemetry")]
	[InlineData(" /telemetry")]
	[InlineData("/telemetry ")]
	[InlineData("//")]
	[InlineData("/telemetry//child")]
	[InlineData("/telemetry//")]
	[InlineData("/./telemetry")]
	[InlineData("/telemetry/..")]
	[InlineData("/telemetry/../")]
	[InlineData("/telemetry/.")]
	[InlineData("/telemetry%2Fchild")]
	[InlineData("/telemetry?query")]
	[InlineData("/telemetry#fragment")]
	[InlineData("/telemetry+extra")]
	[InlineData("/指标")]
	[InlineData("/telemetry\\child")]
	[InlineData("/telemetry\nchild")]
	public void ConstructorRejectsInvalidPaths(string path)
	{
		using var terminal = new CaptureHandler((_, _) => new HttpResponseMessage());
		var exception = Assert.Throws<ArgumentException>(() => new GrpcClientHandler(path, terminal));

		Assert.Equal("path", exception.ParamName);
		Assert.Equal(0, terminal.Calls);
	}

	[Fact]
	public void ConstructorRejectsNullDependencies()
	{
		using var terminal = new CaptureHandler((_, _) => new HttpResponseMessage());
		var path = Assert.Throws<ArgumentNullException>(() => new GrpcClientHandler(null, terminal));
		var inner = Assert.Throws<ArgumentNullException>(() => new GrpcClientHandler("/telemetry", null));

		Assert.Equal("path", path.ParamName);
		Assert.Equal("innerHandler", inner.ParamName);
	}

	[Theory]
	[InlineData("/", "/service.Collector/Export", "/service.Collector/Export")]
	[InlineData("/telemetry", "/service.Collector/Export", "/telemetry/service.Collector/Export")]
	[InlineData("/telemetry/", "/service.Collector/Export", "/telemetry/service.Collector/Export")]
	[InlineData("/A-z_0.~/child", "/service/Export", "/A-z_0.~/child/service/Export")]
	[InlineData("/telemetry", "/telemetry/service/Export", "/telemetry/telemetry/service/Export")]
	[InlineData("/telemetry", "/", "/telemetry/")]
	[InlineData("/telemetry", "/service/%2Fopaque?token=a%2Fb%26c&plus=a+b", "/telemetry/service/%2Fopaque?token=a%2Fb%26c&plus=a+b")]
	public async Task PrefixIsPrependedExactlyOncePerHandlerAsync(string path, string rpcPath, string expected)
	{
		foreach(var synchronous in new[] { false, true })
		{
			using var response = new HttpResponseMessage(HttpStatusCode.OK);
			using var terminal = new CaptureHandler((_, _) => response);
			using var handler = new GrpcClientHandler(path, terminal);
			using var invoker = new HttpMessageInvoker(handler, false);
			using var request = new HttpRequestMessage(HttpMethod.Post, $"http://localhost:4317{rpcPath}");

			var result = synchronous ? invoker.Send(request, CancellationToken.None) : await invoker.SendAsync(request, CancellationToken.None);

			Assert.Equal(path, handler.Path);
			Assert.Equal($"http://localhost:4317{expected}", request.RequestUri.AbsoluteUri);
			Assert.Same(response, result);
			Assert.Equal(1, terminal.Calls);
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task RequestMetadataAndStreamRemainUntouchedAsync(bool synchronous)
	{
		using var cancellation = new CancellationTokenSource();
		using var stream = new UnreadStream();
		using var request = new HttpRequestMessage(HttpMethod.Post, "https://[::1]:8443/service.Collector/Export?encoded=a%2Fb%26c&space=x%20y")
		{
			Content = new StreamContent(stream),
			Version = HttpVersion.Version20,
			VersionPolicy = HttpVersionPolicy.RequestVersionExact,
		};
		request.Headers.TryAddWithoutValidation("x-test", "preserved");
		request.Content.Headers.TryAddWithoutValidation("content-type", "application/grpc");
		var option = new HttpRequestOptionsKey<object>("fixture-state");
		var state = new object();
		request.Options.Set(option, state);
		var headers = request.Headers;
		var content = request.Content;
		using var response = new HttpResponseMessage(HttpStatusCode.Accepted);
		using var terminal = new CaptureHandler((message, token) =>
		{
			Assert.Same(request, message);
			Assert.Equal(cancellation.Token, token);
			return response;
		});
		using var handler = new GrpcClientHandler("/telemetry/", terminal);
		using var invoker = new HttpMessageInvoker(handler, false);

		var result = synchronous ? invoker.Send(request, cancellation.Token) : await invoker.SendAsync(request, cancellation.Token);

		Assert.Same(response, result);
		Assert.Equal("https://[::1]:8443/telemetry/service.Collector/Export?encoded=a%2Fb%26c&space=x%20y", request.RequestUri.AbsoluteUri);
		Assert.Same(headers, request.Headers);
		Assert.Equal("preserved", Assert.Single(request.Headers.GetValues("x-test")));
		Assert.Same(content, request.Content);
		Assert.Equal("application/grpc", request.Content.Headers.ContentType.MediaType);
		Assert.Equal(HttpVersion.Version20, request.Version);
		Assert.Equal(HttpVersionPolicy.RequestVersionExact, request.VersionPolicy);
		Assert.True(request.Options.TryGetValue(option, out var retained));
		Assert.Same(state, retained);
		Assert.Equal(0, stream.Reads);
		Assert.True(stream.CanRead);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task CancellationPassesToInnerHandlerAsync(bool synchronous)
	{
		using var cancellation = new CancellationTokenSource();
		using var terminal = new CaptureHandler((_, token) =>
		{
			cancellation.Cancel();
			token.ThrowIfCancellationRequested();
			return new HttpResponseMessage();
		});
		using var handler = new GrpcClientHandler("/telemetry", terminal);
		using var invoker = new HttpMessageInvoker(handler, false);
		using var request = new HttpRequestMessage(HttpMethod.Post, "https://localhost/service/Call");

		var error = synchronous ?
			Assert.ThrowsAny<OperationCanceledException>(() => invoker.Send(request, cancellation.Token)) :
			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => invoker.SendAsync(request, cancellation.Token));

		Assert.Equal(cancellation.Token, error.CancellationToken);
		Assert.Equal(1, terminal.Calls);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("/relative")]
	[InlineData("ftp://localhost/service/Call")]
	[InlineData("https://localhost/service/Call#fragment")]
	public async Task InvalidRequestUrisFailBeforeForwardingAsync(string uri)
	{
		foreach(var synchronous in new[] { false, true })
		{
			using var terminal = new CaptureHandler((_, _) => new HttpResponseMessage());
			using var handler = new GrpcClientHandler("/telemetry", terminal);
			using var invoker = new HttpMessageInvoker(handler, false);
			using var request = new HttpRequestMessage(HttpMethod.Post, uri == null ? null : new Uri(uri, UriKind.RelativeOrAbsolute));

			var error = synchronous ?
				Assert.Throws<ArgumentException>(() => invoker.Send(request, CancellationToken.None)) :
				await Assert.ThrowsAsync<ArgumentException>(() => invoker.SendAsync(request, CancellationToken.None));

			Assert.Equal("request", error.ParamName);
			Assert.Equal(0, terminal.Calls);
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void DuplicateInstallationIsRejected(bool nested)
	{
		using var terminal = new CaptureHandler((_, _) => new HttpResponseMessage());
		using var inner = new GrpcClientHandler("/inner", terminal);
		using var bridge = new PassThroughHandler(inner);

		Assert.Throws<InvalidOperationException>(() => new GrpcClientHandler("/outer", nested ? bridge : inner));
		Assert.Equal(0, terminal.Calls);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task RuntimeRewiredChainRejectsSecondPathHandlerAsync(bool synchronous)
	{
		using var terminal = new CaptureHandler((_, _) => new HttpResponseMessage());
		using var bridge = new PassThroughHandler(terminal);
		using var outer = new GrpcClientHandler("/outer", bridge);
		bridge.InnerHandler = new GrpcClientHandler("/inner", terminal);
		using var invoker = new HttpMessageInvoker(outer, false);
		using var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/service/Call");

		if(synchronous)
			Assert.Throws<InvalidOperationException>(() => invoker.Send(request, CancellationToken.None));
		else
			await Assert.ThrowsAsync<InvalidOperationException>(() => invoker.SendAsync(request, CancellationToken.None));

		Assert.Equal("/outer/service/Call", request.RequestUri.AbsolutePath);
		Assert.Equal(0, terminal.Calls);
	}

	private sealed class PassThroughHandler(HttpMessageHandler inner) : DelegatingHandler(inner)
	{
	}

	private sealed class CaptureHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> callback) : HttpMessageHandler
	{
		public int Calls { get; private set; }
		protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			this.Calls++;
			return callback(request, cancellationToken);
		}

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
			Task.FromResult(this.Send(request, cancellationToken));
	}

	private sealed class UnreadStream() : MemoryStream([1, 2, 3])
	{
		public int Reads { get; private set; }
		public override int Read(byte[] buffer, int offset, int count)
		{
			this.Reads++;
			throw new InvalidOperationException("The handler must not read the request content.");
		}

		public override int Read(Span<byte> buffer)
		{
			this.Reads++;
			throw new InvalidOperationException("The handler must not read the request content.");
		}

		public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => Task.FromResult(this.Read(buffer, offset, count));
		public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => new(this.Read(buffer.Span));
		public override void CopyTo(Stream destination, int bufferSize)
		{
			this.Reads++;
			throw new InvalidOperationException("The handler must not buffer the request content.");
		}

		public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
		{
			this.CopyTo(destination, bufferSize);
			return Task.CompletedTask;
		}
	}
}
