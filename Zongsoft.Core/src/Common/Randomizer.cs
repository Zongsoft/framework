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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Security.Cryptography;

namespace Zongsoft.Common;

/// <summary>
/// 提供随机数生成的静态类。
/// </summary>
public static class Randomizer
{
	#region 常量定义
	private const string SECRET = "0123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz!#$%&";
	private const string CHARACTERS = "0123456789ABCDEFGHJKMNPRSTUVWXYZ";
	#endregion

	#region 公共方法
	public static byte[] Generate(int length)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(length, 1);
		return RandomNumberGenerator.GetBytes(length);
	}

	public static short GenerateInt16()
	{
		var bytes = RandomNumberGenerator.GetBytes(sizeof(short));
		return BitConverter.ToInt16(bytes);
	}

	public static ushort GenerateUInt16()
	{
		var bytes = RandomNumberGenerator.GetBytes(sizeof(ushort));
		return BitConverter.ToUInt16(bytes);
	}

	public static int GenerateInt32() => RandomNumberGenerator.GetInt32(int.MinValue, int.MaxValue);
	public static uint GenerateUInt32()
	{
		var bytes = RandomNumberGenerator.GetBytes(sizeof(uint));
		return BitConverter.ToUInt32(bytes);
	}

	public static long GenerateInt64()
	{
		var bytes = RandomNumberGenerator.GetBytes(sizeof(long));
		return BitConverter.ToInt64(bytes);
	}

	public static ulong GenerateUInt64()
	{
		var bytes = RandomNumberGenerator.GetBytes(sizeof(ulong));
		return BitConverter.ToUInt64(bytes);
	}

	public static string GenerateSecret(int length = 16)
	{
		if(length < 1 || length > 1024 * 1024)
			throw new ArgumentOutOfRangeException(nameof(length));

		#if NET8_0_OR_GREATER
		return System.Security.Cryptography.RandomNumberGenerator.GetString(SECRET, length);
		#else
		Span<char> buffer = stackalloc char[length];
		var data = System.Security.Cryptography.RandomNumberGenerator.GetBytes(length);

		for(int i = 0; i < length; i++)
			buffer[i] = SECRET[data[i] % SECRET.Length];

		return new string(buffer);
		#endif
	}

	public static string GenerateString() => GenerateString(8);
	public static string GenerateString(int length, bool digitOnly = false)
	{
		if(length < 1 || length > 1024 * 1024)
			throw new ArgumentOutOfRangeException(nameof(length));

		#if NET8_0_OR_GREATER
		if(digitOnly)
		{
			var characters = Random.Shared.GetItems(CHARACTERS.AsSpan(..10), length);
			return new string(characters);
		}

		return System.Security.Cryptography.RandomNumberGenerator.GetString(CHARACTERS, length);
		#else
		Span<char> buffer = stackalloc char[length];
		var data = System.Security.Cryptography.RandomNumberGenerator.GetBytes(length);

		//确保首位字符始终为数字字符
		buffer[0] = CHARACTERS[data[0] % 10];
		var size = digitOnly ? 10 : CHARACTERS.Length;

		for(int i = 1; i < length; i++)
			buffer[i] = CHARACTERS[data[i] % size];

		return new string(buffer);
		#endif
	}
	#endregion
}
