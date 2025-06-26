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
 * This file is part of Zongsoft.Externals.Redis library.
 *
 * The Zongsoft.Externals.Redis is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Redis is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Redis library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using StackExchange.Redis;

namespace Zongsoft.Externals.Redis;

internal static class RedisValueExtension
{
	public static T GetValue<T>(this RedisValue value)
	{
		if(value.IsNull)
			return default;

		if(typeof(T) == typeof(string) || typeof(T) == typeof(object))
			return (T)(object)value.ToString();
		if(typeof(T) == typeof(byte[]))
			return (T)value.Box();

		switch(Type.GetTypeCode(typeof(T)))
		{
			case TypeCode.Byte:
			case TypeCode.SByte:
			case TypeCode.Int16:
			case TypeCode.Int32:
			case TypeCode.UInt16:
				if(value.TryParse(out int integer))
					return (T)(object)integer;
				break;
			case TypeCode.Int64:
			case TypeCode.UInt32:
			case TypeCode.UInt64:
				if(value.TryParse(out long biginteger))
					return (T)(object)biginteger;
				break;
			case TypeCode.Single:
			case TypeCode.Double:
				if(value.TryParse(out double number))
					return (T)(object)number;
				break;
		}

		return Common.Convert.ConvertValue<T>(value.ToString());
	}
}
