using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Zongsoft.Externals.Wechat.Platforms;
using Xunit;

namespace Zongsoft.Externals.Wechat.Tests;

public sealed partial class AccountManagerAsyncTest
{
	#region 测试方法
	[Fact]
	public async Task Authorizers_ConcurrentRefreshPersistsOneCredentialAndDoesNotUseOwnedCache()
	{
		using var cache = new CredentialScope(context);
		var store = new ComponentStore();
		var component = CreateComponent(store);
		store.Authorization = Grant(component.AppId, "authorizer", AccountType.Channel, [2, 4, 7], false);
		cache.Cache.Put($"Zongsoft.Wechat.Component:{component.AppId}:access-token", "component&token", "revision", TimeSpan.FromMinutes(5));
		cache.Cache.Put(CredentialKey(Account.Channel("authorizer")), "owned-token", "revision", TimeSpan.FromMinutes(5));
		var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var calls = 0;
		context.HttpClients.ComponentHandler = async (request, cancellation) =>
		{
			Interlocked.Increment(ref calls);
			Assert.EndsWith("api_authorizer_token?access_token=component%26token", request.RequestUri.AbsoluteUri, StringComparison.Ordinal);
			var body = JsonSerializer.Deserialize<JsonElement>(await request.Content.ReadAsStringAsync(cancellation));
			Assert.Equal(component.AppId, body.GetProperty("component_appid").GetString());
			Assert.Equal("authorizer", body.GetProperty("authorizer_appid").GetString());
			Assert.Equal("refresh-original", body.GetProperty("authorizer_refresh_token").GetString());
			entered.SetResult();
			await release.Task.WaitAsync(cancellation);
			return CredentialResponse(new { authorizer_access_token = "delegated-token", authorizer_refresh_token = "refresh-rotated", expires_in = 7200 });
		};
		var account = await component.GetAccountAsync("authorizer", TestContext.Current.CancellationToken);
		var requests = Enumerable.Range(0, 12).Select(_ => account.GetCredentialAsync(false, TestContext.Current.CancellationToken).AsTask()).ToArray();
		await entered.Task.WaitAsync(_timeout, TestContext.Current.CancellationToken);
		release.SetResult();
		Assert.All(await Task.WhenAll(requests), value => Assert.Equal("delegated-token", value));
		Assert.Equal(1, calls);
		Assert.Equal(1, store.CredentialWrites);
		Assert.Equal("refresh-rotated", store.Authorization.Credential.RefreshToken);
		Assert.Equal(0, cache.Cache.Writes);
		Assert.NotEqual(Account.Channel("authorizer"), account);
		Assert.Equal(component.AppId, account.ComponentId);
		Assert.Null(account.Secret);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Authorizers_RevocationOrNewGrantWinsAgainstInflightRefresh(bool renewed)
	{
		using var cache = new CredentialScope(context);
		var store = new ComponentStore();
		var component = CreateComponent(store);
		store.Authorization = Grant(component.AppId, "authorizer", AccountType.Channel, [4], false);
		cache.Cache.Put($"Zongsoft.Wechat.Component:{component.AppId}:access-token", "component-token", "revision", TimeSpan.FromMinutes(5));
		context.HttpClients.ComponentHandler = (_, _) => Task.FromResult(CredentialResponse(new { authorizer_access_token = "late-token", authorizer_refresh_token = "late-refresh", expires_in = 7200 }));
		var account = await component.GetAccountAsync("authorizer", TestContext.Current.CancellationToken);
		store.BeforeCredentialUpdate = () => store.Authorization = Grant(component.AppId, "authorizer", AccountType.Channel, [4], true, renewed);
		await Assert.ThrowsAsync<Common.OperationException>(() => account.GetCredentialAsync(false, TestContext.Current.CancellationToken).AsTask());
		Assert.Equal(0, store.CredentialWrites);
		Assert.NotEqual("late-token", store.Authorization.Credential.Value);
		Assert.Equal(renewed, store.Authorization.Enabled);
	}

	[Fact]
	public async Task Authorizers_PersistsRefreshBeforeObservingLateCallerCancellation()
	{
		using var cache = new CredentialScope(context);
		using var cancellation = new CancellationTokenSource();
		var store = new ComponentStore();
		var component = CreateComponent(store);
		store.Authorization = Grant(component.AppId, "authorizer", AccountType.Channel, [4], false);
		cache.Cache.Put($"Zongsoft.Wechat.Component:{component.AppId}:access-token", "component-token", "revision", TimeSpan.FromMinutes(5));
		context.HttpClients.ComponentHandler = (_, _) => Task.FromResult(CredentialResponse(new { authorizer_access_token = "fresh-token", authorizer_refresh_token = "fresh-refresh", expires_in = 7200 }));
		store.BeforeCredentialUpdate = cancellation.Cancel;
		var account = await component.GetAccountAsync("authorizer", TestContext.Current.CancellationToken);
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => account.GetCredentialAsync(false, cancellation.Token).AsTask());
		Assert.Equal(1, store.CredentialWrites);
		Assert.Equal("fresh-refresh", store.Authorization.Credential.RefreshToken);
		Assert.False(store.UpdateCancellation.CanBeCanceled);
		Assert.Equal("fresh-token", await account.GetCredentialAsync(false, TestContext.Current.CancellationToken));
	}

