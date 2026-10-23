using System;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Generic;

using Xunit;
using Zongsoft.Externals.Wechat.Paying;

namespace Zongsoft.Externals.Wechat.Tests;

public sealed partial class AccountManagerAsyncTest
{
	[Theory]
	[InlineData(false, "SUCCESS", PaymentStatus.Succeed)]
	[InlineData(true, "SUCCESS", PaymentStatus.Succeed)]
	[InlineData(false, "USERPAYING", PaymentStatus.Paying)]
	[InlineData(true, "SYSTEMERROR", PaymentStatus.Unknown)]
	[InlineData(false, "BANKERROR", PaymentStatus.Unknown)]
	[InlineData(false, "FUTURE_ERROR", PaymentStatus.Unknown)]
	[InlineData(false, "AUTHCODEEXPIRE", PaymentStatus.Failed)]
	public async Task Tickets_PreserveConfirmedAndUncertainOutcomes(bool broker, string outcome, PaymentStatus status)
	{
		var service = TicketService(broker);
		var ticket = service.Request.Ticket(broker ? "wx-child" : "wx-ticket", "ticket-17", 1.23m, "synthetic-auth-code", "商品 <A> & B");
		ticket.Place = new(null) { DeviceIp = "192.0.2.17" };
		context.HttpClients.XmlHandler = async (request, cancellation) =>
		{
			Assert.Equal("/pay/micropay", request.RequestUri.AbsolutePath);
			var fields = await ReadTicketAsync(request, cancellation);
			Assert.Equal("商品 <A> & B", fields["body"]);
			Assert.Equal("123", fields["total_fee"]);
			Assert.Equal("synthetic-auth-code", fields["auth_code"]);
			Assert.Equal("wx-ticket", fields["appid"]);

			if(broker)
				Assert.Equal("wx-child", fields["sub_appid"]);

			var result = TicketResponse(fields);
			if(outcome != "SUCCESS")
			{
				result["result_code"] = "FAIL";
				result["err_code"] = outcome;
				result.Remove("transaction_id");
			}

			return RespondTicket(result);
		};

		try
		{
			var result = await service.PayAsync(ticket, Scenario.Web, TestContext.Current.CancellationToken);
			Assert.Equal(status, PaymentUtility.GetStatus(result.Order.Status));
			Assert.Equal("ticket-17", result.Order.VoucherCode);
			Assert.Equal(status == PaymentStatus.Succeed ? 123 : 0, result.Order.Amount.PaidValue);
			Assert.Equal(status == PaymentStatus.Succeed ? "transaction-17" : null, result.Order.Identifier);
			Assert.Null(result.PrepaymentId);
		}
		finally { context.HttpClients.XmlHandler = null; }
	}

	[Theory]
	[InlineData("USERPAYING", PaymentStatus.Paying)]
	[InlineData("NOTPAY", PaymentStatus.None)]
	[InlineData("UNKNOWN_STATE", PaymentStatus.Unknown)]
	[InlineData("SUCCESS", PaymentStatus.Succeed)]
	[InlineData("REVOKED", PaymentStatus.Revoked)]
	public async Task Tickets_QueryUsesTradeState(string state, PaymentStatus expected)
	{
		var service = TicketService(true);
		context.HttpClients.XmlHandler = async (request, cancellation) =>
		{
			Assert.Equal("/pay/orderquery", request.RequestUri.AbsolutePath);
			var fields = await ReadTicketAsync(request, cancellation);
			Assert.Equal("ticket-17", fields["out_trade_no"]);
			Assert.False(fields.ContainsKey("auth_code"));
			var result = TicketResponse(fields);
			result["trade_state"] = state;
			return RespondTicket(result);
		};

		try
		{
			var order = await service.Tickets.GetAsync("wx-child", "ticket-17", TestContext.Current.CancellationToken);
			Assert.Equal(expected, PaymentUtility.GetStatus(order.Status));
		}
		finally { context.HttpClients.XmlHandler = null; }
	}

