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

namespace Zongsoft.Externals.Wechat
{
	internal static class Utility
	{
		public static bool TryGetJson<T>(this HttpResponseMessage response, out T data)
		{
			data = default;

			if(response == null)
				return false;

			if(string.Equals(response.Content.Headers.ContentType.MediaType, "application/json", StringComparison.OrdinalIgnoreCase) ||
			   string.Equals(response.Content.Headers.ContentType.MediaType, "text/json", StringComparison.OrdinalIgnoreCase))
			{
				var content = response.Content.ReadAsStringAsync()
				                      .ConfigureAwait(false)
				                      .GetAwaiter()
				                      .GetResult();

				if(content != null && content.Length > 0)
				{
					data = Zongsoft.Serialization.Serializer.Json.Deserialize<T>(content);
					return true;
				}
			}

			return false;
		}

		public static async System.Threading.Tasks.Task<(TResult result, ErrorResult error)> GetResultAsync<TResult>(this HttpResponseMessage response)
		{
			if(response == null)
				throw new ArgumentNullException(nameof(response));

			if(response.StatusCode == System.Net.HttpStatusCode.NoContent || response.Content.Headers.ContentLength <= 0)
			{
				return (default(TResult), default(ErrorResult));
			}

			var content = await response.Content.ReadAsStreamAsync();
			var error = await Serialization.Serializer.Json.DeserializeAsync<ErrorResult>(content);

			if(response.IsSuccessStatusCode && error.Code == 0)
				return (await Serialization.Serializer.Json.DeserializeAsync<TResult>(await response.Content.ReadAsStreamAsync()), error);

			return (default, error);
		}

		public static TimeSpan GetDuration(this DateTime timestamp)
		{
			return timestamp.Kind == DateTimeKind.Utc ? timestamp - DateTime.UtcNow : timestamp - DateTime.Now;
		}
	}
}
