using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;

using Xunit;

using Zongsoft.Externals.Wechat.Paying;

namespace Zongsoft.Externals.Wechat.Tests;

public sealed partial class AccountManagerAsyncTest
{
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Refunds_UseV3ForOriginalPaymentAndBindVerifiedContext(bool broker)
	{
		using var material = new HttpMaterial();
		var manager = broker ? PaymentManager.Get(material.Authority, new Authority("sub", "0004294967296")) : PaymentManager.Get(material.Authority);
		var request = manager.Refundment.Request.Create("v2-original", "refund-001", 0.40m, 1m);
		request.FallbackUrl = "https://callback.example.test/refund";
		context.HttpClients.PaymentHandler = async (message, cancellation) =>
		{
			Assert.Equal(HttpMethod.Post, message.Method);
			Assert.Equal("/v3/refund/domestic/refunds", message.RequestUri.AbsolutePath);
			var body = await message.Content.ReadAsStringAsync(cancellation);
			var json = JsonSerializer.Deserialize<JsonElement>(body);
			Assert.Equal("v2-original", json.GetProperty("out_trade_no").GetString());
			Assert.Equal("refund-001", json.GetProperty("out_refund_no").GetString());
			Assert.Equal(request.FallbackUrl, json.GetProperty("notify_url").GetString());
			Assert.Equal(broker, json.TryGetProperty("sub_mchid", out var child));
			Assert.False(json.TryGetProperty("mchid", out _));

			if(broker)
				Assert.Equal("0004294967296", child.GetString());

			material.AssertSignature(message, body);
			return material.Respond(RefundResponse("PROCESSING"));
		};

		try
		{
			var result = await manager.Refundment.RefundAsync(request, TestContext.Current.CancellationToken);
			Assert.Equal("PROCESSING", result.Order.Status);
			Assert.Equal(material.Authority.Code, result.Order.Merchant.Identifier);
			Assert.Equal(broker ? "0004294967296" : null, result.Order.Subsidiary?.Identifier);
			Assert.Null(result.Order.RefundedTime);
		}
		finally { context.HttpClients.PaymentHandler = null; }
	}

	[Theory]
	[InlineData(false, "SUCCESS")]
	[InlineData(true, "CLOSED")]
	[InlineData(true, "ABNORMAL")]
	[InlineData(false, "FUTURE")]
	public async Task Refunds_QueryPreservesStatesAndEncodesPath(bool broker, string status)
	{
		using var material = new HttpMaterial();
		var manager = broker ? PaymentManager.Get(material.Authority, new Authority("sub", "0004294967296")) : PaymentManager.Get(material.Authority);
		context.HttpClients.PaymentHandler = (message, _) =>
		{
			Assert.Equal(HttpMethod.Get, message.Method);
			Assert.Equal("/v3/refund/domestic/refunds/refund%40one", message.RequestUri.AbsolutePath);
			Assert.Equal(broker ? "?sub_mchid=0004294967296" : "", message.RequestUri.Query);
			return Task.FromResult(material.Respond(RefundResponse(status).Replace("refund-001", "refund@one")));
		};

		try { Assert.Equal(status, (await manager.Refundment.GetAsync("refund@one", TestContext.Current.CancellationToken)).Status); }
		finally { context.HttpClients.PaymentHandler = null; }
	}

	[Theory]
	[InlineData("refund-001", "another-refund")]
	[InlineData("v2-original", "another-payment")]
	[InlineData("\"refund\":40", "\"refund\":50")]
	[InlineData("\"total\":100", "\"total\":200")]
	[InlineData("\"status\"", "\"refund_status\"")]
	[InlineData("CNY", "USD")]
	public async Task Refunds_RejectMismatchedOrNotificationShapedResponse(string original, string replacement)
	{
		using var material = new HttpMaterial();
		var manager = PaymentManager.Get(material.Authority);
		var request = manager.Refundment.Request.Create("v2-original", "refund-001", 0.4m, 1m);
		request.FallbackUrl = "https://callback.example.test/refund";
		context.HttpClients.PaymentHandler = (_, _) => Task.FromResult(material.Respond(RefundResponse("PROCESSING").Replace(original, replacement)));

		try { await Assert.ThrowsAsync<FormatException>(() => manager.Refundment.RefundAsync(request, TestContext.Current.CancellationToken).AsTask()); }
		finally { context.HttpClients.PaymentHandler = null; }
	}

	[Fact]
	public async Task Refunds_RejectCrossMerchantRequestsAndMissingCallbackBeforeSending()
	{
		using var material = new HttpMaterial();
		var manager = PaymentManager.Get(material.Authority);
		var request = manager.Refundment.Request.Create("v2-original", "refund-001", 0.4m, 1m);
		request.FallbackUrl = null;
		await Assert.ThrowsAsync<ArgumentException>(() => manager.Refundment.RefundAsync(request, TestContext.Current.CancellationToken).AsTask());
		request.FallbackUrl = "https://callback.example.test/refund";
		await Assert.ThrowsAsync<ArgumentException>(() => PaymentManager.Get(new Authority("different", "different")).Refundment.RefundAsync(request, TestContext.Current.CancellationToken).AsTask());
		var broker = PaymentManager.Get(material.Authority, new Authority("sub", "sub"));
		request = broker.Refundment.Request.Create("v2-original", "refund-001", 0.4m, 1m);
		request.FallbackUrl = "https://callback.example.test/refund";
		await Assert.ThrowsAsync<ArgumentException>(() => PaymentManager.Get(new Authority("different", "different"), new Authority("sub", "sub")).Refundment.RefundAsync(request, TestContext.Current.CancellationToken).AsTask());
	}

	[Fact]
	public void Refunds_NotificationUsesOnlyItsOwnStatusField()
	{
		var notification = JsonSerializer.Deserialize<PaymentManager.RefundmentService.RefundmentNotification>("{\"refund_status\":\"ABNORMAL\"}");
		Assert.Equal("ABNORMAL", notification.Status);
		Assert.Null(JsonSerializer.Deserialize<PaymentManager.RefundmentService.RefundmentOrder>("{\"refund_status\":\"SUCCESS\"}").Status);
	}

	private static string RefundResponse(string status) => $$$"""
		{"refund_id":"wechat-refund","out_refund_no":"refund-001","out_trade_no":"v2-original","transaction_id":"wechat-payment","status":"{{{status}}}","create_time":"2026-10-20T10:00:00+08:00","success_time":{{{(status == "SUCCESS" ? "\"2026-10-20T10:01:00+08:00\"" : "null")}}},"amount":{"refund":40,"total":100,"payer_refund":40,"payer_total":100,"currency":"CNY"}}
		""";
}
