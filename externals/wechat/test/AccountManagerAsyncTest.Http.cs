using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Net.Http;
using System.Text.Json;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

using Xunit;

using Zongsoft.Externals.Wechat.Paying;

namespace Zongsoft.Externals.Wechat.Tests;

public sealed partial class AccountManagerAsyncTest
{
	[Theory]
	[InlineData("GET", "")]
	[InlineData("POST", "{\n\"value\":\"合成请求\"}\n")]
	[InlineData("DELETE", "")]
	public async Task Http_SignsTheExactTransmittedBytesOnce(string method, string body)
	{
		using var material = new HttpMaterial();
		using var content = new TrackedContent(body);
		using var request = new HttpRequestMessage(new HttpMethod(method), "pay/transactions/out-trade-no/a%2Fb?mchid=offline&value=a%2Bb%26c") { Content = content };
		var received = material.Respond("{ \n\"value\": \"已验证\" }");
		context.HttpClients.PaymentHandler = async (message, cancellation) =>
		{
			Assert.Equal("/v3/pay/transactions/out-trade-no/a%2Fb?mchid=offline&value=a%2Bb%26c", message.RequestUri.PathAndQuery);
			await material.AssertRequestAsync(message, body, cancellation);
			return received;
		};

		try
		{
			using var response = await material.Client.SendAsync(request, TestContext.Current.CancellationToken);
			var result = await response.GetResultAsync<HttpPayload>(TestContext.Current.CancellationToken);
			Assert.Equal("已验证", result.Value);
			Assert.Equal(1, content.Writes);
			Assert.True(material.ResponseContent.Disposed);
		}
		finally { context.HttpClients.PaymentHandler = null; }
	}

	[Fact]
	public async Task Http_EmptyPostAnd204StillRequireValidSignatures()
	{
		using var material = new HttpMaterial();
		context.HttpClients.PaymentHandler = async (request, cancellation) =>
		{
			await material.AssertRequestAsync(request, "", cancellation);
			return material.Respond("", HttpStatusCode.NoContent);
		};

		try
		{
			await material.Client.PostAsync("offlineface/contracts/close", TestContext.Current.CancellationToken);
			Assert.True(material.ResponseContent.Disposed);
		}
		finally { context.HttpClients.PaymentHandler = null; }
	}

