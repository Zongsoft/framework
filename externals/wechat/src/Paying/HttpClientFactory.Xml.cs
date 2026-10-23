using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Collections.Generic;

namespace Zongsoft.Externals.Wechat.Paying;

public static partial class HttpClientFactory
{
	internal static class Xml
	{
		public static async ValueTask<IDictionary<string, string>> SendAsync(IAuthority authority, string url, IDictionary<string, object> data, bool mutualTls, CancellationToken cancellation)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(authority.ApiV2Key);
			cancellation.ThrowIfCancellationRequested();
			var algorithm = data.TryGetValue("sign_type", out var type) ? type as string : "MD5";
			data["sign"] = Sign(data, authority.ApiV2Key, algorithm);
			using var certificate = mutualTls ? GetCertificate(authority) : null;
			using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = data.CreateXmlContent() };
			using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
			timeout.CancelAfter(TimeSpan.FromSeconds(30));
			using var transport = CreateTransport(authority, certificate);
			using var response = await (transport ?? _transport).SendAsync(request, timeout.Token);
			var result = await response.GetXmlContentAsync(timeout.Token);
			response.EnsureSuccessStatusCode();

			if(!result.TryGetValue("return_code", out var status) || status != "SUCCESS")
				throw new Zongsoft.Common.OperationException("V2_COMMUNICATION_FAILED", null);
			if(!result.TryGetValue("sign", out var signature) ||
				result.TryGetValue("sign_type", out var responseAlgorithm) && responseAlgorithm != algorithm)
				throw new CryptographicException();

			var expected = Convert.FromHexString(Sign(result.Select(entry => new KeyValuePair<string, object>(entry.Key, entry.Value)), authority.ApiV2Key, algorithm));
			byte[] actual;

			try
			{
				actual = Convert.FromHexString(signature);
			}
			catch(FormatException)
			{
				throw new CryptographicException();
			}

			if(!CryptographicOperations.FixedTimeEquals(expected, actual))
				throw new CryptographicException();

			return result;
		}

		private static string Sign(IEnumerable<KeyValuePair<string, object>> data, string key, string algorithm)
		{
			if(algorithm == "MD5")
				return Utility.Postmark(data, key);
			if(algorithm != "HMAC-SHA256")
				throw new ArgumentException(null, nameof(algorithm));

			using var hmac = new HMACSHA256(System.Text.Encoding.UTF8.GetBytes(key));
			return Utility.Postmark(data, key, hmac);
		}

		private static X509Certificate2 GetCertificate(IAuthority authority)
		{
			if(authority.Signing == null || authority.Signing.MerchantId != authority.Code)
				throw new InvalidOperationException();

			return authority.Signing.CreateClientCertificate();
		}

		private static HttpMessageInvoker CreateTransport(IAuthority authority, X509Certificate2 certificate)
		{
			var factory = Services.ApplicationContext.Current?.Services.GetService(typeof(IHttpMessageHandlerFactory)) as IHttpMessageHandlerFactory;
			if(factory == null)
			{
				if(certificate == null)
					return null;

				var handler = new SocketsHttpHandler { AllowAutoRedirect = false };
				handler.SslOptions.ClientCertificates = new X509CertificateCollection { certificate };
				return new HttpMessageInvoker(handler);
			}

			var name = certificate == null ? "Wechat.Payments.Xml" : $"Wechat.Payments.Xml.{authority.Code}.{authority.Signing.SerialNumber}";
			var pipeline = factory.CreateHandler(name);
			var primary = pipeline;

			while(primary is DelegatingHandler delegating)
				primary = delegating.InnerHandler;

			if(primary is SocketsHttpHandler { AllowAutoRedirect: true } or HttpClientHandler { AllowAutoRedirect: true })
				throw new InvalidOperationException();

			if(certificate != null)
			{
				var configured = primary switch
				{
					SocketsHttpHandler sockets => sockets.SslOptions.ClientCertificates,
					HttpClientHandler http => http.ClientCertificates,
					_ => null,
				};

				if(primary is SocketsHttpHandler or HttpClientHandler && (configured == null || !configured.Contains(certificate)))
					throw new InvalidOperationException();
			}

			return new HttpMessageInvoker(pipeline, false);
		}
	}
}
