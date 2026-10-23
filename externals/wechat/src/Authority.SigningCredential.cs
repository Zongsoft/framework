using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using Zongsoft.Security;

namespace Zongsoft.Externals.Wechat;

public partial class Authority
{
	/// <summary>不可变的商户签名身份；商户号、证书序列号和私钥作为整体使用。</summary>
	public sealed class SigningCredential
	{
		#region 成员字段
		private readonly byte[] _privateKey;
		private readonly byte[] _certificate;
		#endregion

		#region 构造函数
		public SigningCredential(string merchantId, string serialNumber, string privateKey, CertificateValidity validity = default, string certificate = null)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(merchantId);
			ArgumentException.ThrowIfNullOrWhiteSpace(serialNumber);
			ArgumentException.ThrowIfNullOrWhiteSpace(privateKey);

			if(!IsSerialNumber(serialNumber))
				throw new ArgumentException(null, nameof(serialNumber));

			using var rsa = RSA.Create();
			rsa.ImportFromPem(privateKey);

			if(rsa.KeySize != 2048)
				throw new ArgumentException(null, nameof(privateKey));

			if(!string.IsNullOrEmpty(certificate))
			{
				using var x509 = X509Certificate2.CreateFromPem(certificate);
				using var publicKey = x509.GetRSAPublicKey();
				if(!string.Equals(x509.SerialNumber, serialNumber, StringComparison.OrdinalIgnoreCase) || publicKey == null ||
					!publicKey.ExportSubjectPublicKeyInfo().AsSpan().SequenceEqual(rsa.ExportSubjectPublicKeyInfo()))
					throw new CryptographicException();

				_certificate = x509.RawData;
				var start = x509.NotBefore.ToUniversalTime();
				var final = x509.NotAfter.ToUniversalTime();
				validity = new(validity.Start.HasValue && validity.Start.Value > start ? validity.Start.Value : start,
					validity.Final.HasValue && validity.Final.Value < final ? validity.Final.Value : final);
			}

			_privateKey = rsa.ExportPkcs8PrivateKey();
			this.MerchantId = merchantId;
			this.SerialNumber = serialNumber.ToUpperInvariant();
			this.Validity = validity;
		}
		#endregion

		#region 公共属性
		public string MerchantId { get; }
		public string SerialNumber { get; }
		public CertificateValidity Validity { get; }
		#endregion

		#region 公共方法
		public byte[] Sign(ReadOnlySpan<byte> data)
		{
			if(!this.Validity.IsValid())
				throw new CryptographicException();

			using var rsa = RSA.Create();
			rsa.ImportPkcs8PrivateKey(_privateKey, out _);
			return rsa.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
		}

		internal byte[] Decrypt(ReadOnlySpan<byte> data)
		{
			if(!this.Validity.IsValid())
				throw new CryptographicException();

			using var rsa = RSA.Create();
			rsa.ImportPkcs8PrivateKey(_privateKey, out _);
			return rsa.Decrypt(data, RSAEncryptionPadding.OaepSHA1);
		}

		internal X509Certificate2 CreateClientCertificate()
		{
			if(_certificate == null || !this.Validity.IsValid())
				throw new InvalidOperationException();

			using var rsa = RSA.Create();
			rsa.ImportPkcs8PrivateKey(_privateKey, out _);
			using var certificate = X509Certificate2.CreateFromPem(PemEncoding.WriteString("CERTIFICATE", _certificate));
			return certificate.CopyWithPrivateKey(rsa);
		}

		/// <summary>复制已加载的商户私钥；不接管或释放调用方证书。</summary>
		/// <param name="merchantId">签名所属的商户号。</param>
		/// <param name="certificate">已由调用方加载的商户证书。</param>
		/// <param name="publicCertificate">可选的配套商户 X.509 公共证书 PEM。</param>
		/// <returns>独立持有可导出密钥材料的签名身份。</returns>
		public static SigningCredential FromCertificate(string merchantId, ICertificate certificate, string publicCertificate = null)
		{
			ArgumentNullException.ThrowIfNull(certificate);

			if(certificate.GetProtocol() is RSA rsa)
				return new(merchantId, certificate.Identifier, rsa.ExportPkcs8PrivateKeyPem(), certificate.Validity, publicCertificate);

			if(certificate.GetProtocol() is X509Certificate2 x509)
			{
				using var key = x509.GetRSAPrivateKey();
				if(key == null)
					throw new ArgumentException(null, nameof(certificate));

				return new(merchantId, x509.SerialNumber, key.ExportPkcs8PrivateKeyPem(), new(x509.NotBefore, x509.NotAfter), publicCertificate ?? x509.ExportCertificatePem());
			}

			throw new ArgumentException(null, nameof(certificate));
		}
		#endregion
	}

	private static bool IsSerialNumber(string value)
	{
		if(string.IsNullOrEmpty(value))
			return false;

		foreach(var character in value)
		{
			if(!Uri.IsHexDigit(character))
				return false;
		}

		return true;
	}
}
