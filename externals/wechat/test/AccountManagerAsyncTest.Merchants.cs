using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Net.Http;
using System.Text.Json;
using System.Security.Cryptography;
using System.Threading.Tasks;

using Xunit;
using Zongsoft.Data;
using Zongsoft.Common;
using Zongsoft.Externals.Wechat.Paying;

namespace Zongsoft.Externals.Wechat.Tests;

public sealed partial class AccountManagerAsyncTest
{
	[Fact]
	public async Task Merchants_RegistrationBindsRequestAndEncryptsIdentityWithMatchingKey()
	{
		using var material = new HttpMaterial();
		context.HttpClients.PaymentHandler = async (message, cancellation) =>
		{
			Assert.Equal("/v3/ecommerce/applyments/", message.RequestUri.AbsolutePath);
			Assert.Equal("PUB_KEY_ID_HTTP", Assert.Single(message.Headers.GetValues("Wechatpay-Serial")));
			var body = await message.Content.ReadAsStringAsync(cancellation);
			material.AssertSignature(message, body);
			var json = JsonSerializer.Deserialize<JsonElement>(body);
			Assert.Equal("2401", json.GetProperty("organization_type").GetString());
			var identity = json.GetProperty("id_card_info");
			Assert.Equal("2020-01-01", identity.GetProperty("id_card_valid_time_begin").GetString());
			Assert.Equal("长期", identity.GetProperty("id_card_valid_time").GetString());
			Assert.Equal("Synthetic", Encoding.UTF8.GetString(material.Platform.Decrypt(System.Convert.FromBase64String(identity.GetProperty("id_card_name").GetString()), RSAEncryptionPadding.OaepSHA1)));
			Assert.Equal(new[] { "media1", "media2" }, JsonSerializer.Deserialize<string[]>(json.GetProperty("business_addition_pics").GetString()));
			Assert.False(json.TryGetProperty("need_account_info", out _));
			return material.Respond("{\"applyment_id\":123,\"out_request_no\":\"application1\"}");
		};

		try
		{
			var request = new PaymentManager.MerchantService.Registration
			{
				RequestId = "application1",
				MerchantKind = 2401,
				MerchantAbbr = "Synthetic",
				Contact = new() { ContactKind = 65, Name = "Synthetic", Phone = "synthetic-phone" },
				Store = new() { Name = "Synthetic", WebUrl = "https://shop.example.test/" },
				BankAccount = new() { AccountKind = 75, AccountName = "Synthetic", AccountCode = "synthetic-account", BankAbbr = "Synthetic" },
				IdentityCard = new() { Name = "Synthetic", Code = "synthetic-identity", PhotoA = "media-front", PhotoB = "media-back", IssuedTime = new(2020, 1, 1) },
				Attachments = ["media1", "media2"],
			};
			Assert.Equal("123", await PaymentManager.GetMerchantService(material.Authority).RegisterAsync(request, TestContext.Current.CancellationToken));
		}
		finally { context.HttpClients.PaymentHandler = null; }
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Merchants_QueryEscapesOriginalIdAndDecryptsOnlyVerifiedAccount(bool wrongOwner)
	{
		using var material = new HttpMaterial();
		context.HttpClients.PaymentHandler = (message, _) =>
		{
			Assert.Equal("/v3/ecommerce/applyments/out-request-no/application%2F1", message.RequestUri.AbsolutePath);
			return Task.FromResult(material.Respond(JsonSerializer.Serialize(new
			{
				out_request_no = wrongOwner ? "other" : "application/1",
				applyment_id = 123,
				applyment_state = "ACCOUNT_NEED_VERIFY",
				account_validation = new
				{
					account_name = System.Convert.ToBase64String(material.Merchant.Encrypt(Encoding.UTF8.GetBytes("Synthetic"), RSAEncryptionPadding.OaepSHA1)),
					account_no = System.Convert.ToBase64String(material.Merchant.Encrypt(Encoding.UTF8.GetBytes("synthetic-bank"), RSAEncryptionPadding.OaepSHA1)),
					pay_amount = 10,
					deadline = "2026-12-31 12:30:00",
				},
			})));
		};

		try
		{
			var service = PaymentManager.GetMerchantService(material.Authority);
			if(wrongOwner)
				await Assert.ThrowsAsync<FormatException>(() => service.GetAsync("application/1", TestContext.Current.CancellationToken).AsTask());
			else
			{
				var result = await service.GetAsync("application/1", TestContext.Current.CancellationToken);
				Assert.Equal("Synthetic", result.Account.Name);
				Assert.Equal("synthetic-bank", result.Account.Code);
				Assert.Equal(new DateTime(2026, 12, 31, 12, 30, 0), result.Account.Deadline);
			}
		}
		finally { context.HttpClients.PaymentHandler = null; }
	}

	[Fact]
	public async Task Banks_PreservesMultipleCandidatesAndEncryptedQuery()
	{
		using var material = new HttpMaterial();
		context.HttpClients.PaymentHandler = (message, _) =>
		{
			Assert.Equal("PUB_KEY_ID_HTTP", Assert.Single(message.Headers.GetValues("Wechatpay-Serial")));
			var encrypted = Uri.UnescapeDataString(message.RequestUri.Query["?account_number=".Length..]);
			Assert.Equal("synthetic-account", Encoding.UTF8.GetString(material.Platform.Decrypt(System.Convert.FromBase64String(encrypted), RSAEncryptionPadding.OaepSHA1)));
			return Task.FromResult(material.Respond("{\"total_count\":2,\"data\":[{\"account_bank_code\":1001},{\"account_bank_code\":1002}]}"));
		};

		try
		{
			var banks = await material.Authority.FindAsync("synthetic-account", TestContext.Current.CancellationToken);
			Assert.Equal(new uint[] { 1001, 1002 }, banks.Select(bank => bank.Id));
			context.HttpClients.PaymentHandler = (message, _) =>
			{
				Assert.Equal("/v3/capital/capitallhh/banks/bank%2F1/branches?city_code=city%26value&offset=10&limit=10", message.RequestUri.PathAndQuery);
				return Task.FromResult(material.Respond("{\"total_count\":5,\"count\":0,\"offset\":10,\"data\":[]}"));
			};
			var page = Paging.Page(2, 10);
			Assert.Empty(await material.Authority.GetBranchesAsync("bank/1", "city&value", page, TestContext.Current.CancellationToken));
			Assert.Equal(5, page.Total);
		}
		finally { context.HttpClients.PaymentHandler = null; }
	}

	[Theory]
	[InlineData(".png", "image/png", "merchant/media/upload")]
	[InlineData(".pdf", "application/pdf", "merchant/media/upload")]
	[InlineData(".mp4", "video/mp4", "merchant/media/video_upload")]
	public async Task Uploads_BoundsContentAndSignsExactMetadata(string extension, string type, string path)
	{
		using var material = new HttpMaterial();
		var file = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + extension);
		var data = Encoding.UTF8.GetBytes("synthetic media content");
		await File.WriteAllBytesAsync(file, data, TestContext.Current.CancellationToken);
		context.HttpClients.PaymentHandler = async (message, cancellation) =>
		{
			Assert.Equal("/v3/" + path, message.RequestUri.AbsolutePath);
			var parts = Assert.IsType<MultipartFormDataContent>(message.Content).ToArray();
			var meta = await parts[0].ReadAsStringAsync(cancellation);
			material.AssertSignature(message, meta);
			var json = JsonSerializer.Deserialize<JsonElement>(meta);
			Assert.Equal(Path.GetFileName(file), json.GetProperty("filename").GetString());
			Assert.Equal(System.Convert.ToHexString(SHA256.HashData(data)), json.GetProperty("sha256").GetString());
			Assert.Equal(type, parts[1].Headers.ContentType.MediaType);
			Assert.Equal(data, await parts[1].ReadAsByteArrayAsync(cancellation));
			return material.Respond("{\"media_id\":\"synthetic-media\"}");
		};

		try
		{
			Assert.Equal("synthetic-media", await material.Authority.UploadAsync(file, TestContext.Current.CancellationToken));
			context.HttpClients.PaymentHandler = (_, _) => throw new InvalidOperationException("Oversized data reached transport");

			await using(var stream = File.Open(file, FileMode.Open, FileAccess.Write, FileShare.None))
				stream.SetLength(8 * 1024 * 1024);

			var error = await Assert.ThrowsAsync<OperationException>(() => material.Authority.UploadAsync(file, TestContext.Current.CancellationToken).AsTask());
			Assert.Equal("InvalidFileSize", error.Reason);
		}
		finally
		{
			context.HttpClients.PaymentHandler = null;
			File.Delete(file);
		}
	}
}
