using System;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Zongsoft.Externals.Wechat.Tests;

public sealed partial class AccountManagerAsyncTest
{
	[Fact]
	public async Task Membership_CreateSignsBoundMerchantAppAndBrand()
	{
		using var material = new HttpMaterial();
		var membership = new Channel(Account.Channel("wx-channel")).GetMembership(material.Authority, "brand1");
		context.HttpClients.PaymentHandler = async (message, cancellation) =>
		{
			Assert.Equal("/v3/marketing/membercard-open/cards", message.RequestUri.AbsolutePath);
			var body = await message.Content.ReadAsStringAsync(cancellation);
			material.AssertSignature(message, body);
			var json = JsonSerializer.Deserialize<JsonElement>(body);
			Assert.Equal("wx-channel", json.GetProperty("appid").GetString());
			Assert.Equal("brand1", json.GetProperty("brand").GetProperty("brand_id").GetString());
			Assert.False(json.TryGetProperty("total_quantity", out _));
			Assert.Equal(new[] { "USER_FORM_FLAG_BIRTHDAY", "USER_FORM_FLAG_CITY", "USER_FORM_FLAG_MOBILE" },
				json.GetProperty("user_information_form").GetProperty("common_field_list").EnumerateArray().Select(value => value.GetString()));
			return material.Respond(MembershipTemplate());
		};

		try
		{
			var template = new Membership.CardTemplate
			{
				AppId = "wx-channel",
				Brand = new() { BrandId = "brand1", Title = "Synthetic" },
				RequestNo = "request1",
				Title = "Synthetic",
				Description = "Synthetic",
				CodeMode = Membership.CardMode.Manual,
				LogoUrl = "https://media.example.test/logo",
				BackgroundUrl = "https://media.example.test/background",
				Validation = new(new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.FromHours(8)), new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.FromHours(8))),
				User = new() { Flags = Membership.UserFlags.Birthday | Membership.UserFlags.City | Membership.UserFlags.Phone },
			};
			Assert.Equal("card/1", (await membership.Templates.CreateAsync(template, TestContext.Current.CancellationToken)).TemplateId);
			template.AppId = "wx-other";
			await Assert.ThrowsAsync<ArgumentException>(() => membership.Templates.CreateAsync(template, TestContext.Current.CancellationToken).AsTask());
		}
		finally { context.HttpClients.PaymentHandler = null; }
	}

	[Theory]
	[InlineData("delete", false)]
	[InlineData("delete", true)]
	[InlineData("obsolete", false)]
	[InlineData("obsolete", true)]
	[InlineData("allocate", false)]
	[InlineData("allocate", true)]
	public async Task Membership_OperationsVerifyTemplateScopeBeforeMutation(string action, bool foreign)
	{
		using var material = new HttpMaterial();
		var membership = new Membership(material.Authority, Account.Channel("wx-channel"), "brand1");
		var calls = 0;
		context.HttpClients.PaymentHandler = async (message, cancellation) =>
		{
			calls++;
			var body = message.Content == null ? "" : await message.Content.ReadAsStringAsync(cancellation);
			material.AssertSignature(message, body);

			if(calls == 1)
			{
				Assert.Equal("GET", message.Method.Method);
				Assert.Equal("/v3/marketing/membercard-open/cards/card%2F1", message.RequestUri.AbsolutePath);
				return material.Respond(MembershipTemplate(foreign ? "brand2" : "brand1"));
			}

			Assert.False(foreign);
			if(action == "delete")
			{
				Assert.Equal("DELETE", message.Method.Method);
				Assert.Equal("/v3/marketing/membercard-open/cards/card%2F1", message.RequestUri.AbsolutePath);
			}
			else if(action == "obsolete")
			{
				Assert.Equal("POST", message.Method.Method);
				Assert.Equal("/v3/marketing/membercard-open/cards/card%2F1/codes/code%2F1/unavailable", message.RequestUri.AbsolutePath);
				Assert.Equal("synthetic", JsonSerializer.Deserialize<JsonElement>(body).GetProperty("reason").GetString());
			}
			else
			{
				Assert.Equal("/v3/marketing/membercard-open/cards/card%2F1/codes/deposit", message.RequestUri.AbsolutePath);
				Assert.Equal(new[] { "one", "two", "three" }, JsonSerializer.Deserialize<JsonElement>(body).GetProperty("code").EnumerateArray().Select(value => value.GetString()));
				return material.Respond("{\"data\":[{\"code\":\"one\",\"result\":\"SUCCESS\"},{\"code\":\"two\",\"result\":\"DUPLICATE\"},{\"code\":\"three\",\"result\":\"FAIL\"}]}");
			}

			return material.Respond("", HttpStatusCode.NoContent);
		};

		try
		{
			if(foreign)
				await Assert.ThrowsAsync<FormatException>(ActAsync);
			else
				await ActAsync();

			Assert.Equal(foreign ? 1 : 2, calls);
			Assert.True(material.ResponseContent.Disposed);

			async Task ActAsync()
			{
				switch(action)
				{
					case "delete":
						await membership.Templates.DeleteAsync("card/1", TestContext.Current.CancellationToken);
						break;
					case "obsolete":
						await membership.ObsoleteAsync("card/1", "code/1", "synthetic", TestContext.Current.CancellationToken);
						break;
					default:
						var results = await membership.AllocateAsync("card/1", ["one", "two", "three"], TestContext.Current.CancellationToken);
						Assert.Equal(new[] { "SUCCESS", "DUPLICATE", "FAIL" }, results.Select(entry => entry.Result));
						break;
				}
			}
		}
		finally { context.HttpClients.PaymentHandler = null; }
	}

	[Fact]
	public void Membership_RequiresAConfiguredChannel()
	{
		using var material = new HttpMaterial();
		Assert.Throws<ArgumentException>(() => new Membership(material.Authority, Account.Applet("wx-synthetic"), "brand1"));
		Assert.Throws<ArgumentException>(() => new Membership(material.Authority, Account.Channel("wx-foreign"), "brand1"));
	}

	[Theory]
	[InlineData("id")]
	[InlineData("app")]
	[InlineData("brand")]
	[InlineData("signature")]
	public async Task Membership_RejectsUnboundOrUnverifiedTemplate(string mismatch)
	{
		using var material = new HttpMaterial();
		var membership = new Membership(material.Authority, Account.Channel("wx-channel"), "brand1");
		context.HttpClients.PaymentHandler = (_, _) =>
		{
			var response = material.Respond(JsonSerializer.Serialize(new
			{
				card_id = mismatch == "id" ? "other" : "card1",
				appid = mismatch == "app" ? "wx-other" : "wx-channel",
				brand = new { brand_id = mismatch == "brand" ? "other" : "brand1" },
			}));
			if(mismatch == "signature")
				response.Headers.Remove("Wechatpay-Signature");

			return Task.FromResult(response);
		};

		try
		{
			if(mismatch == "signature")
				await Assert.ThrowsAsync<System.Security.Cryptography.CryptographicException>(() => membership.Templates.GetAsync("card1", TestContext.Current.CancellationToken).AsTask());
			else
				await Assert.ThrowsAsync<FormatException>(() => membership.Templates.GetAsync("card1", TestContext.Current.CancellationToken).AsTask());
		}
		finally { context.HttpClients.PaymentHandler = null; }
	}

	private static string MembershipTemplate(string brand = "brand1") => JsonSerializer.Serialize(new
	{
		card_id = "card/1",
		appid = "wx-channel",
		brand = new { brand_id = brand },
		code_mode = "MERCHANT_DEPOSIT",
	});
}
