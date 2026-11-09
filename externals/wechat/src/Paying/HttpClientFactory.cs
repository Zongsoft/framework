/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@qq.com>
 *
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace Zongsoft.Externals.Wechat.Paying;

public static partial class HttpClientFactory
{
	private static readonly ConditionalWeakTable<IAuthority, HttpClient> _clients = new();
	private static readonly HttpMessageInvoker _transport = new(new SocketsHttpHandler { AllowAutoRedirect = false, PooledConnectionLifetime = TimeSpan.FromMinutes(5) });

	public static HttpClient GetHttpClient(IAuthority authority)
	{
		ArgumentNullException.ThrowIfNull(authority);

		if(authority.Signing == null || authority.Verification == null || !string.Equals(authority.Code, authority.Signing.MerchantId, StringComparison.Ordinal))
			throw new ArgumentException(null, nameof(authority));

		return _clients.GetValue(authority, CreateHttpClient);
	}

	private static HttpClient CreateHttpClient(IAuthority authority)
	{
		var client = new HttpClient(new PaymentHttpMessageHandler(authority))
		{
			BaseAddress = new Uri("https://api.mch.weixin.qq.com/v3/"),
			Timeout = TimeSpan.FromSeconds(30),
		};
		client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
		client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Zongsoft.Externals.Wechat", "1.0"));
		return client;
	}

	private sealed class PaymentHttpMessageHandler(IAuthority authority) : HttpMessageHandler
	{
		protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellation)
		{
			await SignAsync(authority, request, cancellation);
			HttpResponseMessage response = null;

			try
			{
				var factory = Services.ApplicationContext.Current?.Services.GetService(typeof(IHttpMessageHandlerFactory)) as IHttpMessageHandlerFactory;

				using(var transport = factory == null ? null : CreateTransport(factory))
					response = await (transport ?? _transport).SendAsync(request, cancellation);

				var body = await ReadAsync(response.Content, cancellation);
				var verified = await VerifyAsync(authority, response, body, cancellation);

				if(!response.IsSuccessStatusCode)
					throw HttpUtility.GetFailure(response, body, verified);

				var original = response.Content;
				var content = new HttpUtility.VerifiedContent(body);
				foreach(var header in original.Headers)
					content.Headers.TryAddWithoutValidation(header.Key, header.Value);

				response.Content = content;
				original.Dispose();
				return response;
			}
			catch(Exception exception)
			{
				if(response != null)
				{
					HttpUtility.SetFailureContext(exception, response);
					response.Dispose();
				}

				throw;
			}
		}

		private static HttpMessageInvoker CreateTransport(IHttpMessageHandlerFactory factory)
		{
			var handler = factory.CreateHandler("Wechat.Payments");
			var primary = handler;

			while(primary is DelegatingHandler delegating)
				primary = delegating.InnerHandler;

			//不能修改工厂共享的处理器；配置允许重定向时在发送前拒绝。
			if(primary is SocketsHttpHandler { AllowAutoRedirect: true } or HttpClientHandler { AllowAutoRedirect: true })
				throw new InvalidOperationException();

			return new HttpMessageInvoker(handler, false);
		}
	}

}
