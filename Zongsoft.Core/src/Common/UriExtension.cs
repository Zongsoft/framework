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
using System.Collections.Generic;

namespace Zongsoft.Common;

public static class UriExtension
{
	public static bool TryGetQueryString(this Uri url, string key, out string value)
	{
		var parts = StringExtension.Slice<KeyValuePair<string, string>>(url.Query, '&', TryGetPart);

		foreach(var part in parts)
		{
			if(string.Equals(key, part.Key, StringComparison.OrdinalIgnoreCase))
			{
				value = part.Value;
				return true;
			}
		}

		value = null;
		return false;

		static bool TryGetPart(string part, out KeyValuePair<string, string> pair)
		{
			if(string.IsNullOrEmpty(part))
			{
				pair = default;
				return false;
			}

			var index = part.IndexOf('=');

			if(index < 0)
				pair = new KeyValuePair<string, string>(part, string.Empty);
			else
				pair = new KeyValuePair<string, string>(part[..index], part[(index + 1)..]);

			return true;
		}
	}
}
