using System;
using System.Net.Http;
using Zongsoft.Services;

namespace Zongsoft.Externals.Wechat;

internal static class AccountHttpClient
{
	private static readonly SocketsHttpHandler _handler = new() { AllowAutoRedirect = false, PooledConnectionLifetime = TimeSpan.FromMinutes(5) };

	public static HttpClient Create(string name = "Wechat.Accounts")
	{
		var factory = ApplicationContext.Current?.Services.GetService(typeof(IHttpMessageHandlerFactory)) as IHttpMessageHandlerFactory;
		var handler = factory?.CreateHandler(name) ?? _handler;
		var primary = handler;

		while(primary is DelegatingHandler delegating)
			primary = delegating.InnerHandler;

		if(primary is SocketsHttpHandler { AllowAutoRedirect: true } or HttpClientHandler { AllowAutoRedirect: true })
			throw new InvalidOperationException();

		return new HttpClient(handler, false) { BaseAddress = new Uri("https://api.weixin.qq.com/"), Timeout = TimeSpan.FromSeconds(30) };
	}
}
