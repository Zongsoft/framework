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
 * Copyright (C) 2010-2024 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.Lua library.
 *
 * The Zongsoft.Externals.Lua is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Lua is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Lua library. If not, see <http://www.gnu.org/licenses/>.
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
