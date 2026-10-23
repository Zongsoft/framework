using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Cryptography;

using Xunit;

using Zongsoft.Collections;
using Zongsoft.Externals.Wechat.Paying;

namespace Zongsoft.Externals.Wechat.Tests;

public class FallbackTest
{
	[Theory]
	[InlineData(FallbackKind.Refundment, "SUCCESS", false)]
	[InlineData(FallbackKind.Refundment, "ABNORMAL", false)]
	[InlineData(FallbackKind.Refundment, "CLOSED", true)]
	[InlineData(FallbackKind.OfflineFaceAgreement, "", false)]
	public async Task Product_ValidatesItsOwnEventAndPreservesVerifiedMetadata(FallbackKind kind, string status, bool broker)
	{
		using var material = new Material();
		var handler = new ProbeHandler(material.Authority, kind);
		using var body = material.Create(broker, kind: kind, status: status);
		Assert.Equal(FallbackReceipt.Completed, await handler.ReceiveAsync(new("1900000100", body, material.Headers), TestContext.Current.CancellationToken));
		Assert.Equal(kind, handler.Notification.Kind);
		Assert.Equal("offline-notification", handler.Notification.Identifier);
		Assert.Equal(broker ? "broker" : "direct", handler.Notification.Format);

		using var wrongProduct = material.Create(broker, kind: kind, status: status);
		var payment = new PaymentHandler(material.Authority);
		await Assert.ThrowsAsync<CryptographicException>(() => payment.ReceiveAsync(new("1900000100", wrongProduct, material.Headers), TestContext.Current.CancellationToken).AsTask());
		Assert.Equal(0, payment.Calls);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Payment_UsesAuthenticatedMerchantRole(bool broker)
	{
		using var material = new Material();
		var handler = new PaymentHandler(material.Authority);
		using var body = material.Create(broker);
		var receipt = await handler.ReceiveAsync(new("1900000100", body, material.Headers), TestContext.Current.CancellationToken);
		Assert.Equal(FallbackReceipt.Completed, receipt);
		Assert.Equal(1, handler.Calls);
		Assert.Equal("1900000100", handler.Order.Merchant.Identifier);
		Assert.Equal(broker ? "1900000200" : null, handler.Order.Subsidiary?.Identifier);
		Assert.Equal("offline-order", handler.Order.VoucherCode);
		Assert.True(body.CanRead);
	}

	[Theory]
	[InlineData("signature")]
	[InlineData("missing")]
	[InlineData("duplicate-header")]
	[InlineData("unknown-key")]
	[InlineData("expired")]
	[InlineData("future")]
	[InlineData("probe")]
	[InlineData("signature-type")]
	[InlineData("merchant")]
	[InlineData("mixed-role")]
	[InlineData("appid")]
	[InlineData("event")]
	[InlineData("resource")]
	[InlineData("algorithm")]
	[InlineData("tag")]
	[InlineData("aad")]
	[InlineData("state")]
	[InlineData("duplicate-property")]
	[InlineData("body")]
	[InlineData("oversized")]
	public async Task Payment_InvalidNotificationNeverReachesBusiness(string fault)
	{
		using var material = new Material();
		var handler = new PaymentHandler(material.Authority);
		using var body = material.Create(fault: fault);
		var exception = await Record.ExceptionAsync(() => handler.ReceiveAsync(new("1900000100", body, material.Headers), TestContext.Current.CancellationToken).AsTask());
		Assert.True(exception is CryptographicException or FormatException);
		Assert.Equal(0, handler.Calls);
	}

	[Theory]
	[InlineData("1900000100:direct")]
	[InlineData("../default")]
	[InlineData("")]
	[InlineData("1900000200")]
	public async Task Payment_RejectsUnboundSelectors(string merchant)
	{
		using var material = new Material();
		var handler = new PaymentHandler(material.Authority);
		using var body = material.Create();
		await Assert.ThrowsAsync<CryptographicException>(() => handler.ReceiveAsync(new(merchant, body, material.Headers), TestContext.Current.CancellationToken).AsTask());
		Assert.Equal(0, handler.Calls);
	}

	[Fact]
	public async Task Payment_PropagatesBusinessFailureAndCancellation()
	{
		using var material = new Material();
		var handler = new PaymentHandler(material.Authority) { Failure = new InvalidOperationException() };
		using var body = material.Create();
		await Assert.ThrowsAsync<InvalidOperationException>(() => handler.ReceiveAsync(new("1900000100", body, material.Headers), TestContext.Current.CancellationToken).AsTask());
		Assert.Equal(1, handler.Calls);

		using var cancelled = new CancellationTokenSource();
		await cancelled.CancelAsync();
		using var cancelledBody = material.Create();
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => handler.ReceiveAsync(new("1900000100", cancelledBody, material.Headers), cancelled.Token).AsTask());
		Assert.Equal(1, handler.Calls);
	}

