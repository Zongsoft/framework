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

namespace Zongsoft.Externals.Lua;

internal static class Utility
{
	public static object Convert(object value)
	{
		return value switch
		{
			NLua.LuaTable table => table.ToDictionary(),
			_ => value
		};
	}

	public static object[] Convert(object[] values)
	{
		if(values == null || values.Length == 0)
			return values;

		for(int i = 0; i < values.Length; i++)
			values[i] = Convert(values[i]);

		return values;
	}

	public static Dictionary<string, object> ToDictionary(this NLua.LuaTable table)
	{
		if(table == null)
			return null;

		var dictionary = new Dictionary<string, object>();
		var enumerator = table.GetEnumerator();

		while(enumerator.MoveNext())
			dictionary[enumerator.Key.ToString()] = enumerator.Value;

		return dictionary;
	}
}