	[Theory]
	[InlineData("signature")]
	[InlineData("unsigned")]
	[InlineData("merchant")]
	[InlineData("appid")]
	[InlineData("submerchant")]
	[InlineData("amount")]
	[InlineData("voucher")]
	[InlineData("duplicate")]
	[InlineData("nested")]
	[InlineData("dtd")]
	public async Task Tickets_RejectUntrustedOrUnboundResponses(string fault)
	{
		var service = TicketService(true);
		var ticket = service.Request.Ticket("wx-child", "ticket-17", 1.23m, "synthetic-code", "商品");
		ticket.Place = new(null) { DeviceIp = "192.0.2.17" };
		context.HttpClients.XmlHandler = async (request, cancellation) =>
		{
			var fields = TicketResponse(await ReadTicketAsync(request, cancellation));
			switch(fault)
			{
				case "merchant":
					fields["mch_id"] = "other";
					break;
				case "appid":
					fields["appid"] = "other";
					break;
				case "submerchant":
					fields["sub_mch_id"] = "other";
					break;
				case "amount":
					fields["total_fee"] = "124";
					break;
				case "voucher":
					fields["out_trade_no"] = "other";
					break;
			}

			var response = RespondTicket(fields);
			if(fault is "signature" or "unsigned" or "duplicate" or "nested" or "dtd")
			{
				var document = XDocument.Parse(await response.Content.ReadAsStringAsync(cancellation));
				if(fault == "signature")
					document.Root.Element("sign").Value = "00";
				if(fault == "unsigned")
					document.Root.Element("sign").Remove();
				if(fault == "duplicate")
					document.Root.Add(new XElement("mch_id", "other"));
				if(fault == "nested")
					document.Root.Element("mch_id").Add(new XElement("nested", "other"));
				response.Content.Dispose();
				response.Content = new StringContent((fault == "dtd" ? "<!DOCTYPE xml [<!ENTITY x 'other'>]>" : "") + document.ToString());
			}

			return response;
		};

		try
		{
			var error = await Record.ExceptionAsync(async () => await service.PayAsync(ticket, Scenario.Web, TestContext.Current.CancellationToken));
			Assert.True(error is CryptographicException or FormatException or System.Xml.XmlException);
		}
		finally { context.HttpClients.XmlHandler = null; }
	}

	[Theory]
	[InlineData("SUCCESS", null, true, false)]
	[InlineData("FAIL", "Y", false, true)]
	[InlineData("FAIL", "N", false, false)]
	public async Task Tickets_ReversalRequiresCertificateAndPreservesRecall(string status, string recall, bool succeeded, bool retry)
	{
		using var rsa = RSA.Create(2048);
		using var certificate = new CertificateRequest("CN=synthetic", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1).CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
		var signing = new Authority.SigningCredential("ticket-merchant", certificate.SerialNumber, rsa.ExportPkcs8PrivateKeyPem(), certificate: certificate.ExportCertificatePem());
		var service = TicketService(true, signing);
		context.HttpClients.XmlHandler = async (request, cancellation) =>
		{
			Assert.Equal("/secapi/pay/reverse", request.RequestUri.AbsolutePath);
			Assert.Equal($"Wechat.Payments.Xml.ticket-merchant.{certificate.SerialNumber}", context.HttpClients.XmlTransportName);
			var fields = TicketResponse(await ReadTicketAsync(request, cancellation));
			fields["result_code"] = status;

			if(recall != null)
				fields["recall"] = recall;
			return RespondTicket(fields);
		};

		try
		{
			var result = await service.Tickets.ReverseAsync("wx-child", "ticket-17", TestContext.Current.CancellationToken);
			Assert.Equal(succeeded, result.Succeeded);
			Assert.Equal(retry, result.RequiresRetry);
			await Assert.ThrowsAsync<InvalidOperationException>(async () => await TicketService(false).Tickets.ReverseAsync("wx-ticket", "ticket-17", TestContext.Current.CancellationToken));
		}
		finally { context.HttpClients.XmlHandler = null; }
	}