	[Fact]
	public async Task Authorizers_RejectsMissingPermissionWrongTypeAndCrossComponentState()
	{
		var store = new ComponentStore();
		var component = CreateComponent(store);
		store.Authorization = Grant(component.AppId, "authorizer", AccountType.Channel, [4]);
		var channel = await component.GetChannelAsync("authorizer", TestContext.Current.CancellationToken);
		context.HttpClients.AccountHandler = (_, _) => throw new Xunit.Sdk.XunitException("Unexpected request");
		var error = await Assert.ThrowsAsync<Common.OperationException>(() => channel.Users.GetInfoAsync("open", TestContext.Current.CancellationToken).AsTask());
		Assert.Equal("AUTHORIZATION_PERMISSION_DENIED", error.Reason);
		await Assert.ThrowsAsync<Common.OperationException>(() => channel.Messager.GetTemplatesAsync(TestContext.Current.CancellationToken).AsTask());
		await Assert.ThrowsAsync<Common.OperationException>(() => component.GetAppletAsync("authorizer", TestContext.Current.CancellationToken).AsTask());
		store.Authorization = Grant("other-component", "authorizer", AccountType.Channel, [4]);
		await Assert.ThrowsAsync<Common.OperationException>(() => channel.GetCredentialAsync(false, TestContext.Current.CancellationToken).AsTask());
		store.Authorization = Grant(component.AppId, "other-authorizer", AccountType.Channel, [4]);
		await Assert.ThrowsAsync<Common.OperationException>(() => channel.GetCredentialAsync(false, TestContext.Current.CancellationToken).AsTask());
	}

	[Fact]
	public async Task Authorizers_AppletUsesDelegatedLoginAndPhoneProtocols()
	{
		using var cache = new CredentialScope(context);
		var store = new ComponentStore();
		var component = CreateComponent(store);
		store.Authorization = Grant(component.AppId, "authorizer", AccountType.Applet, [18]);
		cache.Cache.Put($"Zongsoft.Wechat.Component:{component.AppId}:access-token", "component&token", "revision", TimeSpan.FromMinutes(5));
		var calls = 0;
		context.HttpClients.AccountHandler = async (request, cancellation) =>
		{
			calls++;

			if(request.RequestUri.AbsolutePath == "/sns/component/jscode2session")
			{
				Assert.Contains("component_access_token=component%26token", request.RequestUri.Query, StringComparison.Ordinal);
				Assert.Contains("js_code=login%26code", request.RequestUri.Query, StringComparison.Ordinal);
				Assert.DoesNotContain("secret=", request.RequestUri.Query, StringComparison.Ordinal);
				return CredentialResponse(new { openid = "open", session_key = Convert.ToBase64String(new byte[16]) });
			}

			Assert.Equal("/wxa/business/getuserphonenumber", request.RequestUri.AbsolutePath);
			Assert.Equal("?access_token=current-token", request.RequestUri.Query);
			var body = JsonSerializer.Deserialize<JsonElement>(await request.Content.ReadAsStringAsync(cancellation));
			Assert.Equal("open", body.GetProperty("openid").GetString());
			return CredentialResponse(new { phone_info = new { phoneNumber = "123", purePhoneNumber = "123", countryCode = "86", watermark = new { appid = "authorizer", timestamp = 1 } } });
		};
		var applet = await component.GetAppletAsync("authorizer", TestContext.Current.CancellationToken);
		Assert.Equal("open", (await applet.LoginAsync("login&code", TestContext.Current.CancellationToken)).OpenId);
		Assert.Equal("123", (await applet.GetPhoneNumberAsync("phone-code", "open", TestContext.Current.CancellationToken)).PhoneNumber);
		Assert.Equal(2, calls);
		store.Authorization = Grant(component.AppId, "authorizer", AccountType.Applet, [18], enabled: false);
		await Assert.ThrowsAsync<Common.OperationException>(() => applet.LoginAsync("other", TestContext.Current.CancellationToken).AsTask());
		Assert.Equal(2, calls);
	}

