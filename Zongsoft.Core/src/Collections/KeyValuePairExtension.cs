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

namespace Zongsoft.Collections;

public static class KeyValuePairExtension
{
	#region 公共方法
	public static KeyValuePair<string, object>[] CreatePairs(string[] keys, params object[] values)
	{
		if(keys == null)
			throw new ArgumentNullException(nameof(keys));

		var result = new KeyValuePair<string, object>[keys.Length];

		for(int i = 0; i < result.Length; i++)
		{
			result[i] = new KeyValuePair<string, object>(keys[i], (values != null && i < values.Length ? values[i] : null));
		}

		return result;
	}

	public static KeyValuePair<string, object>[] CreatePairs(object[] values, params string[] keys)
	{
		if(keys == null)
			throw new ArgumentNullException(nameof(keys));

		var result = new KeyValuePair<string, object>[keys.Length];

		for(int i = 0; i < result.Length; i++)
		{
			result[i] = new KeyValuePair<string, object>(keys[i], (values != null && i < values.Length ? values[i] : null));
		}

		return result;
	}

	public static object[] ToArray(this KeyValuePair<string, object>[] pairs)
	{
		if(pairs == null)
			return null;

		var array = new object[pairs.Length];
		Array.Copy(pairs, array, array.Length);
		return array;
	}
	#endregion
}
