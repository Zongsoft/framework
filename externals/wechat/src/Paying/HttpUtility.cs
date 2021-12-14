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

namespace Zongsoft.Externals.Wechat.Paying
{
	public static class HttpUtility
	{
		public static async Task<OperationResult> GetAsync(this HttpClient client, string url, CancellationToken cancellation = default)
		{
			var response = await client.GetAsync(url, cancellation);
			return await GetResultAsync(response, cancellation);
		}

		public static async Task<OperationResult<TResult>> GetAsync<TResult>(this HttpClient client, string url, CancellationToken cancellation = default)
		{
			var response = await client.GetAsync(url, cancellation);
			return await GetResultAsync<TResult>(response, cancellation);
		}

		public static async Task<OperationResult> PostAsync<TRequest>(this HttpClient client, string url, TRequest request, CancellationToken cancellation = default)
		{
			var content = JsonContent.Create(request, request.GetType(), null, Json.Default);
			var response = await client.PostAsync(url, content, cancellation);
			return await GetResultAsync(response, cancellation);
		}

		public static async Task<OperationResult<TResult>> PostAsync<TRequest, TResult>(this HttpClient client, string url, TRequest request, CancellationToken cancellation = default)
		{
			var content = JsonContent.Create(request, request.GetType(), null, Json.Default);
			var response = await client.PostAsync(url, content, cancellation);
			return await GetResultAsync<TResult>(response, cancellation);
		}

		public static async Task<OperationResult> GetResultAsync(this HttpResponseMessage response, CancellationToken cancellation = default)
		{
			if(response == null)
				throw new ArgumentNullException(nameof(response));

			if(response.IsSuccessStatusCode)
				return OperationResult.Success();

			if(response.Content.Headers.ContentLength <= 0)
				return OperationResult.Fail((int)response.StatusCode, response.ReasonPhrase);

			var failure = await response.Content.ReadFromJsonAsync<FailureResult>(Json.Default, cancellation);
			return OperationResult.Fail(failure.Code, failure.Message);
		}

		public static async Task<OperationResult<TResult>> GetResultAsync<TResult>(this HttpResponseMessage response, CancellationToken cancellation = default)
		{
			if(response == null)
				throw new ArgumentNullException(nameof(response));

			if(response.IsSuccessStatusCode)
			{
				if(response.Content.Headers.ContentLength <= 0)
					return OperationResult.Success();

				var result = await response.Content.ReadFromJsonAsync<TResult>(Json.Default, cancellation);
				return OperationResult.Success(result);
			}
			else
			{
				if(response.Content.Headers.ContentLength <= 0)
					return OperationResult.Fail((int)response.StatusCode, response.ReasonPhrase);

				var failure = await response.Content.ReadFromJsonAsync<FailureResult>(Json.Default, cancellation);
				return OperationResult.Fail(failure.Code, failure.Message);
			}
		}
	}
}
