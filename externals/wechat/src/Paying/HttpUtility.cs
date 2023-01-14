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
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Common;
using Zongsoft.Security;

namespace Zongsoft.Externals.Wechat.Paying
{
	public static class HttpUtility
	{
		public static async ValueTask GetAsync(this HttpClient client, string url, CancellationToken cancellation = default)
		{
			var response = await client.GetAsync(url, cancellation);
			await EnsureResultAsync(response, cancellation);
		}

		public static async ValueTask<TResult> GetAsync<TResult>(this HttpClient client, string url, CancellationToken cancellation = default)
		{
			var response = await client.GetAsync(url, cancellation);
			return await GetResultAsync<TResult>(response, cancellation);
		}

		public static ValueTask PostAsync(this HttpClient client, string url, CancellationToken cancellation = default) => PostAsync(client, url, (object)null, cancellation);
		public static async ValueTask PostAsync<TRequest>(this HttpClient client, string url, TRequest request, CancellationToken cancellation = default)
		{
			var content = request is null ? (HttpContent)new StringContent(string.Empty, System.Text.Encoding.UTF8, "application/json") : JsonContent.Create(request, request.GetType(), null, Json.Options);
			var response = await client.PostAsync(url, content, cancellation);
			await EnsureResultAsync(response, cancellation);
		}

		public static ValueTask<TResult> PostAsync<TRequest, TResult>(this HttpClient client, string url, TRequest request, CancellationToken cancellation = default)
		{
			return PostAsync<TRequest, TResult>(client, url, request, null, cancellation);
		}

		public static async ValueTask<TResult> PostAsync<TRequest, TResult>(this HttpClient client, string url, TRequest request, ICertificate certificate, CancellationToken cancellation = default)
		{
			//var json = System.Text.Json.JsonSerializer.Serialize(request, request.GetType(), Json.Options);
			var content = JsonContent.Create(request, request.GetType(), null, Json.Options);

			if(certificate != null)
				content.Headers.Add("Wechatpay-Serial", certificate.Identifier);

			var response = await client.PostAsync(url, content, cancellation);
			return await GetResultAsync<TResult>(response, cancellation);
		}

		public static async ValueTask EnsureResultAsync(this HttpResponseMessage response, CancellationToken cancellation = default)
		{
			if(response == null)
				throw new ArgumentNullException(nameof(response));

			if(response.IsSuccessStatusCode)
				return;

			if(response.Content.Headers.ContentLength <= 0)
				throw new OperationException(response.StatusCode.ToString(), response.ReasonPhrase);

			var failure = response.Content.Headers.ContentType.MediaType.Contains("json", StringComparison.OrdinalIgnoreCase) ?
				await response.Content.ReadFromJsonAsync<FailureResult>(Json.Options, cancellation) :
				new FailureResult(response.StatusCode.ToString(), await response.Content.ReadAsStringAsync(cancellation));

			throw new OperationException(failure.Code, failure.Message);
		}

		public static async ValueTask<TResult> GetResultAsync<TResult>(this HttpResponseMessage response, CancellationToken cancellation = default)
		{
			if(response == null)
				throw new ArgumentNullException(nameof(response));

			if(response.IsSuccessStatusCode)
			{
				if(response.Content.Headers.ContentLength <= 0 || typeof(TResult) == typeof(void) || typeof(TResult) == typeof(object))
					return default;

				//var text = await response.Content.ReadAsStringAsync(cancellation);
				//var result = System.Text.Json.JsonSerializer.Deserialize<TResult>(text, Json.Options);
				var result = await response.Content.ReadFromJsonAsync<TResult>(Json.Options, cancellation);
				return result;
			}
			else
			{
				if(response.Content.Headers.ContentLength <= 0)
					throw new OperationException(response.StatusCode.ToString(), response.ReasonPhrase);

				var failure = response.Content.Headers.ContentType.MediaType.Contains("json", StringComparison.OrdinalIgnoreCase) ?
					await response.Content.ReadFromJsonAsync<FailureResult>(Json.Options, cancellation) :
					new FailureResult(response.StatusCode.ToString(), await response.Content.ReadAsStringAsync(cancellation));

				throw new OperationException(failure.Code, failure.Message);
			}
		}
	}
}
