using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Xunit;

namespace Zongsoft.Externals.Wechat.Tests;

public sealed partial class AccountManagerAsyncTest
{
	[Theory]
	[InlineData("snsapi_base", false)]
	[InlineData("snsapi_userinfo", true)]
	[InlineData("snsapi_base,snsapi_userinfo", true)]
	public async Task OAuth_OnlyAuthorizedScopeRequestsProfileAndSecretsStayOnServer(string scope, bool profile)
	{
		var service = new ChannelAuthentication(Account.Channel(NewCode(), "synthetic&secret"));
		var requests = new List<string>();
		context.HttpClients.AccountHandler = (request, _) =>
		{
			requests.Add(request.RequestUri.AbsolutePath);
			if(requests.Count == 1)
			{
				Assert.Contains("code=code%26value", request.RequestUri.Query);
				Assert.Contains("secret=synthetic%26secret", request.RequestUri.Query);
				return Task.FromResult(CredentialResponse(new { access_token = "synthetic-access", refresh_token = "synthetic-refresh", openid = "open", scope, expires_in = 7200 }));
			}

			Assert.Equal("/sns/userinfo", request.RequestUri.AbsolutePath);
			return Task.FromResult(CredentialResponse(new { openid = "open", unionid = "union" }));
		};

		var result = await service.AuthenticateAsync("code&value", TestContext.Current.CancellationToken);
		Assert.Equal(profile ? 2 : 1, requests.Count);
		Assert.Equal("open", result.OpenId);
		Assert.Equal(profile ? "union" : null, result.UnionId);
		Assert.Null(result.User.Nickname);
		Assert.Equal(service.Account.Code, result.AppId);
		Assert.InRange(result.Period.TotalSeconds, 7190, 7200);
		Assert.DoesNotContain(result.AccessToken, JsonSerializer.Serialize(result));
		Assert.DoesNotContain(result.RenewalToken, Zongsoft.Serialization.Serializer.Json.Serialize(result));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task OAuth_RefreshBindsAccountAndOpenId(bool wrongIdentity)
	{
		var service = new ChannelAuthentication(Account.Channel(NewCode(), "synthetic"));
		var credential = new ChannelAuthentication.Credential(service.Account.Code, "old-access", "refresh&value", "open", "union", DateTimeOffset.UtcNow, "snsapi_base", default);
		context.HttpClients.AccountHandler = (request, _) =>
		{
			Assert.Equal("/sns/oauth2/refresh_token", request.RequestUri.AbsolutePath);
			Assert.Contains("refresh_token=refresh%26value", request.RequestUri.Query);
			return Task.FromResult(CredentialResponse(new { access_token = "new-access", refresh_token = "new-refresh", openid = wrongIdentity ? "other" : "open", scope = "snsapi_base", expires_in = 3600 }));
		};

		if(wrongIdentity)
			await Assert.ThrowsAsync<FormatException>(() => service.RefreshAsync(credential, TestContext.Current.CancellationToken).AsTask());
		else
		{
			var result = await service.RefreshAsync(credential, TestContext.Current.CancellationToken);
			Assert.Equal("new-access", result.AccessToken);
			Assert.Equal("new-refresh", result.RenewalToken);
			Assert.Equal("union", result.UnionId);
		}

		var other = new ChannelAuthentication(Account.Channel(NewCode(), "synthetic"));
		await Assert.ThrowsAsync<ArgumentException>(() => other.RefreshAsync(credential, TestContext.Current.CancellationToken).AsTask());
	}

	[Fact]
	public async Task OAuth_ValidationPreservesProtocolErrors()
	{
		var service = new ChannelAuthentication(Account.Channel(NewCode(), "synthetic"));
		var credential = new ChannelAuthentication.Credential(service.Account.Code, "access", "refresh", "open", null, DateTimeOffset.UtcNow.AddMinutes(1), "snsapi_base", default);
		context.HttpClients.AccountHandler = (request, _) =>
		{
			Assert.Equal("/sns/auth", request.RequestUri.AbsolutePath);
			return Task.FromResult(CredentialResponse(new { errcode = 42001, errmsg = "synthetic-expired" }));
		};
		var error = await Assert.ThrowsAsync<Zongsoft.Common.OperationException>(() => service.ValidateAsync(credential, TestContext.Current.CancellationToken).AsTask());
		Assert.Equal("42001", error.Reason);
		await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetUserInfoAsync(credential, TestContext.Current.CancellationToken).AsTask());
	}

	[Fact]
	public async Task ChannelPostmark_UsesOriginalUrlWithoutFragment()
	{
		using var state = new CredentialScope(context);
		var channel = new Channel(Account.Channel(NewCode(), "synthetic"));
		state.Cache.Put($"Zongsoft.Wechat.Self:Channel:{channel.Account.Code}:ticket:jsapi", "ticket", "revision", TimeSpan.FromMinutes(5));
		var url = "https://Example.test/path?q=%2f&value=a+b";
		var result = await channel.PostmarkAsync(url + "#fragment", TestContext.Current.CancellationToken);
		Assert.Equal(SHA1.HashData(Encoding.UTF8.GetBytes($"jsapi_ticket=ticket&noncestr={result.nonce}&timestamp={result.timestamp}&url={url}")), result.data);
		await Assert.ThrowsAsync<ArgumentException>(() => channel.PostmarkAsync("javascript:alert(1)", TestContext.Current.CancellationToken).AsTask());
	}

