using System;
using System.Linq;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Externals.Wechat.Platforms;

public sealed partial class Component
{
	#region 公共方法
	public ValueTask<string> GetCredentialAsync(bool refresh = false, CancellationToken cancellation = default) => CredentialManager.GetComponentCredentialAsync(this, refresh, cancellation);

	public async ValueTask<Preauthorization> PreauthorizeAsync(CancellationToken cancellation = default)
	{
		var started = DateTimeOffset.UtcNow;
		var result = await this.SendAsync("api_create_preauthcode", new { component_appid = this.AppId }, cancellation);
		return new Preauthorization(this.AppId, Required(result, "pre_auth_code"), Expiration(result, started));
	}

	public async ValueTask<AuthorizationRequest> BeginAuthorizationAsync(string owner, string context, Uri redirectUri, AccountType type, bool mobile = false, CancellationToken cancellation = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(owner);
		ArgumentException.ThrowIfNullOrWhiteSpace(context);
		ValidateRedirect(redirectUri, type);

		if(owner.Length > 128 || context.Length > 128 || redirectUri.Query.TrimStart('?').Split('&').Any(part => Uri.UnescapeDataString(part.Split('=')[0]).Equals("state", StringComparison.OrdinalIgnoreCase)))
			throw new ArgumentException();

		var state = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
		var redirect = new UriBuilder(redirectUri) { Query = redirectUri.Query.TrimStart('?') + (redirectUri.Query.Length > 0 ? "&" : "") + "state=" + state }.Uri;
		var preauthorization = await this.PreauthorizeAsync(cancellation);
		var request = new AuthorizationRequest
		{
			State = state,
			ComponentId = this.AppId,
			Owner = owner,
			Context = context,
			AccountType = type,
			RedirectUri = redirect,
			AuthorizationUrl = preauthorization.GetUrl(redirect, type, mobile),
			Expiration = preauthorization.Expiration,
			PreauthorizationCode = preauthorization.Code,
		};

		if(!await _store.CreateAuthorizationAsync(request, cancellation))
			throw new Common.OperationException("PREAUTHORIZATION_STORE_FAILED", null);

		return request;
	}

	public async ValueTask<Authorization> AuthorizeAsync(string code, string authorizerAppId, CancellationToken cancellation = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(code);
		ArgumentException.ThrowIfNullOrWhiteSpace(authorizerAppId);
		var started = DateTimeOffset.UtcNow;
		var result = await this.SendAsync("api_query_auth", new { component_appid = this.AppId, authorization_code = code }, cancellation);

		if(!result.TryGetProperty("authorization_info", out var information) || Required(information, "authorizer_appid") != authorizerAppId)
			throw new FormatException();

		var credential = Optional(information, "authorizer_access_token");
		var renewal = Optional(information, "authorizer_refresh_token");

		if(string.IsNullOrEmpty(credential) != string.IsNullOrEmpty(renewal))
			throw new FormatException();
		if(!information.TryGetProperty("func_info", out var permissions) || permissions.ValueKind != JsonValueKind.Array)
			throw new FormatException();

		var scopes = permissions.EnumerateArray().Select(permission => permission.GetProperty("funcscope_category").GetProperty("id").GetInt32()).Distinct().ToArray();

		if(scopes.Any(scope => scope <= 0))
			throw new FormatException();

		return new Authorization
		{
			ComponentId = this.AppId,
			AppId = authorizerAppId,
			AccessToken = credential,
			RefreshToken = renewal,
			Expiration = string.IsNullOrEmpty(credential) ? null : Expiration(information, started),
			Permissions = Array.AsReadOnly(scopes),
		};
	}
	#endregion

	#region 内部方法
	internal async ValueTask<(string value, DateTimeOffset expiration)> AcquireCredentialAsync(CancellationToken cancellation)
	{
		var ticket = await _store.GetTicketAsync(this.AppId, cancellation);
		var started = DateTimeOffset.UtcNow;

		if(ticket == null || ticket.AppId != this.AppId || string.IsNullOrWhiteSpace(ticket.Value) || ticket.CreatedTime > started.AddMinutes(5) || ticket.CreatedTime.AddHours(12) <= started)
			throw new Common.OperationException("COMPONENT_TICKET_UNAVAILABLE", null);

		using var client = AccountHttpClient.Create("Wechat.Components");
		using var response = await client.PostAsJsonAsync("cgi-bin/component/api_component_token", new { component_appid = this.AppId, component_appsecret = _secret, component_verify_ticket = ticket.Value }, cancellation);
		var result = await response.GetResultAsync<JsonElement>(cancellation);
		return (Required(result, "component_access_token"), Expiration(result, started));
	}
	#endregion

	#region 私有方法
	private async ValueTask<JsonElement> SendAsync(string method, object data, CancellationToken cancellation)
	{
		var credential = await this.GetCredentialAsync(false, cancellation);
		using var client = AccountHttpClient.Create("Wechat.Components");
		using var response = await client.PostAsJsonAsync($"cgi-bin/component/{method}?access_token={Uri.EscapeDataString(credential)}", data, cancellation);
		return await response.GetResultAsync<JsonElement>(cancellation);
	}

	private static DateTimeOffset Expiration(JsonElement result, DateTimeOffset started)
	{
		if(!result.TryGetProperty("expires_in", out var property) || !property.TryGetInt32(out var seconds) || seconds <= 0)
			throw new FormatException();

		var expiration = started.AddSeconds(seconds - Math.Min(30, seconds * 0.1));

		if(expiration <= DateTimeOffset.UtcNow)
			throw new Common.OperationException("EXPIRED_COMPONENT_RESPONSE", null);

		return expiration;
	}

	private static string Required(JsonElement element, string name) => Optional(element, name) is { Length: > 0 } value && !string.IsNullOrWhiteSpace(value) ? value : throw new FormatException();
	private static string Optional(JsonElement element, string name) => element.TryGetProperty(name, out var property) && property.ValueKind != JsonValueKind.Null ? property.GetString() : null;
	#endregion
}
