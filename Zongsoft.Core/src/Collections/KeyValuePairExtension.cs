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
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
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
