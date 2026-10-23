using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Zongsoft.Externals.Wechat.Platforms;
using Xunit;

namespace Zongsoft.Externals.Wechat.Tests;

public sealed partial class AccountManagerAsyncTest
{
	#region 测试方法
	[Theory]
	[InlineData("component_verify_ticket", Component.NotificationKind.VerificationTicket)]
	[InlineData("authorized", Component.NotificationKind.Authorized)]
	[InlineData("updateauthorized", Component.NotificationKind.Updated)]
	[InlineData("unauthorized", Component.NotificationKind.Unauthorized)]
	public async Task Components_ReceivesVerifiedEventsAndPreservesReliableResult(string type, Component.NotificationKind kind)
	{
		var store = new ComponentStore();
		var component = CreateComponent(store);
		var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
		var xml = $"<xml><AppId>{component.AppId}</AppId><CreateTime>{now}</CreateTime><InfoType>{type}</InfoType><ComponentVerifyTicket>ticket</ComponentVerifyTicket><AuthorizerAppid>authorizer</AuthorizerAppid><AuthorizationCode>code</AuthorizationCode><AuthorizationCodeExpiredTime>{now + 600}</AuthorizationCodeExpiredTime><PreAuthCode>pre-code</PreAuthCode></xml>";
		Assert.Equal(Component.Receipt.Accepted, await Deliver(component, xml));
		Assert.Equal(kind, store.Notification.Kind);
		Assert.Equal(component.AppId, store.Notification.AppId);
		var id = store.Notification.Id;
		store.Receipt = Component.Receipt.Duplicate;
		Assert.Equal(Component.Receipt.Duplicate, await Deliver(component, xml));
		Assert.Equal(id, store.Notification.Id);
		store.Receipt = Component.Receipt.Rejected;
		Assert.Equal(Component.Receipt.Rejected, await Deliver(component, xml));
		store.Failure = true;
		await Assert.ThrowsAsync<InvalidOperationException>(() => Deliver(component, xml));
		Assert.DoesNotContain("ticket", JsonSerializer.Serialize(store.Notification), StringComparison.Ordinal);
		Assert.DoesNotContain("pre-code", JsonSerializer.Serialize(store.Notification), StringComparison.Ordinal);
	}

