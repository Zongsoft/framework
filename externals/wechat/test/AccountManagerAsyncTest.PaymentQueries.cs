using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

using Xunit;

using Zongsoft.Externals.Wechat.Paying;

namespace Zongsoft.Externals.Wechat.Tests;

public sealed partial class AccountManagerAsyncTest
{
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Payments_QueryAndCloseUseBoundRoleAndEscapedIdentifiers(bool broker)
	{
		using var material = new HttpMaterial();
		var manager = broker ? PaymentManager.Get(material.Authority, new Authority("sub", "1900000200")) : PaymentManager.Get(material.Authority);
		const string VOUCHER = "order/with?reserved";
		const string TRANSACTION = "transaction/with?reserved";
		var calls = 0;
		context.HttpClients.PaymentHandler = async (request, cancellation) =>
		{
			var index = calls++;
			var path = $"/v3/pay/{(broker ? "partner/" : "")}transactions/";

			if(index < 2)
			{
				Assert.Equal(HttpMethod.Get, request.Method);
				Assert.Equal(path + (index == 0 ? "out-trade-no/" + Uri.EscapeDataString(VOUCHER) : "id/" + Uri.EscapeDataString(TRANSACTION)), request.RequestUri.AbsolutePath);
				Assert.Equal(broker ? "?sp_mchid=offline-merchant&sub_mchid=1900000200" : "?mchid=offline-merchant", request.RequestUri.Query);
				await material.AssertRequestAsync(request, "", cancellation);
				return material.Respond(QueryBody(broker, VOUCHER, TRANSACTION));
			}

			Assert.Equal(2, index);
			Assert.Equal(HttpMethod.Post, request.Method);
			Assert.Equal(path + "out-trade-no/" + Uri.EscapeDataString(VOUCHER) + "/close", request.RequestUri.AbsolutePath);
			Assert.Empty(request.RequestUri.Query);
			var body = await request.Content.ReadAsStringAsync(cancellation);
			using var json = JsonDocument.Parse(body);
			Assert.Equal("offline-merchant", json.RootElement.GetProperty(broker ? "sp_mchid" : "mchid").GetString());

			if(broker)
				Assert.Equal("1900000200", json.RootElement.GetProperty("sub_mchid").GetString());

			material.AssertSignature(request, body);
			return material.Respond("", HttpStatusCode.NoContent);
		};

		try
		{
			var pending = await manager.Payment.GetAsync(VOUCHER, TestContext.Current.CancellationToken);
			Assert.Equal(PaymentStatus.Unknown, PaymentUtility.GetStatus(pending.Status));
			Assert.Equal(VOUCHER, pending.VoucherCode);
			Assert.Equal(TRANSACTION, (await manager.Payment.GetCompletedAsync(TRANSACTION, TestContext.Current.CancellationToken)).Identifier);
			await manager.Payment.CancelAsync(VOUCHER, TestContext.Current.CancellationToken);
			Assert.Equal(3, calls);
		}
		finally { context.HttpClients.PaymentHandler = null; }
	}

	[Theory]
	[InlineData("merchant")]
	[InlineData("application")]
	[InlineData("subsidiary")]
	[InlineData("sub-application")]
	[InlineData("voucher")]
	[InlineData("amount")]
	[InlineData("currency")]
	[InlineData("transaction")]
	public async Task Payments_QueriesRejectForeignOrMalformedOrders(string fault)
	{
		using var material = new HttpMaterial();
		var manager = PaymentManager.Get(material.Authority, new Authority("sub", "1900000200"));
		context.HttpClients.PaymentHandler = (request, cancellation) => Task.FromResult(material.Respond(QueryBody(true, "order", "transaction", fault)));

		try
		{
			if(fault == "transaction")
				await Assert.ThrowsAsync<FormatException>(() => manager.Payment.GetCompletedAsync("different-transaction", TestContext.Current.CancellationToken).AsTask());
			else
				await Assert.ThrowsAsync<FormatException>(() => manager.Payment.GetAsync("order", TestContext.Current.CancellationToken).AsTask());
		}
		finally { context.HttpClients.PaymentHandler = null; }
	}

	private static string QueryBody(bool broker, string voucher, string transaction, string fault = null)
	{
		var data = new Dictionary<string, object>
		{
			[broker ? "sp_mchid" : "mchid"] = fault == "merchant" ? "foreign-merchant" : "offline-merchant",
			[broker ? "sp_appid" : "appid"] = fault == "application" ? "wx-foreign" : "wx-synthetic",
			["out_trade_no"] = fault == "voucher" ? "different-order" : voucher,
			["transaction_id"] = transaction,
			["trade_state"] = "FUTURE_STATE",
			["amount"] = new { total = fault == "amount" ? -1 : 100, payer_total = 0, currency = fault == "currency" ? "USD" : "CNY" },
		};

		if(broker)
			data.Add("sub_mchid", fault == "subsidiary" ? "foreign-subsidiary" : "1900000200");
		if(fault == "sub-application")
			data.Add("sub_appid", "wx-foreign");

		return JsonSerializer.Serialize(data);
	}
}
