using System;
using System.Text;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Text.Json;

using Xunit;

using Zongsoft.Externals.Wechat.Paying;

namespace Zongsoft.Externals.Wechat.Tests;

public sealed partial class AccountManagerAsyncTest
{
	[Theory]
	[InlineData(false, Scenario.Web)]
	[InlineData(false, Scenario.App)]
	[InlineData(false, Scenario.Native)]
	[InlineData(true, Scenario.Web)]
	[InlineData(true, Scenario.App)]
	[InlineData(true, Scenario.Native)]
	public async Task Payments_PrepaymentIsNotATransaction(bool broker, Scenario scenario)
	{
		using var material = new HttpMaterial();
		var manager = broker ? PaymentManager.Get(material.Authority, new Authority("sub", "0004294967296")) : PaymentManager.Get(material.Authority);
		var request = manager.Payment.Request.Create(scenario == Scenario.App ? "wx-mobile" : "wx-synthetic", "000-order", 1.23m, "CNY", "synthetic-openid", "Synthetic order");
		request.FallbackUrl = "https://callback.example.test/payment";
		context.HttpClients.PaymentHandler = async (message, cancellation) =>
		{
			var body = await message.Content.ReadAsStringAsync(cancellation);
			using var json = JsonDocument.Parse(body);
			Assert.Equal($"/v3/pay/{(broker ? "partner/" : "")}transactions/{(scenario == Scenario.Web ? "jsapi" : scenario == Scenario.App ? "app" : "native")}", message.RequestUri.AbsolutePath);
			Assert.Equal(scenario == Scenario.Web, json.RootElement.TryGetProperty("payer", out _));
			Assert.False(json.RootElement.TryGetProperty("scene_info", out _));
			Assert.Equal("000-order", json.RootElement.GetProperty("out_trade_no").GetString());
			Assert.Equal(123, json.RootElement.GetProperty("amount").GetProperty("total").GetInt32());

			if(broker)
				Assert.Equal("0004294967296", json.RootElement.GetProperty("sub_mchid").GetString());

			material.AssertSignature(message, body);
			return material.Respond(scenario == Scenario.Native ? "{\"code_url\":\"weixin://synthetic\"}" : "{\"prepay_id\":\"synthetic-prepayment\"}");
		};

		try
		{
			var result = await manager.Payment.PayAsync(request, scenario, TestContext.Current.CancellationToken);
			Assert.Null(result.Order);
			Assert.Equal(scenario == Scenario.Native ? null : "synthetic-prepayment", result.PrepaymentId);
			Assert.Equal(scenario == Scenario.Native ? "weixin://synthetic" : null, result.CodeUrl);

			if(scenario == Scenario.Native)
				Assert.Null(result.Parameters);
			else
				AssertInvocation(material, result, scenario, scenario == Scenario.App ? "wx-mobile" : "wx-synthetic", material.Authority.Code);
		}
		finally { context.HttpClients.PaymentHandler = null; }
	}

	[Theory]
	[InlineData(Scenario.Web, "{\"prepay_id\":\"xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx\"}")]
	[InlineData(Scenario.Web, "{}")]
	[InlineData(Scenario.Native, "{}")]
	[InlineData(Scenario.Web, "{\"code_url\":\"weixin://wrong-product\"}")]
	[InlineData(Scenario.Native, "{\"prepay_id\":\"wrong-product\"}")]
	[InlineData(Scenario.Web, "{\"prepay_id\":\"one\",\"code_url\":\"two\"}")]
	public async Task Payments_InvalidPrepaymentResponseFails(Scenario scenario, string body)
	{
		using var material = new HttpMaterial();
		var manager = PaymentManager.Get(material.Authority);
		var request = manager.Payment.Request.Create("wx-synthetic", "000-order", 1m, "CNY", "synthetic-openid", "Synthetic order");
		request.FallbackUrl = "https://callback.example.test/payment";
		context.HttpClients.PaymentHandler = (message, cancellation) => Task.FromResult(material.Respond(body));

		try { await Assert.ThrowsAsync<FormatException>(() => manager.Payment.PayAsync(request, scenario, TestContext.Current.CancellationToken).AsTask()); }
		finally { context.HttpClients.PaymentHandler = null; }
	}

	[Fact]
	public async Task Payments_PreserveMerchantIdentifiersAsStrings()
	{
		using var material = new HttpMaterial("0004294967296");
		var manager = PaymentManager.Get(material.Authority);
		var request = manager.Payment.Request.Create("wx-synthetic", "000-order", 1m, "CNY", "synthetic-openid", "Synthetic order");
		request.FallbackUrl = "https://callback.example.test/payment";
		var json = JsonSerializer.SerializeToElement(request, request.GetType());
		Assert.Equal("0004294967296", json.GetProperty("mchid").GetString());

		context.HttpClients.PaymentHandler = (message, cancellation) => Task.FromResult(material.Respond("{\"mchid\":\"0004294967296\",\"transaction_id\":\"000-transaction\",\"appid\":\"wx-synthetic\",\"out_trade_no\":\"000-order\",\"amount\":{\"total\":100,\"currency\":\"CNY\"}}"));

		try
		{
			var order = await manager.Payment.GetAsync("000-order", TestContext.Current.CancellationToken);
			Assert.Equal("0004294967296", order.Merchant.Identifier);
			Assert.Equal("000-transaction", order.Identifier);
		}
		finally { context.HttpClients.PaymentHandler = null; }
	}

