using System;
using System.Threading;
using System.Threading.Tasks;
using Zongsoft.Common;
using Zongsoft.Services.Distributing;

namespace Zongsoft.Externals.Wechat;

public static partial class CredentialManager
{
	#region 内部方法
	internal static async ValueTask EnsureAuthorizationAsync(this Account account, int permission, CancellationToken cancellation)
	{
		cancellation.ThrowIfCancellationRequested();

		if(account.Component != null)
			await account.Component.GetAuthorizationAsync(account.Code, account.Type, permission, cancellation);
	}

	internal static async ValueTask<string> GetComponentCredentialAsync(this Account account, int permission, CancellationToken cancellation)
	{
		var original = await account.Component.GetAuthorizationAsync(account.Code, account.Type, permission, cancellation);
		var token = await account.Component.GetCredentialAsync(false, cancellation);
		var current = await account.Component.GetAuthorizationAsync(account.Code, account.Type, permission, cancellation);

		if(original.GrantId != current.GrantId)
			throw new OperationException("AUTHORIZATION_CHANGED", null);

		return token;
	}
	#endregion

	#region 私有方法
	private static async ValueTask<string> GetAuthorizerCredentialAsync(Account account, int permission, bool refresh, CancellationToken cancellation)
	{
		var component = account.Component;
		var original = await component.GetAuthorizationAsync(account.Code, account.Type, permission, cancellation);

		if(!refresh && Valid(original.Credential))
			return original.Credential.Value;

		var key = $"Zongsoft.Wechat.Authorizer:{Uri.EscapeDataString(component.AppId)}:{Uri.EscapeDataString(account.Code)}:credential-lock";
		var gate = EnterGate(key);
		var entered = false;

		try
		{
			await gate.Semaphore.WaitAsync(cancellation);
			entered = true;
			var manager = Locker ?? throw new InvalidOperationException();

			for(var attempt = 0; attempt < 60; attempt++)
			{
				var current = await component.GetAuthorizationAsync(account.Code, account.Type, permission, cancellation);
				CheckGrant(current);

				if(Valid(current.Credential) && (!refresh || current.Revision != original.Revision))
					return current.Credential.Value;

				await using(var lease = await manager.AcquireAsync(key, new DistributedLockOptions(TimeSpan.FromSeconds(60)) { RenewalInterval = TimeSpan.FromSeconds(20) }, cancellation))
				{
					if(lease?.IsLocked == true)
					{
						current = await component.GetAuthorizationAsync(account.Code, account.Type, permission, cancellation);
						CheckGrant(current);

						if(Valid(current.Credential) && (!refresh || current.Revision != original.Revision))
							return current.Credential.Value;

						var credential = await component.RefreshCredentialAsync(current, cancellation);

						if(!lease.IsLocked)
							throw new OperationException("CREDENTIAL_LOCK_LOST", null);

						var saved = await component.UpdateCredentialAsync(account.Code, current.Revision, credential, CancellationToken.None);
						var latest = await component.GetAuthorizationAsync(account.Code, account.Type, permission, cancellation);
						CheckGrant(latest);

						if(!saved && latest.Revision == current.Revision || !Valid(latest.Credential))
							throw new OperationException("AUTHORIZATION_CREDENTIAL_STORE_FAILED", null);

						cancellation.ThrowIfCancellationRequested();
						return latest.Credential.Value;
					}
				}

				await Task.Delay(TimeSpan.FromMilliseconds(500), cancellation);
			}

			throw new TimeoutException();
		}
		finally
		{
			if(entered)
				gate.Semaphore.Release();

			ExitGate(key, gate);
		}

		void CheckGrant(Platforms.Component.AuthorizationState authorization)
		{
			if(authorization.GrantId != original.GrantId)
				throw new OperationException("AUTHORIZATION_CHANGED", null);
		}

		static bool Valid(Platforms.Component.AuthorizationCredential credential) => credential != null && !string.IsNullOrWhiteSpace(credential.Value) && credential.Expiration > DateTimeOffset.UtcNow;
	}

	private static async ValueTask<Token> GetAuthorizerTicketAsync(Account account, string type, bool refresh, CancellationToken cancellation)
	{
		var permission = type == "jsapi" ? 4 : 8;
		var original = await account.Component.GetAuthorizationAsync(account.Code, account.Type, permission, cancellation);
		var key = $"Zongsoft.Wechat.Authorizer:{Uri.EscapeDataString(account.ComponentId)}:{Uri.EscapeDataString(account.Code)}:{Uri.EscapeDataString(original.GrantId)}:ticket:{type}";
		var ticket = await GetAsync(key, refresh, Acquire, cancellation);
		var current = await account.Component.GetAuthorizationAsync(account.Code, account.Type, permission, cancellation);

		if(current.GrantId != original.GrantId)
			throw new OperationException("AUTHORIZATION_CHANGED", null);

		return ticket;

		async ValueTask<Token> Acquire(CancellationToken cancellation)
		{
			var credential = await account.GetCredentialAsync(permission, false, cancellation);
			var started = DateTime.UtcNow;
			using var client = AccountHttpClient.Create();
			using var response = await client.GetAsync($"cgi-bin/ticket/getticket?access_token={Uri.EscapeDataString(credential)}&type={Uri.EscapeDataString(type)}", cancellation);
			var result = await response.GetResultAsync<TokenResult>(cancellation);

			if(string.IsNullOrWhiteSpace(result.Ticket) || result.Expiry <= 0)
				throw new OperationException("INVALID_CREDENTIAL_RESPONSE", null);

			return new Token { Value = result.Ticket, Expiration = started.AddSeconds(result.Expiry - Math.Min(30, result.Expiry * 0.1)), Revision = Guid.NewGuid().ToString("N") };
		}
	}
	#endregion
}
