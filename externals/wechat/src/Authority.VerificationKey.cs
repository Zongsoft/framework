using System;
using System.Text;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using Zongsoft.Security;

namespace Zongsoft.Externals.Wechat;

public partial class Authority
{
	/// <summary>微信支付的验签和敏感字段加密材料，不持有私钥。</summary>
	public sealed class VerificationKey
	{
		#region 成员字段
		private readonly byte[] _publicKey;
		#endregion

		#region 构造函数
		private VerificationKey(string identifier, VerificationMode mode, byte[] publicKey, CertificateValidity validity)
		{
			this.Identifier = identifier;
			this.Mode = mode;
			this.Validity = validity;
			_publicKey = publicKey;
		}
		#endregion

		#region 公共属性
		public string Identifier { get; }
		public VerificationMode Mode { get; }
		public CertificateValidity Validity { get; }
		#endregion

		#region 公共方法
		public static VerificationKey FromPublicKey(string identifier, string pem)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
			ArgumentException.ThrowIfNullOrWhiteSpace(pem);

			if(!identifier.StartsWith("PUB_KEY_ID_", StringComparison.Ordinal) || identifier.Length <= 11)
				throw new ArgumentException(null, nameof(identifier));
			if(!pem.Contains("-----BEGIN PUBLIC KEY-----", StringComparison.Ordinal) || pem.Contains("PRIVATE KEY", StringComparison.Ordinal))
				throw new ArgumentException(null, nameof(pem));

			using var rsa = RSA.Create();
			rsa.ImportFromPem(pem);

			if(rsa.KeySize != 2048)
				throw new ArgumentException(null, nameof(pem));

			return new(identifier, VerificationMode.PublicKey, rsa.ExportSubjectPublicKeyInfo(), default);
		}

		public static VerificationKey FromCertificate(string pem)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(pem);

			if(pem.Contains("PRIVATE KEY", StringComparison.Ordinal))
				throw new ArgumentException(null, nameof(pem));

			using var certificate = X509Certificate2.CreateFromPem(pem);
			using var rsa = certificate.GetRSAPublicKey();

			if(rsa == null || rsa.KeySize != 2048)
				throw new ArgumentException(null, nameof(pem));

			return new(certificate.SerialNumber.ToUpperInvariant(), VerificationMode.PlatformCertificate, rsa.ExportSubjectPublicKeyInfo(), new(certificate.NotBefore, certificate.NotAfter));
		}

		public bool Verify(ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature)
		{
			if(!this.Validity.IsValid())
				return false;

			using var rsa = RSA.Create();
			rsa.ImportSubjectPublicKeyInfo(_publicKey, out _);
			return rsa.VerifyData(data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
		}

		public byte[] Encrypt(string text)
		{
			if(!this.Validity.IsValid())
				throw new CryptographicException();

			using var rsa = RSA.Create();
			rsa.ImportSubjectPublicKeyInfo(_publicKey, out _);
			return rsa.Encrypt(Encoding.UTF8.GetBytes(text), RSAEncryptionPadding.OaepSHA1);
		}
		#endregion

		#region 内部方法
		internal VerificationKey Restrict(DateTime effective, DateTime expiration)
		{
			var start = effective.ToUniversalTime() > this.Validity.Start ? effective.ToUniversalTime() : this.Validity.Start.Value;
			var final = expiration.ToUniversalTime() < this.Validity.Final ? expiration.ToUniversalTime() : this.Validity.Final.Value;

			if(start >= final)
				throw new CryptographicException();

			return new(this.Identifier, this.Mode, _publicKey, new(start, final));
		}

		internal bool HasSameKey(VerificationKey other) => _publicKey.AsSpan().SequenceEqual(other._publicKey);
		#endregion
	}
}

public enum VerificationMode
{
	PublicKey = 1,
	PlatformCertificate = 2,
}