	[Fact]
	public async Task Tickets_OnlineAuthenticationVerifiesTheDedicatedResponse()
	{
		context.HttpClients.XmlHandler = async (request, cancellation) =>
		{
			Assert.Equal("https://payapp.weixin.qq.com/face/get_wxpayface_authinfo", request.RequestUri.AbsoluteUri);
			var fields = TicketResponse(await ReadTicketAsync(request, cancellation));
			fields.Remove("result_code");
			fields["authinfo"] = "synthetic-authinfo";
			fields["expires_in"] = "7200";
			return RespondTicket(fields);
		};

		try
		{
			var result = await TicketService(true).AuthenticateOnlineAsync("wx-child", "synthetic-raw-data", "device-17", "store-17", "门店", cancellation: TestContext.Current.CancellationToken);
			var json = System.Text.Json.JsonSerializer.SerializeToElement(result);
			Assert.Equal("synthetic-authinfo", json.GetProperty("Value").GetString());
			Assert.Equal("wx-child", json.GetProperty("SubAppId").GetString());
		}
		finally { context.HttpClients.XmlHandler = null; }
	}

	[Fact]
	public async Task Tickets_CancellationPropagatesAndNeverRetriesTheCharge()
	{
		using var cancellation = new CancellationTokenSource();
		var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var calls = 0;
		context.HttpClients.XmlHandler = async (_, token) =>
		{
			calls++;
			entered.SetResult();
			await Task.Delay(Timeout.Infinite, token);
			throw new InvalidOperationException();
		};
		var service = TicketService(false);
		var ticket = service.Request.Ticket("wx-ticket", "ticket-17", 1.23m, "synthetic-code", "商品");
		ticket.Place = new(null) { DeviceIp = "192.0.2.17" };

		try
		{
			var operation = service.PayAsync(ticket, Scenario.Web, cancellation.Token).AsTask();
			await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
			cancellation.Cancel();
			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);
			Assert.Equal(1, calls);
		}
		finally { context.HttpClients.XmlHandler = null; }
	}

	private const string TICKET_KEY = "0123456789abcdef0123456789abcdef";
	private static PaymentManager.PaymentService TicketService(bool broker, Authority.SigningCredential signing = null)
	{
		var merchant = new Authority("ticket", "ticket-merchant", apiV2Key: TICKET_KEY, signing: signing, accounts: new AccountCollection(Account.Applet("wx-ticket")));
		return (broker ? PaymentManager.Get(merchant, new Authority("child", "ticket-child", accounts: new AccountCollection(Account.Applet("wx-child")))) : PaymentManager.Get(merchant)).Payment;
	}

	private static async Task<Dictionary<string, string>> ReadTicketAsync(HttpRequestMessage request, CancellationToken cancellation)
	{
		var fields = XDocument.Parse(await request.Content.ReadAsStringAsync(cancellation)).Root.Elements().ToDictionary(element => element.Name.LocalName, element => element.Value, StringComparer.Ordinal);
		Assert.Equal(SignTicket(fields), fields["sign"]);
		Assert.Null(request.Headers.Authorization);
		return fields;
	}

	private static Dictionary<string, string> TicketResponse(Dictionary<string, string> request)
	{
		var result = new Dictionary<string, string>(StringComparer.Ordinal)
		{
			["return_code"] = "SUCCESS",
			["result_code"] = "SUCCESS",
			["nonce_str"] = "synthetic-response-nonce",
			["transaction_id"] = "transaction-17",
			["out_trade_no"] = "ticket-17",
			["total_fee"] = "123",
			["cash_fee"] = "123",
			["trade_type"] = "MICROPAY",
		};
		foreach(var field in new[] { "appid", "mch_id", "sub_appid", "sub_mch_id" })
		{
			if(request.TryGetValue(field, out var value))
				result[field] = value;
		}

		return result;
	}

	private static HttpResponseMessage RespondTicket(Dictionary<string, string> fields)
	{
		fields["sign"] = SignTicket(fields);
		return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
		{
			Content = new StringContent(new XElement("xml", fields.Select(field => new XElement(field.Key, field.Value))).ToString(), Encoding.UTF8, "application/xml"),
		};
	}

	private static string SignTicket(Dictionary<string, string> fields)
	{
		var canonical = string.Join("&", fields.Where(field => field.Key != "sign" && !string.IsNullOrEmpty(field.Value)).OrderBy(field => field.Key, StringComparer.Ordinal).Select(field => $"{field.Key}={field.Value}"));
		return Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes($"{canonical}&key={TICKET_KEY}")));
	}
}