	private sealed class PaymentHandler(IAuthority authority) : PaymentHandlerBase(new Services())
	{
		public int Calls { get; private set; }
		public Exception Failure { get; init; }
		public PaymentManager.PaymentService.PaymentOrder Order { get; private set; }
		protected override ValueTask<IAuthority> GetAuthorityAsync(string code, CancellationToken cancellation) => ValueTask.FromResult(authority);
		protected override ValueTask OnHandleAsync(PaymentManager.PaymentService.PaymentOrder request, Parameters parameters, CancellationToken cancellation)
		{
			this.Calls++;
			this.Order = request;
			Assert.IsType<FallbackNotification>(parameters[typeof(FallbackNotification)]);
			return this.Failure == null ? ValueTask.CompletedTask : ValueTask.FromException(this.Failure);
		}
	}

	private sealed class Services : IServiceProvider
	{
		public object GetService(Type serviceType) => null;
	}

	private sealed class ProbeHandler(IAuthority authority, FallbackKind kind) : FallbackHandlerBase<JsonElement>(new Services())
	{
		public override FallbackKind Kind => kind;
		public FallbackNotification Notification { get; private set; }
		protected override ValueTask<IAuthority> GetAuthorityAsync(string code, CancellationToken cancellation) => ValueTask.FromResult(authority);
		protected override Type GetRequestType(string format) => typeof(JsonElement);
		protected override ValueTask OnHandleAsync(JsonElement request, Parameters parameters, CancellationToken cancellation)
		{
			this.Notification = (FallbackNotification)parameters[typeof(FallbackNotification)];
			return ValueTask.CompletedTask;
		}
	}

	private sealed class Material : IDisposable
	{
		private const string SECRET = "01234567890123456789012345678901";
		private readonly RSA _rsa = RSA.Create(2048);
		public IAuthority Authority { get; }
		public Dictionary<string, string[]> Headers { get; } = new(StringComparer.OrdinalIgnoreCase);

		public Material()
		{
			this.Authority = new Authority("offline", "1900000100", apiV3Key: SECRET,
				verification: new Authority.Verifier(VerificationMode.PublicKey, [Wechat.Authority.VerificationKey.FromPublicKey("PUB_KEY_ID_OFFLINE", _rsa.ExportSubjectPublicKeyInfoPem())]),
				accounts: new AccountCollection(new Account(AccountType.Applet, "wx-offline", null)));
		}