	[Theory]
	[InlineData("missing-serial")]
	[InlineData("missing-signature")]
	[InlineData("missing-nonce")]
	[InlineData("missing-timestamp")]
	[InlineData("duplicate")]
	[InlineData("unknown")]
	[InlineData("expired")]
	[InlineData("future")]
	[InlineData("probe")]
	[InlineData("base64")]
	[InlineData("type")]
	[InlineData("tampered")]
	[InlineData("tampered-body")]
	[InlineData("unsigned-204")]
	public async Task Http_RejectsInvalidResponsesBeforeDeserialization(string fault)
	{
		using var material = new HttpMaterial();
		var response = material.Respond("not-json", fault == "unsigned-204" ? HttpStatusCode.NoContent : HttpStatusCode.OK);

		switch(fault)
		{
			case "missing-serial":
				response.Headers.Remove("Wechatpay-Serial");
				break;
			case "missing-signature":
				response.Headers.Remove("Wechatpay-Signature");
				break;
			case "missing-nonce":
				response.Headers.Remove("Wechatpay-Nonce");
				break;
			case "missing-timestamp":
				response.Headers.Remove("Wechatpay-Timestamp");
				break;
			case "duplicate":
				response.Headers.Add("Wechatpay-Nonce", "duplicate");
				break;
			case "unknown":
				Set("Wechatpay-Serial", "PUB_KEY_ID_UNKNOWN");
				break;
			case "expired":
				Set("Wechatpay-Timestamp", DateTimeOffset.UtcNow.AddMinutes(-10).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture));
				break;
			case "future":
				Set("Wechatpay-Timestamp", DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture));
				break;
			case "probe":
				Set("Wechatpay-Signature", "WECHATPAY/SIGNTEST/invalid");
				break;
			case "base64":
				Set("Wechatpay-Signature", "!");
				break;
			case "type":
				Set("Wechatpay-Signature-Type", "UNSUPPORTED");
				break;
			case "tampered":
				Set("Wechatpay-Nonce", "changed");
				break;
			case "tampered-body":
				response.Content.Dispose();
				response.Content = new TrackedContent("changed-body");
				break;
			case "unsigned-204":
				response.Headers.Clear();
				break;
		}

		context.HttpClients.PaymentHandler = (request, cancellation) => Task.FromResult(response);

		try
		{
			await Assert.ThrowsAnyAsync<CryptographicException>(() => material.Client.GetAsync<HttpPayload>("offline", TestContext.Current.CancellationToken).AsTask());
			Assert.True(material.ResponseContent.Disposed);
			Assert.True(((TrackedContent)response.Content).Disposed);
		}
		finally { context.HttpClients.PaymentHandler = null; }

		void Set(string name, string value)
		{
			response.Headers.Remove(name);
			response.Headers.TryAddWithoutValidation(name, value);
		}
	}

	[Theory]
	[InlineData(502, false, "<html>gateway error</html>", "HTTP_502")]
	[InlineData(502, false, "{\"code\":\"FORGED\"}", "HTTP_502")]
	[InlineData(404, true, "{\"code\":\"ORDER_NOT_EXIST\",\"message\":\"synthetic\"}", "ORDER_NOT_EXIST")]
	[InlineData(500, true, "<html>service error</html>", "HTTP_500")]
	[InlineData(200, true, "not-json", "INVALID_RESPONSE")]
	public async Task Http_ReportsStatusAndRequestIdWithoutTrustingUnsignedErrors(int status, bool signed, string body, string reason)
	{
		using var material = new HttpMaterial();
		var count = 0;
		context.HttpClients.PaymentHandler = (request, cancellation) =>
		{
			count++;
			var response = material.Respond(body, (HttpStatusCode)status);
			if(!signed)
				response.Headers.Clear();
			response.Headers.Add("Request-ID", "offline-request");
			response.Content.Headers.ContentType = null;
			return Task.FromResult(response);
		};

		try
		{
			var error = await Assert.ThrowsAsync<Common.OperationException>(() => material.Client.PostAsync<object, HttpPayload>("offline", new { value = "synthetic" }, TestContext.Current.CancellationToken).AsTask());
			Assert.Equal(reason, error.Reason);
			Assert.Equal(status, error.Data["StatusCode"]);
			Assert.Equal(signed, error.Data["ResponseVerified"]);
			Assert.Equal("offline-request", error.Data["RequestId"]);
			Assert.True(material.ResponseContent.Disposed);
			Assert.Equal(1, count);
		}
		finally { context.HttpClients.PaymentHandler = null; }
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Http_MultipartSignsOnlyTheBufferedMetadata(bool quoted)
	{
		using var material = new HttpMaterial();
		const string META = "{\"filename\":\"offline.png\",\"sha256\":\"synthetic\"}";
		using var metadata = new TrackedContent(META);
		metadata.Headers.ContentType = new("application/json");
		using var file = new TrackedContent("synthetic-file");
		using var form = new MultipartFormDataContent("offline-boundary");
		form.Add(metadata, quoted ? "\"meta\"" : "meta");
		form.Add(file, "file", "offline.png");
		using var request = new HttpRequestMessage(HttpMethod.Post, "merchant/media/upload") { Content = form };
		context.HttpClients.PaymentHandler = async (message, cancellation) =>
		{
			material.AssertSignature(message, META);
			var body = await message.Content.ReadAsStringAsync(cancellation);
			Assert.Contains(META, body);
			Assert.Contains("synthetic-file", body);
			return material.Respond("{}", HttpStatusCode.OK);
		};

		try
		{
			using var response = await material.Client.SendAsync(request, TestContext.Current.CancellationToken);
			Assert.Equal(1, metadata.Writes);
			Assert.Equal(1, file.Writes);
		}
		finally { context.HttpClients.PaymentHandler = null; }
	}

	[Fact]
	public async Task Http_BankEncryptionUsesTheMatchingRequestHeaderAndEscapedQuery()
	{
		using var material = new HttpMaterial();
		context.HttpClients.PaymentHandler = async (request, cancellation) =>
		{
			await material.AssertRequestAsync(request, "", cancellation);
			Assert.Equal("PUB_KEY_ID_HTTP", Assert.Single(request.Headers.GetValues("Wechatpay-Serial")));
			var value = request.RequestUri.Query["?account_number=".Length..];
			Assert.DoesNotContain("+", value);
			var ciphertext = Convert.FromBase64String(Uri.UnescapeDataString(value));
			Assert.Equal("synthetic-account", Encoding.UTF8.GetString(material.Platform.Decrypt(ciphertext, RSAEncryptionPadding.OaepSHA1)));
			return material.Respond("{\"data\":[],\"count\":0,\"total_count\":0}");
		};

		try
		{
			Assert.Empty(await material.Authority.FindAsync("synthetic-account", TestContext.Current.CancellationToken));
		}
		finally { context.HttpClients.PaymentHandler = null; }
	}

	private sealed class HttpPayload
	{
		public string Value { get; set; }
	}

	private sealed class HttpMaterial : IDisposable
	{
		public RSA Merchant { get; } = RSA.Create(2048);
		public RSA Platform { get; } = RSA.Create(2048);
		public Authority Authority { get; }
		public HttpClient Client => Paying.HttpClientFactory.GetHttpClient(this.Authority);
		public TrackedContent ResponseContent { get; private set; }
		public HttpMaterial(string merchantId = "offline-merchant")
		{
			this.Authority = new("offline-http", merchantId, signing: new(merchantId, "0102", this.Merchant.ExportPkcs8PrivateKeyPem()), verification: new(VerificationMode.PublicKey, [Authority.VerificationKey.FromPublicKey("PUB_KEY_ID_HTTP", this.Platform.ExportSubjectPublicKeyInfoPem())]), accounts: new AccountCollection(Account.Applet("wx-synthetic")) { Account.Application("wx-mobile"), Account.Channel("wx-channel") });
		}

		public async Task AssertRequestAsync(HttpRequestMessage request, string expected, CancellationToken cancellation)
		{
			var actual = request.Content == null ? "" : await request.Content.ReadAsStringAsync(cancellation);
			Assert.Equal(expected, actual);
			this.AssertSignature(request, actual);
		}

		public void AssertSignature(HttpRequestMessage request, string body)
		{
			Assert.Equal("WECHATPAY2-SHA256-RSA2048", request.Headers.Authorization.Scheme);
			var fields = Regex.Matches(request.Headers.Authorization.Parameter, "(\\w+)=\"([^\"]+)\"").ToDictionary(match => match.Groups[1].Value, match => match.Groups[2].Value);
			Assert.Equal(this.Authority.Code, fields["mchid"]);
			Assert.Equal("0102", fields["serial_no"]);
			var message = Encoding.UTF8.GetBytes($"{request.Method.Method}\n{request.RequestUri.PathAndQuery}\n{fields["timestamp"]}\n{fields["nonce_str"]}\n{body}\n");
			Assert.True(this.Merchant.VerifyData(message, Convert.FromBase64String(fields["signature"]), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
		}

		public HttpResponseMessage Respond(string body, HttpStatusCode status = HttpStatusCode.OK)
		{
			this.ResponseContent = new TrackedContent(body);
			var response = new HttpResponseMessage(status) { Content = this.ResponseContent };
			var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
			const string NONCE = "offline-response";
			var signature = this.Platform.SignData(Encoding.UTF8.GetBytes($"{timestamp}\n{NONCE}\n{body}\n"), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
			response.Headers.Add("Wechatpay-Serial", "PUB_KEY_ID_HTTP");
			response.Headers.Add("Wechatpay-Timestamp", timestamp);
			response.Headers.Add("Wechatpay-Nonce", NONCE);
			response.Headers.Add("Wechatpay-Signature", Convert.ToBase64String(signature));
			return response;
		}

		public void Dispose()
		{
			this.Merchant.Dispose();
			this.Platform.Dispose();
		}
	}

	private sealed class TrackedContent(string body) : HttpContent
	{
		public int Writes { get; private set; }
		public bool Disposed { get; private set; }
		protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
		{
			this.Writes++;
			await stream.WriteAsync(Encoding.UTF8.GetBytes(this.Writes == 1 ? body : "unexpected-reserialization"), TestContext.Current.CancellationToken);
		}
		protected override bool TryComputeLength(out long length)
		{
			length = 0;
			return false;
		}
		protected override void Dispose(bool disposing)
		{
			this.Disposed = true;
			base.Dispose(disposing);
		}
	}
}
