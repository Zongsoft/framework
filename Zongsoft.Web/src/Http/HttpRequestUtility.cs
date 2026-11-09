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
using System.Linq;
using System.Collections.Generic;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Zongsoft.Web.Http;

public static class HttpRequestUtility
{
	public static IEnumerable<KeyValuePair<string, object>> GetParameters(this HttpRequest request)
	{
		if(request == null)
			throw new ArgumentNullException(nameof(request));

		var result = request.RouteValues
			.Concat(request.Query.Map())
			.Concat(request.Headers.Where(header => header.Key.Length > 2 && header.Key.StartsWith("X-", StringComparison.OrdinalIgnoreCase)).Map(entry => new(entry.Key[2..], entry.Value)));

		if(request.HasFormContentType && request.Form != null && request.Form.Count > 0)
			result = result.Concat(request.Form.Map());

		return result;
	}

	private static IEnumerable<KeyValuePair<string, object>> Map(this IEnumerable<KeyValuePair<string, StringValues>> collection, Func<KeyValuePair<string, object>, KeyValuePair<string, object>> mapper = null)
	{
		if(collection == null)
			return [];

		return collection.Select(entry => mapper == null ? GetEntry(entry) : mapper(GetEntry(entry)));

		static KeyValuePair<string, object> GetEntry(KeyValuePair<string, StringValues> entry) => new(entry.Key, entry.Value.Count switch
		{
			0 => null,
			1 => entry.Value.ToString(),
			_ => entry.Value.ToArray(),
		});
	}
}