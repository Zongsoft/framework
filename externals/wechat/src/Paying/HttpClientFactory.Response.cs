using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace Zongsoft.Externals.Wechat.Paying;

public static partial class HttpClientFactory
{
	private static async ValueTask<bool> VerifyAsync(IAuthority authority, HttpResponseMessage response, byte[] body, CancellationToken cancellation)
	{
		//无签名的网关错误不提供可信业务错误码；任何成功响应都必须验签。
		if(!response.IsSuccessStatusCode && !response.Headers.Any(header => header.Key.StartsWith("Wechatpay-", StringComparison.OrdinalIgnoreCase)))
			return false;

		var serial = GetHeader(response, "Wechatpay-Serial");
		var timestamp = GetHeader(response, "Wechatpay-Timestamp");
		var nonce = GetHeader(response, "Wechatpay-Nonce");
		var signature = GetHeader(response, "Wechatpay-Signature");

		if(response.Headers.Contains("Wechatpay-Signature-Type") && GetHeader(response, "Wechatpay-Signature-Type") != "WECHATPAY2-SHA256-RSA2048")
			throw new CryptographicException();

		var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
		if(!long.TryParse(timestamp, NumberStyles.None, CultureInfo.InvariantCulture, out var seconds) || seconds < now - 300 || seconds > now + 300)
			throw new CryptographicException();

		byte[] decoded;

		try
		{
			decoded = Convert.FromBase64String(signature);
		}
		catch(FormatException exception)
		{
			throw new CryptographicException(null, exception);
		}

		var key = await authority.GetCertificateAsync(serial, cancellation);
		if(key == null || !key.Verify(GetMessage($"{timestamp}\n{nonce}\n", body), decoded))
			throw new CryptographicException();

		return true;
	}

	private static string GetHeader(HttpResponseMessage response, string name)
	{
		if(!response.Headers.TryGetValues(name, out var values))
			throw new CryptographicException();

		var items = values.ToArray();
		if(items.Length != 1 || string.IsNullOrWhiteSpace(items[0]) || items[0].Length > 1024 || items[0].Contains('\r') || items[0].Contains('\n'))
			throw new CryptographicException();

		return items[0];
	}

	private static async ValueTask<byte[]> ReadAsync(HttpContent content, CancellationToken cancellation)
	{
		const int LIMIT = 4 * 1024 * 1024;

		if(content.Headers.ContentLength > LIMIT)
			throw new CryptographicException();

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
}
