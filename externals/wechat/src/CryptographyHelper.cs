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
using System.Text;
using System.Security.Cryptography;

namespace Zongsoft.Externals.Wechat;

public static class CryptographyHelper
{
	private const int TAG_SIZE = 16;

	public static byte[] Decrypt1(string key, string nonce, string associatedData, string ciphertext) => Decrypt1
	(
		Encoding.UTF8.GetBytes(key),
		Encoding.UTF8.GetBytes(nonce),
		string.IsNullOrEmpty(associatedData) ? [] : Encoding.UTF8.GetBytes(associatedData),
		Convert.FromBase64String(ciphertext)
	);

	public static byte[] Decrypt1(byte[] key, byte[] nonce, byte[] associatedData, byte[] ciphertext)
	{
		ArgumentNullException.ThrowIfNull(key);
		ArgumentNullException.ThrowIfNull(nonce);
		ArgumentNullException.ThrowIfNull(ciphertext);

		if(key.Length != 32)
			throw new ArgumentException(null, nameof(key));
		if(nonce.Length != 12)
			throw new ArgumentException(null, nameof(nonce));
		if(ciphertext.Length < TAG_SIZE)
			throw new ArgumentException(null, nameof(ciphertext));

		//APIv3 密文的末尾 16 字节为认证标签，关联数据参与认证但不属于明文。
		var plaintext = new byte[ciphertext.Length - TAG_SIZE];
		using var cipher = new AesGcm(key, TAG_SIZE);
		cipher.Decrypt(nonce, ciphertext.AsSpan(0, plaintext.Length), ciphertext.AsSpan(plaintext.Length), plaintext, associatedData);

		return plaintext;
	}

	public static byte[] Decrypt2(string key, string nonce, string associatedData, string ciphertext) => Decrypt1(key, nonce, associatedData, ciphertext);
	public static byte[] Decrypt2(byte[] key, byte[] nonce, byte[] associatedData, byte[] ciphertext) => Decrypt1(key, nonce, associatedData, ciphertext);

}
