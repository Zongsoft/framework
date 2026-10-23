using System;
using System.Text;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using Xunit;

namespace Zongsoft.Externals.Wechat.Tests;

public sealed partial class AccountManagerAsyncTest
{
	[Fact]
	public async Task Certificates_RefreshOnceAndCancelOnlyTheCaller()
	{
		using var original = new PlatformMaterial(-2, 5);
		using var replacement = new PlatformMaterial(-1, 10);
		var authority = original.CreateAuthority();
		var other = original.CreateAuthority();
		var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		using var canceled = new CancellationTokenSource();
		var count = 0;
		context.HttpClients.Handler = async (request, cancellation) =>
		{
			Interlocked.Increment(ref count);
			Assert.Equal("https://api.mch.weixin.qq.com/v3/certificates", request.RequestUri.AbsoluteUri);
			Assert.Contains("mchid=\"offline-merchant\"", request.Headers.Authorization.Parameter);
			entered.TrySetResult();
			await release.Task.WaitAsync(cancellation);
			return original.Respond([original.Entry(), replacement.Entry()]);
		};
		var first = authority.RefreshCertificatesAsync(canceled.Token).AsTask();
		Task second = null;

		try
		{
			await entered.Task.WaitAsync(_timeout, TestContext.Current.CancellationToken);
			second = authority.RefreshCertificatesAsync(TestContext.Current.CancellationToken).AsTask();
			canceled.Cancel();
			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);
			Assert.False(second.IsCompleted);
			release.TrySetResult();
			await second.WaitAsync(_timeout, TestContext.Current.CancellationToken);
			var key = await authority.GetCertificateAsync(TestContext.Current.CancellationToken);
			Assert.Equal(replacement.Certificate.SerialNumber, key.Identifier);
			Assert.True(key.Verify([1, 2], replacement.Key.SignData([1, 2], HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)));
			Assert.Equal(original.Certificate.SerialNumber, (await other.GetCertificateAsync(TestContext.Current.CancellationToken)).Identifier);
			Assert.Null(await authority.GetCertificateAsync("unknown", TestContext.Current.CancellationToken));
			Assert.Equal(1, count);
		}
		finally
		{
			release.TrySetResult();
			await ObserveAsync(first);
			if(second != null)
				await ObserveAsync(second);
			context.HttpClients.Handler = null;
		}
	}

	[Theory]
	[InlineData("signature")]
	[InlineData("serial")]
	[InlineData("ciphertext")]
	[InlineData("algorithm")]
	[InlineData("duplicate")]
	[InlineData("timestamp")]
	public async Task Certificates_RejectUntrustedOrMalformedUpdatesAtomically(string fault)
	{
		using var original = new PlatformMaterial(-2, 5);
		using var replacement = new PlatformMaterial(-1, 10);
		var authority = original.CreateAuthority();
		var count = 0;
		context.HttpClients.Handler = (request, cancellation) =>
		{
			count++;
			var entry = replacement.Entry(fault: fault);
			return Task.FromResult(original.Respond(fault == "duplicate" ? [entry, entry] : [original.Entry(), entry], fault == "signature" ? replacement.Key : null, fault == "timestamp"));
		};

		try
		{
			await Assert.ThrowsAnyAsync<CryptographicException>(() => authority.RefreshCertificatesAsync(TestContext.Current.CancellationToken).AsTask());
			Assert.Equal(original.Certificate.SerialNumber, (await authority.GetCertificateAsync(TestContext.Current.CancellationToken)).Identifier);
			await Assert.ThrowsAnyAsync<CryptographicException>(() => authority.GetCertificateAsync(replacement.Certificate.SerialNumber, TestContext.Current.CancellationToken).AsTask());
			Assert.Equal(1, count);
		}
		finally { context.HttpClients.Handler = null; }
	}

	[Fact]
	public async Task Certificates_RespectRetirementBeforeX509Expiration()
	{
		using var original = new PlatformMaterial(-2, 5);
		using var replacement = new PlatformMaterial(-1, 10);
		var authority = original.CreateAuthority();
		context.HttpClients.Handler = (request, cancellation) => Task.FromResult(original.Respond([original.Entry(DateTime.UtcNow.AddMinutes(-1)), replacement.Entry()]));

		try
		{
			await authority.RefreshCertificatesAsync(TestContext.Current.CancellationToken);
			Assert.True(original.Certificate.NotAfter.ToUniversalTime() > DateTime.UtcNow);
			Assert.Null(await authority.GetCertificateAsync(original.Certificate.SerialNumber, TestContext.Current.CancellationToken));
			Assert.Equal(replacement.Certificate.SerialNumber, (await authority.GetCertificateAsync(TestContext.Current.CancellationToken)).Identifier);
		}
		finally { context.HttpClients.Handler = null; }
	}

	[Fact]
	public async Task Certificates_ExpiredTrustCannotBootstrapItself()
	{
		using var original = new PlatformMaterial(-3, -1);
		var count = 0;
		context.HttpClients.Handler = (request, cancellation) =>
		{
			count++;
			throw new InvalidOperationException();
		};

		try
		{
			await Assert.ThrowsAsync<CryptographicException>(() => original.CreateAuthority().RefreshCertificatesAsync(TestContext.Current.CancellationToken).AsTask());
			Assert.Equal(0, count);
		}
		finally { context.HttpClients.Handler = null; }
	}

	private sealed class PlatformMaterial : IDisposable
	{
		private const string API_KEY = "33333333333333333333333333333333";
		public RSA Key { get; } = RSA.Create(2048);
		public X509Certificate2 Certificate { get; }
		public PlatformMaterial(int start, int end) => this.Certificate = new CertificateRequest("CN=offline-platform", this.Key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1).CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(start), DateTimeOffset.UtcNow.AddDays(end));
		public Authority CreateAuthority() => new("offline", "offline-merchant", apiV3Key: API_KEY, signing: new("offline-merchant", "0102", this.Key.ExportPkcs8PrivateKeyPem()), verification: new(VerificationMode.PlatformCertificate, [Authority.VerificationKey.FromCertificate(this.Certificate.ExportCertificatePem())]));
		public object Entry(DateTime? expiration = null, string fault = null)
		{
			var nonce = RandomNumberGenerator.GetBytes(12);
			//Synthetic test material uses an ASCII nonce, matching the API's UTF-8 wire value.
			var nonceText = Convert.ToHexString(nonce)[..12];
			var plaintext = Encoding.UTF8.GetBytes(this.Certificate.ExportCertificatePem());
			var ciphertext = new byte[plaintext.Length + 16];
			using var cipher = new AesGcm(Encoding.UTF8.GetBytes(API_KEY), 16);
			cipher.Encrypt(Encoding.UTF8.GetBytes(nonceText), plaintext, ciphertext.AsSpan(0, plaintext.Length), ciphertext.AsSpan(plaintext.Length), Encoding.UTF8.GetBytes("certificate"));

			if(fault == "ciphertext")
				ciphertext[^1] ^= 1;

			return new
			{
				serial_no = fault == "serial" ? "010203" : this.Certificate.SerialNumber,
				effective_time = this.Certificate.NotBefore.ToUniversalTime(),
				expire_time = expiration ?? this.Certificate.NotAfter.ToUniversalTime(),
				encrypt_certificate = new { algorithm = fault == "algorithm" ? "invalid" : "AEAD_AES_256_GCM", nonce = nonceText, associated_data = "certificate", ciphertext = Convert.ToBase64String(ciphertext) },
			};
		}
		public HttpResponseMessage Respond(object[] entries, RSA signer = null, bool stale = false)
		{
			var body = JsonSerializer.Serialize(new { data = entries });
			var timestamp = DateTimeOffset.UtcNow.AddMinutes(stale ? -10 : 0).ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture);
			const string NONCE = "offline-response";
			var signature = (signer ?? this.Key).SignData(Encoding.UTF8.GetBytes($"{timestamp}\n{NONCE}\n{body}\n"), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
			var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
			response.Headers.Add("Wechatpay-Serial", this.Certificate.SerialNumber);
			response.Headers.Add("Wechatpay-Timestamp", timestamp);
			response.Headers.Add("Wechatpay-Nonce", NONCE);
			response.Headers.Add("Wechatpay-Signature", Convert.ToBase64String(signature));
			return response;
		}
		public void Dispose()
		{
			this.Certificate.Dispose();
			this.Key.Dispose();
		}
	}
}