	[Fact]
	public async Task Authorizers_ChannelOAuthBindsCredentialsToComponentAndUsesDelegatedEndpoints()
	{
		using var cache = new CredentialScope(context);
		var store = new ComponentStore();
		var component = CreateComponent(store);
		store.Authorization = Grant(component.AppId, "authorizer", AccountType.Channel, [4]);
		cache.Cache.Put($"Zongsoft.Wechat.Component:{component.AppId}:access-token", "component&token", "revision", TimeSpan.FromMinutes(5));
		var paths = new System.Collections.Generic.List<string>();
		context.HttpClients.AccountHandler = (request, _) =>
		{
			paths.Add(request.RequestUri.AbsolutePath);
			Assert.Contains("component_access_token=component%26token", request.RequestUri.Query, StringComparison.Ordinal);
			Assert.DoesNotContain("secret=", request.RequestUri.Query, StringComparison.Ordinal);
			return Task.FromResult(CredentialResponse(new { access_token = "oauth-token", refresh_token = "oauth-refresh", openid = "open", expires_in = 7200, scope = "snsapi_base" }));
		};
		var account = await component.GetAccountAsync("authorizer", TestContext.Current.CancellationToken);
		var oauth = new ChannelAuthentication(account);
		var credential = await oauth.AuthenticateAsync("code", TestContext.Current.CancellationToken);
		Assert.Equal(component.AppId, credential.ComponentId);
		Assert.Equal(component.AppId, (await oauth.RefreshAsync(credential, TestContext.Current.CancellationToken)).ComponentId);
		Assert.Equal(["/sns/oauth2/component/access_token", "/sns/oauth2/component/refresh_token"], paths);
		await Assert.ThrowsAsync<ArgumentException>(() => new ChannelAuthentication(Account.Channel("authorizer", "own-secret")).ValidateAsync(credential, TestContext.Current.CancellationToken).AsTask());
	}

