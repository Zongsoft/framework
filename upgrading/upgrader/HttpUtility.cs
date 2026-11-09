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
using System.Net;
using System.Net.Http;

namespace Zongsoft.Upgrading;

internal static class HttpUtility
{
	public static HttpClient CreateClient(string baseAddress, TimeSpan timeout)
	{
		if(string.IsNullOrWhiteSpace(baseAddress))
			throw new ArgumentNullException(nameof(baseAddress));

		baseAddress = baseAddress.Trim();

		if(baseAddress.IndexOf("://") < 0)
			baseAddress += $"http://{baseAddress}";

		if(!baseAddress.EndsWith('/'))
			baseAddress += "/";

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
