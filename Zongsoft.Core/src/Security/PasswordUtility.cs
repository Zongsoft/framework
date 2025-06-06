﻿/*
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
using System.Security;
using System.Security.Cryptography;

namespace Zongsoft.Security;

/// <summary>
/// 提供密码操作的工具类。
/// </summary>
public static class PasswordUtility
{
	#region 私有变量
	private static readonly char[] PasswordSymbols = ['-', '+', '#', '@', '~', '$', '&', '.'];
	#endregion

	#region 公共方法
	/// <summary>生成随机口令字符串。</summary>
	/// <returns>返回生成的随机口令字符串。</returns>
	public static string GeneratePassword() => GeneratePassword(8);

	/// <summary>生成随机口令字符串。</summary>
	/// <param name="length">指定要生成的口令字符串的长度，长度至少为6。</param>
	/// <returns>返回生成的随机口令字符串。</returns>
	public static string GeneratePassword(int length)
	{
		byte[] buffer = Zongsoft.Common.Randomizer.Generate(Math.Max(length, 6));
		char[] password = new char[buffer.Length];

		for(int i = 0; i < buffer.Length; i++)
		{
			var index = buffer[i] % 70;

			if(index < 10)
				password[i] = (char)((byte)'0' + index);
			else if(index >= 10 && index < 36)
				password[i] = (char)((byte)'A' + (index - 10));
			else if(index >= 36 && index < 62)
				password[i] = (char)((byte)'a' + (index - 36));
			else
				password[i] = PasswordSymbols[index - 62];
		}

		return new string(password);
	}

	/// <summary>使用系统默认的散列算法(SHA1)对口令明文进行散列。</summary>
	/// <param name="password">待散列(哈希)的口令明文。</param>
	/// <param name="passwordSalt">输出参数，返回随机生成的整型数。</param>
	/// <returns>散列后的口令值。</returns>
	public static byte[] HashPassword(string password, out long passwordSalt)
	{
		passwordSalt = Zongsoft.Common.Randomizer.GenerateInt64();
		return HashPassword(password, BitConverter.GetBytes(passwordSalt));
	}

	/// <summary>使用系统默认的散列算法(SHA1)对口令明文进行散列。</summary>
	/// <param name="password">待散列(哈希)的口令明文。</param>
	/// <param name="passwordSalt">输出参数，返回随机生成的字节数组(8个字节)。</param>
	/// <returns>散列后的口令值。</returns>
	public static byte[] HashPassword(string password, out byte[] passwordSalt)
	{
		passwordSalt = Zongsoft.Common.Randomizer.Generate(8);
		return HashPassword(password, passwordSalt);
	}

	/// <summary>使用指定的散列算法对口令明文进行散列。</summary>
	/// <param name="password">待散列(哈希)的口令明文。</param>
	/// <param name="passwordSalt">对密码进行散列操作的随机整型数。</param>
	/// <param name="hashAlgorithm">进行散列算法的名称，默认为SHA1。</param>
	/// <returns>散列后的口令值。</returns>
	public static byte[] HashPassword(string password, long passwordSalt, string hashAlgorithm = "SHA1") =>
		HashPassword(password, (passwordSalt == 0 ? null : BitConverter.GetBytes(passwordSalt)), hashAlgorithm);

	/// <summary>使用指定的散列算法对口令明文进行散列。</summary>
	/// <param name="password">待散列(哈希)的口令明文。</param>
	/// <param name="passwordSalt">对密码进行散列操作的随机值。</param>
	/// <param name="algorithm">进行散列算法的名称，默认为SHA1。</param>
	/// <returns>散列后的口令值。</returns>
	public static byte[] HashPassword(string password, byte[] passwordSalt, string algorithm = "SHA1")
	{
		if(string.IsNullOrWhiteSpace(algorithm))
			algorithm = "SHA1";

		using(var hasher = GetHasher(algorithm))
		{
			var passwordBuffer = System.Text.Encoding.UTF8.GetBytes(password);

			if(passwordSalt != null && passwordSalt.Length > 0)
			{
				var buffer = new byte[passwordBuffer.Length + passwordSalt.Length];

				Buffer.BlockCopy(passwordBuffer, 0, buffer, 0, passwordBuffer.Length);
				Buffer.BlockCopy(passwordSalt, 0, buffer, passwordBuffer.Length, passwordSalt.Length);

				return hasher.ComputeHash(buffer);
			}

			return hasher.ComputeHash(passwordBuffer);
		}
	}

	/// <summary>验证口令是否与存储在数据库中的口令是否匹配。</summary>
	/// <param name="password">待验证的口令明文。</param>
	/// <param name="storedPassword">存储在数据库中的已Salt后的口令散列值。</param>
	/// <param name="storedPasswordSalt">存储在数据库中的与之匹对的整型随机Salt数。</param>
	/// <param name="hashAlgorithm">进行散列算法的名称，默认为SHA1。</param>
	/// <returns>验证成功则返回真，否则返回假。</returns>
	public static bool VerifyPassword(string password, byte[] storedPassword, long storedPasswordSalt, string hashAlgorithm = "SHA1") =>
		VerifyPassword(password, storedPassword, (storedPasswordSalt == 0 ? null : BitConverter.GetBytes(storedPasswordSalt)), hashAlgorithm);

	/// <summary>验证口令是否与存储在数据库中的口令是否匹配。</summary>
	/// <param name="password">待验证的口令明文。</param>
	/// <param name="storedPassword">存储在数据库中的已Salt后的口令散列值。</param>
	/// <param name="storedPasswordSalt">存储在数据库中的与之匹对的随机Salt值。</param>
	/// <param name="hashAlgorithm">进行散列算法的名称，默认为SHA1。</param>
	/// <returns>验证成功则返回真，否则返回假。</returns>
	public static bool VerifyPassword(string password, byte[] storedPassword, byte[] storedPasswordSalt, string hashAlgorithm = "SHA1")
	{
		if(string.IsNullOrEmpty(password))
		{
			return (storedPassword == null || storedPassword.Length == 0) &&
			       (storedPasswordSalt == null || storedPasswordSalt.Length == 0);
		}

		//计算密码与对应的随机值的哈希值
		byte[] hashedPassword = HashPassword(password, storedPasswordSalt, hashAlgorithm);

		//由于存储的密码长度受不同数据库的影响，其返回的长度可能是字段申明的最大长度，所以不能按长度完全相等进行判断
		if(storedPassword == null || storedPassword.Length < hashedPassword.Length)
			return false;

		//逐字节检测口令的散列值，如果有任一差异则整个匹对失败并退出。
		for(int i = 0; i < hashedPassword.Length; i++)
		{
			if(storedPassword[i] != hashedPassword[i])
				return false;
		}

		//匹对成功，返回。
		return true;
	}
	#endregion

	#region 私有方法
	private static HashAlgorithm GetHasher(string name) => name switch
	{
		"MD5" or "md5" => MD5.Create(),
		"SHA1" or "sha1" => SHA1.Create(),
		"SHA256" or "sha256" or "SHA-256" or "sha-256" => SHA256.Create(),
		"SHA384" or "sha384" or "SHA-384" or "sha-384" => SHA384.Create(),
		"SHA512" or "sha512" or "SHA-512" or "sha-512" => SHA512.Create(),
		_ => SHA1.Create(),
	};
	#endregion
}