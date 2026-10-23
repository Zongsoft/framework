using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.Externals.Wechat.Paying;

namespace Zongsoft.Externals.Wechat.Tests;

public sealed partial class AccountManagerAsyncTest
{
	[Theory]
	[InlineData("SUCCESS", true)]
	[InlineData("USERPAYING", false)]
	[InlineData("FUTURE_STATE", false)]
	public async Task OfflinePayments_PreserveDedicatedFieldsAndQueryProduct(string status, bool hasAmount)
	{
		using var material = new HttpMaterial();
		var manager = PaymentManager.Get(material.Authority, new Authority("sub", "1900000200"));
		var request = manager.Payment.Request.Ticket("wx-synthetic", "offline/1", 1.23m, "CNY", "synthetic-face-code",
			new PaymentManager.PaymentService.PaymentRequest.TicketRequest.BusinessInfo(PaymentManager.PaymentService.PaymentRequest.TicketRequest.BusinessProduct.K12, 3), "Meal");
		request.Extra = "Synthetic order";
		request.Place = new("device_1") { DeviceIp = "192.0.2.1" };
		var calls = 0;
		context.HttpClients.PaymentHandler = async (message, cancellation) =>
		{
			if(calls++ == 0)
			{
				Assert.Equal(HttpMethod.Post, message.Method);
				Assert.Equal("/v3/offlineface/transactions", message.RequestUri.AbsolutePath);
				var body = await message.Content.ReadAsStringAsync(cancellation);
				material.AssertSignature(message, body);
				var json = JsonSerializer.Deserialize<JsonElement>(body);
				Assert.Equal(2, json.GetProperty("business").GetProperty("business_product_id").GetInt32());
				Assert.Equal(3, json.GetProperty("business").GetProperty("business_scene_id").GetInt32());
				Assert.Equal("192.0.2.1", json.GetProperty("scene_info").GetProperty("device_ip").GetString());
				Assert.Equal("wx-synthetic", json.GetProperty("sp_appid").GetString());
				Assert.False(json.TryGetProperty("sub_appid", out _));
			}
			else
			{
				Assert.Equal(HttpMethod.Get, message.Method);
				Assert.Equal("/v3/offlineface/transactions/out-trade-no/offline%2F1", message.RequestUri.AbsolutePath);
				Assert.Equal("?sp_mchid=offline-merchant&sub_mchid=1900000200&business_product_id=2", message.RequestUri.Query);
			}

			return material.Respond(OfflineBody(status, hasAmount));
		};

		try
		{
			var result = await manager.Payment.PayAsync(request, Scenario.Web, TestContext.Current.CancellationToken);
			var order = Assert.IsType<PaymentManager.PaymentService.OfflineOrder>(result.Order);
			Assert.Equal(status, order.Status);
			Assert.Equal("synthetic-description", order.StatusDescription);
			Assert.Equal(hasAmount ? 120 : 0, order.Amount.PaidValue);
			Assert.Equal("NOT_DEBT", order.DebtState);
			var queried = await manager.Payment.GetOfflineAsync("offline/1", PaymentManager.PaymentService.PaymentRequest.TicketRequest.BusinessProduct.K12, TestContext.Current.CancellationToken);
			Assert.Equal(order.Amount.PaidValue, queried.Amount.PaidValue);
			Assert.Equal(2, calls);
		}
		finally { context.HttpClients.PaymentHandler = null; }
	}

	[Theory]
	[InlineData(false, false)]
	[InlineData(true, true)]
	public async Task OfflinePayments_RejectMissingAmountsOrUnboundMerchants(bool hasAmount, bool wrongMerchant)
	{
		using var material = new HttpMaterial();
		var manager = PaymentManager.Get(material.Authority, new Authority("sub", "1900000200"));
		context.HttpClients.PaymentHandler = (_, _) => Task.FromResult(material.Respond(wrongMerchant ? OfflineBody("SUCCESS", hasAmount).Replace("1900000200", "1900000201") : OfflineBody("SUCCESS", hasAmount)));

		try
		{
			await Assert.ThrowsAsync<FormatException>(() => manager.Payment.GetOfflineAsync("offline/1", PaymentManager.PaymentService.PaymentRequest.TicketRequest.BusinessProduct.K12, TestContext.Current.CancellationToken).AsTask());
		}
		finally { context.HttpClients.PaymentHandler = null; }
	}

	[Fact]
	public async Task OfflinePayments_AuthenticationUsesSelectedApplicationAndRejectsDirectMerchant()
	{
		using var material = new HttpMaterial();
		var manager = PaymentManager.Get(material.Authority, new Authority("sub", "1900000200"));
		context.HttpClients.PaymentHandler = async (message, cancellation) =>
		{
			Assert.Equal("/v3/offlineface/authinfo", message.RequestUri.AbsolutePath);
			var body = JsonSerializer.Deserialize<JsonElement>(await message.Content.ReadAsStringAsync(cancellation));
			Assert.Equal("wx-mobile", body.GetProperty("sp_appid").GetString());
			Assert.Equal("organization1", body.GetProperty("organization_id").GetString());
			return material.Respond("{\"authinfo\":\"synthetic-authentication\"}");
		};

		try
		{
			var result = await manager.Payment.AuthenticateOfflineAsync("wx-mobile", "raw", "device_1", "organization1", TestContext.Current.CancellationToken);
			Assert.Equal("wx-mobile", JsonSerializer.SerializeToElement(result).GetProperty("AppId").GetString());
			await Assert.ThrowsAsync<NotSupportedException>(() => PaymentManager.Get(material.Authority).Payment.AuthenticateOfflineAsync(null, "raw", "device_1", "organization1", TestContext.Current.CancellationToken).AsTask());
		}
		finally { context.HttpClients.PaymentHandler = null; }
	}

	private static string OfflineBody(string status, bool amount) => JsonSerializer.Serialize(new
	{
		sp_mchid = "offline-merchant",
		sp_appid = "wx-synthetic",
		sub_mchid = "1900000200",
		out_trade_no = "offline/1",
		trade_state = status,
		trade_state_description = "synthetic-description",
		debt_state = "NOT_DEBT",
		transaction_id = status == "SUCCESS" ? "synthetic-transaction" : null,
		amount = amount ? new { total = 123, pay_total = 120, currency = "CNY", pay_currency = "CNY" } : null,
	});
}
