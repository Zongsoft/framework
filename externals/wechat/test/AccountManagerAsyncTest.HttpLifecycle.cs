using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;

using Xunit;

using Zongsoft.Externals.Wechat.Paying;

namespace Zongsoft.Externals.Wechat.Tests;

public sealed partial class AccountManagerAsyncTest
{
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Http_CancellationReleasesTheResponseBody(bool reading)
	{
		using var material = new HttpMaterial();
		using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		using var stream = new WaitingStream();
		var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var count = 0;
		context.HttpClients.PaymentHandler = async (request, token) =>
		{
			count++;
			if(reading)
				return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(stream) };

			entered.SetResult();
			await Task.Delay(Timeout.Infinite, token);
			throw new InvalidOperationException();
		};

		try
		{
			var pending = material.Client.PostAsync("offline", cancellation.Token).AsTask();
			await (reading ? stream.Entered.Task : entered.Task).WaitAsync(_timeout, TestContext.Current.CancellationToken);
			cancellation.Cancel();
			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
			Assert.Equal(1, count);

			if(reading)
				Assert.True(stream.Disposed);
		}
		finally { context.HttpClients.PaymentHandler = null; }
	}

	[Fact]
	public async Task Http_TimeoutDoesNotRetryThePayment()
	{
		using var material = new HttpMaterial();
		material.Client.Timeout = TimeSpan.FromMilliseconds(500);
		var count = 0;
		context.HttpClients.PaymentHandler = async (request, cancellation) =>
		{
			count++;
			await Task.Delay(Timeout.Infinite, cancellation);
			throw new InvalidOperationException();
		};

		try
		{
			var error = await Assert.ThrowsAsync<TaskCanceledException>(() => material.Client.PostAsync("offline", TestContext.Current.CancellationToken).AsTask());
			Assert.IsType<TimeoutException>(error.InnerException);
			Assert.Equal(1, count);
		}
		finally { context.HttpClients.PaymentHandler = null; }
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Http_RejectsOversizedResponseBodies(bool declared)
	{
		using var material = new HttpMaterial();
		var content = new TrackedContent(new string('x', 4 * 1024 * 1024 + 1));
		if(declared)
			content.Headers.ContentLength = 4 * 1024 * 1024 + 1;

		context.HttpClients.PaymentHandler = (request, cancellation) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = content });

		try
		{
			await Assert.ThrowsAnyAsync<CryptographicException>(() => material.Client.GetAsync<HttpPayload>("offline", TestContext.Current.CancellationToken).AsTask());
			Assert.True(content.Disposed);
			Assert.Equal(declared ? 0 : 1, content.Writes);
		}
		finally { context.HttpClients.PaymentHandler = null; }
	}

	[Fact]
	public async Task Http_UnknownPlatformCertificateRefreshesTrustWithoutReplayingTheRequest()
	{
		using var trusted = new PlatformMaterial(-2, 10);
		using var replacement = new PlatformMaterial(-1, 20);
		var authority = trusted.CreateAuthority();
		var payments = 0;
		var downloads = 0;
		context.HttpClients.PaymentHandler = (request, cancellation) =>
		{
			payments++;
			return Task.FromResult(replacement.Respond([]));
		};
		context.HttpClients.Handler = (request, cancellation) =>
		{
			downloads++;
			return Task.FromResult(trusted.Respond([trusted.Entry(), replacement.Entry()]));
		};

		try
		{
			var result = await Paying.HttpClientFactory.GetHttpClient(authority).PostAsync<object, JsonElement>("offline", new { value = "synthetic" }, TestContext.Current.CancellationToken);
			Assert.Equal(0, result.GetProperty("data").GetArrayLength());
			Assert.Equal(1, payments);
			Assert.Equal(1, downloads);
			Assert.NotNull(await authority.GetCertificateAsync(replacement.Certificate.SerialNumber, TestContext.Current.CancellationToken));
			Assert.Equal(1, downloads);
		}
		finally
		{
			context.HttpClients.PaymentHandler = null;
			context.HttpClients.Handler = null;
		}
	}

	[Fact]
	public async Task Http_JsonHelperUsesRequestHeadersAndOwnsItsContent()
	{
		using var material = new HttpMaterial();
		HttpContent content = null;
		context.HttpClients.PaymentHandler = async (request, cancellation) =>
		{
			content = request.Content;
			Assert.Equal("PUB_KEY_ID_HTTP", Assert.Single(request.Headers.GetValues("Wechatpay-Serial")));
			Assert.False(content.Headers.Contains("Wechatpay-Serial"));
			var body = await content.ReadAsStringAsync(cancellation);
			material.AssertSignature(request, body);
			return material.Respond("{\"value\":\"verified\"}");
		};

		try
		{
			var key = await material.Authority.GetCertificateAsync(cancellation: TestContext.Current.CancellationToken);
			var result = await material.Client.PostAsync<object, HttpPayload>("offline", new { value = "synthetic" }, key, TestContext.Current.CancellationToken);
			Assert.Equal("verified", result.Value);
			Assert.NotNull(content);
			await Assert.ThrowsAsync<ObjectDisposedException>(async () => await content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));
		}
		finally { context.HttpClients.PaymentHandler = null; }
	}

	[Theory]
	[InlineData("http://api.mch.weixin.qq.com/v3/offline")]
	[InlineData("https://example.invalid/v3/offline")]
	[InlineData("https://api.mch.weixin.qq.com/pay/offline")]
	[InlineData("https://api.mch.weixin.qq.com/v3/offline#fragment")]
	[InlineData("https://api.mch.weixin.qq.com:8443/v3/offline")]
	public async Task Http_RejectsAnInvalidDestinationBeforeSending(string address)
	{
		using var material = new HttpMaterial();
		var error = await Assert.ThrowsAsync<ArgumentException>(() => material.Client.GetAsync(address, TestContext.Current.CancellationToken));
		Assert.Equal("request", error.ParamName);
	}

	[Fact]
	public async Task Http_PreCancelledRequestsNeverReachTheTransport()
	{
		using var material = new HttpMaterial();
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => material.Client.PostAsync("offline", cancellation.Token).AsTask());
	}

	[Fact]
	public void Http_CachesClientsByCompleteAuthorityIdentity()
	{
		using var first = new HttpMaterial();
		using var second = new HttpMaterial();
		var replacement = new Authority("offline-http", "offline-merchant", signing: first.Authority.Signing, verification: second.Authority.Verification);
		Assert.Same(first.Client, Paying.HttpClientFactory.GetHttpClient(first.Authority));
		Assert.NotSame(first.Client, Paying.HttpClientFactory.GetHttpClient(replacement));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Http_RejectsTransportConfigurationThatEnablesRedirects(bool sockets)
	{
		using var material = new HttpMaterial();
		context.HttpClients.PaymentTransport = () => sockets ? new SocketsHttpHandler() : new HttpClientHandler();

		try
		{
			await Assert.ThrowsAsync<InvalidOperationException>(() => material.Client.PostAsync("offline", TestContext.Current.CancellationToken).AsTask());
		}
		finally { context.HttpClients.PaymentTransport = null; }
	}

	private sealed class WaitingStream : MemoryStream
	{
		public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public bool Disposed { get; private set; }
		public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
		{
			this.Entered.TrySetResult();
			await Task.Delay(Timeout.Infinite, cancellationToken);
			return 0;
		}
		protected override void Dispose(bool disposing)
		{
			this.Disposed = true;
			base.Dispose(disposing);
		}
	}
}
