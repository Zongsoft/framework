using System;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Zongsoft.Externals.Wechat.Paying;
using Xunit;

namespace Zongsoft.Externals.Wechat.Tests;

public sealed partial class AccountManagerAsyncTest
{
	#region 测试方法
	[Fact]
	public async Task Partners_RegistrationUsesSeparateProtocolAndEncryptsAllSensitiveFields()
	{
		using var material = new HttpMaterial();
		var request = PartnerRegistration();
		request.SubjectInfo.IdentityInfo.IdDocInfo = new()
		{
			IdDocCopy = "media",
			IdDocName = "doc-name",
			IdDocNumber = "doc-number",
			IdDocAddress = "doc-address",
			DocPeriodBegin = "2020-01-01",
			DocPeriodEnd = "2030-01-01",
		};
		request.SubjectInfo.UboInfoList = [new()
		{
			UboIdDocType = "IDENTIFICATION_TYPE_IDCARD", UboIdDocCopy = "media", UboIdDocName = "ubo-name", UboIdDocNumber = "ubo-number", UboIdDocAddress = "ubo-address", UboPeriodBegin = "2020-01-01", UboPeriodEnd = "2030-01-01",
		}];
		context.HttpClients.PaymentHandler = async (message, cancellation) =>
		{
			Assert.Equal("/v3/applyment4sub/applyment/", message.RequestUri.AbsolutePath);
			Assert.Equal("PUB_KEY_ID_HTTP", Assert.Single(message.Headers.GetValues("Wechatpay-Serial")));
			var body = await message.Content.ReadAsStringAsync(cancellation);
			material.AssertSignature(message, body);
			var json = JsonSerializer.Deserialize<JsonElement>(body);
			Assert.Equal("partner_request", json.GetProperty("business_code").GetString());
			Assert.False(json.TryGetProperty("out_request_no", out _));
			var contact = json.GetProperty("contact_info");
			Check(contact, "contact_name", "contact-name");
			Check(contact, "contact_id_number", "contact-number");
			Check(contact, "mobile_phone", "phone-number");
			Check(contact, "contact_email", "mail@example.test");
			var identity = json.GetProperty("subject_info").GetProperty("identity_info");
			Check(identity.GetProperty("id_card_info"), "id_card_name", "card-name");
			Check(identity.GetProperty("id_card_info"), "id_card_number", "card-number");
			Check(identity.GetProperty("id_card_info"), "id_card_address", "card-address");
			Check(identity.GetProperty("id_doc_info"), "id_doc_name", "doc-name");
			Check(identity.GetProperty("id_doc_info"), "id_doc_number", "doc-number");
			Check(identity.GetProperty("id_doc_info"), "id_doc_address", "doc-address");
			var ubo = json.GetProperty("subject_info").GetProperty("ubo_info_list")[0];
			Check(ubo, "ubo_id_doc_name", "ubo-name");
			Check(ubo, "ubo_id_doc_number", "ubo-number");
			Check(ubo, "ubo_id_doc_address", "ubo-address");
			Check(json.GetProperty("bank_account_info"), "account_name", "bank-name");
			Check(json.GetProperty("bank_account_info"), "account_number", "bank-number");
			Assert.Equal("sales-app", json.GetProperty("business_info").GetProperty("sales_info").GetProperty("mini_program_info").GetProperty("mini_program_sub_appid").GetString());
			return material.Respond("{\"applyment_id\":9007199254740993}");

			void Check(JsonElement element, string property, string expected) => Assert.Equal(expected, Encoding.UTF8.GetString(material.Platform.Decrypt(Convert.FromBase64String(element.GetProperty(property).GetString()), RSAEncryptionPadding.OaepSHA1)));
		};

		try
		{
			Assert.Equal("9007199254740993", await PaymentManager.GetPartnerService(material.Authority).RegisterAsync(request, TestContext.Current.CancellationToken));
			Assert.Equal("contact-name", request.ContactInfo.ContactName);
		}
		finally { context.HttpClients.PaymentHandler = null; }
	}

