/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
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
using System.IO;
using System.Text;
using System.Security.Cryptography;

using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace Zongsoft.Externals.Wechat
{
	public static class CryptographyHelper
	{
		private const string ALGORITHM = "AES/GCM/NoPadding";
		private static readonly IAeadCipher _cipher1 = new GcmBlockCipher(new AesEngine());
		private static readonly IBufferedCipher _cipher2 = CipherUtilities.GetCipher(ALGORITHM);

		public static byte[] Decrypt1(byte[] key, byte[] nonce, byte[] associatedData, byte[] ciphertext)
		{
			var cipher = _cipher1;
			var parameters = new AeadParameters(new KeyParameter(key), 128, nonce, associatedData);

			cipher.Init(false, parameters);
			var plaintext = new byte[cipher.GetOutputSize(ciphertext.Length)];
			var length = cipher.ProcessBytes(ciphertext, 0, ciphertext.Length, plaintext, 0);
			cipher.DoFinal(plaintext, length);

			return plaintext;
		}

		public static byte[] Decrypt2(byte[] key, byte[] nonce, byte[] associatedData, byte[] ciphertext)
		{
			var cipher = _cipher2;
			var parameters = new AeadParameters(new KeyParameter(key), 128, nonce, associatedData);

			cipher.Init(false, parameters);
			var plaintext = new byte[cipher.GetOutputSize(ciphertext.Length)];
			var length = cipher.ProcessBytes(ciphertext, 0, ciphertext.Length, plaintext, 0);
			cipher.DoFinal(plaintext, length);

			return plaintext;
		}

		public static class Obsolescent
		{
			public static byte[] Encrypt(string identity, string password, byte[] data)
			{
				var key = Convert.FromBase64String(password + "=");
				var iv = new byte[16];
				Array.Copy(key, iv, 16);

				string nonce = Common.Randomizer.GenerateString(16);
				byte[] nonceArray = Encoding.UTF8.GetBytes(nonce);
				byte[] identityArray = Encoding.UTF8.GetBytes(identity);
				byte[] lengthArray = BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder(data.Length));
				byte[] buffer = new byte[nonceArray.Length + lengthArray.Length + identityArray.Length + data.Length];

				Array.Copy(nonceArray, buffer, nonceArray.Length);
				Array.Copy(lengthArray, 0, buffer, nonceArray.Length, lengthArray.Length);
				Array.Copy(data, 0, buffer, nonceArray.Length + lengthArray.Length, data.Length);
				Array.Copy(identityArray, 0, buffer, nonceArray.Length + lengthArray.Length + data.Length, identityArray.Length);

				var algorithm = new RijndaelManaged()
				{
					IV = iv,
					Key = key,
					KeySize = 256,
					BlockSize = 128,
					Mode = CipherMode.CBC,
					Padding = PaddingMode.None, //PaddingMode.PKCS7
				};

				var encryptor = algorithm.CreateEncryptor(algorithm.Key, algorithm.IV);
				byte[] xBuff = null;

				byte[] msg = new byte[buffer.Length + 32 - buffer.Length % 32];
				Array.Copy(buffer, msg, buffer.Length);
				byte[] padding = KCS7Encoder(buffer.Length);
				Array.Copy(padding, 0, msg, buffer.Length, padding.Length);

				using(var ms = new MemoryStream())
				{
					using(var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
					{
						cs.Write(msg, 0, msg.Length);
					}

					xBuff = ms.ToArray();
				}

				return xBuff;
			}

			public static byte[] Decrypt(string identity, string password, byte[] data)
			{
				throw new NotImplementedException();
			}

			#region 私有方法
			private static byte[] KCS7Encoder(int text_length)
			{
				var block_size = 32;

				// 计算需要填充的位数
				var amount_to_pad = block_size - (text_length % block_size);

				if(amount_to_pad == 0)
					amount_to_pad = block_size;

				// 获得补位所用的字符
				var pad_chr = (char)(byte)(amount_to_pad & 0xFF);
				var tmp = "";

				for(int index = 0; index < amount_to_pad; index++)
				{
					tmp += pad_chr;
				}

				return Encoding.UTF8.GetBytes(tmp);
			}
			#endregion
		}
	}
}