	[Theory]
	[InlineData("signature")]
	[InlineData("time")]
	[InlineData("receiver")]
	[InlineData("inner")]
	[InlineData("duplicate")]
	[InlineData("dtd")]
	[InlineData("length")]
	public async Task Components_RejectsUntrustedEventsBeforeStorage(string invalid)
	{
		var store = new ComponentStore();
		var component = CreateComponent(store);
		var time = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
		var plaintext = $"<xml><AppId>{(invalid == "inner" ? "wrong" : component.AppId)}</AppId><CreateTime>{time}</CreateTime><InfoType>component_verify_ticket</InfoType><ComponentVerifyTicket>ticket</ComponentVerifyTicket>{(invalid == "duplicate" ? "<AppId>duplicate</AppId>" : "")}</xml>";

		if(invalid == "dtd")
			plaintext = "<!DOCTYPE xml [<!ENTITY external SYSTEM 'file:///unreachable'>]>" + plaintext;

		var ciphertext = CryptographyUtility.Encrypt(plaintext, new string('A', 43), invalid == "receiver" ? "wrong" : component.AppId);
		var timestamp = (invalid == "time" ? time - 301 : time).ToString(System.Globalization.CultureInfo.InvariantCulture);
		var signature = CryptographyUtility.Sign("synthetic-token", timestamp, "nonce", ciphertext);
		var body = $"<xml><Encrypt>{ciphertext}</Encrypt></xml>";

		if(invalid == "length")
			body = new string('x', 256 * 1024 + 1);

		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(body));
		var exception = await Record.ExceptionAsync(() => component.ReceiveAsync(stream, timestamp, "nonce", invalid == "signature" ? new string('0', 40) : signature, TestContext.Current.CancellationToken).AsTask());
		Assert.IsType(invalid switch { "duplicate" or "length" => typeof(FormatException), "dtd" => typeof(System.Xml.XmlException), _ => typeof(CryptographicException) }, exception);
		Assert.Null(store.Notification);
	}

	[Fact]
	public async Task Components_CoordinatesTokensWithoutUsingOwnedAccountCache()
	{
		using var cache = new CredentialScope(context);
		var store = new ComponentStore();
		var component = CreateComponent(store);
		store.Ticket = new() { AppId = component.AppId, CreatedTime = DateTimeOffset.UtcNow, Value = "verify-ticket" };
		cache.Cache.Put(CredentialKey(Account.Channel(component.AppId)), "owned-token", "owned-revision", TimeSpan.FromMinutes(5));
		var calls = 0;
		context.HttpClients.ComponentHandler = async (request, cancellation) =>
		{
			Interlocked.Increment(ref calls);
			Assert.Equal("https://api.weixin.qq.com/cgi-bin/component/api_component_token", request.RequestUri.AbsoluteUri);
			var body = JsonSerializer.Deserialize<JsonElement>(await request.Content.ReadAsStringAsync(cancellation));
			Assert.Equal(component.AppId, body.GetProperty("component_appid").GetString());
			Assert.Equal("verify-ticket", body.GetProperty("component_verify_ticket").GetString());
			return CredentialResponse(new { component_access_token = "component&token", expires_in = 120 });
		};
		var tokens = await Task.WhenAll(Enumerable.Range(0, 12).Select(_ => component.GetCredentialAsync(cancellation: TestContext.Current.CancellationToken).AsTask()));
		Assert.All(tokens, token => Assert.Equal("component&token", token));
		Assert.Equal(1, calls);
		Assert.Equal(1, cache.Cache.Writes);
	}

	[Fact]
	public async Task Components_PreauthorizationAndExchangeUseExplicitComponentCredentials()
	{
		using var cache = new CredentialScope(context);
		var store = new ComponentStore();
		var component = CreateComponent(store);
		cache.Cache.Put($"Zongsoft.Wechat.Component:{component.AppId}:access-token", "component&token", "revision", TimeSpan.FromMinutes(5));
		context.HttpClients.ComponentHandler = async (request, cancellation) =>
		{
			Assert.Equal("?access_token=component%26token", request.RequestUri.Query);
			var body = JsonSerializer.Deserialize<JsonElement>(await request.Content.ReadAsStringAsync(cancellation));
			Assert.Equal(component.AppId, body.GetProperty("component_appid").GetString());

			if(request.RequestUri.AbsolutePath.EndsWith("api_create_preauthcode", StringComparison.Ordinal))
				return CredentialResponse(new { pre_auth_code = "pre&code", expires_in = 600 });

			Assert.EndsWith("api_query_auth", request.RequestUri.AbsolutePath, StringComparison.Ordinal);
			Assert.Equal("authorization-code", body.GetProperty("authorization_code").GetString());
			return CredentialResponse(new { authorization_info = new { authorizer_appid = "authorizer", authorizer_access_token = "secret-token", authorizer_refresh_token = "secret-renewal", expires_in = 7200, func_info = new[] { new { funcscope_category = new { id = 1 } }, new { funcscope_category = new { id = 4 } } } } });
		};
		var preauthorization = await component.PreauthorizeAsync(TestContext.Current.CancellationToken);
		Assert.InRange((preauthorization.Expiration - DateTimeOffset.UtcNow).TotalSeconds, 560, 570);
		var url = preauthorization.GetUrl(new Uri("https://callback.example.invalid/authorization?state=opaque"), AccountType.Applet, true).AbsoluteUri;
		Assert.StartsWith("https://open.weixin.qq.com/wxaopen/safe/bindcomponent?", url, StringComparison.Ordinal);
		Assert.Contains("pre_auth_code=pre%26code", url, StringComparison.Ordinal);
		Assert.Contains("auth_type=2", url, StringComparison.Ordinal);
		var authorization = await component.AuthorizeAsync("authorization-code", "authorizer", TestContext.Current.CancellationToken);
		Assert.Equal([1, 4], authorization.Permissions);
		Assert.Equal("secret-renewal", authorization.RefreshToken);
		Assert.DoesNotContain("secret-", JsonSerializer.Serialize(authorization), StringComparison.Ordinal);
		await Assert.ThrowsAsync<FormatException>(() => component.AuthorizeAsync("authorization-code", "other", TestContext.Current.CancellationToken).AsTask());
		Assert.Throws<ArgumentException>(() => preauthorization.GetUrl(new Uri("http://callback.example.invalid/"), AccountType.Channel));
		Assert.Throws<Common.OperationException>(() => new Component.Preauthorization(component.AppId, "expired", DateTimeOffset.UtcNow.AddSeconds(-1)).GetUrl(new Uri("https://callback.example.invalid/"), AccountType.Channel));
	}

	[Fact]
	public async Task Components_BeginAuthorizationPersistsOwnerContextAndUnpredictableState()
	{
		using var cache = new CredentialScope(context);
		var store = new ComponentStore();
		var component = CreateComponent(store);
		cache.Cache.Put($"Zongsoft.Wechat.Component:{component.AppId}:access-token", "token", "revision", TimeSpan.FromMinutes(5));
		context.HttpClients.ComponentHandler = (_, _) => Task.FromResult(CredentialResponse(new { pre_auth_code = "pre-code", expires_in = 600 }));
		var request = await component.BeginAuthorizationAsync("owner", "tenant", new Uri("https://callback.example.invalid/return?existing=1"), AccountType.Channel, cancellation: TestContext.Current.CancellationToken);
		Assert.Same(request, store.Request);
		Assert.Equal("owner", request.Owner);
		Assert.Equal("tenant", request.Context);
		Assert.Equal("pre-code", request.PreauthorizationCode);
		Assert.Equal(64, request.State.Length);
		Assert.Equal("?existing=1&state=" + request.State, request.RedirectUri.Query);
		Assert.Contains("auth_type=1", request.AuthorizationUrl.AbsoluteUri, StringComparison.Ordinal);
		store.AcceptCreation = false;
		var error = await Assert.ThrowsAsync<Common.OperationException>(() => component.BeginAuthorizationAsync("owner", "tenant", new Uri("https://callback.example.invalid/return"), AccountType.Channel, cancellation: TestContext.Current.CancellationToken).AsTask());
		Assert.Equal("PREAUTHORIZATION_STORE_FAILED", error.Reason);
		Assert.NotEqual(request.State, store.Request.State);
		await Assert.ThrowsAsync<ArgumentException>(() => component.BeginAuthorizationAsync("owner", "tenant", new Uri("https://callback.example.invalid/?state=injected"), AccountType.Channel, cancellation: TestContext.Current.CancellationToken).AsTask());
	}

	[Fact]
	public async Task Components_ExpiredTicketDoesNotContactWechat()
	{
		using var cache = new CredentialScope(context);
		var store = new ComponentStore();
		var component = CreateComponent(store);
		store.Ticket = new() { AppId = component.AppId, CreatedTime = DateTimeOffset.UtcNow.AddHours(-12), Value = "expired" };
		context.HttpClients.ComponentHandler = (_, _) => throw new Xunit.Sdk.XunitException("Unexpected request");
		var error = await Assert.ThrowsAsync<Common.OperationException>(() => component.GetCredentialAsync(cancellation: TestContext.Current.CancellationToken).AsTask());
		Assert.Equal("COMPONENT_TICKET_UNAVAILABLE", error.Reason);
	}
	#endregion

	#region 辅助方法
	private static Component CreateComponent(ComponentStore store) => new(new Options.ComponentOptions { Name = NewCode(), Secret = "synthetic-secret", Token = "synthetic-token", EncodingAESKey = new string('A', 43) }, store);

	private static async Task<Component.Receipt> Deliver(Component component, string plaintext)
	{
		var ciphertext = CryptographyUtility.Encrypt(plaintext, new string('A', 43), component.AppId);
		var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture);
		using var body = new MemoryStream(Encoding.UTF8.GetBytes($"<xml><Encrypt>{ciphertext}</Encrypt></xml>"));
		return await component.ReceiveAsync(body, timestamp, "nonce", CryptographyUtility.Sign("synthetic-token", timestamp, "nonce", ciphertext), TestContext.Current.CancellationToken);
	}
	#endregion

	#region 嵌套类型
	private sealed partial class ComponentStore : IComponentStore, IComponentAuthorizationStore
	{
		public Component.Notification Notification { get; private set; }
		public Component.VerificationTicket Ticket { get; set; }
		public Component.Receipt Receipt { get; set; } = Component.Receipt.Accepted;
		public bool Failure { get; set; }
		public Component.AuthorizationRequest Request { get; private set; }
		public bool AcceptCreation { get; set; } = true;
		public ValueTask<bool> CreateAuthorizationAsync(Component.AuthorizationRequest request, CancellationToken cancellation = default)
		{
			this.Request = request;
			return ValueTask.FromResult(this.AcceptCreation);
		}
		public ValueTask<Component.VerificationTicket> GetTicketAsync(string componentId, CancellationToken cancellation = default) => ValueTask.FromResult(this.Ticket);
		public ValueTask<Component.Receipt> ReceiveAsync(Component.Notification notification, CancellationToken cancellation = default)
		{
			this.Notification = notification;
			return this.Failure ? ValueTask.FromException<Component.Receipt>(new InvalidOperationException()) : ValueTask.FromResult(this.Receipt);
		}
	}
	#endregion
}
