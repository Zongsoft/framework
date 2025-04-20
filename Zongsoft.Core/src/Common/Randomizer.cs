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
 *   喻星(Xing Yu) <yx@automao.cn>
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
using System.Buffers;

namespace Zongsoft.Common;

/// <summary>
/// 提供随机数生成的静态类。
/// </summary>
public static class Randomizer
{
	#region 常量定义
	private const string Digits = "0123456789ABCDEFGHJKMNPRSTUVWXYZ";
	#endregion

	#region 静态字段
	private static readonly System.Security.Cryptography.RandomNumberGenerator _random = System.Security.Cryptography.RandomNumberGenerator.Create();
	#endregion

	#region 公共方法
	public static byte[] Generate(int length)
	{
		if(length < 1)
			throw new ArgumentOutOfRangeException(nameof(length));

		var bytes = new byte[length];
		_random.GetBytes(bytes);
		return bytes;
	}

	public static short GenerateInt16()
	{
		var bytes = ArrayPool<byte>.Shared.Rent(2);

		try
		{
			_random.GetBytes(bytes, 0, 2);
			return BitConverter.ToInt16(bytes, 0);
		}
		finally
		{
			if(bytes != null)
				ArrayPool<byte>.Shared.Return(bytes);
		}
	}

	public static ushort GenerateUInt16()
	{
		var bytes = ArrayPool<byte>.Shared.Rent(2);

		try
		{
			_random.GetBytes(bytes, 0, 2);
			return BitConverter.ToUInt16(bytes, 0);
		}
		finally
		{
			if(bytes != null)
				ArrayPool<byte>.Shared.Return(bytes);
		}
	}

	public static int GenerateInt32()
	{
		var bytes = ArrayPool<byte>.Shared.Rent(4);

		try
		{
			_random.GetBytes(bytes, 0, 4);
			return BitConverter.ToInt32(bytes, 0);
		}
		finally
		{
			if(bytes != null)
				ArrayPool<byte>.Shared.Return(bytes);
		}
	}

	public static uint GenerateUInt32()
	{
		var bytes = ArrayPool<byte>.Shared.Rent(4);

		try
		{
			_random.GetBytes(bytes, 0, 4);
			return BitConverter.ToUInt32(bytes, 0);
		}
		finally
		{
			if(bytes != null)
				ArrayPool<byte>.Shared.Return(bytes);
		}
	}

	public static long GenerateInt64()
	{
		var bytes = ArrayPool<byte>.Shared.Rent(8);

		try
		{
			_random.GetBytes(bytes, 0, 8);
			return BitConverter.ToInt64(bytes, 0);
		}
		finally
		{
			if(bytes != null)
				ArrayPool<byte>.Shared.Return(bytes);
		}
	}

	public static ulong GenerateUInt64()
	{
		var bytes = ArrayPool<byte>.Shared.Rent(8);

		try
		{
			_random.GetBytes(bytes, 0, 8);
			return BitConverter.ToUInt64(bytes, 0);
		}
		finally
		{
			if(bytes != null)
				ArrayPool<byte>.Shared.Return(bytes);
		}
	}

	public static string GenerateString() => GenerateString(8);
	public static string GenerateString(int length, bool digitOnly = false)
	{
		if(length < 1 || length > 1024)
			throw new ArgumentOutOfRangeException(nameof(length));

#if NET8_0_OR_GREATER
		if(digitOnly)
		{
			var characters = Random.Shared.GetItems(Digits.AsSpan(..10), length);
			return new string(characters);
		}

		return System.Security.Cryptography.RandomNumberGenerator.GetString(Digits, length);
#else
		var result = new char[length];
		var data = System.Security.Cryptography.RandomNumberGenerator.GetBytes(length);

		//确保首位字符始终为数字字符
		result[0] = Digits[data[0] % 10];
		var divisor = digitOnly ? 10 : 32;

		for(int i = 1; i < length; i++)
		{
			result[i] = Digits[data[i] % divisor];
		}

		return new string(result);
#endif
	}

	[Obsolete]
	public static string GenerateStringEx(int length = 8)
	{
		if(length < 1 || length > 1024)
			throw new ArgumentOutOfRangeException(nameof(length));

		var result = new char[length];
		var data = new byte[(int)Math.Ceiling((length * 5) / 8.0)];

		_random.GetBytes(data);

		int value;

		for(int i = 0; i < length; i++)
		{
			int index = i * 5 / 8;
			var bitCount = i * 5 % 8;//当前字节中已获取的位数
			var takeCount = 8 - bitCount;

			if(takeCount < 5)
			{
				value = (((byte)(255 << bitCount)) & data[index]) >> bitCount;
				var count = 8 - (5 - takeCount);
				value += ((byte)(data[index + 1] << count) >> (count - takeCount));
			}
			else
				value = data[index] & (((255 >> (takeCount - 5)) - (255 >> takeCount)) >> bitCount);

			result[i] = Digits[value % 32];
		}

		return new string(result);
	}
	#endregion
}