	[Theory]
	[InlineData("APPLYMENT_STATE_AUDITING", false)]
	[InlineData("APPLYMENT_STATE_TO_BE_SIGNED", false)]
	[InlineData("APPLYMENT_STATE_SIGNING", false)]
	[InlineData("APPLYMENT_STATE_FINISHED", true)]
	[InlineData("UNKNOWN_FUTURE_STATUS", false)]
	public async Task Partners_QueryPreservesStateAndSeparatesApplicationAndMerchant(string state, bool completed)
	{
		using var material = new HttpMaterial();
		context.HttpClients.PaymentHandler = (message, _) =>
		{
			Assert.StartsWith("/v3/applyment4sub/applyment/", message.RequestUri.AbsolutePath, StringComparison.Ordinal);
			return Task.FromResult(material.Respond(JsonSerializer.Serialize(new { business_code = "partner_request", applyment_id = 123, sub_mchid = "merchant123", applyment_state = state, sign_url = "https://pay.example.test/sign" })));
		};

		try
		{
			var service = PaymentManager.GetPartnerService(material.Authority);
			var result = await service.GetByRequestAsync("partner_request", TestContext.Current.CancellationToken);
			Assert.Equal("merchant123", result.MerchantId);
			Assert.Equal(123, result.ApplymentId);
			Assert.Equal(state, result.State);
			Assert.Equal(completed, result.IsCompleted);
			Assert.Equal(completed, (await service.GetByApplicationAsync("123", TestContext.Current.CancellationToken)).IsCompleted);
			await Assert.ThrowsAsync<FormatException>(() => service.GetByRequestAsync("other_request", TestContext.Current.CancellationToken).AsTask());
			await Assert.ThrowsAsync<FormatException>(() => service.GetByApplicationAsync("456", TestContext.Current.CancellationToken).AsTask());
		}
		finally { context.HttpClients.PaymentHandler = null; }
	}

	[Fact]
	public async Task Partners_RejectsInvalidRegistrationBeforeNetworkAndIncompleteFinishedResponse()
	{
		using var material = new HttpMaterial();
		context.HttpClients.PaymentHandler = (_, _) => throw new Xunit.Sdk.XunitException("Unexpected request");
		var service = PaymentManager.GetPartnerService(material.Authority);
		var request = PartnerRegistration();
		request.BusinessCode = "bad/code";
		await Assert.ThrowsAsync<ArgumentException>(() => service.RegisterAsync(request, TestContext.Current.CancellationToken).AsTask());
		request.BusinessCode = "valid_code";
		request.BusinessInfo.SalesInfo.MiniProgramInfo = null;
		await Assert.ThrowsAsync<ArgumentException>(() => service.RegisterAsync(request, TestContext.Current.CancellationToken).AsTask());
		await Assert.ThrowsAsync<ArgumentException>(() => service.GetByApplicationAsync("merchant_id", TestContext.Current.CancellationToken).AsTask());
		context.HttpClients.PaymentHandler = (_, _) => Task.FromResult(material.Respond("{\"business_code\":\"valid_code\",\"applyment_id\":123,\"applyment_state\":\"APPLYMENT_STATE_FINISHED\"}"));

		try { await Assert.ThrowsAsync<FormatException>(() => service.GetByRequestAsync("valid_code", TestContext.Current.CancellationToken).AsTask()); }
		finally { context.HttpClients.PaymentHandler = null; }
	}
	#endregion

	#region 辅助方法
	private static PaymentManager.PartnerService.Registration PartnerRegistration() => new()
	{
		BusinessCode = "partner_request",
		ContactInfo = new() { ContactType = "LEGAL", ContactName = "contact-name", ContactIdNumber = "contact-number", MobilePhone = "phone-number", ContactEmail = "mail@example.test" },
		SubjectInfo = new()
		{
			SubjectType = "SUBJECT_TYPE_ENTERPRISE",
			BusinessLicenseInfo = new() { LicenseCopy = "media", LicenseNumber = "license", MerchantName = "Synthetic", LegalPerson = "Synthetic" },
			IdentityInfo = new()
			{
				IdDocType = "IDENTIFICATION_TYPE_IDCARD",
				IdCardInfo = new() { IdCardCopy = "media-front", IdCardNational = "media-back", IdCardName = "card-name", IdCardNumber = "card-number", IdCardAddress = "card-address", CardPeriodBegin = "2020-01-01", CardPeriodEnd = "2030-01-01" },
			},
		},
		BusinessInfo = new()
		{
			MerchantShortname = "Synthetic",
			ServicePhone = "123456789",
			SalesInfo = new() { SalesScenesType = ["SALES_SCENES_MINI_PROGRAM"], MiniProgramInfo = new() { MiniProgramSubAppid = "sales-app" } },
		},
		SettlementInfo = new() { SettlementId = "719", QualificationType = "example" },
		BankAccountInfo = new() { BankAccountType = "BANK_ACCOUNT_TYPE_CORPORATE", AccountName = "bank-name", AccountBank = "Synthetic", AccountNumber = "bank-number" },
	};
	#endregion
}
