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
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;

namespace Zongsoft.Externals.Wechat;

public static class CertificateUtility
{
	#region 静态字段
	private static readonly SocketsHttpHandler _handler = new() { PooledConnectionLifetime = TimeSpan.FromMinutes(5) };
	#endregion

	#region 公共方法
	public static ValueTask<Authority.VerificationKey> GetCertificateAsync(this IAuthority authority, CancellationToken cancellation = default) => GetCertificateAsync(authority, null, cancellation);
	public static ValueTask<Authority.VerificationKey> GetCertificateAsync(this IAuthority authority, string identifier, CancellationToken cancellation = default)
	{
		ArgumentNullException.ThrowIfNull(authority);
		cancellation.ThrowIfCancellationRequested();

		if(authority.Verification == null)
			throw new InvalidOperationException();

		return authority.Verification.GetAsync(authority, identifier, cancellation);
	}

	public static ValueTask RefreshCertificatesAsync(this IAuthority authority, CancellationToken cancellation = default)
	{
		ArgumentNullException.ThrowIfNull(authority);

		if(authority.Verification == null)
			throw new InvalidOperationException();

		return authority.Verification.RefreshAsync(authority, cancellation);
	}
	#endregion

	#region 内部方法
	internal static async Task<Authority.VerificationKey[]> DownloadAsync(IAuthority authority)
	{
		if(authority.Signing == null || string.IsNullOrEmpty(authority.ApiV3Key))
			throw new InvalidOperationException();

		var factory = Services.ApplicationContext.Current?.Services.GetService(typeof(IHttpClientFactory)) as IHttpClientFactory;
		using var client = factory?.CreateClient("Wechat.Certificates") ?? new HttpClient(_handler, false);
		using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
		using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.mch.weixin.qq.com/v3/certificates");

		var nonce = Guid.NewGuid().ToString("N");
		var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
		var signature = Convert.ToBase64String(authority.Signing.Sign(Encoding.UTF8.GetBytes($"GET\n/v3/certificates\n{timestamp}\n{nonce}\n\n")));

		request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
		request.Headers.Authorization = new AuthenticationHeaderValue("WECHATPAY2-SHA256-RSA2048", $"mchid=\"{authority.Code}\",nonce_str=\"{nonce}\",timestamp=\"{timestamp}\",serial_no=\"{authority.Signing.SerialNumber}\",signature=\"{signature}\"");

		using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
		response.EnsureSuccessStatusCode();

		var body = await ReadAsync(response.Content, timeout.Token);
		var serial = GetHeader(response, "Wechatpay-Serial");
		var responseTimestamp = GetHeader(response, "Wechatpay-Timestamp");
		var responseNonce = GetHeader(response, "Wechatpay-Nonce");
		var responseSignature = GetHeader(response, "Wechatpay-Signature");
		var trusted = authority.Verification.Find(serial);

		if(trusted == null || !long.TryParse(responseTimestamp, NumberStyles.None, CultureInfo.InvariantCulture, out var seconds) ||
		   seconds < DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 300 || seconds > DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 300)
			throw new CryptographicException();

		var prefix = Encoding.UTF8.GetBytes($"{responseTimestamp}\n{responseNonce}\n");
		var message = new byte[prefix.Length + body.Length + 1];
		prefix.CopyTo(message, 0);
		body.CopyTo(message, prefix.Length);
		message[^1] = (byte)'\n';

		if(!trusted.Verify(message, Convert.FromBase64String(responseSignature)))
			throw new CryptographicException();

		var result = JsonSerializer.Deserialize<CertificateResult>(body);
		if(result?.Data == null || result.Data.Length == 0 || result.Data.Length > 32)
			throw new CryptographicException();

		var keys = new Authority.VerificationKey[result.Data.Length];
		for(int i = 0; i < keys.Length; i++)
		{
			var item = result.Data[i];
			if(item?.Encrypted == null || item.Encrypted.Algorithm != "AEAD_AES_256_GCM")
				throw new CryptographicException();

			var plaintext = CryptographyHelper.Decrypt1(authority.ApiV3Key, item.Encrypted.Nonce, item.Encrypted.AssociatedData, item.Encrypted.Ciphertext);
			var key = Authority.VerificationKey.FromCertificate(Encoding.UTF8.GetString(plaintext));

			if(!string.Equals(key.Identifier, item.SerialNumber, StringComparison.Ordinal) || keys.Take(i).Any(previous => previous.Identifier == key.Identifier))
				throw new CryptographicException();

			//接口的弃用时间可以早于 X.509 的失效时间，二者取交集。
			keys[i] = key.Restrict(item.Effective == default ? key.Validity.Start.Value : item.Effective, item.Expiration == default ? key.Validity.Final.Value : item.Expiration);
		}

		return keys;
	}
	#endregion

	#region 私有方法
	private static string GetHeader(HttpResponseMessage response, string name)
	{
		if(!response.Headers.TryGetValues(name, out var values))
			throw new CryptographicException();

		var items = values.ToArray();
		if(items.Length != 1 || string.IsNullOrWhiteSpace(items[0]))
			throw new CryptographicException();

		return items[0];
	}

	private static async ValueTask<byte[]> ReadAsync(HttpContent content, CancellationToken cancellation)
	{
		const int LIMIT = 4 * 1024 * 1024;

		await using var stream = await content.ReadAsStreamAsync(cancellation);
		using var buffer = new MemoryStream();
		var block = new byte[8192];
		int count;

		while((count = await stream.ReadAsync(block, cancellation)) > 0)
		{
			if(buffer.Length + count > LIMIT)
				throw new CryptographicException();

			buffer.Write(block, 0, count);
		}

		return buffer.ToArray();
	}
	#endregion

	#region 嵌套类型
	private sealed class CertificateResult
	{
		[JsonPropertyName("data")]
		public CertificateInfo[] Data { get; set; }
	}

	private sealed class CertificateInfo
	{
		[JsonPropertyName("serial_no")]
		public string SerialNumber { get; set; }
		[JsonPropertyName("effective_time")]
		public DateTime Effective { get; set; }
		[JsonPropertyName("expire_time")]
		public DateTime Expiration { get; set; }
		[JsonPropertyName("encrypt_certificate")]
		public EncryptedCertificate Encrypted { get; set; }
	}

	private sealed class EncryptedCertificate
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
	#endregion
}
