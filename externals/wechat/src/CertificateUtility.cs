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
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http.Json;

using Zongsoft.Security;

namespace Zongsoft.Externals.Wechat
{
	public static class CertificateUtility
	{
		#region 私有变量
		private static Certificate _certificate;
		private static Certificate _transitory;
		private static DateTime _timestamp;
		#endregion

		#region 公共方法
		/// <summary>
		/// 获取微信平台的数字证书。
		/// </summary>
		/// <param name="authority">获取凭证证书的机构，即指定以哪个机构的身份来获取平台证书。</param>
		/// <param name="cancellation">异步任务的取消标记。</param>
		/// <returns>返回的微信平台的数字证书。</returns>
		public static ValueTask<Certificate> GetCertificateAsync(this IAuthority authority, CancellationToken cancellation = default)
		{
			if(_certificate != null && _certificate.Validity.IsValidate(DateTime.UtcNow) && _certificate.Validity.Final > DateTime.UtcNow.AddHours(36))
				return ValueTask.FromResult(_certificate);

			return AcquireCertificateAsync(authority, cancellation);
		}

		/// <summary>
		/// 获取微信平台的数字证书。
		/// </summary>
		/// <param name="authority">获取凭证证书的机构，即指定以哪个机构的身份来获取平台证书。</param>
		/// <param name="code">获取对应凭证证书的代号。</param>
		/// <param name="cancellation">异步任务的取消标记。</param>
		/// <returns>返回的微信平台的数字证书。</returns>
		public static async ValueTask<Certificate> GetCertificateAsync(this IAuthority authority, string code, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(code))
				return await GetCertificateAsync(authority, cancellation);

			//在现有缓存中查找指定的凭证
			var found = Find(code);

			//如果找到或者凭证刷新时间距离此刻小于特定时长则直接返回
			if(found != null || (DateTime.Now - _timestamp).TotalHours < 4)
				return found;

			//刷新获取最新的微信平台数字证书
			await AcquireCertificateAsync(authority, cancellation);

			//从最新的缓存中查找指定的凭证
			return Find(code);
		}

		private static async ValueTask<Certificate> AcquireCertificateAsync(this IAuthority authority, CancellationToken cancellation = default)
		{
			if(authority == null)
				throw new ArgumentNullException(nameof(authority));

			var response = await Paying.HttpClientFactory.GetHttpClient(authority.Certificate).GetAsync("https://api.mch.weixin.qq.com/v3/certificates", cancellation);

			if(!response.IsSuccessStatusCode)
				return _certificate;

			//更新获取的时间
			_timestamp = DateTime.Now;

			var infos = (await response.Content.ReadFromJsonAsync<CertificateResult>(null, cancellation)).Value;

			if(infos == null || infos.Length == 0)
				return _certificate ?? _transitory;

			if(infos.Length == 1)
				return _certificate = Create(authority, infos[0]);

			var sequences = infos.OrderBy(p => p.Expiration);
			_transitory = Create(authority, sequences.FirstOrDefault());
			return _certificate = Create(authority, sequences.LastOrDefault());

			static Certificate Create(IAuthority authority, CertificateResult.CertificateInfo info) =>
				new (info.SerialNo, info.SerialNo, "X509", publicKey: GetCertificatePublicKey(authority, info.Data))
				{
					Validity = new CertificateValidity(info.Effective, info.Expiration),
				};
		}
		#endregion

		#region 私有方法
		private static Certificate Find(string code)
		{
			if(_certificate != null && string.Equals(_certificate.Code, code))
				return _certificate;
			if(_transitory != null && string.Equals(_transitory.Code, code))
				return _transitory;

			return null;
		}

		private static byte[] GetCertificatePublicKey(IAuthority authority, CertificateResult.CertificateData data)
		{
			return CryptographyHelper.Decrypt1(
				Encoding.UTF8.GetBytes(authority.Secret),
				Encoding.UTF8.GetBytes(data.Nonce),
				Encoding.UTF8.GetBytes(data.AssociatedData),
				Convert.FromBase64String(data.Ciphertext));
		}
		#endregion

		#region 嵌套结构
		private struct CertificateResult
		{
			[JsonPropertyName("data")]
			public CertificateInfo[] Value { get; set; }

			public struct CertificateInfo
			{
				[JsonPropertyName("serial_no")]
				public string SerialNo { get; set; }

				[JsonPropertyName("effective_time")]
				public DateTime Effective { get; set; }

				[JsonPropertyName("expire_time")]
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
		#endregion
	}
}
