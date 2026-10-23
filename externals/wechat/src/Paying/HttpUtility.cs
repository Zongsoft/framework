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
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Common;

namespace Zongsoft.Externals.Wechat.Paying;

public static partial class HttpUtility
{
	public static async ValueTask GetAsync(this HttpClient client, string url, CancellationToken cancellation = default)
	{
		using var request = new HttpRequestMessage(HttpMethod.Get, url);
		using var response = await client.SendAsync(request, cancellation);
		await EnsureResultAsync(response, cancellation);
	}

	public static async ValueTask<TResult> GetAsync<TResult>(this HttpClient client, string url, CancellationToken cancellation = default)
	{
		using var request = new HttpRequestMessage(HttpMethod.Get, url);
		using var response = await client.SendAsync(request, cancellation);
		return await GetResultAsync<TResult>(response, cancellation);
	}

	public static ValueTask PostAsync(this HttpClient client, string url, CancellationToken cancellation = default) => PostAsync(client, url, (object)null, cancellation);
	public static async ValueTask PostAsync<TRequest>(this HttpClient client, string url, TRequest request, CancellationToken cancellation = default)
	{
		using var message = new HttpRequestMessage(HttpMethod.Post, url)
		{
			Content = request is null ? null : JsonContent.Create(request, request.GetType(), null, Json.Options),
		};
		using var response = await client.SendAsync(message, cancellation);
		await EnsureResultAsync(response, cancellation);
	}

	public static ValueTask<TResult> PostAsync<TRequest, TResult>(this HttpClient client, string url, TRequest request, CancellationToken cancellation = default)
	{
		return PostAsync<TRequest, TResult>(client, url, request, null, cancellation);
	}

	public static async ValueTask<TResult> PostAsync<TRequest, TResult>(this HttpClient client, string url, TRequest request, Authority.VerificationKey certificate, CancellationToken cancellation = default)
	{
		using var message = new HttpRequestMessage(HttpMethod.Post, url)
		{
			Content = request is null ? null : JsonContent.Create(request, request.GetType(), null, certificate == null ? Json.Options : Json.GetOptions(certificate)),
		};

		if(certificate != null)
			message.Headers.Add("Wechatpay-Serial", certificate.Identifier);

		using var response = await client.SendAsync(message, cancellation);
		return await GetResultAsync<TResult>(response, cancellation);
	}

	public static async ValueTask EnsureResultAsync(this HttpResponseMessage response, CancellationToken cancellation = default)
	{
		ArgumentNullException.ThrowIfNull(response);
		cancellation.ThrowIfCancellationRequested();

		if(response.IsSuccessStatusCode)
			return;

		throw GetFailure(response, await response.Content.ReadAsByteArrayAsync(cancellation), response.Content is VerifiedContent);
	}

	public static async ValueTask<TResult> GetResultAsync<TResult>(this HttpResponseMessage response, CancellationToken cancellation = default)
	{
		await EnsureResultAsync(response, cancellation);
		if(response.StatusCode == System.Net.HttpStatusCode.NoContent)
			return default;

		try
		{
			return JsonSerializer.Deserialize<TResult>(await response.Content.ReadAsByteArrayAsync(cancellation), Json.Options);
		}
		catch(JsonException exception)
		{
			var failure = new OperationException("INVALID_RESPONSE", null, exception);
			SetFailureContext(failure, response);
			throw failure;
		}
	}
}
