using System;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Zongsoft.Collections;
using Xunit;

namespace Zongsoft.Externals.Wechat.Tests;

public sealed partial class AccountManagerAsyncTest
{
	[Theory]
	[InlineData(null)]
	[InlineData("synthetic-union")]
	public async Task Applet_LoginKeepsSessionKeyOnServerAndPreservesOptionalUnionId(string unionId)
	{
		var applet = new Applet(Account.Applet(NewCode(), "synthetic&secret"));
		var calls = 0;
		context.HttpClients.AccountHandler = (request, _) =>
		{
			calls++;
			Assert.Equal(HttpMethod.Get, request.Method);
			Assert.Equal("/sns/jscode2session", request.RequestUri.AbsolutePath);
			Assert.Contains("secret=synthetic%26secret", request.RequestUri.Query);
			Assert.Contains("js_code=code%26value", request.RequestUri.Query);
			return Task.FromResult(CredentialResponse(new { openid = "synthetic-open", unionid = unionId, session_key = "AAECAwQFBgcICQoLDA0ODw==" }));
		};

		var result = await applet.LoginAsync("code&value", TestContext.Current.CancellationToken);
		Assert.Equal("synthetic-open", result.OpenId);
		Assert.Equal(unionId, result.UnionId);
		Assert.Equal("AAECAwQFBgcICQoLDA0ODw==", result.Secret);
		Assert.Equal(1, calls);
		Assert.DoesNotContain(result.Secret, JsonSerializer.Serialize(result));
		Assert.DoesNotContain(result.Secret, Zongsoft.Serialization.Serializer.Json.Serialize(result));
		Assert.DoesNotContain(applet.Account.Secret, JsonSerializer.Serialize(applet.Account));
		Assert.DoesNotContain(applet.Account.Secret, Zongsoft.Serialization.Serializer.Json.Serialize(applet.Account));
		Assert.DoesNotContain(applet.Account.Secret, applet.Account.ToString());
	}

	[Theory]
	[InlineData(null, "AAECAwQFBgcICQoLDA0ODw==")]
	[InlineData("open", null)]
	[InlineData("open", "AA==")]
	public async Task Applet_LoginRejectsIncompleteProtocolIdentity(string openId, string sessionKey)
	{
		context.HttpClients.AccountHandler = (_, _) => Task.FromResult(CredentialResponse(new { openid = openId, session_key = sessionKey }));
		var applet = new Applet(Account.Applet(NewCode(), "synthetic"));
		await Assert.ThrowsAsync<FormatException>(() => applet.LoginAsync("code", TestContext.Current.CancellationToken).AsTask());
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Applet_PhoneBindsCodeToOpenIdAndValidatesApplication(bool wrongApplication)
	{
		using var state = new CredentialScope(context);
		var applet = new Applet(Account.Applet(NewCode(), "synthetic"));
		state.Cache.Put(CredentialKey(applet.Account), "synthetic-token", "revision", TimeSpan.FromMinutes(5));
		context.HttpClients.AccountHandler = async (request, cancellation) =>
		{
			Assert.Equal(HttpMethod.Post, request.Method);
			Assert.Equal("/wxa/business/getuserphonenumber", request.RequestUri.AbsolutePath);
			var body = JsonSerializer.Deserialize<JsonElement>(await request.Content.ReadAsStringAsync(cancellation));
			Assert.Equal("phone-code", body.GetProperty("code").GetString());
			Assert.Equal("synthetic-open", body.GetProperty("openid").GetString());
			return CredentialResponse(new { phone_info = new { phoneNumber = "+8613800000000", purePhoneNumber = "13800000000", countryCode = "86", watermark = new { appid = wrongApplication ? "other-app" : applet.Account.Code, timestamp = 1700000000 } } });
		};

		if(wrongApplication)
			await Assert.ThrowsAsync<FormatException>(() => applet.GetPhoneNumberAsync("phone-code", "synthetic-open", TestContext.Current.CancellationToken).AsTask());
		else
		{
			var phone = await applet.GetPhoneNumberAsync("phone-code", "synthetic-open", TestContext.Current.CancellationToken);
			Assert.Equal(new Applet.PhoneInfo("+8613800000000", "13800000000", "86"), phone);
		}
	}

	[Fact]
	public async Task Applet_AuthenticationUsesConfiguredSecretWithoutCallingOfficialAccountUserApi()
	{
		var code = NewCode();
		var provider = new ControlledProvider();
		using var scope = new ProviderScope(AccountType.Applet, provider);
		var calls = 0;
		context.HttpClients.AccountHandler = (request, _) =>
		{
			calls++;
			Assert.Equal("/sns/jscode2session", request.RequestUri.AbsolutePath);
			Assert.Contains("secret=configured", request.RequestUri.Query);
			return Task.FromResult(CredentialResponse(new { openid = "synthetic-open", session_key = "AAECAwQFBgcICQoLDA0ODw==" }));
		};
		var authenticator = new AppletAuthenticator(Zongsoft.Services.ApplicationContext.Current.Services);
		var operation = authenticator.VerifyAsync($"Applet:{code}", "code", "test", new(), TestContext.Current.CancellationToken).AsTask();
		var call = await provider.NextAsync();
		call.Completion.SetResult(Account.Applet(code, "configured"));
		var identity = await operation.WaitAsync(_timeout, TestContext.Current.CancellationToken);
		Assert.Null(identity.UnionId);
		Assert.Equal("synthetic-open", identity.OpenId);
		Assert.Equal(1, calls);
		await Assert.ThrowsAsync<ArgumentException>(() => authenticator.VerifyAsync($"Applet:{code}=injected", "code", "test", new(), TestContext.Current.CancellationToken).AsTask());
		Assert.Equal(1, calls);
	}

	[Fact]
	public async Task Applet_ProtocolErrorIsPropagatedWithoutRetry()
	{
		var calls = 0;
		context.HttpClients.AccountHandler = (_, _) =>
		{
			calls++;
			return Task.FromResult(CredentialResponse(new { errcode = 40029, errmsg = "synthetic-invalid-code" }));
		};
		var applet = new Applet(Account.Applet(NewCode(), "synthetic"));
		var error = await Assert.ThrowsAsync<Zongsoft.Common.OperationException>(() => applet.LoginAsync("invalid-code", TestContext.Current.CancellationToken).AsTask());
		Assert.Equal("40029", error.Reason);
		Assert.Equal(1, calls);
	}

	[Fact]
	public async Task Applet_CancellationInterruptsProtocolRequest()
	{
		using var cancellation = new CancellationTokenSource();
		var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		context.HttpClients.AccountHandler = async (_, token) =>
		{
			entered.SetResult();
			await Task.Delay(Timeout.Infinite, token);
			throw new InvalidOperationException();
		};
		var applet = new Applet(Account.Applet(NewCode(), "synthetic"));
		var operation = applet.LoginAsync("code", cancellation.Token).AsTask();
		await entered.Task.WaitAsync(_timeout, TestContext.Current.CancellationToken);
		cancellation.Cancel();
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation.WaitAsync(_timeout, TestContext.Current.CancellationToken));
	}

	private sealed class AppletAuthenticator(IServiceProvider services) : Security.WechatAuthenticatorBase(services)
	{
		protected override ValueTask<ClaimsIdentity> OnIssueAsync(Identity identity, string scenario, Parameters parameters, CancellationToken cancellation) => throw new NotSupportedException();
	}
}