		public MemoryStream Create(bool broker = false, string fault = null, FallbackKind kind = FallbackKind.Payment, string status = "SUCCESS")
		{
			var payload = new Dictionary<string, object>
			{
				[broker ? "sp_mchid" : "mchid"] = fault == "merchant" ? "1900000200" : "1900000100",
				[broker ? "sp_appid" : "appid"] = fault == "appid" ? "wx-other" : "wx-offline",
				["trade_state"] = fault == "state" ? "CLOSED" : "SUCCESS",
				["out_trade_no"] = "offline-order",
				["transaction_id"] = "offline-transaction",
			};
			if(broker)
				payload["sub_mchid"] = "1900000200";
			if(fault == "mixed-role")
				payload["sp_mchid"] = "1900000100";
			if(kind == FallbackKind.Refundment)
				payload["refund_status"] = status;

			if(kind == FallbackKind.OfflineFaceAgreement)
			{
				payload.Remove("mchid");
				payload["mch_id"] = "1900000100";
			}

			var text = JsonSerializer.Serialize(payload);
			if(fault == "duplicate-property")
				text = text.Insert(1, "\"mchid\":\"1900000100\",");

			var plain = Encoding.UTF8.GetBytes(text);
			var encrypted = new byte[plain.Length + 16];

			using(var aes = new AesGcm(Encoding.UTF8.GetBytes(SECRET), 16))
				aes.Encrypt("012345678901"u8, plain, encrypted.AsSpan(0, plain.Length), encrypted.AsSpan(plain.Length), "transaction"u8);

			if(fault == "tag")
				encrypted[^1] ^= 1;

			var eventType = kind switch
			{
				FallbackKind.Payment => "TRANSACTION.SUCCESS",
				FallbackKind.Refundment => $"REFUND.{status}",
				_ => "FACEPAY.USER_STATE_CHANGE",
			};
			var original = kind switch
			{
				FallbackKind.Payment => "transaction",
				FallbackKind.Refundment => "refund",
				_ => "facepay",
			};
			var body = JsonSerializer.SerializeToUtf8Bytes(new
			{
				id = "offline-notification",
				create_time = "2026-01-01T00:00:00+08:00",
				event_type = fault == "event" ? "REFUND.SUCCESS" : eventType,
				resource_type = fault == "resource" ? "plaintext" : "encrypt-resource",
				resource = new
				{
					algorithm = fault == "algorithm" ? "AES_CBC" : "AEAD_AES_256_GCM",
					original_type = original,
					nonce = "012345678901",
					associated_data = fault == "aad" ? "wrong" : "transaction",
					ciphertext = Convert.ToBase64String(encrypted),
				},
			});
			var timestamp = (DateTimeOffset.UtcNow.ToUnixTimeSeconds() + (fault == "expired" ? -301 : fault == "future" ? 301 : 0)).ToString(System.Globalization.CultureInfo.InvariantCulture);
			this.Headers.Clear();
			this.Headers["Wechatpay-Timestamp"] = [timestamp];
			this.Headers["Wechatpay-Nonce"] = ["offline-nonce"];
			this.Headers["Wechatpay-Serial"] = [fault == "unknown-key" ? "PUB_KEY_ID_OTHER" : "PUB_KEY_ID_OFFLINE"];
			this.Headers["Wechatpay-Signature"] = [Convert.ToBase64String(_rsa.SignData(Encoding.UTF8.GetBytes($"{timestamp}\noffline-nonce\n{Encoding.UTF8.GetString(body)}\n"), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))];

			switch(fault)
			{
				case "missing":
					this.Headers.Remove("Wechatpay-Signature");
					break;
				case "duplicate-header":
					this.Headers["Wechatpay-Nonce"] = ["one", "two"];
					break;
				case "signature":
					this.Headers["Wechatpay-Signature"] = [Convert.ToBase64String(new byte[256])];
					break;
				case "probe":
					this.Headers["Wechatpay-Signature"] = ["WECHATPAY/SIGNTEST/offline"];
					break;
				case "signature-type":
					this.Headers["Wechatpay-Signature-Type"] = ["HMAC-SHA256"];
					break;
				case "body":
					body[^2] ^= 1;
					break;
				case "oversized":
					body = new byte[2 * 1024 * 1024 + 1];
					break;
			}

			return new MemoryStream(body);
		}

		public void Dispose() => _rsa.Dispose();
	}
}
