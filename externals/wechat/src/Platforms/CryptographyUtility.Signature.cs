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