	[Fact]
	public async Task ChannelMessage_EncodesTemplateValuesAndPreservesFailureWithoutRetry()
	{
		using var state = new CredentialScope(context);
		var channel = new Channel(Account.Channel(NewCode(), "synthetic"));
		state.Cache.Put(CredentialKey(channel.Account), "access&value", "revision", TimeSpan.FromMinutes(5));
		var calls = 0;
		context.HttpClients.AccountHandler = async (request, cancellation) =>
		{
			calls++;
			Assert.Equal("/cgi-bin/message/template/send", request.RequestUri.AbsolutePath);
			Assert.Contains("access_token=access%26value", request.RequestUri.Query);
			var body = JsonSerializer.Deserialize<JsonElement>(await request.Content.ReadAsStringAsync(cancellation));
			Assert.Equal("open", body.GetProperty("touser").GetString());
			Assert.Equal("template", body.GetProperty("template_id").GetString());
			Assert.Equal(12.34m, body.GetProperty("data").GetProperty("amount").GetProperty("value").GetDecimal());
			Assert.Equal("text", body.GetProperty("data").GetProperty("thing").GetProperty("value").GetString());
			Assert.False(body.TryGetProperty("topcolor", out _));
			return calls == 1 ? CredentialResponse(new { errcode = 0, msgid = 9007199254740993L }) : CredentialResponse(new { errcode = 47003, errmsg = "synthetic-invalid-field" });
		};
		var data = new Dictionary<string, object> { ["amount"] = 12.34m, ["thing"] = JsonSerializer.SerializeToElement(new { value = "text" }) };
		Assert.Equal("9007199254740993", await channel.Messager.SendAsync("open", "template", data, TestContext.Current.CancellationToken));
		var error = await Assert.ThrowsAsync<Zongsoft.Common.OperationException>(() => channel.Messager.SendAsync("open", "template", data, TestContext.Current.CancellationToken).AsTask());
		Assert.Equal("47003", error.Reason);
		Assert.Equal(2, calls);
	}
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task OAuth_ProfileMustMatchOpenIdAndMissingUnionRemainsNull(bool mismatch)
	{
		var service = new ChannelAuthentication(Account.Channel(NewCode(), "synthetic"));
		context.HttpClients.AccountHandler = (request, _) => Task.FromResult(request.RequestUri.AbsolutePath == "/sns/oauth2/access_token" ?
			CredentialResponse(new { access_token = "access", refresh_token = "refresh", openid = "open", scope = "snsapi_userinfo", expires_in = 7200 }) :
			CredentialResponse(new { openid = mismatch ? "other" : "open" }));

		if(mismatch)
			await Assert.ThrowsAsync<FormatException>(() => service.AuthenticateAsync("code", TestContext.Current.CancellationToken).AsTask());
		else
		{
			var result = await service.AuthenticateAsync("code", TestContext.Current.CancellationToken);
			Assert.Null(result.UnionId);
			Assert.Null(result.User.UnionId);
			Assert.Equal("open", result.User.OpenId);
		}
	}

	[Fact]
	public async Task ChannelUsers_PreserveEmptyPagesAndEscapeCursor()
	{
		using var state = new CredentialScope(context);
		var channel = new Channel(Account.Channel(NewCode(), "synthetic"));
		state.Cache.Put(CredentialKey(channel.Account), "token", "revision", TimeSpan.FromMinutes(5));
		context.HttpClients.AccountHandler = (request, _) =>
		{
			Assert.Equal("/cgi-bin/user/get", request.RequestUri.AbsolutePath);
			Assert.Contains("next_openid=cursor%26value", request.RequestUri.Query);
			return Task.FromResult(CredentialResponse(new { total = 0, count = 0, next_openid = "" }));
		};
		var result = await channel.Users.GetIdentifiersAsync("cursor&value", TestContext.Current.CancellationToken);
		Assert.Empty(result.identifiers);
		Assert.Equal("", result.bookmark);
	}

	[Fact]
	public async Task Transmitter_AwaitsNamedChannelAndUsesTemplateMessageContract()
	{
		using var state = new CredentialScope(context);
		var code = NewCode();
		var account = Account.Channel(code, "synthetic");
		var provider = new ControlledProvider();
		using var scope = new ProviderScope(AccountType.Channel, provider);
		state.Cache.Put(CredentialKey(account), "token", "revision", TimeSpan.FromMinutes(5));
		var calls = 0;
		context.HttpClients.AccountHandler = async (request, cancellation) =>
		{
			calls++;
			var body = JsonSerializer.Deserialize<JsonElement>(await request.Content.ReadAsStringAsync(cancellation));
			Assert.Equal("open", body.GetProperty("touser").GetString());
			Assert.Equal("template", body.GetProperty("template_id").GetString());
			return CredentialResponse(new { errcode = 0, msgid = 123L });
		};
		Zongsoft.Communication.ITransmitter transmitter = new Transmitter();
		var operation = transmitter.TransmitAsync(code + ":open", "Message", "template", new { thing = "value" }, TestContext.Current.CancellationToken).AsTask();
		var lookup = await provider.NextAsync();
		Assert.False(operation.IsCompleted);
		Assert.Equal(0, calls);
		lookup.Completion.SetResult(account);
		await operation.WaitAsync(_timeout, TestContext.Current.CancellationToken);
		Assert.Equal(1, calls);
		await Assert.ThrowsAsync<ArgumentException>(() => transmitter.TransmitAsync(code + ":open", "Unknown", "template", new { thing = "value" }, TestContext.Current.CancellationToken).AsTask());
		Assert.Equal(1, calls);
	}

}
