using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Common;
using Zongsoft.Services;

namespace Zongsoft.Externals.Wechat;

public static partial class CredentialManager
{
	private static async ValueTask<Token> AcquireAsync(Account account, string type, bool refresh, CancellationToken cancellation)
	{
		var credential = type == null ? null : await account.GetCredentialAsync(false, cancellation);
		if(type == null && string.IsNullOrWhiteSpace(account.Secret))
			throw new ArgumentException(null, nameof(account));

		using var client = CreateClient();
		using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
		timeout.CancelAfter(TimeSpan.FromSeconds(30));

		for(var attempt = 0; ; attempt++)
		{
			var started = DateTime.UtcNow;
			using var request = type == null ?
				new HttpRequestMessage(HttpMethod.Post, "https://api.weixin.qq.com/cgi-bin/stable_token")
				{
					Content = JsonContent.Create(new { grant_type = "client_credential", appid = account.Code, secret = account.Secret, force_refresh = refresh }, options: Json.Options),
				} :
				new HttpRequestMessage(HttpMethod.Get, $"https://api.weixin.qq.com/cgi-bin/ticket/getticket?access_token={Uri.EscapeDataString(credential)}&type={Uri.EscapeDataString(type)}");

			try
			{
				using var response = await (client ?? _http).SendAsync(request, timeout.Token);
				var result = await response.GetResultAsync<TokenResult>(timeout.Token);
				var value = type == null ? result.Credential : result.Ticket;

				if(string.IsNullOrWhiteSpace(value) || result.Expiry <= 0)
					throw new OperationException("INVALID_CREDENTIAL_RESPONSE", null);

				var expiration = started.AddSeconds(result.Expiry - Math.Min(30, result.Expiry * 0.1));
				if(expiration <= DateTime.UtcNow)
					throw new OperationException("EXPIRED_CREDENTIAL_RESPONSE", null);

				return new Token { Value = value, Revision = Guid.NewGuid().ToString("N"), Expiration = expiration };
			}
			catch(OperationException error) when(error.Reason == "-1" && attempt < 2 && !(type == null && refresh))
			{
				await Task.Delay(TimeSpan.FromMilliseconds(500 * (attempt + 1)), timeout.Token);
			}
		}
	}

	private static HttpClient CreateClient()
	{
		var factory = ApplicationContext.Current?.Services.GetService(typeof(IHttpMessageHandlerFactory)) as IHttpMessageHandlerFactory;
		if(factory == null)
			return null;

		var handler = factory.CreateHandler("Wechat.Credentials");
		var primary = handler;
		while(primary is DelegatingHandler delegating)
			primary = delegating.InnerHandler;

		if(primary is SocketsHttpHandler { AllowAutoRedirect: true } or HttpClientHandler { AllowAutoRedirect: true })
			throw new InvalidOperationException();

		return new HttpClient(handler, false) { Timeout = TimeSpan.FromSeconds(30) };
	}

	private struct TokenResult
	{
		[JsonPropertyName("access_token")]
		public string Credential { get; set; }
		[JsonPropertyName("ticket")]
		public string Ticket { get; set; }
		[JsonPropertyName("expires_in")]
		public int Expiry { get; set; }
	}
}
