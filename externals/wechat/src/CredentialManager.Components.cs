using System;
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Externals.Wechat;

public static partial class CredentialManager
{
	internal static async ValueTask<string> GetComponentCredentialAsync(Platforms.Component component, bool refresh, CancellationToken cancellation)
	{
		var key = $"Zongsoft.Wechat.Component:{Uri.EscapeDataString(component.AppId)}:access-token";
		var token = await GetAsync(key, refresh, Acquire, cancellation);
		return token.Value;

		async ValueTask<Token> Acquire(CancellationToken cancellation)
		{
			var (value, expiration) = await component.AcquireCredentialAsync(cancellation);
			return new Token { Value = value, Expiration = expiration.UtcDateTime, Revision = Guid.NewGuid().ToString("N") };
		}
	}
}
