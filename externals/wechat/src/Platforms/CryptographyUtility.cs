/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   Tencent (https://developers.weixin.qq.com/doc/oplatform/Third-party_Platforms/Message_Encryption/Technical_Plan.html)
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
using System.Net;
using System.Text;
using System.Security.Cryptography;

namespace Zongsoft.Externals.Wechat.Platforms
{
	public static class CryptographyUtility
	{
		#region 公共方法
		public static uint HostToNetworkOrder(uint inval)
		{
			uint outval = 0;

			for (int i = 0; i < 4; i++)
				outval = (outval << 8) + ((inval >> (i * 8)) & 255);

			return outval;
		}

		public static int HostToNetworkOrder(int inval)
		{
			int outval = 0;

			for (int i = 0; i < 4; i++)
				outval = (outval << 8) + ((inval >> (i * 8)) & 255);

			return outval;
		}

		public static string Decrypt(string ciphertext, string encodingAESKey, out string appId)
		{
			var key = Convert.FromBase64String(encodingAESKey + "=");
			var iv = new byte[16];
			Array.Copy(key, iv, 16);
			byte[] btmpMsg = AES_Decrypt(ciphertext, iv, key);

			int len = BitConverter.ToInt32(btmpMsg, 16);
			len = IPAddress.NetworkToHostOrder(len);

			var bMsg = new byte[len];
			var bAppid = new byte[btmpMsg.Length - 20 - len];
			Array.Copy(btmpMsg, 20, bMsg, 0, len);
			Array.Copy(btmpMsg, 20 + len, bAppid, 0, btmpMsg.Length - 20 - len);
			string oriMsg = Encoding.UTF8.GetString(bMsg);
			appId = Encoding.UTF8.GetString(bAppid);

			return oriMsg;
		}

		public static string Encrypt(string plaintext, string encodingAESKey, string appId)
		{
			var key = Convert.FromBase64String(encodingAESKey + "=");
			var iv = new byte[16];
			Array.Copy(key, iv, 16);
			var bRand = Zongsoft.Common.Randomizer.Generate(16);
			var bAppid = Encoding.UTF8.GetBytes(appId);
			var btmpMsg = Encoding.UTF8.GetBytes(plaintext);
			var bMsgLen = BitConverter.GetBytes(HostToNetworkOrder(btmpMsg.Length));
			var bMsg = new byte[bRand.Length + bMsgLen.Length + bAppid.Length + btmpMsg.Length];

			Array.Copy(bRand, bMsg, bRand.Length);
			Array.Copy(bMsgLen, 0, bMsg, bRand.Length, bMsgLen.Length);
			Array.Copy(btmpMsg, 0, bMsg, bRand.Length + bMsgLen.Length, btmpMsg.Length);
			Array.Copy(bAppid, 0, bMsg, bRand.Length + bMsgLen.Length + btmpMsg.Length, bAppid.Length);

			return AES_Encrypt(bMsg, iv, key);
		}
		#endregion

		#region 私有方法
		private static string AES_Encrypt(string Input, byte[] iv, byte[] key)
		{
			using var aes = new RijndaelManaged();
			aes.KeySize = 256;
			aes.BlockSize = 128;
			aes.Padding = PaddingMode.PKCS7;
			aes.Mode = CipherMode.CBC;
			aes.Key = key;
			aes.IV = iv;
			var encrypt = aes.CreateEncryptor(aes.Key, aes.IV);
			byte[] xBuff = null;

			using (var ms = new MemoryStream())
			{
				using (var cs = new CryptoStream(ms, encrypt, CryptoStreamMode.Write))
				{
					var xXml = Encoding.UTF8.GetBytes(Input);
					cs.Write(xXml, 0, xXml.Length);
				}

				xBuff = ms.ToArray();
			}

			return Convert.ToBase64String(xBuff);
		}

		private static string AES_Encrypt(byte[] Input, byte[] iv, byte[] key)
		{
			using var aes = new RijndaelManaged();
			//秘钥的大小，以位为单位
			aes.KeySize = 256;
			//支持的块大小
			aes.BlockSize = 128;
			//填充模式
			//aes.Padding = PaddingMode.PKCS7;
			aes.Padding = PaddingMode.None;
			aes.Mode = CipherMode.CBC;
			aes.Key = key;
			aes.IV = iv;
			var encrypt = aes.CreateEncryptor(aes.Key, aes.IV);
			byte[] xBuff = null;

			#region 自己进行PKCS7补位，用系统自己带的不行
			byte[] msg = new byte[Input.Length + 32 - Input.Length % 32];
			Array.Copy(Input, msg, Input.Length);
			byte[] pad = KCS7Encoder(Input.Length);
			Array.Copy(pad, 0, msg, Input.Length, pad.Length);
			#endregion

			#region 注释的也是一种方法，效果一样
			//ICryptoTransform transform = aes.CreateEncryptor();
			//byte[] xBuff = transform.TransformFinalBlock(msg, 0, msg.Length);
			#endregion

			using (var ms = new MemoryStream())
			{
				using (var cs = new CryptoStream(ms, encrypt, CryptoStreamMode.Write))
				{
					cs.Write(msg, 0, msg.Length);
				}

				xBuff = ms.ToArray();
			}

			return Convert.ToBase64String(xBuff);
		}

		private static byte[] KCS7Encoder(int text_length)
		{
			int block_size = 32;
			// 计算需要填充的位数
			int amount_to_pad = block_size - (text_length % block_size);
			if (amount_to_pad == 0)
			{
				amount_to_pad = block_size;
			}
			// 获得补位所用的字符
			char pad_chr = GetCharacter(amount_to_pad);
			string tmp = "";
			for (int index = 0; index < amount_to_pad; index++)
			{
				tmp += pad_chr;
			}

			return Encoding.UTF8.GetBytes(tmp);
		}

		static char GetCharacter(int a)
		{
			byte target = (byte)(a & 0xFF);
			return (char)target;
		}

		private static byte[] AES_Decrypt(string text, byte[] iv, byte[] key)
		{
			using var aes = new RijndaelManaged();
			aes.KeySize = 256;
			aes.BlockSize = 128;
			aes.Mode = CipherMode.CBC;
			aes.Padding = PaddingMode.None;
			aes.Key = key;
			aes.IV = iv;
			var decrypt = aes.CreateDecryptor(aes.Key, aes.IV);
			byte[] xBuff = null;

			using (var ms = new MemoryStream())
			{
				using (var cs = new CryptoStream(ms, decrypt, CryptoStreamMode.Write))
				{
					byte[] xXml = Convert.FromBase64String(text);
					byte[] msg = new byte[xXml.Length + 32 - xXml.Length % 32];
					Array.Copy(xXml, msg, xXml.Length);
					cs.Write(xXml, 0, xXml.Length);
				}
				xBuff = AES_Decode(ms.ToArray());
			}
			return xBuff;
		}

		private static byte[] AES_Decode(byte[] decrypted)
		{
			int pad = (int)decrypted[decrypted.Length - 1];
			if (pad < 1 || pad > 32)
			{
				pad = 0;
			}
			byte[] res = new byte[decrypted.Length - pad];
			Array.Copy(decrypted, 0, res, 0, decrypted.Length - pad);
			return res;
		}
		#endregion
	}
}
