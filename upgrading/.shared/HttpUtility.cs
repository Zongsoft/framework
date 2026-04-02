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
 * Copyright (C) 2020-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Upgrading library.
 *
 * The Zongsoft.Upgrading is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Upgrading is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Upgrading library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Net;
using System.Net.Http;

namespace Zongsoft.Upgrading;

internal static class HttpUtility
{
	public static HttpClient CreateHttpClient(string baseAddress, TimeSpan timeout)
	{
		if(string.IsNullOrEmpty(baseAddress))
			throw new ArgumentNullException(nameof(baseAddress));

		if(baseAddress.IndexOf("://") < 0)
			baseAddress += $"http://{baseAddress}";

		var handler = new HttpClientHandler
		{
			AllowAutoRedirect = true,
			AutomaticDecompression = DecompressionMethods.All,
		};

		return new HttpClient(handler)
		{
			BaseAddress = new Uri(baseAddress),
			Timeout = timeout.Ticks > TimeSpan.TicksPerSecond ? timeout : TimeSpan.FromSeconds(30),
		};
	}

	public static System.Text.Encoding GetEncoding(HttpContent content)
	{
		if(content.Headers.ContentType?.CharSet is string charset)
		{
			try
			{
				if(charset.Length > 2 && charset[0] == '\"' && charset[^1] == '\"')
					return System.Text.Encoding.GetEncoding(charset[1..^1]);
				else
					return System.Text.Encoding.GetEncoding(charset);
			}
			catch { }
		}

		return null;
	}
}
