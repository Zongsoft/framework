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
using System.Text;
using System.Buffers.Binary;
using System.Security.Cryptography;

namespace Zongsoft.Externals.Wechat.Platforms;

public static partial class CryptographyUtility
{
	#region 静态字段
	private static readonly UTF8Encoding _encoding = new(false, true);
	#endregion

	#region 公共方法
	public static string Encrypt(string plaintext, string encodingAESKey, string appId)
	{
		ArgumentNullException.ThrowIfNull(plaintext);
		var buffer = _encoding.GetBytes(plaintext);

		try { return Encrypt(buffer, encodingAESKey, appId); }
		finally { CryptographicOperations.ZeroMemory(buffer); }
	}

	public static string Encrypt(ReadOnlySpan<byte> plaintext, string encodingAESKey, string appId)
	{
		ArgumentException.ThrowIfNullOrEmpty(appId);
		var receiver = _encoding.GetBytes(appId);
		var length = checked(20 + plaintext.Length + receiver.Length);
		var buffer = new byte[checked(length + 32 - length % 32)];
		var key = GetKey(encodingAESKey);

		try
		{
			RandomNumberGenerator.Fill(buffer.AsSpan(0, 16));
			BinaryPrimitives.WriteInt32BigEndian(buffer.AsSpan(16, 4), plaintext.Length);
			plaintext.CopyTo(buffer.AsSpan(20));
			receiver.CopyTo(buffer.AsSpan(20 + plaintext.Length));
			buffer.AsSpan(length).Fill((byte)(buffer.Length - length));
			using var cipher = Aes.Create();
			cipher.Key = key;
			return Convert.ToBase64String(cipher.EncryptCbc(buffer, key.AsSpan(0, 16), PaddingMode.None));
		}
		finally
		{
			CryptographicOperations.ZeroMemory(key);
			CryptographicOperations.ZeroMemory(buffer);
		}
	}

	public static string Decrypt(string ciphertext, string encodingAESKey, string appId)
	{
		var buffer = DecryptBytes(ciphertext, encodingAESKey, appId);

		try { return _encoding.GetString(buffer); }
		finally { CryptographicOperations.ZeroMemory(buffer); }
	}

	public static byte[] DecryptBytes(string ciphertext, string encodingAESKey, string appId)
	{
		ArgumentException.ThrowIfNullOrEmpty(ciphertext);
		ArgumentException.ThrowIfNullOrEmpty(appId);
		var encrypted = Convert.FromBase64String(ciphertext);

		if(encrypted.Length == 0 || encrypted.Length % 32 != 0)
			throw new CryptographicException();

		var receiver = _encoding.GetBytes(appId);
		var key = GetKey(encodingAESKey);
		byte[] buffer = null;

		try
		{
			using var cipher = Aes.Create();
			cipher.Key = key;
			buffer = cipher.DecryptCbc(encrypted, key.AsSpan(0, 16), PaddingMode.None);
			var padding = buffer[^1];

			if(padding == 0 || padding > 32 || padding > buffer.Length - 20)
				throw new CryptographicException();

			for(var index = buffer.Length - padding; index < buffer.Length; index++)
			{
				if(buffer[index] != padding)
					throw new CryptographicException();
			}

			var length = BinaryPrimitives.ReadInt32BigEndian(buffer.AsSpan(16, 4));
			var available = buffer.Length - padding - 20;

			if(length < 0 || length > available || !CryptographicOperations.FixedTimeEquals(buffer.AsSpan(20 + length, available - length), receiver))
				throw new CryptographicException();

			return buffer.AsSpan(20, length).ToArray();
		}
		finally
		{
			CryptographicOperations.ZeroMemory(key);

			if(buffer != null)
				CryptographicOperations.ZeroMemory(buffer);
		}
	}
	#endregion

	#region 私有方法
	private static byte[] GetKey(string encodingAESKey)
	{
		ArgumentException.ThrowIfNullOrEmpty(encodingAESKey);

		if(encodingAESKey.Length != 43)
			throw new ArgumentException(null, nameof(encodingAESKey));

		Span<byte> buffer = stackalloc byte[32];

		if(!Convert.TryFromBase64String(encodingAESKey + "=", buffer, out var length) || length != 32 || Convert.ToBase64String(buffer) != encodingAESKey + "=")
			throw new ArgumentException(null, nameof(encodingAESKey));

		try { return buffer.ToArray(); }
		finally { CryptographicOperations.ZeroMemory(buffer); }
	}
	#endregion
}
