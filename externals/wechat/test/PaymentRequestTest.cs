using System;
using System.Text.Json;
using System.Globalization;

using Xunit;

using Zongsoft.Externals.Wechat.Paying;

namespace Zongsoft.Externals.Wechat.Tests;

public class PaymentRequestTest
{
	[Theory]
	[InlineData("0.01", 1)]
	[InlineData("1.00", 100)]
	[InlineData("12.34", 1234)]
	[InlineData("21474836.47", int.MaxValue)]
	public void Amount_SerializesYuanAsIntegerCents(string yuan, int cents)
	{
		var amount = new PaymentManager.PaymentService.PaymentRequest.AmountInfo(decimal.Parse(yuan, CultureInfo.InvariantCulture), "CNY");
		var json = JsonSerializer.SerializeToElement(amount);

		Assert.Equal(JsonValueKind.Number, json.GetProperty("total").ValueKind);
		Assert.Equal(cents, json.GetProperty("total").GetInt32());
		Assert.Equal("CNY", json.GetProperty("currency").GetString());
	}

	[Fact]
	public void Amount_IntegerInputAlreadyUsesCents()
	{
		var amount = new PaymentManager.PaymentService.PaymentRequest.AmountInfo(1234, "CNY");
		var json = JsonSerializer.SerializeToElement(amount);

		Assert.Equal(1234, json.GetProperty("total").GetInt32());
		Assert.Equal("CNY", json.GetProperty("currency").GetString());
	}

	[Theory]
	[InlineData("0")]
	[InlineData("-0.01")]
	[InlineData("21474836.48")]
	[InlineData("79228162514264337593543950335")]
	public void Amount_RejectsOutOfRangeValues(string value) =>
		Assert.Throws<ArgumentOutOfRangeException>("value", () => new PaymentManager.PaymentService.PaymentRequest.AmountInfo(decimal.Parse(value, CultureInfo.InvariantCulture)));

	[Theory]
	[InlineData("0.001")]
	[InlineData("1.999")]
	public void Amount_RejectsFractionalCents(string value) =>
		Assert.Throws<ArgumentException>("value", () => new PaymentManager.PaymentService.PaymentRequest.AmountInfo(decimal.Parse(value, CultureInfo.InvariantCulture)));

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("CNY")]
	[InlineData("cny")]
	public void Amount_NormalizesSupportedCurrency(string currency)
	{
		var amount = new PaymentManager.PaymentService.PaymentRequest.AmountInfo(1, currency);
		Assert.Equal("CNY", amount.Currency);
		Assert.Equal(1, JsonSerializer.Deserialize<PaymentManager.PaymentService.PaymentRequest.AmountInfo>(JsonSerializer.Serialize(amount)).Value);
	}

	[Theory]
	[InlineData("USD")]
	[InlineData("CN")]
	[InlineData(" ")]
	public void Amount_RejectsUnsupportedCurrency(string currency) =>
		Assert.Throws<ArgumentException>("currency", () => new PaymentManager.PaymentService.PaymentRequest.AmountInfo(1, currency));
}
