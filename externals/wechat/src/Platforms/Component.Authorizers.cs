using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Externals.Wechat.Platforms;

public sealed partial class Component
{
	#region 公共方法
	public async ValueTask<Account> GetAccountAsync(string authorizerAppId, CancellationToken cancellation = default)
	{
		var authorization = await this.GetAuthorizationAsync(authorizerAppId, null, 0, cancellation);
		return Account.Authorizer(this, authorization.AccountType, authorization.AppId);
	}

	public async ValueTask<Applet> GetAppletAsync(string authorizerAppId, CancellationToken cancellation = default)
	{
		var authorization = await this.GetAuthorizationAsync(authorizerAppId, AccountType.Applet, 0, cancellation);
		return new Applet(Account.Authorizer(this, authorization.AccountType, authorization.AppId));
	}

	public async ValueTask<Channel> GetChannelAsync(string authorizerAppId, CancellationToken cancellation = default)
	{
		var authorization = await this.GetAuthorizationAsync(authorizerAppId, AccountType.Channel, 0, cancellation);
		return new Channel(Account.Authorizer(this, authorization.AccountType, authorization.AppId));
	}

	public async ValueTask<AuthorizerInfo> GetAuthorizerInfoAsync(string authorizerAppId, CancellationToken cancellation = default)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(authorizerAppId);
		var result = await this.SendAsync("api_get_authorizer_info", new { component_appid = this.AppId, authorizer_appid = authorizerAppId }, cancellation);
		var authorization = result.GetProperty("authorization_info");
		var information = result.GetProperty("authorizer_info");

		if(Required(authorization, "authorizer_appid") != authorizerAppId)
			throw new FormatException();
		if(new[] { "channels_info", "store_info", "talent_info", "supplier_info" }.Any(field => information.TryGetProperty(field, out var value) && value.ValueKind != System.Text.Json.JsonValueKind.Null))
			throw new NotSupportedException();

		var service = information.GetProperty("service_type_info").GetProperty("id").GetInt32();
		var applet = information.TryGetProperty("MiniProgramInfo", out var miniProgram) && miniProgram.ValueKind == System.Text.Json.JsonValueKind.Object;

		if(applet ? service is not (0 or 12) : service is not (0 or 1 or 2))
			throw new NotSupportedException();

		var scopes = authorization.GetProperty("func_info").EnumerateArray().Select(permission => permission.GetProperty("funcscope_category").GetProperty("id").GetInt32()).Distinct().ToArray();

		if(scopes.Any(scope => scope <= 0))
			throw new FormatException();

		return new AuthorizerInfo
		{
			AppId = authorizerAppId,
			AccountType = applet ? AccountType.Applet : AccountType.Channel,
			Name = Optional(information, "nick_name"),
			Status = information.TryGetProperty("account_status", out var status) ? status.GetInt32() : 0,
			Permissions = Array.AsReadOnly(scopes),
		};
	}
	#endregion

	#region 内部方法
	internal async ValueTask<AuthorizationState> GetAuthorizationAsync(string authorizerAppId, AccountType? type, int permission, CancellationToken cancellation)
	{
		cancellation.ThrowIfCancellationRequested();
		ArgumentException.ThrowIfNullOrWhiteSpace(authorizerAppId);
		var store = _store as IComponentAuthorizationStore ?? throw new InvalidOperationException();
		var authorization = await store.GetAuthorizationAsync(this.AppId, authorizerAppId, cancellation);

		if(authorization == null || !authorization.Enabled || authorization.ComponentId != this.AppId || authorization.AppId != authorizerAppId ||
			authorization.AccountType is not (AccountType.Applet or AccountType.Channel) || type.HasValue && authorization.AccountType != type.Value ||
			string.IsNullOrEmpty(authorization.GrantId) || string.IsNullOrEmpty(authorization.Revision))
			throw new Common.OperationException("AUTHORIZATION_UNAVAILABLE", null);
		if(permission > 0 && authorization.Permissions?.Contains(permission) != true)
			throw new Common.OperationException("AUTHORIZATION_PERMISSION_DENIED", null);

		cancellation.ThrowIfCancellationRequested();
		return authorization;
	}

	internal ValueTask<bool> UpdateCredentialAsync(string authorizerAppId, string revision, AuthorizationCredential credential, CancellationToken cancellation) =>
		((IComponentAuthorizationStore)_store).UpdateCredentialAsync(this.AppId, authorizerAppId, revision, credential, cancellation);

	internal async ValueTask<AuthorizationCredential> RefreshCredentialAsync(AuthorizationState authorization, CancellationToken cancellation)
	{
		if(string.IsNullOrWhiteSpace(authorization.Credential?.RefreshToken))
			throw new Common.OperationException("AUTHORIZATION_CREDENTIAL_UNAVAILABLE", null);

		var started = DateTimeOffset.UtcNow;
		var result = await this.SendAsync("api_authorizer_token", new { component_appid = this.AppId, authorizer_appid = authorization.AppId, authorizer_refresh_token = authorization.Credential.RefreshToken }, cancellation);
		return new AuthorizationCredential { Value = Required(result, "authorizer_access_token"), RefreshToken = Required(result, "authorizer_refresh_token"), Expiration = Expiration(result, started) };
	}
	#endregion
}