	[Fact]
	public async Task Authorizers_TicketsSeparateComponentsAndGrantsAndRejectRevokedCache()
	{
		using var cache = new CredentialScope(context);
		var firstStore = new ComponentStore();
		var secondStore = new ComponentStore();
		var first = CreateComponent(firstStore);
		var second = CreateComponent(secondStore);
		firstStore.Authorization = Grant(first.AppId, "same-authorizer", AccountType.Channel, [4, 8]);
		secondStore.Authorization = Grant(second.AppId, "same-authorizer", AccountType.Channel, [4]);
		var calls = 0;
		context.HttpClients.AccountHandler = (_, _) => Task.FromResult(CredentialResponse(new { ticket = $"ticket-{++calls}", expires_in = 7200 }));
		var account = await first.GetAccountAsync("same-authorizer", TestContext.Current.CancellationToken);
		var other = await second.GetAccountAsync("same-authorizer", TestContext.Current.CancellationToken);
		Assert.Equal("ticket-1", (await account.GetTicketAsync("jsapi", false, TestContext.Current.CancellationToken)).ticket);
		Assert.Equal("ticket-1", (await account.GetTicketAsync("jsapi", false, TestContext.Current.CancellationToken)).ticket);
		Assert.Equal("ticket-2", (await other.GetTicketAsync("jsapi", false, TestContext.Current.CancellationToken)).ticket);
		Assert.Equal("ticket-3", (await account.GetTicketAsync("wx_card", false, TestContext.Current.CancellationToken)).ticket);
		firstStore.Authorization = Grant(first.AppId, "same-authorizer", AccountType.Channel, [4]);
		Assert.Equal("ticket-4", (await account.GetTicketAsync("jsapi", false, TestContext.Current.CancellationToken)).ticket);
		firstStore.Authorization = Grant(first.AppId, "same-authorizer", AccountType.Channel, [4], enabled: false);
		await Assert.ThrowsAsync<Common.OperationException>(() => account.GetTicketAsync("jsapi", false, TestContext.Current.CancellationToken).AsTask());
		Assert.Equal(4, calls);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Authorizers_RefreshFailsWhenLeaseOrPersistenceIsLost(bool persistence)
	{
		using var cache = new CredentialScope(context);
		var store = new ComponentStore { RejectCredentialUpdate = persistence };
		var component = CreateComponent(store);
		store.Authorization = Grant(component.AppId, "authorizer", AccountType.Channel, [4], false);
		cache.Cache.Put($"Zongsoft.Wechat.Component:{component.AppId}:access-token", "component-token", "revision", TimeSpan.FromMinutes(5));
		context.HttpClients.ComponentHandler = (_, _) =>
		{
			if(!persistence)
				cache.Locker.Lease.Locked = false;

			return Task.FromResult(CredentialResponse(new { authorizer_access_token = "late-token", authorizer_refresh_token = "late-refresh", expires_in = 7200 }));
		};
		var account = await component.GetAccountAsync("authorizer", TestContext.Current.CancellationToken);
		var error = await Assert.ThrowsAsync<Common.OperationException>(() => account.GetCredentialAsync(false, TestContext.Current.CancellationToken).AsTask());
		Assert.Equal(persistence ? "AUTHORIZATION_CREDENTIAL_STORE_FAILED" : "CREDENTIAL_LOCK_LOST", error.Reason);
		Assert.Equal(0, store.CredentialWrites);
		Assert.Equal("refresh-original", store.Authorization.Credential.RefreshToken);
	}

	[Theory]
	[InlineData(false, 2, 1, AccountType.Channel)]
	[InlineData(true, 0, 1, AccountType.Applet)]
	[InlineData(true, 12, 19, AccountType.Applet)]
	public async Task Authorizers_InformationUsesActualTypeStatusAndPermissions(bool applet, int service, int status, AccountType type)
	{
		using var cache = new CredentialScope(context);
		var component = CreateComponent(new ComponentStore());
		cache.Cache.Put($"Zongsoft.Wechat.Component:{component.AppId}:access-token", "component-token", "revision", TimeSpan.FromMinutes(5));
		context.HttpClients.ComponentHandler = (_, _) => Task.FromResult(CredentialResponse(new
		{
			authorizer_info = new { nick_name = "example", service_type_info = new { id = service }, account_status = status, MiniProgramInfo = applet ? new object() : null },
			authorization_info = new { authorizer_appid = "authorizer", func_info = new[] { new { funcscope_category = new { id = 4 } }, new { funcscope_category = new { id = 999 } } } },
		}));
		var info = await component.GetAuthorizerInfoAsync("authorizer", TestContext.Current.CancellationToken);
		Assert.Equal(type, info.AccountType);
		Assert.Equal(status, info.Status);
		Assert.Equal([4, 999], info.Permissions);
		await Assert.ThrowsAsync<FormatException>(() => component.GetAuthorizerInfoAsync("other", TestContext.Current.CancellationToken).AsTask());
	}
	#endregion

	#region 辅助方法
	private static Component.AuthorizationState Grant(string componentId, string authorizerAppId, AccountType type, int[] permissions, bool fresh = true, bool enabled = true) => new()
	{
		ComponentId = componentId,
		AppId = authorizerAppId,
		AccountType = type,
		Permissions = Array.AsReadOnly(permissions),
		Enabled = enabled,
		GrantId = Guid.NewGuid().ToString("N"),
		Revision = Guid.NewGuid().ToString("N"),
		Credential = new() { Value = "current-token", RefreshToken = "refresh-original", Expiration = DateTimeOffset.UtcNow.AddMinutes(fresh ? 10 : -10) },
	};
	#endregion

	#region 嵌套类型
	private sealed partial class ComponentStore
	{
		public Component.AuthorizationState Authorization { get; set; }
		public int CredentialWrites { get; private set; }
		public bool RejectCredentialUpdate { get; set; }
		public Action BeforeCredentialUpdate { get; set; }
		public CancellationToken UpdateCancellation { get; private set; }
		public ValueTask<Component.AuthorizationState> GetAuthorizationAsync(string componentId, string authorizerAppId, CancellationToken cancellation = default) => ValueTask.FromResult(this.Authorization);
		public ValueTask<bool> UpdateCredentialAsync(string componentId, string authorizerAppId, string revision, Component.AuthorizationCredential credential, CancellationToken cancellation = default)
		{
			this.BeforeCredentialUpdate?.Invoke();
			this.UpdateCancellation = cancellation;
			var current = this.Authorization;

			if(this.RejectCredentialUpdate || current == null || !current.Enabled || current.ComponentId != componentId || current.AppId != authorizerAppId || current.Revision != revision)
				return ValueTask.FromResult(false);

			this.Authorization = new()
			{
				ComponentId = current.ComponentId,
				AppId = current.AppId,
				AccountType = current.AccountType,
				Permissions = current.Permissions,
				Enabled = current.Enabled,
				GrantId = current.GrantId,
				Revision = Guid.NewGuid().ToString("N"),
				Credential = credential,
			};
			this.CredentialWrites++;
			return ValueTask.FromResult(true);
		}
	}
	#endregion
}
