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
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Security;
using Zongsoft.Services;

namespace Zongsoft.Externals.Wechat.Paying
{
	public static class CertificateUtility
	{
		private static Certificate _certificate;

		public static async Task<ICertificate> GetCertificateAsync(this IAuthority authority, CancellationToken cancellation = default)
		{
			if(_certificate != null && _certificate.Validity.IsValidate(DateTime.UtcNow) && _certificate.Validity.Final > DateTime.UtcNow.AddHours(36))
				return _certificate;

			var client = authority.GetHttpClient();
			var response = await client.GetAsync("https://api.mch.weixin.qq.com/v3/certificates", cancellation);

			if(!response.IsSuccessStatusCode)
				return _certificate;

			var infos = (await response.Content.ReadFromJsonAsync<CertificateResult>(null, cancellation)).Value;

			if(infos == null || infos.Length == 0)
				return _certificate;

			CertificateResult.CertificateInfo? latest = null;

			foreach(var info in infos)
			{
				if(latest == null)
				{
					latest = info;
					continue;
				}

				if(info.Effective <= DateTime.UtcNow && info.Expiration >= DateTime.UtcNow)
				{
					if(info.Expiration > latest.Value.Expiration)
					{
						latest = info;
						continue;
					}
				}
			}

			return _certificate = new Certificate(latest.Value.SerialNo, latest.Value.SerialNo, "RSA", publicKey: authority.GetCertificatePublicKey(latest.Value.Data))
			{
				Validity = new CertificateValidity(latest.Value.Effective, latest.Value.Expiration),
			};
		}

		private static byte[] GetCertificatePublicKey(this IAuthority authority, CertificateResult.CertificateData data)
		{
			return CryptographyHelper.Decrypt1(
				Encoding.UTF8.GetBytes(authority.Secret),
				Encoding.UTF8.GetBytes(data.Nonce),
				Encoding.UTF8.GetBytes(data.AssociatedData),
				Convert.FromBase64String(data.Ciphertext));
		}

		private struct CertificateResult
		{
			[JsonPropertyName("data")]
			public CertificateInfo[] Value { get; set; }

			public struct CertificateInfo
			{
				[JsonPropertyName("serial_no")]
				public string SerialNo { get; set; }

				[JsonPropertyName("effective_time ")]
				public DateTime Effective { get; set; }

				[JsonPropertyName("expire_time ")]
				public DateTime Expiration { get; set; }

				[JsonPropertyName("encrypt_certificate")]
				public CertificateData Data { get; set; }
			}

			public struct CertificateData
			{
				[JsonPropertyName("algorithm")]
				public string Algorithm { get; set; }

				[JsonPropertyName("nonce")]
				public string Nonce { get; set; }

				[JsonPropertyName("associated_data")]
				public string AssociatedData { get; set; }

				[JsonPropertyName("ciphertext")]
				public string Ciphertext { get; set; }
			}
		}
	}
}