	[Theory]
	[InlineData(Scenario.Web)]
	[InlineData(Scenario.App)]
	[InlineData(Scenario.Native)]
	public async Task Payments_SubmerchantApplicationOwnsPayerAndInvocation(Scenario scenario)
	{
		using var material = new HttpMaterial();
		var childApp = scenario == Scenario.App ? "wx-child-mobile" : "wx-child";
		var subsidiary = new Authority("sub", "0004294967296", accounts: new AccountCollection(Account.Applet("wx-child")) { Account.Application("wx-child-mobile") });
		var manager = PaymentManager.Get(material.Authority, subsidiary);
		var request = manager.Payment.Request.Create(childApp, "child-order", 1m, "CNY", scenario == Scenario.Web ? "child-openid" : null, "Synthetic order");
		request.FallbackUrl = "https://callback.example.test/payment";
		context.HttpClients.PaymentHandler = async (message, cancellation) =>
		{
			var body = await message.Content.ReadAsStringAsync(cancellation);
			using var json = JsonDocument.Parse(body);
			Assert.Equal(childApp, json.RootElement.GetProperty("sub_appid").GetString());
			Assert.Equal(scenario == Scenario.App ? "wx-mobile" : "wx-synthetic", json.RootElement.GetProperty("sp_appid").GetString());
			Assert.Equal("0004294967296", json.RootElement.GetProperty("sub_mchid").GetString());

			if(scenario == Scenario.Web)
			{
				var payer = json.RootElement.GetProperty("payer");
				Assert.Equal("child-openid", payer.GetProperty("sub_openid").GetString());
				Assert.False(payer.TryGetProperty("sp_openid", out _));
			}
			else
				Assert.False(json.RootElement.TryGetProperty("payer", out _));

			material.AssertSignature(message, body);
			return material.Respond(scenario == Scenario.Native ? "{\"code_url\":\"weixin://synthetic\"}" : "{\"prepay_id\":\"synthetic-prepayment\"}");
		};

		try
		{
			var result = await manager.Payment.PayAsync(request, scenario, TestContext.Current.CancellationToken);
			if(scenario != Scenario.Native)
				AssertInvocation(material, result, scenario, childApp, subsidiary.Code);
		}
		finally { context.HttpClients.PaymentHandler = null; }
	}

	[Theory]
	[InlineData("identity")]
	[InlineData("scenario")]
	[InlineData("payer")]
	[InlineData("application")]
	[InlineData("callback")]
	[InlineData("credentials")]
	public async Task Payments_InvalidContextFailsBeforeTransport(string fault)
	{
		using var material = new HttpMaterial();
		var manager = PaymentManager.Get(material.Authority);
		var request = manager.Payment.Request.Create("wx-synthetic", "000-order", 1m, "CNY", fault == "payer" ? null : "synthetic-openid", "Synthetic order");
		request.FallbackUrl = fault == "callback" ? null : "https://callback.example.test/payment";

		if(fault == "identity" || fault == "credentials")
			manager = PaymentManager.Get(new Authority("other", fault == "identity" ? "other-merchant" : material.Authority.Code, accounts: material.Authority.Accounts));

		var scenario = fault == "scenario" ? (Scenario)99 : fault == "application" ? Scenario.App : Scenario.Web;
		context.HttpClients.PaymentHandler = (message, cancellation) => throw new InvalidOperationException("Unexpected transport.");

		try { await Assert.ThrowsAnyAsync<ArgumentException>(() => manager.Payment.PayAsync(request, scenario, TestContext.Current.CancellationToken).AsTask()); }
		finally { context.HttpClients.PaymentHandler = null; }
	}

	[Fact]
	public async Task Payments_MobileApplicationConfigurationDoesNotRequireLoginSecrets()
	{
		var options = new Options.AuthorityOptions { Name = "mobile", Code = "1900000100" };
		options.Apps.Add(new Options.AppOptions { Name = "wx-mobile", Type = AccountType.Application });
		var authority = await AuthorityUtility.CreateAsync(options, TestContext.Current.CancellationToken);
		Assert.Equal(AccountType.Application, authority.Accounts.Default.Type);
		Assert.Equal("wx-mobile", authority.Accounts.Default.Code);
		Assert.Null(authority.Accounts.Default.Secret);
		Assert.Throws<ArgumentException>(() => PaymentManager.Get(authority).Payment.Request.Create("wx-unbound", "order", 1m, "CNY", null));
	}

	private static void AssertInvocation(HttpMaterial material, PaymentManager.PaymentService.PaymentResult result, Scenario scenario, string appId, string merchantId)
	{
		var parameters = result.Parameters;
		Assert.Equal(appId, parameters["appId"]);
		Assert.Equal(32, parameters["nonceStr"].Length);
		var signature = Convert.FromBase64String(parameters[scenario == Scenario.App ? "sign" : "paySign"]);
		var message = $"{appId}\n{parameters["timeStamp"]}\n{parameters["nonceStr"]}\n{(scenario == Scenario.App ? result.PrepaymentId : "prepay_id=" + result.PrepaymentId)}\n";
		Assert.True(material.Merchant.VerifyData(Encoding.UTF8.GetBytes(message), signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));

		if(scenario == Scenario.App)
		{
			Assert.Equal(merchantId, parameters["partnerId"]);
			Assert.Equal(result.PrepaymentId, parameters["prepayId"]);
			Assert.Equal("Sign=WXPay", parameters["packageValue"]);
			Assert.False(parameters.ContainsKey("paySign"));
		}
		else
		{
			Assert.Equal("prepay_id=" + result.PrepaymentId, parameters["package"]);
			Assert.Equal("RSA", parameters["signType"]);
			Assert.False(parameters.ContainsKey("partnerId"));
		}
	}
}
