/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   Tencent (https://developers.weixin.qq.com/doc/service/guide/dev/push/encryption.html)
 *
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.WeChat library.
 *
 * The Zongsoft.Externals.WeChat is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.WeChat is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.WeChat library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Security.Cryptography;

namespace Zongsoft.Externals.Wechat.Platforms;

public static partial class CryptographyUtility
{
	public static string Sign(string token, string timestamp, string nonce, string ciphertext) => Convert.ToHexString(Hash(token, timestamp, nonce, ciphertext)).ToLowerInvariant();

	public static bool VerifySignature(string token, string timestamp, string nonce, string ciphertext, string signature)
	{
		if(string.IsNullOrEmpty(signature) || signature.Length != 40)
			return false;

		byte[] expected;

		try { expected = Convert.FromHexString(signature); }
		catch(FormatException) { return false; }

		return CryptographicOperations.FixedTimeEquals(expected, Hash(token, timestamp, nonce, ciphertext));
	}

	private static byte[] Hash(string token, string timestamp, string nonce, string ciphertext)
	{
		ArgumentException.ThrowIfNullOrEmpty(token);
		ArgumentException.ThrowIfNullOrEmpty(timestamp);
		ArgumentException.ThrowIfNullOrEmpty(nonce);
		ArgumentException.ThrowIfNullOrEmpty(ciphertext);
		string[] parts = [token, timestamp, nonce, ciphertext];
		Array.Sort(parts, StringComparer.Ordinal);
		var buffer = _encoding.GetBytes(string.Concat(parts));

		try { return SHA1.HashData(buffer); }
		finally { CryptographicOperations.ZeroMemory(buffer); }
	}
}
