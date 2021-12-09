/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
 *
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.WeChat library.
 *
 * The Zongsoft.Externals.WeChat is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.WeChat is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.WeChat library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Zongsoft.Externals.Wechat.Paying
{
	public static class AuthorityExtension
	{
		private static readonly IDictionary<IAuthority, HttpClient> _clients = new Dictionary<IAuthority, HttpClient>();

		internal static HttpClient GetHttpClient(this IAuthority authority)
		{
			if(authority == null)
				throw new ArgumentNullException(nameof(authority));

			if(_clients.TryGetValue(authority, out var client) && client != null)
				return client;

			lock(_clients)
			{
				if(_clients.TryGetValue(authority, out client))
					return client;

				return _clients.TryAdd(authority, client = CreateHttpClient(authority)) ? client : _clients[authority];
			}
		}

		private static HttpClient CreateHttpClient(IAuthority authority)
		{
			var client = new HttpClient(new PaymentHttpMessageHandler(authority));
			client.BaseAddress = new Uri("https://api.mch.weixin.qq.com/v3/pay/");
			client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
			client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Zongsoft.Externals.Wechat", "1.0"));
			return client;
		}

		private class PaymentHttpMessageHandler : DelegatingHandler
		{
			private readonly IAuthority _authority;

			public PaymentHttpMessageHandler(IAuthority authority)
			{
				_authority = authority ?? throw new ArgumentNullException(nameof(authority));
				this.InnerHandler = new HttpClientHandler();
			}

			protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellation)
			{
				var method = request.Method;
				var content = string.Empty;

				if(method == HttpMethod.Put || method == HttpMethod.Post || method == HttpMethod.Patch)
					content = await request.Content.ReadAsStringAsync(cancellation);

				var value = Signature(_authority, request.Method.ToString(), request.RequestUri.PathAndQuery, content);
				request.Headers.Authorization = new AuthenticationHeaderValue("WECHATPAY2-SHA256-RSA2048", value);
				return await base.SendAsync(request, cancellation);
			}

			private static string Signature(IAuthority account, string method, string url, string content)
			{
				var nonce = Guid.NewGuid().ToString("N");
				var timestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
				var message = $"{method}\n{url}\n{timestamp}\n{nonce}\n{content}\n";
				var signature = System.Convert.ToBase64String(account.Certificate.Signature(System.Text.Encoding.UTF8.GetBytes(message)));

				return $"mchid=\"{account.Certificate.Subject.Identifier}\",nonce_str=\"{nonce}\",timestamp=\"{timestamp}\",serial_no=\"{account.Certificate.Name}\",signature=\"{signature}\"";
			}
		}
	}
}
