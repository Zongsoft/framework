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
using System.Globalization;
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
				if(value.TryParse(out int byteValue))
					return (T)(object)checked((byte)byteValue);
				break;
			case TypeCode.SByte:
				if(value.TryParse(out int sbyteValue))
					return (T)(object)checked((sbyte)sbyteValue);
				break;
			case TypeCode.Int16:
				if(value.TryParse(out int shortValue))
					return (T)(object)checked((short)shortValue);
				break;
			case TypeCode.Int32:
				if(value.TryParse(out int integer))
					return (T)(object)integer;
				break;
			case TypeCode.UInt16:
				if(value.TryParse(out int ushortValue))
					return (T)(object)checked((ushort)ushortValue);
				break;
			case TypeCode.Int64:
				if(value.TryParse(out long biginteger))
					return (T)(object)biginteger;
				break;
			case TypeCode.UInt32:
				if(value.TryParse(out long uintValue))
					return (T)(object)checked((uint)uintValue);
				break;
			case TypeCode.UInt64:
				if(ulong.TryParse(value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var ulongValue))
					return (T)(object)ulongValue;
				break;
			case TypeCode.Single:
				if(value.TryParse(out double singleValue))
					return (T)(object)(float)singleValue;
				break;
			case TypeCode.Double:
				if(value.TryParse(out double number))
					return (T)(object)number;
				break;
		}

		return Common.Convert.ConvertValue<T>(value.ToString());
	}
}
