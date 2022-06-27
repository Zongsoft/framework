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
 * Copyright (C) 2010-2022 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Security.Cryptography;

namespace Zongsoft.Security
{
	public partial class Certificate : ICertificate, IEquatable<Certificate>
	{
		#region 构造函数
		public Certificate(string identifier, string format, string algorithm = null, ICertificateIssuer issuer = null, ICertificateSubject subject = null, ISignaturer signaturer = null) :
			this(identifier, format, algorithm, default, issuer, subject, signaturer) { }

		public Certificate(string identifier, string format, CertificateValidity validity, ICertificateIssuer issuer = null, ICertificateSubject subject = null, ISignaturer signaturer = null) :
			this(identifier, format, null, validity, issuer, subject, signaturer) { }

		public Certificate(string identifier, string format, string algorithm, CertificateValidity validity, ICertificateIssuer issuer = null, ICertificateSubject subject = null, ISignaturer signaturer = null)
		{
			if(string.IsNullOrEmpty(identifier))
				throw new ArgumentNullException(nameof(identifier));
			if(string.IsNullOrEmpty(format))
				throw new ArgumentNullException(nameof(format));

			this.Identifier = identifier;
			this.Format = format.ToUpperInvariant();
			this.Algorithm = string.IsNullOrEmpty(algorithm) ? "SHA256" : algorithm;
			this.Validity = validity;
			this.Issuer = issuer;
			this.Subject = subject;
			this.Signaturer = signaturer;
		}
		#endregion

		#region 公共属性
		public string Identifier { get; }
		public string Format { get; }
		public string Algorithm { get; }
		public ICertificateIssuer Issuer { get; set; }
		public ICertificateSubject Subject { get; set; }
		public CertificateValidity Validity { get; set; }
		public ISignaturer Signaturer { get; }
		#endregion

		#region 公共方法
		public virtual object GetProtocol() => null;
		#endregion

		#region 重写方法
		public virtual bool Equals(Certificate other) => other is not null && string.Equals(this.Identifier, other.Identifier) && string.Equals(this.Format, other.Format);
		public bool Equals(ICertificate other) => this.Equals(other as Certificate);
		public override bool Equals(object obj) => obj is Certificate other && this.Equals(other);
		public override int GetHashCode() => HashCode.Combine(this.Format, this.Identifier);
		public override string ToString() => $"[{this.Format}]{this.Identifier}:{this.Subject}({this.Issuer})";
		#endregion

		#region  静态构造
		public static ICertificate FromX509(byte[] data, string secret = null) => X509.Resolver.Resolve(data, secret);
		public static ICertificate FromX509(Stream data, string secret = null) => X509.Resolver.Resolve(data, secret);
		public static ICertificate FromX509(byte[] data, ICertificateIssuer issuer, string secret = null) => X509.Resolver.Resolve(data, secret, new CertificateDescriptor(null, default, issuer));
		public static ICertificate FromX509(byte[] data, ICertificateIssuer issuer, ICertificateSubject subject, string secret = null) => X509.Resolver.Resolve(data, secret, new CertificateDescriptor(null, default, issuer, subject));

		public static ICertificate FromPem(string identifier, Stream data, CertificateValidity validity, ICertificateIssuer issuer = null, ICertificateSubject subject = null, string secret = null) =>
			RsaCertificate.Resolver.Resolve(data, secret, new CertificateDescriptor(identifier, validity, issuer, subject));
		public static ICertificate FromPem(string identifier, byte[] data, CertificateValidity validity, ICertificateIssuer issuer = null, ICertificateSubject subject = null, string secret = null) =>
			RsaCertificate.Resolver.Resolve(data, secret, new CertificateDescriptor(identifier, validity, issuer, subject));

		public static ICertificate FromRSAPublicKey(string identifier, byte[] data, CertificateValidity validity, ICertificateIssuer issuer = null, ICertificateSubject subject = null)
		{
			var rsa = RSA.Create();
			rsa.ImportRSAPublicKey(data, out _);
			return new RsaCertificate(rsa, new CertificateDescriptor(identifier, validity, issuer, subject));
		}

		public static ICertificate FromRSAPrivateKey(string identifier, byte[] data, CertificateValidity validity, ICertificateIssuer issuer = null, ICertificateSubject subject = null)
		{
			var rsa = RSA.Create();
			rsa.ImportRSAPrivateKey(data, out _);
			return new RsaCertificate(rsa, new CertificateDescriptor(identifier, validity, issuer, subject));
		}

		public static ICertificate FromPkcs8PrivateKey(string identifier, byte[] data, CertificateValidity validity, ICertificateIssuer issuer = null, ICertificateSubject subject = null, string secret = null)
		{
			var rsa = RSA.Create();

			if(string.IsNullOrEmpty(secret))
				rsa.ImportPkcs8PrivateKey(data, out _);
			else
				rsa.ImportEncryptedPkcs8PrivateKey(secret, data, out _);

			return new RsaCertificate(rsa, new CertificateDescriptor(identifier, validity, issuer, subject));
		}
		#endregion

		#region 内部方法
		internal static HashAlgorithmName GetHashAlgorithm(string algorithm)
		{
			if(string.IsNullOrEmpty(algorithm))
				return HashAlgorithmName.SHA256;

			return algorithm switch
			{
				"MD5" => HashAlgorithmName.MD5,
				"md5" => HashAlgorithmName.MD5,
				"SHA1" => HashAlgorithmName.SHA1,
				"sha1" => HashAlgorithmName.SHA1,
				"SHA256" => HashAlgorithmName.SHA256,
				"sha256" => HashAlgorithmName.SHA256,
				"SHA384" => HashAlgorithmName.SHA384,
				"sha384" => HashAlgorithmName.SHA384,
				"SHA512" => HashAlgorithmName.SHA512,
				"sha512" => HashAlgorithmName.SHA512,
				_ => throw new ArgumentException($"The specified '{algorithm}' is an unrecognized hash-algorithm."),
			};
		}
		#endregion

		#region 公共子类
		public class CertificateIssuer : ICertificateIssuer
		{
			public CertificateIssuer(string identifier, string name = null)
			{
				this.Identifier = identifier;
				this.Name = name;
			}

			public string Identifier { get; }
			public string Name { get; }

			public override string ToString() => string.IsNullOrEmpty(this.Name) ? this.Identifier : $"{this.Identifier}:{this.Name}";
		}

		public class CertificateSubject : ICertificateSubject
		{
			public CertificateSubject(string identifier, string name = null)
			{
				this.Identifier = identifier;
				this.Name = name;
			}

			public string Identifier { get; }
			public string Name { get; }

			public override string ToString() => string.IsNullOrEmpty(this.Name) ? this.Identifier : $"{this.Identifier}:{this.Name}";
		}
		#endregion

		#region 私有子类
		private class RSASignaturer : ISignaturer, IDisposable
		{
			private readonly RSA _rsa;
			private readonly string _algorithm;

			public RSASignaturer(RSA rsa, string algorithm)
			{
				_rsa = rsa;
				_algorithm = algorithm;
			}

			public string Name => "RSA";

			public byte[] Signature(ReadOnlySpan<byte> data, string algorithm = null) =>
				_rsa.SignData(data.ToArray(), GetHashAlgorithm(algorithm ?? _algorithm), RSASignaturePadding.Pkcs1);

			public bool Verify(ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature, string algorithm = null) =>
				_rsa.VerifyData(data, signature, GetHashAlgorithm(algorithm ?? _algorithm), RSASignaturePadding.Pkcs1);

			public void Dispose() => _rsa.Dispose();
		}
		#endregion
	}
}
