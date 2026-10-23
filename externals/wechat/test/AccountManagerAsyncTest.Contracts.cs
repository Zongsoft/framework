using System;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.Externals.Wechat.Paying;

namespace Zongsoft.Externals.Wechat.Tests;

public sealed partial class AccountManagerAsyncTest
{
	[Theory]
	[InlineData("CONTRACTED", PaymentManager.MerchantService.ContractService.ContractStatus.Signed)]
	[InlineData("TERMINATED", PaymentManager.MerchantService.ContractService.ContractStatus.Aborted)]
	[InlineData("NEW_STATE", PaymentManager.MerchantService.ContractService.ContractStatus.Unknown)]
	public async Task FaceContracts_QueryBindsContractAndPreservesUnknownStatus(string state, PaymentManager.MerchantService.ContractService.ContractStatus expected)
	{
		using var material = new HttpMaterial();
		context.HttpClients.PaymentHandler = (request, _) =>
		{
			Assert.Equal("/v3/offlineface/contracts/contract%2F1", request.RequestUri.AbsolutePath);
			Assert.Equal("?appid=wx-synthetic", request.RequestUri.Query);
			return Task.FromResult(material.Respond(JsonSerializer.Serialize(new { contract_id = "contract/1", mchid = "offline-merchant", organization_id = "org1", user_id = "face1", contract_state = state })));
		};

		try
		{
			var result = await PaymentManager.GetMerchantService(material.Authority).Contracts.GetContractAsync("contract/1", "wx-synthetic", TestContext.Current.CancellationToken);
			Assert.Equal(expected, result.Status);
			Assert.Equal("org1", result.OrganizationId);
			Assert.Equal(expected, JsonSerializer.Deserialize<PaymentManager.MerchantService.ContractService.ContractInfo>(JsonSerializer.Serialize(result)).Status);
		}
		finally { context.HttpClients.PaymentHandler = null; }
	}

	[Theory]
	[InlineData("org2", "user1")]
	[InlineData("org1", "user2")]
	public async Task FaceContracts_SignatoryRejectsCrossOrganizationOrUser(string organization, string user)
	{
		using var material = new HttpMaterial();
		context.HttpClients.PaymentHandler = (_, _) => Task.FromResult(material.Respond(JsonSerializer.Serialize(new { organization_id = organization, out_user_id = user, user_id = "face1", contract_state = "NOT_CONTRACTED" })));

		try
		{
			await Assert.ThrowsAsync<FormatException>(() => PaymentManager.GetMerchantService(material.Authority).Contracts.GetSignatoryAsync("org1", "user1", TestContext.Current.CancellationToken).AsTask());
		}
		finally { context.HttpClients.PaymentHandler = null; }
	}

	[Fact]
	public async Task FaceContracts_PresignEncryptsOptionalIdentityAndRevokeUsesWechatUser()
	{
		using var material = new HttpMaterial();
		var service = PaymentManager.GetMerchantService(material.Authority).Contracts;
		var calls = 0;
		context.HttpClients.PaymentHandler = async (message, cancellation) =>
		{
			if(calls++ == 0)
			{
				Assert.Equal("/v3/offlineface/contracts/presign", message.RequestUri.AbsolutePath);
				Assert.Equal("PUB_KEY_ID_HTTP", Assert.Single(message.Headers.GetValues("Wechatpay-Serial")));
				var body = await message.Content.ReadAsStringAsync(cancellation);
				material.AssertSignature(message, body);
				var user = JsonSerializer.Deserialize<JsonElement>(body).GetProperty("facepay_user");
				Assert.Equal("user1", user.GetProperty("out_user_id").GetString());
				Assert.False(user.TryGetProperty("identification", out _));
				var cipher = Convert.FromBase64String(user.GetProperty("identification_name").GetString());
				Assert.Equal("Synthetic Name", Encoding.UTF8.GetString(material.Platform.Decrypt(cipher, RSAEncryptionPadding.OaepSHA1)));
				return material.Respond("{\"presign_token\":\"synthetic-presign\"}");
			}

			Assert.Equal("/v3/offlinefacemch/organizations/org1/users/user-id/face%2F1/terminate-contract", message.RequestUri.AbsolutePath);
			await material.AssertRequestAsync(message, "", cancellation);
			return material.Respond("", HttpStatusCode.NoContent);
		};

		try
		{
			var request = new PaymentManager.MerchantService.ContractService.ApplyRequest("K12", new() { UserId = "user1", OrganizationId = "org1", Name = "Synthetic Name" });
			Assert.Equal("synthetic-presign", await service.ApplyAsync(request, TestContext.Current.CancellationToken));
			await service.RevokeAsync("org1", "face/1", TestContext.Current.CancellationToken);
			Assert.Equal(2, calls);
		}
		finally { context.HttpClients.PaymentHandler = null; }
	}
}
