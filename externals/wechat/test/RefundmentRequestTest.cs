using System;
using System.Text.Json;

using Xunit;

using Zongsoft.Externals.Wechat.Paying;

namespace Zongsoft.Externals.Wechat.Tests;

public class RefundmentRequestTest
{
	[Theory]
	[InlineData(0, 100)]
	[InlineData(-1, 100)]
	[InlineData(101, 100)]
	public void Amount_RejectsInvalidRefundRange(int amount, int total) =>
		Assert.Throws<ArgumentOutOfRangeException>("value", () => new PaymentManager.RefundmentService.RefundmentRequest.AmountInfo(amount, total));

	[Fact]
	public void Amount_ValidatesBothAmountsAndRejectsDefaultRequestValue()
	{
		Assert.Throws<ArgumentException>("paidAmount", () => new PaymentManager.RefundmentService.RefundmentRequest.AmountInfo(0.01m, 1.001m));
		Assert.Throws<ArgumentException>("value", () => new PaymentManager.RefundmentService.RefundmentRequest.AmountInfo(0.001m, 1m));
		Assert.Throws<ArgumentOutOfRangeException>("paidAmount", () => new PaymentManager.RefundmentService.RefundmentRequest.AmountInfo(1, 0));
		Assert.Throws<ArgumentException>("currency", () => new PaymentManager.RefundmentService.RefundmentRequest.AmountInfo(1, 100, "USD"));
		var request = new PaymentManager.RefundmentService.RefundmentRequest("order", "refund", 0.01m, 1m);
		Assert.Throws<ArgumentOutOfRangeException>("value", () => request.Amount = default);
		var amount = JsonSerializer.Deserialize<PaymentManager.RefundmentService.RefundmentRequest.AmountInfo>(JsonSerializer.Serialize(request.Amount));
		Assert.Equal(1, amount.Value);
		Assert.Equal(100, amount.PaidAmount);
		Assert.Equal("CNY", amount.Currency);
	}

	[Fact]
	public void Request_PreservesBusinessIdentifiersAndPartialRefundAmounts()
	{
		var request = new PaymentManager.RefundmentService.RefundmentRequest("offline-payment-001", "offline-refund-001", 1.23m, 12.34m, "CNY", "离线部分退款")
		{
			FallbackUrl = "https://example.com/wechat/refund",
		};
		var json = JsonSerializer.SerializeToElement(request);

		Assert.Equal("offline-payment-001", json.GetProperty("out_trade_no").GetString());
		Assert.Equal("offline-refund-001", json.GetProperty("out_refund_no").GetString());
		Assert.False(json.TryGetProperty("transaction_id", out _));
		Assert.Equal("离线部分退款", json.GetProperty("reason").GetString());
		Assert.Equal("https://example.com/wechat/refund", json.GetProperty("notify_url").GetString());

		var amount = json.GetProperty("amount");
		Assert.Equal(123, amount.GetProperty("refund").GetInt32());
		Assert.Equal(1234, amount.GetProperty("total").GetInt32());
		Assert.Equal("CNY", amount.GetProperty("currency").GetString());
	}

	[Theory]
	[InlineData("PROCESSING")]
	[InlineData("SUCCESS")]
	[InlineData("CLOSED")]
	[InlineData("ABNORMAL")]
	public void Response_PreservesRefundStatusAndDistinctOrderIdentifiers(string status)
	{
		var json = $$"""
		{
			"transaction_id": "offline-transaction-001",
			"out_trade_no": "offline-payment-001",
			"refund_id": "offline-refund-id-001",
			"out_refund_no": "offline-refund-001",
			"status": "{{status}}",
			"amount": { "refund": 123, "total": 1234, "payer_refund": 100, "payer_total": 1000, "currency": "CNY" }
		}
		""";
		var order = JsonSerializer.Deserialize<PaymentManager.RefundmentService.RefundmentOrder>(json);

		Assert.NotNull(order);
		Assert.Equal(status, order.Status);
		Assert.Equal("offline-transaction-001", order.PaymentSerialId);
		Assert.Equal("offline-payment-001", order.PaymentVoucherCode);
		Assert.Equal("offline-refund-id-001", order.RefundId);
		Assert.Equal("offline-refund-001", order.RefundVoucherCode);
		Assert.Equal(123, order.Amount.Value);
		Assert.Equal(1234, order.Amount.PaidAmount);
		Assert.Equal(100, order.Amount.ActualRefund);
		Assert.Equal(1000, order.Amount.ActualTotal);
		Assert.Equal("CNY", order.Amount.Currency);
	}
}
