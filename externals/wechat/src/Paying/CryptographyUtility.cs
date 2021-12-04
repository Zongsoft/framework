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
using System.Security.Cryptography;

using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace Zongsoft.Externals.Wechat.Paying
{
	public class CryptographyUtility
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
    }
}
