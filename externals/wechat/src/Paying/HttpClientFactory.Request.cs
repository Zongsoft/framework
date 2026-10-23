using System;
using System.Linq;
using System.Text;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace Zongsoft.Externals.Wechat.Paying;

public static partial class HttpClientFactory
{
	private static async ValueTask SignAsync(IAuthority authority, HttpRequestMessage request, CancellationToken cancellation)
	{
		cancellation.ThrowIfCancellationRequested();
		var uri = request.RequestUri;

		if(uri.Scheme != Uri.UriSchemeHttps || uri.Port != 443 || uri.UserInfo.Length != 0 || uri.Fragment.Length != 0 ||
			(uri.Host != "api.mch.weixin.qq.com" && uri.Host != "api2.mch.weixin.qq.com") || !uri.AbsolutePath.StartsWith("/v3/", StringComparison.Ordinal))
			throw new ArgumentException(null, nameof(request));

		if(request.Content?.Headers.Contains("Wechatpay-Serial") == true)
			throw new ArgumentException(null, nameof(request));

		string identifier = null;
		if(request.Headers.TryGetValues("Wechatpay-Serial", out var values))
		{
			var identifiers = values.ToArray();
			if(identifiers.Length != 1 || string.IsNullOrWhiteSpace(identifiers[0]))
				throw new ArgumentException(null, nameof(request));

			identifier = identifiers[0];
		}

		var key = await authority.GetCertificateAsync(identifier, cancellation) ?? throw new CryptographicException();
		request.Headers.Remove("Wechatpay-Serial");
		request.Headers.Add("Wechatpay-Serial", key.Identifier);

		HttpContent content = request.Content;
		if(content is MultipartContent multipart)
		{
			var metadata = multipart.Where(item => item.Headers.ContentDisposition?.Name?.Trim('"') == "meta").ToArray();
			if(metadata.Length != 1 || metadata[0].Headers.ContentType?.MediaType != "application/json")
				throw new ArgumentException(null, nameof(request));

			content = metadata[0];
		}

		//HttpContent 缓冲序列化结果；后续发送复用相同字节，上传只缓冲 meta。
		var body = content == null ? Array.Empty<byte>() : await content.ReadAsByteArrayAsync(cancellation);
		var nonce = Guid.NewGuid().ToString("N");
		var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
		var message = GetMessage($"{request.Method.Method}\n{uri.PathAndQuery}\n{timestamp}\n{nonce}\n", body);
		var signature = Convert.ToBase64String(authority.Signing.Sign(message));
		request.Headers.Authorization = new AuthenticationHeaderValue("WECHATPAY2-SHA256-RSA2048", $"mchid=\"{authority.Signing.MerchantId}\",nonce_str=\"{nonce}\",timestamp=\"{timestamp}\",serial_no=\"{authority.Signing.SerialNumber}\",signature=\"{signature}\"");
	}

	private static byte[] GetMessage(string prefix, byte[] body)
	{
		var start = Encoding.UTF8.GetBytes(prefix);
		var result = new byte[start.Length + body.Length + 1];
		start.CopyTo(result, 0);
		body.CopyTo(result, start.Length);
		result[^1] = (byte)'\n';
		return result;
	}
}
